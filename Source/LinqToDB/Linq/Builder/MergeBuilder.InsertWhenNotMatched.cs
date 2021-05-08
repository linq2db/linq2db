using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
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
					&& LinqExtensions.InsertWhenNotMatchedAndMethodInfo == methodCall.Method.GetGenericMethodDefinition();
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
						mergeContext.SourceContext);
				}
				else
				{
					// build setters like QueryRunner.Insert
					var sqlTable   = (SqlTable)statement.Target.Source;
					var param      = Expression.Parameter(sqlTable.ObjectType, "s");

					foreach (var field in sqlTable.Fields)
					{
						if (field.IsInsertable)
						{
							var expression = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, param);
							var tgtExpr    = mergeContext.TargetContext.ConvertToSql(builder.ConvertExpression(expression), 1, ConvertFlags.Field)[0].Sql;
							var srcExpr    = mergeContext.SourceContext.ConvertToSql(builder.ConvertExpression(expression), 1, ConvertFlags.Field)[0].Sql;

							operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
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
					var condition     = (LambdaExpression)predicate.Unwrap();
					var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						new ExpressionContext(null, new[] { mergeContext.SourceContext }, condition),
						conditionExpr,
						operation.Where.Conditions);
				}

				return mergeContext;
			}

			protected override SequenceConvertInfo? Convert(
				ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
			{
				return null;
			}
		}
	}
}
