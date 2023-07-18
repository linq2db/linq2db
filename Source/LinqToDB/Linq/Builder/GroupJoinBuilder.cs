using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Reflection;
	using SqlQuery;
	using LinqToDB.Expressions;

	class GroupJoinBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("GroupJoin");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerExpression = methodCall.Arguments[0];
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, outerExpression, buildInfo.SelectQuery));

			var innerExpression = methodCall.Arguments[1].Unwrap();

			var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
			var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();
			var resultLambda   = methodCall.Arguments[4].UnwrapLambda();

			var outerKey = SequenceHelper.PrepareBody(outerKeyLambda, outerContext);

			var elementType = ExpressionBuilder.GetEnumerableElementType(resultLambda.Parameters[1].Type);
			var innerContext = new GroupJoinInnerContext(buildInfo.Parent, outerContext.SelectQuery, builder, 
				elementType,
				outerKey,
				innerKeyLambda, innerExpression);

			var resultExpression = SequenceHelper.PrepareBody(resultLambda, outerContext, innerContext);

			var context = new SelectContext(buildInfo.Parent, resultExpression, outerContext, buildInfo.IsSubQuery);

			return context;
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		class GroupJoinInnerContext : BuildContextBase
		{
			public GroupJoinInnerContext(IBuildContext? parent, SelectQuery outerQuery, ExpressionBuilder builder, Type elementType,
				Expression outerKey, LambdaExpression innerKeyLambda,
				Expression innerExpression)
			:base(builder, elementType, outerQuery)
			{
				Parent          = parent;
				OuterKey        = outerKey;
				InnerKeyLambda  = innerKeyLambda;
				InnerExpression = innerExpression;
			}

			Expression       OuterKey        { get; }
			LambdaExpression InnerKeyLambda  { get; }
			Expression       InnerExpression { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Expression) || flags.HasFlag(ProjectFlags.Expand)) 
				                                             && !path.Type.IsAssignableFrom(ElementType))
				{
					var result = GetGroupJoinCall();
					return result;
				}

				return path;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new GroupJoinInnerContext(null, context.CloneElement(SelectQuery), Builder, ElementType,
					context.CloneExpression(OuterKey), context.CloneExpression(InnerKeyLambda), context.CloneExpression(InnerExpression));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				var expr = GetGroupJoinCall();
				var sequence = Builder.BuildSequence(new BuildInfo(Parent, expr, new SelectQuery()));
				return sequence;
			}

			public override SqlStatement GetResultStatement()
			{
				throw new NotImplementedException();
			}

			Expression GetGroupJoinCall()
			{
				// Generating the following
				// innerExpression.Where(o => o.Key == innerKey)

				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(
						Builder.MappingSchema,
						OuterKey,
						InnerKeyLambda.Body),
					InnerKeyLambda.Parameters[0]);

				var expr = (Expression)Expression.Call(
					Methods.Queryable.Where.MakeGenericMethod(filterLambda.Parameters[0].Type),
					InnerExpression,
					filterLambda);

				expr = SequenceHelper.MoveToScopedContext(expr, this);

				return expr;
			}

		}
		
	}
}
