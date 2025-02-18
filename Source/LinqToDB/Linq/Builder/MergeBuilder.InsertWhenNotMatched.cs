﻿using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		[BuildsMethodCall(nameof(LinqExtensions.InsertWhenNotMatchedAnd))]
		internal sealed class InsertWhenNotMatched : MethodCallBuilder
		{
			public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
				=> call.IsSameGenericMethod(InsertWhenNotMatchedAndMethodInfo);

			protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

					setterExpression = builder.BuildFullEntityExpression(
						builder.MappingSchema, mergeContext.SourceContext.SourcePropAccess,
						mergeContext.SourceContext.SourceContextRef.Type, ProjectFlags.SQL,
						EntityConstructorBase.FullEntityPurpose.Insert);
				}

				var setterExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
				UpdateBuilder.ParseSetter(builder,
					mergeContext.SourceContext.TargetContextRef.WithType(setterExpression.Type), setterExpression,
					setterExpressions);
				UpdateBuilder.InitializeSetExpressions(builder, mergeContext.TargetContext, mergeContext.SourceContext, setterExpressions, operation.Items, createColumns : false);

				if (!predicate.IsNullValue())
				{
					var condition = predicate.UnwrapLambda();

					var conditionExpr = mergeContext.SourceContext.PrepareSourceBody(condition);

					operation.Where = new SqlSearchCondition();

					builder.BuildSearchCondition(
						mergeContext.SourceContext,
						conditionExpr,
						operation.Where);
				}

				return BuildSequenceResult.FromContext(mergeContext);
			}
		}
	}
}
