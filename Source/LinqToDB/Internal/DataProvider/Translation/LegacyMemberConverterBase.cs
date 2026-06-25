using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class LegacyMemberConverterBase : IMemberConverter
	{
		public record OrderByInformation(Expression Expr, bool IsDescending, Sql.NullsPosition Nulls);

		static readonly MethodInfo _toValueMethodInfo           = MemberHelper.MethodOfGeneric<Sql.IAggregateFunction<string, string>>((f) => f.ToValue());

		// The analytic chain's ToValue() is a non-generic method declared on the generic type
		// AnalyticFunctions.IReadyToFunction<TR>, so IsSameGenericMethod (which compares closed
		// MethodInfos for non-generic methods) can't match it across TR instantiations. Match
		// structurally on the declaring type's generic definition instead.
		static bool IsAnalyticToValue(MethodCallExpression mc)
			=> string.Equals(mc.Method.Name, nameof(AnalyticFunctions.IReadyToFunction<>.ToValue), StringComparison.Ordinal)
				&& mc.Method.DeclaringType is { IsGenericType: true } dt
				&& dt.GetGenericTypeDefinition() == typeof(AnalyticFunctions.IReadyToFunction<>);

		static readonly MethodInfo _stringAggregateMethodInfoE  = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => e.StringAggregate(" "));
		static readonly MethodInfo _stringAggregateMethodInfoES = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => e.StringAggregate(" ", x => x));
		static readonly MethodInfo _stringAggregateMethodInfoQ  = MemberHelper.MethodOfGeneric<IQueryable<string>>(e => e.StringAggregate(" "));
		static readonly MethodInfo _stringAggregateMethodInfoQS = MemberHelper.MethodOfGeneric<IQueryable<string>>(e => e.StringAggregate(" ", x => x));
		static readonly MethodInfo _concatStringMethodInfo      = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => Sql.ConcatStringsNullable(" ", e));

#pragma warning disable CS0618 // Expressions.TrimLeft/TrimRight are obsolete-on-purpose
		static readonly MethodInfo _expressionsTrimLeftMethodInfo  = MemberHelper.MethodOf(() => LinqToDB.Linq.Expressions.TrimLeft (null, null!));
		static readonly MethodInfo _expressionsTrimRightMethodInfo = MemberHelper.MethodOf(() => LinqToDB.Linq.Expressions.TrimRight(null, null!));
