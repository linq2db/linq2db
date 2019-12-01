using System;
using System.Data;

namespace LinqToDB.DataProvider.Informix
{
	using System.Data.Common;
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
	internal static class InformixWrappers
	{
		private static object _ifxSyncRoot  = new object();
		private static object _db2SyncRoot  = new object();

		private static IInformixWrapper? _ifxWrapper;
		private static IInformixWrapper? _db2Wrapper;

		internal interface IInformixWrapper
		{
			/// <summary>
			/// IDS or SQLI provider.
			/// </summary>
			bool IsIDSProvider   { get; }

			Type ParameterType   { get; }
			Type DataReaderType  { get; }
			Type ConnectionType  { get; }
			Type TransactionType { get; }

			Action<IDbDataParameter, IfxType>?             IfxTypeSetter { get; }
			Func<IDbDataParameter, IfxType>?               IfxTypeGetter { get; }

			Action<IDbDataParameter, DB2Wrappers.DB2Type>? DB2TypeSetter { get; }
			Func<IDbDataParameter, DB2Wrappers.DB2Type>?   DB2TypeGetter { get; }

			Type  BlobType     { get; }
			Type  ClobType     { get; }
			Type? DecimalType  { get; }
			Type? DateTimeType { get; }
			Type? TimeSpanType { get; }

			Func<TimeSpan, object>? TimeSpanFactory { get; }

			IIFXBulkCopyWrapper?             IFXBulkCopy { get; }
			DB2Wrappers.IDB2BulkCopyWrapper? DB2BulkCopy { get; }
		}

		internal interface IIFXBulkCopyWrapper
		{
			IfxBulkCopy              CreateBulkCopy             (IDbConnection connection, IfxBulkCopyOptions options);
			IfxBulkCopyColumnMapping CreateBulkCopyColumnMapping(int source, string destination);
		}
		

		class IFXBulkCopyWrapper : IIFXBulkCopyWrapper
		{
			private readonly TypeMapper _typeMapper;

			internal IFXBulkCopyWrapper(TypeMapper typeMapper)
			{
				_typeMapper = typeMapper;
			}

			IfxBulkCopy IIFXBulkCopyWrapper.CreateBulkCopy(IDbConnection connection, IfxBulkCopyOptions options)
				=> _typeMapper!.CreateAndWrap(() => new IfxBulkCopy((IfxConnection)connection, options))!;
			IfxBulkCopyColumnMapping IIFXBulkCopyWrapper.CreateBulkCopyColumnMapping(int source, string destination)
				=> _typeMapper!.CreateAndWrap(() => new IfxBulkCopyColumnMapping(source, destination))!;
		}

		class InformixDB2Wrapper : IInformixWrapper
		{
			public InformixDB2Wrapper()
			{
			}

			bool IInformixWrapper.IsIDSProvider   => true;
			Type IInformixWrapper.ParameterType   => DB2Wrappers.ParameterType;
			Type IInformixWrapper.DataReaderType  => DB2Wrappers.DataReaderType;
			Type IInformixWrapper.ConnectionType  => DB2Wrappers.ConnectionType;
			Type IInformixWrapper.TransactionType => DB2Wrappers.TransactionType;

			Action<IDbDataParameter, IfxType>? IInformixWrapper.IfxTypeSetter => null;
			Func<IDbDataParameter, IfxType>?   IInformixWrapper.IfxTypeGetter => null;

			Action<IDbDataParameter, DB2Wrappers.DB2Type> IInformixWrapper.DB2TypeSetter => DB2Wrappers.TypeSetter;
			Func<IDbDataParameter, DB2Wrappers.DB2Type>   IInformixWrapper.DB2TypeGetter => DB2Wrappers.TypeGetter;

			Type  IInformixWrapper.BlobType     => DB2Wrappers.DB2BlobType;
			Type  IInformixWrapper.ClobType     => DB2Wrappers.DB2ClobType;
			// DB2Decimal type is unusable with Informix, as provider returns double value
			// and DB2Decimal(double) constructor is not implemented
			Type? IInformixWrapper.DecimalType  => null;
			Type? IInformixWrapper.DateTimeType => DB2Wrappers.DB2DateTimeType;
			Type? IInformixWrapper.TimeSpanType => DB2Wrappers.DB2TimeSpanType;

			Func<TimeSpan, object>? IInformixWrapper.TimeSpanFactory => null;

			IIFXBulkCopyWrapper?            IInformixWrapper.IFXBulkCopy => null;
			DB2Wrappers.IDB2BulkCopyWrapper IInformixWrapper.DB2BulkCopy => DB2Wrappers.BulkCopy;
		}

		class InformixIFXWrapper : IInformixWrapper
		{
			private readonly Type _connectionType;
			private readonly Type _transactionTypeType;
			private readonly Type _dataReaderType;
			private readonly Type _parameterType;

			private readonly Action<IDbDataParameter, IfxType> _typeSetter;
			private readonly Func<IDbDataParameter, IfxType>   _typeGetter;

