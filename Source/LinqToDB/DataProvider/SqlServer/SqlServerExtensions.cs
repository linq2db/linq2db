using LinqToDB.Linq;
using LinqToDB.Mapping;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	public interface ISqlServerExtensions
	{
	}

	public static class SqlServerExtensions
	{
		public static ISqlServerExtensions SqlServer(this Sql.ISqlExtension ext) => null;

		#region FTS
		public class FreeTextKey<TKey>
		{
			[Column("KEY")]  public TKey Key;
			[Column("RANK")] public int  Rank;
		}

		#region FreeTextTable
		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl1))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl1<TTable, TKey>()
		{
			return (ext, table, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl2))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl2<TTable, TKey>()
		{
			return (ext, table, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl3))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl3<TTable, TKey>()
		{
			return (ext, table, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search', LANGUAGE N'language', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl4))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term, string language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl4<TTable, TKey>()
		{
			return (ext, table, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search', LANGUAGE language_code, top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl5))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term, int language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl5<TTable, TKey>()
		{
			return (ext, table, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// Example: "FREETEXTTABLE(table, *, N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl6))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string term, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl6<TTable, TKey>()
		{
			return (ext, table, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl7))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl7<TTable, TKey>()
		{
			return (ext, table, columns, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl8))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl8<TTable, TKey>()
		{
			return (ext, table, columns, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl9))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, string language)
		{
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl9<TTable, TKey>()
		{
			return (ext, table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search', LANGUAGE N'language', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl10))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, string language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl10<TTable, TKey>()
		{
			return (ext, table, columns, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search', LANGUAGE language_code, top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl11))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl11<TTable, TKey>()
		{
			return (ext, table, columns, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against specified full-text columns.
		/// Example: "FREETEXTTABLE(table, (col1, col2), N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl12))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl12<TTable, TKey>()
		{
			return (ext, table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}
		#endregion

		#region ContainsTable
		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl1))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl1<TTable, TKey>()
		{
			return (ext, table, search) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl2))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl2<TTable, TKey>()
		{
			return (ext, table, search, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl3))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl3<TTable, TKey>()
		{
			return (ext, table, search, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search', LANGUAGE N'language', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl4))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search, string language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl4<TTable, TKey>()
		{
			return (ext, table, search, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl5))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search, int language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl5<TTable, TKey>()
		{
			return (ext, table, search, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// Example: "CONTAINSTABLE(table, *, N'search', LANGUAGE language_code, top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl6))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, string search, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl6<TTable, TKey>()
		{
			return (ext, table, search, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl7))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl7<TTable, TKey>()
		{
			return (ext, table, columns, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl8))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl8<TTable, TKey>()
		{
			return (ext, table, columns, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search', LANGUAGE N'language', top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl9))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, string language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl9<TTable, TKey>()
		{
			return (ext, table, columns, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl10))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl10<TTable, TKey>()
		{
			return (ext, table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search', LANGUAGE language_code, top)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl11))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int language, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl11<TTable, TKey>()
		{
			return (ext, table, columns, term, language, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against specified full-text columns.
		/// Example: "CONTAINSTABLE(table, (col1, col2), N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="columns">Selector expression for full-text columns that should be queried.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl12))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLanguage<TTable, TKey>(this ISqlServerExtensions ext, ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");
		}

		static Expression<Func<ISqlServerExtensions, ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl12<TTable, TKey>()
		{
			return (ext, table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}
		#endregion

		#region FreeText
		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// Example: "FREETEXT(table, N'search')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({1}, {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText<TEntity>(this ISqlServerExtensions ext, TEntity entity, string term)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// Example: "FREETEXT(table, N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({1}, {2}, LANGUAGE {3})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, string term, string language)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeTextWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// Example: "FREETEXT(table, N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({1}, {2}, LANGUAGE {3})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, string term, int language)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeTextWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// Example: "FREETEXT((col1, col2), N'search')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string term, [ExprParameter] params object[] columns)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// Example: "FREETEXT((col1, col2), N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string term, [ExprParameter] string language, [ExprParameter] params object[] columns)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeTextWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// Example: "FREETEXT((col1, col2), N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string term, [ExprParameter] int language, [ExprParameter] params object[] columns)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(FreeTextWithLanguage)}' is server-side method.");
		}

		#endregion

		#region Contains
		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// Example: "CONTAINS(table, N'search')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search term.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({1}, {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains<TEntity>(this ISqlServerExtensions ext, TEntity entity, string search)
			where TEntity: class
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// Example: "CONTAINS(table, N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({1}, {2}, LANGUAGE {3})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, string search, string language)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(ContainsWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// Example: "CONTAINS(table, N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({1}, {2}, LANGUAGE {3})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, string search, int language)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(ContainsWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// Example: "CONTAINS((col1, col2), N'search')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string search, [ExprParameter] params object[] columns)
			where TEntity: class
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// Example: "CONTAINS((col1, col2), N'search', LANGUAGE N'language')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string search, [ExprParameter] string language, [ExprParameter] params object[] columns)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(ContainsWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// Example: "CONTAINS((col1, col2), N'search', LANGUAGE language_code)".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLanguage<TEntity>(this ISqlServerExtensions ext, TEntity entity, [ExprParameter] string search, [ExprParameter] int language, [ExprParameter] params object[] columns)
			where TEntity : class
		{
			throw new LinqException($"'{nameof(ContainsWithLanguage)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// Example: "CONTAINS(PROPERTY(column, 'property'), N'search')".
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl1))]
		public static bool ContainsProperty(this ISqlServerExtensions ext, object column, string property, string search)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<ISqlServerExtensions, object, string, string, bool>> ContainsPropertyImpl1()
		{
			return (ext, column, property, search) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search})");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// Example: "CONTAINS(PROPERTY(column, 'property'), N'search', LANGUAGE N'language')".
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl2))]
		public static bool ContainsPropertyWithLanguage(this ISqlServerExtensions ext, object column, string property, string search, string language)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<ISqlServerExtensions, object, string, string, string, bool>> ContainsPropertyImpl2()
		{
			return (ext, column, property, search, language) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search}, LANGUAGE {language})");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// Example: "CONTAINS(PROPERTY(column, 'property'), N'search', LANGUAGE language_code)".
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl3))]
		public static bool ContainsPropertyWithLanguage(this ISqlServerExtensions ext, object column, string property, string search, int language)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<ISqlServerExtensions, object, string, string, int, bool>> ContainsPropertyImpl3()
		{
			return (ext, column, property, search, language) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search}, LANGUAGE {language})");
		}

		#endregion

		#endregion
	}
}
