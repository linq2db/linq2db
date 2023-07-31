using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	public class ActivityStatistics
	{
		static ActivityStatistics()
		{
			All = new IStatActivity[]
			{
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
					ExecuteScalar2                  = new("  Execute Scalar 2"),
					ExecuteScalar2Async             = new("  Execute Scalar 2 Async"),
					ExecuteNonQuery                 = new("  Execute NonQuery"),
					ExecuteNonQueryAsync            = new("  Execute NonQuery Async"),
					ExecuteNonQuery2                = new("  Execute NonQuery 2"),
					ExecuteNonQuery2Async           = new("  Execute NonQuery 2 Async"),

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
				ExecuteScalar2,
				ExecuteScalar2Async,
				ExecuteNonQuery,
				ExecuteNonQueryAsync,
				ExecuteNonQuery2,
				ExecuteNonQuery2Async,

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

				null!
			};

			All[^1] = Total = new ("Total", All.OfType<StatActivity>().ToArray());
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
				ActivityID.ExecuteScalar2                  => ExecuteScalar2,
				ActivityID.ExecuteScalar2Async             => ExecuteScalar2Async,
				ActivityID.ExecuteNonQuery                 => ExecuteNonQuery,
				ActivityID.ExecuteNonQueryAsync            => ExecuteNonQueryAsync,
				ActivityID.ExecuteNonQuery2                => ExecuteNonQuery2,
				ActivityID.ExecuteNonQuery2Async           => ExecuteNonQuery2Async,

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
				ActivityID.ConnectionBeginTransaction      => ConnectionBeginTransaction,
				ActivityID.ConnectionBeginTransactionAsync => ConnectionBeginTransactionAsync,
				ActivityID.TransactionCommit               => TransactionCommit,
				ActivityID.TransactionCommitAsync          => TransactionCommitAsync,
				ActivityID.TransactionRollback             => TransactionRollback,
				ActivityID.TransactionRollbackAsync        => TransactionRollbackAsync,
				ActivityID.TransactionDispose              => TransactionCommit,
				ActivityID.TransactionDisposeAsync         => TransactionCommitAsync,
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

		public static IActivity Factory(ActivityID metric)
		{
			return GetStat(metric).Start();
		}

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
				Percent = $"{(m.CallCount == 0 ? (decimal?)null : m.Elapsed.Ticks / totalTime),7:P}"
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
		static StatActivity ExecuteScalar2;
		static StatActivity ExecuteScalar2Async;
		static StatActivity ExecuteNonQuery;
		static StatActivity ExecuteNonQueryAsync;
		static StatActivity ExecuteNonQuery2;
		static StatActivity ExecuteNonQuery2Async;

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
