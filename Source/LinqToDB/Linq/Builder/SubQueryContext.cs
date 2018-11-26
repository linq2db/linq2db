using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	class SubQueryContext : PassThroughContext
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

			SubQuery        = subQuery;
			SubQuery.Parent = this;
			SelectQuery     = selectQuery;
			Statement       = subQuery.Statement;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
			: this(subQuery, new SelectQuery { ParentSelect = subQuery.SelectQuery.ParentSelect }, addToSql)
		{
			Statement = subQuery.Statement;
		}

		public          IBuildContext SubQuery    { get; private set; }
		public override SelectQuery   SelectQuery { get; set; }
		public override IBuildContext Parent      { get; set; }

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			return SubQuery
				.ConvertToIndex(expression, level, flags)
				.Select(idx => new SqlInfo(idx.MemberChain) { Sql = SubQuery.SelectQuery.Select.Columns[idx.Index] })
				.ToArray();
		}

		// JoinContext has similar logic. Consider to review it.
		//
		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			return ConvertToSql(expression, level, flags)
				.Select(idx =>
				{
					idx.Query = SelectQuery;
					idx.Index = GetIndex((SqlColumn)idx.Sql);

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

		protected internal readonly Dictionary<ISqlExpression,int> ColumnIndexes = new Dictionary<ISqlExpression,int>();

		protected virtual int GetIndex(SqlColumn column)
		{
			if (!ColumnIndexes.TryGetValue(column, out var idx))
			{
				idx = SelectQuery.Select.Add(column);
				ColumnIndexes.Add(column, idx);
			}

			return idx;
		}

		public override int ConvertToParentIndex(int index, IBuildContext context)
		{
			var idx = GetIndex(context.SelectQuery.Select.Columns[index]);
			return Parent?.ConvertToParentIndex(idx, this) ?? idx;
		}

		public override void SetAlias(string alias)
		{
			if (alias == null)
				return;

			if (alias.Contains('<'))
				return;

			if (SelectQuery.From.Tables[0].Alias == null)
				SelectQuery.From.Tables[0].Alias = alias;
		}

		public override ISqlExpression GetSubQuery(IBuildContext context)
		{
			return null;
		}

		public override SqlStatement GetResultStatement()
		{
			return Statement ?? (Statement = new SqlSelectStatement(SelectQuery));
		}
	}
}
