using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal sealed class InsertWhenNotMatched : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(InsertWhenNotMatchedAndMethodInfo);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.Insert);
				statement.Operations.Add(operation);

				var predicate = methodCall.Arguments[1];
				var setter    = methodCall.Arguments[2];

				Expression setterExpression;

				if (!setter.IsNullValue())
				{
					var setterLambda = setter.UnwrapLambda();

					setterExpression = mergeContext.SourceContext.PrepareSourceBody(setterLambda);

				}
				else
				{
					// build setters like QueryRunner.Insert

					setterExpression = builder.BuildFullEntityExpression(mergeContext.SourceContext.SourcePropAccess,
						mergeContext.SourceContext.SourceContextRef.Type, ProjectFlags.SQL,
						ExpressionBuilder.FullEntityPurpose.Insert);
				}

				var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
				UpdateBuilder.ParseSetter(builder,
					mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
					setterExpressions);
				UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, false);

				if (!predicate.IsNullValue())
				{
					var condition = predicate.UnwrapLambda();

					var conditionExpr = mergeContext.SourceContext.PrepareSourceBody(condition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						mergeContext.SourceContext,
						conditionExpr, ProjectFlags.SQL,
						operation.Where.Conditions);
				}

				return mergeContext;
			}
		}
	}
}
