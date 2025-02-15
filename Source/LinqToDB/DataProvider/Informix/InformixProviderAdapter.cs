using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;

#if !NET9_0_OR_GREATER
using Lock = System.Object;
#endif

using LinqToDB.Common;
using LinqToDB.DataProvider.DB2;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.Informix
{
	// Note on informix providers: there are actually 3 providers:
	// - SQLI Provider(IBM.Data.Informix) : netfx only, no bulk copy
	// - IDS Provider (IBM.Data.Informix) : netfx only, has bulk copy. Basically it is IBM.Data.DB2 with Ifx type names
	// - IDS Provider (IBM.Data.DB2): netfx and core (including linux and macos)
	// More details here: https://www.ibm.com/support/knowledgecenter/en/SSGU8G_14.1.0/com.ibm.cliapinode.doc/netdif.htm
	// actulally IDS provider creates issue for us by deprecating IFxTimeSpan type
	public class InformixProviderAdapter : IDynamicProviderAdapter
	{
		public const string IfxAssemblyName        = "IBM.Data.Informix";
		public const string IfxClientNamespace     = "IBM.Data.Informix";
		public const string IfxProviderFactoryName = "IBM.Data.Informix";
		public const string IfxTypesNamespace      = "IBM.Data.Informix";

		private static readonly Lock _ifxSyncRoot = new ();
		private static readonly Lock _db2SyncRoot = new ();

		private static InformixProviderAdapter? _ifxAdapter;
		private static InformixProviderAdapter? _db2Adapter;

		private InformixProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory,

			MappingSchema mappingSchema,

			Type  ifxBlobType,
			Type  ifxClobType,
			Type  ifxDecimalType,
			Type  ifxDateTimeType,
			Type? ifxTimeSpanType,

			Action<DbParameter, IfxType> ifxTypeSetter,
			Func  <DbParameter, IfxType> ifxTypeGetter,

			Func<TimeSpan, object>? timeSpanFactory,
			BulkCopyAdapter? bulkCopy)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			IsIDSProvider = bulkCopy != null;
			MappingSchema = mappingSchema;

			BlobType     = ifxBlobType;
			ClobType     = ifxClobType;
			DecimalType  = ifxDecimalType;
			DateTimeType = ifxDateTimeType;
			TimeSpanType = ifxTimeSpanType;

			SetIfxType = ifxTypeSetter;
			GetIfxType = ifxTypeGetter;

			TimeSpanFactory = timeSpanFactory;

			InformixBulkCopy = bulkCopy;

			GetDecimalReaderMethod  = "GetIfxDecimal";
			GetDateTimeReaderMethod = "GetIfxDateTime";
			GetTimeSpanReaderMethod = "GetIfxTimeSpan";
			GetBigIntReaderMethod   = IsIDSProvider ? null : "GetBigInt";

			ProviderTypesNamespace  = IfxTypesNamespace;
		}

		private InformixProviderAdapter(DB2ProviderAdapter db2Adapter)
		{
			ConnectionType  = db2Adapter.ConnectionType;
			DataReaderType  = db2Adapter.DataReaderType;
			ParameterType   = db2Adapter.ParameterType;
			CommandType     = db2Adapter.CommandType;
			TransactionType = db2Adapter.TransactionType;
			MappingSchema   = db2Adapter.MappingSchema;
			IsIDSProvider   = true;
			SetDB2Type      = db2Adapter.SetDbType;
			GetDB2Type      = db2Adapter.GetDbType;

			BlobType        = db2Adapter.DB2BlobType;
			ClobType        = db2Adapter.DB2ClobType;
			// DB2Decimal type is unusable with Informix, as provider returns double value
			// and DB2Decimal(double) constructor is not implemented
			DecimalType     = null;
			DateTimeType    = db2Adapter.DB2DateTimeType;
			TimeSpanType    = db2Adapter.DB2TimeSpanType;
			TimeSpanFactory = null;

			DB2BulkCopy     = db2Adapter.BulkCopy;

			GetDecimalReaderMethod  = null;
			GetDateTimeReaderMethod = "GetDB2DateTime";
			GetTimeSpanReaderMethod = "GetDB2TimeSpan";
			GetBigIntReaderMethod   = null;

			ProviderTypesNamespace  = db2Adapter.ProviderTypesNamespace;

			_connectionFactory = db2Adapter.CreateConnection;
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

		public MappingSchema MappingSchema { get; }

		/// <summary>
		/// IDS or SQLI provider.
		/// </summary>
		public bool IsIDSProvider { get; }

		public Action<DbParameter, IfxType>? SetIfxType { get; }
		public Func  <DbParameter, IfxType>? GetIfxType { get; }

		public Action<DbParameter, DB2ProviderAdapter.DB2Type>? SetDB2Type { get; }
		public Func  <DbParameter, DB2ProviderAdapter.DB2Type>? GetDB2Type { get; }

		public Type  BlobType     { get; }
		public Type  ClobType     { get; }
		public Type? DecimalType  { get; }
		public Type? DateTimeType { get; }
		public Type? TimeSpanType { get; }

		public Func<TimeSpan, object>? TimeSpanFactory { get; }

		internal BulkCopyAdapter?                    InformixBulkCopy { get; }
		internal DB2ProviderAdapter.BulkCopyAdapter? DB2BulkCopy      { get; }

		public string? GetDecimalReaderMethod  { get; }
		public string  GetDateTimeReaderMethod { get; }
		public string  GetTimeSpanReaderMethod { get; }
		// SQLI informix provider only
		public string? GetBigIntReaderMethod   { get; }

		public string ProviderTypesNamespace   { get; }

		internal class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<DbConnection, IfxBulkCopyOptions, IfxBulkCopy> bulkCopyCreator,
				Func<int, string, IfxBulkCopyColumnMapping>         bulkCopyColumnMappingCreator)
			{
				Create              = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<DbConnection, IfxBulkCopyOptions, IfxBulkCopy> Create              { get; }
			public Func<int, string, IfxBulkCopyColumnMapping>         CreateColumnMapping { get; }
		}

		public static InformixProviderAdapter GetInstance(InformixProvider provider)
		{
			if (provider == InformixProvider.Informix)
			{
				if (_ifxAdapter == null)
				{
					lock (_ifxSyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_ifxAdapter ??= CreateIfxAdapter();
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _ifxAdapter;
			}
			else
			{
				if (_db2Adapter == null)
				{
					lock (_db2SyncRoot)
#pragma warning disable CA1508 // Avoid dead conditional code
						_db2Adapter ??= new(DB2ProviderAdapter.Instance);
#pragma warning restore CA1508 // Avoid dead conditional code
				}

				return _db2Adapter;
			}
		}

		private static InformixProviderAdapter CreateIfxAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(IfxAssemblyName, IfxProviderFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {IfxAssemblyName}");

			var connectionType  = assembly.GetType($"{IfxClientNamespace}.IfxConnection" , true)!;
			var parameterType   = assembly.GetType($"{IfxClientNamespace}.IfxParameter"  , true)!;
			var dataReaderType  = assembly.GetType($"{IfxClientNamespace}.IfxDataReader" , true)!;
			var commandType     = assembly.GetType($"{IfxClientNamespace}.IfxCommand"    , true)!;
			var transactionType = assembly.GetType($"{IfxClientNamespace}.IfxTransaction", true)!;
			var dbType          = assembly.GetType($"{IfxClientNamespace}.IfxType"       , true)!;

			var mappingSchema = new InformixAdapterMappingSchema();
			var blobType      = loadType("IfxBlob"    , DataType.VarBinary)!;
			var clobType      = loadType("IfxClob"    , DataType.Text)!;
			var dateTimeType  = loadType("IfxDateTime", DataType.DateTime2)!;
			// those two types obsoleted in recent providers
			var decimalType   = loadType("IfxDecimal" , DataType.Decimal)!;
			var timeSpanType  = loadType("IfxTimeSpan", DataType.Time, true, true);

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<IfxConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<IfxParameter>(parameterType);
			typeMapper.RegisterTypeWrapper<IfxType>(dbType);

			if (timeSpanType != null)
				typeMapper.RegisterTypeWrapper<IfxTimeSpan>(timeSpanType);

			// bulk copy exists only for IDS provider version
			BulkCopyAdapter? bulkCopy = null;
			var bulkCopyType = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopy", false);
			if (bulkCopyType != null)
			{
				var bulkCopyOptionsType                 = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyOptions"                , true)!;
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{IfxClientNamespace}.IfxRowsCopiedEventHandler"         , true)!;
				var bulkCopyColumnMappingType           = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyColumnMapping"          , true)!;
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyColumnMappingCollection", true)!;
				var rowsCopiedEventArgsType             = assembly.GetType($"{IfxClientNamespace}.IfxRowsCopiedEventArgs"            , true)!;

				typeMapper.RegisterTypeWrapper<IfxBulkCopy>(bulkCopyType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyOptions>(bulkCopyOptionsType);
				typeMapper.RegisterTypeWrapper<IfxRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyColumnMapping>(bulkCopyColumnMappingType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollectionType);
				typeMapper.RegisterTypeWrapper<IfxRowsCopiedEventArgs>(rowsCopiedEventArgsType);

				typeMapper.FinalizeMappings();

				bulkCopy = new BulkCopyAdapter(
					typeMapper.BuildWrappedFactory((DbConnection connection, IfxBulkCopyOptions options) => new IfxBulkCopy((IfxConnection)(object)connection, options)),
					typeMapper.BuildWrappedFactory((int source, string destination) => new IfxBulkCopyColumnMapping(source, destination)));
			}
			else
				typeMapper.FinalizeMappings();

			var connectionFactory = typeMapper.BuildTypedFactory<string, IfxConnection, DbConnection>((string connectionString) => new IfxConnection(connectionString));

			var paramMapper   = typeMapper.Type<IfxParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.IfxType);

			Func<TimeSpan, object>? timespanFactory = null;
			if (timeSpanType != null)
				timespanFactory = typeMapper.BuildFactory((TimeSpan ts) => new IfxTimeSpan(ts));

			return new InformixProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				connectionFactory,
				mappingSchema,
				blobType,
				clobType,
				decimalType,
				dateTimeType,
				timeSpanType,
				dbTypeBuilder.BuildSetter<DbParameter>(),
				dbTypeBuilder.BuildGetter<DbParameter>(),
				timespanFactory,
				bulkCopy);

			Type? loadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
			{
				var type = assembly!.GetType($"{IfxTypesNamespace}.{typeName}", !optional);
				if (type == null)
					return null;

				if (obsolete && type.HasAttribute<ObsoleteAttribute>(false))
					return null;

				if (register)
				{
					var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(ExpressionHelper.Field(type, "Null"), typeof(object))).CompileExpression();
					mappingSchema.AddScalarType(type, getNullValue(), true, dataType);
				}

				return type;
			}
		}

		sealed class InformixAdapterMappingSchema : LockedMappingSchema
		{
			public InformixAdapterMappingSchema() : base("InformixAdapter")
			{
			}
		}

		#region Wrappers

		[Wrapper]
		private sealed class IfxParameter
		{
			public IfxType IfxType { get; set; }
		}

		[Wrapper]
		public enum IfxType
		{
			// SQLI and IDS field values differ
			// we will use values from IDS as it is modern (compared to SQLI) provider
			// so mapping of SQLI values will be a bit slower due to more complex mapping
			BigInt              = 203,
			BigSerial           = 230,
			Blob                = 110,
			Boolean             = 126,
			Byte                = 11,
			Char                = 0,
			Clob                = 111,
			Date                = 7,
			DateTime            = 10,
			Decimal             = 5,
			Float               = 3,
			Int8                = 17,
			Integer             = 2,
			LVarChar            = 101,
			Money               = 8,
			NChar               = 15,
			Null                = 9,
			NVarChar            = 16,
			Other               = 99,
			Serial              = 6,
			Serial8             = 18,
			SmallFloat          = 4,
			SmallInt            = 1,
			Text                = 12,
			VarChar             = 13,
			// present on both providers, but obsoleted in IDS
			Char1               = 1001,
			IntervalDayFraction = 1499,
			IntervalYearMonth   = 1400,
			List                = 21,
			MultiSet            = 20,
			Row                 = 22,
			Set                 = 19,
			SmartLOBLocator     = 112,
			SQLUDTFixed         = 41,
			SQLUDTVar           = 40,
			// IDS-only types
			Binary              = 215,
			DecimalFloat        = 228,
			Double              = 205,
			Invalid             = 200,
			LongVarBinary       = 217,
			LongVarChar         = 101,
			Numeric             = 208,
			Real                = 4,
			RowId               = 225,
			Time                = 210,
			Timestamp           = 211,
			VarBinary           = 216,
			// IDS-only types (oboleted)
			Datalink            = 224,
			DbClob              = 223,
			DynArray            = 229,
			Graphic             = 218,
			LongVarGraphic      = 220,
			Real370             = 227,
			VarGraphic          = 219,
			Xml                 = 226,
		}

		[Wrapper]
		internal sealed class IfxConnection
		{
			public IfxConnection(string connectionString) => throw new NotImplementedException();
		}

		#region BulkCopy
		[Wrapper]
		internal class IfxBulkCopy : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<IfxBulkCopy>>                                  )((IfxBulkCopy this_                    ) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<IfxBulkCopy, IDataReader>>                     )((IfxBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<IfxBulkCopy, int>>                               )((IfxBulkCopy this_                    ) => this_.NotifyAfter),
				// [3]: get BulkCopyTimeout
				(Expression<Func<IfxBulkCopy, int>>                               )((IfxBulkCopy this_                    ) => this_.BulkCopyTimeout),
				// [4]: get DestinationTableName
				(Expression<Func<IfxBulkCopy, string?>>                           )((IfxBulkCopy this_                    ) => this_.DestinationTableName),
				// [5]: get ColumnMappings
				(Expression<Func<IfxBulkCopy, IfxBulkCopyColumnMappingCollection>>)((IfxBulkCopy this_                    ) => this_.ColumnMappings),
				// [6]: set NotifyAfter
				PropertySetter((IfxBulkCopy this_) => this_.NotifyAfter),
				// [7]: set BulkCopyTimeout
				PropertySetter((IfxBulkCopy this_) => this_.BulkCopyTimeout),
				// [8]: set DestinationTableName
				PropertySetter((IfxBulkCopy this_) => this_.DestinationTableName),
				// [9]: set ColumnMappings
				PropertySetter((IfxBulkCopy this_) => this_.ColumnMappings),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(IfxRowsCopied)
			};

			public IfxBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public IfxBulkCopy(IfxConnection connection, IfxBulkCopyOptions options) => throw new NotImplementedException();

			void IDisposable.Dispose ()                       => ((Action<IfxBulkCopy>)CompiledWrappers[0])(this);
