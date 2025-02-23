using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Extensions;

namespace LinqToDB.Expressions.Internal
{
	public static class WindowFunctionHelpers
	{
		public static Expression BuildWindowDefinition(Expression[] partitionBy, (Expression expr, bool descending)[] orderBy)
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
					var (expr, descending) = orderBy[index];

					string method;

					method = descending ? 
						index == 0 ? nameof(WindowFunctionBuilder.IWindowBuilder.OrderByDesc) : nameof(WindowFunctionBuilder.IThenOrderPart<object>.ThenByDesc) :
						index == 0 ? nameof(WindowFunctionBuilder.IWindowBuilder.OrderBy) : nameof(WindowFunctionBuilder.IThenOrderPart<object>.ThenBy);

					var methodInfo = FindMethodInfo(windowBody.Type, method, 1);

					windowBody = Expression.Call(windowBody, methodInfo, ExpressionHelpers.EnsureObject(expr));
				}
			}

			var defineLambda = Expression.Lambda(windowBody, windowParam);

			var defineCall = Expression.Call(typeof(WindowFunctionBuilder), nameof(WindowFunctionBuilder.DefineWindow), Type.EmptyTypes, Expression.Constant(Sql.Window), defineLambda);

			return defineCall;
		}

		public static Expression BuildRowNumber(Expression[] partitionBy, (Expression expr, bool descending)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var rowNumberCall    = ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.RowNumber(f => f.UseWindow(w)), windowDefinition);

			return rowNumberCall;
		}

		public static (LambdaExpression lambda, bool isDescending)[] ExtractOrderByPart(Expression query, out Expression nonOrderedPart)
		{
			var orderBy = new List<(LambdaExpression lambda, bool isDescending)>();

			var current = query;
			while (current.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)current;
				if (typeof(Queryable) == mc.Method.DeclaringType || typeof(Enumerable) == mc.Method.DeclaringType)
				{
					var supported = true;
					switch (mc.Method.Name)
					{
						case nameof(Enumerable.OrderBy):
						case nameof(Enumerable.ThenBy):
						{
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), false));
							break;
						}
						case nameof(Enumerable.OrderByDescending):
						case nameof(Enumerable.ThenByDescending):
						{
							orderBy.Add((mc.Arguments[1].UnwrapLambda(), true));
							break;
						}
						default:
							supported = false;
							break;
					}

					if (!supported)
						break;

					current = mc.Arguments[0];
				}
				else
					break;
			}

			nonOrderedPart = current;
			orderBy.Reverse();

			return orderBy.ToArray();
		}

		public static Expression ApplyOrderBy(Expression queryExpr, IEnumerable<(LambdaExpression lambda, bool isDescending)> order)
		{
			var isFirst = true;
			foreach (var tuple in order)
			{
				var methodName =
					isFirst ? tuple.Item2 ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy)
					: tuple.Item2 ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);

				if (typeof(IQueryable<>).IsSameOrParentOf(queryExpr.Type))
				{
					queryExpr = Expression.Call(typeof(Queryable), methodName, [tuple.lambda.Parameters[0].Type, tuple.lambda.Body.Type], queryExpr, Expression.Quote(tuple.lambda));
				}
				else
				{
					queryExpr = Expression.Call(typeof(Enumerable), methodName, [tuple.lambda.Parameters[0].Type, tuple.lambda.Body.Type], queryExpr, tuple.lambda);
				}

				isFirst   = false;
			}

			return queryExpr;
		}

		public static Expression BuildAggregateExecuteExpression<TSource, TResult>(IQueryable<TSource> source, Expression<Func<IEnumerable<TSource>, TResult>> aggregate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

			var executeExpression = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [typeof(TSource), typeof(TResult)], source.Expression, aggregate);

			return executeExpression;
		}

		public static Expression BuildAggregateExecuteExpression(MethodCallExpression methodCall)
		{
			if (methodCall == null) throw new ArgumentNullException(nameof(methodCall));

			var elementType = TypeHelper.GetEnumerableElementType(methodCall.Arguments[0].Type);
			var sourceParam = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "source");
			var resultType  = methodCall.Type;

			var aggregationBody = Expression.Call(methodCall.Method.DeclaringType!, methodCall.Method.Name, [elementType, resultType],
				[sourceParam, ..methodCall.Arguments.Skip(1).Select(a => a.Unwrap())]
			);

			var aggregationLambda = Expression.Lambda(aggregationBody, sourceParam);

			var executeExpression = Expression.Call(typeof(LinqExtensions), nameof(LinqExtensions.AggregateExecute), [elementType, resultType], methodCall.Arguments[0], aggregationLambda);

			return executeExpression;
		}

		static MethodInfo? FindMethodInfoInType(Type type, string methodName, int paramCount)
		{
			var method = type.GetRuntimeMethods()
				.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == paramCount);
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
