using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

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

			const string NotNullPropName = "not_null";

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)) || flags.HasFlag(ProjectFlags.Expand))
					return path;

				var expr = base.MakeExpression(path, flags);

				if (flags.IsTraverse())
					return expr;

				if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
				{
					var placeholder = ExpressionBuilder.CreatePlaceholder(this,
						new SqlNullabilityExpression(new SqlValue(1)),
						path,
						alias : NotNullPropName);

					return placeholder;
				}

				expr = Builder.BuildSqlExpression(this, expr, flags);
				expr = Builder.UpdateNesting(this, expr);
				expr = SequenceHelper.CorrectTrackingPath(expr, this);

				if (flags.IsExpression() && expr.UnwrapConvert() is not SqlEagerLoadExpression)
				{
					if (expr is SqlPlaceholderExpression placeholder)
					{
						var nullablePlaceholder = placeholder.MakeNullable();
						if (path.Type != placeholder.Type)
						{
							return Expression.Condition(
								Expression.NotEqual(nullablePlaceholder, Expression.Default(placeholder.Type)),
								placeholder, Expression.Default(path.Type));
						}

						return placeholder;
					}

					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(expr);

					var notNull = placeholders
						.FirstOrDefault(placeholder => !placeholder.Sql.CanBeNullable(NullabilityContext.NonQuery));

					if (notNull != null || _allowNullField)
					{
						if (notNull == null)
						{
							notNull = ExpressionBuilder.CreatePlaceholder(this,
								new SqlNullabilityExpression (new SqlValue(1)), SequenceHelper.CreateSpecialProperty(path, typeof(int?), NotNullPropName), alias: NotNullPropName);
						}

						if (notNull.Type.IsValueType && !notNull.Type.IsNullable())
						{
							notNull = notNull.MakeNullable();
						}

						var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

						var notNullExpression = Expression.NotEqual(notNull, Expression.Constant(null, notNull.Type));
						if (expr is ContextConstructionExpression construct)
							expr = construct.InnerExpression;
						expr = new ContextConstructionExpression(this, Expression.Condition(notNullExpression, expr, defaultValue));
					}
				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneExpression(DefaultValue), _allowNullField);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, buildInfo);
			}
		}
	}
}
