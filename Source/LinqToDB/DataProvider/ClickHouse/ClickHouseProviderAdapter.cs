﻿using System;
using System.Globalization;
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
using LinqToDB.DataProvider.MySql;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.ClickHouse
{
	public class ClickHouseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _octonicaSyncRoot = new ();
		private static readonly object _clientSyncRoot   = new ();
		private static readonly object _mysqlSyncRoot    = new ();

		private static ClickHouseProviderAdapter? _octonicaAdapter;
		private static ClickHouseProviderAdapter? _clientAdapter;
		private static ClickHouseProviderAdapter? _mysqlAdapter;

		public const string OctonicaAssemblyName        = "Octonica.ClickHouseClient";
		public const string OctonicaClientNamespace     = "Octonica.ClickHouseClient";
		public const string OctonicaProviderFactoryName = "Octonica.ClickHouseClient";

		public const string ClientAssemblyName        = "ClickHouse.Client";
		public const string ClientClientNamespace     = "ClickHouse.Client.ADO";
		public const string ClientProviderFactoryName = "ClickHouse.Client";

		private ClickHouseProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,

			string? getDateTimeOffsetReaderMethod,
			string? getIPAddressReaderMethod,
			string? getSByteReaderMethod,
			string  getUInt16ReaderMethod,
			string  getUInt32ReaderMethod,
			string  getUInt64ReaderMethod,
			string? getBigIntegerReaderMethod,
			string? getDateOnlyReaderMethod,

			Func<DbConnection, ClientWrappers.ClickHouseBulkCopy                                       >? clientBulkCopyCreator,
			Func<DbConnection, string, OctonicaWrappers.ClickHouseColumnWriter                         >? octonicaCreateWriter,
			Func<DbConnection, string, CancellationToken, Task<OctonicaWrappers.ClickHouseColumnWriter>>? octonicaCreateWriterAsync,
			Func<Type, OctonicaWrappers.ClickHouseColumnSettings                                       >? octonicaColumnSettings,
			
			Func<string, ClientWrappers.ClickHouseConnectionStringBuilder>? clientConnectionStringBuilder,
			Func<string, DbConnection>?                                     connectionCreator)
		{
			ConnectionType = connectionType;
			DataReaderType = dataReaderType;
			ParameterType  = parameterType;
			CommandType    = commandType;

			GetDateTimeOffsetReaderMethod = getDateTimeOffsetReaderMethod;
			GetIPAddressReaderMethod      = getIPAddressReaderMethod;
			GetUInt16ReaderMethod         = getUInt16ReaderMethod;
			GetUInt32ReaderMethod         = getUInt32ReaderMethod;
			GetUInt64ReaderMethod         = getUInt64ReaderMethod;
			GetSByteReaderMethod          = getSByteReaderMethod;
			GetBigIntegerReaderMethod     = getBigIntegerReaderMethod;
			GetDateOnlyReaderMethod       = getDateOnlyReaderMethod;

			ClientBulkCopyCreator     = clientBulkCopyCreator;
			OctonicaCreateWriter      = octonicaCreateWriter;
			OctonicaCreateWriterAsync = octonicaCreateWriterAsync;
			OctonicaColumnSettings    = octonicaColumnSettings;

			CreateClientConnectionStringBuilder = clientConnectionStringBuilder;
			CreateConnection                    = connectionCreator;
		}

		private ClickHouseProviderAdapter(MySqlProviderAdapter mySqlProviderAdapter)
		{
			ConnectionType = mySqlProviderAdapter.ConnectionType;
			DataReaderType = mySqlProviderAdapter.DataReaderType;
			ParameterType  = mySqlProviderAdapter.ParameterType;
			CommandType    = mySqlProviderAdapter.CommandType;

			GetSByteReaderMethod  = mySqlProviderAdapter.GetSByteMethodName;
			GetUInt16ReaderMethod = mySqlProviderAdapter.GetUInt16MethodName;
			GetUInt32ReaderMethod = mySqlProviderAdapter.GetUInt32MethodName;
			GetUInt64ReaderMethod = mySqlProviderAdapter.GetUInt64MethodName;
		}

		// IDynamicProviderAdapter
		public Type ConnectionType { get; }
		public Type DataReaderType { get; }
		public Type ParameterType  { get; }
		public Type CommandType    { get; }
		public Type? TransactionType => null;

		// BulkCopy
		internal bool SupportsBulkCopy => ClientBulkCopyCreator != null || OctonicaCreateWriter != null || OctonicaCreateWriterAsync != null;

		internal Func<DbConnection, ClientWrappers.ClickHouseBulkCopy                                       >? ClientBulkCopyCreator     { get; }
		internal Func<DbConnection, string, OctonicaWrappers.ClickHouseColumnWriter                         >? OctonicaCreateWriter      { get; }
		internal Func<DbConnection, string, CancellationToken, Task<OctonicaWrappers.ClickHouseColumnWriter>>? OctonicaCreateWriterAsync { get; }
		internal Func<Type, OctonicaWrappers.ClickHouseColumnSettings                                       >? OctonicaColumnSettings    { get; }

		// data reader custom methods (not all, only needed/used)
		internal string? GetDateTimeOffsetReaderMethod { get; }
		internal string? GetIPAddressReaderMethod      { get; }
		internal string? GetUInt16ReaderMethod         { get; }
		internal string? GetUInt32ReaderMethod         { get; }
		internal string? GetUInt64ReaderMethod         { get; }
		internal string? GetSByteReaderMethod          { get; }
		internal string? GetBigIntegerReaderMethod     { get; }
		internal string? GetDateOnlyReaderMethod       { get; }

		// Client connection management
		internal Func<string, ClientWrappers.ClickHouseConnectionStringBuilder>? CreateClientConnectionStringBuilder { get; }
		internal Func<string, DbConnection                                    >? CreateConnection                    { get; }

		public static ClickHouseProviderAdapter GetInstance(ClickHouseProvider provider)
		{
			if (provider == ClickHouseProvider.Octonica)
			{
				if (_octonicaAdapter == null)
					lock (_octonicaSyncRoot)
						if (_octonicaAdapter == null)
							_octonicaAdapter = CreateOctonicaAdapter();

				return _octonicaAdapter;
			}
			else if (provider == ClickHouseProvider.MySqlConnector)
			{
				if (_mysqlAdapter == null)
					lock (_mysqlSyncRoot)
						if (_mysqlAdapter == null)
							_mysqlAdapter = new ClickHouseProviderAdapter(MySqlProviderAdapter.GetInstance(ProviderName.MySqlConnector));

				return _mysqlAdapter;
			}
			else
			{
				if (_clientAdapter == null)
					lock (_clientSyncRoot)
						if (_clientAdapter == null)
							_clientAdapter = CreateClientAdapter();

				return _clientAdapter;
			}
		}

		private static ClickHouseProviderAdapter CreateClientAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(ClientAssemblyName, ClientProviderFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {ClientAssemblyName}");

			var connectionType              = assembly.GetType($"{ClientClientNamespace}.ClickHouseConnection"             , true)!;
			var commandType                 = assembly.GetType($"{ClientClientNamespace}.ClickHouseCommand"                , true)!;
			var parameterType               = assembly.GetType($"{ClientClientNamespace}.Parameters.ClickHouseDbParameter" , true)!;
			var dataReaderType              = assembly.GetType($"{ClientClientNamespace}.Readers.ClickHouseDataReader"     , true)!;
			var connectionStringBuilderType = assembly.GetType($"{ClientClientNamespace}.ClickHouseConnectionStringBuilder", true)!;
			var bulkCopyType                = assembly.GetType($"ClickHouse.Client.Copy.ClickHouseBulkCopy"                , true)!;

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<ClientWrappers.ClickHouseConnection             >(connectionType);
			typeMapper.RegisterTypeWrapper<ClientWrappers.ClickHouseConnectionStringBuilder>(connectionStringBuilderType);
			typeMapper.RegisterTypeWrapper<ClientWrappers.ClickHouseBulkCopy               >(bulkCopyType);
			typeMapper.FinalizeMappings();

			var connectionFactory = typeMapper.BuildWrappedFactory((string connectionString) => new ClientWrappers.ClickHouseConnection(connectionString));

			return new ClickHouseProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,

				null,
				"GetIPAddress",
				"GetSByte",
				"GetUInt16",
				"GetUInt32",
				"GetUInt64",
				"GetBigInteger",
				null,

				typeMapper.BuildWrappedFactory((DbConnection connection) => new ClientWrappers.ClickHouseBulkCopy((ClientWrappers.ClickHouseConnection)(object)connection)),
				null,
				null,
				null,

				typeMapper.BuildWrappedFactory((string connectionString) => new ClientWrappers.ClickHouseConnectionStringBuilder(connectionString)),
				cs => (DbConnection)connectionFactory(cs).instance_);
		}

		private static ClickHouseProviderAdapter CreateOctonicaAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(OctonicaAssemblyName, OctonicaProviderFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {OctonicaAssemblyName}");

			var connectionType     = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseConnection"           , true)!;
			var commandType        = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseCommand"              , true)!;
			var parameterType      = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseParameter"            , true)!;
			var dataReaderType     = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseDataReader"           , true)!;
			var sqlExceptionType   = assembly.GetType($"{OctonicaClientNamespace}.Exceptions.ClickHouseException" , true)!;
			var bulkCopyType       = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseColumnWriter"         , true)!;
			var columnSettingsType = assembly.GetType($"{OctonicaClientNamespace}.ClickHouseColumnSettings"       , true)!;

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<OctonicaWrappers.ClickHouseException     >(sqlExceptionType);
			typeMapper.RegisterTypeWrapper<OctonicaWrappers.ClickHouseConnection    >(connectionType);
			typeMapper.RegisterTypeWrapper<OctonicaWrappers.ClickHouseColumnWriter  >(bulkCopyType);
			typeMapper.RegisterTypeWrapper<OctonicaWrappers.ClickHouseColumnSettings>(columnSettingsType);
			typeMapper.FinalizeMappings();

			var pConnection = Expression.Parameter(typeof(DbConnection));
			var pCommand    = Expression.Parameter(typeof(string));
			var pToken      = Expression.Parameter(typeof(CancellationToken));

			var createColumnWriter = Expression.Lambda<Func<DbConnection, string, OctonicaWrappers.ClickHouseColumnWriter>>(
					typeMapper.MapExpression((DbConnection conn, string insertFormatCommand) => typeMapper.Wrap<OctonicaWrappers.ClickHouseColumnWriter>(((OctonicaWrappers.ClickHouseConnection)(object)conn).CreateColumnWriter(insertFormatCommand)), pConnection, pCommand),
					pConnection, pCommand)
				.CompileExpression();

			var createColumnWriterAsync = Expression.Lambda<Func<DbConnection, string, CancellationToken, Task<OctonicaWrappers.ClickHouseColumnWriter>>>(
					typeMapper.MapExpression((DbConnection conn, string insertFormatCommand, CancellationToken cancellationToken) => typeMapper.WrapTask<OctonicaWrappers.ClickHouseColumnWriter>(((OctonicaWrappers.ClickHouseConnection)(object)conn).CreateColumnWriterAsync(insertFormatCommand, cancellationToken), bulkCopyType, cancellationToken), pConnection, pCommand, pToken),
					pConnection, pCommand, pToken)
				.CompileExpression();

			ClickHouseTransientExceptionDetector.RegisterExceptionType(sqlExceptionType, exceptionErrorsGettter);

			return new ClickHouseProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,

				"GetDateTimeOffset",
				"GetIPAddress",
				"GetSByte",
				"GetUInt16",
				"GetUInt32",
				"GetUInt64",
				"GetBigInteger",
				dataReaderType.GetMethod("GetDate")?.Name,

				null,
				createColumnWriter,
				createColumnWriterAsync,
				typeMapper.BuildWrappedFactory((Type columnType) => new OctonicaWrappers.ClickHouseColumnSettings(columnType)),
				
				null,
				null);

			IEnumerable<int> exceptionErrorsGettter(Exception ex) => new[] { typeMapper.Wrap<OctonicaWrappers.ClickHouseException>(ex).ErrorCode };
		}

		/// <summary>
		/// ClickHouse.Client wrappers.
		/// </summary>
		internal static class ClientWrappers
		{
			[Wrapper]
			public class ClickHouseConnection : TypeWrapper, IDisposable
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Dispose
					(Expression<Action<ClickHouseConnection>>)((ClickHouseConnection this_) => ((IDisposable)this_).Dispose()),
					// [1]: get ConnectionString
					(Expression<Func<ClickHouseConnection, string>>)((ClickHouseConnection this_) => this_.ConnectionString),
				};

				public ClickHouseConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public ClickHouseConnection(string connectionString) => throw new NotImplementedException();

				public string ConnectionString => ((Func<ClickHouseConnection, string>)CompiledWrappers[1])(this);

				void IDisposable.Dispose() => ((Action<ClickHouseConnection>)CompiledWrappers[0])(this);
			}

			[Wrapper]
			public class ClickHouseConnectionStringBuilder : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: get UseSession
					(Expression<Func<ClickHouseConnectionStringBuilder, bool>>)((ClickHouseConnectionStringBuilder this_) => this_.UseSession),
					// [1]: set UseSession
					PropertySetter((ClickHouseConnectionStringBuilder this_) => this_.UseSession),
					// [2]: ToString
					(Expression<Func<ClickHouseConnectionStringBuilder, string>>)((ClickHouseConnectionStringBuilder this_) => this_.ToString()!),
				};

				public ClickHouseConnectionStringBuilder(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public ClickHouseConnectionStringBuilder(string connectionString) => throw new NotImplementedException();

				public bool UseSession
				{
					get => ((Func<ClickHouseConnectionStringBuilder, bool>)CompiledWrappers[0])(this);
					set => ((Action<ClickHouseConnectionStringBuilder, bool>)CompiledWrappers[1])(this, value);
				}

				public override string ToString() => ((Func<ClickHouseConnectionStringBuilder, string>)CompiledWrappers[2])(this);
			}

			[Wrapper]
			public class ClickHouseBulkCopy : TypeWrapper, IDisposable
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Dispose
					(Expression<Action<ClickHouseBulkCopy>>)((ClickHouseBulkCopy this_) => ((IDisposable)this_).Dispose()),
					// [1]: get BatchSize
					(Expression<Func<ClickHouseBulkCopy, int>>)((ClickHouseBulkCopy this_) => this_.BatchSize),
					// [2]: set BatchSize
					PropertySetter((ClickHouseBulkCopy this_) => this_.BatchSize),
					// [3]: get MaxDegreeOfParallelism
					(Expression<Func<ClickHouseBulkCopy, int>>)((ClickHouseBulkCopy this_) => this_.MaxDegreeOfParallelism),
					// [4]: set MaxDegreeOfParallelism
					PropertySetter((ClickHouseBulkCopy this_) => this_.MaxDegreeOfParallelism),
					// [5]: get DestinationTableName
					(Expression<Func<ClickHouseBulkCopy, string?>>)((ClickHouseBulkCopy this_) => this_.DestinationTableName),
					// [6]: set DestinationTableName
					PropertySetter((ClickHouseBulkCopy this_) => this_.DestinationTableName),
					// [7]: get RowsWritten
					(Expression<Func<ClickHouseBulkCopy, long>>)((ClickHouseBulkCopy this_) => this_.RowsWritten),
					// [8]: WriteToServerAsync
					(Expression<Func<ClickHouseBulkCopy, IDataReader, CancellationToken, Task>>)((ClickHouseBulkCopy this_, IDataReader dataReader, CancellationToken cancellationToken) => this_.WriteToServerAsync(dataReader, cancellationToken)),
				};

				public ClickHouseBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public ClickHouseBulkCopy(ClickHouseConnection connection) => throw new NotImplementedException();

				void IDisposable.Dispose() => ((Action<ClickHouseBulkCopy>)CompiledWrappers[0])(this);


				public int BatchSize
				{
					get => ((Func<ClickHouseBulkCopy, int  >)CompiledWrappers[1])(this);
					set => ((Action<ClickHouseBulkCopy, int>)CompiledWrappers[2])(this, value);
				}

				public int MaxDegreeOfParallelism
				{
					get => ((Func<ClickHouseBulkCopy, int  >)CompiledWrappers[3])(this);
					set => ((Action<ClickHouseBulkCopy, int>)CompiledWrappers[4])(this, value);
				}

				public string? DestinationTableName
				{
					get => ((Func<ClickHouseBulkCopy, string?  >)CompiledWrappers[5])(this);
					set => ((Action<ClickHouseBulkCopy, string?>)CompiledWrappers[6])(this, value);
				}

				public long RowsWritten
				{
					get => ((Func<ClickHouseBulkCopy, long>)CompiledWrappers[7])(this);
				}

