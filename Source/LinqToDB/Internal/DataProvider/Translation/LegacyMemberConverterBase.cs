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
		static readonly MethodInfo _stringAggregateMethodInfoE  = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => e.StringAggregate(" "));
		static readonly MethodInfo _stringAggregateMethodInfoES = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => e.StringAggregate(" ", x => x));
		static readonly MethodInfo _stringAggregateMethodInfoQ  = MemberHelper.MethodOfGeneric<IQueryable<string>>(e => e.StringAggregate(" "));
		static readonly MethodInfo _stringAggregateMethodInfoQS = MemberHelper.MethodOfGeneric<IQueryable<string>>(e => e.StringAggregate(" ", x => x));
		static readonly MethodInfo _concatStringMethodInfo      = MemberHelper.MethodOfGeneric<IEnumerable<string>>(e => Sql.ConcatStringsNullable(" ", e));

		public Expression Convert(Expression expression, out bool handled)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;
				if (mc.IsSameGenericMethod(_toValueMethodInfo))
				{
					var result = TryConvertAnalyticFunction(mc) ?? TryConvertStringAggregate(mc);
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
			queryExpr = BuildExpressionUtils.EnsureEnumerableType(queryExpr);
			var entityType = TypeHelper.GetEnumerableElementType(queryExpr.Type);
			var isFirst = true;
			foreach (var tuple in order)
			{
				var lambda = (LambdaExpression)tuple.Expr;
				var methodName = (isFirst, tuple.IsDescending) switch
				{
					(true, true)   => nameof(Queryable.OrderByDescending),
					(true, false)  => nameof(Queryable.OrderBy),
					(false, true)  => nameof(Queryable.ThenByDescending),
					(false, false) => nameof(Queryable.ThenBy),
				};

				queryExpr = Expression.Call(typeof(Enumerable), methodName, [entityType, lambda.Body.Type], queryExpr, lambda);
				isFirst   = false;
			}

			return queryExpr;
		}

		/// <summary>
		/// Tries to convert old Sql.Ext.*().Over().PartitionBy().OrderBy().ToValue() chains
		/// to the new Sql.Window.* API.
		/// Returns null if the chain is not an analytic function chain.
		/// </summary>
		Expression? TryConvertAnalyticFunction(MethodCallExpression toValueCall)
		{
			// Walk the chain from .ToValue() backwards, collecting window clauses.
			// If we never find a root function on AnalyticFunctions, return null.
			var partitionByList = new List<Expression>();
			var orderByList     = new List<(Expression expr, bool descending)>();

			string?     functionName = null;
			Expression? functionArg1 = null;
			Expression? functionArg2 = null;
			Expression? functionArg3 = null;
			int         functionArgCount = 0;

			var current = toValueCall.Object ?? (toValueCall.Arguments.Count > 0 ? toValueCall.Arguments[0] : null);

			while (current is MethodCallExpression mc)
			{
				var declaringType = mc.Method.DeclaringType;
				var methodName    = mc.Method.Name;

				// Check if this is the root analytic function call (on AnalyticFunctions class)
				if (declaringType == typeof(AnalyticFunctions))
				{
					functionName = methodName;

					// Extract function arguments (skip the first 'this Sql.ISqlExtension?' param)
					var parameters = mc.Method.GetParameters();
					functionArgCount = 0;
					for (var i = 0; i < parameters.Length; i++)
					{
						var pType = parameters[i].ParameterType;
						if (pType == typeof(Sql.ISqlExtension) || pType.IsAssignableFrom(typeof(Sql.ISqlExtension)))
							continue;
						if (pType == typeof(Sql.AggregateModifier) || pType == typeof(Sql.Nulls) || pType == typeof(Sql.NullsPosition) || pType == typeof(Sql.From))
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
									if (arg.Type.IsAssignableFrom(typeof(Sql.ISqlExtension)))
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
							orderByList.Insert(0, (mc.Arguments[argIdx], isDesc));
						}

						current = mc.Object ?? (mc.Arguments.Count > 0 ? mc.Arguments[0] : null);
						continue;
					}

					default:
						// Frame spec or other chain method — skip for now
						current = mc.Object ?? (mc.Arguments.Count > 0 ? mc.Arguments[0] : null);
						continue;
				}
			}

			if (functionName == null)
				return null;

			var partitionBy = partitionByList.ToArray();
			var orderBy     = orderByList.ToArray();

			return functionName switch
			{
				nameof(AnalyticFunctions.RowNumber)   => WindowFunctionHelpers.BuildRowNumber(partitionBy, orderBy),
				nameof(AnalyticFunctions.Rank)        => WindowFunctionHelpers.BuildRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.DenseRank)   => WindowFunctionHelpers.BuildDenseRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.PercentRank) => WindowFunctionHelpers.BuildPercentRank(partitionBy, orderBy),
				nameof(AnalyticFunctions.CumeDist)    => WindowFunctionHelpers.BuildCumeDist(partitionBy, orderBy),
				nameof(AnalyticFunctions.NTile)       => functionArg1 != null ? WindowFunctionHelpers.BuildNTile(functionArg1, partitionBy, orderBy) : null,

				nameof(AnalyticFunctions.Sum) when functionArg1     != null => WindowFunctionHelpers.BuildSum(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Average) when functionArg1 != null => WindowFunctionHelpers.BuildAverage(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Min) when functionArg1     != null => WindowFunctionHelpers.BuildMin(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.Max) when functionArg1     != null => WindowFunctionHelpers.BuildMax(functionArg1, partitionBy, orderBy),

				nameof(AnalyticFunctions.Count) when functionArgCount == 0    => WindowFunctionHelpers.BuildCount(partitionBy, orderBy),
				nameof(AnalyticFunctions.Count) when functionArg1     != null => WindowFunctionHelpers.BuildCount(partitionBy, orderBy),

				nameof(AnalyticFunctions.Lead) when functionArg1 != null => WindowFunctionHelpers.BuildLead(functionArg1, functionArg2, functionArg3, partitionBy, orderBy),
				nameof(AnalyticFunctions.Lag) when functionArg1  != null => WindowFunctionHelpers.BuildLag(functionArg1, functionArg2, functionArg3, partitionBy, orderBy),

				nameof(AnalyticFunctions.FirstValue) when functionArg1 != null                       => WindowFunctionHelpers.BuildFirstValue(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.LastValue) when functionArg1  != null                       => WindowFunctionHelpers.BuildLastValue(functionArg1, partitionBy, orderBy),
				nameof(AnalyticFunctions.NthValue) when functionArg1 != null && functionArg2 != null => WindowFunctionHelpers.BuildNthValue(functionArg1, functionArg2, partitionBy, orderBy),

				_ => null, // Unsupported function — fall through to old pipeline
			};
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
	}
}
