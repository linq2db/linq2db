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

			List<(Expression expr, bool descending, Sql.NullsPosition nulls)>? keepOrderByList = null;

			var current = toValueCall.Object ?? (toValueCall.Arguments.Count > 0 ? toValueCall.Arguments[0] : null);

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
						if (pType == typeof(Sql.AggregateModifier) || pType == typeof(Sql.NullsPosition) || pType == typeof(Sql.From))
							continue;

						switch (functionArgCount)
						{
							case 0: functionArg1 = mc.Arguments[i]; break;
							case 1: functionArg2 = mc.Arguments[i]; break;
							case 2: functionArg3 = mc.Arguments[i]; break;
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
				return functionName switch
				{
					nameof(AnalyticFunctions.Sum)     => WindowFunctionHelpers.BuildAggregateWithKeep(WindowFunctionHelpers.SumMethodInfo, functionArg1, isKeepFirst, partitionBy, keepOrderBy),
					nameof(AnalyticFunctions.Average) => WindowFunctionHelpers.BuildAggregateWithKeep(WindowFunctionHelpers.AvgMethodInfo, functionArg1, isKeepFirst, partitionBy, keepOrderBy),
					nameof(AnalyticFunctions.Min)     => WindowFunctionHelpers.BuildAggregateWithKeep(WindowFunctionHelpers.MinMethodInfo, functionArg1, isKeepFirst, partitionBy, keepOrderBy),
					nameof(AnalyticFunctions.Max)     => WindowFunctionHelpers.BuildAggregateWithKeep(WindowFunctionHelpers.MaxMethodInfo, functionArg1, isKeepFirst, partitionBy, keepOrderBy),
					_                                 => null,
				};
			}

			var converted = functionName switch
			{
				nameof(AnalyticFunctions.RowNumber)   => WindowFunctionHelpers.BuildRowNumber(partitionBy, orderBy),
				nameof(AnalyticFunctions.Rank)        => WindowFunctionHelpers.BuildRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.DenseRank)   => WindowFunctionHelpers.BuildDenseRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.PercentRank) => WindowFunctionHelpers.BuildPercentRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.CumeDist)    => WindowFunctionHelpers.BuildCumeDist(partitionBy, orderBy),
				nameof(AnalyticFunctions.NTile)       => functionArg1 != null ? WindowFunctionHelpers.BuildNTile(functionArg1, partitionBy, orderBy) : null,

				nameof(AnalyticFunctions.Sum)     when functionArg1 != null => WindowFunctionHelpers.BuildSum(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Average) when functionArg1 != null => WindowFunctionHelpers.BuildAverage(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Min)     when functionArg1 != null => WindowFunctionHelpers.BuildMin(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Max)     when functionArg1 != null => WindowFunctionHelpers.BuildMax(functionArg1, partitionBy, orderBy),

				nameof(AnalyticFunctions.Count) when functionArgCount == 0 => WindowFunctionHelpers.BuildCount(partitionBy, orderBy),
				nameof(AnalyticFunctions.Count) when functionArg1 != null  => WindowFunctionHelpers.BuildCount(functionArg1, partitionBy, orderBy),

				// LongCount intentionally falls through to the legacy pipeline: Sql.Window has no LongCount equivalent.

				nameof(AnalyticFunctions.Lead) when functionArg1 != null => WindowFunctionHelpers.BuildLead(functionArg1, functionArg2, functionArg3, functionNulls, partitionBy, orderBy),
				nameof(AnalyticFunctions.Lag)  when functionArg1 != null => WindowFunctionHelpers.BuildLag(functionArg1, functionArg2, functionArg3, functionNulls, partitionBy, orderBy),

				nameof(AnalyticFunctions.FirstValue) when functionArg1 != null                       => WindowFunctionHelpers.BuildFirstValue(functionArg1, functionNulls, partitionBy, orderBy),
				nameof(AnalyticFunctions.LastValue)  when functionArg1 != null                       => WindowFunctionHelpers.BuildLastValue(functionArg1, functionNulls, partitionBy, orderBy),
				nameof(AnalyticFunctions.NthValue)   when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildNthValue(functionArg1, functionArg2, functionNulls, partitionBy, orderBy),

				_ => null, // Unsupported function — fall through to old pipeline
			};

			// The Sql.Window.* functions have fixed CLR return types (e.g. RowNumber/Rank -> long, NTile -> int)
			// that can differ from the legacy chain's ToValue() type parameter (TR). Reconcile so the rewritten
			// node slots into the original expression (anonymous-type ctors, casts, etc.) without a type clash.
			if (converted != null && converted.Type != toValueCall.Type)
				converted = Expression.Convert(converted, toValueCall.Type);

			return converted;
		}

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
