using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal sealed class On : MethodCallBuilder
		{
			static readonly MethodInfo[] _supportedMethods = {OnMethodInfo1, OnMethodInfo2, OnTargetKeyMethodInfo};

			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				return methodCall.IsSameGenericMethod(_supportedMethods);
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;

				if (methodCall.Arguments.Count == 2)
				{
					// On<TTarget, TSource>(IMergeableOn<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> matchCondition)
					var predicate = methodCall.Arguments[1];
					var condition = predicate.UnwrapLambda();

					mergeContext.SourceContext.ConnectionLambda       = condition;

					// correct aliases for better error handling
					//
					mergeContext.SourceContext.TargetContextRef.Alias = condition.Parameters[0].Name;
					mergeContext.SourceContext.SourceContextRef.Alias = condition.Parameters[1].Name;

					var preparedCondition = mergeContext.SourceContext.GenerateCondition();

					BuildMatchCondition(builder, preparedCondition, mergeContext.SourceContext, statement.On);
				}
				else if (methodCall.Arguments.Count == 3)
				{
					var targetKeyLambda = methodCall.Arguments[1].UnwrapLambda();
					var sourceKeyLambda = methodCall.Arguments[2].UnwrapLambda();

					var targetKeySelector = mergeContext.SourceContext.PrepareTargetLambda(targetKeyLambda);
					var sourceKeySelector = mergeContext.SourceContext.PrepareSourceBody(sourceKeyLambda);

					mergeContext.SourceContext.TargetKeySelector = targetKeySelector;
					mergeContext.SourceContext.SourceKeySelector = sourceKeySelector;

					BuildMatchCondition(builder, targetKeySelector, sourceKeySelector, mergeContext.SourceContext, statement.On);
				}
				else
				{
					// OnTargetKey<TTarget>(IMergeableOn<TTarget, TTarget> merge)
					//

					var targetType       = statement.Target.SystemType!;
					var pTarget          = Expression.Parameter(targetType, "t");
					var pSource          = Expression.Parameter(targetType, "s");
					var targetDescriptor = builder.MappingSchema.GetEntityDescriptor(targetType);

					Expression? ex = null;

					for (var i = 0; i< targetDescriptor.Columns.Count; i++)
					{
						var column = targetDescriptor.Columns[i];
						if (!column.IsPrimaryKey)
							continue;

						var expr = Expression.Equal(
							Expression.MakeMemberAccess(pTarget, column.MemberInfo),
							Expression.MakeMemberAccess(pSource, column.MemberInfo));
						ex = ex != null ? Expression.AndAlso(ex, expr) : expr;
					}

					if (ex == null)
						throw new LinqToDBException("Method OnTargetKey() needs at least one primary key column");

					var condition = Expression.Lambda(ex, pTarget, pSource);

					mergeContext.SourceContext.ConnectionLambda = condition;

					var generatedCondition = mergeContext.SourceContext.GenerateCondition();

					BuildMatchCondition(builder, generatedCondition, mergeContext.SourceContext, statement.On);
				}

				return mergeContext;
			}
		}
	}
}
