using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;

	sealed class NullabilityBuildContext : BuildContextBase
	{
		public override MappingSchema MappingSchema => Context.MappingSchema;

		public          IBuildContext Context       { get; }

		public NullabilityBuildContext(IBuildContext context) : base(context.Builder, context.ElementType, context.SelectQuery)
		{
			Context = context;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var correctedPath = SequenceHelper.CorrectExpression(path, this, Context);
			var newExpr       = Builder.MakeExpression(Context, correctedPath, flags);

			// nothing changed, return as is
			if (ExpressionEqualityComparer.Instance.Equals(newExpr, correctedPath))
				return path;

			if (!flags.IsTest())
			{
				newExpr = SequenceHelper.StampNullability(newExpr, SelectQuery);
			}

			return newExpr;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new NullabilityBuildContext(context.CloneContext(Context));
		}

		public override SqlStatement GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, Context);
			return Context.GetContext(expression, buildInfo);
		}
	}
}
