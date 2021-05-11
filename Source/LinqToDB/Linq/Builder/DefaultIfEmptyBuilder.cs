using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

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

			if (buildInfo.Parent is SelectManyBuilder.SelectManyContext context)
			{
				if (context.Sequence[0] is JoinBuilder.GroupJoinContext groupJoin)
				{
					groupJoin.SelectQuery.From.Tables[0].Joins[0].JoinType = JoinType.Left;
					groupJoin.SelectQuery.From.Tables[0].Joins[0].IsWeak   = false;
				}
			}

			return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public class DefaultIfEmptyContext : SequenceContextBase
		{
			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, Expression? defaultValue)
				: base(parent, sequence, null)
			{
				DefaultValue = defaultValue;
			}

			public Expression? DefaultValue { get; }

			public bool Disabled { get; set; }

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);

				var expr = Sequence.BuildExpression(expression, level, enforceServerSide);

				if (!Disabled && SequenceHelper.IsSameContext(expression, Sequence))
				{
					var q =
						from col in SelectQuery.Select.Columns
						where !col.CanBeNull
						select SelectQuery.Select.Columns.IndexOf(col);

					var idx = q.DefaultIfEmpty(-1).First();

					if (idx == -1)
					{
						idx = SelectQuery.Select.Add(new SqlValue((int?)1));
						SelectQuery.Select.Columns[idx].RawAlias = "is_empty";
					}

					var n = ConvertToParentIndex(idx, this);

					Expression e = Expression.Call(
						ExpressionBuilder.DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						Expression.Constant(n));

					var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

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

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, level, buildInfo);
			}
		}
	}
}
