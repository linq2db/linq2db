using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Mapping;
	using SqlQuery;

	abstract class PassThroughContext : BuildContextBase
	{
		protected PassThroughContext(IBuildContext context, SelectQuery selectQuery) : base(context.TranslationModifier, context.Builder, context.ElementType, selectQuery)
		{
			Context = context;
			Parent  = context.Parent;
		}

		protected PassThroughContext(IBuildContext context) : this(context, context.SelectQuery)
		{
		}

		public override MappingSchema MappingSchema => Context.MappingSchema;
		public          IBuildContext Context       { get; protected set; }

		public override Expression?   Expression => Context.Expression;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var corrected = SequenceHelper.CorrectExpression(path, this, Context);
			var result = Builder.BuildExpression(Context, corrected);

			if (flags.IsSql())
			{
				result = SequenceHelper.CorrectTrackingPath(result, Context, this);
			}

			return result;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			Context.SetRunQuery(query, expr);
		}

		public override void SetAlias(string? alias)
		{
			Context.SetAlias(alias);
		}

		public override SqlStatement GetResultStatement()
		{
			return Context.GetResultStatement();
		}

		public override bool IsOptional => Context.IsOptional;

		public override bool IsSingleElement => Context.IsSingleElement;
	}
}
