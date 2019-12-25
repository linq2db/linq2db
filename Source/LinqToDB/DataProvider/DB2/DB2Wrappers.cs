using System;
using System.Data;

namespace LinqToDB.DataProvider.DB2
{
	using System.Diagnostics;
	using System.Linq.Expressions;
	using LinqToDB.Data;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	internal static class DB2Wrappers
	{
		private static readonly object _syncRoot = new object();
		private static TypeMapper? _typeMapper;

		internal static Type ConnectionType  = null!;
		internal static Type ParameterType   = null!;
		internal static Type DataReaderType  = null!;
		internal static Type TransactionType = null!;

		internal static Action<IDbDataParameter, DB2Type> TypeSetter = null!;
		internal static Func<IDbDataParameter, DB2Type>   TypeGetter = null!;

		internal static IDB2BulkCopyWrapper BulkCopy = null!;

		internal static DB2Connection CreateDB2Connection(string connectionString)
			=> _typeMapper!.CreateAndWrap(() => new DB2Connection(connectionString))!;

		// TODO: use in Tools
		//internal static Func<IDbConnection, DB2ServerTypes> ServerTypeGetter = null!;

		// not sure if it is still actual, but let's leave it optional for compatibility
		internal static Type? DB2DateTimeType;
		internal static Type  DB2BinaryType       = null!;
		internal static Type  DB2BlobType         = null!;
		internal static Type  DB2ClobType         = null!;
		internal static Type  DB2DateType         = null!;
		internal static Type  DB2DecimalType      = null!;
		internal static Type  DB2DecimalFloatType = null!;
		internal static Type  DB2DoubleType       = null!;
		internal static Type  DB2Int16Type        = null!;
		internal static Type  DB2Int32Type        = null!;
		internal static Type  DB2Int64Type        = null!;
		internal static Type  DB2RealType         = null!;
		internal static Type  DB2Real370Type      = null!;
		internal static Type  DB2RowIdType        = null!;
		internal static Type  DB2StringType       = null!;
		internal static Type  DB2TimeType         = null!;
		internal static Type  DB2TimeStampType    = null!;
		internal static Type  DB2XmlType          = null!;
		// optional, because recent provider version contains it as obsolete stub
		internal static Type? DB2TimeSpanType;

#if NET45 || NET46
		public static string AssemblyName => "IBM.Data.DB2";
#else
		public static string AssemblyName => "IBM.Data.DB2.Core";
#endif

		internal static void Initialize(MappingSchema mappingSchema)
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
						var clientNamespace = AssemblyName;
						ConnectionType               = Type.GetType($"{clientNamespace}.DB2Connection, {AssemblyName}", true);
						var assembly                 = ConnectionType.Assembly;
						ParameterType                = assembly.GetType($"{clientNamespace}.DB2Parameter", true);
						DataReaderType               = assembly.GetType($"{clientNamespace}.DB2DataReader", true);
						TransactionType              = assembly.GetType($"{clientNamespace}.DB2Transaction", true);
						var dbType                   = assembly.GetType($"{clientNamespace}.DB2Type", true);
						var serverTypesType          = assembly.GetType($"{clientNamespace}.DB2ServerTypes", true);

						var bulkCopyType                    = assembly.GetType($"{clientNamespace}.DB2BulkCopy", true);
						var bulkCopyOptionsType             = assembly.GetType($"{clientNamespace}.DB2BulkCopyOptions", true);
						var bulkCopyColumnMappingType       = assembly.GetType($"{clientNamespace}.DB2BulkCopyColumnMapping", true);
						var rowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.DB2RowsCopiedEventHandler", true);
						var rowsCopiedEventArgs             = assembly.GetType($"{clientNamespace}.DB2RowsCopiedEventArgs", true);
						var bulkCopyColumnMappingCollection = assembly.GetType($"{clientNamespace}.DB2BulkCopyColumnMappingCollection", true);

						DB2BinaryType       = loadType("DB2Binary"      , DataType.VarBinary)!;
						DB2BlobType         = loadType("DB2Blob"        , DataType.Blob     )!;
						DB2ClobType         = loadType("DB2Clob"        , DataType.NText    )!;
						DB2DateType         = loadType("DB2Date"        , DataType.Date     )!;
						DB2DateTimeType     = loadType("DB2DateTime"    , DataType.DateTime, true);
						DB2DecimalType      = loadType("DB2Decimal"     , DataType.Decimal  )!;
						DB2DecimalFloatType = loadType("DB2DecimalFloat", DataType.Decimal  )!;
						DB2DoubleType       = loadType("DB2Double"      , DataType.Double   )!;
						DB2Int16Type        = loadType("DB2Int16"       , DataType.Int16    )!;
						DB2Int32Type        = loadType("DB2Int32"       , DataType.Int32    )!;
						DB2Int64Type        = loadType("DB2Int64"       , DataType.Int64    )!;
						DB2RealType         = loadType("DB2Real"        , DataType.Single   )!;
						DB2Real370Type      = loadType("DB2Real370"     , DataType.Single   )!;
						DB2RowIdType        = loadType("DB2RowId"       , DataType.VarBinary)!;
						DB2StringType       = loadType("DB2String"      , DataType.NVarChar )!;
						DB2TimeType         = loadType("DB2Time"        , DataType.Time     )!;
						DB2TimeStampType    = loadType("DB2TimeStamp"   , DataType.DateTime2)!;
						DB2XmlType          = loadType("DB2Xml"         , DataType.Xml      )!;
						// TODO: register only for Informix
						DB2TimeSpanType     = loadType("DB2TimeSpan"    , DataType.Timestamp, true, true);

						var typeMapper = new TypeMapper(ConnectionType, ParameterType, dbType, serverTypesType, TransactionType,
							bulkCopyType, bulkCopyOptionsType, rowsCopiedEventHandlerType, rowsCopiedEventArgs, bulkCopyColumnMappingCollection, bulkCopyColumnMappingType);

						typeMapper.RegisterWrapper<DB2ServerTypes>();
						typeMapper.RegisterWrapper<DB2Connection>();
						typeMapper.RegisterWrapper<DB2Parameter>();
						typeMapper.RegisterWrapper<DB2Type>();
						typeMapper.RegisterWrapper<DB2Transaction>();

						// bulk copy types
						typeMapper.RegisterWrapper<DB2BulkCopy>();
						typeMapper.RegisterWrapper<DB2RowsCopiedEventArgs>();
						typeMapper.RegisterWrapper<DB2RowsCopiedEventHandler>();
						typeMapper.RegisterWrapper<DB2BulkCopyColumnMappingCollection>();
						typeMapper.RegisterWrapper<DB2BulkCopyOptions>();
						typeMapper.RegisterWrapper<DB2BulkCopyColumnMapping>();

						var dbTypeBuilder = typeMapper.Type<DB2Parameter>().Member(p => p.DB2Type);
						TypeSetter        = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						TypeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						_typeMapper = typeMapper;

						BulkCopy = new DB2BulkCopyWrapper(typeMapper);

						// moved from OnConnectionTypeCreated
						if (DataConnection.TraceSwitch.TraceInfo)
						{
							DataConnection.WriteTraceLine(
								DataReaderType.Assembly.FullName,
								DataConnection.TraceSwitch.DisplayName,
								TraceLevel.Info);

							DataConnection.WriteTraceLine(
								DB2DateTimeType != null ? "DB2DateTime is supported." : "DB2DateTime is not supported.",
								DataConnection.TraceSwitch.DisplayName,
								TraceLevel.Info);
						}

						Type? loadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
						{
							var type = assembly.GetType($"IBM.Data.DB2Types.{typeName}", !optional);
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
			}
		}

