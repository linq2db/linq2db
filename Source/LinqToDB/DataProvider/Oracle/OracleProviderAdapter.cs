using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Xml;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;
using LinqToDB.Mapping;

#if !NET9_0_OR_GREATER
using Lock = System.Object;
#endif

namespace LinqToDB.DataProvider.Oracle
{
	public class OracleProviderAdapter : IDynamicProviderAdapter
	{
		const int NanosecondsPerTick = 100;
		private static readonly Type[] IndexParams = new[] {typeof(int) };

		private static readonly Lock                   _nativeSyncRoot = new ();
		private static          OracleProviderAdapter? _nativeAdapter;

		public const string NativeAssemblyName        = "Oracle.DataAccess";
		public const string NativeProviderFactoryName = "Oracle.DataAccess.Client";
		public const string NativeClientNamespace     = "Oracle.DataAccess.Client";
		public const string NativeTypesNamespace      = "Oracle.DataAccess.Types";

		private static readonly Lock                   _managedSyncRoot = new ();
		private static          OracleProviderAdapter? _managedAdapter;

		public const string ManagedAssemblyName    = "Oracle.ManagedDataAccess";
		public const string ManagedClientNamespace = "Oracle.ManagedDataAccess.Client";
		public const string ManagedTypesNamespace  = "Oracle.ManagedDataAccess.Types";

		private static readonly Lock                   _devartSyncRoot = new ();
		private static          OracleProviderAdapter? _devartAdapter;

		public const string DevartAssemblyName    = "Devart.Data.Oracle";
		public const string DevartClientNamespace = "Devart.Data.Oracle";
		public const string DevartTypesNamespace  = "Devart.Data.Oracle";
		public const string DevartFactoryName     = "Devart.Data.Oracle";

		private OracleProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			MappingSchema mappingSchema,
			bool bindingByNameEnabled,

			IReadOnlyList<(Type Type, string ReaderMethodName)> customReaders,

			Type oracleBFileType,
			Type oracleBinaryType,
			Type oracleBlobType,
			Type oracleClobType,
			Type oracleDateType,
			Type oracleDecimalType,
			Type oracleIntervalDSType,
			Type oracleIntervalYMType,
			Type oracleStringType,
			Type oracleTimeStampType,
			Type? oracleTimeStampLTZType,
			Type? oracleTimeStampTZType,
			Type oracleXmlTypeType,
			Type? oracleXmlStreamType,
			Type oracleRefCursorType,
			Type? oracleRefType,

			string typesNamespace,

			Action<DbParameter, OracleDbType>  dbTypeSetter,
			Func  <DbParameter, OracleDbType?> dbTypeGetter,

			Func<DbConnection, string>? hostNameGetter,
			Func<DbConnection, string>? databaseNameGetter,

			Action<DbCommand, bool>    bindByNameSetter,
			Action<DbCommand, int>?    arrayBindCountSetter,
			Action<DbCommand, int>?    initialLONGFetchSizeSetter,
			Func<DbCommand, int, int>? executeArray,

			Func<DateTimeOffset, string, object> createOracleTimeStampTZ,

			Expression<Func<DbDataReader, int, DateTimeOffset>>? readDateTimeOffsetFromOracleTimeStamp,
			Expression<Func<DbDataReader, int, DateTimeOffset>>  readDateTimeOffsetFromOracleTimeStampTZ,
			Expression<Func<DbDataReader, int, decimal>>?        readOracleDecimalToDecimalAdv,
			Expression<Func<DbDataReader, int, int>>?            readOracleDecimalToInt,
			Expression<Func<DbDataReader, int, long>>?           readOracleDecimalToLong,
			Expression<Func<DbDataReader, int, decimal>>?        readOracleDecimalToDecimal,

			IBulkCopyAdapter? bulkCopy)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			MappingSchema = mappingSchema;
			BindingByNameEnabled = bindingByNameEnabled;

			CustomReaders = customReaders;

			OracleBFileType        = oracleBFileType;
			OracleBinaryType       = oracleBinaryType;
			OracleBlobType         = oracleBlobType;
			OracleClobType         = oracleClobType;
			OracleDateType         = oracleDateType;
			OracleDecimalType      = oracleDecimalType;
			OracleIntervalDSType   = oracleIntervalDSType;
			OracleIntervalYMType   = oracleIntervalYMType;
			OracleStringType       = oracleStringType;
			OracleTimeStampType    = oracleTimeStampType;
			OracleTimeStampLTZType = oracleTimeStampLTZType;
			OracleTimeStampTZType  = oracleTimeStampTZType;
			OracleXmlTypeType      = oracleXmlTypeType;
			OracleXmlStreamType    = oracleXmlStreamType;
			OracleRefCursorType    = oracleRefCursorType;
			OracleRefType          = oracleRefType;

			ProviderTypesNamespace = typesNamespace;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			GetHostName     = hostNameGetter;
			GetDatabaseName = databaseNameGetter;

			SetBindByName           = bindByNameSetter;
			SetArrayBindCount       = arrayBindCountSetter;
			SetInitialLONGFetchSize = initialLONGFetchSizeSetter;
			ExecuteArray            = executeArray;

			_createOracleTimeStampTZ = createOracleTimeStampTZ;

			ReadDateTimeOffsetFromOracleTimeStamp    = readDateTimeOffsetFromOracleTimeStamp;
			ReadDateTimeOffsetFromOracleTimeStampTZ  = readDateTimeOffsetFromOracleTimeStampTZ;
			ReadOracleDecimalToDecimalAdv            = readOracleDecimalToDecimalAdv;
			ReadOracleDecimalToInt                   = readOracleDecimalToInt;
			ReadOracleDecimalToLong                  = readOracleDecimalToLong;
			ReadOracleDecimalToDecimal               = readOracleDecimalToDecimal;

			BulkCopy = bulkCopy;
		}

#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

