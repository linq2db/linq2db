using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class UpdateWhenNotMatchedBySource : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.UpdateWhenNotMatchedBySourceAndMethodInfo == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenNotMatchedBySourceAnd(merge, searchCondition, setter)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.UpdateBySource);
				statement.Operations.Add(operation);

				Expression predicate = methodCall.Arguments[1];
				Expression setter = methodCall.Arguments[2];

				UpdateBuilder.BuildSetter(
					builder,
					buildInfo,
					(LambdaExpression)setter.Unwrap(),
					mergeContext,
					operation.Items,
					mergeContext);

				if (!(predicate is ConstantExpression constPredicate) || constPredicate.Value != null)
				{
					var condition = (LambdaExpression)predicate.Unwrap();
					var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.TargetContext, mergeContext.SourceContext }, condition),
						conditionExpr,
						operation.Where.Conditions,
						false);
				}

				return mergeContext;
			}

			protected override SequenceConvertInfo Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
			{
				return null;
			}
		}
	}
}
