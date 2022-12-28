using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Common;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal sealed partial class MergeBuilder : MethodCallBuilder
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
				var deletedTableContext = new AnchorContext(null, new TableBuilder.TableContext(builder, selectQuery, deletedTable), SqlAnchor.AnchorKindEnum.Deleted);
				var insertedTableConext = new AnchorContext(null, new TableBuilder.TableContext(builder, selectQuery, insertedTable), SqlAnchor.AnchorKindEnum.Inserted);

				if (kind == MergeKind.MergeWithOutput)
				{
					var outputExpression = methodCall.Arguments[1].UnwrapLambda();

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
					var outputLambda = methodCall.Arguments[2].UnwrapLambda();
					var outputExpression = SequenceHelper.PrepareBody(outputLambda, actionFieldContext,
						deletedTableContext, insertedTableConext);

					var outputTable = methodCall.Arguments[1];
					var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));
					var destinationRef = new ContextRefExpression(methodCall.Method.GetGenericArguments()[2], destination);

					var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
					UpdateBuilder.ParseSetter(builder, destinationRef, outputExpression, outputSetters);
					UpdateBuilder.InitializeSetExpressions(builder, mergeContext.SourceContext,
						mergeContext.TargetContext, outputSetters, mergeContext.Merge.Output.OutputItems, false);

					mergeContext.Merge.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
				}
			}

			return mergeContext;
		}

		sealed class MergeOutputContext : SelectContext
		{
			public MergeOutputContext(IBuildContext? parent, LambdaExpression lambda, MergeContext mergeContext, IBuildContext emptyTable, IBuildContext deletedTable, IBuildContext insertedTable)
				: base(parent, lambda, false, emptyTable, deletedTable, insertedTable)
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

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				base.SetRunQuery(query, expr);

				var mergeStatement = (SqlMergeStatement)Statement!;

				mergeStatement.Output!.OutputColumns = Sequence[0].SelectQuery.Select.Columns.Select(c => c.Expression).ToList();

			}
		}

		public static void BuildMatchCondition(ExpressionBuilder builder, Expression condition, TableLikeQueryContext source,
			SqlSearchCondition searchCondition)
		{
			BuildMatchCondition(builder, condition, null, null, source, searchCondition);
		}

		public static void BuildMatchCondition(ExpressionBuilder builder, Expression targetKeySelector, Expression sourceKeySelector, TableLikeQueryContext source,
			SqlSearchCondition searchCondition)
		{
			BuildMatchCondition(builder, null, targetKeySelector, sourceKeySelector, source, searchCondition);
		}

		static void BuildMatchCondition(ExpressionBuilder builder, Expression? condition, Expression? targetKeySelector, Expression? sourceKeySelector, TableLikeQueryContext source, SqlSearchCondition searchCondition)
		{
			if (condition == null)
			{
				if (targetKeySelector == null || sourceKeySelector == null)
					throw new InvalidOperationException();

				if (!source.IsTargetAssociation(targetKeySelector))
				{
					var compareSearchCondition = builder.GenerateComparison(source.SourceContextRef.BuildContext, targetKeySelector, sourceKeySelector);
					searchCondition.Conditions.AddRange(compareSearchCondition.Conditions);
				}
				else
				{
					var cloningContext      = new CloningContext();
					var targetContext       = source.TargetContextRef.BuildContext;
					var clonedTargetContext = cloningContext.CloneContext(targetContext);
					var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

					var correctedTargetKeySelector = targetKeySelector.Replace(source.TargetPropAccess, clonedContextRef);

					var compareSearchCondition = builder.GenerateComparison(clonedTargetContext, correctedTargetKeySelector, sourceKeySelector);

					var selectQuery = clonedTargetContext.SelectQuery;

					selectQuery.Where.SearchCondition.Conditions.AddRange(compareSearchCondition.Conditions);

					var targetTable = GetTargetTable(targetContext);
					if (targetTable == null)
						throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

					var clonedTargetTable = GetTargetTable(clonedTargetContext);

					if (clonedTargetTable == null)
						throw new InvalidOperationException();

					var cleanQuery = ReplaceSourceInQuery(selectQuery, clonedTargetTable, targetTable);

					searchCondition.Conditions.Add(new SqlCondition(false,
						new SqlPredicate.FuncLike(SqlFunction.CreateExists(cleanQuery))));					
				}
			}
			else if (!source.IsTargetAssociation(condition))
			{
				builder.BuildSearchCondition(source.SourceContextRef.BuildContext, condition, ProjectFlags.SQL,
					searchCondition.Conditions);
			}
			else
			{
				var cloningContext      = new CloningContext();
				var targetContext       = source.TargetContextRef.BuildContext;
				var clonedTargetContext = cloningContext.CloneContext(targetContext);
				var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

				var correctedCondition = condition.Replace(source.TargetPropAccess, clonedContextRef);

				builder.BuildSearchCondition(clonedTargetContext, correctedCondition, ProjectFlags.SQL,
					clonedTargetContext.SelectQuery.Where.SearchCondition.Conditions);

				var targetTable = GetTargetTable(targetContext);
				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

				var clonedTargetTable = GetTargetTable(clonedTargetContext);

				if (clonedTargetTable == null)
					throw new InvalidOperationException();

				var cleanQuery = ReplaceSourceInQuery(clonedTargetContext.SelectQuery, clonedTargetTable, targetTable);

				searchCondition.Conditions.Add(new SqlCondition(false,
					new SqlPredicate.FuncLike(SqlFunction.CreateExists(cleanQuery))));
			}
		}

		public static SelectQuery ReplaceSourceInQuery(SelectQuery query, SqlTable toReplace, SqlTable replaceBy)
		{
			var clonedTableSource = query.From.Tables[0];
			while (clonedTableSource.Joins.Count > 0)
			{
				var join = clonedTableSource.Joins[0];
				query.From.Tables.Add(join.Table);
				clonedTableSource.Joins.RemoveAt(0);
			}

			query.From.Tables.RemoveAt(0);

			query = query.Convert((toReplace, replaceBy), allowMutation: true, static (visitor, e) =>
			{
				if (e is SqlField field)
				{
					if (field.Table == visitor.Context.toReplace)
					{
						return visitor.Context.replaceBy[field.Name] ?? throw new InvalidOperationException();
					}
				}

				return e;
			});

			return query;
		}

		public static SqlTable? GetTargetTable(IBuildContext target)
		{
			var tableContext = SequenceHelper.GetTableOrCteContext(target);
			return tableContext?.SqlTable;
		}

		static Expression EnsureType(Expression expression, Type type)
		{
			if (expression.Type == type)
				return expression;

			return Expression.Convert(expression, type);
		}
	}
}
