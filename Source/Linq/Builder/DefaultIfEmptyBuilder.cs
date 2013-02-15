using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlBuilder;

	class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("DefaultIfEmpty");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence     = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

			if (buildInfo.Parent is SelectManyBuilder.SelectManyContext)
			{
				var groupJoin = ((SelectManyBuilder.SelectManyContext)buildInfo.Parent).Sequence[0] as JoinBuilder.GroupJoinContext;

				if (groupJoin != null)
				{
					groupJoin.SqlQuery.From.Tables[0].Joins[0].JoinType = SqlQuery.JoinType.Left;
					groupJoin.SqlQuery.From.Tables[0].Joins[0].IsWeak   = false;
				}
			}

			return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public class DefaultIfEmptyContext : SequenceContextBase
		{
			public DefaultIfEmptyContext(IBuildContext parent, IBuildContext sequence, Expression defaultValue) 
				: base(parent, sequence, null)
			{
				_defaultValue = defaultValue;
			}

			private readonly Expression _defaultValue;

			public override Expression BuildExpression(Expression expression, int level)
			{
				var expr = Sequence.BuildExpression(expression, level);

				if (expression == null)
				{
					var q =
						from col in SqlQuery.Select.Columns
						where !col.CanBeNull()
						select SqlQuery.Select.Columns.IndexOf(col);

					var idx = q.DefaultIfEmpty(-1).First();

					if (idx == -1)
						idx = SqlQuery.Select.Add(new SqlValue((int?)1));

					var n = ConvertToParentIndex(idx, this);

					Expression e = Expression.Call(
						ExpressionBuilder.DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						Expression.Constant(n));

					var defaultValue = _defaultValue ?? Expression.Constant(null, expr.Type);

					if (expr.NodeType == ExpressionType.Parameter)
					{
						var par  = (ParameterExpression)expr;
						var pidx = Builder.BlockVariables.IndexOf(par);

						if (pidx >= 0)
						{
							var ex = Builder.BlockExpressions[pidx];

							if (ex.NodeType == ExpressionType.Assign)
							{
								var bex = (BinaryExpression)ex;

								if (bex.Left == expr)
								{
									if (bex.Right.NodeType != ExpressionType.Conditional)
									{
										Builder.BlockExpressions[pidx] =
											Expression.Assign(
												bex.Left,
												Expression.Condition(e, defaultValue, bex.Right));
									}
								}
							}
						}
					}

					expr = Expression.Condition(e, defaultValue, expr);
				}

				return expr;
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				return Sequence.GetContext(expression, level, buildInfo);
			}
		}
	}
}
