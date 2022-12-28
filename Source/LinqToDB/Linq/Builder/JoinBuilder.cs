using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class JoinBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.Method.DeclaringType == typeof(LinqExtensions) || !methodCall.IsQueryable("Join"))
				return false;

			// other overload for Join
			if (methodCall.Arguments[2].Unwrap() is not LambdaExpression lambda)
				return false;

			var body = lambda.Body.Unwrap();

			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in join is not allowed.");
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			outerContext = new SubQueryContext(outerContext);
			innerContext = new SubQueryContext(innerContext);

			List<SqlQueryExtension>? extensions = null;

			if (innerContext is QueryExtensionBuilder.JoinHintContext jhc)
			{
				innerContext = jhc.Context;
				extensions   = jhc.Extensions;
			}

			var join = innerContext.SelectQuery.InnerJoin();
			var sql  = outerContext.SelectQuery;

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			if (extensions != null)
				join.JoinedTable.SqlQueryExtensions = extensions;

			var selector = methodCall.Arguments[4].UnwrapLambda();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
			var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();

			var innerKeyContext  = innerContext;

			var outerKeySelector = SequenceHelper.PrepareBody(outerKeyLambda, outerContext).Unwrap();
			var innerKeySelector = SequenceHelper.PrepareBody(innerKeyLambda, innerKeyContext).Unwrap();

			var compareSearchCondition = builder.GenerateComparison(outerContext, outerKeySelector, innerKeySelector);

			join.JoinedTable.Condition.Conditions.AddRange(compareSearchCondition.Conditions);

			return new SelectContext(buildInfo.Parent, selector, buildInfo.IsSubQuery, outerContext, innerContext)
#if DEBUG
			{
				Debug_MethodCall = methodCall
			}
#endif
			;
		}

	}
}
