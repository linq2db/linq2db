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

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (sequence == null)
				return null;

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
				if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
					return path;

				var corrected = base.MakeExpression(path, flags);

				if (ExpressionEqualityComparer.Instance.Equals(corrected, path))
					return path;

				if (flags.IsTraverse() || flags.IsRoot() || flags.IsTable() || flags.IsExtractProjection())
					return corrected;

				if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
				{
					var placeholder = ExpressionBuilder.CreatePlaceholder(this,
						new SqlNullabilityExpression(new SqlValue(1), true),
						path,
						alias : NotNullPropName);

					return placeholder;
				}

				var expr = Builder.BuildSqlExpression(this, corrected, flags/*.SqlFlag()*/);

				if (!flags.IsTest())
				{
					expr = Builder.UpdateNesting(this, expr);
				}

				expr = SequenceHelper.CorrectTrackingPath(expr, path);

				if (!flags.IsKeys() && expr.UnwrapConvert() is not SqlEagerLoadExpression)
				{
					if (expr is SqlPlaceholderExpression placeholder)
					{
						if (flags.IsExpression())
						{
							var nullablePlaceholder = placeholder.MakeNullable();
							if (path.Type != placeholder.Type)
							{
								return Expression.Condition(
									Expression.NotEqual(nullablePlaceholder, Expression.Default(placeholder.Type)),
									placeholder, Expression.Default(path.Type));
							}
						}

						return placeholder;
					}

					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(expr);

					var notNull = placeholders
						.FirstOrDefault(p => !p.Sql.CanBeNullable(NullabilityContext.NonQuery));

					if (notNull != null || _allowNullField)
					{
						if (notNull == null)
						{
							notNull = ExpressionBuilder.CreatePlaceholder(this,
								new SqlNullabilityExpression (new SqlValue(1), true), SequenceHelper.CreateSpecialProperty(path, typeof(int?), NotNullPropName), alias : NotNullPropName);
						}

						Expression notNullField = notNull;

						if (flags.IsExtractProjection())
						{
							notNullField = notNull.Path;
							if (notNullField.Type.IsValueType && !notNullField.Type.IsNullable())
							{
								notNullField = Expression.Convert(notNullField, notNullField.Type.AsNullable());
							}
						}
						else if (notNull.Type.IsValueType && !notNull.Type.IsNullable())
						{
							notNullField = notNull.MakeNullable();
						}

						var defaultValue = DefaultValue ?? new DefaultValueExpression(MappingSchema, expr.Type);

						var notNullExpression = Expression.NotEqual(notNullField, Expression.Constant(null, notNullField.Type));

						if (flags.IsExtractProjection())
						{
							expr = corrected;
						}

						expr = Expression.Condition(notNullExpression, expr, defaultValue);
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
