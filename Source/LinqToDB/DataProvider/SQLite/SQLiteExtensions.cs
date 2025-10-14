using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SQLite
{
	public static class SQLiteExtensions
	{
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ext is an extension point")]
		public static ISQLiteExtensions? SQLite(this Sql.ISqlExtension? ext) => null;

		#region FTS
		/// <summary>
		/// Applies full-text search condition using MATCH predicate against whole FTS table or specific column.
		/// Examples: "table MATCH 'search query'"/"table.column MATCH 'search query'".
		/// </summary>
		/// <param name="ext">Extension point.</param>
		/// <param name="entityOrColumn">Table or column to perform full-text search against.</param>
		/// <param name="match">Full-text search condition.</param>
		/// <returns>Returns <c>true</c> if full-text search found matching records.</returns>
		/// <remarks>FTS Support: FTS3/4, FTS5.</remarks>
		[ExpressionMethod(nameof(MatchImpl1))]
		public static bool Match(this ISQLiteExtensions? ext, object? entityOrColumn, string match)
			=> throw new ServerSideOnlyException(nameof(Match));

		static Expression<Func<ISQLiteExtensions?, object?, string, bool>> MatchImpl1()
		{
			return (ext, entityOrColumn, match) => Sql.Expr<bool>($"{Sql.TableOrColumnAsField<string?>(entityOrColumn)} MATCH {match}");
		}

		/// <summary>
		/// Performs full-text search query against specified table and returns search results.
		/// Example: "table('search query')".
		/// </summary>
		/// <typeparam name="TEntity">Queried table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="table">Table to perform full-text search against.</param>
		/// <param name="match">Full-text search condition.</param>
		/// <returns>Returns table, filtered using specified search condition, applied to whole table.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(MatchTableImpl1))]
		public static IQueryable<TEntity> MatchTable<TEntity>(this ISQLiteExtensions? ext, ITable<TEntity> table, string match)
			where TEntity : class
		{
			return table.DataContext.QueryFromExpression(() => ext.MatchTable(table, match));
		}

		static Expression<Func<ISQLiteExtensions, ITable<TEntity>, string, IQueryable<TEntity>>> MatchTableImpl1<TEntity>()
			where TEntity : class
		{
			return (ext, table, match) => table.DataContext.FromSql<TEntity>($"{Sql.TableExpr(table, Sql.TableQualification.TableName)}({match})");
		}

		/// <summary>
		/// Provides access to rowid hidden column.
		/// Example: "table.rowid".
		/// </summary>
		/// <typeparam name="TEntity">Type of table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table record instance.</param>
		/// <returns>Returns rowid column value.</returns>
		[ExpressionMethod(nameof(RowIdImpl))]
		public static int RowId<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(RowId));

		static Expression<Func<ISQLiteExtensions, TEntity, int>> RowIdImpl<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<int>($"{Sql.TableField<TEntity, int>(entity, "rowid")}");
		}

		/// <summary>
		/// Provides access to FTS5 rank hidden column.
		/// Example: "table.rank".
		/// </summary>
		/// <typeparam name="TEntity">Type of table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Table record instance.</param>
		/// <returns>Returns rank column value.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(RankImpl))]
		public static double? Rank<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(Rank));

		static Expression<Func<ISQLiteExtensions, TEntity, double?>> RankImpl<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<double?>($"{Sql.TableField<TEntity, double?>(entity, "rank")}");
		}

		#region FTS3 functions
		/// <summary>
		/// FTS3/4 offsets(fts_table) function.
		/// Example: "offsets(table)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#offsets">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3OffsetsImpl))]
		public static string FTS3Offsets<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Offsets));

		static Expression<Func<ISQLiteExtensions, TEntity, string>> Fts3OffsetsImpl<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<string>($"offsets({Sql.TableAsField<TEntity, string>(entity)})");
		}

		/// <summary>
		/// FTS3/4 matchinfo(fts_table) function.
		/// Example: "matchinfo(table)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#matchinfo">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3MatchInfoImpl1))]
		public static byte[] FTS3MatchInfo<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3MatchInfo));

		static Expression<Func<ISQLiteExtensions, TEntity, byte[]>> Fts3MatchInfoImpl1<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<byte[]>($"matchinfo({Sql.TableAsField<TEntity, string>(entity)})");
		}

		/// <summary>
		/// FTS3/4 matchinfo(fts_table, format) function.
		/// Example: "matchinfo(table, 'format')".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="format">Format string function parameter.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#matchinfo">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3MatchInfoImpl2))]
		public static byte[] FTS3MatchInfo<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string format)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3MatchInfo));

		static Expression<Func<ISQLiteExtensions, TEntity, string, byte[]>> Fts3MatchInfoImpl2<TEntity>()
			where TEntity : class
		{
			return (ext, entity, format) => Sql.Expr<byte[]>($"matchinfo({Sql.TableAsField<TEntity, string>(entity)}, {format})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table) function.
		/// Example: "snippet(table)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl1))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string>> Fts3SnippetImpl1<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch) function.
		/// Example: "snippet(table, 'startMatch')".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl2))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string startMatch)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string, string>> Fts3SnippetImpl2<TEntity>()
			where TEntity : class
		{
			return (ext, entity, startMatch) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {startMatch})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch) function.
		/// Example: "snippet(table, 'startMatch', 'endMatch')".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl3))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string startMatch, string endMatch)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string, string, string>> Fts3SnippetImpl3<TEntity>()
			where TEntity : class
		{
			return (ext, entity, startMatch, endMatch) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {startMatch}, {endMatch})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses) function.
		/// Example: "snippet(table, 'startMatch', 'endMatch', 'ellipses')".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl4))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string startMatch, string endMatch, string ellipses)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string, string, string, string>> Fts3SnippetImpl4<TEntity>()
			where TEntity : class
		{
			return (ext, entity, startMatch, endMatch, ellipses) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {startMatch}, {endMatch}, {ellipses})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses, columnIndex) function.
		/// Example: "snippet(table, 'startMatch', 'endMatch', 'ellipses', columnIndex)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="columnIndex">Index of column to extract snippet from. <c>-1</c> matches all columns.</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl5))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string startMatch, string endMatch, string ellipses, int columnIndex)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string, string, string, int, string>> Fts3SnippetImpl5<TEntity>()
			where TEntity : class
		{
			return (ext, entity, startMatch, endMatch, ellipses, columnIndex) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {startMatch}, {endMatch}, {ellipses}, {columnIndex})");
		}

		/// <summary>
		/// FTS3/4 snippet(fts_table, startMatch, endMatch, ellipses, columnIndex, tokensNumber) function.
		/// Example: "snippet(table, 'startMatch', 'endMatch', 'ellipses', columnIndex, tokensNumber)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="columnIndex">Index of column to extract snippet from. <c>-1</c> matches all columns.</param>
		/// <param name="tokensNumber">Manages how many tokens should be returned (check function documentation).</param>
		/// <returns>Check <a href="https://www.sqlite.org/fts3.html#snippet">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS3/4.</remarks>
		[ExpressionMethod(nameof(Fts3SnippetImpl6))]
		public static string FTS3Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, string startMatch, string endMatch, string ellipses, int columnIndex, int tokensNumber)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS3Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, string, string, string, int, int, string>> Fts3SnippetImpl6<TEntity>()
			where TEntity : class
		{
			return (ext, entity, startMatch, endMatch, ellipses, columnIndex, tokensNumber) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {startMatch}, {endMatch}, {ellipses}, {columnIndex}, {tokensNumber})");
		}
		#endregion

		#region FTS5 functions
		/// <summary>
		/// FTS5 bm25(fts_table) function.
		/// Example: "bm25(table)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_bm25_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5bm25Impl1))]
		public static double FTS5bm25<TEntity>(this ISQLiteExtensions? ext, TEntity entity)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS5bm25));

		static Expression<Func<ISQLiteExtensions, TEntity, double>> Fts5bm25Impl1<TEntity>()
			where TEntity : class
		{
			return (ext, entity) => Sql.Expr<double>($"bm25({Sql.TableAsField<TEntity, string>(entity)})");
		}

		/// <summary>
		/// FTS5 bm25(fts_table, ...weights) function.
		/// Example: "bm25(table, col1_weight, col2_weight)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="weights">Weights for columns (each value appied to corresponding column).</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_bm25_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5bm25Impl2))]
		public static double FTS5bm25<TEntity>(this ISQLiteExtensions? ext, TEntity entity, params double[] weights)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS5bm25));

		static Expression<Func<ISQLiteExtensions, TEntity, double[], double>> Fts5bm25Impl2<TEntity>()
			where TEntity : class
		{
			return (ext, entity, weights) => Sql.Expr<double>($"bm25({Sql.TableAsField<TEntity, string>(entity)}, {Sql.Spread(weights)})");
		}

		/// <summary>
		/// FTS5 highlight(fts_table, columnIndex, startMatch, endMatch) function.
		/// Example: "highlight(table, columnIndex, 'startMatch', 'endMatch')".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="columnIndex">Index of column to extract highlighted text from.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_highlight_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5HighlightImpl))]
		public static string FTS5Highlight<TEntity>(this ISQLiteExtensions? ext, TEntity entity, int columnIndex, string startMatch, string endMatch)
			where TEntity : class
			=> throw new ServerSideOnlyException(nameof(FTS5Highlight));

		static Expression<Func<ISQLiteExtensions, TEntity, int, string, string, string>> Fts5HighlightImpl<TEntity>()
			where TEntity : class
		{
			return (ext, entity, columnIndex, startMatch, endMatch) => Sql.Expr<string>($"highlight({Sql.TableAsField<TEntity, string>(entity)}, {columnIndex}, {startMatch}, {endMatch})");
		}

		/// <summary>
		/// FTS5 snippet(fts_table, columnIndex, startMatch, endMatch, ellipses, tokensNumber) function.
		/// Example: "snippet(table, columnIndex, 'startMatch', 'endMatch', 'ellipses', tokensNumber)".
		/// </summary>
		/// <typeparam name="TEntity">Full-text search table mapping class.</typeparam>
		/// <param name="ext">Extension point.</param>
		/// <param name="entity">Full-text search table.</param>
		/// <param name="columnIndex">Index of column to extract snippet from.</param>
		/// <param name="startMatch">Start match wrap text.</param>
		/// <param name="endMatch">End match wrap text.</param>
		/// <param name="ellipses">Ellipses text.</param>
		/// <param name="tokensNumber">Manages how many tokens should be returned (check function documentation).</param>
		/// <returns>Check <a href="https://sqlite.org/fts5.html#the_snippet_function">documentation of SQLite site</a>.</returns>
		/// <remarks>FTS Support: FTS5.</remarks>
		[ExpressionMethod(nameof(Fts5SnippetImpl))]
		public static string FTS5Snippet<TEntity>(this ISQLiteExtensions? ext, TEntity entity, int columnIndex, string startMatch, string endMatch, string ellipses, int tokensNumber)
			where TEntity : class
		=> throw new ServerSideOnlyException(nameof(FTS5Snippet));

		static Expression<Func<ISQLiteExtensions, TEntity, int, string, string, string, int, string>> Fts5SnippetImpl<TEntity>()
			where TEntity : class
		{
			return (ext, entity, columnIndex, startMatch, endMatch, ellipses, tokensNumber) => Sql.Expr<string>($"snippet({Sql.TableAsField<TEntity, string>(entity)}, {columnIndex}, {startMatch}, {endMatch}, {ellipses}, {tokensNumber})");
		}
		#endregion

		#region FTS3 commands
		/// <summary>
		/// Executes FTS3/FTS4 'optimize' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('optimize')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS3Optimize<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('optimize')");
		}

		/// <summary>
		/// Executes FTS3/FTS4 'optimize' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('optimize')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS3OptimizeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('optimize')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS3/FTS4 'rebuild' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('rebuild')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS3Rebuild<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('rebuild')");
		}

		/// <summary>
		/// Executes FTS3/FTS4 'rebuild' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('rebuild')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS3RebuildAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('rebuild')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS3/FTS4 'integrity-check' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('integrity-check')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS3IntegrityCheck<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('integrity-check')");
		}

		/// <summary>
		/// Executes FTS3/FTS4 'integrity-check' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('integrity-check')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS3IntegrityCheckAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('integrity-check')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS3/FTS4 'merge' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('merge=blocks,segments')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="blocks">Blocks command parameter.</param>
		/// <param name="segments">Segments command parameter.</param>
		public static void FTS3Merge<TEntity>(this IDataContext dc, ITable<TEntity> table, int blocks, int segments)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('merge={blocks.ToString(NumberFormatInfo.InvariantInfo)},{segments.ToString(NumberFormatInfo.InvariantInfo)}')");
		}

		/// <summary>
		/// Executes FTS3/FTS4 'merge' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('merge=blocks,segments')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="blocks">Blocks command parameter.</param>
		/// <param name="segments">Segments command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS3MergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int blocks, int segments, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('merge={blocks.ToString(NumberFormatInfo.InvariantInfo)},{segments.ToString(NumberFormatInfo.InvariantInfo)}')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS3/FTS4 'automerge' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('automerge=segments')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="segments">Segments command parameter.</param>
		public static void FTS3AutoMerge<TEntity>(this IDataContext dc, ITable<TEntity> table, int segments)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('automerge={segments.ToString(NumberFormatInfo.InvariantInfo)}')");
		}

		/// <summary>
		/// Executes FTS3/FTS4 'automerge' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('automerge=segments')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="segments">Segments command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS3AutoMergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int segments, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('automerge={segments.ToString(NumberFormatInfo.InvariantInfo)}')", cancellationToken)
				.ConfigureAwait(false);
		}
		#endregion

		#region FTS5 commands
		/// <summary>
		/// Executes FTS5 'automerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('automerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		public static void FTS5AutoMerge<TEntity>(this IDataContext dc, ITable<TEntity> table, int value)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('automerge', {value.ToString(NumberFormatInfo.InvariantInfo)})");
		}

		/// <summary>
		/// Executes FTS5 'automerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('automerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5AutoMergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int value, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('automerge', {value.ToString(NumberFormatInfo.InvariantInfo)})", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'crisismerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('crisismerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		public static void FTS5CrisisMerge<TEntity>(this IDataContext dc, ITable<TEntity> table, int value)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('crisismerge', {value.ToString(NumberFormatInfo.InvariantInfo)})");
		}

		/// <summary>
		/// Executes FTS5 'crisismerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('crisismerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5CrisisMergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int value, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('crisismerge', {value.ToString(NumberFormatInfo.InvariantInfo)})", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'delete' command for specific table.
		/// Example: "INSERT INTO table(table, rowid, col1, col2) VALUES('delete', rowid, 'col1_value', 'col2_value')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="rowid">Record rowid value.</param>
		/// <param name="record">Current record entity.</param>
		public static void FTS5Delete<TEntity>(this IDataContext dc, ITable<TEntity> table, int rowid, TEntity record)
			where TEntity : class
		{
			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(TEntity), dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			var columns = new string[ed.Columns.Count];
			var parameterTokens = new string[ed.Columns.Count];
			var parameters = new DataParameter[ed.Columns.Count];

			var sqlBuilder = dc.GetDataProvider().CreateSqlBuilder(dc.MappingSchema, dc.Options);

			for (var i = 0; i < ed.Columns.Count; i++)
			{
				columns[i]         = sqlBuilder.ConvertInline(ed.Columns[i].ColumnName, ConvertType.NameToQueryField);
				parameterTokens[i] = FormattableString.Invariant($"@p{i}");
				parameters[i]      = DataParameter.VarChar(FormattableString.Invariant($"@p{i}"), (string)ed.Columns[i].GetProviderValue(record)!);
			}

			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rowid, {string.Join(", ", columns)}) VALUES('delete', {rowid.ToString(NumberFormatInfo.InvariantInfo)}, {string.Join(", ", parameterTokens)})", parameters);
		}

		/// <summary>
		/// Executes FTS5 'delete' command for specific table.
		/// Example: "INSERT INTO table(table, rowid, col1, col2) VALUES('delete', rowid, 'col1_value', 'col2_value')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="rowid">Record rowid value.</param>
		/// <param name="record">Current record entity.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5DeleteAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int rowid, TEntity record, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(TEntity), dc.Options.ConnectionOptions.OnEntityDescriptorCreated);

			var columns = new string[ed.Columns.Count];
			var parameterTokens = new string[ed.Columns.Count];
			var parameters = new DataParameter[ed.Columns.Count];

			var sqlBuilder = dc.GetDataProvider().CreateSqlBuilder(dc.MappingSchema, dc.Options);

			for (var i = 0; i < ed.Columns.Count; i++)
			{
				columns[i] = sqlBuilder.ConvertInline(ed.Columns[i].ColumnName, ConvertType.NameToQueryField);
				parameterTokens[i] = FormattableString.Invariant($"@p{i}");
				parameters[i] = DataParameter.VarChar(FormattableString.Invariant($"@p{i}"), (string)ed.Columns[i].GetProviderValue(record)!);
			}

			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rowid, {string.Join(", ", columns)}) VALUES('delete', {rowid.ToString(NumberFormatInfo.InvariantInfo)}, {string.Join(", ", parameterTokens)})", parameters, cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'delete-all' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('delete-all')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS5DeleteAll<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('delete-all')");
		}

		/// <summary>
		/// Executes FTS5 'delete-all' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('delete-all')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5DeleteAllAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('delete-all')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'integrity-check' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('integrity-check')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS5IntegrityCheck<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('integrity-check')");
		}

		/// <summary>
		/// Executes FTS5 'integrity-check' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('integrity-check')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5IntegrityCheckAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('integrity-check')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'merge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('merge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		public static void FTS5Merge<TEntity>(this IDataContext dc, ITable<TEntity> table, int value)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('merge', {value.ToString(NumberFormatInfo.InvariantInfo)})");
		}

		/// <summary>
		/// Executes FTS5 'merge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('merge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5MergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int value, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('merge', {value.ToString(NumberFormatInfo.InvariantInfo)})", cancellationToken)
			.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'optimize' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('optimize')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS5Optimize<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('optimize')");
		}

		/// <summary>
		/// Executes FTS5 'optimize' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('optimize')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5OptimizeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('optimize')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'pgsz' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('pgsz', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		public static void FTS5Pgsz<TEntity>(this IDataContext dc, ITable<TEntity> table, int value)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('pgsz', {value.ToString(NumberFormatInfo.InvariantInfo)})");
		}

		/// <summary>
		/// Executes FTS5 'pgsz' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('pgsz', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5PgszAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int value, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('pgsz', {value.ToString(NumberFormatInfo.InvariantInfo)})", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'rank' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('rank', 'function')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="function">Rank function.</param>
		public static void FTS5Rank<TEntity>(this IDataContext dc, ITable<TEntity> table, string function)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('rank', @rank)", DataParameter.VarChar("@rank", function));
		}

		/// <summary>
		/// Executes FTS5 'rank' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('rank', 'function')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="function">Rank function.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5RankAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, string function, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('rank', @rank)", DataParameter.VarChar("@rank", function), cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'rebuild' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('rebuild')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		public static void FTS5Rebuild<TEntity>(this IDataContext dc, ITable<TEntity> table)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('rebuild')");
		}

		/// <summary>
		/// Executes FTS5 'rebuild' command for specific table.
		/// Example: "INSERT INTO table(table) VALUES('rebuild')".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5RebuildAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}) VALUES('rebuild')", cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Executes FTS5 'usermerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('usermerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		public static void FTS5UserMerge<TEntity>(this IDataContext dc, ITable<TEntity> table, int value)
			where TEntity : class
		{
			dc.Execute($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('usermerge', {value.ToString(NumberFormatInfo.InvariantInfo)})");
		}

		/// <summary>
		/// Executes FTS5 'usermerge' command for specific table.
		/// Example: "INSERT INTO table(table, rank) VALUES('usermerge', value)".
		/// </summary>
		/// <typeparam name="TEntity">Table mapping class.</typeparam>
		/// <param name="dc"><see cref="IDataContext"/> instance.</param>
		/// <param name="table">FTS table.</param>
		/// <param name="value">Command parameter.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public static async Task FTS5UserMergeAsync<TEntity>(this IDataContext dc, ITable<TEntity> table, int value, CancellationToken cancellationToken = default)
			where TEntity : class
		{
			await dc.ExecuteAsync($"INSERT INTO {Sql.TableName(table)}({Sql.TableName(table, Sql.TableQualification.TableName)}, rank) VALUES('usermerge', {value.ToString(NumberFormatInfo.InvariantInfo)})", cancellationToken)
				.ConfigureAwait(false);
		}

		#endregion

		#endregion
	}
}
