using LinqToDB.Expressions;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder
	{
		internal class On : MethodCallBuilder
		{
			protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				if (methodCall.Method.IsGenericMethod)
				{
					var genericMethod = methodCall.Method.GetGenericMethodDefinition();
					return  LinqExtensions.OnMethodInfo1         == genericMethod
						 || LinqExtensions.OnMethodInfo2         == genericMethod
						 || LinqExtensions.OnTargetKeyMethodInfo == genericMethod;
				}

				return false;
			}

			protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
			{
				var mergeContext = (MergeContext)builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

				var statement = mergeContext.Merge;

				if (methodCall.Arguments.Count == 2)
				{
					// On<TTarget, TSource>(IMergeableOn<TTarget, TSource> merge, Expression<Func<TTarget, TSource, bool>> matchCondition)
					var predicate     = methodCall.Arguments[1];
					var condition     = (LambdaExpression)predicate.Unwrap();
					var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());

					mergeContext.AddTargetParameter(condition.Parameters[0]);
					mergeContext.AddSourceParameter(condition.Parameters[1]);

					var filterExpression = BuildSearchCondition(builder, statement, mergeContext.TargetContext, mergeContext.SourceContext,
						condition);

					statement.On.Conditions.AddRange(filterExpression.Conditions);
				}
				else if (methodCall.Arguments.Count == 3)
				{
					var targetKeyLambda = ((LambdaExpression)methodCall.Arguments[1].Unwrap());
					var sourceKeyLambda = ((LambdaExpression)methodCall.Arguments[2].Unwrap());

					var targetKeySelector = targetKeyLambda.Body.Unwrap();
					var sourceKeySelector = sourceKeyLambda.Body.Unwrap();

					var targetKeyContext = new ExpressionContext(buildInfo.Parent, mergeContext.TargetContext, targetKeyLambda);
					var sourceKeyContext = new ExpressionContext(buildInfo.Parent, mergeContext.SourceContext, sourceKeyLambda);

					if (targetKeySelector.NodeType == ExpressionType.New)
					{
						var new1 = (NewExpression)targetKeySelector;
						var new2 = (NewExpression)sourceKeySelector;

						for (var i = 0; i < new1.Arguments.Count; i++)
						{
							var arg1 = new1.Arguments[i];
							var arg2 = new2.Arguments[i];

							JoinBuilder.BuildJoin(builder, statement.On, targetKeyContext, arg1, sourceKeyContext, arg2);
						}
					}
					else if (targetKeySelector.NodeType == ExpressionType.MemberInit)
					{
						// TODO: migrate unordered members support to original code
						var mi1 = (MemberInitExpression)targetKeySelector;
						var mi2 = (MemberInitExpression)sourceKeySelector;

						if (mi1.Bindings.Count != mi2.Bindings.Count)
							throw new LinqException($"List of member inits does not match for entity type '{targetKeySelector.Type}'.");

						for (var i = 0; i < mi1.Bindings.Count; i++)
						{
							var binding2 = (MemberAssignment)mi2.Bindings.Where(b => b.Member == mi1.Bindings[i].Member).FirstOrDefault();
							if (binding2 == null)
								throw new LinqException($"List of member inits does not match for entity type '{targetKeySelector.Type}'.");

							var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
							var arg2 = binding2.Expression;

							JoinBuilder.BuildJoin(builder, statement.On, targetKeyContext, arg1, sourceKeyContext, arg2);
						}
					}
					else
					{
						JoinBuilder.BuildJoin(builder, statement.On, targetKeyContext, targetKeySelector, sourceKeyContext, sourceKeySelector);
					}
				}
				else
				{
					// OnTargetKey<TTarget>(IMergeableOn<TTarget, TTarget> merge)
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

				mergeContext.SourceContext.MatchBuilt();
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
