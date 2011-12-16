using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Sql;

namespace LinqToDB.Data.Linq.Builder
{
	class SubQueryContext : PassThroughContext
	{
		public readonly IBuildContext SubQuery;

		public SubQueryContext(IBuildContext subQuery, SqlQuery sqlQuery, bool addToSql)
			: base(subQuery)
		{
			if (sqlQuery == subQuery.SqlQuery)
				throw new ArgumentException("Wrong subQuery argument.", "subQuery");

			SubQuery = subQuery;
			SubQuery.Parent = this;

			SqlQuery = sqlQuery;

			if (addToSql)
				sqlQuery.From.Table(SubQuery.SqlQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql)
			: this(subQuery, new SqlQuery { ParentSql = subQuery.SqlQuery.ParentSql }, addToSql)
		{
		}

		public SubQueryContext(IBuildContext subQuery)
			: this(subQuery, true)
		{
		}

		public override SqlQuery      SqlQuery { get; set; }
		public override IBuildContext Parent   { get; set; }

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			return SubQuery
				.ConvertToIndex(expression, level, flags)
				.Select(idx => new SqlInfo { Sql = SubQuery.SqlQuery.Select.Columns[idx.Index], Member = idx.Member })
				.ToArray();
		}

		// JoinContext has similar logic. Consider to review it.
		//
		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			return ConvertToSql(expression, level, flags)
				.Select(idx =>
				{
					idx.Query = SqlQuery;
					idx.Index = GetIndex((SqlQuery.Column)idx.Sql);

					return idx;
				})
				.ToArray();
		}

		public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
		{
			switch (testFlag)
			{
				case RequestFor.SubQuery : return IsExpressionResult.True;
			}

			return base.IsExpression(expression, level, testFlag);
		}

		internal protected readonly Dictionary<ISqlExpression,int> ColumnIndexes = new Dictionary<ISqlExpression,int>();

		protected virtual int GetIndex(SqlQuery.Column column)
		{
			int idx;

			if (!ColumnIndexes.TryGetValue(column, out idx))
			{
				idx = SqlQuery.Select.Add(column);
				ColumnIndexes.Add(column, idx);
			}

			return idx;
		}

		public override int ConvertToParentIndex(int index, IBuildContext context)
		{
			var idx = GetIndex(context.SqlQuery.Select.Columns[index]);
			return Parent == null ? idx : Parent.ConvertToParentIndex(idx, this);
		}

		public override void SetAlias(string alias)
		{
			if (alias.Contains('<'))
				return;

			if (SqlQuery.From.Tables[0].Alias == null)
				SqlQuery.From.Tables[0].Alias = alias;
		}

		public override ISqlExpression GetSubQuery(IBuildContext context)
		{
			return null;
		}
	}
}
