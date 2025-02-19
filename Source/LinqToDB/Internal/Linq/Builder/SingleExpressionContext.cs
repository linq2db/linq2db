using System;
using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class SingleExpressionContext : BuildContextBase
	{
		public SingleExpressionContext(TranslationModifier translationModifier, ExpressionBuilder builder, ISqlExpression sqlExpression, SelectQuery selectQuery)
			: base(translationModifier, builder, sqlExpression.SystemType ?? typeof(object), selectQuery)
		{
			SqlExpression = sqlExpression;
		}

		public ISqlExpression SqlExpression { get; }

		public override MappingSchema MappingSchema => Builder.MappingSchema;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				return ExpressionBuilder.CreatePlaceholder(this, SqlExpression, path);
			}

			throw new NotImplementedException();
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new SingleExpressionContext(TranslationModifier, Builder, context.CloneElement(SqlExpression), context.CloneElement(SelectQuery));
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}
	}
}