#pragma warning disable RS0030 // API mapping must preserve type
				public Task WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken) => ((Func<ClickHouseBulkCopy, IDataReader, CancellationToken, Task>)CompiledWrappers[8])(this, dataReader, cancellationToken);
#pragma warning restore RS0030 //  API mapping must preserve type
			}
		}

		/// <summary>
		/// Octonica.ClicHouseClient wappers.
		/// </summary>
		public static class OctonicaWrappers
		{
			[Wrapper]
			internal class ClickHouseConnection
			{
				public ClickHouseColumnWriter       CreateColumnWriter(string insertFormatCommand)                                           => throw new NotImplementedException();
				public Task<ClickHouseColumnWriter> CreateColumnWriterAsync(string insertFormatCommand, CancellationToken cancellationToken) => throw new NotImplementedException();
			}

			[Wrapper]
			public class ClickHouseColumnSettings : TypeWrapper
			{
				public ClickHouseColumnSettings(object instance) : base(instance, null)
				{
				}

				public ClickHouseColumnSettings(Type columnType) => throw new NotImplementedException();
			}

			[Wrapper]
			public class ClickHouseColumnWriter : TypeWrapper, IDisposable,
#if NATIVE_ASYNC
				IAsyncDisposable
#else
				Async.IAsyncDisposable
#endif
			{
				private static object[] Wrappers { get; }
					= new object[]
				{
					// [0]: Dispose
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter>>)((ClickHouseColumnWriter this_) => this_.Dispose()), true),
					// [1]: EndWrite
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter>>)((ClickHouseColumnWriter this_) => this_.EndWrite()), true),
					// [2]: EndWriteAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseColumnWriter, CancellationToken, Task>>)((ClickHouseColumnWriter this_, CancellationToken cancellationToken) => this_.EndWriteAsync(cancellationToken)), true),
					// [3]: WriteTable
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter, IReadOnlyList<object>, int>>)((ClickHouseColumnWriter this_, IReadOnlyList<object> columns, int rowCount) => this_.WriteTable(columns, rowCount)), true),
					// [4]: WriteTableAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseColumnWriter, IReadOnlyList<object>, int, CancellationToken, Task>>)((ClickHouseColumnWriter this_, IReadOnlyList<object> columns, int rowCount, CancellationToken cancellationToken) => this_.WriteTableAsync(columns, rowCount, cancellationToken)), true),

