using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	// old System.Data.SqlClient versions for .net core (< 4.5.0)
	// miss UDT and BulkCopy support
	// We don't take it into account, as there is no reason to use such old provider versions
	public class SqlServerProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _sysSyncRoot = new object();
		private static readonly object _msSyncRoot  = new object();

		private static SqlServerProviderAdapter? _systemAdapter;
		private static SqlServerProviderAdapter? _microsoftAdapter;

		public const string SystemAssemblyName    = "System.Data.SqlClient";
		public const string MicrosoftAssemblyName = "Microsoft.Data.SqlClient";

		private SqlServerProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Type sqlDataRecordType,
			Type sqlExceptionType,

			Action<IDbDataParameter, SqlDbType> dbTypeSetter,
			Func  <IDbDataParameter, SqlDbType> dbTypeGetter,
			Action<IDbDataParameter, string> udtTypeNameSetter,
			Func  <IDbDataParameter, string> udtTypeNameGetter,
			Action<IDbDataParameter, string> typeNameSetter,
			Func  <IDbDataParameter, string> typeNameGetter,

			Func<string, SqlConnectionStringBuilder> createConnectionStringBuilder,
			Func<string, string>                     quoteIdentifier,
			Func<string, SqlConnection>              createConnection,

			Func<IDbConnection, SqlBulkCopyOptions, IDbTransaction?, SqlBulkCopy> createBulkCopy,
			Func<int, string, SqlBulkCopyColumnMapping>                           createBulkCopyColumnMapping)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			SqlDataRecordType = sqlDataRecordType;
			SqlExceptionType  = sqlExceptionType;

			SetDbType      = dbTypeSetter;
			GetDbType      = dbTypeGetter;
			SetUdtTypeName = udtTypeNameSetter;
			GetUdtTypeName = udtTypeNameGetter;
			SetTypeName    = typeNameSetter;
			GetTypeName    = typeNameGetter;

			_createConnectionStringBuilder = createConnectionStringBuilder;
			_quoteIdentifier               = quoteIdentifier;
			_createConnection              = createConnection;

			_createBulkCopy              = createBulkCopy;
			_createBulkCopyColumnMapping = createBulkCopyColumnMapping;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Type SqlDataRecordType { get; }
		public Type SqlExceptionType  { get; }

		private readonly Func<string, SqlConnectionStringBuilder> _createConnectionStringBuilder;
		public SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString) => _createConnectionStringBuilder(connectionString);

		private readonly Func<IDbConnection, SqlBulkCopyOptions, IDbTransaction?, SqlBulkCopy> _createBulkCopy;
		public SqlBulkCopy CreateBulkCopy(IDbConnection connection, SqlBulkCopyOptions options, IDbTransaction? transaction)
			=> _createBulkCopy(connection, options, transaction);

		private readonly Func<int, string, SqlBulkCopyColumnMapping> _createBulkCopyColumnMapping;
		public SqlBulkCopyColumnMapping CreateBulkCopyColumnMapping(int source, string destination)
			=> _createBulkCopyColumnMapping(source, destination);

		public Action<IDbDataParameter, SqlDbType> SetDbType { get; }
		public Func  <IDbDataParameter, SqlDbType> GetDbType { get; }

		public Action<IDbDataParameter, string> SetUdtTypeName { get; }
		public Func  <IDbDataParameter, string> GetUdtTypeName { get; }

		public Action<IDbDataParameter, string> SetTypeName { get; }
		public Func  <IDbDataParameter, string> GetTypeName { get; }


		private readonly Func<string, string> _quoteIdentifier;
		public string QuoteIdentifier(string identifier) => _quoteIdentifier(identifier);

		private readonly Func<string, SqlConnection> _createConnection;
		public SqlConnection CreateConnection(string connectionString) => _createConnection(connectionString);

		public static SqlServerProviderAdapter GetInstance(SqlServerProvider provider)
		{
			if (provider == SqlServerProvider.SystemDataSqlClient)
			{
				if (_systemAdapter == null)
				{
					lock (_sysSyncRoot)
					{
						if (_systemAdapter == null)
						{
							_systemAdapter = CreateAdapter(SystemAssemblyName, "System.Data.SqlClient", "System.Data.SqlClient");
						}
					}
				}

				return _systemAdapter;
			}
			else
			{
				if (_microsoftAdapter == null)
				{
					lock (_msSyncRoot)
					{
						if (_microsoftAdapter == null)
						{
							_microsoftAdapter = CreateAdapter(MicrosoftAssemblyName, "Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient");
						}
					}
				}

				return _microsoftAdapter;
			}
		}

		private static SqlServerProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string factoryName)
		{
			var isSystem = assemblyName == SystemAssemblyName;

			Assembly? assembly;
#if NET45 || NET46
			if (isSystem)
			{
				assembly = typeof(System.Data.SqlClient.SqlConnection).Assembly;
			}
			else
#endif
			{
				assembly = Type.GetType($"{clientNamespace}.SqlConnection, ${assemblyName}", false)?.Assembly
#if !NETSTANDARD2_0
					?? DbProviderFactories.GetFactory(factoryName).GetType().Assembly
#endif
					;
			}

			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType                 = assembly.GetType($"{clientNamespace}.SqlConnection", true);
			var parameterType                  = assembly.GetType($"{clientNamespace}.SqlParameter", true);
			var dataReaderType                 = assembly.GetType($"{clientNamespace}.SqlDataReader", true);
			var transactionType                = assembly.GetType($"{clientNamespace}.SqlTransaction", true);
			var commandType                    = assembly.GetType($"{clientNamespace}.SqlCommand", true);
			var sqlCommandBuilderType          = assembly.GetType($"{clientNamespace}.SqlCommandBuilder", true);
			var sqlConnectionStringBuilderType = assembly.GetType($"{clientNamespace}.SqlConnectionStringBuilder", true);
			var sqlExceptionType               = assembly.GetType($"{clientNamespace}.SqlException", true);
			var sqlErrorCollectionType         = assembly.GetType($"{clientNamespace}.SqlErrorCollection", true);
			var sqlErrorType                   = assembly.GetType($"{clientNamespace}.SqlError", true);

			var sqlDataRecordType = connectionType.Assembly.GetType(
				isSystem
					? "Microsoft.SqlServer.Server.SqlDataRecord"
					: "Microsoft.Data.SqlClient.Server.SqlDataRecord",
				true);

			var bulkCopyType                        = assembly.GetType($"{clientNamespace}.SqlBulkCopy", true);
			var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.SqlBulkCopyOptions", true);
			var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventHandler", true);
			var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMapping", true);
			var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMappingCollection", true);
			var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventArgs", true);

			var typeMapper = new TypeMapper(
				connectionType, parameterType, transactionType,
				sqlCommandBuilderType, sqlConnectionStringBuilderType,
				sqlExceptionType, sqlErrorCollectionType, sqlErrorType,
				bulkCopyType, bulkCopyOptionsType, bulkRowsCopiedEventHandlerType, bulkCopyColumnMappingType, bulkCopyColumnMappingCollectionType, rowsCopiedEventArgsType);

			typeMapper.RegisterWrapper<SqlConnection>();
			typeMapper.RegisterWrapper<SqlParameter>();
			typeMapper.RegisterWrapper<SqlTransaction>();
			typeMapper.RegisterWrapper<SqlCommandBuilder>();
			typeMapper.RegisterWrapper<SqlException>();
			typeMapper.RegisterWrapper<SqlErrorCollection>();
			typeMapper.RegisterWrapper<SqlError>();
			typeMapper.RegisterWrapper<SqlConnectionStringBuilder>();

			// bulk copy types
			typeMapper.RegisterWrapper<SqlBulkCopy>();
			typeMapper.RegisterWrapper<SqlBulkCopyOptions>();
			typeMapper.RegisterWrapper<SqlRowsCopiedEventHandler>();
			typeMapper.RegisterWrapper<SqlBulkCopyColumnMapping>();
			typeMapper.RegisterWrapper<SqlBulkCopyColumnMappingCollection>();
			typeMapper.RegisterWrapper<SqlRowsCopiedEventArgs>();

			var paramMapper        = typeMapper.Type<SqlParameter>();
			var dbTypeBuilder      = paramMapper.Member(p => p.SqlDbType);
			var udtTypeNameBuilder = paramMapper.Member(p => p.UdtTypeName);
			var typeNameBuilder    = paramMapper.Member(p => p.TypeName);

			var builder = typeMapper.CreateAndWrap(() => new SqlCommandBuilder())!;

			Func<Exception, IEnumerable<int>> exceptionErrorsGettter = (Exception ex)
				=> typeMapper.Wrap<SqlException>(ex)!.Errors.Errors.Select(err => err.Number);

			SqlServerTransientExceptionDetector.RegisterExceptionType(sqlExceptionType, exceptionErrorsGettter);

			return new SqlServerProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				sqlDataRecordType,
				sqlExceptionType,

				dbTypeBuilder.BuildSetter<IDbDataParameter>(),
				dbTypeBuilder.BuildGetter<IDbDataParameter>(),
				udtTypeNameBuilder.BuildSetter<IDbDataParameter>(),
				udtTypeNameBuilder.BuildGetter<IDbDataParameter>(),
				typeNameBuilder.BuildSetter<IDbDataParameter>(),
				typeNameBuilder.BuildGetter<IDbDataParameter>(),

				(string connectionString) => typeMapper.CreateAndWrap(() => new SqlConnectionStringBuilder(connectionString))!,
				builder.QuoteIdentifier,
				(string connectionString) => typeMapper.CreateAndWrap(() => new SqlConnection(connectionString))!,

				(IDbConnection connection, SqlBulkCopyOptions options, IDbTransaction? transaction)
					=> typeMapper.CreateAndWrap(() => new SqlBulkCopy((SqlConnection)connection, options, (SqlTransaction?)transaction))!,
				(int source, string destination)
					=> typeMapper.CreateAndWrap(() => new SqlBulkCopyColumnMapping(source, destination))!);
		}

		#region Wrappers

		#region SqlException
		[Wrapper]
		internal class SqlException : TypeWrapper
		{
			public SqlException(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlErrorCollection Errors
			{
				get => this.Wrap(t => t.Errors);
			}
		}

		[Wrapper]
		internal class SqlErrorCollection : TypeWrapper
		{
			public SqlErrorCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public IEnumerator GetEnumerator() => this.Wrap(t => t.GetEnumerator());

			public IEnumerable<SqlError> Errors
			{
				get
				{
					var e = GetEnumerator();

					while (e.MoveNext())
						yield return mapper_.Wrap<SqlError>(e.Current)!;
				}
			}
		}

		[Wrapper]
		internal class SqlError : TypeWrapper
		{
			public SqlError(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public int Number
			{
				get => this.Wrap(t => t.Number);
			}
		}
		#endregion

		[Wrapper]
		internal class SqlCommandBuilder : TypeWrapper
		{
			public SqlCommandBuilder(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlCommandBuilder() => throw new NotImplementedException();

			public string QuoteIdentifier(string unquotedIdentifier) => this.Wrap(t => t.QuoteIdentifier(unquotedIdentifier));
		}

		[Wrapper]
		internal class SqlParameter
		{
			// string return type is correct, TypeName and UdtTypeName return empty string instead of null
			public string UdtTypeName { get; set; } = null!;
			public string TypeName { get; set; } = null!;
			public SqlDbType SqlDbType { get; set; }
		}

		[Wrapper]
		public class SqlConnectionStringBuilder : TypeWrapper
		{
			public SqlConnectionStringBuilder(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlConnectionStringBuilder(string connectionString) => throw new NotImplementedException();

			public bool MultipleActiveResultSets
			{
				get => this.Wrap(t => t.MultipleActiveResultSets);
				set => this.SetPropValue(t => t.MultipleActiveResultSets, value);
			}
		}

		[Wrapper]
		public class SqlConnection : TypeWrapper, IDisposable
		{
			public SqlConnection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlConnection(string connectionString) => throw new NotImplementedException();

			public IDbCommand CreateCommand() => this.Wrap(t => t.CreateCommand());

			public string ServerVersion => this.Wrap(t => t.ServerVersion);

			public void Open() => this.WrapAction(c => c.Open());

			public void Dispose() => this.WrapAction(t => t.Dispose());
		}

		[Wrapper]
		public class SqlTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		public class SqlBulkCopy : TypeWrapper, IDisposable
		{
			public SqlBulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<SqlBulkCopy, SqlRowsCopiedEventHandler>(nameof(SqlRowsCopied));
			}

			public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions options, SqlTransaction? transaction) => throw new NotImplementedException();

			void IDisposable.Dispose() => this.WrapAction(t => ((IDisposable)t).Dispose());

			public void WriteToServer(IDataReader dataReader) => this.WrapAction(t => t.WriteToServer(dataReader));

			public int NotifyAfter
			{
				get => this.Wrap(t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BatchSize
			{
				get => this.Wrap(t => t.BatchSize);
				set => this.SetPropValue(t => t.BatchSize, value);
			}

			public int BulkCopyTimeout
			{
				get => this.Wrap(t => t.BulkCopyTimeout);
				set => this.SetPropValue(t => t.BulkCopyTimeout, value);
			}

			public string? DestinationTableName
			{
				get => this.Wrap(t => t.DestinationTableName);
				set => this.SetPropValue(t => t.DestinationTableName, value);
			}

			public SqlBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event SqlRowsCopiedEventHandler SqlRowsCopied
			{
				add => Events.AddHandler(nameof(SqlRowsCopied), value);
				remove => Events.RemoveHandler(nameof(SqlRowsCopied), value);
			}
		}

		[Wrapper]
		public class SqlRowsCopiedEventArgs : TypeWrapper
		{
			public SqlRowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public long RowsCopied
			{
				get => this.Wrap(t => t.RowsCopied);
			}

			public bool Abort
			{
				get => this.Wrap(t => t.Abort);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		public delegate void SqlRowsCopiedEventHandler(object sender, SqlRowsCopiedEventArgs e);

		[Wrapper]
		public class SqlBulkCopyColumnMappingCollection : TypeWrapper
		{
			public SqlBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlBulkCopyColumnMapping Add(SqlBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		public enum SqlBulkCopyOptions
		{
			Default                          = 0,
			KeepIdentity                     = 1,
			CheckConstraints                 = 2,
			TableLock                        = 4,
			KeepNulls                        = 8,
			FireTriggers                     = 16,
			UseInternalTransaction           = 32,
			AllowEncryptedValueModifications = 64
		}

		[Wrapper]
		public class SqlBulkCopyColumnMapping : TypeWrapper
		{
			public SqlBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
		#endregion
	}
}
