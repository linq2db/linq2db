using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	static class SqlExtensions
	{
		public static bool IsInsert(this SqlStatement statement)
		{
			return
				statement.QueryType == QueryType.Insert ||
				statement.QueryType == QueryType.InsertOrUpdate;
		}

		public static bool NeedsIdentity(this SqlStatement statement)
		{
			return
				statement.QueryType == QueryType.Insert && ((SqlInsertStatement)statement).Insert.WithIdentity ||
				statement.QueryType == QueryType.InsertOrUpdate;
		}

		public static bool IsUpdate(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Update;
		}

		public static SqlField GetIdentityField(this SqlStatement statement)
		{
			return statement.GetInsertClause()?.Into.GetIdentityField();
		}

		public static SqlInsertClause GetInsertClause(this SqlStatement statement)
		{
			switch (statement)
			{
				case SqlInsertStatement insert:
					return insert.Insert;
				case SqlInsertOrUpdateStatement update:
					return update.Insert;
			}
			return null;
		}

		public static SqlInsertClause RequireInsertClause(this SqlStatement statement)
		{
			var result = statement.GetInsertClause();
			if (result == null)
				throw new LinqToDBException($"Insert clause not found in {statement.GetType().Name}");
			return result;
		}

		public static SqlUpdateClause GetUpdateClause(this SqlStatement statement)
		{
			switch (statement)
		{
				case SqlUpdateStatement update:
					return update.Update;
				case SqlInsertOrUpdateStatement insertOrUpdate:
					return insertOrUpdate.Update;
			}
			return null;
		}

		public static SqlUpdateClause RequireUpdateClause(this SqlStatement statement)
		{
			var result = statement.GetUpdateClause();
			if (result == null)
				throw new LinqToDBException($"Update clause not found in {statement.GetType().Name}");
			return result;
		}

		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
				return selectQuery;
		}

		public static T Clone<T>(this T cloneable)
			where T: ICloneableElement
		{
			return (T)cloneable.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);
		}

		public static T Clone<T>(this T cloneable, Predicate<ICloneableElement> doClone)
			where T: ICloneableElement
		{
			return (T)cloneable.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), doClone);
		}
	}
}
