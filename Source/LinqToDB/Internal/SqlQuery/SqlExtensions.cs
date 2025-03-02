using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace LinqToDB.Internal.SqlQuery
{
	public static class SqlExtensions
	{
		public static bool IsInsert(this SqlStatement statement)
		{
			return
				statement.QueryType == QueryType.Insert ||
				statement.QueryType == QueryType.InsertOrUpdate ||
				statement.QueryType == QueryType.MultiInsert;
		}

		public static bool NeedsIdentity(this SqlStatement statement)
		{
			return statement.QueryType == QueryType.Insert && ((SqlInsertStatement)statement).Insert.WithIdentity;
		}

		public static bool IsUpdate(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Update;
		}

		public static bool IsDelete(this SqlStatement statement)
		{
			return statement != null && statement.QueryType == QueryType.Delete;
		}

		public static bool HasSomeModifiers(this SqlSelectClause select, bool ignoreSkip, bool ignoreTake)
		{
			return select.IsDistinct || (!ignoreSkip && select.SkipValue != null) || (!ignoreTake && select.TakeValue != null);
		}

		public static SqlField? GetIdentityField(this SqlStatement statement)
		{
			return statement.GetInsertClause()?.Into!.GetIdentityField();
		}

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

		public static SqlInsertClause RequireInsertClause(this SqlStatement statement)
		{
			var result = statement.GetInsertClause();
			if (result == null)
				throw new LinqToDBException($"Insert clause not found in {statement.GetType().Name}");
			return result;
		}

		public static SqlUpdateClause? GetUpdateClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlUpdateStatement update                 => update.Update,
				SqlInsertOrUpdateStatement insertOrUpdate => insertOrUpdate.Update,
				_                                         => null,
			};
		}

		public static SqlUpdateClause RequireUpdateClause(this SqlStatement statement)
		{
			var result = statement.GetUpdateClause();
			if (result == null)
				throw new LinqToDBException($"Update clause not found in {statement.GetType().Name}");
			return result;
		}

		public static SqlOutputClause? GetOutputClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlInsertStatement insert => insert.Output,
				SqlUpdateStatement update => update.Output,
				SqlDeleteStatement delete => delete.Output,
				_ => null,
			};
		}

		public static SelectQuery EnsureQuery(this SqlStatement statement)
		{
			var selectQuery = statement.SelectQuery;
			if (selectQuery == null)
				throw new LinqToDBException("Sqlect Query required");
			return selectQuery;
		}

		internal static bool IsSqlRow(this Expression expression)
			=> expression.Type.IsSqlRow();

		private static bool IsSqlRow(this Type type)
			=> type.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(Sql.SqlRow<,>);

		internal static ReadOnlyCollection<Expression> GetSqlRowValues(this Expression expr)
		{
			return expr is MethodCallExpression { Method.Name: "Row" } call
				? call.Arguments
				: throw new LinqToDBException("Calls to Sql.Row() are the only valid expressions of type SqlRow.");
		}

		internal static SqlTable Set(this SqlTable table, bool? set, TableOptions tableOptions)
		{
			if (set.HasValue)
			{
				if (set.Value) table.TableOptions |=  tableOptions;
				else           table.TableOptions &= ~tableOptions;
			}

			return table;
		}
	}
}
