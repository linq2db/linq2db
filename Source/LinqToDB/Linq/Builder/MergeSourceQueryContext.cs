namespace LinqToDB.Linq.Builder
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using SqlQuery;

	class MergeSourceQueryContext : SubQueryContext
	{
		private readonly IDictionary<string, SqlField> _sourceFields;

		public MergeSourceQueryContext(IBuildContext source, IDictionary<string, SqlField> sourceFields)
			: base(source, new SelectQuery { ParentSelect = source.SelectQuery }, true)
		{
			_sourceFields = sourceFields;
		}

		public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
		{
			return base.BuildExpression(expression, level, enforceServerSide);
		}

		public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			base.BuildQuery(query, queryParameter);
		}

		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			return base.ConvertToIndex(expression, level, flags);
		}

		public override int ConvertToParentIndex(int index, IBuildContext context)
		{
			return base.ConvertToParentIndex(index, context);
		}

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			return base.ConvertToSql(expression, level, flags);
		}

		public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
		{
			return base.GetContext(expression, level, buildInfo);
		}

		protected override int GetIndex(SqlColumn column)
		{
			return base.GetIndex(column);
		}

		public override ISqlExpression GetSubQuery(IBuildContext context)
		{
			return base.GetSubQuery(context);
		}

		public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
		{
			return base.IsExpression(expression, level, testFlag);
		}

		public override void SetAlias(string alias)
		{
			base.SetAlias(alias);
		}

		public override string ToString()
		{
			return base.ToString();
		}

		public override SqlStatement GetResultStatement()
		{
			return base.GetResultStatement();
		}
	}
}
