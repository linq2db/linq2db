﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class AllJoinsBuilder : MethodCallBuilder
	{
		static readonly string[] RightNullableOnlyMethodNames    = { "RightJoin", "FullJoin" };
		static readonly string[] NotRightNullableOnlyMethodNames = { "InnerJoin", "LeftJoin", "RightJoin", "FullJoin" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return IsMatchingMethod(methodCall, false);
		}

		internal static bool IsMatchingMethod(MethodCallExpression methodCall, bool rightNullableOnly)
		{
			return
				methodCall.IsQueryable("Join") && methodCall.Arguments.Count == 3
				|| !rightNullableOnly && methodCall.IsQueryable(NotRightNullableOnlyMethodNames) && methodCall.Arguments.Count == 2
				|| rightNullableOnly  && methodCall.IsQueryable(RightNullableOnlyMethodNames)    && methodCall.Arguments.Count == 2;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var argument = methodCall.Arguments[0];
			if (buildInfo.Parent != null)
			{
				argument = SequenceHelper.MoveToScopedContext(argument, buildInfo.Parent);
			}

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, argument));

			JoinType joinType;
			var conditionIndex = 1;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin" : joinType = JoinType.Inner; break;
				case "LeftJoin"  : joinType = JoinType.Left;  break;
				case "RightJoin" : joinType = JoinType.Right; break;
				case "FullJoin"  : joinType = JoinType.Full;  break;
				default:
					conditionIndex = 2;

					joinType = (SqlJoinType) builder.EvaluateExpression(methodCall.Arguments[1])! switch
					{
						SqlJoinType.Inner => JoinType.Inner,
						SqlJoinType.Left  => JoinType.Left,
						SqlJoinType.Right => JoinType.Right,
						SqlJoinType.Full  => JoinType.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)builder.EvaluateExpression(methodCall.Arguments[1])!}")
					};
					break;
			}

			buildInfo.JoinType = joinType;

			if (joinType == JoinType.Left || joinType == JoinType.Full)
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, null, false);

			sequence = new SubQueryContext(sequence);

			if (methodCall.Arguments[conditionIndex] != null)
			{
				var condition = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();

				var result = builder.BuildWhere(sequence, sequence,
					condition : condition, checkForSubQuery : false, enforceHaving : false,
					isTest : buildInfo.IsTest);

				if (result == null)
					return BuildSequenceResult.Error(methodCall);

				/*if (joinType == JoinType.Full)
				{
					result.SelectQuery.Where.SearchCondition =
						QueryHelper.CorrectComparisonForJoin(result.SelectQuery.Where.SearchCondition);
				}*/

				result.SetAlias(condition.Parameters[0].Name);
				return BuildSequenceResult.FromContext(result);
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
