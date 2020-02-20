using System;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using System.Linq.Expressions;
	using LinqToDB.DataProvider.DB2;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

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

		private static object _ifxSyncRoot = new object();
		private static object _db2SyncRoot = new object();

		private static InformixProviderAdapter? _ifxAdapter;
		private static InformixProviderAdapter? _db2Adapter;

		private InformixProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			MappingSchema mappingSchema,

			Type  ifxBlobType,
			Type  ifxClobType,
			Type  ifxDecimalType,
			Type  ifxDateTimeType,
			Type? ifxTimeSpanType,

			Action<IDbDataParameter, IfxType> ifxTypeSetter,
			Func  <IDbDataParameter, IfxType> ifxTypeGetter,

			Func<TimeSpan, object>? timeSpanFactory,
			BulkCopyAdapter? bulkCopy)
		{
			IsIDSProvider   = bulkCopy != null;
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

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
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; }

		/// <summary>
		/// IDS or SQLI provider.
		/// </summary>
		public bool IsIDSProvider { get; }

		public Action<IDbDataParameter, IfxType>? SetIfxType { get; }
		public Func  <IDbDataParameter, IfxType>? GetIfxType { get; }

		public Action<IDbDataParameter, DB2ProviderAdapter.DB2Type>? SetDB2Type { get; }
		public Func  <IDbDataParameter, DB2ProviderAdapter.DB2Type>? GetDB2Type { get; }

		public Type  BlobType     { get; }
		public Type  ClobType     { get; }
		public Type? DecimalType  { get; }
		public Type? DateTimeType { get; }
		public Type? TimeSpanType { get; }

		public Func<TimeSpan, object>? TimeSpanFactory { get; }

		public BulkCopyAdapter?                    InformixBulkCopy { get; }
		public DB2ProviderAdapter.BulkCopyAdapter? DB2BulkCopy      { get; }

		public string? GetDecimalReaderMethod  { get; }
		public string  GetDateTimeReaderMethod { get; }
		public string  GetTimeSpanReaderMethod { get; }
		// SQLI informix provider only
		public string? GetBigIntReaderMethod   { get; }

		public string ProviderTypesNamespace   { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<IDbConnection, IfxBulkCopyOptions, IfxBulkCopy> bulkCopyCreator,
				Func<int, string, IfxBulkCopyColumnMapping> bulkCopyColumnMappingCreator)
			{
				Create              = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<IDbConnection, IfxBulkCopyOptions, IfxBulkCopy> Create              { get; }
			public Func<int, string, IfxBulkCopyColumnMapping>          CreateColumnMapping { get; }
		}

		public static InformixProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.Informix)
			{
				if (_ifxAdapter == null)
					lock (_ifxSyncRoot)
						if (_ifxAdapter == null)
							_ifxAdapter = CreateIfxAdapter();

				return _ifxAdapter;
			}
			else
			{
				if (_db2Adapter == null)
					lock (_db2SyncRoot)
						if (_db2Adapter == null)
							_db2Adapter = new InformixProviderAdapter(DB2ProviderAdapter.GetInstance());

				return _db2Adapter;
			}
		}

		private static InformixProviderAdapter CreateIfxAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(IfxAssemblyName, IfxProviderFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {IfxAssemblyName}");

			var connectionType  = assembly.GetType($"{IfxClientNamespace}.IfxConnection" , true);
			var parameterType   = assembly.GetType($"{IfxClientNamespace}.IfxParameter"  , true);
			var dataReaderType  = assembly.GetType($"{IfxClientNamespace}.IfxDataReader" , true);
			var commandType     = assembly.GetType($"{IfxClientNamespace}.IfxCommand"    , true);
			var transactionType = assembly.GetType($"{IfxClientNamespace}.IfxTransaction", true);
			var dbType          = assembly.GetType($"{IfxClientNamespace}.IfxType"       , true);

			var mappingSchema = new MappingSchema();
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
				var bulkCopyOptionsType                 = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyOptions"                , true);
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{IfxClientNamespace}.IfxRowsCopiedEventHandler"         , true);
				var bulkCopyColumnMappingType           = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyColumnMapping"          , true);
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{IfxClientNamespace}.IfxBulkCopyColumnMappingCollection", true);
				var rowsCopiedEventArgsType             = assembly.GetType($"{IfxClientNamespace}.IfxRowsCopiedEventArgs"            , true);

				typeMapper.RegisterTypeWrapper<IfxBulkCopy>(bulkCopyType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyOptions>(bulkCopyOptionsType);
				typeMapper.RegisterTypeWrapper<IfxRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyColumnMapping>(bulkCopyColumnMappingType);
				typeMapper.RegisterTypeWrapper<IfxBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollectionType);
				typeMapper.RegisterTypeWrapper<IfxRowsCopiedEventArgs>(rowsCopiedEventArgsType);

				typeMapper.FinalizeMappings();

				bulkCopy = new BulkCopyAdapter(
					(IDbConnection connection, IfxBulkCopyOptions options)
						=> typeMapper.CreateAndWrap(() => new IfxBulkCopy((IfxConnection)connection, options))!,
					(int source, string destination)
						=> typeMapper.CreateAndWrap(() => new IfxBulkCopyColumnMapping(source, destination))!);
			}
			else
				typeMapper.FinalizeMappings();

			var paramMapper   = typeMapper.Type<IfxParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.IfxType);

			Func<TimeSpan, object>? timespanFactory = null;
			if (timeSpanType != null)
				timespanFactory = ts => typeMapper.CreateAndWrap(() => new IfxTimeSpan(ts))!.instance_!;

			return new InformixProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				mappingSchema,
				blobType,
				clobType,
				decimalType,
				dateTimeType,
				timeSpanType,
				dbTypeBuilder.BuildSetter<IDbDataParameter>(),
				dbTypeBuilder.BuildGetter<IDbDataParameter>(),
				timespanFactory,
				bulkCopy);

			Type? loadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
			{
				var type = assembly!.GetType($"{IfxTypesNamespace}.{typeName}", !optional);
				if (type == null)
					return null;

				if (obsolete && type.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0)
					return null;

				if (register)
				{
					var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object))).Compile();
					mappingSchema.AddScalarType(type, getNullValue(), true, dataType);
				}

				return type;
			}
		}

		#region Wrappers

		[Wrapper]
		private class IfxParameter
		{
			public IfxType IfxType { get; set; }
		}

		[Wrapper]
		public enum IfxType
		{
			// SQLI and IDS fields (no numbers as they differ)
			BigInt,
			BigSerial,
			Blob,
			Boolean,
			Byte,
			Char,
			Char1,
			Clob,
			Date,
			DateTime,
			Decimal,
			Float,
			Int8,
			Integer,
			IntervalDayFraction,
			IntervalYearMonth,
			List,
			LVarChar,
			Money,
			MultiSet,
			NChar,
			Null,
			NVarChar,
			Other,
			Row,
			Serial,
			Serial8,
			Set,
			SmallFloat,
			SmallInt,
			SmartLOBLocator,
			SQLUDTFixed,
			SQLUDTVar,
			Text,
			VarChar,

			// IDS-only types
			Binary,
			Datalink,
			DbClob,
			Double,
			DynArray,
			Graphic,
			Invalid,
			LongVarBinary,
			LongVarChar,
			LongVarGraphic,
			Numeric,
			Real,
			Real370,
			RowId,
			Time,
			Timestamp,
			VarBinary,
			VarGraphic,
			Xml
		}

		[Wrapper]
		public class IfxConnection
		{
		}

		#region BulkCopy
		[Wrapper]
		public class IfxBulkCopy : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<IfxBulkCopy>>)((IfxBulkCopy this_) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<IfxBulkCopy, IDataReader>>)((IfxBulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<IfxBulkCopy, int>>)((IfxBulkCopy this_) => this_.NotifyAfter),
				// [3]: get BulkCopyTimeout
				(Expression<Func<IfxBulkCopy, int>>)((IfxBulkCopy this_) => this_.BulkCopyTimeout),
				// [4]: get DestinationTableName
				(Expression<Func<IfxBulkCopy, string?>>)((IfxBulkCopy this_) => this_.DestinationTableName),
				// [5]: get ColumnMappings
				(Expression<Func<IfxBulkCopy, IfxBulkCopyColumnMappingCollection>>)((IfxBulkCopy this_) => this_.ColumnMappings),
				// [6]: set NotifyAfter
				PropertySetter((IfxBulkCopy this_) => this_.NotifyAfter),
				// [7]: set BulkCopyTimeout
				PropertySetter((IfxBulkCopy this_) => this_.BulkCopyTimeout),
				// [8]: set DestinationTableName
				PropertySetter((IfxBulkCopy this_) => this_.DestinationTableName),
				// [9]: set ColumnMappings
				PropertySetter((IfxBulkCopy this_) => this_.ColumnMappings),
			};

			public IfxBulkCopy(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
				this.WrapEvent<IfxBulkCopy, IfxRowsCopiedEventHandler>(nameof(IfxRowsCopied));
			}

			public IfxBulkCopy(IfxConnection connection, IfxBulkCopyOptions options) => throw new NotImplementedException();

			void IDisposable.Dispose ()                       => ((Action<IfxBulkCopy>)CompiledWrappers[0])(this);
			public void WriteToServer(IDataReader dataReader) => ((Action<IfxBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);

			public int NotifyAfter
			{
				get => ((Func<IfxBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<IfxBulkCopy, int>)CompiledWrappers[6])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func<IfxBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<IfxBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func<IfxBulkCopy, string?>)CompiledWrappers[4])(this);
				set => ((Action<IfxBulkCopy, string?>)CompiledWrappers[8])(this, value);
			}

			public IfxBulkCopyColumnMappingCollection ColumnMappings
			{
				get => ((Func<IfxBulkCopy, IfxBulkCopyColumnMappingCollection>)CompiledWrappers[5])(this);
				set => ((Action<IfxBulkCopy, IfxBulkCopyColumnMappingCollection>)CompiledWrappers[9])(this, value);
			}

			public event IfxRowsCopiedEventHandler IfxRowsCopied
			{
				add => Events.AddHandler(nameof(IfxRowsCopied), value);
				remove => Events.RemoveHandler(nameof(IfxRowsCopied), value);
			}
		}

		[Wrapper]
		public class IfxRowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<IfxRowsCopiedEventArgs, int>>)((IfxRowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<IfxRowsCopiedEventArgs, bool>>)((IfxRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set Abort
				PropertySetter((IfxRowsCopiedEventArgs this_) => this_.Abort),
			};

			public IfxRowsCopiedEventArgs(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public int RowsCopied => ((Func<IfxRowsCopiedEventArgs, int>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func<IfxRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
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

			public IfxBulkCopyColumnMappingCollection(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
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
			public IfxBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper, null)
			{
			}

			public IfxBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		[Wrapper]
		internal class IfxTimeSpan : TypeWrapper
		{
			public IfxTimeSpan(object instance, TypeMapper mapper) : base(instance, mapper, null)
			{
			}

			public IfxTimeSpan(TimeSpan timeSpan) => throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
