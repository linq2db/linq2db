using System;
using System.Data;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Linq;
	using System.Collections;
	using System.Collections.Generic;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	internal static class SqlServerWrappers
	{
		private static object _sysSyncRoot = new object();
		private static object _msSyncRoot  = new object();

		private static ISqlServerWrapper? _systemWrapper;
		private static ISqlServerWrapper? _msWrapper;

		// old System.Data.SqlClient versions for .net core (< 4.5.0)
		// miss UDT and BulkCopy support
		// We don't take it into account, as there is no reason to use such old provider versions
		internal interface ISqlServerWrapper
		{
			Type ParameterType     { get; }
			Type SqlDataRecordType { get; }
			Type DataReaderType    { get; }
			Type ConnectionType    { get; }
			Type TransactionType   { get; }
			Type SqlExceptionType  { get; }

			SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString);
			SqlBulkCopy                CreateBulkCopy               (IDbConnection connection, SqlBulkCopyOptions options, IDbTransaction? transaction);
			SqlBulkCopyColumnMapping   CreateBulkCopyColumnMapping  (int source, string destination);

			Action<IDbDataParameter, SqlDbType> TypeSetter { get; }
			Func<IDbDataParameter, SqlDbType>   TypeGetter { get; }

			Action<IDbDataParameter, string> UdtTypeNameSetter { get; }
			Func<IDbDataParameter, string>   UdtTypeNameGetter { get; }

			Action<IDbDataParameter, string> TypeNameSetter { get; }
			Func<IDbDataParameter, string>   TypeNameGetter { get; }

			string QuoteIdentifier(string identifier);

			SqlConnection CreateSqlConnection(string connectionString);
		}

		class SqlClientWrapper : ISqlServerWrapper
		{
			private readonly TypeMapper _typeMapper;

			private readonly Type _sqlDataRecordType;
			private readonly Type _connectionType;
			private readonly Type _transactionTypeType;
			private readonly Type _dataReaderType;
			private readonly Type _parameterType;
			private readonly Type _sqlExceptionType;

			private readonly Action<IDbDataParameter, SqlDbType> _typeSetter;
			private readonly Func<IDbDataParameter, SqlDbType>   _typeGetter;

			private readonly Action<IDbDataParameter, string> _udtTypeNameSetter;
			private readonly Func<IDbDataParameter, string>   _udtTypeNameGetter;

			private readonly Action<IDbDataParameter, string> _typeNameSetter;
			private readonly Func<IDbDataParameter, string>   _typeNameGetter;

			private readonly Func<string, string> _quoteIdentifier;

			SqlClientWrapper(
				TypeMapper typeMapper,
				Type connectionType,
				Type parameterType,
				Type dataReaderType,
				Type transactionTypeType,
				Type sqlDataRecordType,
				Type sqlExceptionType,
				Action<IDbDataParameter, SqlDbType> typeSetter,
				Func<IDbDataParameter, SqlDbType>   typeGetter,
				Action<IDbDataParameter, string> udtTypeNameSetter,
				Func<IDbDataParameter, string>   udtTypeNameGetter,
				Action<IDbDataParameter, string> typeNameSetter,
				Func<IDbDataParameter, string>   typeNameGetter,
				Func<string, string> quoteIdentifier)
			{
				_typeMapper          = typeMapper;
				_sqlDataRecordType   = sqlDataRecordType;
				_connectionType      = connectionType;
				_dataReaderType      = dataReaderType;
				_transactionTypeType = transactionTypeType;
				_parameterType       = parameterType;
				_sqlExceptionType    = sqlExceptionType;
				_typeSetter          = typeSetter;
				_typeGetter          = typeGetter;
				_udtTypeNameSetter   = udtTypeNameSetter;
				_udtTypeNameGetter   = udtTypeNameGetter;
				_typeNameSetter      = typeNameSetter;
				_typeNameGetter      = typeNameGetter;
				_quoteIdentifier     = quoteIdentifier;
			}

			Type ISqlServerWrapper.SqlDataRecordType => _sqlDataRecordType;
			Type ISqlServerWrapper.ConnectionType    => _connectionType;
			Type ISqlServerWrapper.TransactionType   => _transactionTypeType;
			Type ISqlServerWrapper.DataReaderType    => _dataReaderType;
			Type ISqlServerWrapper.ParameterType     => _parameterType;
			Type ISqlServerWrapper.SqlExceptionType  => _sqlExceptionType;

			Action<IDbDataParameter, SqlDbType> ISqlServerWrapper.TypeSetter => _typeSetter;
			Func<IDbDataParameter, SqlDbType> ISqlServerWrapper.TypeGetter   => _typeGetter;

			Action<IDbDataParameter, string> ISqlServerWrapper.UdtTypeNameSetter => _udtTypeNameSetter;
			Func<IDbDataParameter, string> ISqlServerWrapper.UdtTypeNameGetter   => _udtTypeNameGetter;

			Action<IDbDataParameter, string> ISqlServerWrapper.TypeNameSetter => _typeNameSetter;
			Func<IDbDataParameter, string> ISqlServerWrapper.TypeNameGetter   => _typeNameGetter;

			SqlConnection ISqlServerWrapper.CreateSqlConnection(string connectionString)
				=> _typeMapper!.CreateAndWrap(() => new SqlConnection(connectionString))!;

			SqlConnectionStringBuilder ISqlServerWrapper.CreateConnectionStringBuilder(string connectionString)
				=> _typeMapper!.CreateAndWrap(() => new SqlConnectionStringBuilder(connectionString))!;
			SqlBulkCopy ISqlServerWrapper.CreateBulkCopy(IDbConnection connection, SqlBulkCopyOptions options, IDbTransaction? transaction)
				=> _typeMapper!.CreateAndWrap(() => new SqlBulkCopy((SqlConnection)connection, options, (SqlTransaction?)transaction))!;
			SqlBulkCopyColumnMapping ISqlServerWrapper.CreateBulkCopyColumnMapping(int source, string destination)
				=> _typeMapper!.CreateAndWrap(() => new SqlBulkCopyColumnMapping(source, destination))!;

			string ISqlServerWrapper.QuoteIdentifier(string identifier) => _quoteIdentifier(identifier);

			internal static ISqlServerWrapper Initialize(MappingSchema mappingSchema, bool system)
			{
				var clientNamespace = system ? "System.Data.SqlClient" : "Microsoft.Data.SqlClient";

				Type connectionType;
				if (system)
				{
#if NET45 || NET46
					connectionType = typeof(System.Data.SqlClient.SqlConnection);
#else
					connectionType = Type.GetType("System.Data.SqlClient.SqlConnection, System.Data.SqlClient", true);
#endif
				}
				else
					connectionType = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient", true);

				var assembly = connectionType.Assembly;

				var parameterType   = assembly.GetType($"{clientNamespace}.SqlParameter", true);
				var dataReaderType  = assembly.GetType($"{clientNamespace}.SqlDataReader", true);
				var transactionType = assembly.GetType($"{clientNamespace}.SqlTransaction", true);

				var sqlCommandBuilderType          = assembly.GetType($"{clientNamespace}.SqlCommandBuilder", true);
				var sqlConnectionStringBuilderType = assembly.GetType($"{clientNamespace}.SqlConnectionStringBuilder", true);

				var sqlExceptionType       = assembly.GetType($"{clientNamespace}.SqlException", true);
				var sqlErrorCollectionType = assembly.GetType($"{clientNamespace}.SqlErrorCollection", true);
				var sqlErrorType           = assembly.GetType($"{clientNamespace}.SqlError", true);

				var sqlDataRecordType = connectionType.Assembly.GetType(
					system
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

				return new SqlClientWrapper(
					typeMapper,
					connectionType,
					parameterType,
					dataReaderType,
					transactionType,
					sqlDataRecordType,
					sqlExceptionType,
					dbTypeBuilder.BuildSetter<IDbDataParameter>(),
					dbTypeBuilder.BuildGetter<IDbDataParameter>(),
					udtTypeNameBuilder.BuildSetter<IDbDataParameter>(),
					udtTypeNameBuilder.BuildGetter<IDbDataParameter>(),
					typeNameBuilder.BuildSetter<IDbDataParameter>(),
					typeNameBuilder.BuildGetter<IDbDataParameter>(),
					builder.QuoteIdentifier);
			}
		}

		internal static ISqlServerWrapper Initialize(SqlServerProvider provider, MappingSchema mappingSchema)
		{
			if (provider == SqlServerProvider.SystemDataSqlClient)
			{
				if (_systemWrapper == null)
				{
					lock (_sysSyncRoot)
					{
						if (_systemWrapper == null)
						{
							_systemWrapper = SqlClientWrapper.Initialize(mappingSchema, true);
						}
					}
				}

				return _systemWrapper;
			}
			else
			{
				if (_msWrapper == null)
				{
					lock (_msSyncRoot)
					{
						if (_msWrapper == null)
						{
							_msWrapper = SqlClientWrapper.Initialize(mappingSchema, false);
						}
					}
				}

				return _msWrapper;
			}
		}

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
			public string UdtTypeName  { get; set; } = null!;
			public string TypeName     { get; set; } = null!;
			public SqlDbType SqlDbType { get; set; }
		}

		[Wrapper]
		internal class SqlConnectionStringBuilder : TypeWrapper
		{
			public SqlConnectionStringBuilder(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlConnectionStringBuilder(string connectionString) => throw new NotImplementedException();

			public bool MultipleActiveResultSets
			{
				get => this.Wrap        (t => t.MultipleActiveResultSets);
				set => this.SetPropValue(t => t.MultipleActiveResultSets, value);
			}
		}

		[Wrapper]
		internal class SqlConnection : TypeWrapper, IDisposable
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
		internal class SqlTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		internal class SqlBulkCopy : TypeWrapper, IDisposable
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
				get => this.Wrap        (t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BatchSize
			{
				get => this.Wrap        (t => t.BatchSize);
				set => this.SetPropValue(t => t.BatchSize, value);
			}

			public int BulkCopyTimeout
			{
				get => this.Wrap        (t => t.BulkCopyTimeout);
				set => this.SetPropValue(t => t.BulkCopyTimeout, value);
			}

			public string? DestinationTableName
			{
				get => this.Wrap        (t => t.DestinationTableName);
				set => this.SetPropValue(t => t.DestinationTableName, value);
			}

			public SqlBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event SqlRowsCopiedEventHandler SqlRowsCopied
			{
				add    => Events.AddHandler   (nameof(SqlRowsCopied), value);
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
				get => this.Wrap        (t => t.Abort);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		internal delegate void SqlRowsCopiedEventHandler(object sender, SqlRowsCopiedEventArgs e);

		[Wrapper]
		internal class SqlBulkCopyColumnMappingCollection : TypeWrapper
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
		internal class SqlBulkCopyColumnMapping : TypeWrapper
		{
			public SqlBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public SqlBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
