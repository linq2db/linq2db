using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public static class SqlExtensions
	{

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsInsert(this SqlStatement statement)
		{
			return
				statement.QueryType == QueryType.Insert ||
				statement.QueryType == QueryType.InsertOrUpdate;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool NeedsIdentity(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert && ((SqlInsertStatement)statement).Insert.WithIdentity;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static bool IsUpdate(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Update;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlField GetIdentityField(this SqlStatement statement)
		{
			return statement.GetInsertClause()?.Into.GetIdentityField();
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
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

		public static SqlWithClause GetWithClause(this SqlStatement statement)
		{
			switch (statement)
			{
				case SqlStatementWithQueryBase query:
					return query.With;
			}
			return null;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlInsertClause RequireInsertClause(this SqlStatement statement)
		{
			var result = statement.GetInsertClause();
			if (result == null)
				throw new LinqToDBException($"Insert clause not found in {statement.GetType().Name}");
			return result;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
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

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlUpdateClause RequireUpdateClause(this SqlStatement statement)
		{
			var result = statement.GetUpdateClause();
			if (result == null)
				throw new LinqToDBException($"Update clause not found in {statement.GetType().Name}");
			return result;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlOutputClause GetOutputClause(this SqlStatement statement)
		{
			switch (statement)
			{
				case SqlInsertStatement insert:
					return insert.Output;
				//case SqlUpdateStatement update:
				//	throw new NotImplementedException();
				//case SqlDeleteStatement delete:
				//	throw new NotImplementedException();
			}
			return null;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
				return selectQuery;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static T Clone<T>(this T cloneable)
			where T: ICloneableElement
		{
			return (T)cloneable.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static T Clone<T>(this T cloneable, Predicate<ICloneableElement> doClone)
			where T: ICloneableElement
		{
			return (T)cloneable.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), doClone);
		}
	}
}
