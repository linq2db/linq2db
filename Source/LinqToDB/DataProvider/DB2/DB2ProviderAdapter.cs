using System;
using System.Data;

namespace LinqToDB.DataProvider.DB2
{
	using System.Linq.Expressions;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	public class DB2ProviderAdapter : IDynamicProviderAdapter
	{
		public const string ProviderFactoryName  = "IBM.Data.DB2";
		public const string TypesNamespace       = "IBM.Data.DB2Types";
		public const string NetFxClientNamespace = "IBM.Data.DB2";
		public const string CoreClientNamespace  = "IBM.Data.DB2.Core";

#if NET45 || NET46
		public const string AssemblyName         = "IBM.Data.DB2";
		public const string ClientNamespace      = "IBM.Data.DB2";
#else
		public const string AssemblyName         = "IBM.Data.DB2.Core";
		public const string ClientNamespace      = "IBM.Data.DB2.Core";
#endif

		private static readonly object _syncRoot = new object();
		private static DB2ProviderAdapter? _instance;

		private DB2ProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			Type? db2DateTimeType,
			Type db2BinaryType,
			Type db2BlobType,
			Type db2ClobType,
			Type db2DateType,
			Type db2DecimalType,
			Type db2DecimalFloatType,
			Type db2DoubleType,
			Type db2Int16Type,
			Type db2Int32Type,
			Type db2Int64Type,
			Type db2RealType,
			Type db2Real370Type,
			Type db2RowIdType,
			Type db2StringType,
			Type db2TimeType,
			Type db2TimeStampType,
			Type db2XmlType,
			// optional, because recent provider version contains it as obsolete stub
			Type? db2TimeSpanType,

			MappingSchema mappingSchema,

			Action<IDbDataParameter, DB2Type> dbTypeSetter,
			Func  <IDbDataParameter, DB2Type> dbTypeGetter,

			Func<string, DB2Connection> connectionCreator,
			Func<object, bool>          isDB2BinaryNull,

			BulkCopyAdapter bulkCopy)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			DB2DateTimeType     = db2DateTimeType;
			DB2BinaryType       = db2BinaryType;
			DB2BlobType         = db2BlobType;
			DB2ClobType         = db2ClobType;
			DB2DateType         = db2DateType;
			DB2DecimalType      = db2DecimalType;
			DB2DecimalFloatType = db2DecimalFloatType;
			DB2DoubleType       = db2DoubleType;
			DB2Int16Type        = db2Int16Type;
			DB2Int32Type        = db2Int32Type;
			DB2Int64Type        = db2Int64Type;
			DB2RealType         = db2RealType;
			DB2Real370Type      = db2Real370Type;
			DB2RowIdType        = db2RowIdType;
			DB2StringType       = db2StringType;
			DB2TimeType         = db2TimeType;
			DB2TimeStampType    = db2TimeStampType;
			DB2XmlType          = db2XmlType;
			DB2TimeSpanType     = db2TimeSpanType;

			MappingSchema = mappingSchema;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			CreateConnection = connectionCreator;
			IsDB2BinaryNull  = isDB2BinaryNull;

