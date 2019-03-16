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
		[ExpressionMethod(nameof(FreeTextTableImpl1))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term})");
		}

		static Expression<Func<ITable<TTable>, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl1<TTable, TKey>()
		{
			return (table, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term})");
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
		[ExpressionMethod(nameof(FreeTextTableImpl2))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl2<TTable, TKey>()
		{
			return (table, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl3))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, string, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl3<TTable, TKey>()
		{
			return (table, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl4))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term, int top, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl4<TTable, TKey>()
		{
			return (table, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl5))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, string term, int top, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl5<TTable, TKey>()
		{
			return (table, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using FREETEXTTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl6))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLangCode<TTable, TKey>(this ITable<TTable> table, string term, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl6<TTable, TKey>()
		{
			return (table, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, *, {term}, LANGUAGE {language})");
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
		[ExpressionMethod(nameof(FreeTextTableImpl7))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl7<TTable, TKey>()
		{
			return (table, columns, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
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
		[ExpressionMethod(nameof(FreeTextTableImpl8))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl8<TTable, TKey>()
		{
			return (table, columns, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
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
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl9))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, string language)
		{
				return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl9<TTable, TKey>()
		{
			return (table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
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
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl10))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int top, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, string, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl10<TTable, TKey>()
		{
			return (table, columns, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
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
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl11))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int top, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl11<TTable, TKey>()
		{
			return (table, columns, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
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
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(FreeTextTableImpl12))]
		public static IQueryable<FreeTextKey<TKey>> FreeTextTableWithLangCode<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string term, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> FreeTextTableImpl12<TTable, TKey>()
		{
			return (table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"FREETEXTTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
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
		[ExpressionMethod(nameof(ContainsTableImpl1))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search})");
		}

		static Expression<Func<ITable<TTable>, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl1<TTable, TKey>()
		{
			return (table, search) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search})");
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
		[ExpressionMethod(nameof(ContainsTableImpl2))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, string search, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl2<TTable, TKey>()
		{
			return (table, search, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl3))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, string search, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, string, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl3<TTable, TKey>()
		{
			return (table, search, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl4))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, string search, int top, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl4<TTable, TKey>()
		{
			return (table, search, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="top">Top filter to return top N ranked results.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl5))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, string search, int top, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, string, int, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl5<TTable, TKey>()
		{
			return (table, search, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language}, {top})");
		}

		/// <summary>
		/// Performs full-text search query using CONTAINSTABLE function against all full-text columns in table.
		/// </summary>
		/// <typeparam name="TTable">Queried table mapping class.</typeparam>
		/// <typeparam name="TKey">Full-text index key type.</typeparam>
		/// <param name="table">Table to perform full-text search query against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl6))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLangCode<TTable, TKey>(this ITable<TTable> table, string search, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl6<TTable, TKey>()
		{
			return (table, search, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, *, {search}, LANGUAGE {language})");
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
		[ExpressionMethod(nameof(ContainsTableImpl7))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl7<TTable, TKey>()
		{
			return (table, columns, term) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term})");
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
		[ExpressionMethod(nameof(ContainsTableImpl8))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int top)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl8<TTable, TKey>()
		{
			return (table, columns, term, top) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, {top})");
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
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl9))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int top, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl9<TTable, TKey>()
		{
			return (table, columns, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
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
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl10))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, string language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, string, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl10<TTable, TKey>()
		{
			return (table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
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
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl11))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTable<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int top, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language}, {top})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl11<TTable, TKey>()
		{
			return (table, columns, term, top, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language}, {top})");
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
		/// <returns>Returns full-text search ranking table.</returns>
		[ExpressionMethod(nameof(ContainsTableImpl12))]
		public static IQueryable<FreeTextKey<TKey>> ContainsTableWithLangCode<TTable, TKey>(this ITable<TTable> table, Expression<Func<TTable, object>> columns, string search, int language)
		{
			return table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {search}, LANGUAGE {language})");
		}

		static Expression<Func<ITable<TTable>, Expression<Func<TTable, object>>, string, int, IQueryable<FreeTextKey<TKey>>>> ContainsTableImpl12<TTable, TKey>()
		{
			return (table, columns, term, language) => table.DataContext.FromSql<FreeTextKey<TKey>>($"CONTAINSTABLE({Sql.TableExpr(table)}, ({Sql.FieldsExpr(table, columns)}), {term}, LANGUAGE {language})");
		}
		#endregion

		#endregion
	}
}
