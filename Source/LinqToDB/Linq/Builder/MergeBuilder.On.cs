using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	using static LinqToDB.Reflection.Methods.LinqToDB.Merge;

	internal partial class MergeBuilder
	{
		internal class On : MethodCallBuilder
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

					var filterExpression = BuildSearchCondition(builder, statement, mergeContext.TargetContext, mergeContext.SourceContext,
						condition);

					statement.On.Conditions.AddRange(filterExpression.Conditions);
				}
				else if (methodCall.Arguments.Count == 3)
				{
					var targetKeyLambda = methodCall.Arguments[1].UnwrapLambda();
					var sourceKeyLambda = methodCall.Arguments[2].UnwrapLambda();

					var targetKeySelector = SequenceHelper.PrepareBody(targetKeyLambda, mergeContext.TargetContext).Unwrap();
					var sourceKeySelector = SequenceHelper.PrepareBody(sourceKeyLambda, mergeContext.SourceContext).Unwrap();

					var comparePredicate = builder.ConvertCompare(mergeContext, ExpressionType.Equal, targetKeySelector, sourceKeySelector,
						ProjectFlags.SQL);

					if (comparePredicate == null)
						throw new LinqException($"Could not create comparison for '{SqlErrorExpression.PrepareExpression(targetKeyLambda)}' and {SqlErrorExpression.PrepareExpression(sourceKeyLambda)}.");

					if (comparePredicate is SqlSearchCondition sc)
						statement.On.Conditions.AddRange(sc.Conditions);
					else
						statement.On.Conditions.Add(new SqlCondition(false, comparePredicate, false));
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

					var filterExpression = BuildSearchCondition(builder, statement, mergeContext.TargetContext, mergeContext.SourceContext,
						condition);

					statement.On.Conditions.AddRange(filterExpression.Conditions);
				}

				return mergeContext;
			}
		}
	}
}
