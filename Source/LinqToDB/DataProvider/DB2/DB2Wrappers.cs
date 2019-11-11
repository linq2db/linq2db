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

		internal static Type ConnectionType = null!;
		internal static Type ParameterType  = null!;
		internal static Type DataReaderType = null!;

		internal static Action<IDbDataParameter, DB2Type> TypeSetter = null!;
		internal static Func<IDbDataParameter, DB2Type>   TypeGetter = null!;
		
		// TODO: use in Tools
		//internal static Func<IDbConnection, DB2ServerTypes> ServerTypeGetter = null!;

		// not sure if it is still actual, but let's leave it optional for compatibility
		internal static Type? DB2DateTimeType;
		internal static Type DB2BinaryType       = null!;
		internal static Type DB2BlobType         = null!;
		internal static Type DB2ClobType         = null!;
		internal static Type DB2DateType         = null!;
		internal static Type DB2DecimalType      = null!;
		internal static Type DB2DecimalFloatType = null!;
		internal static Type DB2DoubleType       = null!;
		internal static Type DB2Int16Type        = null!;
		internal static Type DB2Int32Type        = null!;
		internal static Type DB2Int64Type        = null!;
		internal static Type DB2RealType         = null!;
		internal static Type DB2Real370Type      = null!;
		internal static Type DB2RowIdType        = null!;
		internal static Type DB2StringType       = null!;
		internal static Type DB2TimeType         = null!;
		internal static Type DB2TimeStampType    = null!;
		internal static Type DB2XmlType          = null!;

		// TODO: second parameter cast is a temporary hack
		internal static DB2BulkCopy NewDB2BulkCopy(IDbConnection connection, DB2BulkCopyOptions options)     => _typeMapper!.CreateAndWrap(() => new DB2BulkCopy((DB2Connection)connection, (DB2BulkCopyOptions)options))!;
		internal static DB2BulkCopyColumnMapping NewDB2BulkCopyColumnMapping(int source, string destination) => _typeMapper!.CreateAndWrap(() => new DB2BulkCopyColumnMapping(source, destination))!;

		internal static void Initialize(MappingSchema mappingSchema)
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
#if NET45 || NET46
						const string assemblyName    = "IBM.Data.DB2";
						const string clientNamespace = "IBM.Data.DB2";
#else
						const string assemblyName    = "IBM.Data.DB2.Core";
						const string clientNamespace = "IBM.Data.DB2.Core";
#endif
						ConnectionType = Type.GetType($"{clientNamespace}.DB2Connection, {assemblyName}", true);
						var assembly   = ConnectionType.Assembly;
						ParameterType  = assembly.GetType($"{clientNamespace}.DB2Parameter", true);
						DataReaderType = assembly.GetType($"{clientNamespace}.DB2DataReader", true);
						var dbType     = assembly.GetType($"{clientNamespace}.DB2Type", true);

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

						var typeMapper = new TypeMapper(ConnectionType, ParameterType, dbType,
							bulkCopyType, bulkCopyOptionsType, rowsCopiedEventHandlerType, rowsCopiedEventArgs, bulkCopyColumnMappingCollection, bulkCopyColumnMappingType);

						var dbTypeBuilder = typeMapper.Type<DB2Parameter>().Member(p => p.DB2Type);
						TypeSetter        = dbTypeBuilder.BuildSetter<IDbDataParameter>();
						TypeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						//var serverTypeBuilder = typeMapper.Type<DB2Connection>().Member(p => p.eServerType);
						//ServerTypeGetter = serverTypeBuilder.BuildGetter<IDbConnection>();

						_typeMapper = typeMapper;

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

						DB2Tools.Initialized();

						Type? loadType(string typeName, DataType dataType, bool optional = false)
						{
							var type = assembly.GetType($"IBM.Data.DB2Types.{typeName}", !optional);
							if (type == null)
								return null;

							var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object))).Compile();

							mappingSchema.AddScalarType(type, getNullValue(), true, dataType);

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
			DB2_390,
			DB2_400,
			DB2_IDS,
			DB2_UNKNOWN,
			DB2_UW,
			DB2_VM,
			DB2_VM_VSE,
			DB2_VSE
		}

		[Wrapper]
		internal class DB2Connection
		{
			internal DB2ServerTypes eServerType { get; }
		}

		[Wrapper]
		internal class DB2Parameter
		{
			public DB2Type DB2Type { get; set; }
		}

		[Wrapper]
		internal enum DB2Type
		{
			BigInt,
			BigSerial,
			Binary,
			BinaryXml,
			Blob,
			Boolean,
			Byte,
			Char,
			Char1,
			Clob,
			Cursor,
			Datalink,
			Date,
			DateTime,
			DbClob,
			Decimal,
			DecimalFloat,
			Double,
			DynArray,
			Float,
			Graphic,
			Int8,
			Integer,
			IntervalDayFraction,
			IntervalYearMonth,
			Invalid,
			List,
			LongVarBinary,
			LongVarChar,
			LongVarGraphic,
			Money,
			MultiSet,
			NChar,
			Null,
			Numeric,
			NVarChar,
			Other,
			Real,
			Real370,
			Row,
			RowId,
			Serial,
			Serial8,
			Set,
			SmallFloat,
			SmallInt,
			SmartLobLocator,
			SQLUDTFixed,
			SQLUDTVar,
			Text,
			Time,
			Timestamp,
			TimeStampWithTimeZone,
			VarBinary,
			VarChar,
			VarGraphic,
			Xml
		}

		#region BulkCopy
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
		public class DB2RowsCopiedEventArgs : EventArgs
		{
			public int RowsCopied { get; }

			public bool Abort { get; set; }
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

		[Wrapper]
		internal enum DB2BulkCopyOptions
		{
			Default,
			KeepIdentity,
			TableLock,
			Truncate
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
