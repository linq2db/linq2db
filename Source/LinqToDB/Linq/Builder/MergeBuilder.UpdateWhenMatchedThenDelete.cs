using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.UpdateWhenMatchedAndThenDelete))]
		internal sealed class UpdateWhenMatchedThenDelete : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(UpdateWhenMatchedAndThenDeleteMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenMatchedAndThenDelete(merge, searchCondition, setter, deleteCondition)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.UpdateWithDelete);
				statement.Operations.Add(operation);

				var predicate       = methodCall.Arguments[1];
				var setter          = methodCall.Arguments[2];
				var deletePredicate = methodCall.Arguments[3];

				if (!setter.IsNullValue())
				{
					var setterLambda = setter.UnwrapLambda();
					var setterExpression = mergeContext.SourceContext.PrepareTargetSource(setterLambda);

					mergeContext.SourceContext.TargetContextRef.Alias = setterLambda.Parameters[0].Name;
					mergeContext.SourceContext.SourceContextRef.Alias = setterLambda.Parameters[1].Name;

					var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
					UpdateBuilder.ParseSetter(builder,
						mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
						setterExpressions);
					UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);
				}
				else
				{
					// build setters like QueryRunner.Update
					var sqlTable   = (SqlTable)statement.Target.Source;
					var sourceProp = mergeContext.SourceContext.SourcePropAccess.EnsureType(sqlTable.ObjectType);
					var targetProp = mergeContext.SourceContext.TargetPropAccess.EnsureType(sqlTable.ObjectType);
					var keys       = (sqlTable.GetKeys(false) ?? Enumerable.Empty<ISqlExpression>()).Cast<SqlField>().ToList();

					foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
					{
						var sourceExpr = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, sourceProp);
						var targetExpr = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, targetProp);

						var tgtExpr = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, targetExpr);
						var srcExpr = builder.ConvertToSql(mergeContext.SourceContext.SourceContextRef.BuildContext, sourceExpr);;

						operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
					}
				}

				var saveIsSourceOuter = mergeContext.SourceContext.IsSourceOuter;
				mergeContext.SourceContext.IsSourceOuter = false;

				if (!predicate.IsNullValue())
				{
					var predicateCondition = predicate.UnwrapLambda();
					var predicateConditionCorrected = mergeContext.SourceContext.PrepareTargetSource(predicateCondition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(mergeContext.SourceContext, predicateConditionCorrected, operation.Where);
				}

				if (!deletePredicate.IsNullValue())
				{
					var deleteCondition = deletePredicate.UnwrapLambda();
					var deleteConditionCorrected = mergeContext.SourceContext.PrepareTargetSource(deleteCondition);

					operation.WhereDelete = new SqlSearchCondition();

					builder.BuildSearchCondition(mergeContext.SourceContext, deleteConditionCorrected, operation.WhereDelete);
				}

				mergeContext.SourceContext.IsSourceOuter = saveIsSourceOuter;

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
