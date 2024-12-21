using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Linq.Builder
{
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

					var saveIsSourceOuter = mergeContext.SourceContext.IsSourceOuter;
					mergeContext.SourceContext.IsSourceOuter = true;

					builder.BuildSearchCondition(
						mergeContext.SourceContext, 
						conditionExpression, 
						operation.Where);

					mergeContext.SourceContext.IsSourceOuter = saveIsSourceOuter;
				}

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
