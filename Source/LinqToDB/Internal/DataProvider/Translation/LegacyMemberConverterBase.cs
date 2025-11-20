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

		static string[] _allowedSequqnceMethods = new[]
		{
			nameof(Queryable.Select),
			nameof(Queryable.Distinct),
			nameof(Queryable.Where),
			nameof(Queryable.OrderBy),
			nameof(Queryable.OrderByDescending),
			nameof(Queryable.ThenBy),
			nameof(Queryable.ThenByDescending),
			nameof(Queryable.AsQueryable),
			nameof(Enumerable.AsEnumerable),
		};

		public Expression Convert(Expression expression, out bool handled)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				if (((MethodCallExpression)expression).IsSameGenericMethod(_toValueMethodInfo))
				{
					var chain = new List<MethodCallExpression>();
					if (BuildFunctionsChain(expression, chain, out var foudMethod, _stringAggregateMethodInfoE, _stringAggregateMethodInfoQ, _stringAggregateMethodInfoES, _stringAggregateMethodInfoQS))
					{
						var sequence  = foudMethod.Arguments[0];
						var separator = foudMethod.Arguments[1];

						sequence = BuildExpressionUtils.UnwrapEnumerableCasting(sequence);
						sequence = BuildExpressionUtils.EnsureEnumerableType(sequence);

						CollectOrderBy(chain, out var orderBy);
						if (orderBy.Length > 0)
						{
							sequence = ApplyOrderBy(sequence, orderBy);
						}

						if (foudMethod.Arguments.Count > 2)
						{
							var selector = foudMethod.Arguments[2].UnwrapLambda();
							sequence = Expression.Call(
								typeof(Enumerable),
								nameof(Enumerable.Select),
								new[] { selector.Parameters[0].Type, typeof(string) },
								sequence,
								selector);
						}

						var startSequence = sequence;
						while (startSequence is MethodCallExpression mc && mc.IsQueryable(_allowedSequqnceMethods))
						{
							startSequence = mc.Arguments[0];
						}

						if (startSequence.UnwrapConvert() is ParameterExpression)
						{
							// short path
							var simpleConcatExpression = Expression.Call(_concatStringMethodInfo, separator, sequence);
							handled = true;
							return simpleConcatExpression;
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

						var aggregateExecuteMethod = Methods.LinqToDB.AggregateExecute.MakeGenericMethod(elementType, expression.Type);
						var aggregateExecute = Expression.Call(
							aggregateExecuteMethod,
							startSequence,
							Expression.Lambda(concatExpression, parameter));

						handled = true;
						return aggregateExecute;
					}
				}
			}

			handled = false;
			return expression;
		}

		void CollectOrderBy(List<MethodCallExpression> chain, out OrderByInformation[] orderBy)
		{
			List<OrderByInformation>? orderByList = null;
			foreach (var methodCall in chain)
			{
				var methodName = methodCall.Method.Name;

				var isThenBy = methodName is nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending) or
					nameof(Enumerable.ThenBy) or nameof(Enumerable.ThenByDescending);

				if (isThenBy || methodName is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending) or
					nameof(Enumerable.OrderBy) or nameof(Enumerable.OrderByDescending))
				{
					var isDescending = methodName.EndsWith("Descending");

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
                    (true, true) => nameof(Queryable.OrderByDescending),
                    (true, false) => nameof(Queryable.OrderBy),
                    (false, true) => nameof(Queryable.ThenByDescending),
                    (false, false) => nameof(Queryable.ThenBy),
                };

				queryExpr = Expression.Call(typeof(Enumerable), methodName, [entityType, lambda.Body.Type], queryExpr, lambda);
				isFirst   = false;
			}

			return queryExpr;
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
