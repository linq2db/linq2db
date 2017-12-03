using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public static class SqlExtensions
	{
		public static bool IsInsert(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert ||
			       statement.QueryType == QueryType.InsertOrUpdate;
		}

		public static bool IsUpdate(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Update;
		}

		public static bool IsInsertWithIdentity(this SqlStatement statement)
		{
			return statement.IsInsert() && statement.SelectQuery.Insert.WithIdentity;
		}

		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
			return selectQuery;
		}

	}
}
