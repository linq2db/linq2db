using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class InsertWhenNotMatched : MethodCallBuilder
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

				if (!setter.IsNullValue())
				{
					var setterExpression = (LambdaExpression)setter.Unwrap();
					mergeContext.AddSourceParameter(setterExpression.Parameters[0]);

					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						setterExpression,
						mergeContext.TargetContext,
						operation.Items,
						mergeContext.SourceContext);
				}
				else
				{
					// build setters like QueryRunner.Insert
					var sqlTable   = (SqlTable)statement.Target.Source;

					var sourceRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.SourceContext);
					var targetRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.TargetContext);

					var ed = builder.MappingSchema.GetEntityDescriptor(sqlTable.ObjectType);

					foreach (var column in ed.Columns)
					{
						var targetExpression = LinqToDB.Expressions.Extensions.GetMemberGetter(column.MemberInfo, targetRef);

						if (!column.SkipOnInsert)
						{
							var sourceExpression = LinqToDB.Expressions.Extensions.GetMemberGetter(column.MemberInfo, sourceRef);
							var tgtExpr    = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);
							var srcExpr    = builder.ConvertToSql(mergeContext.SourceContext, sourceExpression);

							operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
						}
						else if (column.IsIdentity)
						{
							var expr    = builder.DataContext.CreateSqlProvider().GetIdentityExpression(sqlTable);
							var tgtExpr = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);

							if (expr != null)
								operation.Items.Add(new SqlSetExpression(tgtExpr, expr));
						}
					}
				}

				if (!predicate.IsNullValue())
				{
					var condition = predicate.UnwrapLambda();

					var sourceRef = new ContextRefExpression(condition.Parameters[0].Type, mergeContext.SourceContext);
					var conditionExpr = builder.ConvertExpression(condition.GetBody(sourceRef));

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
