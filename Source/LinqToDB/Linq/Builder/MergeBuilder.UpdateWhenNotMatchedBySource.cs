using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal sealed class UpdateWhenNotMatchedBySource : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(UpdateWhenNotMatchedBySourceAndMethodInfo);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenNotMatchedBySourceAnd(merge, searchCondition, setter)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.UpdateBySource);
				statement.Operations.Add(operation);

				Expression predicate = methodCall.Arguments[1];
				var setter = methodCall.Arguments[2].UnwrapLambda();

				var setterCorrected = Expression.Lambda(mergeContext.SourceContext.PrepareSelfTargetLambda(setter));

				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					setterCorrected,
					mergeContext.TargetContext,
					operation.Items,
					mergeContext.SourceContext);

				if (!predicate.IsNullValue())
				{
					var condition          = predicate.UnwrapLambda();
					var conditionCorrected = mergeContext.SourceContext.PrepareSelfTargetLambda(condition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(mergeContext.TargetContext, conditionCorrected, ProjectFlags.SQL, operation.Where.Conditions);
				}

				return mergeContext;
			}
		}
	}
}
