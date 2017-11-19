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
			return
				methodCall.IsQueryable("Join") && methodCall.Arguments.Count == 3 ||
				methodCall.IsQueryable("InnerJoin", "LeftJoin", "RightJoin", "FullJoin");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			SelectQuery.JoinType joinType;
			var conditionIndex = 1;

			switch (methodCall.Method.Name)
			{
				case "InnerJoin" : joinType = SelectQuery.JoinType.Inner; break;
				case "LeftJoin"  : joinType = SelectQuery.JoinType.Left;  break;
				case "RightJoin" : joinType = SelectQuery.JoinType.Right; break;
				case "FullJoin"  : joinType = SelectQuery.JoinType.Full;  break;
				default:
					conditionIndex = 2;

					var joinValue = (SqlJoinType) methodCall.Arguments[1].EvaluateExpression();

					switch (joinValue)
					{
						case SqlJoinType.Inner : joinType = SelectQuery.JoinType.Inner; break;
						case SqlJoinType.Left  : joinType = SelectQuery.JoinType.Left;  break;
						case SqlJoinType.Right : joinType = SelectQuery.JoinType.Right; break;
						case SqlJoinType.Full  : joinType = SelectQuery.JoinType.Full;  break;
						default                : throw new ArgumentOutOfRangeException();
					}

					break;
			}

			buildInfo.JoinType = joinType;

			if (methodCall.Arguments[conditionIndex] != null)
			{
				var condition = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();

				if (sequence.SelectQuery.Select.IsDistinct ||
					sequence.SelectQuery.Select.TakeValue != null ||
					sequence.SelectQuery.Select.SkipValue != null)
					sequence = new SubQueryContext(sequence);

				var result = builder.BuildWhere(buildInfo.Parent, sequence, condition, false, false);

				result.SetAlias(condition.Parameters[0].Name);
				return result;
			}

			return sequence;
		}

		// Method copied from WhereBuilder
		protected override SequenceConvertInfo Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression param)
		{
			var predicate = (LambdaExpression)methodCall.Arguments[2].Unwrap();
			var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0]);

			if (info != null)
			{
				info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, predicate.Parameters[0], ex));

				if (param != null)
				{
					if (param.Type != info.Parameter.Type)
						param = Expression.Parameter(info.Parameter.Type, param.Name);

					if (info.ExpressionsToReplace != null)
						foreach (var path in info.ExpressionsToReplace)
						{
							path.Path = path.Path.Transform(e => e == info.Parameter ? param : e);
							path.Expr = path.Expr.Transform(e => e == info.Parameter ? param : e);
						}
				}

				info.Parameter = param;

				return info;
			}

			return null;
		}
	}
}