using System;

namespace LinqToDB.SqlQuery
{
	public static class SqlExtensions
	{
		public static bool IsInsert(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert ||
			       statement.QueryType == QueryType.InsertOrUpdate;
		}

		public static bool IsInsertWithIdentity(this SqlStatement statement)
		{
			return statement.IsInsert() && ((SelectQuery)statement).Insert.WithIdentity;
		}

		public static SqlSelectClause AsSelect(this SqlStatement statement)
		{
			if (statement is SelectQuery selectQuery)
				return selectQuery.Select;
			throw new LinqToDBException($"Satetement {statement.QueryType} is not Select Statement");
		}

		public static SqlInsertClause AsInsert(this SqlStatement statement)
		{
			if (statement is SelectQuery selectQuery)
				return selectQuery.Insert;
			throw new LinqToDBException($"Satetement {statement.QueryType} is not Insert Statement");
		}

		public static SelectQuery AsQuery(this SqlStatement statement)
		{
			if (statement is SelectQuery selectQuery)
				return selectQuery;
			throw new LinqToDBException($"Satetement {statement.QueryType} is not SelectQuery");
		}

		public static ISqlExpression AsExpression(this SqlStatement statement)
		{
			if (statement is ISqlExpression expression)
				return expression;
			throw new LinqToDBException($"Satetement {statement.QueryType} do not supports ISqlExpression interface");
		}
	}
}