		[Wrapper] internal class DB2Binary       { public static readonly DB2Binary       Null = null!; }
		[Wrapper] internal class DB2Blob         { public static readonly DB2Blob         Null = null!; }
		[Wrapper] internal class DB2Clob         { public static readonly DB2Clob         Null = null!; }
		[Wrapper] internal class DB2Date         { public static readonly DB2Date         Null = null!; }
		[Wrapper] internal class DB2DateTime     { public static readonly DB2DateTime     Null = null!; }
		[Wrapper] internal class DB2Decimal      { public static readonly DB2Decimal      Null = null!; }
		[Wrapper] internal class DB2DecimalFloat { public static readonly DB2DecimalFloat Null = null!; }
		[Wrapper] internal class DB2Double       { public static readonly DB2Double       Null = null!; }
		[Wrapper] internal class DB2Int16        { public static readonly DB2Int16        Null = null!; }
		[Wrapper] internal class DB2Int32        { public static readonly DB2Int32        Null = null!; }
		[Wrapper] internal class DB2Int64        { public static readonly DB2Int64        Null = null!; }
		[Wrapper] internal class DB2Real         { public static readonly DB2Real         Null = null!; }
		[Wrapper] internal class DB2Real370      { public static readonly DB2Real370      Null = null!; }
		[Wrapper] internal class DB2RowId        { public static readonly DB2RowId        Null = null!; }
		[Wrapper] internal class DB2String       { public static readonly DB2String       Null = null!; }
		[Wrapper] internal class DB2Time         { public static readonly DB2Time         Null = null!; }
		[Wrapper] internal class DB2TimeStamp    { public static readonly DB2TimeStamp    Null = null!; }
		[Wrapper] internal class DB2Xml          { public static readonly DB2Xml          Null = null!; }

