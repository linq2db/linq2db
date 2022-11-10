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

			return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue);
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
				throw new NotImplementedException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
				{
				if (SequenceHelper.IsSameContext(path, this) && (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
					return path;

				var expr = base.MakeExpression(path, flags);

				if (!Disabled && flags.HasFlag(ProjectFlags.Expression) && SequenceHelper.IsSameContext(path, this))
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

					var defaultValue = DefaultValue ?? new DefaultValueExpression(Builder.MappingSchema, expr.Type);

					expr = Expression.Condition(new SqlReaderIsNullExpression(notNull), defaultValue, expr);
				}

				return expr;
				}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneExpression(DefaultValue));
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
