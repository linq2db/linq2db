using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllJoinsBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return IsMatchingMethod(methodCall, false);
		}

		internal static bool IsMatchingMethod(MethodCallExpression methodCall, bool rightNullableOnly)
		{
			return
				methodCall.IsQueryable("Join") && methodCall.Arguments.Count == 3
				|| !rightNullableOnly && methodCall.IsQueryable("InnerJoin", "WeakInnerJoin", "LeftJoin", "WeakLeftJoin", "RightJoin", "FullJoin") && methodCall.Arguments.Count == 2
				|| rightNullableOnly && methodCall.IsQueryable("RightJoin", "FullJoin") && methodCall.Arguments.Count == 2;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			JoinType joinType;
			var conditionIndex = 1;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin"     : joinType = JoinType.Inner; break;
				case "WeakInnerJoin" : joinType = JoinType.Inner; buildInfo.IsWeakJoin = true; break;
				case "LeftJoin"      : joinType = JoinType.Left;  break;
				case "WeakLeftJoin"  : joinType = JoinType.Left;  buildInfo.IsWeakJoin = true; break;
				case "RightJoin"     : joinType = JoinType.Right; break;
				case "FullJoin"      : joinType = JoinType.Full;  break;
				default:
					conditionIndex = 2;

					joinType = (SqlJoinType) methodCall.Arguments[1].EvaluateExpression()! switch
					{
						SqlJoinType.Inner => JoinType.Inner,
						SqlJoinType.Left  => JoinType.Left,
						SqlJoinType.Right => JoinType.Right,
						SqlJoinType.Full  => JoinType.Full,
						_                 => throw new ArgumentOutOfRangeException(),
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
