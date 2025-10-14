using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Internal.Expressions.Types;
using LinqToDB.SqlQuery;
using LinqToDB.Mapping;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	public sealed class ClickHouseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _octonicaSyncRoot = new ();
		private static readonly Lock _driverSyncRoot   = new ();
		private static readonly Lock _mysqlSyncRoot    = new ();

		private static ClickHouseProviderAdapter? _octonicaAdapter;
		private static ClickHouseProviderAdapter? _driverAdapter;
		private static ClickHouseProviderAdapter? _mysqlAdapter;

		public const string OctonicaAssemblyName        = "Octonica.ClickHouseClient";
		public const string OctonicaClientNamespace     = "Octonica.ClickHouseClient";
		public const string OctonicaProviderFactoryName = "Octonica.ClickHouseClient";

		public const string DriverAssemblyName           = "ClickHouse.Driver";
		public const string DriverClientNamespace        = "ClickHouse.Driver.ADO";
		public const string DriverProviderFactoryName    = "ClickHouse.Driver";
		public const string DriverProviderTypesNamespace = "ClickHouse.Driver.Numerics";

		private ClickHouseProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Func<string, DbConnection> connectionFactory,

			Type?                 driverDecimalType,
			Func<object, string>? driverDecimalToStringConverter,

			string? getDateTimeOffsetReaderMethod,
			string? getIPAddressReaderMethod,
			string? getSByteReaderMethod,
			string  getUInt16ReaderMethod,
			string  getUInt32ReaderMethod,
			string  getUInt64ReaderMethod,
			string? getBigIntegerReaderMethod,
			string? getDateOnlyReaderMethod,

			Func<DbConnection, DriverWrappers.ClickHouseBulkCopy                                       >? driverBulkCopyCreator,
			Func<DbConnection, string, OctonicaWrappers.ClickHouseColumnWriter                         >? octonicaCreateWriter,
			Func<DbConnection, string, CancellationToken, Task<OctonicaWrappers.ClickHouseColumnWriter>>? octonicaCreateWriterAsync,
			Func<Type, OctonicaWrappers.ClickHouseColumnSettings                                       >? octonicaColumnSettings,

			Func<string, DriverWrappers.ClickHouseConnectionStringBuilder>? driverConnectionStringBuilder,

			MappingSchema? mappingSchema)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			_connectionFactory = connectionFactory;

			DriverDecimalType              = driverDecimalType;
			DriverDecimalToStringConverter = driverDecimalToStringConverter;

			GetDateTimeOffsetReaderMethod = getDateTimeOffsetReaderMethod;
			GetIPAddressReaderMethod      = getIPAddressReaderMethod;
			GetUInt16ReaderMethod         = getUInt16ReaderMethod;
			GetUInt32ReaderMethod         = getUInt32ReaderMethod;
			GetUInt64ReaderMethod         = getUInt64ReaderMethod;
			GetSByteReaderMethod          = getSByteReaderMethod;
			GetBigIntegerReaderMethod     = getBigIntegerReaderMethod;
			GetDateOnlyReaderMethod       = getDateOnlyReaderMethod;

			DriverBulkCopyCreator     = driverBulkCopyCreator;
			OctonicaCreateWriter      = octonicaCreateWriter;
			OctonicaCreateWriterAsync = octonicaCreateWriterAsync;
			OctonicaColumnSettings    = octonicaColumnSettings;

			CreateDriverConnectionStringBuilder = driverConnectionStringBuilder;

			MappingSchema = mappingSchema;
		}

		private ClickHouseProviderAdapter(MySqlProviderAdapter mySqlProviderAdapter)
		{
			ConnectionType = mySqlProviderAdapter.ConnectionType;
			DataReaderType = mySqlProviderAdapter.DataReaderType;
			ParameterType  = mySqlProviderAdapter.ParameterType;
			CommandType    = mySqlProviderAdapter.CommandType;

			GetSByteReaderMethod        = mySqlProviderAdapter.GetSByteMethodName;
			GetUInt16ReaderMethod       = mySqlProviderAdapter.GetUInt16MethodName;
			GetUInt32ReaderMethod       = mySqlProviderAdapter.GetUInt32MethodName;
			GetUInt64ReaderMethod       = mySqlProviderAdapter.GetUInt64MethodName;
			GetMySqlDecimalReaderMethod = mySqlProviderAdapter.GetMySqlDecimalMethodName;

			_connectionFactory          = mySqlProviderAdapter.CreateConnection;
		}

