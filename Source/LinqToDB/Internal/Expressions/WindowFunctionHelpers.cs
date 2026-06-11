using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
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

		public static Expression BuildRank(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.Rank(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildDenseRank(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.DenseRank(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildPercentRank(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.PercentRank(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildCumeDist(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.CumeDist(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildNTile(Expression nTileArg, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((int n, WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.NTile(n, f => f.UseWindow(w)), nTileArg, windowDefinition);
		}

		// Pre-found MethodInfo for generic window functions
		static readonly MethodInfo _leadMethodInfo       = MemberHelper.MethodOfGeneric(() => Sql.Window.Lead(1, f => f.OrderBy(1)));
		static readonly MethodInfo _leadOffMethodInfo    = MemberHelper.MethodOfGeneric(() => Sql.Window.Lead(1, 1, f => f.OrderBy(1)));
		static readonly MethodInfo _leadOffDefMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.Lead(1, 1, 1, f => f.OrderBy(1)));
		static readonly MethodInfo _lagMethodInfo        = MemberHelper.MethodOfGeneric(() => Sql.Window.Lag(1, f => f.OrderBy(1)));
		static readonly MethodInfo _lagOffMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.Lag(1, 1, f => f.OrderBy(1)));
		static readonly MethodInfo _lagOffDefMethodInfo  = MemberHelper.MethodOfGeneric(() => Sql.Window.Lag(1, 1, 1, f => f.OrderBy(1)));
		static readonly MethodInfo _firstValueMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.FirstValue(1, f => f.OrderBy(1)));
		static readonly MethodInfo _lastValueMethodInfo  = MemberHelper.MethodOfGeneric(() => Sql.Window.LastValue(1, f => f.OrderBy(1)));
		static readonly MethodInfo _nthValueMethodInfo   = MemberHelper.MethodOfGeneric(() => Sql.Window.NthValue(1, 1L, f => f.OrderBy(1)));

		// Pre-found MethodInfo for concrete-typed aggregate window functions (use name to find type-specific overload)
		internal static readonly MethodInfo SumMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Sum(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo AvgMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Average(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo MinMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Min(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo MaxMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Max(0, f => f.OrderBy(1)));

		static readonly MethodInfo _countArgMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Count((object?)0, f => f.OrderBy(1)));

		// Builds `f => [f.Distinct()].UseWindow(windowDefinition)`. Legacy AggregateModifier.Distinct maps to the
		// builder's Distinct(); AggregateModifier.All is the SQL default and emits nothing (MAX(ALL x) == MAX(x)).
		// builderType must expose IDistinctPart (IAggregateFinal) when modifier may be Distinct.
		static LambdaExpression BuildAggregateUseWindowLambda(Type builderType, Expression windowDefinition, Sql.AggregateModifier modifier)
		{
			var windowParam = Expression.Parameter(builderType, "f");
			Expression body = windowParam;

			if (modifier == Sql.AggregateModifier.Distinct)
			{
				var distinctMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IDistinctPart<>.Distinct), 0);
				body = Expression.Call(body, distinctMethod);
			}

			var useWindowMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IUseWindow<>.UseWindow), 1);
			body = Expression.Call(body, useWindowMethod, windowDefinition);
			return Expression.Lambda(body, windowParam);
		}

		/// <summary>
		/// Finds the concrete overload of a non-generic window function (Sum, Avg, Min, Max)
		/// matching the argument type.
		/// </summary>
		static MethodInfo? FindConcreteOverload(MethodInfo sampleMethod, Type argumentType)
		{
			if (sampleMethod.GetParameters()[1].ParameterType == argumentType)
				return sampleMethod;

			// Returns null when no Sql.Window overload matches the value type — callers fall back to the legacy pipeline.
			return sampleMethod.DeclaringType!.GetMethods()
				.FirstOrDefault(m => string.Equals(m.Name, sampleMethod.Name, StringComparison.Ordinal)
					&& m.GetParameters().Length == sampleMethod.GetParameters().Length
					&& m.GetParameters()[1].ParameterType == argumentType);
		}

		// Builds `f => [f.IgnoreNulls()].UseWindow(windowDefinition)`. Legacy IGNORE NULLS maps to the builder's
		// IgnoreNulls(); RESPECT/None is the SQL default and emits nothing. builderType must expose INullTreatmentPart
		// (e.g. IValueFinal/ILeadLagFinal/INthValueFinal) when nullTreatment may be Ignore.
		static LambdaExpression BuildUseWindowLambda(Type builderType, Expression windowDefinition, Sql.Nulls nullTreatment)
		{
			var windowParam = Expression.Parameter(builderType, "f");
			Expression body = windowParam;

			if (nullTreatment == Sql.Nulls.Ignore)
			{
				var ignoreMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.INullTreatmentPart<>.IgnoreNulls), 0);
				body = Expression.Call(body, ignoreMethod);
			}

			var useWindowMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IUseWindow<>.UseWindow), 1);
			body = Expression.Call(body, useWindowMethod, windowDefinition);
			return Expression.Lambda(body, windowParam);
		}

		static Expression BuildWindowFunctionWithGenericArg(MethodInfo genericMethod, Expression argument, Expression windowDefinition, Type windowInterfaceType, Sql.Nulls nullTreatment)
		{
			var method       = genericMethod.MakeGenericMethod(argument.Type);
			var windowLambda = BuildUseWindowLambda(windowInterfaceType, windowDefinition, nullTreatment);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
		}

		static Expression? BuildWindowFunctionWithConcreteArg(MethodInfo sampleMethod, Expression argument, Expression windowDefinition, Type windowInterfaceType, Sql.AggregateModifier modifier)
		{
			var method = FindConcreteOverload(sampleMethod, argument.Type);
			if (method == null)
				return null; // no matching Sql.Window overload for this value type (e.g. Average<TR>(object? expr)) — fall back to the legacy pipeline

			var windowLambda = BuildAggregateUseWindowLambda(windowInterfaceType, windowDefinition, modifier);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
		}

		public static Expression? BuildSum(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
			=> BuildWindowFunctionWithConcreteArg(SumMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildAverage(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
			=> BuildWindowFunctionWithConcreteArg(AvgMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildMin(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
			=> BuildWindowFunctionWithConcreteArg(MinMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildMax(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
			=> BuildWindowFunctionWithConcreteArg(MaxMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression BuildCount(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.Count(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildCount(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var argumentObject   = argument.Type.IsValueType ? Expression.Convert(argument, typeof(object)) : argument;
			var windowLambda     = BuildAggregateUseWindowLambda(typeof(WindowFunctionBuilder.IAggregateFinal), windowDefinition, modifier);
			return Expression.Call(_countArgMethodInfo, Expression.Constant(Sql.Window), argumentObject, windowLambda);
		}

		public static Expression BuildLead(Expression argument, Expression? offset, Expression? defaultValue, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var windowLambda     = BuildUseWindowLambda(typeof(WindowFunctionBuilder.ILeadLagFinal), windowDefinition, nullTreatment);

			if (offset != null && defaultValue != null)
			{
				var method = _leadOffDefMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, offset, defaultValue, windowLambda);
			}

			if (offset != null)
			{
				var method = _leadOffMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, offset, windowLambda);
			}

			{
				var method = _leadMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
			}
		}

		public static Expression BuildLag(Expression argument, Expression? offset, Expression? defaultValue, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var windowLambda     = BuildUseWindowLambda(typeof(WindowFunctionBuilder.ILeadLagFinal), windowDefinition, nullTreatment);

			if (offset != null && defaultValue != null)
			{
				var method = _lagOffDefMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, offset, defaultValue, windowLambda);
			}

			if (offset != null)
			{
				var method = _lagOffMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, offset, windowLambda);
			}

			{
				var method = _lagMethodInfo.MakeGenericMethod(argument.Type);
				return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
			}
		}

		public static Expression BuildFirstValue(Expression argument, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
			=> BuildWindowFunctionWithGenericArg(_firstValueMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IValueFinal), nullTreatment);

		public static Expression BuildLastValue(Expression argument, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
			=> BuildWindowFunctionWithGenericArg(_lastValueMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IValueFinal), nullTreatment);

		public static Expression BuildNthValue(Expression argument, Expression nArg, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy)
		{
			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var method           = _nthValueMethodInfo.MakeGenericMethod(argument.Type);
			var windowLambda     = BuildUseWindowLambda(typeof(WindowFunctionBuilder.INthValueFinal), windowDefinition, nullTreatment);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, nArg, windowLambda);
		}

		/// <summary>
		/// Builds an aggregate window function with KEEP (DENSE_RANK FIRST/LAST) clause.
		/// </summary>
		public static Expression? BuildAggregateWithKeep(
			MethodInfo sampleMethod, Expression argument, bool isKeepFirst,
			Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] keepOrderBy)
		{
			var method      = FindConcreteOverload(sampleMethod, argument.Type);
			if (method == null)
				return null; // no matching Sql.Window overload for this value type — fall back to the legacy pipeline

			var windowParam = Expression.Parameter(typeof(WindowFunctionBuilder.IAggregateFinal), "f");

			// Build: f.KeepFirst() or f.KeepLast()
			var keepMethodName = isKeepFirst
				? nameof(WindowFunctionBuilder.IKeepPart<>.KeepFirst)
				: nameof(WindowFunctionBuilder.IKeepPart<>.KeepLast);
			var keepMethod = FindMethodInfo(windowParam.Type, keepMethodName, 0);
			Expression body = Expression.Call(windowParam, keepMethod);

			// Build: .OrderBy(expr)[.ThenBy(expr)]
			for (var i = 0; i < keepOrderBy.Length; i++)
			{
				// KEEP (DENSE_RANK FIRST/LAST) ORDER BY does not carry a NULLS position in the legacy conversion.
				var (expr, descending, _) = keepOrderBy[i];
				var orderMethodName = (descending, i) switch
				{
					(true, 0)  => nameof(WindowFunctionBuilder.IOrderByPart<>.OrderByDesc),
					(true, _)  => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc),
					(false, 0) => nameof(WindowFunctionBuilder.IOrderByPart<>.OrderBy),
					(false, _) => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenBy),
				};

				var orderMethod = FindMethodInfo(body.Type, orderMethodName, 1);
				body = Expression.Call(body, orderMethod, ExpressionHelpers.EnsureObject(expr));
			}

			// Build: .PartitionBy(expr1, expr2, ...)
			if (partitionBy.Length > 0)
			{
				var objects         = partitionBy.Select(ExpressionHelpers.EnsureObject);
				var partitionArray  = Expression.NewArrayInit(typeof(object), objects);
				var partitionMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IPartitionPart<>.PartitionBy), 1);
				body = Expression.Call(body, partitionMethod, partitionArray);
			}

			var windowLambda = Expression.Lambda(body, windowParam);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
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
					var nulls = (Sql.NullsPosition)mc.Arguments[2].EvaluateExpression()!;
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
