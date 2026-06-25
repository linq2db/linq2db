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
		/// <summary>
		/// Describes a converted window frame: <c>ROWS</c>/<c>RANGE</c> plus its start and end boundaries.
		/// <paramref name="StartMember"/>/<paramref name="EndMember"/> are the
		/// <see cref="WindowFunctionBuilder.IBoundaryPart{T}"/> member names — <c>"Unbounded"</c>, <c>"CurrentRow"</c>,
		/// <c>"ValuePreceding"</c>, or <c>"ValueFollowing"</c>; the matching value is the offset expression for the
		/// <c>Value*</c> members and <see langword="null"/> otherwise.
		/// </summary>
		public readonly record struct WindowFrameSpec(bool IsRange, string StartMember, Expression? StartValue, string EndMember, Expression? EndValue);

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

		// Statistical aggregates — generic on the argument type, always return double?.
		static readonly MethodInfo _stdDevMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.StdDev(1.0,     f => f.OrderBy(1)));
		static readonly MethodInfo _stdDevPopMethodInfo  = MemberHelper.MethodOfGeneric(() => Sql.Window.StdDevPop(1.0,  f => f.OrderBy(1)));
		static readonly MethodInfo _stdDevSampMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.StdDevSamp(1.0, f => f.OrderBy(1)));
		static readonly MethodInfo _varianceMethodInfo   = MemberHelper.MethodOfGeneric(() => Sql.Window.Variance(1.0,   f => f.OrderBy(1)));
		static readonly MethodInfo _varPopMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.VarPop(1.0,     f => f.OrderBy(1)));
		static readonly MethodInfo _varSampMethodInfo    = MemberHelper.MethodOfGeneric(() => Sql.Window.VarSamp(1.0,    f => f.OrderBy(1)));

		// RATIO_TO_REPORT (not a statistical aggregate; kept out of the aligned block above).
		static readonly MethodInfo _ratioToReportMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.RatioToReport(1.0, f => f.PartitionBy(1)));

		// MEDIAN — partition-only OVER (IOPartitionFinal builder).
		static readonly MethodInfo _medianMethodInfo        = MemberHelper.MethodOfGeneric(() => Sql.Window.Median(1.0, f => f.PartitionBy(1)));

		// Windowed ordered-set aggregates: PERCENTILE_CONT/DISC(fraction) WITHIN GROUP (ORDER BY k) OVER (PARTITION BY ...).
		static readonly MethodInfo _percentileContWindowedMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.PercentileCont(1.0, w => w.OrderBy(1)));
		static readonly MethodInfo _percentileDiscWindowedMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.PercentileDisc(1.0, w => w.OrderBy(1)));

		// Two-argument statistical aggregates (covariance/correlation/regression) — generic on both argument types.
		static readonly MethodInfo _covarPopMethodInfo      = MemberHelper.MethodOfGeneric(() => Sql.Window.CovarPop(1.0, 1.0,      f => f.OrderBy(1)));
		static readonly MethodInfo _covarSampMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.CovarSamp(1.0, 1.0,     f => f.OrderBy(1)));
		static readonly MethodInfo _corrMethodInfo          = MemberHelper.MethodOfGeneric(() => Sql.Window.Corr(1.0, 1.0,          f => f.OrderBy(1)));
		static readonly MethodInfo _regrSlopeMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrSlope(1.0, 1.0,     f => f.OrderBy(1)));
		static readonly MethodInfo _regrInterceptMethodInfo = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrIntercept(1.0, 1.0, f => f.OrderBy(1)));
		static readonly MethodInfo _regrCountMethodInfo     = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrCount(1.0, 1.0,     f => f.OrderBy(1)));
		static readonly MethodInfo _regrR2MethodInfo        = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrR2(1.0, 1.0,        f => f.OrderBy(1)));
		static readonly MethodInfo _regrAvgXMethodInfo      = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrAvgX(1.0, 1.0,      f => f.OrderBy(1)));
		static readonly MethodInfo _regrAvgYMethodInfo      = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrAvgY(1.0, 1.0,      f => f.OrderBy(1)));
		static readonly MethodInfo _regrSXXMethodInfo       = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrSXX(1.0, 1.0,       f => f.OrderBy(1)));
		static readonly MethodInfo _regrSYYMethodInfo       = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrSYY(1.0, 1.0,       f => f.OrderBy(1)));
		static readonly MethodInfo _regrSXYMethodInfo       = MemberHelper.MethodOfGeneric(() => Sql.Window.RegrSXY(1.0, 1.0,       f => f.OrderBy(1)));

		// Pre-found MethodInfo for concrete-typed aggregate window functions (use name to find type-specific overload)
		internal static readonly MethodInfo SumMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Sum(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo AvgMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Average(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo MinMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Min(0, f => f.OrderBy(1)));
		internal static readonly MethodInfo MaxMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Max(0, f => f.OrderBy(1)));

		static readonly MethodInfo _countArgMethodInfo = MemberHelper.MethodOf(() => Sql.Window.Count((object?)0, f => f.OrderBy(1)));
		static readonly MethodInfo _countMethodInfo    = MemberHelper.MethodOf(() => Sql.Window.Count(f => f.OrderBy(1)));
		static readonly MethodInfo _longCountMethodInfo    = MemberHelper.MethodOf(() => Sql.Window.LongCount(f => f.OrderBy(1)));
		static readonly MethodInfo _longCountArgMethodInfo = MemberHelper.MethodOf(() => Sql.Window.LongCount((object?)0, f => f.OrderBy(1)));

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

		// Builds `f => [f.IgnoreNulls()|f.RespectNulls()].UseWindow(windowDefinition)`. Legacy IGNORE/RESPECT NULLS
		// map to the builder's IgnoreNulls()/RespectNulls(); only None emits nothing. RESPECT must be carried through
		// explicitly — some providers (e.g. ClickHouse) do not default FIRST_VALUE/LAST_VALUE to RESPECT NULLS, and
		// the per-provider emission still drops the token where it is the natural default. builderType must expose
		// INullTreatmentPart (e.g. IValueFinal/ILeadLagFinal/INthValueFinal) when nullTreatment may be set.
		static LambdaExpression BuildUseWindowLambda(Type builderType, Expression windowDefinition, Sql.Nulls nullTreatment)
		{
			var windowParam = Expression.Parameter(builderType, "f");
			Expression body = windowParam;

			if (nullTreatment == Sql.Nulls.Ignore)
			{
				var ignoreMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.INullTreatmentPart<>.IgnoreNulls), 0);
				body = Expression.Call(body, ignoreMethod);
			}
			else if (nullTreatment == Sql.Nulls.Respect)
			{
				var respectMethod = FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.INullTreatmentPart<>.RespectNulls), 0);
				body = Expression.Call(body, respectMethod);
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

		public static Expression? BuildSum(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> frame is { } sumFrame
				? BuildConcreteAggregateWithFrame(SumMethodInfo, argument, partitionBy, orderBy, modifier, sumFrame)
				: BuildWindowFunctionWithConcreteArg(SumMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildAverage(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> frame is { } avgFrame
				? BuildConcreteAggregateWithFrame(AvgMethodInfo, argument, partitionBy, orderBy, modifier, avgFrame)
				: BuildWindowFunctionWithConcreteArg(AvgMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildMin(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> frame is { } minFrame
				? BuildConcreteAggregateWithFrame(MinMethodInfo, argument, partitionBy, orderBy, modifier, minFrame)
				: BuildWindowFunctionWithConcreteArg(MinMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression? BuildMax(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> frame is { } maxFrame
				? BuildConcreteAggregateWithFrame(MaxMethodInfo, argument, partitionBy, orderBy, modifier, maxFrame)
				: BuildWindowFunctionWithConcreteArg(MaxMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IAggregateFinal), modifier);

		public static Expression BuildCount(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
		{
			if (frame is { } f)
			{
				var frameLambda = BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IOFilterOPartitionOOrderOFrameFinal), partitionBy, orderBy, Sql.AggregateModifier.None, Sql.Nulls.None, f);
				return Expression.Call(_countMethodInfo, Expression.Constant(Sql.Window), frameLambda);
			}

			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.Count(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildCount(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
		{
			var argumentObject = argument.Type.IsValueType ? Expression.Convert(argument, typeof(object)) : argument;

			if (frame is { } f)
			{
				var frameLambda = BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IAggregateFinal), partitionBy, orderBy, modifier, Sql.Nulls.None, f);
				return Expression.Call(_countArgMethodInfo, Expression.Constant(Sql.Window), argumentObject, frameLambda);
			}

			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var windowLambda     = BuildAggregateUseWindowLambda(typeof(WindowFunctionBuilder.IAggregateFinal), windowDefinition, modifier);
			return Expression.Call(_countArgMethodInfo, Expression.Constant(Sql.Window), argumentObject, windowLambda);
		}

		// LongCount == COUNT returning long. Same SQL/builders as Count; only the CLR result type differs.
		public static Expression BuildLongCount(Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
		{
			if (frame is { } f)
			{
				var frameLambda = BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IOFilterOPartitionOOrderOFrameFinal), partitionBy, orderBy, Sql.AggregateModifier.None, Sql.Nulls.None, f);
				return Expression.Call(_longCountMethodInfo, Expression.Constant(Sql.Window), frameLambda);
			}

			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			return ExpressionHelpers.MakeCall((WindowFunctionBuilder.IDefinedWindow w) => Sql.Window.LongCount(f => f.UseWindow(w)), windowDefinition);
		}

		public static Expression BuildLongCount(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
		{
			var argumentObject = argument.Type.IsValueType ? Expression.Convert(argument, typeof(object)) : argument;

			if (frame is { } f)
			{
				var frameLambda = BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IAggregateFinal), partitionBy, orderBy, modifier, Sql.Nulls.None, f);
				return Expression.Call(_longCountArgMethodInfo, Expression.Constant(Sql.Window), argumentObject, frameLambda);
			}

			var windowDefinition = BuildWindowDefinition(partitionBy, orderBy);
			var windowLambda     = BuildAggregateUseWindowLambda(typeof(WindowFunctionBuilder.IAggregateFinal), windowDefinition, modifier);
			return Expression.Call(_longCountArgMethodInfo, Expression.Constant(Sql.Window), argumentObject, windowLambda);
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

		public static Expression BuildFirstValue(Expression argument, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> frame is { } f
				? BuildGenericValueWithFrame(_firstValueMethodInfo, argument, typeof(WindowFunctionBuilder.IValueFinal), nullTreatment, partitionBy, orderBy, f)
				: BuildWindowFunctionWithGenericArg(_firstValueMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IValueFinal), nullTreatment);

		public static Expression BuildLastValue(Expression argument, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> frame is { } f
				? BuildGenericValueWithFrame(_lastValueMethodInfo, argument, typeof(WindowFunctionBuilder.IValueFinal), nullTreatment, partitionBy, orderBy, f)
				: BuildWindowFunctionWithGenericArg(_lastValueMethodInfo, argument, BuildWindowDefinition(partitionBy, orderBy), typeof(WindowFunctionBuilder.IValueFinal), nullTreatment);

		public static Expression BuildNthValue(Expression argument, Expression nArg, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
		{
			var method       = _nthValueMethodInfo.MakeGenericMethod(argument.Type);
			var windowLambda = frame is { } f
				? BuildInlineFrameLambda(typeof(WindowFunctionBuilder.INthValueFinal), partitionBy, orderBy, Sql.AggregateModifier.None, nullTreatment, f)
				: BuildUseWindowLambda(typeof(WindowFunctionBuilder.INthValueFinal), BuildWindowDefinition(partitionBy, orderBy), nullTreatment);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, nArg, windowLambda);
		}

		// Statistical aggregates (STDDEV/VAR_POP/...) are generic on the argument type and always return double?,
		// so a single generic method + MakeGenericMethod(arg.Type) handles any T (including object — sidestepping
		// the typed-overload matching that Sum/Average need). They are IAggregateFinal, so Distinct/Filter/frame apply.
		static Expression BuildGenericAggregate(MethodInfo genericMethod, Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier, WindowFrameSpec? frame)
		{
			var method       = genericMethod.MakeGenericMethod(argument.Type);
			var windowLambda = frame is { } f
				? BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IAggregateFinal), partitionBy, orderBy, modifier, Sql.Nulls.None, f)
				: BuildAggregateUseWindowLambda(typeof(WindowFunctionBuilder.IAggregateFinal), BuildWindowDefinition(partitionBy, orderBy), modifier);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, windowLambda);
		}

		public static Expression BuildStdDev(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_stdDevMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		public static Expression BuildStdDevPop(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_stdDevPopMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		public static Expression BuildStdDevSamp(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_stdDevSampMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		public static Expression BuildVariance(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_varianceMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		public static Expression BuildVarPop(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_varPopMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		public static Expression BuildVarSamp(Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, WindowFrameSpec? frame = null)
			=> BuildGenericAggregate(_varSampMethodInfo, argument, partitionBy, orderBy, modifier, frame);

		// Builds the Sql.Window.RatioToReport call. Like MEDIAN, RATIO_TO_REPORT's OVER clause carries PARTITION BY only;
		// the translator emits it natively (Oracle/DB2) or emulates it via BuildRatioToReportEmulation elsewhere.
		public static Expression BuildRatioToReport(Expression argument, Expression[] partitionBy)
			=> BuildPartitionOnlyAggregate(_ratioToReportMethodInfo, argument, partitionBy);

		// Builds the Sql.Window.Median call. MEDIAN's OVER clause carries PARTITION BY only.
		public static Expression BuildMedian(Expression argument, Expression[] partitionBy)
			=> BuildPartitionOnlyAggregate(_medianMethodInfo, argument, partitionBy);

		// Builds the windowed Sql.Window.PercentileCont call from a converted legacy WITHIN GROUP ... OVER (...) chain.
		public static Expression BuildPercentileContWindowed(Expression fraction, (Expression expr, bool descending, Sql.NullsPosition nulls)[] withinGroupOrderBy, Expression[] partitionBy)
			=> BuildPercentileWindowed(_percentileContWindowedMethodInfo, typeof(WindowFunctionBuilder.IOrderedSetWindowSingleOrder), fraction, withinGroupOrderBy, partitionBy);

		// Builds the windowed Sql.Window.PercentileDisc call from a converted legacy WITHIN GROUP ... OVER (...) chain.
		public static Expression BuildPercentileDiscWindowed(Expression fraction, (Expression expr, bool descending, Sql.NullsPosition nulls)[] withinGroupOrderBy, Expression[] partitionBy)
			=> BuildPercentileWindowed(_percentileDiscWindowedMethodInfo, typeof(WindowFunctionBuilder.IOrderedSetWindowMultiOrder), fraction, withinGroupOrderBy, partitionBy);

		// Builds Sql.Window.PercentileCont/Disc(fraction, w => w.OrderBy(key)[.PartitionBy(...)]). The legacy ordered-set
		// form carries a single WITHIN GROUP key, so only the first order entry is used; PartitionBy maps to the OVER clause.
		static Expression BuildPercentileWindowed(MethodInfo genericMethod, Type entryInterface, Expression fraction, (Expression expr, bool descending, Sql.NullsPosition nulls)[] withinGroupOrderBy, Expression[] partitionBy)
		{
			var (orderExpr, descending, _) = withinGroupOrderBy[0];
			var valueType = orderExpr.Type;

			var param = Expression.Parameter(entryInterface, "w");

			// w.OrderBy<TKey>(key) / w.OrderByDesc<TKey>(key) -> IPartitionPart<IDefinedFunction<TKey>>
			var orderMethod = entryInterface
				.GetMethods()
				.First(m => string.Equals(m.Name, descending ? "OrderByDesc" : "OrderBy", StringComparison.Ordinal) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
				.MakeGenericMethod(valueType);
			Expression body = Expression.Call(param, orderMethod, orderExpr);

			// Optional OVER (PARTITION BY ...)
			if (partitionBy.Length > 0)
			{
				var partitionArr   = Expression.NewArrayInit(typeof(object), partitionBy.Select(ExpressionHelpers.EnsureObject));
				var partitionIface = typeof(WindowFunctionBuilder.IPartitionPart<>).MakeGenericType(typeof(WindowFunctionBuilder.IDefinedFunction<>).MakeGenericType(valueType));
				body = Expression.Call(body, partitionIface.GetMethod("PartitionBy")!, partitionArr);
			}

			var resultType = typeof(WindowFunctionBuilder.IDefinedFunction<>).MakeGenericType(valueType);
			var lambda     = Expression.Lambda(typeof(Func<,>).MakeGenericType(entryInterface, resultType), body, param);

			var fractionArg = fraction.Type == typeof(double) ? fraction : Expression.Convert(fraction, typeof(double));

			return Expression.Call(genericMethod.MakeGenericMethod(valueType), Expression.Constant(Sql.Window), fractionArg, lambda);
		}

		// Shared builder for partition-only window aggregates (MEDIAN, RATIO_TO_REPORT): their OVER clause carries only
		// PARTITION BY, so this builds the IOPartitionFinal lambda directly rather than the shared aggregate (UseWindow) path.
		static Expression BuildPartitionOnlyAggregate(MethodInfo genericMethod, Expression argument, Expression[] partitionBy)
		{
			var param = Expression.Parameter(typeof(WindowFunctionBuilder.IOPartitionFinal), "f");

			Expression body = param;
			if (partitionBy.Length > 0)
			{
				var partitionArr = Expression.NewArrayInit(typeof(object), partitionBy.Select(ExpressionHelpers.EnsureObject));
				body = ExpressionHelpers.MakeCall((WindowFunctionBuilder.IOPartitionFinal b, object[] partition) => b.PartitionBy(partition), param, partitionArr);
			}

			var lambda = Expression.Lambda<Func<WindowFunctionBuilder.IOPartitionFinal, WindowFunctionBuilder.IDefinedFunction>>(body, param);

			return Expression.Call(genericMethod.MakeGenericMethod(argument.Type), Expression.Constant(Sql.Window), argument, lambda);
		}

		// Emulation for providers without native RATIO_TO_REPORT: expr / SUM(expr) OVER (PARTITION BY ...). The builder
		// lambda is partition-only (IOPartitionFinal), so rebuild its PARTITION BY for SUM (which uses IAggregateFinal).
		internal static Expression? BuildRatioToReportEmulation(Expression argument, Expression windowFuncLambda)
		{
			var sumCall = BuildSum(argument, ExtractPartitionBy(windowFuncLambda), []);
			if (sumCall == null)
				return null;

			return Expression.Divide(Expression.Convert(argument, typeof(double?)), Expression.Convert(sumCall, typeof(double?)));
		}

		// Extracts the PARTITION BY expressions from a partition-only window-builder lambda (f => f.PartitionBy(a, b)).
		static Expression[] ExtractPartitionBy(Expression windowFuncLambda)
		{
			var lambda = windowFuncLambda as LambdaExpression ?? (windowFuncLambda as UnaryExpression)?.Operand as LambdaExpression;

			if (lambda?.Body is MethodCallExpression { Arguments: [NewArrayExpression arr] } call && string.Equals(call.Method.Name, "PartitionBy", StringComparison.Ordinal))
				return arr.Expressions
					.Select(e => e is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : e)
					.ToArray();

			return [];
		}

		// Two-argument statistical aggregate (COVAR_POP(x, y) etc.) — generic on both argument types, returns double?.
		static Expression BuildGeneric2ArgAggregate(MethodInfo genericMethod, Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame)
		{
			var method       = genericMethod.MakeGenericMethod(argument1.Type, argument2.Type);
			var windowLambda = frame is { } f
				? BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IBivariateAggregateFinal), partitionBy, orderBy, Sql.AggregateModifier.None, Sql.Nulls.None, f)
				: BuildAggregateUseWindowLambda(typeof(WindowFunctionBuilder.IBivariateAggregateFinal), BuildWindowDefinition(partitionBy, orderBy), Sql.AggregateModifier.None);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument1, argument2, windowLambda);
		}

		public static Expression BuildCovarPop(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_covarPopMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildCovarSamp(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_covarSampMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildCorr(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_corrMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrSlope(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrSlopeMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrIntercept(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrInterceptMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrCount(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrCountMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrR2(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrR2MethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrAvgX(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrAvgXMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrAvgY(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrAvgYMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrSXX(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrSXXMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrSYY(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrSYYMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		public static Expression BuildRegrSXY(Expression argument1, Expression argument2, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec? frame = null)
			=> BuildGeneric2ArgAggregate(_regrSXYMethodInfo, argument1, argument2, partitionBy, orderBy, frame);

		/// <summary>
		/// Builds an aggregate window function with KEEP (DENSE_RANK FIRST/LAST) clause.
		/// </summary>
		public static Expression? BuildAggregateWithKeep(
			MethodInfo sampleMethod, Expression argument, bool isKeepFirst,
			Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] keepOrderBy)
		{
			// Sum/Average/Min/Max are non-generic per-type overloads (resolved by argument type); the statistical
			// aggregates (Variance/StdDev/...) are generic methods closed over the argument type.
			var method      = sampleMethod.IsGenericMethodDefinition
				? sampleMethod.MakeGenericMethod(argument.Type)
				: FindConcreteOverload(sampleMethod, argument.Type);
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

		/// <summary>
		/// Builds a KEEP (DENSE_RANK FIRST/LAST) aggregate window function for the named legacy single-argument
		/// aggregate (Sum/Average/Min/Max and the statistical aggregates). Returns <see langword="null"/> for
		/// functions that have no single-argument KEEP form, so the caller can decide how to handle them.
		/// </summary>
		internal static Expression? BuildAggregateWithKeep(
			string functionName, Expression argument, bool isKeepFirst,
			Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] keepOrderBy)
		{
			MethodInfo? sampleMethod = functionName switch
			{
				nameof(AnalyticFunctions.Sum)        => SumMethodInfo,
				nameof(AnalyticFunctions.Average)    => AvgMethodInfo,
				nameof(AnalyticFunctions.Min)        => MinMethodInfo,
				nameof(AnalyticFunctions.Max)        => MaxMethodInfo,
				nameof(AnalyticFunctions.StdDev)     => _stdDevMethodInfo,
				nameof(AnalyticFunctions.StdDevPop)  => _stdDevPopMethodInfo,
				nameof(AnalyticFunctions.StdDevSamp) => _stdDevSampMethodInfo,
				nameof(AnalyticFunctions.Variance)   => _varianceMethodInfo,
				nameof(AnalyticFunctions.VarPop)     => _varPopMethodInfo,
				nameof(AnalyticFunctions.VarSamp)    => _varSampMethodInfo,
				_                                    => null,
			};

			return sampleMethod == null
				? null
				: BuildAggregateWithKeep(sampleMethod, argument, isKeepFirst, partitionBy, keepOrderBy);
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

		// --- Frame conversion (legacy Sql.Ext ROWS/RANGE chains -> new Sql.Window inline frame builder) ---
		// The frame lives on the function builder's inline path (PartitionBy/OrderBy/RowsBetween), NOT after UseWindow
		// (UseWindow returns IDefinedFunction, which has no frame), so framed functions build the window inline here.

		static LambdaExpression BuildInlineFrameLambda(
			Type                                                          builderType,
			Expression[]                                                  partitionBy,
			(Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy,
			Sql.AggregateModifier                                         modifier,
			Sql.Nulls                                                     nullTreatment,
			WindowFrameSpec                                               frame)
		{
			var        param = Expression.Parameter(builderType, "f");
			Expression body  = param;

			if (modifier == Sql.AggregateModifier.Distinct)
				body = Expression.Call(body, FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IDistinctPart<>.Distinct), 0));
			else if (nullTreatment == Sql.Nulls.Ignore)
				body = Expression.Call(body, FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.INullTreatmentPart<>.IgnoreNulls), 0));
			else if (nullTreatment == Sql.Nulls.Respect)
				body = Expression.Call(body, FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.INullTreatmentPart<>.RespectNulls), 0));

			if (partitionBy.Length > 0)
			{
				var partition = Expression.NewArrayInit(typeof(object), partitionBy.Select(ExpressionHelpers.EnsureObject));
				body = Expression.Call(body, FindMethodInfo(body.Type, nameof(WindowFunctionBuilder.IPartitionPart<>.PartitionBy), 1), partition);
			}

			for (var index = 0; index < orderBy.Length; index++)
			{
				var (expr, descending, nulls) = orderBy[index];

				var method = (descending, index) switch
				{
					(true,  0) => nameof(WindowFunctionBuilder.IOrderByPart<>.OrderByDesc),
					(true,  _) => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc),
					(false, 0) => nameof(WindowFunctionBuilder.IOrderByPart<>.OrderBy),
					(false, _) => nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenBy),
				};

				if (nulls != Sql.NullsPosition.None)
					body = Expression.Call(body, FindMethodInfo(body.Type, method, 2), ExpressionHelpers.EnsureObject(expr), Expression.Constant(nulls));
				else
					body = Expression.Call(body, FindMethodInfo(body.Type, method, 1), ExpressionHelpers.EnsureObject(expr));
			}

			// <Rows|Range>Between . <start> . And . <end>
			body = Expression.Property(body, FindPropertyInfo(body.Type,
				frame.IsRange ? nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetween) : nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetween)));
			body = ApplyFrameBoundary(body, frame.StartMember, frame.StartValue);
			body = Expression.Property(body, FindPropertyInfo(body.Type, nameof(WindowFunctionBuilder.IRangePrecedingPartFunction.And)));
			body = ApplyFrameBoundary(body, frame.EndMember, frame.EndValue);

			return Expression.Lambda(body, param);
		}

		// Unbounded / CurrentRow are properties; ValuePreceding / ValueFollowing are methods taking the offset.
		static Expression ApplyFrameBoundary(Expression body, string member, Expression? value)
			=> value != null
				? Expression.Call(body, FindMethodInfo(body.Type, member, 1), ExpressionHelpers.EnsureObject(value))
				: Expression.Property(body, FindPropertyInfo(body.Type, member));

		static Expression? BuildConcreteAggregateWithFrame(MethodInfo sampleMethod, Expression argument, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, Sql.AggregateModifier modifier, WindowFrameSpec frame)
		{
			var method = FindConcreteOverload(sampleMethod, argument.Type);
			if (method == null)
				return null; // no matching Sql.Window overload for this value type — fall back to the legacy pipeline

			var lambda = BuildInlineFrameLambda(typeof(WindowFunctionBuilder.IAggregateFinal), partitionBy, orderBy, modifier, Sql.Nulls.None, frame);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, lambda);
		}

		static Expression BuildGenericValueWithFrame(MethodInfo genericMethod, Expression argument, Type builderType, Sql.Nulls nullTreatment, Expression[] partitionBy, (Expression expr, bool descending, Sql.NullsPosition nulls)[] orderBy, WindowFrameSpec frame)
		{
			var method = genericMethod.MakeGenericMethod(argument.Type);
			var lambda = BuildInlineFrameLambda(builderType, partitionBy, orderBy, Sql.AggregateModifier.None, nullTreatment, frame);
			return Expression.Call(method, Expression.Constant(Sql.Window), argument, lambda);
		}

		static PropertyInfo FindPropertyInfo(Type type, string propertyName)
		{
			var property = type.GetRuntimeProperty(propertyName)
				?? type.GetInterfaces().Select(it => it.GetRuntimeProperty(propertyName)).FirstOrDefault(p => p != null);

			if (property == null)
				throw new InvalidOperationException($"Property '{propertyName}' not found in type '{type.Name}'.");

			return property;
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
