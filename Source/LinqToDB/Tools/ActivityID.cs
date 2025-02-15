namespace LinqToDB.Tools
{
	/// <summary>
	/// Activity Service event ID.
	/// </summary>
	public enum ActivityID
	{
		None = 0,
		QueryProviderExecuteT,
		QueryProviderExecute,
		QueryProviderGetEnumeratorT,
		QueryProviderGetEnumerator,
		GetQueryTotal,
			GetQueryFind,
				GetQueryFindExpose,
				GetQueryFindFind,
			GetQueryCreate,
				Build,
					BuildSequence,
						BuildSequenceCanBuild,
						BuildSequenceBuild,
					BuildQuery,
						FinalizeQuery,
			GetIEnumerable,
		ExecuteQuery,
		ExecuteQueryAsync,
		ExecuteElement,
		ExecuteElementAsync,
		ExecuteScalar,
		ExecuteScalarAsync,
		ExecuteNonQuery,
		ExecuteNonQueryAsync,
		ExecuteNonQuery2,
		ExecuteNonQuery2Async,
		/// <summary>
		/// Alternative implementation of <see cref="LinqToDB.DataExtensions.InsertOrReplace{T}(IDataContext,T,InsertOrUpdateColumnFilter{T}?,string?,string?,string?,string?,TableOptions)"/> method.
		/// </summary>
		ExecuteScalarAlternative,
		ExecuteScalarAlternativeAsync,
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

			BuildSql,

		CommandInfoExecute,
		CommandInfoExecuteT,
		CommandInfoExecuteCustom,
		CommandInfoExecuteAsync,
		CommandInfoExecuteAsyncT,

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

		/// <summary>The <see cref="Interceptors.ICommandInterceptor.CommandInitialized"/> method call.</summary>
		CommandInterceptorCommandInitialized,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteScalar"/> method call.</summary>
		CommandInterceptorExecuteScalar,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteScalarAsync"/> method call.</summary>
		CommandInterceptorExecuteScalarAsync,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteNonQuery"/> method call.</summary>
		CommandInterceptorExecuteNonQuery,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteNonQueryAsync"/> method call.</summary>
		CommandInterceptorExecuteNonQueryAsync,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteReader"/> method call.</summary>
		CommandInterceptorExecuteReader,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.ExecuteReaderAsync"/> method call.</summary>
		CommandInterceptorExecuteReaderAsync,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.AfterExecuteReader"/> method call.</summary>
		CommandInterceptorAfterExecuteReader,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.BeforeReaderDispose"/> method call.</summary>
		CommandInterceptorBeforeReaderDispose,
		/// <summary>The <see cref="Interceptors.ICommandInterceptor.BeforeReaderDisposeAsync"/> method call.</summary>
		CommandInterceptorBeforeReaderDisposeAsync,

		/// <summary>The <see cref="Interceptors.IConnectionInterceptor.ConnectionOpening"/> method call.</summary>
		ConnectionInterceptorConnectionOpening,
		/// <summary>The <see cref="Interceptors.IConnectionInterceptor.ConnectionOpeningAsync"/> method call.</summary>
		ConnectionInterceptorConnectionOpeningAsync,
		/// <summary>The <see cref="Interceptors.IConnectionInterceptor.ConnectionOpened"/> method call.</summary>
		ConnectionInterceptorConnectionOpened,
		/// <summary>The <see cref="Interceptors.IConnectionInterceptor.ConnectionOpenedAsync"/> method call.</summary>
		ConnectionInterceptorConnectionOpenedAsync,

		/// <summary>The <see cref="Interceptors.IDataContextInterceptor.OnClosing"/> method call.</summary>
		DataContextInterceptorOnClosing,
		/// <summary>The <see cref="Interceptors.IDataContextInterceptor.OnClosingAsync"/> method call.</summary>
		DataContextInterceptorOnClosingAsync,
		/// <summary>The <see cref="Interceptors.IDataContextInterceptor.OnClosed"/> method call.</summary>
		DataContextInterceptorOnClosed,
		/// <summary>The <see cref="Interceptors.IDataContextInterceptor.OnClosedAsync"/> method call.</summary>
		DataContextInterceptorOnClosedAsync,

		/// <summary>The <see cref="Interceptors.IEntityServiceInterceptor.EntityCreated"/> method call.</summary>
		EntityServiceInterceptorEntityCreated,

		/// <summary>The <see cref="Interceptors.IUnwrapDataObjectInterceptor.UnwrapConnection"/> method call.</summary>
		UnwrapDataObjectInterceptorUnwrapConnection,
		/// <summary>The <see cref="Interceptors.IUnwrapDataObjectInterceptor.UnwrapTransaction"/> method call.</summary>
		UnwrapDataObjectInterceptorUnwrapTransaction,
		/// <summary>The <see cref="Interceptors.IUnwrapDataObjectInterceptor.UnwrapCommand"/> method call.</summary>
		UnwrapDataObjectInterceptorUnwrapCommand,
		/// <summary>The <see cref="Interceptors.IUnwrapDataObjectInterceptor.UnwrapDataReader"/> method call.</summary>
		UnwrapDataObjectInterceptorUnwrapDataReader,

		/// <summary>The <see cref="Interceptors.IExceptionInterceptor.ProcessException"/> method call.</summary>
		ExceptionInterceptorProcessException,

		GetSqlText,

			Materialization,
			OnTraceInternal,
	}
}
