using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;

#pragma warning disable MA0048

namespace LinqToDB.Internal.Expressions
{
	public static class WindowFunctionHelpers
	{
		public static Expression BuildWindowDefinition(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowParam = Expression.Parameter(typeof(WindowFunctionBuilder.IWindowBuilder), "w");

			Expression windowBody = windowParam;

			if (partitionBy.Length > 0)
			{
				var objects = partitionBy.Select(ExpressionHelpers.EnsureObject);

				var partitionByPart = Expression.NewArrayInit(typeof(object), objects);

				var partitionCall = ExpressionHelpers.MakeCall((WindowFunctionBuilder.IWindowBuilder b, object[] partition) => b.PartitionBy(partition), windowBody, partitionByPart);

				windowBody = partitionCall;
			}

			if (orderBy.Length > 0)
			{
				for (var index = 0; index < orderBy.Length; index++)
				{
					var (expr, descending, nulls) = orderBy[index];

					string method;

					method = (descending, index) switch
                    {
                        (true, 0) => nameof(WindowFunctionBuilder.IWindowBuilder.OrderByDesc),
                        (true, _) => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc),
                        (false, 0) => nameof(WindowFunctionBuilder.IWindowBuilder.OrderBy),
                        (false, _) => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenBy),
                    };

					if (nulls != Sql.NullsPosition.None)
					{
						var methodInfo = FindMethodInfo(windowBody.Type, method, 2);
						windowBody = Expression.Call(windowBody, methodInfo, ExpressionHelpers.EnsureObject(expr), Expression.Constant(nulls));
					}
					else
					{
						var methodInfo = FindMethodInfo(windowBody.Type, method, 1);
						windowBody = Expression.Call(windowBody, methodInfo, ExpressionHelpers.EnsureObject(expr));
					}
				}
			}

			var defineLambda = Expression.Lambda(windowBody, windowParam);

			var defineCall = Expression.Call(typeof(WindowFunctionBuilder), nameof(WindowFunctionBuilder.DefineWindow), Type.EmptyTypes, Expression.Constant(Sql.Window), defineLambda);

