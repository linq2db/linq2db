﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllJoinsBuilder : MethodCallBuilder
	{
		private static readonly string[] RightNullableOnlyMethodNames    = { "RightJoin", "FullJoin" };
		private static readonly string[] NotRightNullableOnlyMethodNames = { "InnerJoin", "LeftJoin", "RightJoin", "FullJoin" };

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

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

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

					joinType = (SqlJoinType) methodCall.Arguments[1].EvaluateExpression()! switch
					{
						SqlJoinType.Inner => JoinType.Inner,
						SqlJoinType.Left  => JoinType.Left,
						SqlJoinType.Right => JoinType.Right,
						SqlJoinType.Full  => JoinType.Full,
						_                 => throw new InvalidOperationException($"Unexpected join type: {(SqlJoinType)methodCall.Arguments[1].EvaluateExpression()!}")
					};
					break;
			}

			buildInfo.JoinType = joinType;

			if (joinType == JoinType.Left || joinType == JoinType.Full)
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null);
			sequence = new SubQueryContext(sequence);

			if (methodCall.Arguments[conditionIndex] != null)
			{
				var condition = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();

				var result = builder.BuildWhere(buildInfo.Parent, sequence, condition, false, false);

				result.SetAlias(condition.Parameters[0].Name);
				return result;
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression? param)
		{
			return null;
		}
	}
}