			private readonly Type  _blobType;
			private readonly Type  _clobType;
			private readonly Type  _decimalType;
			private readonly Type  _dateTimeType;
			private readonly Type? _timeSpanType;

			private readonly Func<TimeSpan, object>? _timeSpanFactory;

			private readonly IFXBulkCopyWrapper? _bulkCopy;

			InformixIFXWrapper(
				TypeMapper typeMapper,
				Type connectionType,
				Type parameterType,
				Type dataReaderType,
				Type transactionTypeType,
				Action<IDbDataParameter, IfxType> typeSetter,
				Func<IDbDataParameter, IfxType>   typeGetter,
				IFXBulkCopyWrapper? bulkCopy,
				Type blobType,
				Type clobType,
				Type decimalType,
				Type dateTimeType,
				Type? timeSpanType)
			{
				_connectionType      = connectionType;
				_dataReaderType      = dataReaderType;
				_transactionTypeType = transactionTypeType;
				_parameterType       = parameterType;
				_typeSetter          = typeSetter;
				_typeGetter          = typeGetter;
				_bulkCopy            = bulkCopy;

				_blobType     = blobType;
				_clobType     = clobType;
				_decimalType  = decimalType;
				_dateTimeType = dateTimeType;
				_timeSpanType = timeSpanType;

				if (_timeSpanType != null)
					_timeSpanFactory = ts => typeMapper.CreateAndWrap(() => new IfxTimeSpan(ts))!.instance_!;

			}

			bool IInformixWrapper.IsIDSProvider => _bulkCopy != null;

			IIFXBulkCopyWrapper?             IInformixWrapper.IFXBulkCopy => _bulkCopy;
			DB2Wrappers.IDB2BulkCopyWrapper? IInformixWrapper.DB2BulkCopy => null;

			Type IInformixWrapper.ConnectionType  => _connectionType;
			Type IInformixWrapper.TransactionType => _transactionTypeType;
			Type IInformixWrapper.DataReaderType  => _dataReaderType;
			Type IInformixWrapper.ParameterType   => _parameterType;

			Type  IInformixWrapper.BlobType     => _blobType;
			Type  IInformixWrapper.ClobType     => _clobType;
			Type  IInformixWrapper.DecimalType  => _decimalType;
			Type  IInformixWrapper.DateTimeType => _dateTimeType;
			Type? IInformixWrapper.TimeSpanType => _timeSpanType;

			Func<TimeSpan, object>? IInformixWrapper.TimeSpanFactory => _timeSpanFactory;

			Action<IDbDataParameter, DB2Wrappers.DB2Type>? IInformixWrapper.DB2TypeSetter => null;
			Func<IDbDataParameter, DB2Wrappers.DB2Type>?   IInformixWrapper.DB2TypeGetter => null;
			Action<IDbDataParameter, IfxType> IInformixWrapper.IfxTypeSetter => _typeSetter;
			Func<IDbDataParameter, IfxType>   IInformixWrapper.IfxTypeGetter => _typeGetter;

