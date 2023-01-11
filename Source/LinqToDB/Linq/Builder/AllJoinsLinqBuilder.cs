using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class AllJoinsLinqBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames4 = { "InnerJoin", "LeftJoin", "RightJoin", "FullJoin" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.Method.DeclaringType != typeof(LinqExtensions))
				return false;

			return
				methodCall.IsQueryable("Join"      ) && methodCall.Arguments.Count == 5 ||
				methodCall.IsQueryable(MethodNames4) && methodCall.Arguments.Count == 4 ||
				methodCall.IsQueryable("CrossJoin" ) && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			List<SqlQueryExtension>? extensions = null;

			if (innerContext is QueryExtensionBuilder.JoinHintContext jhc)
			{
				innerContext = jhc.Context;
				extensions   = jhc.Extensions;
			}

			JoinType joinType;
			var conditionIndex = 2;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin" : joinType = JoinType.Inner; break;
				case "CrossJoin" : joinType = JoinType.Inner; conditionIndex = -1; break;
				case "LeftJoin"  : joinType = JoinType.Left;  break;
				case "RightJoin" : joinType = JoinType.Right; break;
				case "FullJoin"  : joinType = JoinType.Full;  break;
				default:
					conditionIndex = 3;

					joinType = (SqlJoinType) methodCall.Arguments[2].EvaluateExpression()! switch
					{
						SqlJoinType.Inner => JoinType.Inner,
						SqlJoinType.Left  => JoinType.Left,
						SqlJoinType.Right => JoinType.Right,
						SqlJoinType.Full  => JoinType.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)methodCall.Arguments[2].EvaluateExpression()!}")
					};
					break;
			}


			DefaultIfEmptyBuilder.DefaultIfEmptyContext? outerDefaultIfEmpty = null;
			if (joinType == JoinType.Right || joinType == JoinType.Full)
				outerContext = outerDefaultIfEmpty = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, outerContext, null, false);
			outerContext = new SubQueryContext(outerContext);

			DefaultIfEmptyBuilder.DefaultIfEmptyContext? innerDefaultIfEmpty = null;
			if (joinType == JoinType.Left || joinType == JoinType.Full)
				innerContext = innerDefaultIfEmpty = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, innerContext, null, false);
			innerContext = new SubQueryContext(innerContext);

			var selector = methodCall.Arguments[^1].UnwrapLambda();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var joinContext = new JoinContext(buildInfo.Parent, selector, buildInfo.IsSubQuery, outerContext, innerContext)
#if DEBUG
			{
				Debug_MethodCall = methodCall
			}
#endif
			;

			if (conditionIndex != -1)
			{
				var condition     = methodCall.Arguments[conditionIndex].UnwrapLambda();
				
				// Comparison should be provided without DefaultIfEmptyBuilder, so we left original contexts for comparison
				// ScopeContext ensures that comparison will placed on needed level.
				//
				var conditionExpr = SequenceHelper.PrepareBody(condition, outerContext, innerContext);

				conditionExpr = builder.ConvertExpression(conditionExpr);

				var join  = new SqlFromClause.Join(joinType, innerContext.SelectQuery, null, false,
					Array<SqlFromClause.Join>.Empty);

				outerContext.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				if (extensions != null)
					join.JoinedTable.SqlQueryExtensions = extensions;

				var flags = ProjectFlags.SQL;

				builder.BuildSearchCondition(
					joinContext, 
					conditionExpr, flags,
					join.JoinedTable.Condition.Conditions);
			}
			else
			{
				outerContext.SelectQuery.From.Table(innerContext.SelectQuery);
			}

			return joinContext;
		}

		//TODO: Do we need this?
		sealed class JoinContext : SelectContext
		{
			public JoinContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, IBuildContext outerContext, IBuildContext innerContext) : base(parent, lambda, isSubQuery, outerContext, innerContext)
			{
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}
		}
	}
}
