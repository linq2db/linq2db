using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("Join", "InnerJoin", "LeftJoin", "RightJoin", "FullJoin", "CrossJoin")]
	sealed class AllJoinsLinqBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (call.Method.DeclaringType != typeof(LinqExtensions))
				return false;

			if (!call.IsQueryable())
				return false;

			return call.Arguments.Count == (call.Method.Name switch
			{
				"Join" => 5,
				"CrossJoin" => 3,
				_ => 4,
			});
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			List<SqlQueryExtension>? extensions = null;

			var jhc = SequenceHelper.GetJoinHintContext(innerContext);
			if (jhc != null)
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

					joinType = (SqlJoinType) builder.EvaluateExpression(methodCall.Arguments[2])! switch
					{
						SqlJoinType.Inner => JoinType.Inner,
						SqlJoinType.Left  => JoinType.Left,
						SqlJoinType.Right => JoinType.Right,
						SqlJoinType.Full  => JoinType.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)builder.EvaluateExpression(methodCall.Arguments[2])!}")
					};
					break;
			}

			if (joinType is JoinType.Right or JoinType.Full)
			{
				outerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					outerContext,
					outerContext,
					defaultValue: null,
					allowNullField: false,
					isNullValidationDisabled: false);
			}

			outerContext = new SubQueryContext(outerContext);

			if (joinType is JoinType.Left or JoinType.Full)
			{
				innerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					innerContext,
					innerContext,
					defaultValue: null,
					allowNullField: false,
					isNullValidationDisabled: false);
			}

			innerContext = new SubQueryContext(innerContext);

			var selector = methodCall.Arguments[^1].UnwrapLambda();
			var selectorBody = SequenceHelper.PrepareBody(selector, outerContext, new ScopeContext(innerContext, outerContext));

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var joinContext = new SelectContext(buildInfo.Parent, builder, null, selectorBody, outerContext.SelectQuery, buildInfo.IsSubQuery)
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

				var join = new SqlFromClause.Join(joinType, innerContext.SelectQuery, null, false, []);

				if (extensions != null)
					join.JoinedTable.SqlQueryExtensions = extensions;

				builder.BuildSearchCondition(
					joinContext,
					conditionExpr, 
					join.JoinedTable.Condition);

				outerContext.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				/*if (joinType == JoinType.Full)
				{
					join.JoinedTable.Condition = QueryHelper.CorrectComparisonForJoin(join.JoinedTable.Condition);
				}*/
			}
			else
			{
				outerContext.SelectQuery.From.Table(innerContext.SelectQuery);
			}

			return BuildSequenceResult.FromContext(joinContext);
		}
	}
}