#endregion

		public bool BindingByNameEnabled   { get; }
		public MappingSchema MappingSchema { get; }

		internal IReadOnlyList<(Type Type, string ReaderMethodName)> CustomReaders { get; }

		public Type  OracleBFileType        { get; }
		public Type  OracleBinaryType       { get; }
		public Type  OracleBlobType         { get; }
		public Type  OracleClobType         { get; }
		public Type  OracleDateType         { get; }
		public Type  OracleDecimalType      { get; }
		public Type  OracleIntervalDSType   { get; }
		public Type  OracleIntervalYMType   { get; }
		public Type  OracleStringType       { get; }
		public Type  OracleTimeStampType    { get; }
		public Type? OracleTimeStampLTZType { get; }
		public Type? OracleTimeStampTZType  { get; }
		public Type  OracleXmlTypeType      { get; }
		public Type  OracleRefCursorType    { get; }

		private Type? OracleXmlStreamType   { get; }
		private Type? OracleRefType         { get; }

		public string ProviderTypesNamespace { get; }

		internal Action<DbParameter, OracleDbType>  SetDbType { get; }
		internal Func  <DbParameter, OracleDbType?> GetDbType { get; }

		public Func<DbConnection, string>? GetHostName     { get; }
		public Func<DbConnection, string>? GetDatabaseName { get; }

		public Action<DbCommand, bool>    SetBindByName           { get; }
		public Action<DbCommand, int>?    SetArrayBindCount       { get; }
		public Action<DbCommand, int>?    SetInitialLONGFetchSize { get; }
		public Func<DbCommand, int, int>? ExecuteArray            { get; }

		public Expression<Func<DbDataReader, int, DateTimeOffset>>? ReadDateTimeOffsetFromOracleTimeStamp    { get; }
		public Expression<Func<DbDataReader, int, DateTimeOffset>>  ReadDateTimeOffsetFromOracleTimeStampTZ  { get; }
		public Expression<Func<DbDataReader, int, decimal>>?        ReadOracleDecimalToDecimalAdv            { get; }
		public Expression<Func<DbDataReader, int, int>>?            ReadOracleDecimalToInt                   { get; }
		public Expression<Func<DbDataReader, int, long>>?           ReadOracleDecimalToLong                  { get; }
		public Expression<Func<DbDataReader, int, decimal>>?        ReadOracleDecimalToDecimal               { get; }

		private readonly Func<DateTimeOffset, string, object> _createOracleTimeStampTZ;
		public object CreateOracleTimeStampTZ(DateTimeOffset dto, string offset) => _createOracleTimeStampTZ(dto, offset);

		internal IBulkCopyAdapter? BulkCopy { get; }

		internal interface IBulkCopyService : IDisposable
		{
			void AddColumn(int ordinal, ColumnDescriptor columnDescriptor);

			void Execute(IDataReader reader);
		}

		internal interface IBulkCopyAdapter
		{
			IBulkCopyService Create(
				DbConnection                connection,
				BulkCopyOptions             options,
				string                      table,
				string?                     schema,
				int?                        notifyAfter,
				Action<BulkCopyRowsCopied>? rowsCopiedCallback,
				BulkCopyRowsCopied          rowsCopiedArgs,
				int?                        batchSize,
				int?                        timeout);
		}

		private sealed class OracleBulkCopyAdapter : IBulkCopyAdapter
		{
			private readonly Func<DbConnection, OracleWrappers.OracleBulkCopyOptions, OracleWrappers.OracleBulkCopy> _create;
			private readonly Func<int, string, OracleWrappers.OracleBulkCopyColumnMapping> _createColumnMapping;

			internal OracleBulkCopyAdapter(
				Func<DbConnection, OracleWrappers.OracleBulkCopyOptions, OracleWrappers.OracleBulkCopy> bulkCopyCreator,
				Func<int, string, OracleWrappers.OracleBulkCopyColumnMapping> bulkCopyColumnMappingCreator)
			{
				_create = bulkCopyCreator;
				_createColumnMapping = bulkCopyColumnMappingCreator;
			}

			IBulkCopyService IBulkCopyAdapter.Create(
				DbConnection connection,
				BulkCopyOptions options,
				string table,
				string? schema,
				int? notifyAfter,
				Action<BulkCopyRowsCopied>? rowsCopiedCallback,
				BulkCopyRowsCopied rowsCopiedArgs,
				int? batchSize,
				int? timeout)
			{
				var providerOptions = OracleWrappers.OracleBulkCopyOptions.Default;
				if (options.HasFlag(BulkCopyOptions.UseInternalTransaction))
					providerOptions |= OracleWrappers.OracleBulkCopyOptions.UseInternalTransaction;

				var bc = _create(connection, providerOptions);

				bc.DestinationTableName = table;
				bc.DestinationSchemaName = schema;
				if (timeout != null)
					bc.BulkCopyTimeout = timeout.Value;
				if (batchSize != null)
					bc.BatchSize = batchSize.Value;

				if (notifyAfter != null && rowsCopiedCallback != null)
				{
					bc.NotifyAfter = notifyAfter.Value;

					bc.OracleRowsCopied += (sender, args) =>
					{
						rowsCopiedArgs.RowsCopied = args.RowsCopied;
						rowsCopiedCallback(rowsCopiedArgs);
						if (rowsCopiedArgs.Abort)
							args.Abort = true;
					};
				}

				return new BulkCopyWrapper(bc, _createColumnMapping);
			}

			private sealed class BulkCopyWrapper : IBulkCopyService
			{
				private readonly OracleWrappers.OracleBulkCopy _bulkCopy;
				private readonly Func<int, string, OracleWrappers.OracleBulkCopyColumnMapping> _createColumnMapping;

				public BulkCopyWrapper(
					OracleWrappers.OracleBulkCopy bulkCopy,
					Func<int, string, OracleWrappers.OracleBulkCopyColumnMapping> bulkCopyColumnMappingCreator)
				{
					_bulkCopy = bulkCopy;
					_createColumnMapping = bulkCopyColumnMappingCreator;
				}

				void IBulkCopyService.AddColumn(int ordinal, ColumnDescriptor columnDescriptor)
				{
					_bulkCopy.ColumnMappings.Add(_createColumnMapping(ordinal, columnDescriptor.ColumnName));
				}

				void IDisposable.Dispose() => _bulkCopy.Dispose();

				void IBulkCopyService.Execute(IDataReader reader) => _bulkCopy.WriteToServer(reader);
			}
		}

		private sealed class DevArtBulkCopyAdapter : IBulkCopyAdapter
		{
			private readonly Func<string, DbConnection, DevartWrappers.OracleLoaderOptions, DevartWrappers.OracleLoader> _create;
			private readonly Func<DevartWrappers.OracleLoader, string, DevartWrappers.OracleDbType, int, int, int, string, int> _createColumnMapping;

			internal DevArtBulkCopyAdapter(
				Func<string, DbConnection, DevartWrappers.OracleLoaderOptions, DevartWrappers.OracleLoader>        bulkCopyCreator,
				Func<DevartWrappers.OracleLoader, string, DevartWrappers.OracleDbType, int, int, int, string, int> bulkCopyColumnMappingCreator)
			{
				_create              = bulkCopyCreator;
				_createColumnMapping = bulkCopyColumnMappingCreator;
			}

			IBulkCopyService IBulkCopyAdapter.Create(
				DbConnection connection,
				BulkCopyOptions options,
				string table,
				string? schema,
				int? notifyAfter,
				Action<BulkCopyRowsCopied>? rowsCopiedCallback,
				BulkCopyRowsCopied rowsCopiedArgs,
				int? batchSize,
				int? timeout)
			{
				if (schema != null)
					table = $"{schema}.{table}";

				var providerOptions = DevartWrappers.OracleLoaderOptions.Default;

				if (options.HasFlag(BulkCopyOptions.UseInternalTransaction))
					providerOptions |= DevartWrappers.OracleLoaderOptions.UseInternalTransaction;
				if (options.HasFlag(BulkCopyOptions.KeepConstraints))
					providerOptions |= DevartWrappers.OracleLoaderOptions.KeepConstraints;
				if (options.HasFlag(BulkCopyOptions.DisableTriggers))
					providerOptions |= DevartWrappers.OracleLoaderOptions.DisableTriggers;

				var bc = _create(table, connection, providerOptions);

				bc.BatchSize = batchSize;

				if (notifyAfter != null && rowsCopiedCallback != null)
				{
					bc.NotifyAfter = notifyAfter.Value;

					bc.RowsCopied += (sender, args) =>
					{
						rowsCopiedArgs.RowsCopied = args.RowsCopied;
						rowsCopiedCallback(rowsCopiedArgs);
						if (rowsCopiedArgs.Abort)
							args.Abort = true;
					};
				}

				return new BulkCopyWrapper(bc, _createColumnMapping);
			}

			private sealed class BulkCopyWrapper : IBulkCopyService
			{
				private readonly DevartWrappers.OracleLoader _bulkCopy;
				private readonly Func<DevartWrappers.OracleLoader, string, DevartWrappers.OracleDbType, int, int, int, string, int> _createColumnMapping;

				public BulkCopyWrapper(
					DevartWrappers.OracleLoader bulkCopy,
					Func<DevartWrappers.OracleLoader, string, DevartWrappers.OracleDbType, int, int, int, string, int> bulkCopyColumnMappingCreator)
				{
					_bulkCopy = bulkCopy;
					_createColumnMapping = bulkCopyColumnMappingCreator;
				}

				void IBulkCopyService.AddColumn(int ordinal, ColumnDescriptor columnDescriptor)
				{
					_createColumnMapping(_bulkCopy, columnDescriptor.ColumnName, DevartWrappers.GetDbType(columnDescriptor.GetConvertedDbDataType()), columnDescriptor.Length ?? 0, columnDescriptor.Precision ?? 0, columnDescriptor.Scale ?? 0, string.Empty);
				}

				void IDisposable.Dispose() => _bulkCopy.Dispose();

				void IBulkCopyService.Execute(IDataReader reader) => _bulkCopy.LoadTable(reader);
			}
		}

		public static OracleProviderAdapter GetInstance(OracleProvider provider)
		{
			if (provider == OracleProvider.Native)
			{
				if (_nativeAdapter == null)
				{
					lock (_nativeSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_nativeAdapter ??= CreateAdapter(NativeAssemblyName, NativeClientNamespace, NativeTypesNamespace, NativeProviderFactoryName, new OracleNativeClientAdapterMappingSchema());
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _nativeAdapter;
			}
			else if (provider == OracleProvider.Devart)
			{
				if (_devartAdapter == null)
				{
					lock (_devartSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_devartAdapter ??= CreateDevartAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _devartAdapter;
			}
			else
			{
				if (_managedAdapter == null)
				{
					lock (_managedSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_managedAdapter ??= CreateAdapter(ManagedAssemblyName, ManagedClientNamespace, ManagedTypesNamespace, null, new OracleManagedClientAdapterMappingSchema());
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _managedAdapter;
			}
		}

		sealed class OracleNativeClientAdapterMappingSchema : LockedMappingSchema
		{
			public OracleNativeClientAdapterMappingSchema() : base("OracleNativeClientAdapter")
			{
			}
		}

		sealed class OracleManagedClientAdapterMappingSchema : LockedMappingSchema
		{
			public OracleManagedClientAdapterMappingSchema() : base("OracleManagedClientAdapter")
			{
			}
		}

		sealed class OracleDevartClientAdapterMappingSchema : LockedMappingSchema
		{
			public OracleDevartClientAdapterMappingSchema() : base("OracleDevartClientAdapter")
			{
			}
		}

		static OracleProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string typesNamespace, string? factoryName, MappingSchema mappingSchema)
		{
			var isNative = assemblyName == NativeAssemblyName;

			var assembly = Common.Tools.TryLoadAssembly(assemblyName, factoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType  = assembly.GetType($"{clientNamespace}.OracleConnection" , true)!;
			var parameterType   = assembly.GetType($"{clientNamespace}.OracleParameter"  , true)!;
			var dataReaderType  = assembly.GetType($"{clientNamespace}.OracleDataReader" , true)!;
			var transactionType = assembly.GetType($"{clientNamespace}.OracleTransaction", true)!;
			var dbType          = assembly.GetType($"{clientNamespace}.OracleDbType"     , true)!;
			var commandType     = assembly.GetType($"{clientNamespace}.OracleCommand"    , true)!;

			var customReaders = new List<(Type Type, string ReaderMethodName)>();

			TryAddTypeReader<TimeSpan      >(dataReaderType, customReaders, "GetTimeSpan");
			TryAddTypeReader<DateTimeOffset>(dataReaderType, customReaders, "GetDateTimeOffset");
			TryAddTypeReader<XmlReader     >(dataReaderType, customReaders, "GetXmlReader");

			// with convert expression BFile fails e.g. ProcedureOutParameters test
			// first we need to figure out how to work with this type
			var oracleBFileType        = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleBFile"       , "GetOracleBFile"       , DataType.BFile, skipConvertExpression: true)!;
			var oracleBinaryType       = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleBinary"      , "GetOracleBinary"      , DataType.VarBinary)!;
			var oracleBlobType         = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleBlob"        , "GetOracleBlob"        , DataType.Blob)!;
			var oracleClobType         = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleClob"        , "GetOracleClob"        , DataType.NText)!;
			var oracleDateType         = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleDate"        , "GetOracleDate"        , DataType.DateTime)!;
			var oracleDecimalType      = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleDecimal"     , "GetOracleDecimal"     , DataType.Decimal)!;
			var oracleIntervalDSType   = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleIntervalDS"  , "GetOracleIntervalDS"  , DataType.Time)!;
			var oracleIntervalYMType   = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleIntervalYM"  , "GetOracleIntervalYM"  , DataType.Date)!;
			var oracleRefType          = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleRef"         , "GetOracleRef"         , DataType.Binary, optional: true);
			var oracleStringType       = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleString"      , "GetOracleString"      , DataType.NVarChar)!;
			var oracleTimeStampType    = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleTimeStamp"   , "GetOracleTimeStamp"   , DataType.DateTime2)!;
			var oracleTimeStampLTZType = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleTimeStampLTZ", "GetOracleTimeStampLTZ", DataType.DateTimeOffset)!;
			var oracleTimeStampTZType  = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleTimeStampTZ" , "GetOracleTimeStampTZ" , DataType.DateTimeOffset)!;
			var oracleXmlTypeType      = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleXmlType"     , "GetOracleXmlType"     , DataType.Xml)!;
			var oracleXmlStreamType    = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleXmlStream"   , null                   , DataType.Xml, optional: true, hasNull: false, hasIsNull: false)!;
			var oracleRefCursorType    = LoadType(assembly, mappingSchema, typesNamespace, customReaders, "OracleRefCursor"   , null                   , DataType.Cursor, hasValue: false)!;

			IBulkCopyAdapter? bulkCopy = null;
			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleConnection  >(connectionType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleParameter   >(parameterType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleDbType      >(dbType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleCommand     >(commandType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleDataReader  >(dataReaderType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleTimeStampTZ >(oracleTimeStampTZType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleTimeStampLTZ>(oracleTimeStampLTZType);
			typeMapper.RegisterTypeWrapper<OracleWrappers.OracleDecimal     >(oracleDecimalType);

			var bulkCopyType = assembly.GetType($"{clientNamespace}.OracleBulkCopy", false);
			if (bulkCopyType != null)
			{
				var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.OracleBulkCopyOptions", true)!;
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.OracleRowsCopiedEventHandler", true)!;
				var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.OracleBulkCopyColumnMapping", true)!;
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.OracleBulkCopyColumnMappingCollection", true)!;
				var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.OracleRowsCopiedEventArgs", true)!;

				// bulk copy types
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleBulkCopy>(bulkCopyType);
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleBulkCopyOptions>(bulkCopyOptionsType);
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleBulkCopyColumnMapping>(bulkCopyColumnMappingType);
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollectionType);
				typeMapper.RegisterTypeWrapper<OracleWrappers.OracleRowsCopiedEventArgs>(rowsCopiedEventArgsType);
				typeMapper.FinalizeMappings();

				bulkCopy = new OracleBulkCopyAdapter(
					typeMapper.BuildWrappedFactory((DbConnection connection, OracleWrappers.OracleBulkCopyOptions options) => new OracleWrappers.OracleBulkCopy((OracleWrappers.OracleConnection)(object)connection, options)),
					typeMapper.BuildWrappedFactory((int source, string destination) => new OracleWrappers.OracleBulkCopyColumnMapping(source, destination)));
			}
			else
				typeMapper.FinalizeMappings();

			var paramMapper      = typeMapper.Type<OracleWrappers.OracleParameter>();
			var dbTypeBuilder    = paramMapper.Member(p => p.OracleDbType);
			var connectionMapper = typeMapper.Type<OracleWrappers.OracleConnection>();
			var commandMapper    = typeMapper.Type<OracleWrappers.OracleCommand>();

			// data reader expressions
			// rd.GetOracleTimeStampTZ(i) => DateTimeOffset
			var generator    = new ExpressionGenerator(typeMapper);
			var rdParam      = Expression.Parameter(typeof(DbDataReader), "rd");
			var indexParam   = Expression.Parameter(typeof(int), "i");
			var tstzExpr     = generator.MapExpression((DbDataReader rd, int i) => ((OracleWrappers.OracleDataReader)(object)rd).GetOracleTimeStampTZ(i), rdParam, indexParam);
			var tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			var expr         = generator.MapExpression((OracleWrappers.OracleTimeStampTZ tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			var body = generator.Build();
			var readDateTimeOffsetFromOracleTimeStampTZ = (Expression<Func<DbDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

			// rd.GetOracleDecimal(i) => decimal
			var readOracleDecimal  = typeMapper.MapLambda<DbDataReader, int, OracleWrappers.OracleDecimal>((rd, i) => ((OracleWrappers.OracleDataReader)(object)rd).GetOracleDecimal(i));
			var oracleDecimalParam = Expression.Parameter(readOracleDecimal.ReturnType, "dec");

			generator = new ExpressionGenerator(typeMapper);
			var precision  = generator.AssignToVariable(ExpressionInstances.Constant29, "precision");
			var decimalVar = generator.AddVariable(Expression.Parameter(typeof(decimal), "dec"));
			var label      = Expression.Label(typeof(decimal));

			generator.AddExpression(
				Expression.Loop(
					Expression.TryCatch(
						Expression.Block(
							Expression.Assign(oracleDecimalParam, generator.MapExpression((OracleWrappers.OracleDecimal d, int p) => OracleWrappers.OracleDecimal.SetPrecision(d, p), oracleDecimalParam, precision)),
							Expression.Assign(decimalVar, Expression.Convert(oracleDecimalParam, typeof(decimal))),
							Expression.Break(label, decimalVar)),
						Expression.Catch(
							typeof(OverflowException),
							Expression.Block(
								Expression.IfThen(
									Expression.LessThanOrEqual(Expression.SubtractAssign(precision, ExpressionInstances.Constant1), ExpressionInstances.Constant26),
									Expression.Rethrow()))),
						// since 23.5 exception thrown is InvalidCastException
						Expression.Catch(
							typeof(InvalidCastException),
							Expression.Block(
								Expression.IfThen(
									Expression.LessThanOrEqual(Expression.SubtractAssign(precision, ExpressionInstances.Constant1), ExpressionInstances.Constant26),
									Expression.Rethrow())))),
					label));

			body = generator.Build();

			// workaround for mapper issue with complex reader expressions handling
			// https://github.com/linq2db/linq2db/issues/2032
			var compiledReader                = Expression.Lambda(body, oracleDecimalParam).CompileExpression();
			var readOracleDecimalToDecimalAdv = (Expression<Func<DbDataReader, int, decimal>>)Expression.Lambda(
				Expression.Invoke(
					Expression.Constant(compiledReader),
					readOracleDecimal.GetBody(rdParam, indexParam)),
				rdParam,
				indexParam);

			var readOracleDecimalToInt     = (Expression<Func<DbDataReader, int, int>>)typeMapper.MapLambda<DbDataReader, int, int>((rd, i) => (int)(decimal)OracleWrappers.OracleDecimal.SetPrecision(((OracleWrappers.OracleDataReader)(object)rd).GetOracleDecimal(i), 27));
			var readOracleDecimalToLong    = (Expression<Func<DbDataReader, int, long>>)typeMapper.MapLambda<DbDataReader, int, long>((rd, i) => (long)(decimal)OracleWrappers.OracleDecimal.SetPrecision(((OracleWrappers.OracleDataReader)(object)rd).GetOracleDecimal(i), 27));
			var readOracleDecimalToDecimal = (Expression<Func<DbDataReader, int, decimal>>)typeMapper.MapLambda<DbDataReader, int, decimal>((rd, i) => (decimal)OracleWrappers.OracleDecimal.SetPrecision(((OracleWrappers.OracleDataReader)(object)rd).GetOracleDecimal(i), 27));

			var dbTypeSetter = dbTypeBuilder.BuildSetter<DbParameter>();
			var dbTypeGetter = dbTypeBuilder.BuildGetter<DbParameter>();

			var connectionFactory = typeMapper.BuildTypedFactory<string, OracleWrappers.OracleConnection, DbConnection>((string connectionString) => new OracleWrappers.OracleConnection(connectionString));

			return new OracleProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				connectionFactory,

				mappingSchema,
				assemblyName != ManagedAssemblyName,

				customReaders,

				oracleBFileType,
				oracleBinaryType,
				oracleBlobType,
				oracleClobType,
				oracleDateType,
				oracleDecimalType,
				oracleIntervalDSType,
				oracleIntervalYMType,
				oracleStringType,
				oracleTimeStampType,
				oracleTimeStampLTZType,
				oracleTimeStampTZType,
				oracleXmlTypeType,
				oracleXmlStreamType,
				oracleRefCursorType,
				oracleRefType,

				typesNamespace,

				(p, t) =>
				{
					var convertedType = OracleWrappers.ConvertDbType(t);
					if (convertedType != null)
						dbTypeSetter(p, convertedType.Value);
				},
				p => OracleWrappers.ConvertDbType(dbTypeGetter(p)),

				connectionMapper.Member(c => c.HostName).BuildGetter<DbConnection>(),
				connectionMapper.Member(c => c.ServiceName).BuildGetter<DbConnection>(),

				commandMapper.Member(p => p.BindByName).BuildSetter<DbCommand>(),
				commandMapper.Member(p => p.ArrayBindCount).BuildSetter<DbCommand>(),
				commandMapper.Member(p => p.InitialLONGFetchSize).BuildSetter<DbCommand>(),
				null,

				typeMapper.BuildFactory((DateTimeOffset dto, string offset) => new OracleWrappers.OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset)),

				null,
				readDateTimeOffsetFromOracleTimeStampTZ,
				readOracleDecimalToDecimalAdv,
				readOracleDecimalToInt,
				readOracleDecimalToLong,
				readOracleDecimalToDecimal,
				bulkCopy);
		}

		static OracleProviderAdapter CreateDevartAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(DevartAssemblyName, DevartFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {DevartAssemblyName}");

			var mappingSchema = new OracleDevartClientAdapterMappingSchema();

			var connectionType  = assembly.GetType($"{DevartClientNamespace}.OracleConnection" , true)!;
			var parameterType   = assembly.GetType($"{DevartClientNamespace}.OracleParameter"  , true)!;
			var dataReaderType  = assembly.GetType($"{DevartClientNamespace}.OracleDataReader" , true)!;
			var transactionType = assembly.GetType($"{DevartClientNamespace}.OracleTransaction", true)!;
			var dbType          = assembly.GetType($"{DevartClientNamespace}.OracleDbType"     , true)!;
			var commandType     = assembly.GetType($"{DevartClientNamespace}.OracleCommand"    , true)!;

			var customReaders = new List<(Type Type, string ReaderMethodName)>();

			TryAddTypeReader<TimeSpan>(dataReaderType, customReaders, "GetTimeSpan");
			TryAddTypeReader<DateTimeOffset>(dataReaderType, customReaders, "GetDateTimeOffset");

			// following types (with getters) are not mapped as they are obsoleted:
			// OracleDateTime+GetOracleDateTime
			// OracleBoolean (no read method)
			// OracleMonthSpan GetOracleMonthSpan
			// OracleTimeSpan GetOracleTimeSpan

			var oracleBFileType        = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleBFile"        , "GetOracleBFile"       , DataType.BFile)!;
			var oracleBinaryType       = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleBinary"       , "GetOracleBinary"      , DataType.VarBinary)!;
			var oracleDateType         = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleDate"         , "GetOracleDate"        , DataType.DateTime)!;
			var oracleIntervalDSType   = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleIntervalDS"   , "GetOracleIntervalDS"  , DataType.Time)!;
			var oracleIntervalYMType   = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleIntervalYM"   , "GetOracleIntervalYM"  , DataType.Date)!;
			var oracleStringType       = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleString"       , "GetOracleString"      , DataType.NVarChar)!;
			var oracleTimeStampType    = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleTimeStamp"    , "GetOracleTimeStamp"   , DataType.DateTime2)!;
			var oracleRefType          = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleRef"          , "GetOracleRef"         , DataType.Binary)!;
			var nativeOracleArrayType  = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "NativeOracleArray"  , "GetNativeOracleArray" , DataType.Undefined, hasNull: false)!;
			var nativeOracleObjectType = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "NativeOracleObject" , "GetNativeOracleObject", DataType.Undefined, hasNull: false, hasValue: false)!;
			var nativeOracleTableType  = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "NativeOracleTable"  , "GetNativeOracleTable" , DataType.Undefined, hasNull: false)!;
			var oracleAnyDataType      = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleAnyData"      , "GetOracleAnyData"     , DataType.Variant)!;
			var oracleArrayType        = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleArray"        , "GetOracleArray"       , DataType.Undefined, hasValue: false)!;
			var oracleCursorType       = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleCursor"       , "GetOracleCursor"      , DataType.Cursor, hasValue: false)!;
			var oracleObjectType       = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleObject"       , "GetOracleObject"      , DataType.Undefined, hasValue: false)!;
			var oracleTableType        = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleTable"        , "GetOracleTable"       , DataType.Undefined, hasValue: false)!;
			// Null is obsolete
			var oracleLobType          = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleLob"          , "GetOracleLob"         , DataType.Blob, hasNull: false)!;
			var oracleXmlType          = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleXml"          , "GetOracleXml"         , DataType.Xml)!;
			var oracleNumberType       = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleNumber"       , "GetOracleNumber"      , DataType.Decimal)!;
			// actually this is not a data type, but type information
			// but nothing prevents us from such mapping
			var oracleTypeType         = LoadType(assembly, mappingSchema, DevartTypesNamespace, customReaders, "OracleType"         , "GetObjectType"        , DataType.Undefined, hasNull: false, hasValue: false, hasIsNull: false)!;

			IBulkCopyAdapter? bulkCopy = null;
			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleParameter >(parameterType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleDbType    >(dbType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleCommand   >(commandType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleDataReader>(dataReaderType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleTimeStamp >(oracleTimeStampType);
			typeMapper.RegisterTypeWrapper<DevartWrappers.OracleNumber    >(oracleNumberType);

			var bulkCopyType = assembly.GetType($"{DevartClientNamespace}.OracleLoader", false);
			if (bulkCopyType != null)
			{
				var bulkCopyOptionsType                 = assembly.GetType($"{DevartClientNamespace}.OracleLoaderOptions", true)!;
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{DevartClientNamespace}.OracleLoaderRowsCopiedEventHandler", true)!;
				var bulkCopyColumnMappingType           = assembly.GetType($"{DevartClientNamespace}.OracleLoaderColumn", true)!;
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{DevartClientNamespace}.OracleLoaderColumnCollection", true)!;
				var rowsCopiedEventArgsType             = assembly.GetType($"{DevartClientNamespace}.OracleLoaderRowsCopiedEventArgs", true)!;

				// bulk copy types
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoader                      >(bulkCopyType);
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoaderOptions               >(bulkCopyOptionsType);
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoaderRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoaderColumn                >(bulkCopyColumnMappingType);
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoaderColumnCollection      >(bulkCopyColumnMappingCollectionType);
				typeMapper.RegisterTypeWrapper<DevartWrappers.OracleLoaderRowsCopiedEventArgs   >(rowsCopiedEventArgsType);

				typeMapper.FinalizeMappings();

				var loaderFactory = typeMapper.BuildWrappedFactory((string tableName, DbConnection connection, DevartWrappers.OracleLoaderOptions options) => new DevartWrappers.OracleLoader(tableName, (DevartWrappers.OracleConnection)(object)connection, options));
				var columnFactory = typeMapper.BuildWrappedFactory((string name, DevartWrappers.OracleDbType dbType, int size, int precision, int scale, string dateFormat) => new DevartWrappers.OracleLoaderColumn(name, dbType, size, precision, scale, dateFormat));

				bulkCopy = new DevArtBulkCopyAdapter(
					loaderFactory,
					typeMapper.BuildFunc<DevartWrappers.OracleLoader, string, DevartWrappers.OracleDbType, int, int, int, string, int>((DevartWrappers.OracleLoader loader, string name, DevartWrappers.OracleDbType dbType, int size, int precision, int scale, string dateFormat) => loader.Columns.Add(columnFactory(name, dbType, size, precision, scale, dateFormat))));
			}
			else
				typeMapper.FinalizeMappings();

			var paramMapper      = typeMapper.Type<DevartWrappers.OracleParameter>();
			var dbTypeBuilder    = paramMapper.Member(p => p.OracleDbType);
			var connectionMapper = typeMapper.Type<DevartWrappers.OracleConnection>();
			var commandMapper    = typeMapper.Type<DevartWrappers.OracleCommand>();

			var dbTypeSetter = dbTypeBuilder.BuildSetter<DbParameter>();
			var dbTypeGetter = dbTypeBuilder.BuildGetter<DbParameter>();

			// data reader expressions
			// there is no separate tstz class, but issue with timezone loss still here for GetDateTimeOffset method (e.g. use OracleTests.TestDateTimeSQL test)
			// so we need to generate own mapper
			// TSTZ:rd.GetOracleTimeStamp(i) => DateTimeOffset
			var generator    = new ExpressionGenerator(typeMapper);
			var rdParam      = Expression.Parameter(typeof(DbDataReader), "rd");
			var indexParam   = Expression.Parameter(typeof(int), "i");
			var tstzExpr     = generator.MapExpression((DbDataReader rd, int i) => ((DevartWrappers.OracleDataReader)(object)rd).GetOracleTimeStamp(i), rdParam, indexParam);
			var tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			var expr         = generator.MapExpression((DevartWrappers.OracleTimeStamp tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.TimeZoneOffset).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			var body = generator.Build();
			var readDateTimeOffsetFromOracleTimeStampTZ = (Expression<Func<DbDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

			// TS:rd.GetOracleTimeStamp(i) => DateTimeOffset
			generator    = new ExpressionGenerator(typeMapper);
			tstzExpr     = generator.MapExpression((DbDataReader rd, int i) => ((DevartWrappers.OracleDataReader)(object)rd).GetOracleTimeStamp(i), rdParam, indexParam);
			tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			expr         = generator.MapExpression((DevartWrappers.OracleTimeStamp tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				TimeZoneInfo.Local.GetUtcOffset(new DateTimeOffset(tstz.Year, tstz.Month, tstz.Day, tstz.Hour, tstz.Minute, tstz.Second, default))).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			body = generator.Build();
			var readDateTimeOffsetFromOracleTimeStamp = (Expression<Func<DbDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

			// command.ExecuteArray(int)
			var executeArray = typeMapper.BuildFunc<DbCommand, int, int>(typeMapper.MapLambda((DevartWrappers.OracleCommand conn, int iters) => conn.ExecuteArray(iters)));

			var connectionFactory = typeMapper.BuildTypedFactory<string, DevartWrappers.OracleConnection, DbConnection>((string connectionString) => new DevartWrappers.OracleConnection(connectionString));

			return new OracleProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				connectionFactory,

				mappingSchema,
				true,

				customReaders,

				oracleBFileType,
				oracleBinaryType,
				oracleLobType,
				oracleLobType,
				oracleDateType,
				oracleNumberType,
				oracleIntervalDSType,
				oracleIntervalYMType,
				oracleStringType,
				oracleTimeStampType,
				null,
				null,
				oracleXmlType,
				null,
				oracleCursorType,
				oracleRefType,

				DevartTypesNamespace,

				(p, t) =>
				{
					var convertedType = DevartWrappers.ConvertDbType(t);
					if (convertedType != null)
						dbTypeSetter(p, convertedType.Value);
				},
				p => DevartWrappers.ConvertDbType(dbTypeGetter(p)),

				null,
				null,

				commandMapper.Member(p => p.PassParametersByName).BuildSetter<DbCommand>(),
				null,
				null,
				executeArray,

				typeMapper.BuildFactory((DateTimeOffset dto, string offset) => new DevartWrappers.OracleTimeStamp(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset)),

				readDateTimeOffsetFromOracleTimeStamp,
				readDateTimeOffsetFromOracleTimeStampTZ,
				null,
				null,
				null,
				null,
				bulkCopy);
		}

		private static void TryAddTypeReader<TType>(Type dataReaderType, List<(Type Type, string ReaderMethodName)> customReaders, string methodName)
		{
			if (dataReaderType.GetMethod(methodName, IndexParams) != null)
				customReaders.Add((typeof(TType), methodName));
		}

		private static Type? LoadType(
			Assembly                                   assembly,
			MappingSchema                              mappingSchema,
			string                                     typesNamespace,
			List<(Type Type, string ReaderMethodName)> readers,
			string                                     typeName,
			string?                                    readMethodName,
			DataType                                   dataType,
			bool                                       optional              = false,
			bool                                       hasNull               = true,
			bool                                       hasValue              = true,
			bool                                       skipConvertExpression = false,
			bool                                       hasIsNull             = true)
		{
			var type = assembly!.GetType($"{typesNamespace}.{typeName}", !optional);
			if (type == null)
				return null;

			if (readMethodName != null)
				readers.Add((type, readMethodName));

			if (hasNull)
			{
				// if native provider fails here, check that you have ODAC installed properly
				var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(ExpressionHelper.PropertyOrField(type, "Null", allowInherited: false), typeof(object))).CompileExpression();
				mappingSchema.AddScalarType(type, getNullValue(), true, dataType);
			}
			else
				mappingSchema.AddScalarType(type, null, true, dataType);

			if (skipConvertExpression || !(hasNull || hasIsNull))
				return type;

			// conversion from provider-specific type
			var valueParam = Expression.Parameter(type);

			Expression memberExpression = valueParam;
			if (!hasValue)
				memberExpression = valueParam;
			else
				memberExpression = ExpressionHelper.Property(valueParam, "Value");

			var condition = Expression.Condition(
				hasIsNull ? Expression.Property(valueParam, "IsNull") : Expression.Equal(valueParam, ExpressionHelper.PropertyOrField(type, "Null")),
				Expression.Constant(null, typeof(object)),
				Expression.Convert(memberExpression, typeof(object)));

			var convertExpression = Expression.Lambda(condition, valueParam);
			mappingSchema.SetConvertExpression(type, typeof(object), convertExpression);

			return type;
		}

		private static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
		{
			var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

			return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
		}

		#region Wrappers

		/// <summary>
		/// Intermediate enum to expose from adapter instead of two incompatible provider-specific enums.
		/// </summary>
		internal enum OracleDbType
		{
			// shared types
			BFile,
			Blob,
			Boolean,
			Byte,
			Char,
			Clob,
			Date,
			Decimal,
			Double,
			Int16,
			Int32,
			Int64,
			IntervalDS,
			IntervalYM,
			Long,
			LongRaw,
			NChar,
			NClob,
			NVarchar2,
			Raw,
			RefCursor,
			Single,
			TimeStamp,
			TimeStampLTZ,
			TimeStampTZ,
			Varchar2,
			XmlType,
			Array,
			Object,
			Ref,
			// oracle-specific
			Json,
			ArrayAsJson,
			ObjectAsJson,
			BinaryDouble,
			BinaryFloat,
			// devart-specific
			AnyData,
			RowId,
			Table,
		}

		/// <summary>
		/// Intermediate enum to expose from adapter instead of two incompatible provider-specific enums.
		/// </summary>
		[Flags]
		public enum BulkCopyOptions
		{
			Default                       = 0,
			DisableIndexes                = 0x001,
			DisableTriggers               = 0x002,
			KeepConstraints               = 0x004,
			KeepExternalForeignKeys       = 0x008,
			KeepPrimaryKeys               = 0x010,
			KeepSelfReferencedForeignKeys = 0x020,
			NoLogging                     = 0x040,
			UseArrayBinding               = 0x080,
			UseInternalTransaction        = 0x100
		}

		internal static class DevartWrappers
		{
			private static readonly IReadOnlyDictionary<OracleProviderAdapter.OracleDbType, OracleDbType> OracleDbTypeMap =
				new Dictionary<OracleProviderAdapter.OracleDbType, OracleDbType>()
				{
					{ OracleProviderAdapter.OracleDbType.BFile       , OracleDbType.BFile        },
					{ OracleProviderAdapter.OracleDbType.Blob        , OracleDbType.Blob         },
					{ OracleProviderAdapter.OracleDbType.Boolean     , OracleDbType.Boolean      },
					{ OracleProviderAdapter.OracleDbType.Byte        , OracleDbType.Byte         },
					{ OracleProviderAdapter.OracleDbType.Char        , OracleDbType.Char         },
					{ OracleProviderAdapter.OracleDbType.Clob        , OracleDbType.Clob         },
					{ OracleProviderAdapter.OracleDbType.Date        , OracleDbType.Date         },
					{ OracleProviderAdapter.OracleDbType.Decimal     , OracleDbType.Number       },
					{ OracleProviderAdapter.OracleDbType.Double      , OracleDbType.Double       },
					{ OracleProviderAdapter.OracleDbType.Int16       , OracleDbType.Int16        },
					{ OracleProviderAdapter.OracleDbType.Int32       , OracleDbType.Integer      },
					{ OracleProviderAdapter.OracleDbType.Int64       , OracleDbType.Int64        },
					{ OracleProviderAdapter.OracleDbType.IntervalDS  , OracleDbType.IntervalDS   },
					{ OracleProviderAdapter.OracleDbType.IntervalYM  , OracleDbType.IntervalYM   },
					{ OracleProviderAdapter.OracleDbType.Long        , OracleDbType.Long         },
					{ OracleProviderAdapter.OracleDbType.LongRaw     , OracleDbType.LongRaw      },
					{ OracleProviderAdapter.OracleDbType.NChar       , OracleDbType.NChar        },
					{ OracleProviderAdapter.OracleDbType.NClob       , OracleDbType.NClob        },
					{ OracleProviderAdapter.OracleDbType.NVarchar2   , OracleDbType.NVarChar     },
					{ OracleProviderAdapter.OracleDbType.Raw         , OracleDbType.Raw          },
					{ OracleProviderAdapter.OracleDbType.RefCursor   , OracleDbType.Cursor       },
					{ OracleProviderAdapter.OracleDbType.Single      , OracleDbType.Float        },
					{ OracleProviderAdapter.OracleDbType.TimeStamp   , OracleDbType.TimeStamp    },
					{ OracleProviderAdapter.OracleDbType.TimeStampLTZ, OracleDbType.TimeStampLTZ },
					{ OracleProviderAdapter.OracleDbType.TimeStampTZ , OracleDbType.TimeStampTZ  },
					{ OracleProviderAdapter.OracleDbType.Varchar2    , OracleDbType.VarChar      },
					{ OracleProviderAdapter.OracleDbType.XmlType     , OracleDbType.Xml          },
					{ OracleProviderAdapter.OracleDbType.Array       , OracleDbType.Array        },
					{ OracleProviderAdapter.OracleDbType.Object      , OracleDbType.Object       },
					{ OracleProviderAdapter.OracleDbType.Ref         , OracleDbType.Ref          },
					{ OracleProviderAdapter.OracleDbType.AnyData     , OracleDbType.AnyData      },
					{ OracleProviderAdapter.OracleDbType.RowId       , OracleDbType.RowId        },
					{ OracleProviderAdapter.OracleDbType.Table       , OracleDbType.Table        },
				};

			private static readonly IReadOnlyDictionary<OracleDbType, OracleProviderAdapter.OracleDbType> OracleDbTypeMapReverse =
				OracleDbTypeMap.ToDictionary(_ => _.Value, _ => _.Key);

			public static OracleDbType? ConvertDbType(OracleProviderAdapter.OracleDbType type)
			{
				if (OracleDbTypeMap.TryGetValue(type, out var converted))
					return converted;

				return null;
			}

			public static OracleProviderAdapter.OracleDbType? ConvertDbType(OracleDbType type)
			{
				if (OracleDbTypeMapReverse.TryGetValue(type, out var converted))
					return converted;

				return null;
			}

			public static OracleDbType GetDbType(DbDataType dbDataType)
			{
				return dbDataType.DataType switch
				{
					DataType.Enum or DataType.Json or DataType.NVarChar or DataType.NChar                                                                       => OracleDbType.NVarChar,
					DataType.Text                                                                                                                               => OracleDbType.Clob,
					DataType.NText                                                                                                                              => OracleDbType.NClob,
					DataType.BinaryJson or DataType.BitArray or DataType.Timestamp or DataType.Binary or DataType.VarBinary or DataType.Blob or DataType.Image  => OracleDbType.Blob,
					DataType.Boolean                                                                                                                            => OracleDbType.Boolean,
					DataType.Byte or DataType.SByte or DataType.Int16                                                                                           => OracleDbType.Int16,
					DataType.UInt16 or DataType.Int32                                                                                                           => OracleDbType.Integer,
					DataType.UInt32 or DataType.Int64                                                                                                           => OracleDbType.Int64,
					DataType.Int128 or DataType.DecFloat or DataType.VarNumeric or DataType.SmallMoney or DataType.Money or DataType.Decimal or DataType.UInt64 => OracleDbType.Number,
					DataType.Single                                                                                                                             => OracleDbType.Float,
					DataType.Double                                                                                                                             => OracleDbType.Double,
					DataType.Date                                                                                                                               => OracleDbType.Date,
					DataType.Interval or DataType.Time                                                                                                          => OracleDbType.IntervalDS,
					DataType.DateTime2 or DataType.DateTime                                                                                                     => OracleDbType.TimeStamp,
					DataType.TimeTZ or DataType.DateTimeOffset                                                                                                  => OracleDbType.TimeStampTZ,
					DataType.Xml                                                                                                                                => OracleDbType.Xml,
					DataType.Variant                                                                                                                            => OracleDbType.AnyData,
					DataType.Udt                                                                                                                                => OracleDbType.Object,
					DataType.Cursor                                                                                                                             => OracleDbType.Cursor,
					DataType.Structured                                                                                                                         => OracleDbType.Table,
					DataType.Long                                                                                                                               => OracleDbType.Long,
					DataType.LongRaw                                                                                                                            => OracleDbType.LongRaw,
					DataType.BFile                                                                                                                              => OracleDbType.BFile,
					DataType.Guid                                                                                                                               => OracleDbType.Raw,
					_                                                                                                                                           => OracleDbType.VarChar
				};
			}

			[Wrapper]
			public enum OracleDbType
			{
				AnyData      = 30,
				Array        = 1,
				BFile        = 2,
				Blob         = 3,
				Boolean      = 4,
				Byte         = 31,
				Char         = 5,
				Clob         = 6,
				Cursor       = 7,
				Date         = 8,
				Double       = 9,
				Float        = 10,
				Int16        = 32,
				Int64        = 33,
				Integer      = 11,
				IntervalDS   = 12,
				IntervalYM   = 13,
				Long         = 14,
				LongRaw      = 15,
				NChar        = 16,
				NClob        = 17,
				Number       = 19,
				NVarChar     = 18,
				Object       = 20,
				Raw          = 22,
				Ref          = 21,
				RowId        = 23,
				Table        = 24,
				TimeStamp    = 25,
				TimeStampLTZ = 26,
				TimeStampTZ  = 27,
				VarChar      = 28,
				Xml          = 29
			}

			[Wrapper]
			public sealed class OracleParameter
			{
				public OracleDbType OracleDbType { get; set; }
			}

			[Wrapper]
			public sealed class OracleDataReader
			{
				public OracleTimeStamp GetOracleTimeStamp(int i) => throw new NotImplementedException();
				public OracleNumber    GetOracleNumber   (int i) => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class OracleCommand
			{
				public bool PassParametersByName
				{
					get => throw new NotImplementedException();
					set => throw new NotImplementedException();
				}

				public int ExecuteArray(int iters) => throw new NotImplementedException();
			}

			[Wrapper]
			internal sealed class OracleConnection
			{
				public OracleConnection(string connectionString) => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class OracleNumber
			{
				public static explicit operator decimal(OracleNumber val) => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class OracleTimeStamp : TypeWrapper
			{
				public OracleTimeStamp(object instance) : base(instance, null)
				{
				}

				public OracleTimeStamp(int year, int month, int day, int hour, int minute, int second, int nanosecond, string timeZone) => throw new NotImplementedException();

				public int Year                => throw new NotImplementedException();
				public int Month               => throw new NotImplementedException();
				public int Day                 => throw new NotImplementedException();
				public int Hour                => throw new NotImplementedException();
				public int Minute              => throw new NotImplementedException();
				public int Second              => throw new NotImplementedException();
				public int Nanosecond          => throw new NotImplementedException();
				public string TimeZone         => throw new NotImplementedException();
				public TimeSpan TimeZoneOffset => throw new NotImplementedException();
			}

			#region BulkCopy
			[Wrapper]
			internal sealed class OracleLoader : TypeWrapper, IDisposable
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Dispose
					(Expression<Action<OracleLoader>>                            )((OracleLoader this_                    ) => ((IDisposable)this_).Dispose()),
					// [1]: LoadTable
					(Expression<Action<OracleLoader, IDataReader>>               )((OracleLoader this_, IDataReader reader) => this_.LoadTable(reader)),
					// [2]: get NotifyAfter
					(Expression<Func<OracleLoader, int>>                         )((OracleLoader this_                    ) => this_.NotifyAfter),
					// [3]: set NotifyAfter
					PropertySetter((OracleLoader this_) => this_.NotifyAfter),
					// [4]: get BatchSize
					(Expression<Func<OracleLoader, int?>>                        )((OracleLoader this_                    ) => this_.BatchSize),
					// [5]: set BatchSize
					PropertySetter((OracleLoader this_) => this_.BatchSize),
					// [6]: get ColumnMappings
					(Expression<Func<OracleLoader, OracleLoaderColumnCollection>>)((OracleLoader this_                    ) => this_.Columns),
				};

				private static string[] Events { get; }
					= new[]
				{
					nameof(RowsCopied)
				};

				public OracleLoader(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public OracleLoader(string tableName, OracleConnection connection, OracleLoaderOptions options) => throw new NotImplementedException();

				public void Dispose() => ((Action<OracleLoader>)CompiledWrappers[0])(this);
#pragma warning disable RS0030 // API mapping must preserve type
				public void LoadTable(IDataReader dataReader) => ((Action<OracleLoader, IDataReader>)CompiledWrappers[1])(this, dataReader);
#pragma warning restore RS0030 //  API mapping must preserve type

				public int NotifyAfter
				{
					get => ((Func<OracleLoader, int>)CompiledWrappers[2])(this);
					set => ((Action<OracleLoader, int>)CompiledWrappers[3])(this, value);
				}

				public int? BatchSize
				{
					get => ((Func<OracleLoader, int?>)CompiledWrappers[4])(this);
					set => ((Action<OracleLoader, int?>)CompiledWrappers[5])(this, value);
				}

				public OracleLoaderColumnCollection Columns => ((Func<OracleLoader, OracleLoaderColumnCollection>)CompiledWrappers[6])(this);

				private      OracleLoaderRowsCopiedEventHandler? _RowsCopied;
				public event OracleLoaderRowsCopiedEventHandler?  RowsCopied
				{
					add    => _RowsCopied = (OracleLoaderRowsCopiedEventHandler?)Delegate.Combine(_RowsCopied, value);
					remove => _RowsCopied = (OracleLoaderRowsCopiedEventHandler?)Delegate.Remove (_RowsCopied, value);
				}
			}

			[Wrapper]
			public sealed class OracleLoaderRowsCopiedEventArgs : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
				// [0]: get RowsCopied
				(Expression<Func<OracleLoaderRowsCopiedEventArgs, int>>)((OracleLoaderRowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<OracleLoaderRowsCopiedEventArgs, bool>>)((OracleLoaderRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set Abort
				PropertySetter((OracleLoaderRowsCopiedEventArgs this_) => this_.Abort),
				};

				public OracleLoaderRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public int RowsCopied => ((Func<OracleLoaderRowsCopiedEventArgs, int>)CompiledWrappers[0])(this);

				public bool Abort
				{
					get => ((Func<OracleLoaderRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
					set => ((Action<OracleLoaderRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
				}
			}

			[Wrapper]
			public delegate void OracleLoaderRowsCopiedEventHandler(object sender, OracleLoaderRowsCopiedEventArgs e);

			[Wrapper]
			public sealed class OracleLoaderColumnCollection : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Add
					(Expression<Func<OracleLoaderColumnCollection, OracleLoaderColumn, int>>)((OracleLoaderColumnCollection this_, OracleLoaderColumn column) => this_.Add(column)),
				};

				public OracleLoaderColumnCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public int Add(OracleLoaderColumn bulkCopyColumnMapping) => ((Func<OracleLoaderColumnCollection, OracleLoaderColumn, int>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
			}

			[Wrapper, Flags]
			public enum OracleLoaderOptions
			{
				Default                       = 0,
				DisableIndexes                = 16,
				DisableTriggers               = 32,
				KeepConstraints               = 448,
				KeepExternalForeignKeys       = 128,
				KeepPrimaryKeys               = 64,
				KeepSelfReferencedForeignKeys = 256,
				NoLogging                     = 8,
				UseArrayBinding               = 4,
				UseInternalTransaction        = 2
			}

			[Wrapper]
			public sealed class OracleLoaderColumn : TypeWrapper
			{
				public OracleLoaderColumn(object instance) : base(instance, null)
				{
				}

				public OracleLoaderColumn(string name, OracleDbType dbType, int size, int precision, int scale, string dateFormat) => throw new NotImplementedException();
			}

			#endregion
		}

		internal static class OracleWrappers
		{
			private static readonly IReadOnlyDictionary<OracleProviderAdapter.OracleDbType, OracleDbType> OracleDbTypeMap =
				new Dictionary<OracleProviderAdapter.OracleDbType, OracleDbType>()
				{
					{ OracleProviderAdapter.OracleDbType.BFile       , OracleDbType.BFile        },
					{ OracleProviderAdapter.OracleDbType.Blob        , OracleDbType.Blob         },
					{ OracleProviderAdapter.OracleDbType.Boolean     , OracleDbType.Boolean      },
					{ OracleProviderAdapter.OracleDbType.Byte        , OracleDbType.Byte         },
					{ OracleProviderAdapter.OracleDbType.Char        , OracleDbType.Char         },
					{ OracleProviderAdapter.OracleDbType.Clob        , OracleDbType.Clob         },
					{ OracleProviderAdapter.OracleDbType.Date        , OracleDbType.Date         },
					{ OracleProviderAdapter.OracleDbType.Decimal     , OracleDbType.Decimal      },
					{ OracleProviderAdapter.OracleDbType.Double      , OracleDbType.Double       },
					{ OracleProviderAdapter.OracleDbType.Int16       , OracleDbType.Int16        },
					{ OracleProviderAdapter.OracleDbType.Int32       , OracleDbType.Int32        },
					{ OracleProviderAdapter.OracleDbType.Int64       , OracleDbType.Int64        },
					{ OracleProviderAdapter.OracleDbType.IntervalDS  , OracleDbType.IntervalDS   },
					{ OracleProviderAdapter.OracleDbType.IntervalYM  , OracleDbType.IntervalYM   },
					{ OracleProviderAdapter.OracleDbType.Long        , OracleDbType.Long         },
					{ OracleProviderAdapter.OracleDbType.LongRaw     , OracleDbType.LongRaw      },
					{ OracleProviderAdapter.OracleDbType.NChar       , OracleDbType.NChar        },
					{ OracleProviderAdapter.OracleDbType.NClob       , OracleDbType.NClob        },
					{ OracleProviderAdapter.OracleDbType.NVarchar2   , OracleDbType.NVarchar2    },
					{ OracleProviderAdapter.OracleDbType.Raw         , OracleDbType.Raw          },
					{ OracleProviderAdapter.OracleDbType.RefCursor   , OracleDbType.RefCursor    },
					{ OracleProviderAdapter.OracleDbType.Single      , OracleDbType.Single       },
					{ OracleProviderAdapter.OracleDbType.TimeStamp   , OracleDbType.TimeStamp    },
					{ OracleProviderAdapter.OracleDbType.TimeStampLTZ, OracleDbType.TimeStampLTZ },
					{ OracleProviderAdapter.OracleDbType.TimeStampTZ , OracleDbType.TimeStampTZ  },
					{ OracleProviderAdapter.OracleDbType.Varchar2    , OracleDbType.Varchar2     },
					{ OracleProviderAdapter.OracleDbType.XmlType     , OracleDbType.XmlType      },
					{ OracleProviderAdapter.OracleDbType.Array       , OracleDbType.Array        },
					{ OracleProviderAdapter.OracleDbType.Object      , OracleDbType.Object       },
					{ OracleProviderAdapter.OracleDbType.Ref         , OracleDbType.Ref          },
					{ OracleProviderAdapter.OracleDbType.Json        , OracleDbType.Json         },
					{ OracleProviderAdapter.OracleDbType.ArrayAsJson , OracleDbType.ArrayAsJson  },
					{ OracleProviderAdapter.OracleDbType.ObjectAsJson, OracleDbType.ObjectAsJson },
					{ OracleProviderAdapter.OracleDbType.BinaryDouble, OracleDbType.BinaryDouble },
					{ OracleProviderAdapter.OracleDbType.BinaryFloat , OracleDbType.BinaryFloat  },
				};

			private static readonly IReadOnlyDictionary<OracleDbType, OracleProviderAdapter.OracleDbType> OracleDbTypeMapReverse =
				OracleDbTypeMap.ToDictionary(_ => _.Value, _ => _.Key);

			public static OracleDbType? ConvertDbType(OracleProviderAdapter.OracleDbType type)
			{
				if (OracleDbTypeMap.TryGetValue(type, out var converted))
					return converted;

				return null;
			}

			public static OracleProviderAdapter.OracleDbType? ConvertDbType(OracleDbType type)
			{
				if (OracleDbTypeMapReverse.TryGetValue(type, out var converted))
					return converted;

				return null;
			}

			[Wrapper]
			public sealed class OracleParameter
			{
				public OracleDbType OracleDbType { get; set; }
			}

			[Wrapper]
			public sealed class OracleDataReader
			{
				public OracleTimeStampTZ  GetOracleTimeStampTZ (int i) => throw new NotImplementedException();
				public OracleTimeStampLTZ GetOracleTimeStampLTZ(int i) => throw new NotImplementedException();
				public OracleDecimal      GetOracleDecimal     (int i) => throw new NotImplementedException();
			}

			[Wrapper]
			public enum OracleDbType
			{
				BFile        = 101,
				BinaryDouble = 132,
				BinaryFloat  = 133,
				Blob         = 102,
				Boolean      = 134,
				Byte         = 103,
				Char         = 104,
				Clob         = 105,
				Date         = 106,
				Decimal      = 107,
				Double       = 108,
				Int16        = 111,
				Int32        = 112,
				Int64        = 113,
				IntervalDS   = 114,
				IntervalYM   = 115,
				Long         = 109,
				LongRaw      = 110,
				NChar        = 117,
				NClob        = 116,
				NVarchar2    = 119,
				Raw          = 120,
				RefCursor    = 121,
				Single       = 122,
				TimeStamp    = 123,
				TimeStampLTZ = 124,
				TimeStampTZ  = 125,
				Varchar2     = 126,
				XmlType      = 127,

				// native provider and recent managed (21.3.0)
				Array        = 128,
				Object       = 129,
				Ref          = 130,

				// Oracle 21c
				Json         = 135,
				ArrayAsJson  = 136,
				ObjectAsJson = 137,

				// Oracle 23
				Vector         = 138,
				Vector_Int8    = 139,
				Vector_Float32 = 140,
				Vector_Float64 = 141,
			}

			[Wrapper]
			public sealed class OracleCommand
			{
				public int ArrayBindCount
				{
					get => throw new NotImplementedException();
					set => throw new NotImplementedException();
				}

				public bool BindByName
				{
					get => throw new NotImplementedException();
					set => throw new NotImplementedException();
				}

				public int InitialLONGFetchSize
				{
					get => throw new NotImplementedException();
					set => throw new NotImplementedException();
				}
			}

			[Wrapper]
			public sealed class OracleConnection : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Open
					(Expression<Action<OracleConnection>>         )((OracleConnection this_) => this_.Open()),
					// [1]: CreateCommand
					(Expression<Func<OracleConnection, DbCommand>>)((OracleConnection this_) => this_.CreateCommand()),
					// [2]: Dispose
					(Expression<Action<OracleConnection>>         )((OracleConnection this_) => this_.Dispose()),
				};

				public OracleConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public OracleConnection(string connectionString) => throw new NotImplementedException();

				// not called using wrapper
				public string HostName     => throw new NotImplementedException();
				public string DatabaseName => throw new NotImplementedException();
				public string ServiceName  => throw new NotImplementedException();

				public void      Open         () => ((Action<OracleConnection>         )CompiledWrappers[0])(this);
				public DbCommand CreateCommand() => ((Func<OracleConnection, DbCommand>)CompiledWrappers[1])(this);
				public void      Dispose      () => ((Action<OracleConnection>         )CompiledWrappers[2])(this);
			}

			[Wrapper]
			public sealed class OracleTimeStampLTZ
			{
				public int Year       => throw new NotImplementedException();
				public int Month      => throw new NotImplementedException();
				public int Day        => throw new NotImplementedException();
				public int Hour       => throw new NotImplementedException();
				public int Minute     => throw new NotImplementedException();
				public int Second     => throw new NotImplementedException();
				public int Nanosecond => throw new NotImplementedException();

				public OracleTimeStampTZ ToOracleTimeStampTZ()    => throw new NotImplementedException();
				public static TimeSpan   GetLocalTimeZoneOffset() => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class OracleDecimal
			{
				public static OracleDecimal SetPrecision(OracleDecimal value1, int precision) => throw new NotImplementedException();

				public static explicit operator decimal(OracleDecimal value1) => throw new NotImplementedException();
			}

			[Wrapper]
			public sealed class OracleTimeStampTZ : TypeWrapper
			{
				public OracleTimeStampTZ(object instance) : base(instance, null)
				{
				}

				public OracleTimeStampTZ(int year, int month, int day, int hour, int minute, int second, int nanosecond, string timeZone) => throw new NotImplementedException();

				public int Year        => throw new NotImplementedException();
				public int Month       => throw new NotImplementedException();
				public int Day         => throw new NotImplementedException();
				public int Hour        => throw new NotImplementedException();
				public int Minute      => throw new NotImplementedException();
				public int Second      => throw new NotImplementedException();
				public int Nanosecond  => throw new NotImplementedException();
				public string TimeZone => throw new NotImplementedException();

				public TimeSpan GetTimeZoneOffset() => throw new NotImplementedException();
			}

			#region BulkCopy
			[Wrapper]
			public sealed class OracleBulkCopy : TypeWrapper, IDisposable
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Dispose
					(Expression<Action<OracleBulkCopy>>                                     )((OracleBulkCopy this_                    ) => ((IDisposable)this_).Dispose()),
					// [1]: WriteToServer
					(Expression<Action<OracleBulkCopy, IDataReader>>                        )((OracleBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
					// [2]: get NotifyAfter
					(Expression<Func<OracleBulkCopy, int>>                                  )((OracleBulkCopy this_                    ) => this_.NotifyAfter),
					// [3]: get BatchSize
					(Expression<Func<OracleBulkCopy, int>>                                  )((OracleBulkCopy this_                    ) => this_.BatchSize),
					// [4]: get BulkCopyTimeout
					(Expression<Func<OracleBulkCopy, int>>                                  )((OracleBulkCopy this_                    ) => this_.BulkCopyTimeout),
					// [5]: get DestinationTableName
					(Expression<Func<OracleBulkCopy, string?>>                              )((OracleBulkCopy this_                    ) => this_.DestinationTableName),
					// [6]: get DestinationSchemaName
					(Expression<Func<OracleBulkCopy, string?>>                              )((OracleBulkCopy this_                    ) => this_.DestinationSchemaName),
					// [7]: get ColumnMappings
					(Expression<Func<OracleBulkCopy, OracleBulkCopyColumnMappingCollection>>)((OracleBulkCopy this_                    ) => this_.ColumnMappings),
					// [8]: set NotifyAfter
					PropertySetter((OracleBulkCopy this_) => this_.NotifyAfter),
					// [9]: set BatchSize
					PropertySetter((OracleBulkCopy this_) => this_.BatchSize),
					// [10]: set BulkCopyTimeout
					PropertySetter((OracleBulkCopy this_) => this_.BulkCopyTimeout),
					// [11]: set DestinationTableName
					PropertySetter((OracleBulkCopy this_) => this_.DestinationTableName),
					// [12]: set DestinationSchemaName
					PropertySetter((OracleBulkCopy this_) => this_.DestinationSchemaName),
				};

				private static string[] Events { get; }
					= new[]
				{
					nameof(OracleRowsCopied)
				};

				public OracleBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public OracleBulkCopy(OracleConnection connection, OracleBulkCopyOptions options) => throw new NotImplementedException();

				public void Dispose      ()                       => ((Action<OracleBulkCopy>)CompiledWrappers[0])(this);
	#pragma warning disable RS0030 // API mapping must preserve type
				public void WriteToServer(IDataReader dataReader) => ((Action<OracleBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);
	#pragma warning restore RS0030 //  API mapping must preserve type

				public int NotifyAfter
				{
					get => ((Func  <OracleBulkCopy, int>)CompiledWrappers[2])(this);
					set => ((Action<OracleBulkCopy, int>)CompiledWrappers[8])(this, value);
				}

				public int BatchSize
				{
					get => ((Func  <OracleBulkCopy, int>)CompiledWrappers[3])(this);
					set => ((Action<OracleBulkCopy, int>)CompiledWrappers[9])(this, value);
				}

				public int BulkCopyTimeout
				{
					get => ((Func  <OracleBulkCopy, int>)CompiledWrappers[4])(this);
					set => ((Action<OracleBulkCopy, int>)CompiledWrappers[10])(this, value);
				}

				public string? DestinationTableName
				{
					get => ((Func  <OracleBulkCopy, string?>)CompiledWrappers[5])(this);
					set => ((Action<OracleBulkCopy, string?>)CompiledWrappers[11])(this, value);
				}

				public string? DestinationSchemaName
				{
					get => ((Func  <OracleBulkCopy, string?>)CompiledWrappers[6])(this);
					set => ((Action<OracleBulkCopy, string?>)CompiledWrappers[12])(this, value);
				}

				public OracleBulkCopyColumnMappingCollection ColumnMappings => ((Func<OracleBulkCopy, OracleBulkCopyColumnMappingCollection>) CompiledWrappers[7])(this);

				private      OracleRowsCopiedEventHandler? _OracleRowsCopied;
				public event OracleRowsCopiedEventHandler?  OracleRowsCopied
				{
					add    => _OracleRowsCopied = (OracleRowsCopiedEventHandler?)Delegate.Combine(_OracleRowsCopied, value);
					remove => _OracleRowsCopied = (OracleRowsCopiedEventHandler?)Delegate.Remove (_OracleRowsCopied, value);
				}
			}

			[Wrapper]
			public sealed class OracleRowsCopiedEventArgs : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: get RowsCopied
					(Expression<Func<OracleRowsCopiedEventArgs, long>>)((OracleRowsCopiedEventArgs this_) => this_.RowsCopied),
					// [1]: get Abort
					(Expression<Func<OracleRowsCopiedEventArgs, bool>>)((OracleRowsCopiedEventArgs this_) => this_.Abort),
					// [2]: set Abort
					PropertySetter((OracleRowsCopiedEventArgs this_) => this_.Abort),
				};

				public OracleRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public long RowsCopied => ((Func<OracleRowsCopiedEventArgs, long>)CompiledWrappers[0])(this);

				public bool Abort
				{
					get => ((Func  <OracleRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
					set => ((Action<OracleRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
				}
			}

			[Wrapper]
			public delegate void OracleRowsCopiedEventHandler(object sender, OracleRowsCopiedEventArgs e);

			[Wrapper]
			public sealed class OracleBulkCopyColumnMappingCollection : TypeWrapper
			{
				private static LambdaExpression[] Wrappers { get; }
					= new LambdaExpression[]
				{
					// [0]: Add
					(Expression<Func<OracleBulkCopyColumnMappingCollection, OracleBulkCopyColumnMapping, OracleBulkCopyColumnMapping>>)((OracleBulkCopyColumnMappingCollection this_, OracleBulkCopyColumnMapping column) => this_.Add(column)),
				};

				public OracleBulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
				{
				}

				public OracleBulkCopyColumnMapping Add(OracleBulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<OracleBulkCopyColumnMappingCollection, OracleBulkCopyColumnMapping, OracleBulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
			}

			[Wrapper, Flags]
			public enum OracleBulkCopyOptions
			{
				Default                = 0,
				UseInternalTransaction = 1
			}

			[Wrapper]
			public sealed class OracleBulkCopyColumnMapping : TypeWrapper
			{
				public OracleBulkCopyColumnMapping(object instance) : base(instance, null)
				{
				}

				public OracleBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
			}
		}

		#endregion

		#endregion
	}
}