			internal static IInformixWrapper Initialize(MappingSchema mappingSchema)
			{
				const string clientNamespace = "IBM.Data.Informix";

#if !NETSTANDARD2_0
				var assembly = Type.GetType($"{clientNamespace}.IfxConnection, IBM.Data.Informix", false)?.Assembly
					?? DbProviderFactories.GetFactory("IBM.Data.Informix").GetType().Assembly;
#else
				var assembly = Type.GetType($"{clientNamespace}.IfxConnection, IBM.Data.Informix", true).Assembly;
#endif

				var connectionType  = assembly.GetType($"{clientNamespace}.IfxConnection", true);
				var parameterType   = assembly.GetType($"{clientNamespace}.IfxParameter", true);
				var dataReaderType  = assembly.GetType($"{clientNamespace}.IfxDataReader", true);
				var transactionType = assembly.GetType($"{clientNamespace}.IfxTransaction", true);
				var dbType          = assembly.GetType($"{clientNamespace}.IfxType", true);

				var blobType     = loadType("IfxBlob", DataType.VarBinary)!;
				var clobType     = loadType("IfxClob", DataType.Text)!;
				var dateTimeType = loadType("IfxDateTime", DataType.DateTime2)!;
				// those two types obsoleted in recent providers
				var decimalType  = loadType("IfxDecimal", DataType.Decimal)!;
				var timeSpanType = loadType("IfxTimeSpan", DataType.Time, true, true);

				// bulk copy presense depends on assembly used
				// there are two assemblies: with and without bulk copy (maybe other differences)
				IFXBulkCopyWrapper? bulkCopy = null;
				TypeMapper typeMapper;
				var bulkCopyType = assembly.GetType($"{clientNamespace}.IfxBulkCopy", false);
				if (bulkCopyType != null)
				{
					var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.IfxBulkCopyOptions", true);
					var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.IfxRowsCopiedEventHandler", true);
					var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.IfxBulkCopyColumnMapping", true);
					var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.IfxBulkCopyColumnMappingCollection", true);
					var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.IfxRowsCopiedEventArgs", true);

					if (timeSpanType != null)
						typeMapper = new TypeMapper(
							connectionType, parameterType, transactionType, dbType,
							timeSpanType,
							bulkCopyType, bulkCopyOptionsType, bulkRowsCopiedEventHandlerType, bulkCopyColumnMappingType, bulkCopyColumnMappingCollectionType, rowsCopiedEventArgsType);
					else
						typeMapper = new TypeMapper(
							connectionType, parameterType, transactionType, dbType,
							bulkCopyType, bulkCopyOptionsType, bulkRowsCopiedEventHandlerType, bulkCopyColumnMappingType, bulkCopyColumnMappingCollectionType, rowsCopiedEventArgsType);

					bulkCopy = new IFXBulkCopyWrapper(typeMapper);
				}
				else
				{
					if (timeSpanType != null)
						typeMapper = new TypeMapper(connectionType, parameterType, transactionType, dbType,
							timeSpanType);
					else
						typeMapper = new TypeMapper(connectionType, parameterType, transactionType, dbType);
				}

				typeMapper.RegisterWrapper<IfxConnection>();
				typeMapper.RegisterWrapper<IfxParameter>();
				typeMapper.RegisterWrapper<IfxType>();
				typeMapper.RegisterWrapper<IfxTransaction>();

				if (timeSpanType != null)
					typeMapper.RegisterWrapper<IfxTimeSpan>();

				if (bulkCopyType != null)
				{
					// bulk copy types
					typeMapper.RegisterWrapper<IfxBulkCopy>();
					typeMapper.RegisterWrapper<IfxBulkCopyOptions>();
					typeMapper.RegisterWrapper<IfxRowsCopiedEventHandler>();
					typeMapper.RegisterWrapper<IfxBulkCopyColumnMapping>();
					typeMapper.RegisterWrapper<IfxBulkCopyColumnMappingCollection>();
					typeMapper.RegisterWrapper<IfxRowsCopiedEventArgs>();
				}

				var paramMapper = typeMapper.Type<IfxParameter>();
				var dbTypeBuilder = paramMapper.Member(p => p.IfxType);

				return new InformixIFXWrapper(
					typeMapper,
					connectionType,
					parameterType,
					dataReaderType,
					transactionType,
					dbTypeBuilder.BuildSetter<IDbDataParameter>(),
					dbTypeBuilder.BuildGetter<IDbDataParameter>(),
					bulkCopy,
					blobType,
					clobType,
					decimalType,
					dateTimeType,
					timeSpanType);

				Type? loadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
				{
					var type = assembly.GetType($"{clientNamespace}.{typeName}", !optional);
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
		}

		internal static IInformixWrapper Initialize(InformixDataProvider provider)
		{
			if (provider.Name == ProviderName.Informix)
			{
				if (_ifxWrapper == null)
				{
					lock (_ifxSyncRoot)
					{
						if (_ifxWrapper == null)
						{
							_ifxWrapper = InformixIFXWrapper.Initialize(provider.MappingSchema);
						}
					}
				}

				return _ifxWrapper;
			}
			else
			{
				if (_db2Wrapper == null)
				{
					lock (_db2SyncRoot)
					{
						if (_db2Wrapper == null)
						{
							DB2Wrappers.Initialize(provider.MappingSchema);
							_db2Wrapper = new InformixDB2Wrapper();
						}
					}
				}

				return _db2Wrapper;
			}
		}

		[Wrapper]
		internal class IfxParameter
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
		internal class IfxConnection
		{
		}

		[Wrapper]
		internal class IfxTransaction
		{
		}

#region BulkCopy
		[Wrapper]
		internal class IfxBulkCopy : TypeWrapper, IDisposable
		{
			public IfxBulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<IfxBulkCopy, IfxRowsCopiedEventHandler>(nameof(IfxRowsCopied));
			}

			public IfxBulkCopy(IfxConnection connection, IfxBulkCopyOptions options) => throw new NotImplementedException();

			void IDisposable.Dispose() => this.WrapAction(t => ((IDisposable)t).Dispose());

			public void WriteToServer(IDataReader dataReader) => this.WrapAction(t => t.WriteToServer(dataReader));

			public int NotifyAfter
			{
				get => this.Wrap(t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
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

			public IfxBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
				set => this.SetPropValue(t => t.ColumnMappings, value);
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
			public IfxRowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public int RowsCopied
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
		internal delegate void IfxRowsCopiedEventHandler(object sender, IfxRowsCopiedEventArgs e);

		[Wrapper]
		internal class IfxBulkCopyColumnMappingCollection : TypeWrapper
		{
			public IfxBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public IfxBulkCopyColumnMapping Add(IfxBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
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
		internal class IfxBulkCopyColumnMapping : TypeWrapper
		{
			public IfxBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public IfxBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		[Wrapper]
		internal class IfxTimeSpan : TypeWrapper
		{
			public IfxTimeSpan(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public IfxTimeSpan(TimeSpan timeSpan) => throw new NotImplementedException();
		}

#endregion
	}
}
