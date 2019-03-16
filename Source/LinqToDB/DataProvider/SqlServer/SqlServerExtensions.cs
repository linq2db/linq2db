using LinqToDB.Mapping;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	public static class SqlServerExtensions
	{
		#region FTS
		public class FreeTextKey<TKey>
		{
			[Column("KEY")]  public TKey Key;
			[Column("RANK")] public int  Rank;
		}

		#region FreeTextTable
		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			string term,
			int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			string term,
			string language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			string term,
			int language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string term,
			int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string term,
			string language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string term,
			int language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}
		#endregion

		#region ContainsTable

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			string search,
			int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			string search,
			string language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			string search,
			int language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string search,
			int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string search,
			string language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Optional top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(
			this ITable<TTable> table,
			Expression<Func<TTable, object>> columns,
			string search,
			int language,
			int? top)
		{
			if (top == null)
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");

			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}
		#endregion

		#endregion
	}
}