			BulkCopy = bulkCopy;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; }

		// not sure if it is still actual, but let's leave it optional for compatibility
		public Type? DB2DateTimeType     { get; }
		public Type  DB2BinaryType       { get; }
		public Type  DB2BlobType         { get; }
		public Type  DB2ClobType         { get; }
		public Type  DB2DateType         { get; }
		public Type  DB2DecimalType      { get; }
		public Type  DB2DecimalFloatType { get; }
		public Type  DB2DoubleType       { get; }
		public Type  DB2Int16Type        { get; }
		public Type  DB2Int32Type        { get; }
		public Type  DB2Int64Type        { get; }
		public Type  DB2RealType         { get; }
		public Type  DB2Real370Type      { get; }
		public Type  DB2RowIdType        { get; }
		public Type  DB2StringType       { get; }
		public Type  DB2TimeType         { get; }
		public Type  DB2TimeStampType    { get; }
		public Type  DB2XmlType          { get; }
		// optional, because recent provider version contains it as obsolete stub
		public Type? DB2TimeSpanType     { get; }

		public string  GetDB2Int64ReaderMethod        => "GetDB2Int64";
		public string  GetDB2Int32ReaderMethod        => "GetDB2Int32";
		public string  GetDB2Int16ReaderMethod        => "GetDB2Int16";
		public string  GetDB2DecimalReaderMethod      => "GetDB2Decimal";
		public string  GetDB2DecimalFloatReaderMethod => "GetDB2DecimalFloat";
		public string  GetDB2RealReaderMethod         => "GetDB2Real";
		public string  GetDB2Real370ReaderMethod      => "GetDB2Real370";
		public string  GetDB2DoubleReaderMethod       => "GetDB2Double";
		public string  GetDB2StringReaderMethod       => "GetDB2String";
		public string  GetDB2ClobReaderMethod         => "GetDB2Clob";
		public string  GetDB2BinaryReaderMethod       => "GetDB2Binary";
		public string  GetDB2BlobReaderMethod         => "GetDB2Blob";
		public string  GetDB2DateReaderMethod         => "GetDB2Date";
		public string  GetDB2TimeReaderMethod         => "GetDB2Time";
		public string  GetDB2TimeStampReaderMethod    => "GetDB2TimeStamp";
		public string  GetDB2XmlReaderMethod          => "GetDB2Xml";
		public string  GetDB2RowIdReaderMethod        => "GetDB2RowId";
		public string? GetDB2DateTimeReaderMethod     => DB2DateTimeType == null ? null : "GetDB2DateTime";

		public string ProviderTypesNamespace => TypesNamespace;

		public Action<IDbDataParameter, DB2Type> SetDbType { get; }
		public Func  <IDbDataParameter, DB2Type> GetDbType { get; }

		public Func<string, DB2Connection> CreateConnection { get; }

		public Func<object, bool> IsDB2BinaryNull { get; }

		public BulkCopyAdapter BulkCopy { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<IDbConnection, DB2BulkCopyOptions, DB2BulkCopy> bulkCopyCreator,
				Func<int, string, DB2BulkCopyColumnMapping> bulkCopyColumnMappingCreator)
			{
				Create = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<IDbConnection, DB2BulkCopyOptions, DB2BulkCopy> Create              { get; }
			public Func<int, string, DB2BulkCopyColumnMapping>          CreateColumnMapping { get; }
		}

		public static DB2ProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.DB2Connection" , true);
						var parameterType   = assembly.GetType($"{ClientNamespace}.DB2Parameter"  , true);
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.DB2DataReader" , true);
						var transactionType = assembly.GetType($"{ClientNamespace}.DB2Transaction", true);
						var commandType     = assembly.GetType($"{ClientNamespace}.DB2Command"    , true);
						var dbType          = assembly.GetType($"{ClientNamespace}.DB2Type"       , true);
						var serverTypesType = assembly.GetType($"{ClientNamespace}.DB2ServerTypes", true);

						var bulkCopyType                    = assembly.GetType($"{ClientNamespace}.DB2BulkCopy"                       , true);
						var bulkCopyOptionsType             = assembly.GetType($"{ClientNamespace}.DB2BulkCopyOptions"                , true);
						var bulkCopyColumnMappingType       = assembly.GetType($"{ClientNamespace}.DB2BulkCopyColumnMapping"          , true);
						var rowsCopiedEventHandlerType      = assembly.GetType($"{ClientNamespace}.DB2RowsCopiedEventHandler"         , true);
						var rowsCopiedEventArgs             = assembly.GetType($"{ClientNamespace}.DB2RowsCopiedEventArgs"            , true);
						var bulkCopyColumnMappingCollection = assembly.GetType($"{ClientNamespace}.DB2BulkCopyColumnMappingCollection", true);


						var mappingSchema = new MappingSchema();

						var db2BinaryType       = loadType("DB2Binary"      , DataType.VarBinary)!;
						var db2BlobType         = loadType("DB2Blob"        , DataType.Blob)!;
						var db2ClobType         = loadType("DB2Clob"        , DataType.NText)!;
						var db2DateType         = loadType("DB2Date"        , DataType.Date)!;
						var db2DateTimeType     = loadType("DB2DateTime"    , DataType.DateTime , true);
						var db2DecimalType      = loadType("DB2Decimal"     , DataType.Decimal)!;
						var db2DecimalFloatType = loadType("DB2DecimalFloat", DataType.Decimal)!;
						var db2DoubleType       = loadType("DB2Double"      , DataType.Double)!;
						var db2Int16Type        = loadType("DB2Int16"       , DataType.Int16)!;
						var db2Int32Type        = loadType("DB2Int32"       , DataType.Int32)!;
						var db2Int64Type        = loadType("DB2Int64"       , DataType.Int64)!;
						var db2RealType         = loadType("DB2Real"        , DataType.Single)!;
						var db2Real370Type      = loadType("DB2Real370"     , DataType.Single)!;
						var db2RowIdType        = loadType("DB2RowId"       , DataType.VarBinary)!;
						var db2StringType       = loadType("DB2String"      , DataType.NVarChar)!;
						var db2TimeType         = loadType("DB2Time"        , DataType.Time)!;
						var db2TimeStampType    = loadType("DB2TimeStamp"   , DataType.DateTime2)!;
						var db2XmlType          = loadType("DB2Xml"         , DataType.Xml)!;
						// TODO: register only for Informix
						var db2TimeSpanType     = loadType("DB2TimeSpan"    , DataType.Timestamp, true, true);

						var typeMapper = new TypeMapper();

						typeMapper.RegisterTypeWrapper<DB2ServerTypes>(serverTypesType);
						typeMapper.RegisterTypeWrapper<DB2Connection>(connectionType);
						typeMapper.RegisterTypeWrapper<DB2Parameter>(parameterType);
						typeMapper.RegisterTypeWrapper<DB2Type>(dbType);
						typeMapper.RegisterTypeWrapper<DB2Transaction>(transactionType);
						typeMapper.RegisterTypeWrapper<DB2Binary>(db2BinaryType);

						// bulk copy types
						typeMapper.RegisterTypeWrapper<DB2BulkCopy>(bulkCopyType);
						typeMapper.RegisterTypeWrapper<DB2RowsCopiedEventArgs>(rowsCopiedEventArgs);
						typeMapper.RegisterTypeWrapper<DB2RowsCopiedEventHandler>(rowsCopiedEventHandlerType);
						typeMapper.RegisterTypeWrapper<DB2BulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollection);
						typeMapper.RegisterTypeWrapper<DB2BulkCopyOptions>(bulkCopyOptionsType);
						typeMapper.RegisterTypeWrapper<DB2BulkCopyColumnMapping>(bulkCopyColumnMappingType);

						typeMapper.FinalizeMappings();

						var db2BinaryBuilder = typeMapper.Type<DB2Binary>().Member(p => p.IsNull);
						var isDB2BinaryNull  = db2BinaryBuilder.BuildGetter<object>();

						var dbTypeBuilder = typeMapper.Type<DB2Parameter>().Member(p => p.DB2Type);
						var typeSetter    = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						var typeGetter    = dbTypeBuilder.BuildGetter<IDbDataParameter>();


						var bulkCopy = new BulkCopyAdapter(
							(IDbConnection connection, DB2BulkCopyOptions options)
								=> typeMapper.CreateAndWrap(() => new DB2BulkCopy((DB2Connection)connection, options))!,
							(int source, string destination)
								=> typeMapper.CreateAndWrap(() => new DB2BulkCopyColumnMapping(source, destination))!);


						_instance = new DB2ProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,

							db2DateTimeType,
							db2BinaryType,
							db2BlobType,
							db2ClobType,
							db2DateType,
							db2DecimalType,
							db2DecimalFloatType,
							db2DoubleType,
							db2Int16Type,
							db2Int32Type,
							db2Int64Type,
							db2RealType,
							db2Real370Type,
							db2RowIdType,
							db2StringType,
							db2TimeType,
							db2TimeStampType,
							db2XmlType,
							db2TimeSpanType,

							mappingSchema,
							typeSetter,
							typeGetter,
							(string connectionString) => typeMapper.CreateAndWrap(() => new DB2Connection(connectionString))!,
							isDB2BinaryNull,
							bulkCopy);

						Type? loadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
						{
							var type = assembly!.GetType($"{TypesNamespace}.{typeName}", !optional);
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

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		private class DB2Binary
		{
			public bool IsNull { get; }
		}

		[Wrapper]
		public enum DB2ServerTypes
		{
			DB2_390     = 2,
			DB2_400     = 4,
			DB2_IDS     = 16,
			DB2_UNKNOWN = 0,
			DB2_UW      = 1,
			DB2_VM      = 24,
			DB2_VM_VSE  = 8,
			DB2_VSE     = 40
		}

		[Wrapper]
		public class DB2Connection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get eServerType
				(Expression<Func<DB2Connection, DB2ServerTypes>>)((DB2Connection this_) => this_.eServerType),
				// [1]: Open
				(Expression<Action<DB2Connection>>)((DB2Connection this_) => this_.Open()),
				// [2]: Dispose
				(Expression<Action<DB2Connection>>)((DB2Connection this_) => this_.Dispose()),
			};

			public DB2Connection(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public DB2Connection(string connectionString) => throw new NotImplementedException();

			// internal actually
			public DB2ServerTypes eServerType => ((Func<DB2Connection, DB2ServerTypes>)CompiledWrappers[0])(this);
			public void           Open()      => ((Action<DB2Connection>)CompiledWrappers[1])(this);
			public void           Dispose()   => ((Action<DB2Connection>)CompiledWrappers[2])(this);
		}

		[Wrapper]
		private class DB2Parameter
		{
			public DB2Type DB2Type { get; set; }
		}

		[Wrapper]
		public enum DB2Type
		{
			BigInt                = 3,
			BigSerial             = 30,
			Binary                = 15,
			BinaryXml             = 31,
			Blob                  = 22,
			Boolean               = 1015,
			Byte                  = 40,
			Char                  = 12,
			Clob                  = 21,
			Cursor                = 33,
			Datalink              = 24,
			Date                  = 9,
			DateTime              = 38,
			DbClob                = 23,
			Decimal               = 7,
			DecimalFloat          = 28,
			Double                = 5,
			DynArray              = 29,
			Float                 = 6,
			Graphic               = 18,
			Int8                  = 35,
			Integer               = 2,
			Invalid               = 0,
			LongVarBinary         = 17,
			LongVarChar           = 14,
			LongVarGraphic        = 20,
			Money                 = 37,
			NChar                 = 1006,
			Null                  = 1003,
			Numeric               = 8,
			NVarChar              = 1007,
			Other                 = 1016,
			Real                  = 4,
			Real370               = 27,
			RowId                 = 25,
			Serial                = 34,
			Serial8               = 36,
			SmallFloat            = 1002,
			SmallInt              = 1,
			Text                  = 39,
			Time                  = 10,
			Timestamp             = 11,
			TimeStampWithTimeZone = 32,
			VarBinary             = 16,
			VarChar               = 13,
			VarGraphic            = 19,
			Xml                   = 26,

			// not compat(i|a)ble with Informix
			Char1                 = 1001,
			IntervalDayFraction   = 1005,
			IntervalYearMonth     = 1004,
			List                  = 1010,
			MultiSet              = 1009,
			Row                   = 1011,
			Set                   = 1008,
			SmartLobLocator       = 1014,
			SQLUDTFixed           = 1013,
			SQLUDTVar             = 1012,
		}

		[Wrapper]
		internal class DB2Transaction
		{
		}

		#region BulkCopy
		[Wrapper]
		public class DB2BulkCopy : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<DB2BulkCopy>>)((DB2BulkCopy this_) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<DB2BulkCopy, IDataReader>>)((DB2BulkCopy this_, IDataReader reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<DB2BulkCopy, int>>)((DB2BulkCopy this_) => this_.NotifyAfter),
				// [3]: get BulkCopyTimeout
				(Expression<Func<DB2BulkCopy, int>>)((DB2BulkCopy this_) => this_.BulkCopyTimeout),
				// [4]: get DestinationTableName
				(Expression<Func<DB2BulkCopy, string?>>)((DB2BulkCopy this_) => this_.DestinationTableName),
				// [5]: get ColumnMappings
				(Expression<Func<DB2BulkCopy, DB2BulkCopyColumnMappingCollection>>)((DB2BulkCopy this_) => this_.ColumnMappings),
			};

			public DB2BulkCopy(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
				this.WrapEvent<DB2BulkCopy, DB2RowsCopiedEventHandler>(nameof(DB2RowsCopied));
			}

			public DB2BulkCopy(DB2Connection connection, DB2BulkCopyOptions options) => throw new NotImplementedException();

			public void Dispose      ()                       => ((Action<DB2BulkCopy>)CompiledWrappers[0])(this);
			public void WriteToServer(IDataReader dataReader) => ((Action<DB2BulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);

			public int NotifyAfter
			{
				get => ((Func<DB2BulkCopy, int>)CompiledWrappers[2])(this);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func<DB2BulkCopy, int>)CompiledWrappers[3])(this);
				set => this.SetPropValue(t => t.BulkCopyTimeout, value);
			}

			public string? DestinationTableName
			{
				get => ((Func<DB2BulkCopy, string?>)CompiledWrappers[4])(this);
				set => this.SetPropValue(t => t.DestinationTableName, value);
			}

			public DB2BulkCopyColumnMappingCollection ColumnMappings
			{
				get => ((Func<DB2BulkCopy, DB2BulkCopyColumnMappingCollection>)CompiledWrappers[5])(this);
				set => this.SetPropValue(t => t.ColumnMappings, value);
			}

			public event DB2RowsCopiedEventHandler DB2RowsCopied
			{
				add    => Events.AddHandler   (nameof(DB2RowsCopied), value);
				remove => Events.RemoveHandler(nameof(DB2RowsCopied), value);
			}
		}

		[Wrapper]
		public class DB2RowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<DB2RowsCopiedEventArgs, int>>)((DB2RowsCopiedEventArgs this_) => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<DB2RowsCopiedEventArgs, bool>>)((DB2RowsCopiedEventArgs this_) => this_.Abort),
			};

			public DB2RowsCopiedEventArgs(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public int RowsCopied => ((Func<DB2RowsCopiedEventArgs, int>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func<DB2RowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		public delegate void DB2RowsCopiedEventHandler(object sender, DB2RowsCopiedEventArgs e);

		[Wrapper]
		public class DB2BulkCopyColumnMappingCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<DB2BulkCopyColumnMappingCollection, DB2BulkCopyColumnMapping, DB2BulkCopyColumnMapping>>)((DB2BulkCopyColumnMappingCollection this_, DB2BulkCopyColumnMapping column) => this_.Add(column)),
			};

			public DB2BulkCopyColumnMappingCollection(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public DB2BulkCopyColumnMapping Add(DB2BulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<DB2BulkCopyColumnMappingCollection, DB2BulkCopyColumnMapping, DB2BulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
		}

		[Wrapper, Flags]
		public enum DB2BulkCopyOptions
		{
			Default      = 0,
			KeepIdentity = 1,
			TableLock    = 2,
			Truncate     = 4
		}

		[Wrapper]
		public class DB2BulkCopyColumnMapping : TypeWrapper
		{
			public DB2BulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper, null)
			{
			}

			public DB2BulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
