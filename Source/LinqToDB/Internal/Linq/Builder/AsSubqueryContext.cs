using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class AsSubqueryContext : SubQueryContext
	{
		public AsSubqueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql) : base(subQuery, selectQuery, addToSql)
		{
		}

		public AsSubqueryContext(IBuildContext subQuery, bool addToSql = true) : base(subQuery, addToSql)
		{
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAggregationRoot())
				return path;

			return base.MakeExpression(path, flags);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new AsSubqueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}
	}
}
