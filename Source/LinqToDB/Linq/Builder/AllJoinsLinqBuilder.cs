﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllJoinsLinqBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames4 = { "InnerJoin", "LeftJoin", "RightJoin", "FullJoin" };

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

			if (joinType == JoinType.Right || joinType == JoinType.Full)
				outerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, outerContext, null);
			outerContext = new SubQueryContext(outerContext);


			if (joinType == JoinType.Left || joinType == JoinType.Full)
				innerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, innerContext, null);
			innerContext = new SubQueryContext(innerContext);

			var selector = (LambdaExpression)methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var joinContext = new JoinContext(buildInfo.Parent, selector, outerContext, innerContext)
#if DEBUG
			{
				Debug_MethodCall = methodCall
			}
#endif
			;

			if (conditionIndex != -1)
			{
				var condition     = (LambdaExpression)methodCall.Arguments[conditionIndex].Unwrap();
				var conditionExpr = condition.GetBody(selector.Parameters[0], selector.Parameters[1]);

				conditionExpr = builder.ConvertExpression(conditionExpr);

				var join  = new SqlFromClause.Join(joinType, innerContext.SelectQuery, null, false,
					Array<SqlFromClause.Join>.Empty);

				outerContext.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				builder.BuildSearchCondition(
					joinContext, 
					conditionExpr,
					@join.JoinedTable.Condition.Conditions);
			}
			else
			{
				outerContext.SelectQuery.From.Table(innerContext.SelectQuery);
			}

			return joinContext;
		}

		protected override SequenceConvertInfo? Convert(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo,
			ParameterExpression? param)
		{
			return null;
		}

		class JoinContext : SelectContext
		{
			public JoinContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext outerContext, IBuildContext innerContext) : base(parent, lambda, outerContext, innerContext)
			{
			}

			public IBuildContext OuterContext => Sequence[0];
			public IBuildContext InnerContext => Sequence[1];

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				SqlInfo[]? result = null;

				if (expression != null)
				{
					var root = Builder.GetRootObject(expression);

					if (root.NodeType == ExpressionType.Parameter && root == Lambda.Parameters[1])
					{
						result = base.ConvertToSql(expression, level, flags);

						// we need exact column from InnerContext
						result = result.Select(s =>
							{
								var idx = InnerContext.SelectQuery.Select.Add(s.Sql);
								return new SqlInfo(s.MemberChain, InnerContext.SelectQuery.Select.Columns[idx], InnerContext.SelectQuery, idx);
							})
							.ToArray();
					}
				}

				if (result == null)
					result = base.ConvertToSql(expression, level, flags);

				return result;
			}
		}
	}
}
