using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Metrics;

namespace LinqToDB.Tools.Activity
{
	/// <summary>
	/// Collects LinqToDB call statistics.
	/// </summary>
	public static class ActivityStatistics
	{
		[SuppressMessage(
			"Performance",
			"CA1810:Initialize reference type static fields inline",
			Justification = "Too many referential fields to be easily initialized outside of static constructor"
		)]
		static ActivityStatistics()
		{
			All =
			[
				QueryProviderExecuteT               = new("IQueryProvider.Execute<T>",            ActivityID.QueryProviderExecuteT),
				QueryProviderExecute                = new("IQueryProvider.Execute",               ActivityID.QueryProviderExecute),
				QueryProviderGetEnumeratorT         = new("IQueryProvider.GetEnumerator<T>",      ActivityID.QueryProviderGetEnumeratorT),
				QueryProviderGetEnumerator          = new("IQueryProvider.GetEnumerator",         ActivityID.QueryProviderGetEnumerator),
				GetQueryTotal                       = new("  GetQuery",                           ActivityID.GetQueryTotal),
				GetQueryFind                        = new("    Find",                             ActivityID.GetQueryFind),
				GetQueryFindExpose                  = new("      Expose",                         ActivityID.GetQueryFindExpose),
				GetQueryFindFind                    = new("      Find",                           ActivityID.GetQueryFindFind),
				GetQueryCreate                      = new("    Create",                           ActivityID.GetQueryCreate),
				Build                               = new("      Build",                          ActivityID.Build),
				BuildSequence                       = new("        BuildSequence",                ActivityID.BuildSequence),
				BuildSequenceCanBuild               = new("          CanBuild",                   ActivityID.BuildSequenceCanBuild),
				BuildSequenceBuild                  = new("          Build",                      ActivityID.BuildSequenceBuild),
				BuildQuery                          = new("        BuildQuery",                   ActivityID.BuildQuery),
				FinalizeQuery                       = new("          FinalizeQuery",              ActivityID.FinalizeQuery),
				GetIEnumerable                      = new("  GetIEnumerable",                     ActivityID.GetIEnumerable),

				ExecuteTotal                        = new("Execute",
					ExecuteQuery                    = new("  Execute Query",                      ActivityID.ExecuteQuery),
					ExecuteQueryAsync               = new("  Execute Query Async",                ActivityID.ExecuteQueryAsync),
					ExecuteElement                  = new("  Execute Element",                    ActivityID.ExecuteElement),
					ExecuteElementAsync             = new("  Execute Element Async",              ActivityID.ExecuteElementAsync),
					ExecuteScalar                   = new("  Execute Scalar",                     ActivityID.ExecuteScalar),
					ExecuteScalarAsync              = new("  Execute Scalar Async",               ActivityID.ExecuteScalarAsync),
					ExecuteScalarAlternative        = new("  Execute Scalar Alternative",         ActivityID.ExecuteScalarAlternative),
					ExecuteScalarAlternativeAsync   = new("  Execute Scalar Alternative Async",   ActivityID.ExecuteScalarAlternativeAsync),
					ExecuteNonQuery                 = new("  Execute NonQuery",                   ActivityID.ExecuteNonQuery),
					ExecuteNonQueryAsync            = new("  Execute NonQuery Async",             ActivityID.ExecuteNonQueryAsync),
					ExecuteNonQueryAlternative      = new("  Execute NonQuery Alternative",       ActivityID.ExecuteNonQuery2),
					ExecuteNonQueryAlternativeAsync = new("  Execute NonQuery Alternative Async", ActivityID.ExecuteNonQuery2Async),

					CommandInfoExecute              = new("  SQL Execute",                        ActivityID.CommandInfoExecute),
					CommandInfoExecuteAsync         = new("  SQL ExecuteAsync",                   ActivityID.CommandInfoExecuteAsync),
					CommandInfoExecuteT             = new("  SQL Execute<T>",                     ActivityID.CommandInfoExecuteT),
					CommandInfoExecuteAsyncT        = new("  SQL ExecuteAsync<T>",                ActivityID.CommandInfoExecuteAsyncT),
					CommandInfoExecuteCustom        = new("  SQL ExecuteCustom",                  ActivityID.CommandInfoExecuteCustom),

					CreateTable                     = new("  CreateTable",                        ActivityID.CreateTable),
					CreateTableAsync                = new("  CreateTable Async",                  ActivityID.CreateTableAsync),
					DropTable                       = new("  DropTable",                          ActivityID.DropTable),
					DropTableAsync                  = new("  DropTable Async",                    ActivityID.DropTableAsync),
					DeleteObject                    = new("  Delete Object",                      ActivityID.DeleteObject),
					DeleteObjectAsync               = new("  Delete Object Async",                ActivityID.DeleteObjectAsync),
					InsertObject                    = new("  Insert Object",                      ActivityID.InsertObject),
					InsertObjectAsync               = new("  Insert Object Async",                ActivityID.InsertObjectAsync),
					InsertOrReplaceObject           = new("  InsertOrReplace Object",             ActivityID.InsertOrReplaceObject),
					InsertOrReplaceObjectAsync      = new("  InsertOrReplace Object Async",       ActivityID.InsertOrReplaceObjectAsync),
					InsertWithIdentityObject        = new("  InsertWithIdentity Object",          ActivityID.InsertWithIdentityObject),
					InsertWithIdentityObjectAsync   = new("  InsertWithIdentity Object Async",    ActivityID.InsertWithIdentityObjectAsync),
					UpdateObject                    = new("  Update Object",                      ActivityID.UpdateObject),
					UpdateObjectAsync               = new("  Update Object Async",                ActivityID.UpdateObjectAsync),
					BulkCopy                        = new("  BulkCopy",                           ActivityID.BulkCopy),
					BulkCopyAsync                   = new("  BulkCopy Async",                     ActivityID.BulkCopyAsync)
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

				BuildSql                            = new("    BuildSql",                           ActivityID.BuildSql),

				CommandInfoExecute,
				CommandInfoExecuteT,
				CommandInfoExecuteCustom,
				CommandInfoExecuteAsync,
				CommandInfoExecuteAsyncT,

				ExecuteAdo                          = new("    ADO.NET",
					ConnectionOpen                  = new("      Connection Open",                  ActivityID.ConnectionOpen),
					ConnectionOpenAsync             = new("      Connection OpenAsync",             ActivityID.ConnectionOpenAsync),
					ConnectionClose                 = new("      Connection Close",                 ActivityID.ConnectionClose),
					ConnectionCloseAsync            = new("      Connection CloseAsync",            ActivityID.ConnectionCloseAsync),
					ConnectionDispose               = new("      Connection Dispose",               ActivityID.ConnectionDispose),
					ConnectionDisposeAsync          = new("      Connection DisposeAsync",          ActivityID.ConnectionDisposeAsync),
					ConnectionBeginTransaction      = new("      Connection BeginTransaction",      ActivityID.ConnectionBeginTransaction),
					ConnectionBeginTransactionAsync = new("      Connection BeginTransactionAsync", ActivityID.ConnectionBeginTransactionAsync),
					TransactionCommit               = new("      Transaction Commit",               ActivityID.TransactionCommit),
					TransactionCommitAsync          = new("      Transaction CommitAsync",          ActivityID.TransactionCommitAsync),
					TransactionRollback             = new("      Transaction Rollback",             ActivityID.TransactionRollback),
					TransactionRollbackAsync        = new("      Transaction RollbackAsync",        ActivityID.TransactionRollbackAsync),
					TransactionDispose              = new("      Transaction Dispose",              ActivityID.TransactionDispose),
					TransactionDisposeAsync         = new("      Transaction DisposeAsync",         ActivityID.TransactionDisposeAsync),
					CommandExecuteScalar            = new("      Command ExecuteScalar",            ActivityID.CommandExecuteScalar),
					CommandExecuteScalarAsync       = new("      Command ExecuteScalarAsync",       ActivityID.CommandExecuteScalarAsync),
					CommandExecuteReader            = new("      Command ExecuteReader",            ActivityID.CommandExecuteReader),
					CommandExecuteReaderAsync       = new("      Command ExecuteReaderAsync",       ActivityID.CommandExecuteReaderAsync),
					CommandExecuteNonQuery          = new("      Command ExecuteNonQuery",          ActivityID.CommandExecuteNonQuery),
					CommandExecuteNonQueryAsync     = new("      Command ExecuteNonQueryAsync",     ActivityID.CommandExecuteNonQueryAsync)
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

				OnTraceInternal                                      = new("    OnTraceInternal",                      ActivityID.OnTraceInternal),
				Materialization                                      = new("    Materialization",                      ActivityID.Materialization),

				GetSqlText                                           = new("  GetSqlText",                             ActivityID.GetSqlText),

				Interceptors                                         = new("Interceptors",
					CommandInterceptor                               = new("  CommandInterceptor",
						CommandInterceptorCommandInitialized         = new("    CommandInitialized",                   ActivityID.CommandInterceptorCommandInitialized),
						CommandInterceptorExecuteScalar              = new("    ExecuteScalar",                        ActivityID.CommandInterceptorExecuteScalar),
						CommandInterceptorExecuteScalarAsync         = new("    ExecuteScalarAsync",                   ActivityID.CommandInterceptorExecuteScalarAsync),
						CommandInterceptorExecuteNonQuery            = new("    ExecuteNonQuery",                      ActivityID.CommandInterceptorExecuteNonQuery),
						CommandInterceptorExecuteNonQueryAsync       = new("    ExecuteNonQueryAsync",                 ActivityID.CommandInterceptorExecuteNonQueryAsync),
						CommandInterceptorExecuteReader              = new("    ExecuteReader",                        ActivityID.CommandInterceptorExecuteReader),
						CommandInterceptorExecuteReaderAsync         = new("    ExecuteReaderAsync",                   ActivityID.CommandInterceptorExecuteReaderAsync),
						CommandInterceptorAfterExecuteReader         = new("    AfterExecuteReader",                   ActivityID.CommandInterceptorAfterExecuteReader),
						CommandInterceptorBeforeReaderDispose        = new("    BeforeReaderDispose",                  ActivityID.CommandInterceptorBeforeReaderDispose),
						CommandInterceptorBeforeReaderDisposeAsync   = new("    BeforeReaderDisposeAsync",             ActivityID.CommandInterceptorBeforeReaderDisposeAsync)
					),
					ConnectionInterceptor           = new("  ConnectionInterceptor",
						ConnectionInterceptorConnectionOpening       = new("    ConnectionOpening",                    ActivityID.ConnectionInterceptorConnectionOpening),
						ConnectionInterceptorConnectionOpeningAsync  = new("    ConnectionOpeningAsync",               ActivityID.ConnectionInterceptorConnectionOpeningAsync),
						ConnectionInterceptorConnectionOpened        = new("    ConnectionOpened",                     ActivityID.ConnectionInterceptorConnectionOpened),
						ConnectionInterceptorConnectionOpenedAsync   = new("    ConnectionOpenedAsync",                ActivityID.ConnectionInterceptorConnectionOpenedAsync)
					),
					DataContextInterceptor          = new("  DataContextInterceptor",
						DataContextInterceptorOnClosing              = new("    OnClosing",                            ActivityID.DataContextInterceptorOnClosing),
						DataContextInterceptorOnClosingAsync         = new("    OnClosingAsync",                       ActivityID.DataContextInterceptorOnClosingAsync),
						DataContextInterceptorOnClosed               = new("    OnClosed",                             ActivityID.DataContextInterceptorOnClosed),
						DataContextInterceptorOnClosedAsync          = new("    OnClosedAsync",                        ActivityID.DataContextInterceptorOnClosedAsync)
					),
					EntityServiceInterceptor        = new("  EntityServiceInterceptor",
						EntityServiceInterceptorEntityCreated        = new("    EntityCreated",                        ActivityID.EntityServiceInterceptorEntityCreated)
					),
					UnwrapDataObjectInterceptor     = new("  UnwrapDataObjectInterceptor",
						UnwrapDataObjectInterceptorUnwrapConnection  = new("    UnwrapConnection",                     ActivityID.UnwrapDataObjectInterceptorUnwrapConnection),
						UnwrapDataObjectInterceptorUnwrapTransaction = new("    UnwrapTransaction",                    ActivityID.UnwrapDataObjectInterceptorUnwrapTransaction),
						UnwrapDataObjectInterceptorUnwrapCommand     = new("    UnwrapCommand",                        ActivityID.UnwrapDataObjectInterceptorUnwrapCommand),
						UnwrapDataObjectInterceptorUnwrapDataReader  = new("    UnwrapDataReader",                     ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader)
					),
					ExceptionInterceptor            = new("  ExceptionInterceptor",
						ExceptionInterceptorProcessException         = new("    ExceptionInterceptorProcessException", ActivityID.ExceptionInterceptorProcessException)
					)
				),

				CommandInterceptor,
				CommandInterceptorCommandInitialized,
				CommandInterceptorExecuteScalar,
				CommandInterceptorExecuteScalarAsync,
				CommandInterceptorExecuteNonQuery,
				CommandInterceptorExecuteNonQueryAsync,
				CommandInterceptorExecuteReader,
				CommandInterceptorExecuteReaderAsync,
				CommandInterceptorAfterExecuteReader,
				CommandInterceptorBeforeReaderDispose,
				CommandInterceptorBeforeReaderDisposeAsync,

				ConnectionInterceptor,
				ConnectionInterceptorConnectionOpening,
				ConnectionInterceptorConnectionOpeningAsync,
				ConnectionInterceptorConnectionOpened,
				ConnectionInterceptorConnectionOpenedAsync,

				DataContextInterceptor,
				DataContextInterceptorOnClosing,
				DataContextInterceptorOnClosingAsync,
				DataContextInterceptorOnClosed,
				DataContextInterceptorOnClosedAsync,

				EntityServiceInterceptor,
				EntityServiceInterceptorEntityCreated,

				UnwrapDataObjectInterceptor,
				UnwrapDataObjectInterceptorUnwrapConnection,
				UnwrapDataObjectInterceptorUnwrapTransaction,
				UnwrapDataObjectInterceptorUnwrapCommand,
				UnwrapDataObjectInterceptorUnwrapDataReader,

				// Placeholder for Total, must be last, do not remove or change position!
				//
				null!
			];

			All[^1] = Total = new ("Total", All.Where(a => a is StatActivity).ToArray());
		}

		internal static StatActivity GetStat(ActivityID metric)
		{
			return metric switch
			{
				ActivityID.QueryProviderExecuteT                        => QueryProviderExecuteT,
				ActivityID.QueryProviderExecute                         => QueryProviderExecute,
				ActivityID.QueryProviderGetEnumeratorT                  => QueryProviderGetEnumeratorT,
				ActivityID.QueryProviderGetEnumerator                   => QueryProviderGetEnumerator,
				ActivityID.GetQueryTotal                                => GetQueryTotal,
				ActivityID.GetQueryFind                                 => GetQueryFind,
				ActivityID.GetQueryFindExpose                           => GetQueryFindExpose,
				ActivityID.GetQueryFindFind                             => GetQueryFindFind,
				ActivityID.GetQueryCreate                               => GetQueryCreate,
				ActivityID.Build                                        => Build,
				ActivityID.BuildSequence                                => BuildSequence,
				ActivityID.BuildSequenceCanBuild                        => BuildSequenceCanBuild,
				ActivityID.BuildSequenceBuild                           => BuildSequenceBuild,
				ActivityID.BuildQuery                                   => BuildQuery,
				ActivityID.FinalizeQuery                                => FinalizeQuery,
				ActivityID.GetIEnumerable                               => GetIEnumerable,
				ActivityID.ExecuteQuery                                 => ExecuteQuery,
				ActivityID.ExecuteQueryAsync                            => ExecuteQueryAsync,
				ActivityID.ExecuteElement                               => ExecuteElement,
				ActivityID.ExecuteElementAsync                          => ExecuteElementAsync,
				ActivityID.ExecuteScalar                                => ExecuteScalar,
				ActivityID.ExecuteScalarAsync                           => ExecuteScalarAsync,
				ActivityID.ExecuteScalarAlternative                     => ExecuteScalarAlternative,
				ActivityID.ExecuteScalarAlternativeAsync                => ExecuteScalarAlternativeAsync,
				ActivityID.ExecuteNonQuery                              => ExecuteNonQuery,
				ActivityID.ExecuteNonQueryAsync                         => ExecuteNonQueryAsync,
				ActivityID.ExecuteNonQuery2                             => ExecuteNonQueryAlternative,
				ActivityID.ExecuteNonQuery2Async                        => ExecuteNonQueryAlternativeAsync,

				ActivityID.CreateTable                                  => CreateTable,
				ActivityID.CreateTableAsync                             => CreateTableAsync,
				ActivityID.DropTable                                    => DropTable,
				ActivityID.DropTableAsync                               => DropTableAsync,
				ActivityID.DeleteObject                                 => DeleteObject,
				ActivityID.DeleteObjectAsync                            => DeleteObjectAsync,
				ActivityID.InsertObject                                 => InsertObject,
				ActivityID.InsertObjectAsync                            => InsertObjectAsync,
				ActivityID.InsertOrReplaceObject                        => InsertOrReplaceObject,
				ActivityID.InsertOrReplaceObjectAsync                   => InsertOrReplaceObjectAsync,
				ActivityID.InsertWithIdentityObject                     => InsertWithIdentityObject,
				ActivityID.InsertWithIdentityObjectAsync                => InsertWithIdentityObjectAsync,
				ActivityID.UpdateObject                                 => UpdateObject,
				ActivityID.UpdateObjectAsync                            => UpdateObjectAsync,
				ActivityID.BulkCopy                                     => BulkCopy,
				ActivityID.BulkCopyAsync                                => BulkCopyAsync,

				ActivityID.BuildSql                                     => BuildSql,

				ActivityID.ConnectionOpen                               => ConnectionOpen,
				ActivityID.ConnectionOpenAsync                          => ConnectionOpenAsync,
				ActivityID.ConnectionClose                              => ConnectionClose,
				ActivityID.ConnectionCloseAsync                         => ConnectionCloseAsync,
				ActivityID.ConnectionDispose                            => ConnectionDispose,
				ActivityID.ConnectionDisposeAsync                       => ConnectionDisposeAsync,
				ActivityID.ConnectionBeginTransaction                   => ConnectionBeginTransaction,
				ActivityID.ConnectionBeginTransactionAsync              => ConnectionBeginTransactionAsync,
				ActivityID.TransactionCommit                            => TransactionCommit,
				ActivityID.TransactionCommitAsync                       => TransactionCommitAsync,
				ActivityID.TransactionRollback                          => TransactionRollback,
				ActivityID.TransactionRollbackAsync                     => TransactionRollbackAsync,
				ActivityID.TransactionDispose                           => TransactionDispose,
				ActivityID.TransactionDisposeAsync                      => TransactionDisposeAsync,
				ActivityID.CommandExecuteScalar                         => CommandExecuteScalar,
				ActivityID.CommandExecuteScalarAsync                    => CommandExecuteScalarAsync,
				ActivityID.CommandExecuteReader                         => CommandExecuteReader,
				ActivityID.CommandExecuteReaderAsync                    => CommandExecuteReaderAsync,
				ActivityID.CommandExecuteNonQuery                       => CommandExecuteNonQuery,
				ActivityID.CommandExecuteNonQueryAsync                  => CommandExecuteNonQueryAsync,

				ActivityID.CommandInfoExecute                           => CommandInfoExecute,
				ActivityID.CommandInfoExecuteT                          => CommandInfoExecuteT,
				ActivityID.CommandInfoExecuteCustom                     => CommandInfoExecuteCustom,
				ActivityID.CommandInfoExecuteAsync                      => CommandInfoExecuteAsync,
				ActivityID.CommandInfoExecuteAsyncT                     => CommandInfoExecuteAsyncT,

				ActivityID.CommandInterceptorCommandInitialized         => CommandInterceptorCommandInitialized,
				ActivityID.CommandInterceptorExecuteScalar              => CommandInterceptorExecuteScalar,
				ActivityID.CommandInterceptorExecuteScalarAsync         => CommandInterceptorExecuteScalarAsync,
				ActivityID.CommandInterceptorExecuteNonQuery            => CommandInterceptorExecuteNonQuery,
				ActivityID.CommandInterceptorExecuteNonQueryAsync       => CommandInterceptorExecuteNonQueryAsync,
				ActivityID.CommandInterceptorExecuteReader              => CommandInterceptorExecuteReader,
				ActivityID.CommandInterceptorExecuteReaderAsync         => CommandInterceptorExecuteReaderAsync,
				ActivityID.CommandInterceptorAfterExecuteReader         => CommandInterceptorAfterExecuteReader,
				ActivityID.CommandInterceptorBeforeReaderDispose        => CommandInterceptorBeforeReaderDispose,
				ActivityID.CommandInterceptorBeforeReaderDisposeAsync   => CommandInterceptorBeforeReaderDisposeAsync,

				ActivityID.ConnectionInterceptorConnectionOpening       => ConnectionInterceptorConnectionOpening,
				ActivityID.ConnectionInterceptorConnectionOpeningAsync  => ConnectionInterceptorConnectionOpeningAsync,
				ActivityID.ConnectionInterceptorConnectionOpened        => ConnectionInterceptorConnectionOpened,
				ActivityID.ConnectionInterceptorConnectionOpenedAsync   => ConnectionInterceptorConnectionOpenedAsync,

				ActivityID.DataContextInterceptorOnClosing              => DataContextInterceptorOnClosing,
				ActivityID.DataContextInterceptorOnClosingAsync         => DataContextInterceptorOnClosingAsync,
				ActivityID.DataContextInterceptorOnClosed               => DataContextInterceptorOnClosed,
				ActivityID.DataContextInterceptorOnClosedAsync          => DataContextInterceptorOnClosedAsync,

				ActivityID.EntityServiceInterceptorEntityCreated        => EntityServiceInterceptorEntityCreated,

				ActivityID.UnwrapDataObjectInterceptorUnwrapConnection  => UnwrapDataObjectInterceptorUnwrapConnection,
				ActivityID.UnwrapDataObjectInterceptorUnwrapTransaction => UnwrapDataObjectInterceptorUnwrapTransaction,
				ActivityID.UnwrapDataObjectInterceptorUnwrapCommand     => UnwrapDataObjectInterceptorUnwrapCommand,
				ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader  => UnwrapDataObjectInterceptorUnwrapDataReader,

				ActivityID.ExceptionInterceptorProcessException         => ExceptionInterceptorProcessException,

				ActivityID.GetSqlText                                   => GetSqlText,
				ActivityID.OnTraceInternal                              => OnTraceInternal,
				ActivityID.Materialization                              => Materialization,

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
		/// <param name="includeAll">If <c>true</c>, includes metrics with zero call count. Default is <c>false</c>.</param>
		/// <returns>
		/// A report with collected statistics.
		/// </returns>
		public static string GetReport(bool includeAll = false)
		{
			decimal totalTime = Total.Elapsed.Ticks;

			return All
				.Where(a => includeAll || a.CallCount > 0)
				.Select(m => new
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
					Percent = m.CallCount == 0 ? "" : FormattableString.Invariant($"{m.Elapsed.Ticks / totalTime * 100,7:0.00}%")
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

		static StatActivity CommandInterceptorCommandInitialized;
		static StatActivity CommandInterceptorExecuteScalar;
		static StatActivity CommandInterceptorExecuteScalarAsync;
		static StatActivity CommandInterceptorExecuteNonQuery;
		static StatActivity CommandInterceptorExecuteNonQueryAsync;
		static StatActivity CommandInterceptorExecuteReader;
		static StatActivity CommandInterceptorExecuteReaderAsync;
		static StatActivity CommandInterceptorAfterExecuteReader;
		static StatActivity CommandInterceptorBeforeReaderDispose;
		static StatActivity CommandInterceptorBeforeReaderDisposeAsync;

		static StatActivity ConnectionInterceptorConnectionOpening;
		static StatActivity ConnectionInterceptorConnectionOpeningAsync;
		static StatActivity ConnectionInterceptorConnectionOpened;
		static StatActivity ConnectionInterceptorConnectionOpenedAsync;

		static StatActivity DataContextInterceptorOnClosing;
		static StatActivity DataContextInterceptorOnClosingAsync;
		static StatActivity DataContextInterceptorOnClosed;
		static StatActivity DataContextInterceptorOnClosedAsync;

		static StatActivity EntityServiceInterceptorEntityCreated;

		static StatActivity UnwrapDataObjectInterceptorUnwrapConnection;
		static StatActivity UnwrapDataObjectInterceptorUnwrapTransaction;
		static StatActivity UnwrapDataObjectInterceptorUnwrapCommand;
		static StatActivity UnwrapDataObjectInterceptorUnwrapDataReader;

		static StatActivity ExceptionInterceptorProcessException;

		static StatActivity GetSqlText;

		static StatActivity OnTraceInternal;
		static StatActivity Materialization;

#pragma warning disable IDE0052 // Remove unread private members
		static StatActivitySum ExecuteTotal;
		static StatActivitySum ExecuteAdo;
#pragma warning restore IDE0052 // Remove unread private members
		static StatActivitySum Total;

#pragma warning disable IDE0052 // Remove unread private members
		static StatActivitySum Interceptors;
#pragma warning restore IDE0052 // Remove unread private members
		static StatActivitySum CommandInterceptor;
		static StatActivitySum ConnectionInterceptor;
		static StatActivitySum DataContextInterceptor;
		static StatActivitySum EntityServiceInterceptor;
		static StatActivitySum UnwrapDataObjectInterceptor;
#pragma warning disable IDE0052 // Remove unread private members
		static StatActivitySum ExceptionInterceptor;
#pragma warning restore IDE0052 // Remove unread private members

		static IStatActivity[] All;
	}
}
