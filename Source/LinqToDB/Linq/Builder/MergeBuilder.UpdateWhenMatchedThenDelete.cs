using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class UpdateWhenMatchedThenDelete : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(UpdateWhenMatchedAndThenDeleteMethodInfo);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenMatchedAndThenDelete(merge, searchCondition, setter, deleteCondition)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.UpdateWithDelete);
				statement.Operations.Add(operation);

				var predicate       = methodCall.Arguments[1];
				var setter          = methodCall.Arguments[2];
				var deletePredicate = methodCall.Arguments[3];

				if (!setter.IsNullValue())
				{
					var setterExpression = setter.UnwrapLambda();
					var setterExpressionCorrected = Expression.Lambda(mergeContext.SourceContext.PrepareTargetSourceLambda(setterExpression));

					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						setterExpressionCorrected,
						mergeContext.TargetContext,
						operation.Items);
				}
				else
				{
					// build setters like QueryRunner.Update
					var sqlTable   = (SqlTable)statement.Target.Source;
					var sourceProp = EnsureType(mergeContext.SourceContext.SourcePropAccess, sqlTable.ObjectType);
					var targetProp = EnsureType(mergeContext.SourceContext.TargetPropAccess, sqlTable.ObjectType);
					var keys       = sqlTable.GetKeys(false).Cast<SqlField>().ToList();

					foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
					{
						var sourceExpr = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, sourceProp);
						var targetExpr = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, targetProp);

						var tgtExpr    = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, targetExpr);
						var srcExpr    = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, sourceExpr);;

						operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
					}
				}

				if (!predicate.IsNullValue())
				{
					var predicateCondition = predicate.UnwrapLambda();
					var predicateConditionCorrected = mergeContext.SourceContext.PrepareTargetSourceLambda(predicateCondition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(mergeContext.SourceContext, predicateConditionCorrected,
						ProjectFlags.SQL, operation.Where.Conditions);
				}

				if (!deletePredicate.IsNullValue())
				{
					var deleteCondition = deletePredicate.UnwrapLambda();
					var deleteConditionCorrected = mergeContext.SourceContext.PrepareTargetSourceLambda(deleteCondition);

					operation.WhereDelete = new SqlSearchCondition();

					builder.BuildSearchCondition(mergeContext.SourceContext, deleteConditionCorrected,
						ProjectFlags.SQL, operation.WhereDelete.Conditions);
				}

				return mergeContext;
			}
		}
	}
}
