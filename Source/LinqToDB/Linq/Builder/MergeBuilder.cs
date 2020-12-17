using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;
	using SqlQuery;

	internal partial class MergeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Method.IsGenericMethod
				&& LinqExtensions.ExecuteMergeMethodInfo == methodCall.Method.GetGenericMethodDefinition();
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var mergeContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			return mergeContext;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		private static SelectQuery RemoveContextFromQuery(TableBuilder.TableContext tableContext, SelectQuery query)
		{
			var clonedTableSource = tableContext.SelectQuery.From.Tables[0];
			while (clonedTableSource.Joins.Count > 0)
			{
				var join = clonedTableSource.Joins[0];
				tableContext.SelectQuery.From.Tables.Add(join.Table);
				clonedTableSource.Joins.RemoveAt(0);
			}

			tableContext.SelectQuery.From.Tables.RemoveAt(0);
			var queryVisitor = new QueryVisitor();
			queryVisitor.Visit(query, e =>
			{
				if (e is SelectQuery selectQuery && selectQuery.From.Tables.Count > 0)
				{
					if (selectQuery.From.Tables[0].Source is SelectQuery subSelect)
					{
						if (subSelect.From.Tables.Count == 0)
						{
							if (!subSelect.Where.IsEmpty)
							{
								selectQuery.Where.ConcatSearchCondition(subSelect.Where.SearchCondition);
							}

							selectQuery.From.Tables.RemoveAt(0);

							query.Walk(new WalkOptions(), qe =>
								{
									if (qe is SqlColumn column && column.Parent == subSelect)
									{
										return column.Expression;
									}

									return qe;
								});
						}

					}
				}
			});

			return query;
		}

		public static SqlSearchCondition BuildSearchCondition(ExpressionBuilder builder, SqlStatement statement, IBuildContext onContext, IBuildContext? secondContext, LambdaExpression condition)
		{
			SqlSearchCondition result;

			var isTableContext = onContext.IsExpression(null, 0, RequestFor.Table);
			if (isTableContext.Result)
			{
				var tableContext  = (TableBuilder.TableContext)onContext;
				var clonedContext = new TableBuilder.TableContext(builder, new SelectQuery(), tableContext.SqlTable);

				var targetParameter = Expression.Parameter(tableContext.ObjectType);

				if (secondContext != null)
				{
					var secondContextRefExpression =
							new ContextRefExpression(condition.Parameters[1].Type, secondContext);

					var newBody = condition.GetBody(targetParameter, secondContextRefExpression);
					condition = Expression.Lambda(newBody, targetParameter);
				}
				else
				{
					var newBody = condition.GetBody(targetParameter);
					condition   = Expression.Lambda(newBody, targetParameter);
				}

				var subqueryContext = new SubQueryContext(clonedContext);
				var contextRef      = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(tableContext.ObjectType), subqueryContext);
				var whereMethodInfo = Methods.Queryable.Where.MakeGenericMethod(tableContext.ObjectType);
				var whereCall       = Expression.Call(whereMethodInfo, contextRef, condition);

				var buildSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, whereCall, new SelectQuery()));

				var query = buildSequence.SelectQuery;
				query     = RemoveContextFromQuery(clonedContext, query);

				//TODO: Why it is not handled by main optimizer
				var sqlFlags = builder.DataContext.SqlProviderFlags;
				new SelectQueryOptimizer(sqlFlags, query, query, 0, statement)
					.FinalizeAndValidate(sqlFlags.IsApplyJoinSupported, sqlFlags.IsGroupByExpressionSupported);
				
				if (query.From.Tables.Count == 0)
				{
					result = query.Where.SearchCondition;
				}
				else
				{
					result = new SqlSearchCondition();
					result.Conditions.Add(new SqlCondition(false,
						new SqlPredicate.FuncLike(SqlFunction.CreateExists(query))));
				}
			}
			else
			{
				var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());
				result = new SqlSearchCondition();

				builder.BuildSearchCondition(
					new ExpressionContext(null, secondContext == null? new[] { onContext } : new[] { onContext, secondContext }, condition),
					conditionExpr,
					result.Conditions);
							
			}

			return result;
		}
	}
}
