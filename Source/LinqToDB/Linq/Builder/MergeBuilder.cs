using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;
	using LinqToDB.SqlQuery;

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
			queryVisitor.VisitParentFirst(query, e =>
			{
				// if (e is SqlJoinedTable join)
				// {
				// 	join.IsWeak = false;
				// }
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

				return true;
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

				TableBuilder.TableContext? secondClonedTableContext = null;

				if (secondContext != null)
				{
					ContextRefExpression secondContextRefExpression;

					//TODO: Investigate that this is needed for Source
					var isSecondTableContext = secondContext.IsExpression(null, 0, RequestFor.Table);
					if (isSecondTableContext.Result)
					{
						var secondTableContext = ((TableBuilder.TableContext)isSecondTableContext.Context!);
						secondClonedTableContext = new TableBuilder.TableContext(builder, new SelectQuery(),
							secondTableContext.SqlTable);
						secondContextRefExpression =
							new ContextRefExpression(condition.Parameters[1].Type, secondClonedTableContext);
					
					}
					else
					{
						secondContextRefExpression =
							new ContextRefExpression(condition.Parameters[1].Type, secondContext);
					}

					var newBody = condition.Body.Replace(condition.Parameters[1], secondContextRefExpression);
					condition = Expression.Lambda(newBody, condition.Parameters[0]);
				}

				var subqueryContext = new SubQueryContext(clonedContext);
				var contextRef      = new ContextRefExpression(typeof(IQueryable<>).MakeGenericType(tableContext.ObjectType), subqueryContext);
				var whereMethodInfo = Methods.Queryable.Where.MakeGenericMethod(tableContext.ObjectType);
				var whereCall       = Expression.Call(whereMethodInfo, contextRef, condition);

				var buildSequence = builder.BuildSequence(new BuildInfo((IBuildContext?)null, whereCall, new SelectQuery()));

				var query = buildSequence.SelectQuery;

				if (secondClonedTableContext != null)
					query.From.Table(secondClonedTableContext.SelectQuery);

				query = RemoveContextFromQuery(clonedContext, query);
				if (secondClonedTableContext != null)
				{
					query = RemoveContextFromQuery(secondClonedTableContext, query);
				}

				new SelectQueryOptimizer(builder.DataContext.SqlProviderFlags, null, query)
					.FinalizeAndValidate(false, true);

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
					result.Conditions,
					false);
							
			}

			return result;
		}
	}
}
