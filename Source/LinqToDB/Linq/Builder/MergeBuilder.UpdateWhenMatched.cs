using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class UpdateWhenMatched : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(UpdateWhenMatchedAndMethodInfo);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenMatchedAnd<TTarget, TSource>(merge, searchCondition, setter)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.Update);

				var predicate = methodCall.Arguments[1];
				var setter    = methodCall.Arguments[2];

				if (!setter.IsNullValue())
				{
					var setterExpression = (LambdaExpression)setter.Unwrap();
					UpdateBuilder.BuildSetterWithContext(
						builder,
						buildInfo,
						setterExpression,
						mergeContext.TargetContext,
						operation.Items,
						mergeContext.TargetContext, mergeContext.SourceContext);
				}
				else
				{
					// build setters like QueryRunner.Update
					var sqlTable   = (SqlTable)statement.Target.Source;

					var sourceRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.SourceContext);
					var targetRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.TargetContext);

					var keys       = sqlTable.GetKeys(false).Cast<SqlField>().ToList();
					foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
					{
						var sourceExpression = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, sourceRef);
						var targetExpression = LinqToDB.Expressions.Extensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, targetRef);
						var tgtExpr    = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);
						var srcExpr    = builder.ConvertToSql(mergeContext.SourceContext, sourceExpression);

						operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
					}

					// skip empty Update operation with implicit setter
					// per https://github.com/linq2db/linq2db/issues/2843
					if (operation.Items.Count == 0)
						return mergeContext;
				}

				statement.Operations.Add(operation);

				if (!predicate.IsNullValue())
				{
					var condition = predicate.UnwrapLambda();

					operation.Where = BuildSearchCondition(builder, statement, mergeContext.TargetContext, mergeContext.SourceContext, condition);
				}

				return mergeContext;
			}
		}
	}
}
