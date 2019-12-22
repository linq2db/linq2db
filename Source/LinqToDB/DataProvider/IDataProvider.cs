using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using Common;
	using SchemaProvider;
	using SqlProvider;

	public interface IDataProvider
	{
		string             Name                  { get; }
		string             ConnectionNamespace   { get; }
		Type               DataReaderType        { get; }
		MappingSchema      MappingSchema         { get; }
		SqlProviderFlags   SqlProviderFlags      { get; }
		IDbConnection      CreateConnection      (string connectionString);
		ISqlBuilder        CreateSqlBuilder      (MappingSchema mappingSchema);
		ISqlOptimizer      GetSqlOptimizer       ();
		/// <summary>
		/// Initializes <see cref="DataConnection.Command"/> object.
		/// </summary>
		/// <param name="dataConnection">Data connection instance to initialize with new command.</param>
		/// <param name="commandType">Type of command.</param>
		/// <param name="commandText">Command SQL.</param>
		/// <param name="parameters">Optional list of parameters to add to initialized command.</param>
		/// <param name="withParameters">Flag to indicate that command has parameters. Used to configure parameters support when method called without parameters and parameters added later to command.</param>
		void InitCommand           (DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters);
		void               DisposeCommand        (DataConnection dataConnection);
		object?            GetConnectionInfo     (DataConnection dataConnection, string parameterName);
		Expression         GetReaderExpression   (MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType);
		bool?              IsDBNullAllowed       (IDataReader reader, int idx);
		void               SetParameter          (DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value);
		Type               ConvertParameterType  (Type type, DbDataType dataType);
		CommandBehavior    GetCommandBehavior    (CommandBehavior commandBehavior);
		/// <summary>
		/// Returns context object to wrap calls of Execute* methods.
		/// Using this, provider could e.g. change thread culture during Execute* calls.
		/// Following calls wrapped right now:
		/// DataConnection.ExecuteNonQuery
		/// DataConnection.ExecuteReader.
		/// </summary>
		/// <param name="dataConnection">Data connection instance used with scope.</param>
		/// <returns>Returns disposable scope object. Cannot be null.</returns>
		IDisposable?       ExecuteScope          (DataConnection dataConnection);

		ISchemaProvider    GetSchemaProvider     ();

		BulkCopyRowsCopied BulkCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source);
	}
}
