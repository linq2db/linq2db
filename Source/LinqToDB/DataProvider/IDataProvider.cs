using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public interface IDataProvider
	{
		string           Name                  { get; }
		int              ID                    { get; }
		string?          ConnectionNamespace   { get; }
		Type             DataReaderType        { get; }
		MappingSchema    MappingSchema         { get; }
		SqlProviderFlags SqlProviderFlags      { get; }
		TableOptions     SupportedTableOptions { get; }
		void             InitContext           (IDataContext dataContext);
		DbConnection     CreateConnection      (string connectionString);
		ISqlBuilder      CreateSqlBuilder      (MappingSchema mappingSchema, LinqOptions linqOptions);
		ISqlOptimizer    GetSqlOptimizer       ();
		/// <summary>
		/// Initializes <see cref="DataConnection"/> command object.
		/// </summary>
		/// <param name="dataConnection">Data connection instance to initialize with new command.</param>
		/// <param name="command">Command instance to initialize.</param>
		/// <param name="commandType">Type of command.</param>
		/// <param name="commandText">Command SQL.</param>
		/// <param name="parameters">Optional list of parameters to add to initialized command.</param>
		/// <param name="withParameters">Flag to indicate that command has parameters. Used to configure parameters support when method called without parameters and parameters added later to command.</param>
		/// <returns>Initialized command instance.</returns>
		DbCommand          InitCommand           (DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters);
		void               DisposeCommand        (DbCommand command);
#if NETSTANDARD2_1PLUS
		ValueTask          DisposeCommandAsync   (DbCommand command);
#endif
		object?            GetConnectionInfo     (DataConnection dataConnection, string parameterName);
		Expression         GetReaderExpression   (DbDataReader reader, int idx, Expression readerExpression, Type toType);
		bool?              IsDBNullAllowed       (DbDataReader reader, int idx);
		void               SetParameter          (DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value);
		Type               ConvertParameterType  (Type type, DbDataType dataType);
		CommandBehavior    GetCommandBehavior    (CommandBehavior commandBehavior);
		/// <summary>
		/// Returns scoped context object to wrap calls of Execute* methods.
		/// Using this, provider could e.g. change thread culture during Execute* calls.
		/// Following calls wrapped right now:
		/// DataConnection.ExecuteNonQuery
		/// DataConnection.ExecuteReader.
		/// </summary>
		/// <param name="dataConnection">Data connection instance used with scope.</param>
		/// <returns>Returns disposable scope object. Can be <c>null</c>.</returns>
		IExecutionScope?   ExecuteScope          (DataConnection dataConnection);

		ISchemaProvider    GetSchemaProvider     ();

		BulkCopyRowsCopied BulkCopy<T>(
			DataOptions     options,
			ITable<T>       table,
			BulkCopyOptions bulkCopyOptions,
			IEnumerable<T>  source)
		where T : notnull;

		Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions       options,
			ITable<T>         table,
			BulkCopyOptions   bulkCopyOptions,
			IEnumerable<T>    source,
			CancellationToken cancellationToken)
		where T : notnull;

#if NATIVE_ASYNC
		Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions         options,
			ITable<T>           table,
			BulkCopyOptions     bulkCopyOptions,
			IAsyncEnumerable<T> source,
			CancellationToken   cancellationToken)
		where T: notnull;
#endif
	}
}
