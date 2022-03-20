using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = {ExecuteMergeMethodInfo, MergeWithOutput, MergeWithOutputInto};

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(_supportedMethods);
		}

		enum MergeKind
		{
			Merge,
			MergeWithOutput,
			MergeWithOutputInto
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var kind = MergeKind.Merge; 

			if (methodCall.IsSameGenericMethod(MergeWithOutputInto))
				kind = MergeKind.MergeWithOutputInto;
			else if (methodCall.IsSameGenericMethod(MergeWithOutput))
			{
				kind = MergeKind.MergeWithOutput;
			}

			if (kind != MergeKind.Merge)
			{
				var objectType = methodCall.Method.GetGenericArguments()[0];

				var actionField   = SqlField.FakeField(new DbDataType(typeof(string)), "$action", false);
				var insertedTable = SqlTable.Inserted(objectType);
				var deletedTable  = SqlTable.Deleted(objectType);

				mergeContext.Merge.Output = new SqlOutputClause()
				{
					InsertedTable = insertedTable,
					DeletedTable  = deletedTable,
				};

				var selectQuery = new SelectQuery();

				var actionFieldContext  = new SingleExpressionContext(null, builder, actionField, selectQuery);
				var deletedTableContext = new TableBuilder.TableContext(builder, selectQuery, deletedTable);
				var insertedTableConext = new TableBuilder.TableContext(builder, selectQuery, insertedTable);

				if (kind == MergeKind.MergeWithOutput)
				{
					var outputExpression = (LambdaExpression)methodCall.Arguments[1].Unwrap();

					var outputContext = new MergeOutputContext(
						buildInfo.Parent,
						outputExpression,
						mergeContext,
						actionFieldContext,
						deletedTableContext,
						insertedTableConext
					);

					return outputContext;
				}
				else
				{
					var outputExpression = (LambdaExpression)methodCall.Arguments[2].Unwrap();

					var outputTable = methodCall.Arguments[1];
					var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));

					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						outputExpression,
						destination,
						mergeContext.Merge.Output.OutputItems,
						actionFieldContext,
						deletedTableContext,
						insertedTableConext
					);

					mergeContext.Merge.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
				}
			}

			return mergeContext;
		}


		class MergeOutputContext : SelectContext
		{
			public MergeOutputContext(IBuildContext? parent, LambdaExpression lambda, MergeContext mergeContext, IBuildContext emptyTable, IBuildContext deletedTable, IBuildContext insertedTable)
				: base(parent, lambda, emptyTable, deletedTable, insertedTable)
			{
				Statement = mergeContext.Statement;
				Sequence[0].SelectQuery.Select.Columns.Clear();
				Sequence[1].SelectQuery = Sequence[0].SelectQuery;
				Sequence[2].SelectQuery = Sequence[0].SelectQuery;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				var mergeStatement = (SqlMergeStatement)Statement!;

				mergeStatement.Output!.OutputColumns = Sequence[0].SelectQuery.Select.Columns.Select(c => c.Expression).ToList();

				QueryRunner.SetRunQuery(query, mapper);
			}
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
			query.Visit(query, static (query, e) =>
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

							query.Walk(WalkOptions.Default, subSelect, static (subSelect, qe) =>
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
					.FinalizeAndValidate(sqlFlags.IsApplyJoinSupported);
				
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
