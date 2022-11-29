using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("DefaultIfEmpty");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence     = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

			return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue);
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, Expression? defaultValue)
				: base(parent, sequence, null)
			{
				DefaultValue = defaultValue;
			}

			public Expression? DefaultValue { get; }

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			static Expression ApplyNullability(Expression expr)
			{
				return expr.Transform(e =>
				{
					if (e.NodeType == ExpressionType.Extension && e is SqlPlaceholderExpression placeholder)
					{
						if (!placeholder.Sql.CanBeNull)
						{
							return placeholder.WithSql(new SqlNullabilityExpression(placeholder.Sql));
						}
					}

					return e;
				});
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)) || flags.HasFlag(ProjectFlags.Expand))
					return path;

				var expr = base.MakeExpression(path, flags);

				if (SequenceHelper.IsSameContext(path, this))
				{
					expr = Builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), this, expr, flags);

					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(expr);

					var notNull = placeholders
						.FirstOrDefault(placeholder => !placeholder.Sql.CanBeNull);

					if (notNull == null)
					{
						notNull = ExpressionBuilder.CreatePlaceholder(this,
							new SqlValue(1), Expression.Constant(1), alias: "not_null");
					}

					if (!flags.HasFlag(ProjectFlags.Comparison))
						expr = ApplyNullability(expr);

					if (flags.HasFlag(ProjectFlags.Expression))
					{
						var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

						if (expr is not SqlPlaceholderExpression)
						{
							Expression notNullExpression;

							notNullExpression = new SqlReaderIsNullExpression(notNull, true);

							expr = Expression.Condition(notNullExpression, expr, defaultValue);
						}
					}
				}
				else
				{
					if (!flags.HasFlag(ProjectFlags.Comparison))
						expr = ApplyNullability(expr);
				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneExpression(DefaultValue));
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, level, buildInfo);
			}
		}
	}
}
