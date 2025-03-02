using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.Merge), 
		nameof(LinqExtensions.MergeWithOutput), 
		nameof(LinqExtensions.MergeWithOutputInto))]
	internal sealed partial class MergeBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods =
		{
			ExecuteMergeMethodInfo,
			MergeWithOutput,
			MergeWithOutputSource,
			MergeWithOutputInto,
			MergeWithOutputIntoSource
		};

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsSameGenericMethod(_supportedMethods);

		enum MergeKind
		{
			Merge,
			MergeWithOutput,
			MergeWithOutputSource,
			MergeWithOutputInto,
			MergeWithOutputIntoSource
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var kind =
				methodCall.IsSameGenericMethod(MergeWithOutput)           ? MergeKind.MergeWithOutput :
				methodCall.IsSameGenericMethod(MergeWithOutputSource)     ? MergeKind.MergeWithOutputSource :
				methodCall.IsSameGenericMethod(MergeWithOutputInto)       ? MergeKind.MergeWithOutputInto :
				methodCall.IsSameGenericMethod(MergeWithOutputIntoSource) ? MergeKind.MergeWithOutputIntoSource :
				                                                            MergeKind.Merge;

			mergeContext.Kind = kind;

			if (kind != MergeKind.Merge)
			{
				var actionField   = SqlField.FakeField(new DbDataType(typeof(string)), PseudoFunctions.MERGE_ACTION, false);

				var (deletedContext, insertedContext) = UpdateBuilder.CreateDeletedInsertedContexts(builder, mergeContext.TargetContext, out var outputContext);

				mergeContext.Merge.Output = new SqlOutputClause();

				mergeContext.OutputContext = outputContext;

				var selectQuery        = outputContext.SelectQuery;
				var actionFieldContext = new SingleExpressionContext(builder.GetTranslationModifier(), builder, actionField, selectQuery);

				IBuildContext? sourceTableContext = null;

				if (kind is MergeKind.MergeWithOutputSource or MergeKind.MergeWithOutputIntoSource)
					sourceTableContext = mergeContext.SourceContext;

				if (kind is MergeKind.MergeWithOutput or MergeKind.MergeWithOutputSource)
				{
					var outputLambda = methodCall.Arguments[1].UnwrapLambda();
					var outputExpression = SequenceHelper.PrepareBody(outputLambda, actionFieldContext, deletedContext, insertedContext);

					// source
					if (outputLambda.Parameters.Count > 3)
					{
						outputExpression = outputExpression.Replace(outputLambda.Parameters[3],
							mergeContext.SourceContext.SourcePropAccess);
					}

					mergeContext.OutputExpression = outputExpression;
				}
				else
				{
					var outputLambda = methodCall.Arguments[2].UnwrapLambda();
					var outputExpression = SequenceHelper.PrepareBody(outputLambda, actionFieldContext, deletedContext, insertedContext);
					// source
					if (outputLambda.Parameters.Count > 3)
					{
						outputExpression = outputExpression.Replace(outputLambda.Parameters[3],
							mergeContext.SourceContext.SourcePropAccess);
					}

					var outputTable = methodCall.Arguments[1];
					var destination = builder.BuildSequence(new BuildInfo(buildInfo, outputTable, new SelectQuery()));
					var destinationRef = new ContextRefExpression(methodCall.Method.GetGenericArguments()[2], destination);

					var outputSetters = new List<UpdateBuilder.SetExpressionEnvelope>();
					UpdateBuilder.ParseSetter(builder, destinationRef, outputExpression, outputSetters);
					UpdateBuilder.InitializeSetExpressions(builder, mergeContext.SourceContext,
						mergeContext.TargetContext, outputSetters, mergeContext.Merge.Output.OutputItems, createColumns : false);

					mergeContext.Merge.Output.OutputTable = ((TableBuilder.TableContext)destination).SqlTable;
					mergeContext.OutputExpression = outputExpression;
				}
			}

			return BuildSequenceResult.FromContext(mergeContext);
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
					var compareSearchCondition = builder.GenerateComparison(source.SourceContextRef.BuildContext, targetKeySelector, sourceKeySelector, BuildPurpose.Sql);
					searchCondition.Predicates.AddRange(compareSearchCondition.Predicates);
				}
				else
				{
					var cloningContext      = new CloningContext();
					cloningContext.CloneElements(builder.GetCteClauses());
					var targetContext       = source.TargetContextRef.BuildContext;
					var clonedTargetContext = cloningContext.CloneContext(targetContext);
					var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

					var correctedTargetKeySelector = targetKeySelector.Replace(source.TargetPropAccess, clonedContextRef);

					var compareSearchCondition = builder.GenerateComparison(clonedTargetContext, correctedTargetKeySelector, sourceKeySelector, BuildPurpose.Sql);

					var selectQuery = clonedTargetContext.SelectQuery;

					selectQuery.Where.SearchCondition.Predicates.AddRange(compareSearchCondition.Predicates);

					var targetTable = GetTargetTable(targetContext);
					if (targetTable == null)
						throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

					var clonedTargetTable = GetTargetTable(clonedTargetContext);

					if (clonedTargetTable == null)
						throw new InvalidOperationException();

					var cleanQuery = ReplaceSourceInQuery(selectQuery, clonedTargetTable, targetTable);

					searchCondition.AddExists(cleanQuery);
				}
			}
			else if (!source.IsTargetAssociation(condition))
			{
				builder.BuildSearchCondition(source.SourceContextRef.BuildContext, condition, searchCondition);
			}
			else
			{
				var cloningContext      = new CloningContext();
				cloningContext.CloneElements(builder.GetCteClauses());
				var targetContext       = source.TargetContextRef.BuildContext;
				var clonedTargetContext = cloningContext.CloneContext(targetContext);
				var clonedContextRef    = source.TargetContextRef.WithContext(clonedTargetContext);

				var correctedCondition = condition.Replace(source.TargetPropAccess, clonedContextRef);

				builder.BuildSearchCondition(clonedTargetContext, correctedCondition,
					clonedTargetContext.SelectQuery.Where.EnsureConjunction());

				var targetTable = GetTargetTable(targetContext);
				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

				var clonedTargetTable = GetTargetTable(clonedTargetContext);

				if (clonedTargetTable == null)
					throw new InvalidOperationException();

				var cleanQuery = ReplaceSourceInQuery(clonedTargetContext.SelectQuery, clonedTargetTable, targetTable);

				searchCondition.AddExists(cleanQuery);
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
						return visitor.Context.replaceBy.FindFieldByMemberName(field.Name) ?? throw new InvalidOperationException();
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
	}
}
