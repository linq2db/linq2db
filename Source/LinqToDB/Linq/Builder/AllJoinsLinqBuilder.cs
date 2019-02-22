using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllJoinsLinqBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.Method.DeclaringType != typeof(LinqExtensions))
				return false;

			return
				methodCall.IsQueryable("Join") && methodCall.Arguments.Count == 5 ||
				methodCall.IsQueryable("InnerJoin", "LeftJoin", "RightJoin", "FullJoin") && methodCall.Arguments.Count == 4 ||
				methodCall.IsQueryable("CrossJoin") && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			outerContext = new SubQueryContext(outerContext);
			innerContext = new SubQueryContext(innerContext);

			var selector = (LambdaExpression)methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

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

					var joinValue = (SqlJoinType) methodCall.Arguments[2].EvaluateExpression();

					switch (joinValue)
					{
						case SqlJoinType.Inner : joinType = JoinType.Inner; break;
						case SqlJoinType.Left  : joinType = JoinType.Left;  break;
						case SqlJoinType.Right : joinType = JoinType.Right; break;
						case SqlJoinType.Full  : joinType = JoinType.Full;  break;
						default                : throw new ArgumentOutOfRangeException();
					}

					break;
			}

			if (conditionIndex != -1)
			{
				var condition     = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();
				var conditionExpr = builder.ConvertExpression(condition.Body.Unwrap());

				var join  = new SqlFromClause.Join(joinType, innerContext.SelectQuery, null, false,
					Array<SqlFromClause.Join>.Empty);

				outerContext.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				builder.BuildSearchCondition(
					new ExpressionContext(null, new[] {outerContext, innerContext}, condition),
					conditionExpr,
					join.JoinedTable.Condition.Conditions,
					false);
			}
			else
			{
				outerContext.SelectQuery.From.Table(innerContext.SelectQuery);
			}

			return new SelectContext(buildInfo.Parent, selector, outerContext, innerContext)
#if DEBUG
				{
					MethodCall = methodCall
				}
#endif
				;
		}

		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			return null;
		}
	}
}
