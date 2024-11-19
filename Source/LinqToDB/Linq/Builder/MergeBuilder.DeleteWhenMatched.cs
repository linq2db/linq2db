using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.DeleteWhenMatchedAnd))]
		internal sealed class DeleteWhenMatched : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(DeleteWhenMatchedAndMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.Delete);
				statement.Operations.Add(operation);

				var predicate = methodCall.Arguments[1];
				if (!predicate.IsNullValue())
				{
					var condition           = predicate.UnwrapLambda();
					var conditionExpression = mergeContext.SourceContext.PrepareTargetSource(condition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						mergeContext.SourceContext, 
						conditionExpression, 
						buildInfo.GetFlags(ProjectFlags.ForceOuterAssociation), 
						operation.Where);
				}

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