			return defineCall;
		}

		public static Expression BuildRowNumber(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var rowNumberCall    = ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.RowNumber(f => f.UseWindow(w)), windowDefinition);

			return rowNumberCall;
		}

		// A null nulls element means the ordering key did not specify a NULLS position (plain BCL OrderBy);
		// a non-null value (including Sql.NullsPosition.None) means it was specified explicitly. Consumers use
		// this to tell "use the configured default" apart from "explicitly opt out of the default".
		public static (LambdaExpression lambda, bool isDescending, Sql.NullsPosition? nulls)[] ExtractOrderByPart(Expression query, out Expression nonOrderedPart)
		{
			var orderBy = new List<(LambdaExpression lambda, bool isDescending, Sql.NullsPosition? nulls)>();

			var current = query;
			while (current.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)current;

				var supported = true;

				// Only the 2-argument key-selector overloads are extractable. The 3-argument BCL comparer overloads
				// (OrderBy(keySelector, IComparer<TKey>)) must not be treated as the 2-argument form here — that would
				// silently drop the comparer. Leaving them unsupported stops extraction, so they flow to OrderByBuilder,
				// which rejects them (a custom comparer has no SQL equivalent).
				if ((typeof(Queryable) == mc.Method.DeclaringType || typeof(Enumerable) == mc.Method.DeclaringType)
					&& mc.Arguments.Count == 2)
				{
					switch (mc.Method.Name)
					{
						case nameof(Enumerable.OrderBy):
						case nameof(Enumerable.ThenBy):
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), false, null));
							break;
						case nameof(Enumerable.OrderByDescending):
						case nameof(Enumerable.ThenByDescending):
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), true, null));
							break;
						default:
							supported = false;
							break;
					}
				}
				// linq2db OrderBy/ThenBy overloads that carry an explicit Sql.NullsPosition.
				else if (typeof(LinqExtensions) == mc.Method.DeclaringType
					&& mc.Arguments.Count == 3
					&& mc.Method.GetParameters()[2].ParameterType == typeof(Sql.NullsPosition))
				{
					var nulls = (Sql.NullsPosition)((ConstantExpression)mc.Arguments[2]).Value!;
					switch (mc.Method.Name)
					{
						case nameof(LinqExtensions.OrderBy):
						case nameof(LinqExtensions.ThenBy):
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), false, nulls));
							break;
						case nameof(LinqExtensions.OrderByDescending):
						case nameof(LinqExtensions.ThenByDescending):
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), true, nulls));
							break;
						default:
							supported = false;
							break;
					}
				}
				else
					supported = false;

				if (!supported)
					break;

				current = mc.Arguments[0];
			}

			nonOrderedPart = current;
			orderBy.Reverse();

			return orderBy.ToArray();
		}

		public static Expression ApplyOrderBy(Expression queryExpr, IEnumerable<(LambdaExpression lambda, bool isDescending, Sql.NullsPosition nulls)> order)
		{
			var isFirst = true;
			foreach (var (lambda, isDescending, nulls) in order)
			{
				var isQueryable = typeof(IQueryable<>).IsSameOrParentOf(queryExpr.Type);

				// The incoming position is already resolved (the caller applied any configured default), so always
				// re-emit via the linq2db NULLS-aware overload on the queryable path — including Sql.NullsPosition.None
				// — so OrderByBuilder treats it as explicit and does not re-apply the default a second time.
				if (isQueryable)
				{
					var methodName =
						isFirst ? isDescending ? nameof(LinqExtensions.OrderByDescending) : nameof(LinqExtensions.OrderBy)
						: isDescending ? nameof(LinqExtensions.ThenByDescending) : nameof(LinqExtensions.ThenBy);

					queryExpr = Expression.Call(typeof(LinqExtensions), methodName, [lambda.Parameters[0].Type, lambda.Body.Type], queryExpr, Expression.Quote(lambda), Expression.Constant(nulls));
				}
				else
				{
					// In-memory (IEnumerable) ordering has no NULLS-aware overload; the position is not meaningful here.
					var methodName =
						isFirst ? isDescending ? nameof(Enumerable.OrderByDescending) : nameof(Enumerable.OrderBy)
						: isDescending ? nameof(Enumerable.ThenByDescending) : nameof(Enumerable.ThenBy);

					queryExpr = Expression.Call(typeof(Enumerable), methodName, [lambda.Parameters[0].Type, lambda.Body.Type], queryExpr, lambda);
				}

				isFirst   = false;
			}

			return queryExpr;
		}

		public static Expression BuildAggregateExecuteExpression<TSource, TResult>(IQueryable<TSource> source, Expression<Func<IEnumerable<TSource>, TResult>> aggregate)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(aggregate);

			var executeExpression = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [typeof(TSource), typeof(TResult)], source.Expression, aggregate);

			return executeExpression;
		}

		public static Expression BuildAggregateExecuteExpression(MethodCallExpression methodCall, int sequenceIndex = 0)
		{
			ArgumentNullException.ThrowIfNull(methodCall);

			var sequenceArgument = methodCall.Arguments[sequenceIndex];
			var elementType      = TypeHelper.GetEnumerableElementType(sequenceArgument.Type);
			var sourceParam      = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
			var resultType       = methodCall.Type;

			Type[] typeArguments = methodCall.Method.IsGenericMethod ? [elementType, resultType] : [];

			var aggregationBody = Expression.Call(methodCall.Method.DeclaringType!, methodCall.Method.Name, typeArguments,
				[..methodCall.Arguments.Take(sequenceIndex), sourceParam, ..methodCall.Arguments.Skip(sequenceIndex + 1).Select(a => a.Unwrap())]
			);

			var aggregationLambda = Expression.Lambda(aggregationBody, sourceParam);

			var method = Methods.LinqToDB.AggregateExecute.MakeGenericMethod(elementType, resultType);

			var queryableArgument = sequenceArgument;
			var queryableType     = typeof(IQueryable<>).MakeGenericType(elementType);
		
			if (queryableArgument.Type != queryableType)
			{
				queryableArgument = Expression.Call(
					Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
					queryableArgument);
			}

			var executeExpression = Expression.Call(method, queryableArgument, aggregationLambda);

			return executeExpression;
		}

		static MethodInfo? FindMethodInfoInType(Type type, string methodName, int paramCount)
		{
			var method = type.GetRuntimeMethods()
				.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal) && m.GetParameters().Length == paramCount);
			return method;
		}

		static MethodInfo FindMethodInfo(Type type, string methodName, int paramCount)
		{
			var method = FindMethodInfoInType(type, methodName, paramCount);

			if (method != null)
				return method;

			method = type.GetInterfaces().Select(it => FindMethodInfoInType(it, methodName, paramCount))
				.FirstOrDefault(m => m != null);

			if (method == null)
				throw new InvalidOperationException($"Method '{methodName}' not found in type '{type.Name}'.");

			return method;
		}

	}
}
