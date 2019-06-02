using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class InsertWhenNotMatched : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.Method.IsGenericMethod
					&& LinqExtensions.InsertWhenNotMatchedAndMethodInfo.GetGenericMethodDefinition() == methodCall.Method.GetGenericMethodDefinition();
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.Insert);
				statement.Operations.Add(operation);

				var predicate = methodCall.Arguments[1];
				var setter    = methodCall.Arguments[2];

				if (!(setter is ConstantExpression constSetter) || constSetter.Value != null)
				{
					var setterExpression = (LambdaExpression)setter.Unwrap();
					mergeContext.AddSourceParameter(setterExpression.Parameters[0]);

					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						setterExpression,
						mergeContext.TargetContext,
						operation.Items,
						new ExpressionContext(buildInfo.Parent, new[] { mergeContext.SourceContext }, setterExpression));
				}
				else
				{
					// build setters like QueryRunner.Insert
					var targetType = methodCall.Method.GetGenericArguments()[0];
					var sqlTable = new SqlTable(builder.MappingSchema, targetType);

					var param = Expression.Parameter(targetType, "s");
					foreach (var field in sqlTable.Fields.Values)
					{
						if (field.IsInsertable)
						{
							var expression = Expression.PropertyOrField(param, field.Name);
							var expr = mergeContext.SourceContext.ConvertToSql(expression, 1, ConvertFlags.Field)[0].Sql;

							operation.Items.Add(new SqlSetExpression(field, expr));
						}
						else if (field.IsIdentity)
						{
							var expr = builder.DataContext.CreateSqlProvider().GetIdentityExpression(sqlTable);

							if (expr != null)
								operation.Items.Add(new SqlSetExpression(field, expr));
						}
					}
				}

				if (!(predicate is ConstantExpression constPredicate) || constPredicate.Value != null)
				{
					var condition = (LambdaExpression)predicate.Unwrap();
					var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.SourceContext }, condition),
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
