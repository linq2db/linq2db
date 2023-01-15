using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("DefaultIfEmpty")]
	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

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

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)) || flags.HasFlag(ProjectFlags.Expand))
					return path;

				var expr = base.MakeExpression(path, flags);
				expr = Builder.BuildSqlExpression(this, expr, flags);
				expr = Builder.UpdateNesting(this, expr);

				if (flags.HasFlag(ProjectFlags.Expression) && expr is not SqlPlaceholderExpression)
				{
					var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(expr);

					var notNull = placeholders
						.FirstOrDefault(placeholder => !placeholder.Sql.CanBeNull);

					if (notNull != null || _allowNullField)
					{
						if (notNull == null)
						{
							notNull = ExpressionBuilder.CreatePlaceholder(this,
								new SqlNullabilityExpression (new SqlValue(1)), Expression.Constant(1), alias: "not_null");
						}

						if (notNull.Type.IsValueType && !notNull.Type.IsNullable())
						{
							notNull = notNull.MakeNullable();
						}

						var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

						var notNullExpression = new SqlReaderIsNullExpression(notNull, true);
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