#pragma warning disable RS0030 // API mapping must preserve type
			public void WriteToServer(IDataReader dataReader) => ((Action<IfxBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);
#pragma warning restore RS0030 //  API mapping must preserve type

			public int NotifyAfter
			{
				get => ((Func  <IfxBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<IfxBulkCopy, int>)CompiledWrappers[6])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func  <IfxBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<IfxBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func  <IfxBulkCopy, string?>)CompiledWrappers[4])(this);
				set => ((Action<IfxBulkCopy, string?>)CompiledWrappers[8])(this, value);
			}

			public IfxBulkCopyColumnMappingCollection ColumnMappings
			{
				get => ((Func  <IfxBulkCopy, IfxBulkCopyColumnMappingCollection>)CompiledWrappers[5])(this);
				set => ((Action<IfxBulkCopy, IfxBulkCopyColumnMappingCollection>)CompiledWrappers[9])(this, value);
			}

			private      IfxRowsCopiedEventHandler? _IfxRowsCopied;
			public event IfxRowsCopiedEventHandler?  IfxRowsCopied
			{
				add    => _IfxRowsCopied = (IfxRowsCopiedEventHandler?)Delegate.Combine(_IfxRowsCopied, value);
				remove => _IfxRowsCopied = (IfxRowsCopiedEventHandler?)Delegate.Remove (_IfxRowsCopied, value);
			}
		}

		[Wrapper]
		public class IfxRowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<IfxRowsCopiedEventArgs, int>> )((IfxRowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<IfxRowsCopiedEventArgs, bool>>)((IfxRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set Abort
				PropertySetter((IfxRowsCopiedEventArgs this_) => this_.Abort),
			};

			public IfxRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public int RowsCopied => ((Func<IfxRowsCopiedEventArgs, int>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func  <IfxRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => ((Action<IfxRowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
			}
		}

		[Wrapper]
		public delegate void IfxRowsCopiedEventHandler(object sender, IfxRowsCopiedEventArgs e);

		[Wrapper]
		public class IfxBulkCopyColumnMappingCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<IfxBulkCopyColumnMappingCollection, IfxBulkCopyColumnMapping, IfxBulkCopyColumnMapping>>)((IfxBulkCopyColumnMappingCollection this_, IfxBulkCopyColumnMapping column) => this_.Add(column)),
			};

			public IfxBulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public IfxBulkCopyColumnMapping Add(IfxBulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<IfxBulkCopyColumnMappingCollection, IfxBulkCopyColumnMapping, IfxBulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
		}

		[Wrapper, Flags]
		public enum IfxBulkCopyOptions
		{
			Default      = 0,
			KeepIdentity = 1,
			TableLock    = 2,
			Truncate     = 4
		}

		[Wrapper]
		public class IfxBulkCopyColumnMapping : TypeWrapper
		{
			public IfxBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public IfxBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		[Wrapper]
		internal sealed class IfxTimeSpan : TypeWrapper
		{
			public IfxTimeSpan(object instance) : base(instance, null)
			{
			}

			public IfxTimeSpan(TimeSpan timeSpan) => throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
