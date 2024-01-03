using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	/// <summary>
	/// Collects LinqToDB call statistics.
	/// </summary>
	public static class ActivityStatistics
	{
		static ActivityStatistics()
		{
			All =
			[
				QueryProviderExecuteT               = new("IQueryProvider.Execute<T>"),
				QueryProviderExecute                = new("IQueryProvider.Execute"),
				QueryProviderGetEnumeratorT         = new("IQueryProvider.GetEnumerator<T>"),
				QueryProviderGetEnumerator          = new("IQueryProvider.GetEnumerator"),
				GetQueryTotal                       = new("  GetQuery"),
				GetQueryFind                        = new("    Find"),
				GetQueryFindExpose                  = new("      Expose"),
				GetQueryFindFind                    = new("      Find"),
				GetQueryCreate                      = new("    Create"),
				Build                               = new("      Build"),
				BuildSequence                       = new("        BuildSequence"),
				BuildSequenceCanBuild               = new("          CanBuild"),
				BuildSequenceBuild                  = new("          Build"),
				ReorderBuilders                     = new("        ReorderBuilders"),
				BuildQuery                          = new("        BuildQuery"),
				FinalizeQuery                       = new("          FinalizeQuery"),
				GetIEnumerable                      = new("  GetIEnumerable"),

				ExecuteTotal                        = new("Execute",
					ExecuteQuery                    = new("  Execute Query"),
					ExecuteQueryAsync               = new("  Execute Query Async"),
					ExecuteElement                  = new("  Execute Element"),
					ExecuteElementAsync             = new("  Execute Element Async"),
					ExecuteScalar                   = new("  Execute Scalar"),
					ExecuteScalarAsync              = new("  Execute Scalar Async"),
					ExecuteScalarAlternative        = new("  Execute Scalar Alternative"),
					ExecuteScalarAlternativeAsync   = new("  Execute Scalar Alternative Async"),
					ExecuteNonQuery                 = new("  Execute NonQuery"),
					ExecuteNonQueryAsync            = new("  Execute NonQuery Async"),
					ExecuteNonQueryAlternative      = new("  Execute NonQuery Alternative"),
					ExecuteNonQueryAlternativeAsync = new("  Execute NonQuery Alternative Async"),

					CommandInfoExecute              = new("  SQL Execute"),
					CommandInfoExecuteAsync         = new("  SQL ExecuteAsync"),
					CommandInfoExecuteT             = new("  SQL Execute<T>"),
					CommandInfoExecuteAsyncT        = new("  SQL ExecuteAsync<T>"),
					CommandInfoExecuteCustom        = new("  SQL ExecuteCustom"),

					CreateTable                     = new("  CreateTable"),
					CreateTableAsync                = new("  CreateTable Async"),
					DropTable                       = new("  DropTable"),
					DropTableAsync                  = new("  DropTable Async"),
					DeleteObject                    = new("  Delete Object"),
					DeleteObjectAsync               = new("  Delete Object Async"),
					InsertObject                    = new("  Insert Object"),
					InsertObjectAsync               = new("  Insert Object Async"),
					InsertOrReplaceObject           = new("  InsertOrReplace Object"),
					InsertOrReplaceObjectAsync      = new("  InsertOrReplace Object Async"),
					InsertWithIdentityObject        = new("  InsertWithIdentity Object"),
					InsertWithIdentityObjectAsync   = new("  InsertWithIdentity Object Async"),
					UpdateObject                    = new("  Update Object"),
					UpdateObjectAsync               = new("  Update Object Async"),
					BulkCopy                        = new("  BulkCopy"),
					BulkCopyAsync                   = new("  BulkCopy Async")
				),
				ExecuteQuery,
				ExecuteQueryAsync,
				ExecuteElement,
				ExecuteElementAsync,
				ExecuteScalar,
				ExecuteScalarAsync,
				ExecuteScalarAlternative,
				ExecuteScalarAlternativeAsync,
				ExecuteNonQuery,
				ExecuteNonQueryAsync,
				ExecuteNonQueryAlternative,
				ExecuteNonQueryAlternativeAsync,

				CreateTable,
				CreateTableAsync,
				DropTable,
				DropTableAsync,
				DeleteObject,
				DeleteObjectAsync,
				InsertObject,
				InsertObjectAsync,
				InsertOrReplaceObject,
				InsertOrReplaceObjectAsync,
				InsertWithIdentityObject,
				InsertWithIdentityObjectAsync,
				UpdateObject,
				UpdateObjectAsync,
				BulkCopy,
				BulkCopyAsync,

				BuildSql                            = new("    BuildSql"),

				CommandInfoExecute,
				CommandInfoExecuteT,
				CommandInfoExecuteCustom,
				CommandInfoExecuteAsync,
				CommandInfoExecuteAsyncT,

				ExecuteAdo                          = new("    ADO.NET",
					ConnectionOpen                  = new("      Connection Open"),
					ConnectionOpenAsync             = new("      Connection OpenAsync"),
					ConnectionClose                 = new("      Connection Close"),
					ConnectionCloseAsync            = new("      Connection CloseAsync"),
					ConnectionDispose               = new("      Connection Dispose"),
					ConnectionDisposeAsync          = new("      Connection DisposeAsync"),
					ConnectionBeginTransaction      = new("      Connection BeginTransaction"),
					ConnectionBeginTransactionAsync = new("      Connection BeginTransactionAsync"),
					TransactionCommit               = new("      Transaction Commit"),
					TransactionCommitAsync          = new("      Transaction CommitAsync"),
					TransactionRollback             = new("      Transaction Rollback"),
					TransactionRollbackAsync        = new("      Transaction RollbackAsync"),
					TransactionDispose              = new("      Transaction Dispose"),
					TransactionDisposeAsync         = new("      Transaction DisposeAsync"),
					CommandExecuteScalar            = new("      Command ExecuteScalar"),
					CommandExecuteScalarAsync       = new("      Command ExecuteScalarAsync"),
					CommandExecuteReader            = new("      Command ExecuteReader"),
					CommandExecuteReaderAsync       = new("      Command ExecuteReaderAsync"),
					CommandExecuteNonQuery          = new("      Command ExecuteNonQuery"),
					CommandExecuteNonQueryAsync     = new("      Command ExecuteNonQueryAsync")
				),
				ConnectionOpen,
				ConnectionOpenAsync,
				ConnectionClose,
				ConnectionCloseAsync,
				ConnectionDispose,
				ConnectionDisposeAsync,
				ConnectionBeginTransaction,
				ConnectionBeginTransactionAsync,
				TransactionCommit,
				TransactionCommitAsync,
				TransactionRollback,
				TransactionRollbackAsync,
				TransactionDispose,
				TransactionDisposeAsync,
				CommandExecuteScalar,
				CommandExecuteScalarAsync,
				CommandExecuteReader,
				CommandExecuteReaderAsync,
				CommandExecuteNonQuery,
				CommandExecuteNonQueryAsync,

				OnTraceInternal                     = new("    OnTraceInternal"),
				Materialization                     = new("    Materialization"),

				GetSqlText                          = new("  GetSqlText"),

				// Placeholder for Total, must be last, do not remove or change position!
				//
				null!
			];

			All[^1] = Total = new ("Total",
				new []
				{
					QueryProviderExecuteT,
					QueryProviderExecute,
					QueryProviderGetEnumeratorT,
					QueryProviderGetEnumerator
				}
				.Concat(ExecuteTotal.Metrics).ToArray());
		}

		internal static StatActivity GetStat(ActivityID metric)
		{
			return metric switch
			{
				ActivityID.QueryProviderExecuteT           => QueryProviderExecuteT,
				ActivityID.QueryProviderExecute            => QueryProviderExecute,
				ActivityID.QueryProviderGetEnumeratorT     => QueryProviderGetEnumeratorT,
				ActivityID.QueryProviderGetEnumerator      => QueryProviderGetEnumerator,
				ActivityID.GetQueryTotal                   => GetQueryTotal,
				ActivityID.GetQueryFind                    => GetQueryFind,
				ActivityID.GetQueryFindExpose              => GetQueryFindExpose,
				ActivityID.GetQueryFindFind                => GetQueryFindFind,
				ActivityID.GetQueryCreate                  => GetQueryCreate,
				ActivityID.Build                           => Build,
				ActivityID.BuildSequence                   => BuildSequence,
				ActivityID.BuildSequenceCanBuild           => BuildSequenceCanBuild,
				ActivityID.BuildSequenceBuild              => BuildSequenceBuild,
				ActivityID.ReorderBuilders                 => ReorderBuilders,
				ActivityID.BuildQuery                      => BuildQuery,
				ActivityID.FinalizeQuery                   => FinalizeQuery,
				ActivityID.GetIEnumerable                  => GetIEnumerable,
				ActivityID.ExecuteQuery                    => ExecuteQuery,
				ActivityID.ExecuteQueryAsync               => ExecuteQueryAsync,
				ActivityID.ExecuteElement                  => ExecuteElement,
				ActivityID.ExecuteElementAsync             => ExecuteElementAsync,
				ActivityID.ExecuteScalar                   => ExecuteScalar,
				ActivityID.ExecuteScalarAsync              => ExecuteScalarAsync,
				ActivityID.ExecuteScalarAlternative        => ExecuteScalarAlternative,
				ActivityID.ExecuteScalarAlternativeAsync   => ExecuteScalarAlternativeAsync,
				ActivityID.ExecuteNonQuery                 => ExecuteNonQuery,
				ActivityID.ExecuteNonQueryAsync            => ExecuteNonQueryAsync,
				ActivityID.ExecuteNonQuery2                => ExecuteNonQueryAlternative,
				ActivityID.ExecuteNonQuery2Async           => ExecuteNonQueryAlternativeAsync,

				ActivityID.CreateTable                     => CreateTable,
				ActivityID.CreateTableAsync                => CreateTableAsync,
				ActivityID.DropTable                       => DropTable,
				ActivityID.DropTableAsync                  => DropTableAsync,
				ActivityID.DeleteObject                    => DeleteObject,
				ActivityID.DeleteObjectAsync               => DeleteObjectAsync,
				ActivityID.InsertObject                    => InsertObject,
				ActivityID.InsertObjectAsync               => InsertObjectAsync,
				ActivityID.InsertOrReplaceObject           => InsertOrReplaceObject,
				ActivityID.InsertOrReplaceObjectAsync      => InsertOrReplaceObjectAsync,
				ActivityID.InsertWithIdentityObject        => InsertWithIdentityObject,
				ActivityID.InsertWithIdentityObjectAsync   => InsertWithIdentityObjectAsync,
				ActivityID.UpdateObject                    => UpdateObject,
				ActivityID.UpdateObjectAsync               => UpdateObjectAsync,
				ActivityID.BulkCopy                        => BulkCopy,
				ActivityID.BulkCopyAsync                   => BulkCopyAsync,

				ActivityID.BuildSql                        => BuildSql,

				ActivityID.ConnectionOpen                  => ConnectionOpen,
				ActivityID.ConnectionOpenAsync             => ConnectionOpenAsync,
				ActivityID.ConnectionClose                 => ConnectionClose,
				ActivityID.ConnectionCloseAsync            => ConnectionCloseAsync,
				ActivityID.ConnectionDispose               => ConnectionDispose,
				ActivityID.ConnectionDisposeAsync          => ConnectionDisposeAsync,
				ActivityID.ConnectionBeginTransaction      => ConnectionBeginTransaction,
				ActivityID.ConnectionBeginTransactionAsync => ConnectionBeginTransactionAsync,
				ActivityID.TransactionCommit               => TransactionCommit,
				ActivityID.TransactionCommitAsync          => TransactionCommitAsync,
				ActivityID.TransactionRollback             => TransactionRollback,
				ActivityID.TransactionRollbackAsync        => TransactionRollbackAsync,
				ActivityID.TransactionDispose              => TransactionDispose,
				ActivityID.TransactionDisposeAsync         => TransactionDisposeAsync,
				ActivityID.CommandExecuteScalar            => CommandExecuteScalar,
				ActivityID.CommandExecuteScalarAsync       => CommandExecuteScalarAsync,
				ActivityID.CommandExecuteReader            => CommandExecuteReader,
				ActivityID.CommandExecuteReaderAsync       => CommandExecuteReaderAsync,
				ActivityID.CommandExecuteNonQuery          => CommandExecuteNonQuery,
				ActivityID.CommandExecuteNonQueryAsync     => CommandExecuteNonQueryAsync,
				ActivityID.CommandInfoExecute              => CommandInfoExecute,
				ActivityID.CommandInfoExecuteT             => CommandInfoExecuteT,
				ActivityID.CommandInfoExecuteCustom        => CommandInfoExecuteCustom,
				ActivityID.CommandInfoExecuteAsync         => CommandInfoExecuteAsync,
				ActivityID.CommandInfoExecuteAsyncT        => CommandInfoExecuteAsyncT,
				ActivityID.GetSqlText                      => GetSqlText,
				ActivityID.OnTraceInternal                 => OnTraceInternal,
				ActivityID.Materialization                 => Materialization,

				_ => throw new InvalidOperationException($"Unknown metric type {metric}")
			};
		}

		/// <summary>
		/// <para>
		/// Creates an instance of the <see cref="IActivity"/> class for the specified metric.
		/// Can be used in a factory method for <see cref="ActivityService"/>:
		/// </para>
		/// <code>
		/// ActivityService.AddFactory(ActivityStatistics.Factory);
		/// </code>
		/// </summary>
		/// <param name="activityID">One of the <see cref="ActivityID"/> values.</param>
		/// <returns>
		/// An instance of the <see cref="IActivity"/> class for the specified metric.
		/// </returns>
		public static IActivity Factory(ActivityID activityID)
		{
			return GetStat(activityID).Start();
		}

		/// <summary>
		/// Returns a report with collected statistics.
		/// </summary>
		/// <returns>
		/// A report with collected statistics.
		/// </returns>
		public static string GetReport()
		{
			decimal totalTime = Total.Elapsed.Ticks;

			return All.Select(m => new
			{
				m.Name,
				m.Elapsed,
				m.CallCount,
				TimePerCall = m.CallCount switch
				{
					0 => TimeSpan.Zero,
					1 => m.Elapsed,
					_ => new TimeSpan(m.Elapsed.Ticks / m.CallCount)
				},
				Percent = m.CallCount == 0 ? "" : $"{m.Elapsed.Ticks / totalTime * 100,7:0.00}%"
			})
			.ToDiagnosticString();
		}

		static StatActivity QueryProviderExecuteT;
		static StatActivity QueryProviderExecute;
		static StatActivity QueryProviderGetEnumeratorT;
		static StatActivity QueryProviderGetEnumerator;
		static StatActivity GetQueryTotal;
		static StatActivity GetQueryFind;
		static StatActivity GetQueryFindExpose;
		static StatActivity GetQueryFindFind;
		static StatActivity GetQueryCreate;
		static StatActivity Build;
		static StatActivity BuildSequence;
		static StatActivity BuildSequenceCanBuild;
		static StatActivity BuildSequenceBuild;
		static StatActivity ReorderBuilders;
		static StatActivity BuildQuery;
		static StatActivity FinalizeQuery;
		static StatActivity GetIEnumerable;

		static StatActivity ExecuteQuery;
		static StatActivity ExecuteQueryAsync;
		static StatActivity ExecuteElement;
		static StatActivity ExecuteElementAsync;
		static StatActivity ExecuteScalar;
		static StatActivity ExecuteScalarAsync;
		static StatActivity ExecuteScalarAlternative;
		static StatActivity ExecuteScalarAlternativeAsync;
		static StatActivity ExecuteNonQuery;
		static StatActivity ExecuteNonQueryAsync;
		static StatActivity ExecuteNonQueryAlternative;
		static StatActivity ExecuteNonQueryAlternativeAsync;

		static StatActivity CreateTable;
		static StatActivity CreateTableAsync;
		static StatActivity DropTable;
		static StatActivity DropTableAsync;
		static StatActivity DeleteObject;
		static StatActivity DeleteObjectAsync;
		static StatActivity InsertObject;
		static StatActivity InsertObjectAsync;
		static StatActivity InsertOrReplaceObject;
		static StatActivity InsertOrReplaceObjectAsync;
		static StatActivity InsertWithIdentityObject;
		static StatActivity InsertWithIdentityObjectAsync;
		static StatActivity UpdateObject;
		static StatActivity UpdateObjectAsync;
		static StatActivity BulkCopy;
		static StatActivity BulkCopyAsync;

		static StatActivity BuildSql;

		static StatActivity CommandInfoExecute;
		static StatActivity CommandInfoExecuteT;
		static StatActivity CommandInfoExecuteCustom;
		static StatActivity CommandInfoExecuteAsync;
		static StatActivity CommandInfoExecuteAsyncT;

		static StatActivity ConnectionOpen;
		static StatActivity ConnectionOpenAsync;
		static StatActivity ConnectionClose;
		static StatActivity ConnectionCloseAsync;
		static StatActivity ConnectionDispose;
		static StatActivity ConnectionDisposeAsync;
		static StatActivity ConnectionBeginTransaction;
		static StatActivity ConnectionBeginTransactionAsync;
		static StatActivity TransactionCommit;
		static StatActivity TransactionCommitAsync;
		static StatActivity TransactionRollback;
		static StatActivity TransactionRollbackAsync;
		static StatActivity TransactionDispose;
		static StatActivity TransactionDisposeAsync;
		static StatActivity CommandExecuteScalar;
		static StatActivity CommandExecuteScalarAsync;
		static StatActivity CommandExecuteReader;
		static StatActivity CommandExecuteReaderAsync;
		static StatActivity CommandExecuteNonQuery;
		static StatActivity CommandExecuteNonQueryAsync;

		static StatActivity GetSqlText;

		static StatActivity OnTraceInternal;
		static StatActivity Materialization;

		static StatActivitySum ExecuteTotal;
		static StatActivitySum ExecuteAdo;
		static StatActivitySum Total;

		static IStatActivity[] All;
	}
}
