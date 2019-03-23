using LinqToDB.Linq;
using LinqToDB.Mapping;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SQLite
{
	public static class SQLiteExtensions
	{
		#region FTS
		/// <summary>
		/// Applies full-text search condition using MATCH predicate against whole FTS table.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="match">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		/// <remarks>FTS Support: FTS3/4, FTS5.</remarks>
		[ExpressionMethod(nameof(MatchImpl1))]
		public static bool Match<TEntity>(this TEntity entity, string match)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, bool>> MatchImpl1<TEntity>()
		{
			return (entity, match) => Sql.Expr<bool>($"{Sql.TableAsField(entity)} MATCH {match}");
		}

		/// <summary>
		/// Applies full-text search condition using MATCH predicate against specific FTS table column.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <typeparam name="TColumn">Type of queried full-text search column.</typeparam>
		/// <param name="entity">Table to perform full-text search against.</param>
		/// <param name="column">Full-text column that should be queried.</param>
		/// <param name="match">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		/// <remarks>FTS Support: FTS3/4, FTS5.</remarks>
		[ExpressionMethod(nameof(MatchImpl2))]
		public static bool Match<TEntity, TColumn>(this TEntity entity, TColumn column, string match)
		{
			throw new LinqException($"'{nameof(Match)}' is server-side method.");
		}

		static Expression<Func<TEntity, TColumn, string, bool>> MatchImpl2<TEntity, TColumn>()
		{
			return (entity, column, match) => Sql.Expr<bool>($"{column} MATCH {match}");
		}

		/// <summary>
		/// Performs full-text search query against against speficied table and returns search results.
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="table">Table to perform full-text search against.</param>
		/// <param name="match">Full-text search condition.</param>
		/// <returns>Returns table, filtered using specified search condition, applied to whole table.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(MatchTableImpl1))]
		public static IQueryable<TEntity> MatchTable<TEntity>(this ITable<TEntity> table, string match)
		{
			return table.DataContext.FromSql<TEntity>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}({match})");
		}

		static Expression<Func<ITable<TEntity>, string, IQueryable<TEntity>>> MatchTableImpl1<TEntity>()
		{
			return (table, match) => table.DataContext.FromSql<TEntity>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}({match})");
		}

		/// <summary>
		/// Provides access to rowid hidden column.
		/// </summary>
		/// <typeparam name="TEntity">Type of table mapping class.</typeparam>
		/// <param name="entity">Table record instance.</param>
		/// <returns>Returns rowid column value.</returns>
		[ExpressionMethod(nameof(RowIdImpl))]
		public static int RowId<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(RowId)}' is server-side method.");
		}

		static Expression<Func<TEntity, int>> RowIdImpl<TEntity>()
		{
			return entity => Sql.Expr<int>($"{Sql.TableField(entity, "rowid")}");
		}

		/// <summary>
		/// Provides access to FTS5 rank hidden column.
		/// </summary>
		/// <typeparam name="TEntity">Type of table mapping class.</typeparam>
		/// <param name="entity">Table record instance.</param>
		/// <returns>Returns rank column value.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(RankImpl))]
		public static double? Rank<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(Rank)}' is server-side method.");
		}

		static Expression<Func<TEntity, double?>> RankImpl<TEntity>()
		{
			return entity => Sql.Expr<double?>($"{Sql.TableField(entity, "rank")}");
		}

		/// <summary>
		/// FTS3/4 offsets(fts_table) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#offsets">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3OffsetsImpl))]
		public static string Fts3Offsets<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(Fts3Offsets)}' is server-side method.");
		}

		static Expression<Func<TEntity, string>> Fts3OffsetsImpl<TEntity>()
		{
			return entity => Sql.Expr<string>($"offsets({Sql.TableAsField(entity)})");
		}

		/// <summary>
		/// FTS3/4 matchinfo(fts_table) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#matchinfo">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3MatchInfoImpl1))]
		public static byte[] Fts3MatchInfo<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(Fts3MatchInfo)}' is server-side method.");
		}

		static Expression<Func<TEntity, byte[]>> Fts3MatchInfoImpl1<TEntity>()
		{
			return entity => Sql.Expr<byte[]>($"matchinfo({Sql.TableAsField(entity)})");
		}

		/// <summary>
		/// FTS3/4 matchinfo(fts_table, format) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="format">Format string function parameter.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#matchinfo">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3MatchInfoImpl2))]
		public static byte[] Fts3MatchInfo<TEntity>(this TEntity entity, string format)
		{
			throw new LinqException($"'{nameof(Fts3MatchInfo)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, byte[]>> Fts3MatchInfoImpl2<TEntity>()
		{
			return (entity, format) => Sql.Expr<byte[]>($"matchinfo({Sql.TableAsField(entity)}, {format})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl1))]
		public static string Fts3Snippet<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string>> Fts3SnippetImpl1<TEntity>()
		{
			return entity => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl2))]
		public static string Fts3Snippet<TEntity>(this TEntity entity, string startMatch)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, string>> Fts3SnippetImpl2<TEntity>()
		{
			return (entity, startMatch) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {startMatch})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl3))]
		public static string Fts3Snippet<TEntity>(this TEntity entity, string startMatch, string endMatch)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, string, string>> Fts3SnippetImpl3<TEntity>()
		{
			return (entity, startMatch, endMatch) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {startMatch}, {endMatch})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl4))]
		public static string Fts3Snippet<TEntity>(this TEntity entity, string startMatch, string endMatch, string ellipses)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, string, string, string>> Fts3SnippetImpl4<TEntity>()
		{
			return (entity, startMatch, endMatch, ellipses) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {startMatch}, {endMatch}, {ellipses})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses, columnIndex) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="columnIndex">Index of column to extract snippet from. <c>-1</c> matches all columns.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl5))]
		public static string Fts3Snippet<TEntity>(this TEntity entity, string startMatch, string endMatch, string ellipses, int columnIndex)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, string, string, int, string>> Fts3SnippetImpl5<TEntity>()
		{
			return (entity, startMatch, endMatch, ellipses, columnIndex) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {startMatch}, {endMatch}, {ellipses}, {columnIndex})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses, columnIndex, tokensNumber) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="columnIndex">Index of column to extract snippet from. <c>-1</c> matches all columns.</param>
		/// <param name="tokensNumber">Manages how many tokens should be returned (check function documentation).</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl6))]
		public static string Fts3Snippet<TEntity>(this TEntity entity, string startMatch, string endMatch, string ellipses, int columnIndex, int tokensNumber)
		{
			throw new LinqException($"'{nameof(Fts3Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, string, string, string, int, int, string>> Fts3SnippetImpl6<TEntity>()
		{
			return (entity, startMatch, endMatch, ellipses, columnIndex, tokensNumber) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {startMatch}, {endMatch}, {ellipses}, {columnIndex}, {tokensNumber})");
		}


		/// <summary>
		/// FTS5 bm25(fts_table) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_bm25_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5bm25Impl1))]
		public static double Fts5bm25<TEntity>(this TEntity entity)
		{
			throw new LinqException($"'{nameof(Fts5bm25)}' is server-side method.");
		}

		static Expression<Func<TEntity, double>> Fts5bm25Impl1<TEntity>()
		{
			return entity => Sql.Expr<double>($"bm25({Sql.TableAsField(entity)})");
		}

		/// <summary>
		/// FTS5 bm25(fts_table, ...weights) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="weights">Weights for columns (each value appied to corresponding column).</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_bm25_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5bm25Impl2))]
		public static double Fts5bm25<TEntity>(this TEntity entity, params double[] weights)
		{
			throw new LinqException($"'{nameof(Fts5bm25)}' is server-side method.");
		}

		static Expression<Func<TEntity, double[], double>> Fts5bm25Impl2<TEntity>()
		{
			return (entity, weights) => Sql.Expr<double>($"bm25({Sql.TableAsField(entity)}, {Sql.Spread(weights)})");
		}


		/// <summary>
		/// FTS5 highlight(fts_table, columnIndex, startMatch, endMatch) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="columnIndex">Index of column to extract highlighted text from.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_highlight_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5HighlightImpl))]
		public static string Fts5Highlight<TEntity>(this TEntity entity, int columnIndex, string startMatch, string endMatch)
		{
			throw new LinqException($"'{nameof(Fts5bm25)}' is server-side method.");
		}

		static Expression<Func<TEntity, int, string, string, string>> Fts5HighlightImpl<TEntity>()
		{
			return (entity, columnIndex, startMatch, endMatch) => Sql.Expr<string>($"highlight({Sql.TableAsField(entity)}, {columnIndex}, {startMatch}, {endMatch})");
		}

		/// <summary>
		/// FTS5 snippet(fts_table, columnIndex, startMatch, endMatch, ellipses, tokensNumber) function.
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="columnIndex">Index of column to extract snippet from.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="tokensNumber">Manages how many tokens should be returned (check function documentation).</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_snippet_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5SnippetImpl))]
		public static string Fts5Snippet<TEntity>(this TEntity entity, int columnIndex, string startMatch, string endMatch, string ellipses, int tokensNumber)
		{
			throw new LinqException($"'{nameof(Fts5Snippet)}' is server-side method.");
		}

		static Expression<Func<TEntity, int, string, string, string, int, string>> Fts5SnippetImpl<TEntity>()
		{
			return (entity, columnIndex, startMatch, endMatch, ellipses, tokensNumber) => Sql.Expr<string>($"snippet({Sql.TableAsField(entity)}, {columnIndex}, {startMatch}, {endMatch}, {ellipses}, {tokensNumber})");
		}

		public static void Fts3CommandOptimize<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "optimize").Insert();
		}

		public static void Fts3CommandRebuild<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "rebuild").Insert();
		}

		public static void Fts3CommandIntegrityCheck<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "integrity-check").Insert();
		}

		public static void Fts3CommandMerge<TEntity>(this ITable<TEntity> table, int blocks, int segments)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => Sql.Expr<string>($"merge={blocks},{segments}")).Insert();
		}

		public static void Fts3CommandAutoMerge<TEntity>(this ITable<TEntity> table, int segments)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => Sql.Expr<string>($"automerge={segments}")).Insert();
		}

		public static void Fts5CommandAutoMerge<TEntity>(this ITable<TEntity> table, int value)
		{
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "automerge")
				.Value(_ => _.Rank(), () => value)
				.Insert();
		}

		public static void Fts5CommandCrisisMerge<TEntity>(this ITable<TEntity> table, int value)
		{
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "crisismerge")
				.Value(_ => _.Rank(), () => value)
				.Insert();
		}

		public static void Fts5CommandDelete<TEntity>(this ITable<TEntity> table, int rowid)
		{
			// TODO: columns
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "delete")
				.Value(_ => _.RowId(), () => rowid)
				.Insert();
		}

		public static void Fts5CommandDeleteAll<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "delete-all").Insert();
		}

		public static void Fts5CommandIntegrityCheck<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "integrity-check").Insert();
		}

		public static void Fts5CommandMerge<TEntity>(this ITable<TEntity> table, int value)
		{
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "merge")
				.Value(_ => _.Rank(), () => value)
				.Insert();
		}

		public static void Fts5CommandOptimize<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "optimize").Insert();
		}

		public static void Fts5CommandPgsz<TEntity>(this ITable<TEntity> table, int value)
		{
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "pgsz")
				.Value(_ => _.Rank(), () => value)
				.Insert();
		}

		//public static void Fts5CommandRank<TEntity>(this ITable<TEntity> table, string value)
		//{
		//	table
		//		.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "rank")
		//		.Value(_ => _.Rank(), () => value)
		//		.Insert();
		//}

		public static void Fts5CommandRebuild<TEntity>(this ITable<TEntity> table)
		{
			table.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "rebuild").Insert();
		}

		public static void Fts5CommandUserMerge<TEntity>(this ITable<TEntity> table, int value)
		{
			table
				.Value(_ => Sql.Expr<string>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}"), () => "usermerge")
				.Value(_ => _.Rank(), () => value)
				.Insert();
		}
		#endregion
	}
}
