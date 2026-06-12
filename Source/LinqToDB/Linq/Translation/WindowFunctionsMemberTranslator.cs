using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public class WindowFunctionsMemberTranslator : MemberTranslatorBase
	{
		// Function support flags — override in provider subclasses
		protected virtual bool IsWindowFunctionsSupported => true;
		protected virtual bool IsPercentRankSupported     => true;
		protected virtual bool IsCumeDistSupported        => true;
		protected virtual bool IsNTileSupported           => true;
		protected virtual bool IsNthValueSupported        => true;
		protected virtual bool IsLeadLagSupported         => true;
		protected virtual bool IsFirstLastValueSupported  => true;
		protected virtual bool IsPercentileContSupported           => true;
		protected virtual bool IsPercentileDiscSupported           => true;
		protected virtual bool IsAggregateWindowFunctionsSupported => true;

		// Window clause support flags
		protected virtual bool IsWindowFilterSupported         => false;
		protected virtual bool IsNullsOrderSupported           => false;
		protected virtual bool IsFrameRowsSupported            => true;
		protected virtual bool IsFrameRangeSupported           => true;
		protected virtual bool IsFrameGroupsSupported          => true;
		protected virtual bool IsFrameExclusionSupported       => true;
		protected virtual bool IsKeepSupported                 => false;
		// IGNORE NULLS support is split by function family because some providers (e.g. YDB) support it
		// for FIRST_VALUE/LAST_VALUE/NTH_VALUE but not for LEAD/LAG.
		protected virtual bool IsLeadLagNullTreatmentSupported => false;
		protected virtual bool IsValueNullTreatmentSupported   => false;
		protected virtual bool IsNthValueFromSupported         => false;
		// DISTINCT in a window aggregate (SUM(DISTINCT x) OVER ...) is unsupported by most databases.
		protected virtual bool IsAggregateDistinctSupported    => false;
		// FILTER (WHERE ...) on ordered-set aggregates (PERCENTILE_CONT/DISC WITHIN GROUP). Unlike the OVER-clause
		// FILTER it cannot be CASE-WHEN-emulated (it filters the input set), so it is gated separately. PostgreSQL only.
		protected virtual bool IsOrderedSetFilterSupported     => false;

		public WindowFunctionsMemberTranslator()
		{
			Registration.RegisterMethod(() => Sql.Window.RowNumber(f => f.OrderBy(1)), TranslateRowNumber);
			Registration.RegisterMethod(() => Sql.Window.Rank(f => f.OrderBy(1)), TranslateRank);
			Registration.RegisterMethod(() => Sql.Window.DenseRank(f => f.OrderBy(1)), TranslateDenseRank);
			Registration.RegisterMethod(() => Sql.Window.PercentRank(f => f.OrderBy(1)), TranslatePercentRank);
			Registration.RegisterMethod(() => Sql.Window.CumeDist(f => f.OrderBy(1)), TranslateCumeDist);
			Registration.RegisterMethod(() => Sql.Window.NTile(1, f => f.OrderBy(1)), TranslateNTile);

			Registration.RegisterMethod((IEnumerable<int> g) => g.PercentileCont(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileCont, isGenericTypeMatch: true);
			Registration.RegisterMethod((IQueryable<int>  g) => g.PercentileCont(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileCont, isGenericTypeMatch: true);

			Registration.RegisterMethod((IEnumerable<int> g) => g.PercentileDisc(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileDisc, isGenericTypeMatch: true);
			Registration.RegisterMethod((IQueryable<int>  g) => g.PercentileDisc(0.5, (e, f) => f.OrderBy(e)), TranslatePercentileDisc, isGenericTypeMatch: true);

			Registration.RegisterMethod(() => Sql.Window.Count(f => f.OrderBy(1)), TranslateCount);
			Registration.RegisterMethod(() => Sql.Window.Count(1, f => f.OrderBy(1)), TranslateCount);

			// LongCount is COUNT returning long — same SQL, reuse TranslateCount (it derives the type from methodCall.Type).
			Registration.RegisterMethod(() => Sql.Window.LongCount(f => f.OrderBy(1)), TranslateCount);
			Registration.RegisterMethod(() => Sql.Window.LongCount(1, f => f.OrderBy(1)), TranslateCount);

			Registration.RegisterMethod(() => Sql.Window.Lead(1,    f => f.OrderBy(1)), TranslateLead, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Lead(1, 1, f => f.OrderBy(1)), TranslateLead, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Lead(1, 1, 1, f => f.OrderBy(1)), TranslateLead, isGenericTypeMatch: true);

			Registration.RegisterMethod(() => Sql.Window.Lag(1,    f => f.OrderBy(1)), TranslateLag, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Lag(1, 1, f => f.OrderBy(1)), TranslateLag, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Lag(1, 1, 1, f => f.OrderBy(1)), TranslateLag, isGenericTypeMatch: true);

			Registration.RegisterMethod(() => Sql.Window.FirstValue(1, f => f.OrderBy(1)), TranslateFirstValue, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.LastValue(1,  f => f.OrderBy(1)), TranslateLastValue, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.NthValue(1, 1L, f => f.OrderBy(1)), TranslateNthValue, isGenericTypeMatch: true);

			RegisterSum();
			RegisterAvg();
			RegisterMin();
			RegisterMax();
			RegisterStatistical();
		}

		void RegisterSum()
		{
			Registration.RegisterMethod(() => Sql.Window.Sum((int)1,        f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((int?)1,       f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((long)1L,      f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((long?)1L,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((double)1.0,   f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((double?)1.0,  f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((decimal)1.0,  f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((decimal?)1.0, f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((float)1f,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((float?)1f,    f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((short)1,      f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((short?)1,     f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((byte)1,       f => f.OrderBy(1)), TranslateSum);
			Registration.RegisterMethod(() => Sql.Window.Sum((byte?)1,      f => f.OrderBy(1)), TranslateSum);
		}

		void RegisterAvg()
		{
			Registration.RegisterMethod(() => Sql.Window.Average((int)1,        f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((int?)1,       f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((long)1L,      f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((long?)1L,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((double)1.0,   f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((double?)1.0,  f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((decimal)1.0,  f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((decimal?)1.0, f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((float)1f,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((float?)1f,    f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((short)1,      f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((short?)1,     f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((byte)1,       f => f.OrderBy(1)), TranslateAverage);
			Registration.RegisterMethod(() => Sql.Window.Average((byte?)1,      f => f.OrderBy(1)), TranslateAverage);
		}

		void RegisterMin()
		{
			Registration.RegisterMethod(() => Sql.Window.Min((int)1,        f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((int?)1,       f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((long)1L,      f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((long?)1L,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((double)1.0,   f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((double?)1.0,  f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((decimal)1.0,  f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((decimal?)1.0, f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((float)1f,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((float?)1f,    f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((short)1,      f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((short?)1,     f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((byte)1,       f => f.OrderBy(1)), TranslateMin);
			Registration.RegisterMethod(() => Sql.Window.Min((byte?)1,      f => f.OrderBy(1)), TranslateMin);
		}

		void RegisterMax()
		{
			Registration.RegisterMethod(() => Sql.Window.Max((int)1,        f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((int?)1,       f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((long)1L,      f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((long?)1L,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((double)1.0,   f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((double?)1.0,  f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((decimal)1.0,  f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((decimal?)1.0, f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((float)1f,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((float?)1f,    f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((short)1,      f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((short?)1,     f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((byte)1,       f => f.OrderBy(1)), TranslateMax);
			Registration.RegisterMethod(() => Sql.Window.Max((byte?)1,      f => f.OrderBy(1)), TranslateMax);
		}

		void RegisterStatistical()
		{
			// Statistical aggregates always return double? regardless of argument type, so a single generic
			// registration each (isGenericTypeMatch) covers every T — no per-type overloads needed.
			Registration.RegisterMethod(() => Sql.Window.StdDev(1.0,     f => f.OrderBy(1)), TranslateStdDev,     isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.StdDevPop(1.0,  f => f.OrderBy(1)), TranslateStdDevPop,  isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.StdDevSamp(1.0, f => f.OrderBy(1)), TranslateStdDevSamp, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Variance(1.0,   f => f.OrderBy(1)), TranslateVariance,   isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.VarPop(1.0,     f => f.OrderBy(1)), TranslateVarPop,     isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.VarSamp(1.0,    f => f.OrderBy(1)), TranslateVarSamp,    isGenericTypeMatch: true);

			Registration.RegisterMethod(() => Sql.Window.CovarPop(1.0, 1.0,      f => f.OrderBy(1)), TranslateCovarPop,      isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.CovarSamp(1.0, 1.0,     f => f.OrderBy(1)), TranslateCovarSamp,     isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.Corr(1.0, 1.0,          f => f.OrderBy(1)), TranslateCorr,          isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrSlope(1.0, 1.0,     f => f.OrderBy(1)), TranslateRegrSlope,     isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrIntercept(1.0, 1.0, f => f.OrderBy(1)), TranslateRegrIntercept, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrCount(1.0, 1.0,     f => f.OrderBy(1)), TranslateRegrCount,     isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrR2(1.0, 1.0,        f => f.OrderBy(1)), TranslateRegrR2,        isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrAvgX(1.0, 1.0,      f => f.OrderBy(1)), TranslateRegrAvgX,      isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrAvgY(1.0, 1.0,      f => f.OrderBy(1)), TranslateRegrAvgY,      isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrSXX(1.0, 1.0,       f => f.OrderBy(1)), TranslateRegrSXX,       isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrSYY(1.0, 1.0,       f => f.OrderBy(1)), TranslateRegrSYY,       isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Window.RegrSXY(1.0, 1.0,       f => f.OrderBy(1)), TranslateRegrSXY,       isGenericTypeMatch: true);
		}

		public record ArgumentInformation(Expression Expr, Sql.AggregateModifier Modifier);
		public record OrderByInformation(Expression Expr, bool IsDescending, Sql.NullsPosition Nulls);
		public record FrameBoundary(bool IsPreceding, SqlFrameBoundary.FrameBoundaryType BoundaryType, Expression? Offset);

		public class WindowFunctionInformation
		{
			public required ArgumentInformation[]?            Arguments      { get; set; }
			public required Expression[]?                     PartitionBy    { get; set; }
			public required OrderByInformation[]?             OrderBy        { get; set; }
			public required Expression?                       Filter         { get; set; }
			public required SqlFrameClause.FrameTypeKind?     FrameType      { get; set; }
			public required FrameBoundary?                    Start          { get; set; }
			public required FrameBoundary?                    End            { get; set; }
			public required SqlFrameClause.FrameExclusionKind FrameExclusion { get; set; }
			public required SqlKeepClause.KeepType?           KeepType       { get; set; }
			public required OrderByInformation[]?             KeepOrderBy    { get; set; }
			public          Sql.Nulls                         NullTreatment  { get; set; }
			public          Sql.From                          FromPosition   { get; set; }
		}

		static bool TryParseOrderByMethod(MethodCallExpression mc, ref List<OrderByInformation>? list, out Expression? next)
		{
			switch (mc.Method.Name)
			{
				case nameof(WindowFunctionBuilder.IOrderByPart<>.OrderBy):
				case nameof(WindowFunctionBuilder.IOrderByPart<>.OrderByDesc):
				case nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenBy):
				case nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc):
				{
					var isDesc = mc.Method.Name
						is nameof(WindowFunctionBuilder.IOrderByPart<>.OrderByDesc)
						or nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc);

					list ??= new();

					// Strip the object-cast the builder's OrderBy(object?) overload introduces (EnsureObject in the
					// legacy converter, or the compiler's boxing on a direct call). A bare column survives either way,
					// but a wrapped scalar subquery — e.g. OrderBy(db.Select(() => "x")) — only translates unwrapped.
					var argument = mc.Arguments[0].UnwrapConvertToObject();
					var nulls    = mc.Arguments.Count == 2
						? (Sql.NullsPosition)mc.Arguments[1].EvaluateExpression()!
						: Sql.NullsPosition.None;

					list.Insert(0, new OrderByInformation(argument, isDesc, nulls));
					next = mc.Object!;
					return true;
				}
			}

			next = null;
			return false;
		}

		protected static bool CollectWindowFunctionInformation(
			ITranslationContext                                 translationContext,
			Type                                                expressionType,
			Expression[]?                                       functionArguments,
			Expression                                          buildBody,
			[NotNullWhen(true)] out  WindowFunctionInformation? functionInfo,
			[NotNullWhen(false)] out SqlErrorExpression?        error)
		{
			functionInfo = null;
			error        = null;

			List<ArgumentInformation>? argumentsList   = null;
			List<Expression>?          partitionByList = null;
			List<OrderByInformation>?  orderByList     = null;
			Expression?                filter          = null;

			SqlFrameClause.FrameTypeKind?     frameType         = null;
			SqlFrameClause.FrameExclusionKind frameExclusion    = SqlFrameClause.FrameExclusionKind.None;
			FrameBoundary?                    endBoundary       = null;
			FrameBoundary?                    startBoundary     = null;
			SqlKeepClause.KeepType?           keepType          = null;
			List<OrderByInformation>?         keepOrderByList   = null;
			Sql.Nulls                         nullTreatment     = Sql.Nulls.None;
			Sql.From                          fromPosition      = Sql.From.None;
			Sql.AggregateModifier             aggregateModifier = Sql.AggregateModifier.None;

			if (functionArguments != null)
			{
				argumentsList ??= new();
				foreach (var argument in functionArguments)
				{
					argumentsList.Add(new(argument, Sql.AggregateModifier.None));
				}
			}

			while (true)
			{
				var current = buildBody;

				if (buildBody is MethodCallExpression mc)
				{
					switch (mc.Method.Name)
					{
						case nameof(WindowFunctionBuilder.IOrderByPart<>.OrderBy):
						case nameof(WindowFunctionBuilder.IOrderByPart<>.OrderByDesc):
						case nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenBy):
						case nameof(WindowFunctionBuilder.IThenOrderPart<>.ThenByDesc):
						{
							TryParseOrderByMethod(mc, ref orderByList, out var next);
							buildBody = next!;
							break;
						}

						case nameof(WindowFunctionBuilder.IPartitionPart<>.PartitionBy):
						{
							partitionByList ??= new();

							if (mc.Arguments[0].NodeType == ExpressionType.NewArrayInit)
							{
								foreach (var argument in ((NewArrayExpression)mc.Arguments[0]).Expressions)
								{
									partitionByList.Add(argument);
								}
							}
							else
							{
								partitionByList.Add(mc.Arguments[0]);
							}

							buildBody = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IDistinctPart<>.Distinct):
						{
							aggregateModifier = Sql.AggregateModifier.Distinct;
							buildBody = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IFilterPart<>.Filter):
						{
							filter = mc.Arguments[0];

							buildBody = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IKeepPart<>.KeepFirst):
						case nameof(WindowFunctionBuilder.IKeepPart<>.KeepLast):
						{
							keepType = string.Equals(mc.Method.Name, nameof(WindowFunctionBuilder.IKeepPart<>.KeepFirst), StringComparison.Ordinal)
								? SqlKeepClause.KeepType.First
								: SqlKeepClause.KeepType.Last;

							// OrderBy collected so far belongs to KEEP, not to the window
							keepOrderByList = orderByList;
							orderByList     = null;

							buildBody = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.INullTreatmentPart<>.IgnoreNulls):
						{
							nullTreatment = Sql.Nulls.Ignore;
							buildBody     = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.INullTreatmentPart<>.RespectNulls):
						{
							nullTreatment = Sql.Nulls.Respect;
							buildBody     = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IFromPart<>.FromFirst):
						{
							fromPosition = Sql.From.First;
							buildBody    = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IFromPart<>.FromLast):
						{
							fromPosition = Sql.From.Last;
							buildBody    = mc.Object!;
							break;
						}

						case nameof(WindowFunctionBuilder.IUseWindow<>.UseWindow):
						{
							// Function-level modifiers (Distinct, IgnoreNulls/RespectNulls, FromFirst/FromLast) are chained
							// BEFORE UseWindow and live on mc.Object — apply them here, otherwise switching buildBody to
							// the referenced window definition below would silently drop them.
							var preChain    = mc.Object;
							var keepWalking = true;
							while (keepWalking && preChain is MethodCallExpression preMc)
							{
								switch (preMc.Method.Name)
								{
									case nameof(WindowFunctionBuilder.IDistinctPart<>.Distinct):          aggregateModifier = Sql.AggregateModifier.Distinct; preChain = preMc.Object; break;
									case nameof(WindowFunctionBuilder.INullTreatmentPart<>.IgnoreNulls):  nullTreatment = Sql.Nulls.Ignore;  preChain = preMc.Object; break;
									case nameof(WindowFunctionBuilder.INullTreatmentPart<>.RespectNulls): nullTreatment = Sql.Nulls.Respect; preChain = preMc.Object; break;
									case nameof(WindowFunctionBuilder.IFromPart<>.FromFirst):             fromPosition  = Sql.From.First;    preChain = preMc.Object; break;
									case nameof(WindowFunctionBuilder.IFromPart<>.FromLast):              fromPosition  = Sql.From.Last;     preChain = preMc.Object; break;
									default:                                                              keepWalking   = false;             break;
								}
							}

							buildBody = mc.Arguments[0];
							var expanded = translationContext.Translate(buildBody, TranslationFlags.Expand);
							if (expanded is MethodCallExpression { Method.Name: nameof(WindowFunctionBuilder.DefineWindow) } mce)
							{
								buildBody = mce.Arguments[1].UnwrapLambda().Body;
							}
							else
							{
								error = translationContext.CreateErrorExpression(buildBody, "Expected window definition", expressionType);
								return false;
							}

							break;
						}

						case nameof(WindowFunctionBuilder.DefineWindow):
						{
							buildBody = mc.Arguments[1];
							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<>.ValuePreceding):
						case nameof(WindowFunctionBuilder.IBoundaryPart<>.ValueFollowing):
						{
							// Direction is explicit: ValuePreceding => PRECEDING, ValueFollowing => FOLLOWING.
							// Start/end slotting stays positional (first parsed = end, second = start).
							var isPreceding = mc.Method.Name switch
							{
								nameof(WindowFunctionBuilder.IBoundaryPart<>.ValuePreceding) => true,
								_                                                            => false,
							};
							var boundary    = new FrameBoundary(isPreceding, SqlFrameBoundary.FrameBoundaryType.Offset, mc.Arguments[0].UnwrapConvertToObject());
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = mc.Object ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetweenValues):
						case nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetweenValues):
						case nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetweenValues):
						{
							// Shortcut: <frame> BETWEEN <preceding> PRECEDING AND <following> FOLLOWING — sets frame type and both boundaries in one call.
							frameType = mc.Method.Name switch
							{
								nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetweenValues) => SqlFrameClause.FrameTypeKind.Groups,
								nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetweenValues)  => SqlFrameClause.FrameTypeKind.Range,
								_                                                                    => SqlFrameClause.FrameTypeKind.Rows,
							};

							startBoundary = new FrameBoundary(true,  SqlFrameBoundary.FrameBoundaryType.Offset, mc.Arguments[0].UnwrapConvertToObject());
							endBoundary   = new FrameBoundary(false, SqlFrameBoundary.FrameBoundaryType.Offset, mc.Arguments[1].UnwrapConvertToObject());

							buildBody = mc.Object ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IDefinedRangeFrameFunction.ExcludeCurrentRow):
						{
							frameExclusion = SqlFrameClause.FrameExclusionKind.CurrentRow;
							buildBody = mc.Object ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IDefinedRangeFrameFunction.ExcludeGroup):
						{
							frameExclusion = SqlFrameClause.FrameExclusionKind.Group;
							buildBody = mc.Object ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IDefinedRangeFrameFunction.ExcludeTies):
						{
							frameExclusion = SqlFrameClause.FrameExclusionKind.Ties;
							buildBody = mc.Object ?? buildBody;
							break;
						}

					}
				}
				else if (buildBody is MemberExpression me)
				{
					switch (me.Member.Name)
					{
						case nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetween):
						case nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetween):
						case nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetween):
						{
							switch (me.Member.Name)
							{
								case nameof(WindowFunctionBuilder.IFramePartFunction.GroupsBetween):
									frameType = SqlFrameClause.FrameTypeKind.Groups;
									break;
								case nameof(WindowFunctionBuilder.IFramePartFunction.RangeBetween):
									frameType = SqlFrameClause.FrameTypeKind.Range;
									break;
								case nameof(WindowFunctionBuilder.IFramePartFunction.RowsBetween):
									frameType = SqlFrameClause.FrameTypeKind.Rows;
									break;
								default:
									error = translationContext.CreateErrorExpression(buildBody, $"Unexpected frame type {me.Member.Name}", expressionType);
									return false;
							}

							buildBody = me.Expression ?? buildBody;

							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<>.CurrentRow):
						{
							var boundary = new FrameBoundary(endBoundary != null, SqlFrameBoundary.FrameBoundaryType.CurrentRow, null);
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = me.Expression ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IBoundaryPart<>.Unbounded):
						{
							var boundary = new FrameBoundary(endBoundary != null, SqlFrameBoundary.FrameBoundaryType.Unbounded, null);
							if (endBoundary == null)
								endBoundary = boundary;
							else
								startBoundary = boundary;

							buildBody = me.Expression ?? buildBody;
							break;
						}

						case nameof(WindowFunctionBuilder.IRangePrecedingPartFunction.And):
						{
							buildBody = me.Expression ?? buildBody;
							break;	
						}
					}
				}

				if (buildBody == current)
				{
					if (current is not ParameterExpression)
					{
						error = translationContext.CreateErrorExpression(buildBody, "Unexpected member.", expressionType);
						return false;
					}

					break;
				}
			}

			if (frameType != null && (startBoundary == null || endBoundary == null))
			{
				error = translationContext.CreateErrorExpression(buildBody, "Expected both start and end boundaries", expressionType);
				return false;
			}

			// .Distinct() applies the DISTINCT modifier to the aggregated argument(s).
			if (aggregateModifier != Sql.AggregateModifier.None && argumentsList != null)
			{
				for (var i = 0; i < argumentsList.Count; i++)
					argumentsList[i] = argumentsList[i] with { Modifier = aggregateModifier };
			}

			functionInfo = new WindowFunctionInformation
			{
				Arguments      = argumentsList?.ToArray(),
				PartitionBy    = partitionByList?.ToArray(),
				OrderBy        = orderByList?.ToArray(),
				Filter         = filter,
				FrameType      = frameType,
				Start          = startBoundary,
				End            = endBoundary,
				FrameExclusion = frameExclusion,
				KeepType       = keepType,
				KeepOrderBy    = keepOrderByList?.ToArray(),
				NullTreatment  = nullTreatment,
				FromPosition   = fromPosition,
			};

			return true;
		}

		protected bool TranslateOrderItems(ISqlExpressionTranslator translator, NullsDefaultOrdering defaultNullsOrdering, Type errorType, IEnumerable<OrderByInformation> orderBy, List<SqlWindowOrderItem> orderItems, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			error = null;
			foreach (var orderItem in orderBy)
			{
				if (!translator.TranslateExpression(orderItem.Expr, out var sql, out error))
				{
					error = error!.WithType(errorType);
					return false;
				}

				// On a provider without native window NULLS ordering, decide whether a CASE emulation key is needed.
				// It is unnecessary — and for a non-nullable key folds to a bare constant (invalid as a window ORDER BY
				// key on SQL Server) — when the key can't be null, or when the requested position already matches the
				// provider's natural NULL placement. NullabilityContext.NonQuery reflects the expression's intrinsic
				// nullability (the query nullability context is not available at translation time).
				if (orderItem.Nulls != Sql.NullsPosition.None && !IsNullsOrderSupported)
				{
					var needsEmulation =
						sql.CanBeNullable(NullabilityContext.NonQuery)
						&& !QueryHelper.MatchesNaturalNullsPosition(defaultNullsOrdering, orderItem.Nulls, orderItem.IsDescending);

					if (needsEmulation)
					{
						// ORDER BY CASE WHEN expr IS NULL THEN 0/1 ELSE 1/0 END, expr
						var nullFirst   = orderItem.Nulls == Sql.NullsPosition.First;
						var isNull      = new SqlSearchCondition().Add(new SqlPredicate.IsNull(sql, false));
						var nullSortVal = new SqlConditionExpression(isNull,
							new SqlValue(nullFirst ? 0 : 1),
							new SqlValue(nullFirst ? 1 : 0));

						orderItems.Add(new SqlWindowOrderItem(nullSortVal, false, Sql.NullsPosition.None));
						orderItems.Add(new SqlWindowOrderItem(sql, orderItem.IsDescending, Sql.NullsPosition.None));
					}
					else
					{
						// Natural ordering already yields the requested position (or the key can't be null) — drop it.
						orderItems.Add(new SqlWindowOrderItem(sql, orderItem.IsDescending, Sql.NullsPosition.None));
					}
				}
				else
				{
					orderItems.Add(new SqlWindowOrderItem(sql, orderItem.IsDescending, orderItem.Nulls));
				}
			}

			return true;
		}

		protected bool TranslatePartitionBy(ISqlExpressionTranslator translator, Type errorType, IEnumerable<Expression> partitionBy, List<ISqlExpression> partitionByItems, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			error = null;
			foreach (var partition in partitionBy)
			{
				if (!translator.TranslateExpression(partition, out var sql, out error))
				{
					error = error!.WithType(errorType);
					return false;
				}

				partitionByItems.Add(sql);
			}

			return true;
		}

		protected Expression TranslateWindowFunction(
			ITranslationContext                    translationContext,
			MethodCallExpression                   methodCall,
			int?                                   argumentIndex,
			int                                    windowArgument,
			DbDataType                             dbDataType,
			string                                 functionName,
			Action<List<SqlFunctionArgument>, bool>? adjustArguments = null)
			=> TranslateWindowFunctionCore(
				translationContext, methodCall,
				argumentIndex == null ? null : [methodCall.Arguments[argumentIndex.Value]],
				windowArgument, dbDataType, functionName, adjustArguments);

		// Shared implementation for single- and multi-argument window functions (was ~120 duplicated lines
		// across TranslateWindowFunction / TranslateWindowFunctionMultiArg).
		Expression TranslateWindowFunctionCore(
			ITranslationContext                      translationContext,
			MethodCallExpression                     methodCall,
			Expression[]?                            functionArguments,
			int                                      windowArgument,
			DbDataType                               dbDataType,
			string                                   functionName,
			Action<List<SqlFunctionArgument>, bool>? adjustArguments)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);

			if (!CollectWindowFunctionInformation(
				    translationContext,
				    methodCall.Type,
				    functionArguments,
				    methodCall.Arguments[windowArgument].UnwrapLambda().Body,
				    out var information,
				    out var error))
				return error;

			// Aggregate window functions: providers that don't fully support them (pre-2012 SQL Server) still allow
			// the bare OVER () / OVER (PARTITION BY ...) forms — only ORDER BY / frames inside OVER for an aggregate
			// need 2012+. Reject just the ordered/framed case so COUNT(*) OVER () / SUM(x) OVER (PARTITION BY ...) work.
			if (!IsAggregateWindowFunctionsSupported
				&& functionName is "SUM" or "AVG" or "MIN" or "MAX" or "COUNT"
				&& (information.OrderBy != null || information.FrameType != null))
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_AggregateWindowFunctions, methodCall.Type);

			var                       arguments   = new List<SqlFunctionArgument>();
			List<ISqlExpression>?     partitionBy = null;
			List<SqlWindowOrderItem>? orderItems  = null;
			SqlSearchCondition?       filter      = null;
			SqlFrameClause?           frame       = null;

			if (information.Arguments != null)
			{
				foreach (var argument in information.Arguments)
				{
					var translated = translationContext.Translate(argument.Expr);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					arguments.Add(new SqlFunctionArgument(placeholder.Sql, argument.Modifier));
				}
			}

			adjustArguments?.Invoke(arguments, information.Filter != null);

			if (information.PartitionBy != null)
			{
				partitionBy ??= new();
				if (!TranslatePartitionBy(translationContext, methodCall.Type, information.PartitionBy, partitionBy, out var partitionError))
					return partitionError;
			}

			if (information.OrderBy != null)
			{
				orderItems ??= new();
				if (!TranslateOrderItems(translationContext, translationContext.ProviderFlags.DefaultNullsOrdering, methodCall.Type, information.OrderBy, orderItems, out var orderError))
					return orderError;
			}

			if (information.Filter != null)
			{
				var translated = translationContext.Translate(information.Filter);
				if (translated is not SqlPlaceholderExpression placeholder || placeholder.Sql is not SqlSearchCondition sc)
					return SqlErrorExpression.EnsureError(translated, methodCall.Type);

				if (IsWindowFilterSupported)
				{
					filter = sc;
				}
				else
				{
					// Emulate FILTER (WHERE cond) with CASE WHEN cond THEN arg ELSE NULL END
					var factory = translationContext.ExpressionFactory;
					for (var i = 0; i < arguments.Count; i++)
					{
						var arg      = arguments[i];
						var argType  = QueryHelper.GetDbDataTypeWithoutSchema(arg.Expression);
						var caseExpr = factory.Condition(sc, arg.Expression, factory.Null(argType));
						arguments[i] = new SqlFunctionArgument(caseExpr, arg.Modifier);
					}
				}
			}

			if (information.FrameType != null)
			{
				var frameType = information.FrameType.Value;

				if (frameType == SqlFrameClause.FrameTypeKind.Rows && !IsFrameRowsSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FrameRows, methodCall.Type);

				if (frameType == SqlFrameClause.FrameTypeKind.Range && !IsFrameRangeSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FrameRange, methodCall.Type);

				if (frameType == SqlFrameClause.FrameTypeKind.Groups && !IsFrameGroupsSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FrameGroups, methodCall.Type);

				if (information.FrameExclusion != SqlFrameClause.FrameExclusionKind.None && !IsFrameExclusionSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FrameExclude, methodCall.Type);

				var start = information.Start;
				var end   = information.End;

				if (start == null || end == null)
					throw new InvalidOperationException("Expected both start and end boundaries");

				// A RANGE/GROUPS frame with a value offset is defined relative to the single ORDER BY key's value,
				// so the SQL standard requires exactly one ORDER BY expression. Fail at translate time rather than
				// sending SQL the database will reject.
				if (frameType is SqlFrameClause.FrameTypeKind.Range or SqlFrameClause.FrameTypeKind.Groups
					&& (start.BoundaryType == SqlFrameBoundary.FrameBoundaryType.Offset || end.BoundaryType == SqlFrameBoundary.FrameBoundaryType.Offset)
					&& (orderItems == null || orderItems.Count != 1))
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FrameRangeGroupsOrderBy, methodCall.Type);

				ISqlExpression? startOffset = null;
				ISqlExpression? endOffset   = null;

				if (start.Offset != null)
				{
					var translated = translationContext.Translate(start.Offset);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					startOffset = placeholder.Sql;
				}

				if (end.Offset != null)
				{
					var translated = translationContext.Translate(end.Offset);
					if (translated is not SqlPlaceholderExpression placeholder)
						return SqlErrorExpression.EnsureError(translated, methodCall.Type);
					endOffset = placeholder.Sql;
				}

				var startBoundary = new SqlFrameBoundary(start.IsPreceding, start.BoundaryType, startOffset);
				var endBoundary   = new SqlFrameBoundary(end.IsPreceding, end.BoundaryType, endOffset);
				frame = new SqlFrameClause(frameType, startBoundary, endBoundary, information.FrameExclusion);
			}

			SqlKeepClause? keepClause = null;

			if (information.KeepType != null)
			{
				if (!IsKeepSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_Keep, methodCall.Type);

				if (information.KeepOrderBy != null)
				{
					var keepOrderItems = new List<SqlWindowOrderItem>();
					if (!TranslateOrderItems(translationContext, translationContext.ProviderFlags.DefaultNullsOrdering, methodCall.Type, information.KeepOrderBy, keepOrderItems, out var keepOrderError))
						return keepOrderError;

					keepClause = new SqlKeepClause(information.KeepType.Value, keepOrderItems);
				}
			}

			if (information.NullTreatment == Sql.Nulls.Ignore)
			{
				var nullTreatmentSupported = functionName is "LEAD" or "LAG"
					? IsLeadLagNullTreatmentSupported
					: IsValueNullTreatmentSupported;

				if (!nullTreatmentSupported)
					return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NullTreatment, methodCall.Type);
			}

			if (information.FromPosition == Sql.From.Last && !IsNthValueFromSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NthValueFrom, methodCall.Type);

			if (!IsAggregateDistinctSupported && information.Arguments != null
				&& Array.Exists(information.Arguments, a => a.Modifier == Sql.AggregateModifier.Distinct))
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_AggregateDistinct, methodCall.Type);

			var function = translationContext.ExpressionFactory.Function(dbDataType, functionName,
				arguments.ToArray(),
				arguments.Select(a => true).ToArray(),
				partitionBy     : partitionBy,
				orderBy         : orderItems,
				filter          : filter,
				frameClause     : frame,
				keepClause      : keepClause,
				nullTreatment   : information.NullTreatment,
				fromPosition    : information.FromPosition,
				isWindowFunction: true
			);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, function, methodCall);
		}

		static LambdaExpression SimplifyEntityLambda(LambdaExpression lambda, int parameterIndex, Expression contextExpression)
		{
			var paramToReplace = lambda.Parameters[parameterIndex];
			var newBody = lambda.Body.Transform(e =>
			{
				if (e == paramToReplace)
				{
					if (contextExpression is ContextRefExpression contextRefExpression)
					{
						var contextTyped = contextRefExpression.WithType(e.Type);
						return contextTyped;
					}
				}

				return e;
			});

			var newParameters = lambda.Parameters.ToList();
			newParameters.RemoveAt(parameterIndex);

			return Expression.Lambda(newBody, newParameters);
		}

		public virtual Expression? TranslateRowNumber(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "ROW_NUMBER");
		}

		public virtual Expression? TranslateRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "RANK");
		}

		public virtual Expression? TranslateDenseRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "DENSE_RANK");
		}

		public virtual Expression? TranslatePercentRank(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsPercentRankSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_PercentRank, methodCall.Type);

			var factory    = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "PERCENT_RANK");
		}

		public virtual Expression? TranslateCumeDist(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsCumeDistSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_CumeDist, methodCall.Type);

			var factory    = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "CUME_DIST");
		}

		public virtual Expression? TranslateNTile(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsNTileSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NTile, methodCall.Type);

			var factory    = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "NTILE");
		}

		public virtual Expression? TranslatePercentileCont(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsPercentileContSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_PercentileCont, methodCall.Type);

			return TranslatePercentileFunction(translationContext, methodCall, "PERCENTILE_CONT", requireSingleOrderBy: true);
		}

		public virtual Expression? TranslatePercentileDisc(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsPercentileDiscSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_PercentileDisc, methodCall.Type);

			return TranslatePercentileFunction(translationContext, methodCall, "PERCENTILE_DISC", requireSingleOrderBy: false);
		}

		Expression? TranslatePercentileFunction(ITranslationContext translationContext, MethodCallExpression methodCall, string functionName, bool requireSingleOrderBy)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);

			var result = new AggregateFunctionBuilder()
				.ConfigureAggregate(c => c
					.HasSequenceIndex(0)
					.HasValue(false)
					.OnBuildFunction(composer =>
					{
						var argumentExpr = methodCall.Arguments[1];
						if (!composer.AggregationContext.TranslateExpression(argumentExpr, out var argumentSql, out var argError))
						{
							composer.SetError(argError);
							return;
						}

						var builderLambda = methodCall.Arguments[2].UnwrapLambda();
						builderLambda = composer.AggregationContext.SimplifyEntityLambda(builderLambda, 0);

						if (!CollectWindowFunctionInformation(
							    translationContext,
							    methodCall.Type,
							    null,
							    builderLambda.Body,
							    out var wfInfo,
							    out var error))
						{
							composer.SetError(error);
							return;
						}

						if (wfInfo.OrderBy == null || (requireSingleOrderBy && wfInfo.OrderBy.Length != 1))
						{
							composer.SetError(translationContext.CreateErrorExpression(
								methodCall.Arguments[2],
								requireSingleOrderBy ? "Expected single order by expression" : "Expected order by expression",
								methodCall.Type));
							return;
						}

						List<SqlWindowOrderItem> withinGroupOrder = new();
						if (!TranslateOrderItems(composer.AggregationContext, translationContext.ProviderFlags.DefaultNullsOrdering, methodCall.Type, wfInfo.OrderBy, withinGroupOrder, out var orderError))
						{
							composer.SetError(orderError);
							return;
						}

						List<ISqlExpression>? partitionBy = null;
						if (wfInfo.PartitionBy != null)
						{
							partitionBy = new();
							if (!TranslatePartitionBy(composer.AggregationContext, methodCall.Type, wfInfo.PartitionBy, partitionBy, out var partitionError))
							{
								composer.SetError(partitionError);
								return;
							}
						}

						SqlSearchCondition? filter = null;
						if (wfInfo.Filter != null)
						{
							// FILTER (WHERE ...) on an ordered-set aggregate filters the input set, so it cannot be
							// CASE-WHEN-emulated like the OVER-clause FILTER — gate it on a dedicated capability.
							if (!IsOrderedSetFilterSupported)
							{
								composer.SetError(translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_OrderedSetFilter, methodCall.Type));
								return;
							}

							if (!composer.AggregationContext.TranslateExpression(wfInfo.Filter, out var filterSql, out var filterError))
							{
								composer.SetError(filterError);
								return;
							}

							if (filterSql is not SqlSearchCondition filterSc)
							{
								composer.SetError(translationContext.CreateErrorExpression(methodCall.Arguments[2], "Expected a boolean FILTER predicate", methodCall.Type));
								return;
							}

							filter = filterSc;
						}

						var functionType = translationContext.GetDbDataType(withinGroupOrder[0].Expression);

						var windowFunction = translationContext.ExpressionFactory.Function(
							functionType,
							functionName,
							[new SqlFunctionArgument(argumentSql, Sql.AggregateModifier.None)],
							[true],
							withinGroup : withinGroupOrder,
							partitionBy : partitionBy,
							filter      : filter,
							isAggregate : true
						);

						composer.SetResult(windowFunction);
					}))
				.Build(translationContext, methodCall);

			if (result == null)
				return translationContext.CreateErrorExpression(methodCall, $"Failed to build aggregation function for {functionName}.", methodCall.Type);

			return result;
		}

		public virtual Expression? TranslateSum(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "SUM");
		}

		public virtual Expression? TranslateAverage(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "AVG");
		}

		public virtual Expression? TranslateMin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "MIN");
		}

		public virtual Expression? TranslateMax(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "MAX");
		}

		// Sample standard deviation. STDDEV on Oracle/DuckDB/PostgreSQL; STDEV on SQL Server / Sybase. Override per provider.
		protected virtual string StdDevFunctionName => "STDDEV";

		public virtual Expression? TranslateStdDev(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, StdDevFunctionName);
		}

		public virtual Expression? TranslateStdDevPop(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "STDDEV_POP");
		}

		public virtual Expression? TranslateStdDevSamp(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "STDDEV_SAMP");
		}

		public virtual Expression? TranslateVariance(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "VARIANCE");
		}

		public virtual Expression? TranslateVarPop(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "VAR_POP");
		}

		public virtual Expression? TranslateVarSamp(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "VAR_SAMP");
		}

		// Two-value-argument statistical aggregates (COVAR_POP(x, y) etc.). The window-builder lambda is the 4th
		// argument (this, expr1, expr2, func), and TranslateWindowFunctionCore already loops over the argument array.
		public virtual Expression? TranslateCovarPop(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "COVAR_POP");

		public virtual Expression? TranslateCovarSamp(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "COVAR_SAMP");

		public virtual Expression? TranslateCorr(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "CORR");

		public virtual Expression? TranslateRegrSlope(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_SLOPE");

		public virtual Expression? TranslateRegrIntercept(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_INTERCEPT");

		public virtual Expression? TranslateRegrCount(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_COUNT");

		public virtual Expression? TranslateRegrR2(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_R2");

		public virtual Expression? TranslateRegrAvgX(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_AVGX");

		public virtual Expression? TranslateRegrAvgY(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_AVGY");

		public virtual Expression? TranslateRegrSXX(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_SXX");

		public virtual Expression? TranslateRegrSYY(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_SYY");

		public virtual Expression? TranslateRegrSXY(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			=> TranslateBivariate(translationContext, methodCall, "REGR_SXY");

		// Shared dispatch for the two-value-argument statistical aggregates: the window-builder lambda is the 4th
		// argument (this, expr1, expr2, func); TranslateWindowFunctionCore emits both arguments.
		Expression? TranslateBivariate(ITranslationContext translationContext, MethodCallExpression methodCall, string functionName)
		{
			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunctionCore(translationContext, methodCall, [methodCall.Arguments[1], methodCall.Arguments[2]], 3, dbDataType, functionName, null);
		}

		public virtual Expression? TranslateCount(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var factory    = translationContext.ExpressionFactory;
			var dbDataType = factory.GetDbDataType(methodCall.Type);

			// Count(expr, func) — args [window, expr, func]: argument=1, window=2 → COUNT(expr) / COUNT(DISTINCT expr).
			if (methodCall.Arguments.Count == 3)
				return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "COUNT");

			// Count(func) — args [window, func] → COUNT(*).
			return TranslateWindowFunction(translationContext, methodCall, null, 1, dbDataType, "COUNT",
				(arguments, hasFilter) =>
				{
					// COUNT(*):
					// - with native FILTER or no filter: COUNT(*)
					// - with emulated FILTER: COUNT(CASE WHEN cond THEN 1 ELSE NULL END)
					if (arguments.Count == 0)
					{
						if (hasFilter && !IsWindowFilterSupported)
							arguments.Add(new SqlFunctionArgument(factory.Value(dbDataType, 1), Sql.AggregateModifier.None));
						else
							arguments.Add(new SqlFunctionArgument(factory.Fragment("*"), Sql.AggregateModifier.None));
					}
				});
		}

		public virtual Expression? TranslateLead(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsLeadLagSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_LeadLag, methodCall.Type);

			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			// Lead(expr, func) — 2 args: expr=1, window=last
			// Lead(expr, offset, func) — 3 args: expr=1, offset=2, window=last
			// Lead(expr, offset, default, func) — 4 args: expr=1, offset=2, default=3, window=last
			var argCount     = methodCall.Arguments.Count;
			var windowArgIdx = argCount - 1;

			return argCount switch
			{
				3 => TranslateWindowFunction(translationContext, methodCall, 1, windowArgIdx, dbDataType, "LEAD"),
				4 => TranslateWindowFunctionMultiArg(translationContext, methodCall, [1, 2], windowArgIdx, dbDataType, "LEAD"),
				5 => TranslateWindowFunctionMultiArg(translationContext, methodCall, [1, 2, 3], windowArgIdx, dbDataType, "LEAD"),
				_ => translationContext.CreateErrorExpression(methodCall, "Unexpected argument count for LEAD", methodCall.Type),
			};
		}

		public virtual Expression? TranslateLag(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsLeadLagSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_LeadLag, methodCall.Type);

			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			var argCount     = methodCall.Arguments.Count;
			var windowArgIdx = argCount - 1;

			return argCount switch
			{
				3 => TranslateWindowFunction(translationContext, methodCall, 1, windowArgIdx, dbDataType, "LAG"),
				4 => TranslateWindowFunctionMultiArg(translationContext, methodCall, [1, 2], windowArgIdx, dbDataType, "LAG"),
				5 => TranslateWindowFunctionMultiArg(translationContext, methodCall, [1, 2, 3], windowArgIdx, dbDataType, "LAG"),
				_ => translationContext.CreateErrorExpression(methodCall, "Unexpected argument count for LAG", methodCall.Type),
			};
		}

		public virtual Expression? TranslateFirstValue(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsFirstLastValueSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FirstLastValue, methodCall.Type);

			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "FIRST_VALUE");
		}

		public virtual Expression? TranslateLastValue(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsFirstLastValueSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_FirstLastValue, methodCall.Type);

			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunction(translationContext, methodCall, 1, 2, dbDataType, "LAST_VALUE");
		}

		public virtual Expression? TranslateNthValue(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!IsWindowFunctionsSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NotSupported, methodCall.Type);
			if (!IsNthValueSupported)
				return translationContext.CreateErrorExpression(methodCall, ErrorHelper.Error_WindowFunction_NthValue, methodCall.Type);

			var dbDataType = translationContext.ExpressionFactory.GetDbDataType(methodCall.Type);

			return TranslateWindowFunctionMultiArg(translationContext, methodCall, [1, 2], 3, dbDataType, "NTH_VALUE");
		}

		protected Expression TranslateWindowFunctionMultiArg(
			ITranslationContext  translationContext,
			MethodCallExpression methodCall,
			int[]                argumentIndexes,
			int                  windowArgument,
			DbDataType           dbDataType,
			string               functionName)
		{
			var functionArgs = new Expression[argumentIndexes.Length];
			for (var i = 0; i < argumentIndexes.Length; i++)
				functionArgs[i] = methodCall.Arguments[argumentIndexes[i]];

			return TranslateWindowFunctionCore(translationContext, methodCall, functionArgs, windowArgument, dbDataType, functionName, null);
		}
	}
}
