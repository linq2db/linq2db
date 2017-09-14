using System;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class AllJoinsBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Join") && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var tableContext = sequence as TableBuilder.TableContext;
			if (tableContext != null)
			{
				var joinValue = Expression.Lambda<Func<SqlJoinType>>(methodCall.Arguments[1]).Compile()();
				SelectQuery.JoinType joinType;
				switch (joinValue)
				{
					case SqlJoinType.Inner:
						joinType = SelectQuery.JoinType.Inner;
						break;
					case SqlJoinType.Left:
						joinType = SelectQuery.JoinType.Left;
						break;
					case SqlJoinType.Right:
						joinType = SelectQuery.JoinType.Right;
						break;
					case SqlJoinType.Full:
						joinType = SelectQuery.JoinType.Full;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				tableContext.JoinType = joinType;
			}

			if (methodCall.Arguments[2] != null)
			{
				var condition = (LambdaExpression) methodCall.Arguments[2].Unwrap();

				if (sequence.SelectQuery.Select.IsDistinct ||
				    sequence.SelectQuery.Select.TakeValue != null ||
				    sequence.SelectQuery.Select.SkipValue != null)
					sequence = new SubQueryContext(sequence);

				var result = builder.BuildWhere(buildInfo.Parent, sequence, condition, false, false);

				result.SetAlias(condition.Parameters[0].Name);
				return result;
			}
			
			return tableContext;
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