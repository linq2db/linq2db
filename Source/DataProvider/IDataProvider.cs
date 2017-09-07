using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;

#if !NOASYNC
using System.Threading.Tasks;
#endif

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
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
		ISqlBuilder        CreateSqlBuilder      ();
		ISqlOptimizer      GetSqlOptimizer       ();
		void               InitCommand           (DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters);
		void               DisposeCommand        (DataConnection dataConnection);
		object             GetConnectionInfo     (DataConnection dataConnection, string parameterName);
		Expression         GetReaderExpression   (MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType);
		bool?              IsDBNullAllowed       (IDataReader reader, int idx);
		void               SetParameter          (IDbDataParameter parameter, string name, DataType dataType, object value);
		Type               ConvertParameterType  (Type type, DataType dataType);
		bool               IsCompatibleConnection(IDbConnection connection);
		CommandBehavior    GetCommandBehavior    (CommandBehavior commandBehavior);
		/// <summary>
		/// Returns conext object to wrap calls of Execute* methods.
		/// Using this, provider could e.g. change thread culture during Execute* calls.
		/// Following calls wrapped right now:
		/// DataConnection.ExecuteNonQuery
		/// DataConnection.ExecuteReader.
		/// </summary>
		/// <returns>Returns disposable scope object. Cannot be null.</returns>
		IDisposable        ExecuteScope          ();

#if !NETSTANDARD1_6
		ISchemaProvider    GetSchemaProvider     ();
#endif

		BulkCopyRowsCopied BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source);

		int Merge<T>(
			DataConnection           dataConnection,
			Expression<Func<T,bool>> predicate,
			bool                     delete,
			IEnumerable<T>           source,
			string                   tableName,
			string                   databaseName,
			string                   schemaName)
			where T : class;

#if !NOASYNC
		Task<int> MergeAsync<T>(
			DataConnection           dataConnection,
			Expression<Func<T,bool>> predicate,
			bool                     delete,
			IEnumerable<T>           source,
			string                   tableName,
			string                   databaseName,
			string                   schemaName,
			CancellationToken        token)
			where T : class;
#endif

		int Merge<TTarget, TSource>(DataConnection dataConnection, IMergeable<TTarget, TSource> merge)
			where TTarget : class
			where TSource : class;

#if !NOASYNC
		Task<int> MergeAsync<TTarget, TSource>(DataConnection dataConnection, IMergeable<TTarget, TSource> merge, CancellationToken token)
			where TTarget : class
			where TSource : class;
#endif

		//TimeSpan? ShouldRetryOn(Exception exception, int retryCount, TimeSpan baseDelay);
	}
}
