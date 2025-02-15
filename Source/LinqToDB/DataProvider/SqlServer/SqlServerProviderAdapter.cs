﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SqlServer
{
	// old System.Data.SqlClient versions for .net core (< 4.5.0)
	// miss UDT and BulkCopy support
	// We don't take it into account, as there is no reason to use such old provider versions
	public class SqlServerProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _sysSyncRoot = new ();
		private static readonly object _msSyncRoot  = new ();

		private static SqlServerProviderAdapter? _systemAdapter;
		private static SqlServerProviderAdapter? _microsoftAdapter;

		public const string SystemAssemblyName           = "System.Data.SqlClient";
		public const string SystemClientNamespace        = "System.Data.SqlClient";
		public const string SystemProviderFactoryName    = "System.Data.SqlClient";

		public const string MicrosoftAssemblyName        = "Microsoft.Data.SqlClient";
		public const string MicrosoftClientNamespace     = "Microsoft.Data.SqlClient";
		public const string MicrosoftProviderFactoryName = "Microsoft.Data.SqlClient";

		private SqlServerProviderAdapter(
			SqlServerProvider provider,

			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			Type sqlDataRecordType,
			Type sqlExceptionType,

			Action<DbParameter, SqlDbType> dbTypeSetter,
			Func  <DbParameter, SqlDbType> dbTypeGetter,
			Action<DbParameter, string> udtTypeNameSetter,
			Func  <DbParameter, string> udtTypeNameGetter,
			Action<DbParameter, string> typeNameSetter,
			Func  <DbParameter, string> typeNameGetter,

			Func<string, SqlConnectionStringBuilder> createConnectionStringBuilder,

			Func<DbConnection, SqlBulkCopyOptions, DbTransaction?, SqlBulkCopy> createBulkCopy,
			Func<int, string, SqlBulkCopyColumnMapping>                         createBulkCopyColumnMapping,
			
			MappingSchema? mappingSchema,
			Type? sqlJsonType)
		{
			Provider = provider;

			ConnectionType = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			SqlDataRecordType = sqlDataRecordType;
			SqlExceptionType  = sqlExceptionType;

			SetDbType      = dbTypeSetter;
			GetDbType      = dbTypeGetter;
			SetUdtTypeName = udtTypeNameSetter;
			GetUdtTypeName = udtTypeNameGetter;
			SetTypeName    = typeNameSetter;
			GetTypeName    = typeNameGetter;

			_createConnectionStringBuilder = createConnectionStringBuilder;

			_createBulkCopy              = createBulkCopy;
			_createBulkCopyColumnMapping = createBulkCopyColumnMapping;

			MappingSchema = mappingSchema;
			SqlJsonType   = sqlJsonType;
		}

		public SqlServerProvider Provider { get; }

#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

#endregion

		public Type SqlDataRecordType { get; }
		public Type SqlExceptionType  { get; }

		public string GetSqlXmlReaderMethod         => "GetSqlXml";
		public string GetDateTimeOffsetReaderMethod => "GetDateTimeOffset";
		public string GetTimeSpanReaderMethod       => "GetTimeSpan";

		public MappingSchema? MappingSchema { get; }

		public Type?   SqlJsonType { get; }
		public string? GetSqlJsonReaderMethod => SqlJsonType == null ? null : "GetSqlJson";
		public SqlDbType JsonDbType => SqlJsonType == null ? SqlDbType.NVarChar : (SqlDbType)35;

		private readonly Func<string, SqlConnectionStringBuilder> _createConnectionStringBuilder;
		public SqlConnectionStringBuilder CreateConnectionStringBuilder(string connectionString) => _createConnectionStringBuilder(connectionString);

		private readonly Func<DbConnection, SqlBulkCopyOptions, DbTransaction?, SqlBulkCopy> _createBulkCopy;
		internal SqlBulkCopy CreateBulkCopy(DbConnection connection, SqlBulkCopyOptions options, DbTransaction? transaction)
			=> _createBulkCopy(connection, options, transaction);

		private readonly Func<int, string, SqlBulkCopyColumnMapping> _createBulkCopyColumnMapping;
		public SqlBulkCopyColumnMapping CreateBulkCopyColumnMapping(int source, string destination)
			=> _createBulkCopyColumnMapping(source, destination);

		public Action<DbParameter, SqlDbType> SetDbType { get; }
		public Func  <DbParameter, SqlDbType> GetDbType { get; }

		public Action<DbParameter, string> SetUdtTypeName { get; }
		public Func  <DbParameter, string> GetUdtTypeName { get; }

		public Action<DbParameter, string> SetTypeName { get; }
		public Func  <DbParameter, string> GetTypeName { get; }

		public static SqlServerProviderAdapter GetInstance(SqlServerProvider provider)
		{
			if (provider == SqlServerProvider.SystemDataSqlClient)
			{
				if (_systemAdapter == null)
				{
					lock (_sysSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_systemAdapter ??= CreateAdapter(provider, SystemAssemblyName, SystemClientNamespace, SystemProviderFactoryName);
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _systemAdapter;
			}
			else
			{
				if (_microsoftAdapter == null)
				{
					lock (_msSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_microsoftAdapter ??= CreateAdapter(provider, MicrosoftAssemblyName, MicrosoftClientNamespace, MicrosoftProviderFactoryName);
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _microsoftAdapter;
			}
		}

		private static SqlServerProviderAdapter CreateAdapter(SqlServerProvider provider, string assemblyName, string clientNamespace, string factoryName)
		{
			var isSystem = assemblyName == SystemAssemblyName;

			Assembly? assembly;
#if NETFRAMEWORK
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

			var connectionType                 = assembly.GetType($"{clientNamespace}.SqlConnection"             , true)!;
			var parameterType                  = assembly.GetType($"{clientNamespace}.SqlParameter"              , true)!;
			var dataReaderType                 = assembly.GetType($"{clientNamespace}.SqlDataReader"             , true)!;
			var transactionType                = assembly.GetType($"{clientNamespace}.SqlTransaction"            , true)!;
			var commandType                    = assembly.GetType($"{clientNamespace}.SqlCommand"                , true)!;
			var sqlCommandBuilderType          = assembly.GetType($"{clientNamespace}.SqlCommandBuilder"         , true)!;
			var sqlConnectionStringBuilderType = assembly.GetType($"{clientNamespace}.SqlConnectionStringBuilder", true)!;
			var sqlExceptionType               = assembly.GetType($"{clientNamespace}.SqlException"              , true)!;
			var sqlErrorCollectionType         = assembly.GetType($"{clientNamespace}.SqlErrorCollection"        , true)!;
			var sqlErrorType                   = assembly.GetType($"{clientNamespace}.SqlError"                  , true)!;

			var sqlDataRecordType = connectionType.Assembly.GetType(
				isSystem
					? "Microsoft.SqlServer.Server.SqlDataRecord"
					: "Microsoft.Data.SqlClient.Server.SqlDataRecord",
				true)!;

			var bulkCopyType                        = assembly.GetType($"{clientNamespace}.SqlBulkCopy"                       , true)!;
			var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.SqlBulkCopyOptions"                , true)!;
			var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventHandler"         , true)!;
			var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMapping"          , true)!;
			var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.SqlBulkCopyColumnMappingCollection", true)!;
			var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.SqlRowsCopiedEventArgs"            , true)!;

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<SqlConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<SqlParameter>(parameterType);
			typeMapper.RegisterTypeWrapper<SqlTransaction>(transactionType);
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

			SqlServerTransientExceptionDetector.RegisterExceptionType(sqlExceptionType, exceptionErrorsGettter);

			var connectionFactory = typeMapper.BuildTypedFactory<string, SqlConnection, DbConnection>((string connectionString) => new SqlConnection(connectionString));

			MappingSchema? mappingSchema = null;
			Type?          sqlJsonType   = null;

			if (provider == SqlServerProvider.MicrosoftDataSqlClient)
			{
				sqlJsonType = LoadType("SqlJson", DataType.Json, null, true, true);
				if (sqlJsonType != null)
				{
					var sb = Expression.Parameter(typeof(StringBuilder));
					var dt = Expression.Parameter(typeof(SqlDataType));
					var op = Expression.Parameter(typeof(DataOptions));
					var v = Expression.Parameter(typeof(object));

					var converter = Expression.Lambda<Action<StringBuilder,SqlDataType,DataOptions,object>>(
						Expression.Call(
							null,
							Methods.SqlServer.ConvertStringToSql,
							sb,
							ExpressionHelper.Property(ExpressionHelper.Property(dt, nameof(SqlDataType.Type)), nameof(DbDataType.DataType)),
							ExpressionHelper.Property(Expression.Convert(v, sqlJsonType), "Value")
							),
						sb, dt, op, v)
						.CompileExpression();

					mappingSchema!.SetValueToSqlConverter(sqlJsonType, converter);

					// JsonDocument inlining
					var jsonDocumentType = Type.GetType("System.Text.Json.JsonDocument, System.Text.Json");

					if (jsonDocumentType != null)
					{
						mappingSchema.SetScalarType(jsonDocumentType);
						mappingSchema.SetDataType(jsonDocumentType, new SqlDataType(new DbDataType(jsonDocumentType, DataType.Json)));

						var jsdocConverter = Expression.Lambda<Action<StringBuilder,SqlDataType,DataOptions,object>>(
						Expression.Call(
							null,
							Methods.SqlServer.ConvertStringToSql,
							sb,
							ExpressionHelper.Property(ExpressionHelper.Property(dt, nameof(SqlDataType.Type)), nameof(DbDataType.DataType)),
							Expression.Call(ExpressionHelper.Property(Expression.Convert(v, jsonDocumentType), "RootElement"), "GetRawText", null)
							),
						sb, dt, op, v)
						.CompileExpression();

						mappingSchema!.SetValueToSqlConverter(jsonDocumentType, jsdocConverter);
					}

				}
			}

			return new SqlServerProviderAdapter(
				provider,

				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				connectionFactory,

				sqlDataRecordType,
				sqlExceptionType,

				dbTypeBuilder.BuildSetter<DbParameter>(),
				dbTypeBuilder.BuildGetter<DbParameter>(),
				udtTypeNameBuilder.BuildSetter<DbParameter>(),
				udtTypeNameBuilder.BuildGetter<DbParameter>(),
				typeNameBuilder.BuildSetter<DbParameter>(),
				typeNameBuilder.BuildGetter<DbParameter>(),

				typeMapper.BuildWrappedFactory((string connectionString) => new SqlConnectionStringBuilder(connectionString)),

				typeMapper.BuildWrappedFactory((DbConnection connection, SqlBulkCopyOptions options, DbTransaction? transaction) => new SqlBulkCopy((SqlConnection)(object)connection, options, (SqlTransaction?)(object?)transaction)),
				typeMapper.BuildWrappedFactory((int source, string destination) => new SqlBulkCopyColumnMapping(source, destination)),

				mappingSchema,
				sqlJsonType);

			IEnumerable<int> exceptionErrorsGettter(Exception ex) => typeMapper.Wrap<SqlException>(ex).Errors.Errors.Select(err => err.Number);

			Type? LoadType(string typeName, DataType dataType, string? dbType, bool optional = false, bool register = true)
			{
				var type = assembly!.GetType($"Microsoft.Data.SqlTypes.{typeName}", !optional);

				if (type == null)
					return null;

				if (register)
				{
					var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(ExpressionHelper.Property(type, "Null"), typeof(object))).CompileExpression();

					mappingSchema ??= new SqlServerAdapterMappingSchema(provider);

					mappingSchema.SetScalarType(type);
					mappingSchema.SetDefaultValue(type, getNullValue());
					mappingSchema.SetCanBeNull(type, true);
					mappingSchema.SetDataType(type, new SqlDataType(new DbDataType(type, dataType, dbType)));
				}

				return type;
			}
		}

		sealed class SqlServerAdapterMappingSchema : LockedMappingSchema
		{
			public SqlServerAdapterMappingSchema(SqlServerProvider provider) : base($"SqlServerAdapter.{provider}")
			{
			}
		}

		#region Wrappers

		#region SqlException
		[Wrapper]
		internal sealed class SqlException : TypeWrapper
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
		internal sealed class SqlErrorCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: GetEnumerator
				(Expression<Func<SqlErrorCollection, IEnumerator>>)((SqlErrorCollection this_) => this_.GetEnumerator()),
				// [1]: SqlError wrapper
				(Expression<Func<object, SqlError>>               )((object error            ) => (SqlError)error),
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
						yield return wrapper(e.Current!);
				}
			}
		}

		[Wrapper]
		internal sealed class SqlError : TypeWrapper
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
		private sealed class SqlParameter
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
				get => ((Func  <SqlConnectionStringBuilder, bool>)CompiledWrappers[0])(this);
				set => ((Action<SqlConnectionStringBuilder, bool>)CompiledWrappers[1])(this, value);
			}
		}

		[Wrapper]
		internal class SqlConnection
		{
			public SqlConnection(string connectionString) => throw new NotImplementedException();
		}

		[Wrapper]
		public class SqlTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		internal class SqlBulkCopy : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<SqlBulkCopy>>                                  )((SqlBulkCopy this_                    ) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<SqlBulkCopy, IDataReader>>                     )((SqlBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<SqlBulkCopy, int>>                               )((SqlBulkCopy this_                    ) => this_.NotifyAfter),
				// [3]: get BatchSize
				(Expression<Func<SqlBulkCopy, int>>                               )((SqlBulkCopy this_                    ) => this_.BatchSize),
				// [4]: get BulkCopyTimeout
				(Expression<Func<SqlBulkCopy, int>>                               )((SqlBulkCopy this_                    ) => this_.BulkCopyTimeout),
				// [5]: get DestinationTableName
				(Expression<Func<SqlBulkCopy, string?>>                           )((SqlBulkCopy this_                    ) => this_.DestinationTableName),
				// [6]: get ColumnMappings
				(Expression<Func<SqlBulkCopy, SqlBulkCopyColumnMappingCollection>>)((SqlBulkCopy this_                    ) => this_.ColumnMappings),
				// [7]: set NotifyAfter
				PropertySetter((SqlBulkCopy this_) => this_.NotifyAfter),
				// [8]: set BatchSize
				PropertySetter((SqlBulkCopy this_) => this_.BatchSize),
				// [9]: set BulkCopyTimeout
				PropertySetter((SqlBulkCopy this_) => this_.BulkCopyTimeout),
				// [10]: set DestinationTableName
				PropertySetter((SqlBulkCopy this_) => this_.DestinationTableName),
				// [11]: WriteToServerAsync
				(Expression<Func<SqlBulkCopy, IDataReader, CancellationToken, Task>>)((SqlBulkCopy this_, IDataReader reader, CancellationToken token)
					=> this_.WriteToServerAsync(reader, token)),
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
#pragma warning disable RS0030 // API mapping must preserve type
			public void WriteToServer(IDataReader dataReader) => ((Action<SqlBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);
			public Task WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken)
				=> ((Func<SqlBulkCopy, IDataReader, CancellationToken, Task>)CompiledWrappers[11])(this, dataReader, cancellationToken);
#pragma warning restore RS0030 //  API mapping must preserve type

			public int NotifyAfter
			{
				get => ((Func  <SqlBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public int BatchSize
			{
				get => ((Func  <SqlBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[8])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func  <SqlBulkCopy, int>)CompiledWrappers[4])(this);
				set => ((Action<SqlBulkCopy, int>)CompiledWrappers[9])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func  <SqlBulkCopy, string?>)CompiledWrappers[5] )(this);
				set => ((Action<SqlBulkCopy, string?>)CompiledWrappers[10])(this, value);
			}

			public SqlBulkCopyColumnMappingCollection ColumnMappings => ((Func<SqlBulkCopy, SqlBulkCopyColumnMappingCollection>) CompiledWrappers[6])(this);

			private      SqlRowsCopiedEventHandler? _SqlRowsCopied;
			public event SqlRowsCopiedEventHandler?  SqlRowsCopied
			{
				add    => _SqlRowsCopied = (SqlRowsCopiedEventHandler?)Delegate.Combine(_SqlRowsCopied, value);
				remove => _SqlRowsCopied = (SqlRowsCopiedEventHandler?)Delegate.Remove (_SqlRowsCopied, value);
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
				get => ((Func  <SqlRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
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
