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

			if (buildInfo.Parent is SelectManyBuilder.SelectManyContext)
			{
				var groupJoin = ((SelectManyBuilder.SelectManyContext)buildInfo.Parent).Sequence[0] as JoinBuilder.GroupJoinContext;

				if (groupJoin != null)
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
				_defaultValue = defaultValue;
			}

			private readonly Expression? _defaultValue;

			public bool Disabled { get; set; }

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				expression = CorrectExpression(expression, this, Sequence);

				var expr = Sequence.BuildExpression(expression, level, enforceServerSide);

				if (!Disabled && expression == null)
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

					var defaultValue = _defaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

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

			static Expression? CorrectExpression(Expression? expression, IBuildContext current, IBuildContext underlying)
			{
				if (expression != null)
				{
					var root = expression.GetRootObject(current.Builder.MappingSchema);
					if (root is ContextRefExpression refExpression)
					{
						if (refExpression.BuildContext == current)
						{
							expression = expression.Replace(root, new ContextRefExpression(root.Type, underlying));
						};
					}
				}

				return expression;
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				expression = CorrectExpression(expression, this, Sequence);
				return Sequence.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				expression = CorrectExpression(expression, this, Sequence);
				return Sequence.ConvertToIndex(expression, level, flags);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				expression = CorrectExpression(expression, this, Sequence);
				return Sequence.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				expression = CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, level, buildInfo);
			}
		}
	}
}
