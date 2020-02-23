using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
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

		public const string SystemAssemblyName           = "System.Data.SqlClient";
		public const string SystemClientNamespace        = "System.Data.SqlClient";
		public const string SystemProviderFactoryName    = "System.Data.SqlClient";

		public const string MicrosoftAssemblyName        = "Microsoft.Data.SqlClient";
		public const string MicrosoftClientNamespace     = "Microsoft.Data.SqlClient";
		public const string MicrosoftProviderFactoryName = "Microsoft.Data.SqlClient";

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

		public string GetSqlXmlReaderMethod         => "GetSqlXml";
		public string GetDateTimeOffsetReaderMethod => "GetDateTimeOffset";
		public string GetTimeSpanReaderMethod       => "GetTimeSpan";

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
					lock (_sysSyncRoot)
						if (_systemAdapter == null)
							_systemAdapter = CreateAdapter(SystemAssemblyName, SystemClientNamespace, SystemProviderFactoryName);

				return _systemAdapter;
			}
			else
			{
				if (_microsoftAdapter == null)
					lock (_msSyncRoot)
						if (_microsoftAdapter == null)
							_microsoftAdapter = CreateAdapter(MicrosoftAssemblyName, MicrosoftClientNamespace, MicrosoftProviderFactoryName);

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
				assembly = Common.Tools.TryLoadAssembly(assemblyName, factoryName);
			}

			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType                 = assembly.GetType($"{clientNamespace}.SqlConnection"             , true);
			var parameterType                  = assembly.GetType($"{clientNamespace}.SqlParameter"              , true);
			var dataReaderType                 = assembly.GetType($"{clientNamespace}.SqlDataReader"             , true);
			var transactionType                = assembly.GetType($"{clientNamespace}.SqlTransaction"            , true);
			var commandType                    = assembly.GetType($"{clientNamespace}.SqlCommand"                , true);
			var sqlCommandBuilderType          = assembly.GetType($"{clientNamespace}.SqlCommandBuilder"         , true);
			var sqlConnectionStringBuilderType = assembly.GetType($"{clientNamespace}.SqlConnectionStringBuilder", true);
			var sqlExceptionType               = assembly.GetType($"{clientNamespace}.SqlException"              , true);
			var sqlErrorCollectionType         = assembly.GetType($"{clientNamespace}.SqlErrorCollection"        , true);
			var sqlErrorType                   = assembly.GetType($"{clientNamespace}.SqlError"                  , true);

			var sqlDataRecordType = connectionType.Assembly.GetType(
				isSystem
					? "Microsoft.SqlServer.Server.SqlDataRecord"
					: "Microsoft.Data.SqlClient.Server.SqlDataRecord",
				true);

			var bulkCopyType                        = assembly.GetType($"{clientNamespace}.SqlBulkCopy"                       , true);
			var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.SqlBulkCopyOptions"                , true);
			var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventHandler"         , true);
			var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMapping"          , true);
			var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMappingCollection", true);
			var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventArgs"            , true);

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<SqlConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<SqlParameter>(parameterType);
			typeMapper.RegisterTypeWrapper<SqlTransaction>(transactionType);
			typeMapper.RegisterTypeWrapper<SqlCommandBuilder>(sqlCommandBuilderType);
			typeMapper.RegisterTypeWrapper<SqlErrorCollection>(sqlErrorCollectionType);
			typeMapper.RegisterTypeWrapper<SqlException>(sqlExceptionType);
			typeMapper.RegisterTypeWrapper<SqlError>(sqlErrorType);
			typeMapper.RegisterTypeWrapper<SqlConnectionStringBuilder>(sqlConnectionStringBuilderType);

			// bulk copy types
			typeMapper.RegisterTypeWrapper<SqlBulkCopy>(bulkCopyType);
			typeMapper.RegisterTypeWrapper<SqlBulkCopyOptions>(bulkCopyOptionsType);
			typeMapper.RegisterTypeWrapper<SqlRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
			typeMapper.RegisterTypeWrapper<SqlBulkCopyColumnMapping>(bulkCopyColumnMappingType);
			typeMapper.RegisterTypeWrapper<SqlBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollectionType);
			typeMapper.RegisterTypeWrapper<SqlRowsCopiedEventArgs>(rowsCopiedEventArgsType);
			typeMapper.FinalizeMappings();

			var paramMapper        = typeMapper.Type<SqlParameter>();
			var dbTypeBuilder      = paramMapper.Member(p => p.SqlDbType);
			var udtTypeNameBuilder = paramMapper.Member(p => p.UdtTypeName);
			var typeNameBuilder    = paramMapper.Member(p => p.TypeName);

			var builder = typeMapper.BuildWrappedFactory(() => new SqlCommandBuilder());

			Func<Exception, IEnumerable<int>> exceptionErrorsGettter = (Exception ex)
				=> typeMapper.Wrap<SqlException>(ex).Errors.Errors.Select(err => err.Number);

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

				typeMapper.BuildWrappedFactory((string connectionString) => new SqlConnectionStringBuilder(connectionString)),
				builder().QuoteIdentifier,
				typeMapper.BuildWrappedFactory((string connectionString) => new SqlConnection(connectionString)),

				typeMapper.BuildWrappedFactory((IDbConnection connection, SqlBulkCopyOptions options, IDbTransaction? transaction) => new SqlBulkCopy((SqlConnection)connection, options, (SqlTransaction?)transaction)),
				typeMapper.BuildWrappedFactory((int source, string destination) => new SqlBulkCopyColumnMapping(source, destination)));
		}

		#region Wrappers

		#region SqlException
		[Wrapper]
		internal class SqlException : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get Errors
				(Expression<Func<SqlException, SqlErrorCollection>>)((SqlException this_) => this_.Errors),
			};

			public SqlException(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlErrorCollection Errors => ((Func<SqlException, SqlErrorCollection>)CompiledWrappers[0])(this);
		}

		[Wrapper]
		internal class SqlErrorCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: GetEnumerator
				(Expression<Func<SqlErrorCollection, IEnumerator>>)((SqlErrorCollection this_) => this_.GetEnumerator()),
				// [1]: SqlError wrapper
				(Expression<Func<object, SqlError>>)((object error) => (SqlError)error),
			};

			public SqlErrorCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public IEnumerator GetEnumerator() => ((Func<SqlErrorCollection, IEnumerator>)CompiledWrappers[0])(this);

			public IEnumerable<SqlError> Errors
			{
				get
				{
					var wrapper = (Func<object, SqlError>)CompiledWrappers[1];
					var e = GetEnumerator();

					while (e.MoveNext())
						yield return wrapper(e.Current);
				}
			}
		}

		[Wrapper]
		internal class SqlError : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get Number
				(Expression<Func<SqlError, int>>)((SqlError this_) => this_.Number),
			};

			public SqlError(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public int Number => ((Func<SqlError, int>)CompiledWrappers[0])(this);
		}
		#endregion

		[Wrapper]
		internal class SqlCommandBuilder : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: QuoteIdentifier
				(Expression<Func<SqlCommandBuilder, string, string>>)((SqlCommandBuilder this_, string identifier) => this_.QuoteIdentifier(identifier)),
			};

			public SqlCommandBuilder(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlCommandBuilder() => throw new NotImplementedException();

			public string QuoteIdentifier(string unquotedIdentifier) => ((Func<SqlCommandBuilder, string, string>)CompiledWrappers[0])(this, unquotedIdentifier);
		}

		[Wrapper]
		private class SqlParameter
		{
			// string return type is correct, TypeName and UdtTypeName return empty string instead of null
			public string    UdtTypeName { get; set; } = null!;
			public string    TypeName    { get; set; } = null!;
			public SqlDbType SqlDbType   { get; set; }
		}

		[Wrapper]
		public class SqlConnectionStringBuilder : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get MultipleActiveResultSets
				(Expression<Func<SqlConnectionStringBuilder, bool>>)((SqlConnectionStringBuilder this_) => this_.MultipleActiveResultSets),
				// [1]: set MultipleActiveResultSets
				PropertySetter((SqlConnectionStringBuilder this_) => this_.MultipleActiveResultSets),
			};

			public SqlConnectionStringBuilder(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlConnectionStringBuilder(string connectionString) => throw new NotImplementedException();

			public bool MultipleActiveResultSets
			{
				get => ((Func<SqlConnectionStringBuilder, bool>)CompiledWrappers[0])(this);
				set => ((Action<SqlConnectionStringBuilder, bool>)CompiledWrappers[1])(this, value);
			}
		}

		[Wrapper]
		public class SqlConnection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get ServerVersion
				(Expression<Func<SqlConnection, string>>)((SqlConnection this_) => this_.ServerVersion),
				// [1]: CreateCommand
				(Expression<Func<SqlConnection, IDbCommand>>)((SqlConnection this_) => this_.CreateCommand()),
				// [2]: Open
				(Expression<Action<SqlConnection>>)((SqlConnection this_) => this_.Open()),
				// [3]: Dispose
				(Expression<Action<SqlConnection>>)((SqlConnection this_) => this_.Dispose()),
			};

			public SqlConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlConnection(string connectionString) => throw new NotImplementedException();

			public string     ServerVersion   => ((Func<SqlConnection, string>)CompiledWrappers[0])(this);
			public IDbCommand CreateCommand() => ((Func<SqlConnection, IDbCommand>)CompiledWrappers[1])(this);
			public void       Open()          => ((Action<SqlConnection>)CompiledWrappers[2])(this);
			public void       Dispose()       => ((Action<SqlConnection>)CompiledWrappers[3])(this);
		}

		[Wrapper]
		public class SqlTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		public class SqlBulkCopy : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<SqlBulkCopy>>)((SqlBulkCopy this_) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<SqlBulkCopy, IDataReader>>)((SqlBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<SqlBulkCopy, int>>)((SqlBulkCopy this_) => this_.NotifyAfter),
				// [3]: get BatchSize
				(Expression<Func<SqlBulkCopy, int>>)((SqlBulkCopy this_) => this_.BatchSize),
				// [4]: get BulkCopyTimeout
				(Expression<Func<SqlBulkCopy, int>>)((SqlBulkCopy this_) => this_.BulkCopyTimeout),
				// [5]: get DestinationTableName
				(Expression<Func<SqlBulkCopy, string?>>)((SqlBulkCopy this_) => this_.DestinationTableName),
				// [6]: get ColumnMappings
				(Expression<Func<SqlBulkCopy, SqlBulkCopyColumnMappingCollection>>)((SqlBulkCopy this_) => this_.ColumnMappings),
				// [7]: set NotifyAfter
				PropertySetter((SqlBulkCopy this_) => this_.NotifyAfter),
				// [8]: set BatchSize
				PropertySetter((SqlBulkCopy this_) => this_.BatchSize),
				// [9]: set BulkCopyTimeout
				PropertySetter((SqlBulkCopy this_) => this_.BulkCopyTimeout),
				// [10]: set DestinationTableName
				PropertySetter((SqlBulkCopy this_) => this_.DestinationTableName),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(SqlRowsCopied)
			};

			public SqlBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlBulkCopy(SqlConnection connection, SqlBulkCopyOptions options, SqlTransaction? transaction) => throw new NotImplementedException();

			void IDisposable.Dispose()                        => ((Action<SqlBulkCopy>)CompiledWrappers[0])(this);
			public void WriteToServer(IDataReader dataReader) => ((Action<SqlBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);

			public int NotifyAfter
			{
				get => ((Func<SqlBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public int BatchSize
			{
				get => ((Func<SqlBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[8])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func<SqlBulkCopy, int>)CompiledWrappers[4])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[9])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func<SqlBulkCopy, string?>)CompiledWrappers[5])(this);
				set => ((Action<SqlBulkCopy, string?>)CompiledWrappers[10])(this, value);
			}

			public SqlBulkCopyColumnMappingCollection ColumnMappings => ((Func<SqlBulkCopy, SqlBulkCopyColumnMappingCollection>) CompiledWrappers[6])(this);

			private      SqlRowsCopiedEventHandler? _SqlRowsCopied;
			public event SqlRowsCopiedEventHandler   SqlRowsCopied
			{
				add    => _SqlRowsCopied = (SqlRowsCopiedEventHandler)Delegate.Combine(_SqlRowsCopied, value);
				remove => _SqlRowsCopied = (SqlRowsCopiedEventHandler)Delegate.Remove (_SqlRowsCopied, value);
			}
		}

		[Wrapper]
		public class SqlRowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<SqlRowsCopiedEventArgs, long>>)((SqlRowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<SqlRowsCopiedEventArgs, bool>>)((SqlRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set Abort
				PropertySetter((SqlRowsCopiedEventArgs this_) => this_.Abort),
			};

			public SqlRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public long RowsCopied => ((Func<SqlRowsCopiedEventArgs, long>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func<SqlRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => ((Action<SqlRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
			}
		}

		[Wrapper]
		public delegate void SqlRowsCopiedEventHandler(object sender, SqlRowsCopiedEventArgs e);

		[Wrapper]
		public class SqlBulkCopyColumnMappingCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<SqlBulkCopyColumnMappingCollection, SqlBulkCopyColumnMapping, SqlBulkCopyColumnMapping>>)((SqlBulkCopyColumnMappingCollection this_, SqlBulkCopyColumnMapping column) => this_.Add(column)),
			};

			public SqlBulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public SqlBulkCopyColumnMapping Add(SqlBulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<SqlBulkCopyColumnMappingCollection, SqlBulkCopyColumnMapping, SqlBulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
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
			public SqlBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public SqlBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
		#endregion
	}
}
