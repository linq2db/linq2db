using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

using static LinqToDB.Internal.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Internal.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.UpdateWhenMatchedAnd))]
		internal sealed class UpdateWhenMatched : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(UpdateWhenMatchedAndMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				// UpdateWhenMatchedAnd<TTarget, TSource>(merge, searchCondition, setter)
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;
				var operation = new SqlMergeOperationClause(MergeOperationType.Update);

				var predicate = methodCall.Arguments[1];
				var setter    = methodCall.Arguments[2];

				if (!setter.IsNullValue())
				{
					var setterLambda = (LambdaExpression)setter.Unwrap();

					var setterExpression = mergeContext.SourceContext.PrepareTargetSource(setterLambda);

					var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
					UpdateBuilder.ParseSetter(builder,
						mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type),
						setterExpression, setterExpressions);

					UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);
				}
				else
				{
					// build setters like QueryRunner.Update
					var sqlTable   = (SqlTable)statement.Target.Source;

					var sourceRef = mergeContext.SourceContext.SourcePropAccess;
					var targetRef = new ContextRefExpression(sqlTable.ObjectType, mergeContext.TargetContext);
					var keys      = sqlTable.GetKeys(false)!.Cast<SqlField>().ToList();

					foreach (var field in sqlTable.Fields.Where(f => f.IsUpdatable).Except(keys))
					{
						var sourceMemberInfo = sourceRef.Type.GetMemberEx(field.ColumnDescriptor.MemberInfo);
						if (sourceMemberInfo is null)
							throw new InvalidOperationException($"Member '{field.ColumnDescriptor.MemberInfo}' not found in type '{sourceRef.Type}'.");

						var sourceExpression = ExpressionExtensions.GetMemberGetter(sourceMemberInfo, sourceRef);
						var targetExpression = ExpressionExtensions.GetMemberGetter(field.ColumnDescriptor.MemberInfo, targetRef);
						var tgtExpr          = builder.ConvertToSql(mergeContext.TargetContext, targetExpression);
						var srcExpr          = builder.ConvertToSql(mergeContext.SourceContext, sourceExpression);

						operation.Items.Add(new SqlSetExpression(tgtExpr, srcExpr));
					}

					// skip empty Update operation with implicit setter
					// per https://github.com/linq2db/linq2db/issues/2843
					if (operation.Items.Count == 0)
						return BuildSequenceResult.FromContext(mergeContext);
				}

				statement.Operations.Add(operation);

				if (!predicate.IsNullValue())
				{
					var condition = predicate.UnwrapLambda();

					var conditionPrepared = mergeContext.SourceContext.PrepareTargetSource(condition);

					operation.Where = new SqlSearchCondition();

					var saveIsSourceOuter = mergeContext.SourceContext.IsSourceOuter;
					mergeContext.SourceContext.IsSourceOuter = false;

					builder.BuildSearchCondition(mergeContext.SourceContext.SourceContextRef.BuildContext,
						conditionPrepared, operation.Where);

					mergeContext.SourceContext.IsSourceOuter = saveIsSourceOuter;
				}

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