#region IDynamicProviderAdapter

		public Type  ConnectionType  { get; }
		public Type  DataReaderType  { get; }
		public Type  ParameterType   { get; }
		public Type  CommandType     { get; }
		public Type? TransactionType => null;

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		public Type?                 DriverDecimalType              { get; }
		public Func<object, string>? DriverDecimalToStringConverter { get; }

		public MappingSchema? MappingSchema { get; }

		// BulkCopy
		internal bool SupportsBulkCopy => DriverBulkCopyCreator != null || OctonicaCreateWriter != null || OctonicaCreateWriterAsync != null;

		internal Func<DbConnection, DriverWrappers.ClickHouseBulkCopy                                       >? DriverBulkCopyCreator     { get; }
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
		internal string? GetMySqlDecimalReaderMethod   { get; }

		// Driver connection management
		internal Func<string, DriverWrappers.ClickHouseConnectionStringBuilder>? CreateDriverConnectionStringBuilder { get; }

		public static ClickHouseProviderAdapter GetInstance(ClickHouseProvider provider)
		{
			if (provider == ClickHouseProvider.Octonica)
			{
				if (_octonicaAdapter == null)
				{
					lock (_octonicaSyncRoot)
						// https://github.com/dotnet/roslyn-analyzers/issues/1649
#pragma warning disable CA1508 // Avoid dead conditional code
						_octonicaAdapter ??= CreateOctonicaAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _octonicaAdapter;
			}
			else if (provider == ClickHouseProvider.MySqlConnector)
			{
				if (_mysqlAdapter == null)
				{
					lock (_mysqlSyncRoot)
						// https://github.com/dotnet/roslyn-analyzers/issues/1649
#pragma warning disable CA1508 // Avoid dead conditional code
						_mysqlAdapter ??= new ClickHouseProviderAdapter(MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector));
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _mysqlAdapter;
			}
			else
			{
				if (_driverAdapter == null)
				{
					lock (_driverSyncRoot)
						// https://github.com/dotnet/roslyn-analyzers/issues/1649
#pragma warning disable CA1508 // Avoid dead conditional code
						_driverAdapter ??= CreateDriverAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _driverAdapter;
			}
		}

		private static ClickHouseProviderAdapter CreateDriverAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(DriverAssemblyName, DriverProviderFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {DriverAssemblyName}");

			var connectionType              = assembly.GetType($"{DriverClientNamespace}.ClickHouseConnection"             , true)!;
			var commandType                 = assembly.GetType($"{DriverClientNamespace}.ClickHouseCommand"                , true)!;
			var parameterType               = assembly.GetType($"{DriverClientNamespace}.Parameters.ClickHouseDbParameter" , true)!;
			var dataReaderType              = assembly.GetType($"{DriverClientNamespace}.Readers.ClickHouseDataReader"     , true)!;
			var connectionStringBuilderType = assembly.GetType($"{DriverClientNamespace}.ClickHouseConnectionStringBuilder", true)!;
			var bulkCopyType                = assembly.GetType($"ClickHouse.Driver.Copy.ClickHouseBulkCopy"                , true)!;
			var decimalType                 = assembly.GetType($"{DriverProviderTypesNamespace}.ClickHouseDecimal"         , true)!;

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<DriverWrappers.ClickHouseConnection             >(connectionType);
			typeMapper.RegisterTypeWrapper<DriverWrappers.ClickHouseConnectionStringBuilder>(connectionStringBuilderType);
			typeMapper.RegisterTypeWrapper<DriverWrappers.ClickHouseBulkCopy               >(bulkCopyType);
			typeMapper.RegisterTypeWrapper<DriverWrappers.ClickHouseDecimal                >(decimalType);
			typeMapper.FinalizeMappings();

			var mappingSchema = new MappingSchema();
			mappingSchema.AddScalarType(decimalType, new SqlDataType(new DbDataType(decimalType, DataType.Decimal256, null, null, 76, ClickHouseMappingSchema.DEFAULT_DECIMAL_SCALE)));

			var decimalConverter = typeMapper.BuildFunc<object, string>(typeMapper.MapLambda((object value) => ((DriverWrappers.ClickHouseDecimal)value).ToString(CultureInfo.InvariantCulture)));

			var connectionFactory = typeMapper.BuildTypedFactory<string, DriverWrappers.ClickHouseConnection, DbConnection>(connectionString => new DriverWrappers.ClickHouseConnection(connectionString));

			return new ClickHouseProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				connectionFactory,

				decimalType,
				decimalConverter,

				null,
				"GetIPAddress",
				"GetSByte",
				"GetUInt16",
				"GetUInt32",
				"GetUInt64",
				"GetBigInteger",
				null,

				typeMapper.BuildWrappedFactory((DbConnection connection) => new DriverWrappers.ClickHouseBulkCopy((DriverWrappers.ClickHouseConnection)(object)connection)),
				null,
				null,
				null,

				typeMapper.BuildWrappedFactory((string connectionString) => new DriverWrappers.ClickHouseConnectionStringBuilder(connectionString)),

				mappingSchema);
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
					typeMapper.MapExpression((DbConnection conn, string insertFormatCommand, CancellationToken cancellationToken) => typeMapper.WrapTask<OctonicaWrappers.ClickHouseColumnWriter>(((OctonicaWrappers.ClickHouseConnection)(object)conn).CreateColumnWriterAsync(insertFormatCommand, cancellationToken)), pConnection, pCommand, pToken),
					pConnection, pCommand, pToken)
				.CompileExpression();

			ClickHouseTransientExceptionDetector.RegisterExceptionType(sqlExceptionType, exceptionErrorsGettter);

			var connectionFactory = typeMapper.BuildTypedFactory<string, OctonicaWrappers.ClickHouseConnection, DbConnection>(connectionString => new OctonicaWrappers.ClickHouseConnection(connectionString));

			return new ClickHouseProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				connectionFactory,

				null,
				null,

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
		/// ClickHouse.Driver wrappers.
		/// </summary>
		internal static class DriverWrappers
		{
			[Wrapper]
			internal sealed class ClickHouseConnection
			{
				public ClickHouseConnection(string connectionString) => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class ClickHouseConnectionStringBuilder : TypeWrapper
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: get UseSession
					(Expression<Func<ClickHouseConnectionStringBuilder, bool>>)(this_ => this_.UseSession),
					// [1]: set UseSession
					PropertySetter((ClickHouseConnectionStringBuilder this_) => this_.UseSession),
					// [2]: ToString
					(Expression<Func<ClickHouseConnectionStringBuilder, string>>)(this_ => this_.ToString()!),
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
			internal sealed class ClickHouseDecimal
			{
				public string ToString(IFormatProvider provider) => throw new NotImplementedException();
			}

			[Wrapper]
			internal sealed class ClickHouseBulkCopy : TypeWrapper, IDisposable
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static object[] Wrappers { get; }
					= new []
				{
					// [0]: Dispose
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseBulkCopy>>)(this_ => ((IDisposable)this_).Dispose()), true),
					// [1]: get BatchSize
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, int>>)(this_ => this_.BatchSize), true),
					// [2]: set BatchSize
					new Tuple<LambdaExpression, bool>(PropertySetter((ClickHouseBulkCopy this_) => this_.BatchSize), true),
					// [3]: get MaxDegreeOfParallelism
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, int>>)(this_ => this_.MaxDegreeOfParallelism), true),
					// [4]: set MaxDegreeOfParallelism
					new Tuple<LambdaExpression, bool>(PropertySetter((ClickHouseBulkCopy this_) => this_.MaxDegreeOfParallelism), true),
					// [5]: get DestinationTableName
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, string?>>)(this_ => this_.DestinationTableName), true),
					// [6]: set DestinationTableName
					new Tuple<LambdaExpression, bool>(PropertySetter((ClickHouseBulkCopy this_) => this_.DestinationTableName), true),
					// [7]: get RowsWritten
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, long>>)(this_ => this_.RowsWritten), true),
					// [8]: WriteToServerAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, IDataReader, CancellationToken, Task>>)((this_, dataReader, cancellationToken) => this_.WriteToServerAsync(dataReader, cancellationToken)), true),
					// [9]: InitAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseBulkCopy, Task>>)(this_ => this_.InitAsync()), false),
					// [10]: ColumnNames { set; }
					new Tuple<LambdaExpression, bool>(PropertySetter((ClickHouseBulkCopy this_) => this_.ColumnNames), false),
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

				// 6.8.0+
				public bool HasInitAsync => CompiledWrappers[9] != null;
				public Task InitAsync() => ((Func<ClickHouseBulkCopy, Task>)CompiledWrappers[9])(this);
				public IReadOnlyCollection<string> ColumnNames
				{
					get => throw new InvalidOperationException($"get_ColumnNames is not mapped");
					set => ((Action<ClickHouseBulkCopy, IReadOnlyCollection<string>>)CompiledWrappers[10])(this, value);
				}
			}
		}

		/// <summary>
		/// Octonica.ClicHouseClient wappers.
		/// </summary>
		public static class OctonicaWrappers
		{
			[Wrapper]
			internal sealed class ClickHouseConnection
			{
				public ClickHouseConnection(string connectionString) => throw new NotImplementedException();

				public ClickHouseColumnWriter       CreateColumnWriter     (string insertFormatCommand                                     ) => throw new NotImplementedException();
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
			public class ClickHouseColumnWriter : TypeWrapper, IDisposable, IAsyncDisposable
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static object[] Wrappers { get; }
					= new object[]
				{
					// [0]: Dispose
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter>>)(this_ => this_.Dispose()), true),
					// [1]: EndWrite
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter>>)(this_ => this_.EndWrite()), true),
					// [2]: EndWriteAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseColumnWriter, CancellationToken, Task>>)((this_, cancellationToken) => this_.EndWriteAsync(cancellationToken)), true),
					// [3]: WriteTable
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter, IReadOnlyList<object>, int>>)((this_, columns, rowCount) => this_.WriteTable(columns, rowCount)), true),
					// [4]: WriteTableAsync
					new Tuple<LambdaExpression, bool>((Expression<Func<ClickHouseColumnWriter, IReadOnlyList<object>, int, CancellationToken, Task>>)((this_, columns, rowCount, cancellationToken) => this_.WriteTableAsync(columns, rowCount, cancellationToken)), true),

					// [5]: DisposeAsync
					new Tuple<LambdaExpression, bool>
					((Expression<Func<ClickHouseColumnWriter, ValueTask>>)(this_ => this_.DisposeAsync()), true),
					// [6]: ConfigureColumn
					new Tuple<LambdaExpression, bool>((Expression<Action<ClickHouseColumnWriter, int, ClickHouseColumnSettings>>)((this_, ordinal, columnSettings) => this_.ConfigureColumn(ordinal, columnSettings)), true),
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

				public ValueTask DisposeAsync() => ((Func<ClickHouseColumnWriter, ValueTask>)CompiledWrappers[5])(this);
			}

			[Wrapper]
			internal sealed class ClickHouseException : TypeWrapper
			{
				[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
				private static LambdaExpression[] Wrappers { get; } =
				[
						// [0]: get ErrorCode
						(Expression<Func<ClickHouseException, int>>)(this_ => this_.ErrorCode),
				];

				public ClickHouseException(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public int ErrorCode => ((Func<ClickHouseException, int>)CompiledWrappers[0])(this);
			}
		}
	}
}
