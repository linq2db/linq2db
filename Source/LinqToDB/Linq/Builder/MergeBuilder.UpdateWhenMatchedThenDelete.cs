using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class UpdateWhenMatchedThenDelete : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.UpdateWhenMatchedAndThenDeleteMethodInfo.GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition();
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

				if (!(setter is ConstantExpression constSetter) || constSetter.Value != null)
					UpdateBuilder.BuildSetter(
						builder,
						buildInfo,
						(LambdaExpression)setter.Unwrap(),
						mergeContext,
						operation.Items,
						mergeContext);

				if (!(predicate is ConstantExpression constPredicate) || constPredicate.Value != null)
				{
					var predicateCondition = (LambdaExpression)predicate.Unwrap();
					var predicateConditionExpr = builder.ConvertExpression(predicateCondition.Body.Unwrap());

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.TargetContext, mergeContext.SourceContext }, predicateCondition),
						predicateConditionExpr,
						operation.Where.Conditions);
				}

				var deleteCondition = (LambdaExpression)predicate.Unwrap();
				var deleteConditionExpr = builder.ConvertExpression(deleteCondition.Body.Unwrap());

				builder.BuildSearchCondition(
					new ExpressionContext(null, new[] { mergeContext.TargetContext, mergeContext.SourceContext }, deleteCondition),
					deleteConditionExpr,
					operation.WhereDelete.Conditions);

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