#pragma warning restore CS0618
		static readonly MethodInfo _stringTrimStartCharArrayMethodInfo = MemberHelper.MethodOf<string>(s => s.TrimStart((char[])null!));
		static readonly MethodInfo _stringTrimEndCharArrayMethodInfo   = MemberHelper.MethodOf<string>(s => s.TrimEnd  ((char[])null!));

		public Expression Convert(Expression expression, IConvertContext context, out bool handled)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;

				// Expressions.TrimLeft (s, chars) -> s != null ? s.TrimStart(chars) : null
				// Expressions.TrimRight(s, chars) -> s != null ? s.TrimEnd  (chars) : null
				// The obsolete static helpers had `return str?.TrimStart(trimChars);` bodies —
				// preserve that null-propagation in the rewrite so client-side fallback
				// (when the translator returns null) doesn't NRE on null source values.
				if (mc.Method == _expressionsTrimLeftMethodInfo)
				{
					handled = true;
					return MakeNullSafeStringTrimCall(mc, _stringTrimStartCharArrayMethodInfo);
				}

				if (mc.Method == _expressionsTrimRightMethodInfo)
				{
					handled = true;
					return MakeNullSafeStringTrimCall(mc, _stringTrimEndCharArrayMethodInfo);
				}

				if (mc.IsSameGenericMethod(_toValueMethodInfo) || IsAnalyticToValue(mc))
				{
					var result = TryConvertAnalyticFunction(mc, context) ?? TryConvertStringAggregate(mc);
					if (result != null)
					{
						handled = true;
						return result;
					}
				}
			}

			handled = false;
			return expression;
		}

		Expression? TryConvertStringAggregate(MethodCallExpression toValueCall)
		{
			var chain = new List<MethodCallExpression>();
			if (!BuildFunctionsChain(toValueCall, chain, out var foundMethod, _stringAggregateMethodInfoE, _stringAggregateMethodInfoQ, _stringAggregateMethodInfoES, _stringAggregateMethodInfoQS))
				return null;

			var sequence  = foundMethod.Arguments[0];
			var separator = foundMethod.Arguments[1];

			sequence = BuildExpressionUtils.UnwrapEnumerableCasting(sequence);
			sequence = BuildExpressionUtils.EnsureEnumerableType(sequence);

			CollectOrderBy(chain, out var orderBy);
			if (orderBy.Length > 0)
			{
				sequence = ApplyOrderBy(sequence, orderBy);
			}

			if (foundMethod.Arguments.Count > 2)
			{
				var selector = foundMethod.Arguments[2].UnwrapLambda();
				sequence = Expression.Call(
					typeof(Enumerable),
					nameof(Enumerable.Select),
					new[] { selector.Parameters[0].Type, typeof(string) },
					sequence,
					selector);
			}

			var startSequence = sequence;
			while (
				startSequence is MethodCallExpression
				{
					IsQueryable: true,
					Method.Name:
						nameof(Queryable.Select)
						or nameof(Queryable.Distinct)
						or nameof(Queryable.Where)
						or nameof(Queryable.OrderBy)
						or nameof(Queryable.OrderByDescending)
						or nameof(Queryable.ThenBy)
						or nameof(Queryable.ThenByDescending)
						or nameof(Queryable.AsQueryable)
						or nameof(Enumerable.AsEnumerable),
					Arguments: [var a0, ..],
				}
			)
			{
				startSequence = a0;
			}

			if (startSequence.UnwrapConvert() is ParameterExpression)
			{
				// short path
				return Expression.Call(_concatStringMethodInfo, separator, sequence);
			}

			var elementType      = TypeHelper.GetEnumerableElementType(startSequence.Type);
			var parameter        = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(elementType), "x");
			var functionCallBody = (Expression)parameter;
			if (functionCallBody.Type != startSequence.Type)
			{
				functionCallBody = Expression.Convert(functionCallBody, startSequence.Type);
			}

			functionCallBody = sequence.Replace(startSequence, functionCallBody);

			var concatExpression = Expression.Call(_concatStringMethodInfo, separator, functionCallBody);

			startSequence = BuildExpressionUtils.UnwrapEnumerableCasting(startSequence);
			if (!typeof(IQueryable<>).IsSameOrParentOf(startSequence.Type))
			{
				startSequence = Expression.Call(
					Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
					startSequence);
			}

			var queryableType = typeof(IQueryable<>).MakeGenericType(elementType);
			if (startSequence.Type != queryableType)
			{
				startSequence = Expression.Convert(startSequence, queryableType);
			}

			var aggregateExecuteMethod = Methods.LinqToDB.AggregateExecute.MakeGenericMethod(elementType, toValueCall.Type);
			return Expression.Call(
				aggregateExecuteMethod,
				startSequence,
				Expression.Lambda(concatExpression, parameter));
		}

		void CollectOrderBy(List<MethodCallExpression> chain, out OrderByInformation[] orderBy)
		{
			List<OrderByInformation>? orderByList = null;
			foreach (var methodCall in chain)
			{
				var methodName = methodCall.Method.Name;

				var isThenBy = methodName is nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending);

				if (isThenBy || methodName is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending))
				{
					var isDescending = methodName.EndsWith("Descending", StringComparison.Ordinal);

					LambdaExpression lambda;
					if (methodCall.Arguments.Count > 1)
					{
						lambda = methodCall.Arguments[1].UnwrapLambda();
					}
					else
					{
						var elementType = methodCall.Method.GetGenericArguments()[^1];
						var parameter   = Expression.Parameter(elementType, "x");
						lambda = Expression.Lambda(parameter, parameter);
					}

					var nulls        = Sql.NullsPosition.None;

					orderByList ??= new List<OrderByInformation>();
					orderByList.Add(new OrderByInformation(lambda, isDescending, nulls));

					if (!isThenBy)
						break;
				}
				else
				{
					break;
				}
			}

			if (orderByList == null)
			{
				orderBy = [];
				return;
			}

			orderByList.Reverse();
			orderBy = orderByList.ToArray();
		}

		public static Expression ApplyOrderBy(Expression queryExpr, OrderByInformation[] order)
		{
			// Delegate to the canonical implementation: it Quotes the key selector for IQueryable
			// sources and passes a bare Func lambda for IEnumerable ones. This string-aggregate path
			// always operates on an IEnumerable-shaped sequence (see EnsureEnumerableType), so it takes
			// the Enumerable branch — the shape AggregateExecute translates to SQL.
			queryExpr = BuildExpressionUtils.EnsureEnumerableType(queryExpr);
			return WindowFunctionHelpers.ApplyOrderBy(queryExpr, order.Select(o => ((LambdaExpression)o.Expr, o.IsDescending, o.Nulls)));
		}

		/// <summary>
		/// Tries to convert old Sql.Ext.*().Over().PartitionBy().OrderBy().ToValue() chains
		/// to the new Sql.Window.* API.
		/// Returns null if the chain is not an analytic function chain.
		/// </summary>
		Expression? TryConvertAnalyticFunction(MethodCallExpression toValueCall, IConvertContext context)
		{
			// Walk the chain from .ToValue() backwards, collecting window clauses.
			// If we never find a root function on AnalyticFunctions, return null.
			var partitionByList = new List<Expression>();
			var orderByList     = new List<(Expression expr, bool descending, Sql.NullsPosition nulls)>();

			string?     functionName     = null;
			Expression? functionArg1     = null;
			Expression? functionArg2     = null;
			Expression? functionArg3     = null;
			int         functionArgCount = 0;
			bool        isKeepFirst      = false;
			bool        sawOver          = false;
			Sql.Nulls   functionNulls    = Sql.Nulls.None;
			Sql.AggregateModifier functionModifier = Sql.AggregateModifier.None;

			List<(Expression expr, bool descending, Sql.NullsPosition nulls)>? keepOrderByList = null;

			var current = toValueCall.Object ?? (toValueCall.Arguments.Count > 0 ? toValueCall.Arguments[0] : null);

			// --- Window frame pre-pass ---
			// A legacy ROWS/RANGE frame, when present, sits at the front of the chain (between .ToValue() and the
			// OrderBy/function). It is built from property getters (Rows/Range/Between/UnboundedPreceding/CurrentRow/
			// And/UnboundedFollowing) plus the ValuePreceding/ValueFollowing methods. Consume it here into a
			// WindowFrameSpec, then let the loop below walk the OrderBy/PartitionBy/Over/function chain unchanged. The
			// new builder only has the BETWEEN form, so a single-boundary legacy frame (e.g. ROWS UNBOUNDED PRECEDING)
			// normalises to "BETWEEN <boundary> AND CURRENT ROW" — the equivalent SQL.
			var         frameSeen        = false;
			var         frameIsRange     = false;
			string?     frameStartMember = null;
			Expression? frameStartValue  = null;
			string?     frameEndMember   = null;
			Expression? frameEndValue    = null;

			static Type? FrameInterface(Type? declaringType)
			{
				var open = declaringType is { IsGenericType: true } ? declaringType.GetGenericTypeDefinition() : declaringType;
				return open == typeof(AnalyticFunctions.IOrderedReadyToWindowing<>)
					|| open == typeof(AnalyticFunctions.IBoundaryExpected<>)
					|| open == typeof(AnalyticFunctions.IBetweenStartExpected<>)
					|| open == typeof(AnalyticFunctions.IAndExpected<>)
					|| open == typeof(AnalyticFunctions.ISecondBoundaryExpected<>)
						? open
						: null;
			}

			// Maps a legacy boundary member name to the new IBoundaryPart<> member name (UNBOUNDED PRECEDING/FOLLOWING
			// both collapse to the positional Unbounded; the *Value* members carry the offset expression).
			static (string member, Expression? value) MapLegacyBoundary(string legacyName, Expression? value) => legacyName switch
			{
				nameof(AnalyticFunctions.IBoundaryExpected<>.UnboundedPreceding)       => ("Unbounded",      null),
				nameof(AnalyticFunctions.ISecondBoundaryExpected<>.UnboundedFollowing) => ("Unbounded",      null),
				nameof(AnalyticFunctions.IBoundaryExpected<>.CurrentRow)               => ("CurrentRow",     null),
				nameof(AnalyticFunctions.IBoundaryExpected<>.ValuePreceding)           => ("ValuePreceding", value),
				nameof(AnalyticFunctions.ISecondBoundaryExpected<>.ValueFollowing)     => ("ValueFollowing", value),
				_                                                                         => throw new InvalidOperationException($"Unexpected window frame boundary '{legacyName}'."),
			};

			while (current != null)
			{
				Type?       frameDecl = null;
				string      memberName;
				Expression? boundaryValue = null;
				Expression? receiver;

				if (current is MemberExpression frameMember && (frameDecl = FrameInterface(frameMember.Member.DeclaringType)) != null)
				{
					memberName = frameMember.Member.Name;
					receiver   = frameMember.Expression;
				}
				else if (current is MethodCallExpression frameMethod
					&& frameMethod.Method.Name is nameof(AnalyticFunctions.IBoundaryExpected<>.ValuePreceding) or nameof(AnalyticFunctions.ISecondBoundaryExpected<>.ValueFollowing)
					&& (frameDecl = FrameInterface(frameMethod.Method.DeclaringType)) != null)
				{
					memberName    = frameMethod.Method.Name;
					boundaryValue = frameMethod.Arguments.Count > 0 ? frameMethod.Arguments[^1] : null;
					receiver      = frameMethod.Object;
				}
				else
					break;

				if (frameDecl == typeof(AnalyticFunctions.IOrderedReadyToWindowing<>))
				{
					if (memberName is not (nameof(AnalyticFunctions.IOrderedReadyToWindowing<>.Rows) or nameof(AnalyticFunctions.IOrderedReadyToWindowing<>.Range)))
						break; // ThenBy etc. — not a frame node; hand back to the main loop

					frameIsRange = string.Equals(memberName, nameof(AnalyticFunctions.IOrderedReadyToWindowing<>.Range), StringComparison.Ordinal);
				}
				else if (frameDecl == typeof(AnalyticFunctions.ISecondBoundaryExpected<>))
					(frameEndMember, frameEndValue) = MapLegacyBoundary(memberName, boundaryValue);
				else if (frameDecl == typeof(AnalyticFunctions.IBetweenStartExpected<>))
					(frameStartMember, frameStartValue) = MapLegacyBoundary(memberName, boundaryValue);
				else if (frameDecl == typeof(AnalyticFunctions.IBoundaryExpected<>) && !string.Equals(memberName, nameof(AnalyticFunctions.IBoundaryExpected<>.Between), StringComparison.Ordinal))
					(frameStartMember, frameStartValue) = MapLegacyBoundary(memberName, boundaryValue); // single-boundary form -> start
				// IAndExpected.And and IBoundaryExpected.Between are markers — nothing to capture.

				frameSeen = true;
				current   = receiver;
			}

			while (current is MethodCallExpression mc)
			{
				var declaringType = mc.Method.DeclaringType;
				var methodName    = mc.Method.Name;

				// Check if this is the root analytic function call (on AnalyticFunctions class)
				if (declaringType == typeof(AnalyticFunctions))
				{
					// KeepFirst/KeepLast — not the root function, continue to find actual aggregate
					if (methodName is "KeepFirst" or "KeepLast")
					{
						isKeepFirst = string.Equals(methodName, "KeepFirst", StringComparison.Ordinal);
						// OrderBy collected so far belongs to KEEP, not the window
						keepOrderByList = orderByList;
						orderByList     = new();
						current = mc.Object ?? (mc.Arguments.Count > 0 ? mc.Arguments[0] : null);
						continue;
					}

					functionName = methodName;

					// Extract function arguments (skip the first 'this Sql.ISqlExtension?' param)
					var parameters = mc.Method.GetParameters();
					functionArgCount = 0;
					for (var i = 0; i < parameters.Length; i++)
					{
						var pType = parameters[i].ParameterType;
						if (typeof(Sql.ISqlExtension).IsAssignableFrom(pType))
							continue;
						// Capture the value/offset functions' NULL-treatment modifier (FirstValue/LastValue/Lead/Lag/NthValue)
						// so it can be re-applied as .IgnoreNulls() on the converted call; it is not a positional argument.
						if (pType == typeof(Sql.Nulls))
						{
							functionNulls = (Sql.Nulls)mc.Arguments[i].EvaluateExpression()!;
							continue;
						}

						// Capture the aggregate modifier. DISTINCT is reproduced via the new builder's .Distinct();
						// ALL is the SQL default and is dropped (MAX(ALL x) == MAX(x)).
						if (pType == typeof(Sql.AggregateModifier))
						{
							functionModifier = (Sql.AggregateModifier)mc.Arguments[i].EvaluateExpression()!;
							continue;
						}

						if (pType == typeof(Sql.NullsPosition) || pType == typeof(Sql.From))
							continue;

						var capturedArg = mc.Arguments[i];

						// Legacy object?-typed parameters (Average/StdDev/Covar/Regr/...) box the real expression as
						// Convert(x, object). Unwrap that boxing so the converted call matches a typed/generic Sql.Window
						// overload (e.g. Average(int?)) instead of seeing `object` (which has no concrete overload).
						while (capturedArg is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conv && conv.Type == typeof(object))
							capturedArg = conv.Operand;

						switch (functionArgCount)
						{
							case 0: functionArg1 = capturedArg; break;
							case 1: functionArg2 = capturedArg; break;
							case 2: functionArg3 = capturedArg; break;
						}

						functionArgCount++;
					}

					break;
				}

				// Only process methods from AnalyticFunctions interface hierarchy
				if (declaringType?.DeclaringType != typeof(AnalyticFunctions) && declaringType != typeof(AnalyticFunctions))
				{
					// Not part of analytic function chain — abort
					return null;
				}

				switch (methodName)
				{
					case "Over":
						sawOver = true;
						current = mc.Object ?? mc.Arguments[0];
						continue;

					case "PartitionBy":
					{
						if (mc.Arguments.Count > 0)
						{
							var lastArg = mc.Arguments[^1];
							if (lastArg is NewArrayExpression newArray)
							{
								partitionByList.AddRange(newArray.Expressions);
							}
							else
							{
								for (var i = mc.Method.IsStatic ? 1 : 0; i < mc.Arguments.Count; i++)
								{
									var arg = mc.Arguments[i];
									if (typeof(Sql.ISqlExtension).IsAssignableFrom(arg.Type))
										continue;
									partitionByList.Add(arg);
								}
							}
						}

						current = mc.Object ?? mc.Arguments[0];
						continue;
					}

					case "OrderBy":
					case "OrderByDesc":
					case "ThenBy":
					case "ThenByDesc":
					{
						var isDesc  = methodName.Contains("Desc", StringComparison.Ordinal);
						var argIdx  = mc.Method.IsStatic ? 1 : 0;
						if (argIdx < mc.Arguments.Count)
						{
							// Legacy analytic ORDER BY doesn't specify a NULLS position; resolve the configured default
							// so it matches a plain query OrderBy (explicit NULLS-positioning chain methods bail to
							// the legacy pipeline via the default arm below).
							orderByList.Insert(0, (mc.Arguments[argIdx], isDesc, context.DataOptions.SqlOptions.DefaultNullsPosition));
						}

						current = mc.Object ?? (mc.Arguments.Count > 0 ? mc.Arguments[0] : null);
						continue;
					}

					default:
						// An unrecognized analytic chain method (ROWS/RANGE/GROUPS frame, NULLS positioning, etc.)
						// can't be represented by this conversion. Bail so the call falls back to the legacy Sql.Ext
						// pipeline (which renders those clauses) instead of silently dropping them.
						return null;
				}
			}

			if (functionName == null)
				return null;

			// Without an explicit .Over() the chain is a plain aggregate (e.g. Sql.Ext.Sum(x).ToValue() -> SUM(x)),
			// not a window function. Leave those on the legacy pipeline so they don't get an erroneous OVER () clause.
			if (!sawOver)
				return null;

			var partitionBy = partitionByList.ToArray();
			var orderBy     = orderByList.ToArray();

			// If KEEP was detected, use KEEP-aware builder
			if (keepOrderByList != null && functionArg1 != null)
			{
				var keepOrderBy = keepOrderByList.ToArray();
				// KEEP returns here, so run the same return-type reconciliation the main switch path uses below.
				return ReconcileConvertedType(WindowFunctionHelpers.BuildAggregateWithKeep(functionName, functionArg1, isKeepFirst, partitionBy, keepOrderBy), toValueCall.Type);
			}

			WindowFunctionHelpers.WindowFrameSpec? frame = null;
			if (frameSeen)
			{
				// A frame needs a start boundary, and the new pipeline frames only aggregate and value functions
				// (ranking / LEAD / LAG cannot carry a frame) — otherwise fall back to the legacy pipeline.
				if (frameStartMember == null
					|| functionName is not (
						nameof(AnalyticFunctions.Sum)        or nameof(AnalyticFunctions.Average)    or nameof(AnalyticFunctions.Min) or
						nameof(AnalyticFunctions.Max)        or nameof(AnalyticFunctions.Count)      or nameof(AnalyticFunctions.LongCount)  or
						nameof(AnalyticFunctions.FirstValue) or
						nameof(AnalyticFunctions.LastValue)  or nameof(AnalyticFunctions.NthValue)   or
						nameof(AnalyticFunctions.StdDev)     or nameof(AnalyticFunctions.StdDevPop)  or nameof(AnalyticFunctions.StdDevSamp) or
						nameof(AnalyticFunctions.Variance)   or nameof(AnalyticFunctions.VarPop)     or nameof(AnalyticFunctions.VarSamp)    or
						nameof(AnalyticFunctions.CovarPop)   or nameof(AnalyticFunctions.CovarSamp)  or nameof(AnalyticFunctions.Corr)       or
						nameof(AnalyticFunctions.RegrSlope)  or nameof(AnalyticFunctions.RegrIntercept) or nameof(AnalyticFunctions.RegrCount) or
						nameof(AnalyticFunctions.RegrR2)     or nameof(AnalyticFunctions.RegrAvgX)   or nameof(AnalyticFunctions.RegrAvgY)   or
						nameof(AnalyticFunctions.RegrSXX)    or nameof(AnalyticFunctions.RegrSYY)    or nameof(AnalyticFunctions.RegrSXY)))
				{
					return null;
				}

				frameEndMember ??= "CurrentRow"; // single-boundary legacy form -> BETWEEN <boundary> AND CURRENT ROW

				frame = new WindowFunctionHelpers.WindowFrameSpec(frameIsRange, frameStartMember, frameStartValue, frameEndMember, frameEndValue);
			}

			var converted = functionName switch
			{
				nameof(AnalyticFunctions.RowNumber)   => WindowFunctionHelpers.BuildRowNumber(partitionBy, orderBy),
				nameof(AnalyticFunctions.Rank)        => WindowFunctionHelpers.BuildRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.DenseRank)   => WindowFunctionHelpers.BuildDenseRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.PercentRank) => WindowFunctionHelpers.BuildPercentRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.CumeDist)    => WindowFunctionHelpers.BuildCumeDist(partitionBy, orderBy),
				nameof(AnalyticFunctions.NTile)       => functionArg1 != null ? WindowFunctionHelpers.BuildNTile(functionArg1, partitionBy, orderBy) : null,

				nameof(AnalyticFunctions.Sum)     when functionArg1 != null => WindowFunctionHelpers.BuildSum(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.Average) when functionArg1 != null => WindowFunctionHelpers.BuildAverage(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.Min)     when functionArg1 != null => WindowFunctionHelpers.BuildMin(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.Max)     when functionArg1 != null => WindowFunctionHelpers.BuildMax(functionArg1, partitionBy, orderBy, functionModifier, frame),

				nameof(AnalyticFunctions.StdDev)     when functionArg1 != null => WindowFunctionHelpers.BuildStdDev(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.StdDevPop)  when functionArg1 != null => WindowFunctionHelpers.BuildStdDevPop(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.StdDevSamp) when functionArg1 != null => WindowFunctionHelpers.BuildStdDevSamp(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.Variance)   when functionArg1 != null => WindowFunctionHelpers.BuildVariance(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.VarPop)     when functionArg1 != null => WindowFunctionHelpers.BuildVarPop(functionArg1, partitionBy, orderBy, functionModifier, frame),
				nameof(AnalyticFunctions.VarSamp)    when functionArg1 != null => WindowFunctionHelpers.BuildVarSamp(functionArg1, partitionBy, orderBy, functionModifier, frame),

				nameof(AnalyticFunctions.CovarPop)      when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildCovarPop(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.CovarSamp)     when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildCovarSamp(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.Corr)          when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildCorr(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrSlope)     when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrSlope(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrIntercept) when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrIntercept(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrCount)     when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrCount(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrR2)        when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrR2(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrAvgX)      when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrAvgX(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrAvgY)      when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrAvgY(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrSXX)       when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrSXX(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrSYY)       when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrSYY(functionArg1, functionArg2, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.RegrSXY)       when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildRegrSXY(functionArg1, functionArg2, partitionBy, orderBy, frame),

				nameof(AnalyticFunctions.Count) when functionArgCount == 0 => WindowFunctionHelpers.BuildCount(partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.Count) when functionArg1 != null  => WindowFunctionHelpers.BuildCount(functionArg1, partitionBy, orderBy, functionModifier, frame),

				nameof(AnalyticFunctions.LongCount) when functionArgCount == 0 => WindowFunctionHelpers.BuildLongCount(partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.LongCount) when functionArg1 != null  => WindowFunctionHelpers.BuildLongCount(functionArg1, partitionBy, orderBy, functionModifier, frame),

				nameof(AnalyticFunctions.Lead) when functionArg1 != null => WindowFunctionHelpers.BuildLead(functionArg1, functionArg2, functionArg3, functionNulls, partitionBy, orderBy),
				nameof(AnalyticFunctions.Lag)  when functionArg1 != null => WindowFunctionHelpers.BuildLag(functionArg1, functionArg2, functionArg3, functionNulls, partitionBy, orderBy),

				nameof(AnalyticFunctions.FirstValue) when functionArg1 != null                       => WindowFunctionHelpers.BuildFirstValue(functionArg1, functionNulls, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.LastValue)  when functionArg1 != null                       => WindowFunctionHelpers.BuildLastValue(functionArg1, functionNulls, partitionBy, orderBy, frame),
				nameof(AnalyticFunctions.NthValue)   when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildNthValue(functionArg1, functionArg2, functionNulls, partitionBy, orderBy, frame),

				nameof(AnalyticFunctions.RatioToReport) when functionArg1 != null => WindowFunctionHelpers.BuildRatioToReport(functionArg1, partitionBy, orderBy, frame),

				_ => null, // Unsupported function — fall through to old pipeline
			};

			// The Sql.Window.* functions have fixed CLR return types (e.g. RowNumber/Rank -> long, NTile -> int,
			// the statistical aggregates -> double?) that can differ from the legacy chain's ToValue() type
			// parameter (TR). Reconcile so the rewritten node slots into the original expression without a type clash.
			return ReconcileConvertedType(converted, toValueCall.Type);
		}

		// Reconciles a converted Sql.Window expression's CLR type to the legacy chain's ToValue() slot type.
		// Called from both the main dispatch and the KEEP path so neither slots a double? into a non-nullable slot.
		static Expression? ReconcileConvertedType(Expression? converted, Type targetType)
		{
			if (converted == null || converted.Type == targetType)
				return converted;

			var sourceUnderlying = Nullable.GetUnderlyingType(converted.Type);
			var targetUnderlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

			if (converted.Type == typeof(double?) && targetUnderlying == typeof(decimal))
			{
				// double? -> decimal / decimal?. A non-finite double (NaN/Infinity, e.g. CORR over a single-row
				// partition) has no decimal representation and a direct cast throws. Map non-finite values to NULL,
				// reproducing how the legacy provider column reader materialised a non-finite float into a decimal
				// slot; then narrow to the requested slot (coalescing to 0m only for the non-nullable decimal).
				var asDecimal = Expression.Call(FiniteOrNullMethod, converted);
				return targetType == typeof(decimal)
					? Expression.Coalesce(asDecimal, Expression.Default(typeof(decimal)))
					: asDecimal;
			}

			if (sourceUnderlying != null && targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
			{
				// Nullable<S> -> non-nullable value type T (e.g. the statistical aggregates return double? but the
				// legacy ToValue<T>() slot is double/int/float). A plain Convert unwraps via .Value and throws when the
				// DB returns NULL. Coalesce the NULL to default(S) first, then perform the numeric S -> T conversion.
				var nonNull = Expression.Coalesce(converted, Expression.Default(sourceUnderlying));
				return sourceUnderlying == targetType ? nonNull : Expression.Convert(nonNull, targetType);
			}

			return Expression.Convert(converted, targetType);
		}

		static readonly MethodInfo FiniteOrNullMethod = typeof(LegacyMemberConverterBase).GetMethod(nameof(FiniteOrNull), BindingFlags.Static | BindingFlags.NonPublic)!;

		// double/float NaN or Infinity has no decimal representation (a direct cast throws). Returns null for a
		// non-finite input so the converted analytic value materialises into a decimal slot the way the legacy
		// provider column reader did. Used by the return-type reconciliation in TryConvertAnalyticFunction.
		static decimal? FiniteOrNull(double? value)
			=> value is double v && !double.IsNaN(v) && !double.IsInfinity(v) ? (decimal)v : null;

		protected bool BuildFunctionsChain(Expression expr, List<MethodCallExpression> chain, [NotNullWhen(true)] out MethodCallExpression? foundMethod, params MethodInfo[] stopMethods)
		{
			Expression? current = expr;

			while (current != null)
			{
				Expression? next       = null;

				switch (current.NodeType)
				{
					case ExpressionType.Call:
					{
						var call = (MethodCallExpression) current;

						if (call.Method.IsStatic)
							next = call.Arguments.FirstOrDefault();
						else
							next = call.Object;

						if (stopMethods.Any(call.IsSameGenericMethod))
						{
							chain.RemoveAt(0);
							foundMethod = call;
							return true;
						}

						chain.Add(call);

						break;
					}

					case ExpressionType.Constant:
					{
						if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(current.Type))
						{
							next = current.EvaluateExpression<Sql.IQueryableContainer>()!.Query.Expression;
						}

						break;
					}
				}

				current = next;
			}

			foundMethod = null;
			return false;
		}

		static Expression MakeNullSafeStringTrimCall(MethodCallExpression methodCall, MethodInfo instanceTrimMethod)
		{
			var source = methodCall.Arguments[0];
			var chars  = methodCall.Arguments[1];

			// `s != null ? s.TrimStart/TrimEnd(chars) : null`
			return Expression.Condition(
				Expression.NotEqual(source, Expression.Constant(null, typeof(string))),
				Expression.Call(source, instanceTrimMethod, chars),
				Expression.Constant(null, typeof(string)));
		}
	}
}
