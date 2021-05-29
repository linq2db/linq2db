﻿namespace LinqToDB.SqlQuery
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
				statement.QueryType == QueryType.InsertOrUpdate ||
				statement.QueryType == QueryType.MultiInsert;
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
		public static bool IsDelete(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Delete;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlField? GetIdentityField(this SqlStatement statement)
		{
			return statement.GetInsertClause()?.Into!.GetIdentityField();
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlInsertClause? GetInsertClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlInsertStatement insert         => insert.Insert,
				SqlInsertOrUpdateStatement update => update.Insert,
				_                                 => null,
			};
		}

		public static SqlWithClause? GetWithClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlStatementWithQueryBase query => query.With,
				_                               => null,
			};
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
		public static SqlUpdateClause? GetUpdateClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlUpdateStatement update                 => update.Update,
				SqlInsertOrUpdateStatement insertOrUpdate => insertOrUpdate.Update,
				_                                         => null,
			};
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
		public static SqlOutputClause? GetOutputClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlInsertStatement insert => insert.Output,
				// SqlUpdateStatement update => throw new NotImplementedException(),
				SqlDeleteStatement delete => delete.Output,
				_ => null,
			};
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
	}
}