#if NATIVE_ASYNC
					// [5]: DisposeAsync
					new Tuple<LambdaExpression, bool>
					((Expression<Func<ClickHouseColumnWriter, ValueTask>>)((ClickHouseColumnWriter this_) => this_.DisposeAsync()), true),
#else
					// [5]: DisposeAsync
					new Tuple<LambdaExpression, bool>
					((Expression<Func<ClickHouseColumnWriter, Task>>)((ClickHouseColumnWriter this_) => this_.DisposeAsync()), true),
#endif
					// [6]: ConfigureColumn
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter, int, ClickHouseColumnSettings>>)((ClickHouseColumnWriter this_, int ordinal, ClickHouseColumnSettings columnSettings) => this_.ConfigureColumn(ordinal, columnSettings)), true),
				};

				public ClickHouseColumnWriter(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public void Dispose()                                                                                         => ((Action<ClickHouseColumnWriter>)CompiledWrappers[0])(this);
				public void EndWrite()                                                                                        => ((Action<ClickHouseColumnWriter>)CompiledWrappers[1])(this);
				public Task EndWriteAsync(CancellationToken cancellationToken)                                                => ((Func<ClickHouseColumnWriter, CancellationToken, Task>)CompiledWrappers[2])(this, cancellationToken);
				public void WriteTable(IReadOnlyList<object> columns, int rowCount)                                           => ((Action<ClickHouseColumnWriter, IReadOnlyList<object>, int>)CompiledWrappers[3])(this, columns, rowCount);
				public Task WriteTableAsync(IReadOnlyList<object> columns, int rowCount, CancellationToken cancellationToken) => ((Func<ClickHouseColumnWriter, IReadOnlyList<object>, int, CancellationToken, Task>)CompiledWrappers[4])(this, columns, rowCount, cancellationToken);
				public void ConfigureColumn(int ordinal, ClickHouseColumnSettings columnSettings)                             => ((Action<ClickHouseColumnWriter, int, ClickHouseColumnSettings>)CompiledWrappers[6])(this, ordinal, columnSettings);

#if NATIVE_ASYNC
				public ValueTask DisposeAsync() => ((Func<ClickHouseColumnWriter, ValueTask>)CompiledWrappers[5])(this);
#else
				[return: CustomMapper(typeof(ValueTaskToTaskMapper))]
				public Task DisposeAsync() => ((Func<ClickHouseColumnWriter, Task>)CompiledWrappers[5])(this);
#endif
			}

			[Wrapper]
			internal class ClickHouseException : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
				// [0]: get ErrorCode
				(Expression<Func<ClickHouseException, int>>)((ClickHouseException this_) => this_.ErrorCode),
				};

				public ClickHouseException(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public int ErrorCode => ((Func<ClickHouseException, int>)CompiledWrappers[0])(this);
			}
		}
	}
}
