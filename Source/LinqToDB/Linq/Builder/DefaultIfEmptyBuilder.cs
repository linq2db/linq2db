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

			return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue, true);
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			readonly bool _allowNullField;

			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, Expression? defaultValue, bool allowNullField)
				: base(parent, sequence, null)
			{
				_allowNullField = allowNullField;
				DefaultValue    = defaultValue;
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
				expr = Builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), this, expr, flags);

				if (flags.HasFlag(ProjectFlags.Expression) && expr is not SqlPlaceholderExpression)
				{
					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(expr);

					expr = ApplyNullability(expr);

					var notNull = placeholders
						.FirstOrDefault(placeholder => !placeholder.Sql.CanBeNull);

					if (notNull != null || _allowNullField)
					{
						if (notNull == null)
						{
							notNull = ExpressionBuilder.CreatePlaceholder(this,
								new SqlValue(1), Expression.Constant(1), alias: "not_null");
						}

						var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

						var notNullExpression = new SqlReaderIsNullExpression(notNull, true);
						expr = Expression.Condition(notNullExpression, expr, defaultValue);
					}
				}
				else
				{
					if (!flags.HasFlag(ProjectFlags.Keys))
						expr = ApplyNullability(expr);
				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneExpression(DefaultValue), _allowNullField);
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