		// not used now types
		//[Wrapper] internal class DB2TimeStampOffset { }
		//[Wrapper] internal class DB2XsrObjectId { } (don't have Null field)

		[Wrapper]
		internal enum DB2ServerTypes
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
		internal class DB2Connection : TypeWrapper, IDisposable
		{
			public DB2Connection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public DB2Connection(string connectionString) => throw new NotImplementedException();

			// internal actually
			public DB2ServerTypes eServerType => this.Wrap(t => t.eServerType);

			public void Open() => this.WrapAction(c => c.Open());

			public void Dispose() => this.WrapAction(t => t.Dispose());
		}

		[Wrapper]
		internal class DB2Parameter
		{
			public DB2Type DB2Type { get; set; }
		}

		[Wrapper]
		internal enum DB2Type
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
			Char1               = 1001,
			IntervalDayFraction = 1005,
			IntervalYearMonth   = 1004,
			List                = 1010,
			MultiSet            = 1009,
			Row                 = 1011,
			Set                 = 1008,
			SmartLobLocator     = 1014,
			SQLUDTFixed         = 1013,
			SQLUDTVar           = 1012,
		}

		[Wrapper]
		internal class DB2Transaction
		{
		}

		#region BulkCopy

		internal interface IDB2BulkCopyWrapper
		{
			DB2BulkCopy CreateBulkCopy(IDbConnection connection, DB2BulkCopyOptions options);
			DB2BulkCopyColumnMapping CreateBulkCopyColumnMapping(int source, string destination);
		}

		class DB2BulkCopyWrapper : IDB2BulkCopyWrapper
		{
			private readonly TypeMapper _typeMapper;

			internal DB2BulkCopyWrapper(TypeMapper typeMapper)
			{
				_typeMapper = typeMapper;
			}

			DB2BulkCopy IDB2BulkCopyWrapper.CreateBulkCopy(IDbConnection connection, DB2BulkCopyOptions options)
				=> _typeMapper!.CreateAndWrap(() => new DB2BulkCopy((DB2Connection)connection, (DB2BulkCopyOptions)options))!;
			DB2BulkCopyColumnMapping IDB2BulkCopyWrapper.CreateBulkCopyColumnMapping(int source, string destination)
				=> _typeMapper!.CreateAndWrap(() => new DB2BulkCopyColumnMapping(source, destination))!;
		}

		[Wrapper]
		internal class DB2BulkCopy : TypeWrapper, IDisposable
		{
			public DB2BulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<DB2BulkCopy, DB2RowsCopiedEventHandler>(nameof(DB2RowsCopied));
			}

			public DB2BulkCopy(DB2Connection connection, DB2BulkCopyOptions options) => throw new NotImplementedException();

			public void Dispose() => this.WrapAction(t => t.Dispose());

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

			public DB2BulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
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
			public DB2RowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
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
		internal delegate void DB2RowsCopiedEventHandler(object sender, DB2RowsCopiedEventArgs e);

		[Wrapper]
		internal class DB2BulkCopyColumnMappingCollection : TypeWrapper
		{
			public DB2BulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public DB2BulkCopyColumnMapping Add(DB2BulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		internal enum DB2BulkCopyOptions
		{
			Default      = 0,
			KeepIdentity = 1,
			TableLock    = 2,
			Truncate     = 4
		}

		[Wrapper]
		internal class DB2BulkCopyColumnMapping : TypeWrapper
		{
			public DB2BulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public DB2BulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
