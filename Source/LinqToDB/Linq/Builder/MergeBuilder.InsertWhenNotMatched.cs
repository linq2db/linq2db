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

				if (!setter.IsNullValue())
				{
					var setterExpression = setter.UnwrapLambda();

					var correctedExpression = Expression.Lambda(mergeContext.SourceContext.PrepareSourceLambda(setterExpression));

					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						correctedExpression,
						mergeContext.TargetContext,
						operation.Items,
						mergeContext.SourceContext);
				}
				else
				{
					// build setters like QueryRunner.Insert
					var sqlTable   = (SqlTable)statement.Target.Source;

					var sourceRef = mergeContext.SourceContext.SourcePropAccess;
					var targetRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.TargetContext);

					var ed = builder.MappingSchema.GetEntityDescriptor(sqlTable.ObjectType);

					foreach (var column in ed.Columns)
					{
						var targetExpression = ExpressionExtensions.GetMemberGetter(column.MemberInfo, targetRef);

						if (!column.SkipOnInsert)
						{
							var sourceMemberInfo = sourceRef.Type.GetMemberEx(column.MemberInfo);
							if (sourceMemberInfo is null)
								throw new InvalidOperationException($"Member '{column.MemberInfo}' not found in type '{sourceRef.Type}'.");

							var sourceExpression = ExpressionExtensions.GetMemberGetter(sourceMemberInfo, sourceRef);
							var tgtExpr          = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);
							var srcExpr          = builder.ConvertToSql(mergeContext.SourceContext, sourceExpression);

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

					var conditionExpr = mergeContext.SourceContext.PrepareSourceLambda(condition);

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
