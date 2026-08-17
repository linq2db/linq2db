using LinqToDB;
using LinqToDB.Data;

namespace Tests
{
	public static class TestExtensions
	{
		/// <summary>
		/// Use this extension to disable generation of aditional null checks on join condition for ClickHouse and YDB for nullable keys.
		/// </summary>
		public static DataOptions OmitUnsupportedCompareNulls(this DataOptions options, string context)
		{
			return options.UseCompareNulls(context.IsAnyOf(TestProvName.AllClickHouse, TestProvName.AllYdb) ? CompareNulls.LikeSql : CompareNulls.LikeClr);
		}

		/// <summary>
		/// Whether multi-step <b>DML</b> on this context runs as a single combined command — an identity insert as
		/// <c>INSERT; SELECT last_insert_rowid()</c> rather than two round-trips. Use it to pick expectations instead of
		/// hard-coding either shape: the difference is observable in round-trip counts and in which
		/// <c>ICommandInterceptor</c> callbacks fire (a combined insert reports through the reader, so ExecuteScalar and
		/// ExecuteNonQuery are never called).
		/// <para>
		/// Deliberately does NOT require the context to be a <see cref="DataConnection"/>: a <see cref="DataContext"/> is
		/// not one but owns one, and the DML interpreter runs on that inner connection — so combining does happen there.
		/// Getting this wrong is exactly what made <c>DataContext_ExecuteScalar</c> fail.
		/// </para>
		/// </summary>
		public static bool UsesCombinedCommands(this IDataContext db)
		{
			return db.Options.LinqOptions.UseCombinedCommands
				&& db.SqlProviderFlags.IsMultiStatementBatchSupported
				&& db.SqlProviderFlags.IsMultipleResultSetsSupported;
		}

		/// <summary>
		/// Whether <b>eager loading</b> on this context collapses a main query and its child collections into one combined
		/// multi-result-set command.
		/// <para>
		/// Stricter than <see cref="UsesCombinedCommands"/>: it additionally requires the context itself to be a
		/// <see cref="DataConnection"/>, because the combined eager executor is built on one — a
		/// <see cref="DataContext"/> or a remote context loads sequentially however the option is set.
		/// </para>
		/// </summary>
		public static bool UsesCombinedEagerLoading(this IDataContext db)
		{
			return db.UsesCombinedCommands() && db is DataConnection;
		}
	}
}
