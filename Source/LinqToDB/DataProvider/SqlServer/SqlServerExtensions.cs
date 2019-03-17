using LinqToDB.Linq;
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

		#region FreeText
		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against table in single-table query.
		/// </summary>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT(*, {0})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText(string term)
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against table in single-table query.
		/// </summary>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT(*, {0}, LANGUAGE {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText(string term, string language)
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against table in single-table query.
		/// </summary>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT(*, {0}, LANGUAGE {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText(string term, int language)
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({0}, {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText<TEntity>(this TEntity entity, string term)
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({0}, {1}, LANGUAGE {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLang<TEntity>(this TEntity entity, string term, string language)
		{
			throw new LinqException($"'{nameof(FreeTextWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("FREETEXT({0}, {1}, LANGUAGE {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLang<TEntity>(this TEntity entity, string term, int language)
		{
			throw new LinqException($"'{nameof(FreeTextWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeText<TEntity>(this TEntity entity, [ExprParameter("term")] string term, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(FreeText)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLang<TEntity>(this TEntity entity, [ExprParameter("term")] string term, [ExprParameter("language")] string language, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(FreeTextWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using FREETEXT predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="term">Full-text search term.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("FREETEXT(({columns, ', '}), {term}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool FreeTextWithLang<TEntity>(this TEntity entity, [ExprParameter("term")] string term, [ExprParameter("language")] int language, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(FreeTextWithLang)}' is server-side method.");
		}

		#endregion

		#region Contains
		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against table in single-table query.
		/// </summary>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS(*, {0})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains(string search)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against table in single-table query.
		/// </summary>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS(*, {0}, LANGUAGE {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains(string search, string language)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against table in single-table query.
		/// </summary>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS(*, {0}, LANGUAGE {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains(string search, int language)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search term.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({0}, {1})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains<TEntity>(this TEntity entity, string search)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({0}, {1}, LANGUAGE {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLang<TEntity>(this TEntity entity, string search, string language)
		{
			throw new LinqException($"'{nameof(ContainsWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against all full-text columns in specified table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Expression("CONTAINS({0}, {1}, LANGUAGE {2})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLang<TEntity>(this TEntity entity, string search, int language)
		{
			throw new LinqException($"'{nameof(ContainsWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search})", IsPredicate = true, ServerSideOnly = true)]
		public static bool Contains<TEntity>(this TEntity entity, [ExprParameter("search")] string search, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(Contains)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLang<TEntity>(this TEntity entity, [ExprParameter("search")] string search, [ExprParameter("language")] string language, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(ContainsWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS predicate against specified full-text columns.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <param name="columns">Full-text columns that should be queried.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[Sql.Extension("CONTAINS(({columns, ', '}), {search}, LANGUAGE {language})", IsPredicate = true, ServerSideOnly = true)]
		public static bool ContainsWithLang<TEntity>(this TEntity entity, [ExprParameter("search")] string search, [ExprParameter("language")] int language, [ExprParameter("columns")] params object[] columns)
		{
			throw new LinqException($"'{nameof(ContainsWithLang)}' is server-side method.");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl1))]
		public static bool ContainsProperty<TEntity, TColumn>(this TEntity entity, TColumn column, string property, string search)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<TEntity, TColumn, string, string, bool>> ContainsPropertyImpl1<TEntity, TColumn>()
		{
			return (entity, column, property, search) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search})");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language name (see syslanguages.alias).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl2))]
		public static bool ContainsProperty<TEntity, TColumn>(this TEntity entity, TColumn column, string property, string search, string language)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<TEntity, TColumn, string, string, string, bool>> ContainsPropertyImpl2<TEntity, TColumn>()
		{
			return (entity, column, property, search, language) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search}, LANGUAGE {language})");
		}

		/// <summary>
		/// Applies full-text search condition using CONTAINS(PROPERTY(...)) predicate against specified full-text column property.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="property">Name of document property to search in.</param>
		/// <param name="search">Full-text search condition.</param>
		/// <param name="language">Language LCID code (see syslanguages.lcid).</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		[ExpressionMethod(nameof(ContainsPropertyImpl3))]
		public static bool ContainsProperty<TEntity, TColumn>(this TEntity entity, TColumn column, string property, string search, int language)
		{
			throw new LinqException($"'{nameof(ContainsProperty)}' is server-side method.");
		}

		static Expression<Func<TEntity, TColumn, string, string, int, bool>> ContainsPropertyImpl3<TEntity, TColumn>()
		{
			return (entity, column, property, search, language) => Sql.Expr<bool>($"CONTAINS(PROPERTY({column}, {Sql.ToSql(property)}), {search}, LANGUAGE {language})");
		}

		#endregion

		#endregion
	}
}
