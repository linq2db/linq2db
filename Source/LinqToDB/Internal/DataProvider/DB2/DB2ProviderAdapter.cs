using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.Types;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.DB2
{
	public class DB2ProviderAdapter : IDynamicProviderAdapter
	{
		public const string ProviderFactoryName  = "IBM.Data.DB2";
		public const string TypesNamespace       = "IBM.Data.DB2Types";
		public const string NetFxClientNamespace = "IBM.Data.DB2";
		public const string CoreClientNamespace  = "IBM.Data.DB2.Core";

#if NETFRAMEWORK
		public const string AssemblyName         = "IBM.Data.DB2";
		public const string ClientNamespace      = "IBM.Data.DB2";
#else
		public const string  AssemblyName        = "IBM.Data.Db2";
		public const string  ClientNamespace     = "IBM.Data.Db2";
		public const string  AssemblyNameOld     = "IBM.Data.DB2.Core";
		public const string  ClientNamespaceOld  = "IBM.Data.DB2.Core";
#endif

		DB2ProviderAdapter()
		{
			var clientNamespace = ClientNamespace;
			var assembly        = Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);

#if !NETFRAMEWORK
			if (assembly == null)
			{
				assembly = Common.Tools.TryLoadAssembly(AssemblyNameOld, ProviderFactoryName);
				if (assembly != null)
					clientNamespace = ClientNamespaceOld;
			}
			else if (assembly.GetName().Name == AssemblyNameOld)
			{
				// cover case when provider factory loaded old assembly
				clientNamespace = ClientNamespaceOld;
			}
#endif

			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

			ConnectionType  = assembly.GetType($"{clientNamespace}.DB2Connection" , true)!;
			ParameterType   = assembly.GetType($"{clientNamespace}.DB2Parameter"  , true)!;
			DataReaderType  = assembly.GetType($"{clientNamespace}.DB2DataReader" , true)!;
			TransactionType = assembly.GetType($"{clientNamespace}.DB2Transaction", true)!;
			CommandType     = assembly.GetType($"{clientNamespace}.DB2Command"    , true)!;

			var dbType          = assembly.GetType($"{clientNamespace}.DB2Type"       , true)!;
			var serverTypesType = assembly.GetType($"{clientNamespace}.DB2ServerTypes", true)!;

			var bulkCopyType                    = assembly.GetType($"{clientNamespace}.DB2BulkCopy"                       , true)!;
			var bulkCopyOptionsType             = assembly.GetType($"{clientNamespace}.DB2BulkCopyOptions"                , true)!;
			var bulkCopyColumnMappingType       = assembly.GetType($"{clientNamespace}.DB2BulkCopyColumnMapping"          , true)!;
			var rowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.DB2RowsCopiedEventHandler"         , true)!;
			var rowsCopiedEventArgs             = assembly.GetType($"{clientNamespace}.DB2RowsCopiedEventArgs"            , true)!;
			var bulkCopyColumnMappingCollection = assembly.GetType($"{clientNamespace}.DB2BulkCopyColumnMappingCollection", true)!;

			MappingSchema = new DB2AdapterMappingSchema();

			DB2BinaryType       = LoadType("DB2Binary"      , DataType.VarBinary)!;
			DB2BlobType         = LoadType("DB2Blob"        , DataType.Blob)!;
			DB2ClobType         = LoadType("DB2Clob"        , DataType.NText)!;
			DB2DateType         = LoadType("DB2Date"        , DataType.Date)!;
			DB2DateTimeType     = LoadType("DB2DateTime"    , DataType.DateTime , true);
			DB2DecimalType      = LoadType("DB2Decimal"     , DataType.Decimal)!;
			DB2DecimalFloatType = LoadType("DB2DecimalFloat", DataType.Decimal)!;
			DB2DoubleType       = LoadType("DB2Double"      , DataType.Double)!;
			DB2Int16Type        = LoadType("DB2Int16"       , DataType.Int16)!;
			DB2Int32Type        = LoadType("DB2Int32"       , DataType.Int32)!;
			DB2Int64Type        = LoadType("DB2Int64"       , DataType.Int64)!;
			DB2RealType         = LoadType("DB2Real"        , DataType.Single)!;
			DB2Real370Type      = LoadType("DB2Real370"     , DataType.Single)!;
			DB2RowIdType        = LoadType("DB2RowId"       , DataType.VarBinary)!;
			DB2StringType       = LoadType("DB2String"      , DataType.NVarChar)!;
			DB2TimeType         = LoadType("DB2Time"        , DataType.Time)!;
			DB2TimeStampType    = LoadType("DB2TimeStamp"   , DataType.DateTime2)!;
			DB2XmlType          = LoadType("DB2Xml"         , DataType.Xml)!;
			DB2TimeSpanType     = LoadType("DB2TimeSpan"    , DataType.Timestamp, true, true);
			// not mapped currently: DB2MonthSpan, DB2SmartLOB, DB2TimeStampOffset, DB2XsrObjectId

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<DB2ServerTypes>(serverTypesType);
			typeMapper.RegisterTypeWrapper<DB2Connection>(ConnectionType);
			typeMapper.RegisterTypeWrapper<DB2Parameter>(ParameterType);
			typeMapper.RegisterTypeWrapper<DB2Type>(dbType);
			typeMapper.RegisterTypeWrapper<DB2Transaction>(TransactionType);
			typeMapper.RegisterTypeWrapper<DB2Binary>(DB2BinaryType);

			// bulk copy types
			typeMapper.RegisterTypeWrapper<DB2BulkCopy>(bulkCopyType);
			typeMapper.RegisterTypeWrapper<DB2RowsCopiedEventArgs>(rowsCopiedEventArgs);
			typeMapper.RegisterTypeWrapper<DB2RowsCopiedEventHandler>(rowsCopiedEventHandlerType);
			typeMapper.RegisterTypeWrapper<DB2BulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollection);
			typeMapper.RegisterTypeWrapper<DB2BulkCopyOptions>(bulkCopyOptionsType);
			typeMapper.RegisterTypeWrapper<DB2BulkCopyColumnMapping>(bulkCopyColumnMappingType);

			typeMapper.FinalizeMappings();

			var db2BinaryBuilder = typeMapper.Type<DB2Binary>().Member(p => p.IsNull);

			IsDB2BinaryNull  = db2BinaryBuilder.BuildGetter<object>();

			var dbTypeBuilder = typeMapper.Type<DB2Parameter>().Member(p => p.DB2Type);

			SetDbType = dbTypeBuilder.BuildSetter<DbParameter>();
			GetDbType = dbTypeBuilder.BuildGetter<DbParameter>();

			BulkCopy = new BulkCopyAdapter(
				typeMapper.BuildWrappedFactory((DbConnection connection, DB2BulkCopyOptions options) => new DB2BulkCopy((DB2Connection)(object)connection, options)),
				typeMapper.BuildWrappedFactory((int source, string destination) => new DB2BulkCopyColumnMapping(source, destination)));

			_connectionFactory = typeMapper.BuildTypedFactory<string, DB2Connection, DbConnection>(connectionString => new DB2Connection(connectionString));
			ConnectionWrapper  = typeMapper.Wrap<DB2Connection>;

			Type? LoadType(string typeName, DataType dataType, bool optional = false, bool obsolete = false, bool register = true)
			{
				var type = assembly!.GetType($"{TypesNamespace}.{typeName}", !optional);

				if (type == null)
					return null;

				if (obsolete && type.HasAttribute<ObsoleteAttribute>(false))
					return null;

				if (register)
				{
					var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(ExpressionHelper.Field(type, "Null"), typeof(object))).CompileExpression();
					MappingSchema.AddScalarType(type, getNullValue(), true, dataType);
				}

				return type;
			}
		}

		static readonly Lazy<DB2ProviderAdapter> _lazy = new (() => new ());
		public static   DB2ProviderAdapter       Instance => _lazy.Value;

		sealed class DB2AdapterMappingSchema : LockedMappingSchema
		{
			public DB2AdapterMappingSchema() : base("DB2Adapter")
			{
			}
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

		public Action<DbParameter, DB2Type> SetDbType { get; }
		public Func  <DbParameter, DB2Type> GetDbType { get; }

		public Func<object, bool> IsDB2BinaryNull { get; }

		internal Func<DbConnection, DB2Connection> ConnectionWrapper { get; }

		public BulkCopyAdapter BulkCopy { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<DbConnection, DB2BulkCopyOptions, DB2BulkCopy> bulkCopyCreator,
				Func<int, string, DB2BulkCopyColumnMapping> bulkCopyColumnMappingCreator)
			{
				Create = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<DbConnection,DB2BulkCopyOptions,DB2BulkCopy> Create              { get; }
			public Func<int,string,DB2BulkCopyColumnMapping>         CreateColumnMapping { get; }
		}

		#region Wrappers

		[Wrapper]
		private sealed class DB2Binary
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
		public class DB2Connection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get eServerType
				(Expression<Func<DB2Connection, DB2ServerTypes>>)(this_ => this_.eServerType),
			};

			public DB2Connection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public DB2Connection(string connectionString) => throw new NotImplementedException();

			// internal actually
			public DB2ServerTypes eServerType => ((Func<DB2Connection, DB2ServerTypes>)CompiledWrappers[0])(this);
		}

		[Wrapper]
		private sealed class DB2Parameter
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
		internal sealed class DB2Transaction
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
				(Expression<Action<DB2BulkCopy>>                                  )(this_ => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<DB2BulkCopy, IDataReader>>                     )((this_, reader) => this_.WriteToServer(reader)),
				// [2]: get NotifyAfter
				(Expression<Func<DB2BulkCopy, int>>                               )(this_ => this_.NotifyAfter),
				// [3]: get BulkCopyTimeout
				(Expression<Func<DB2BulkCopy, int>>                               )(this_ => this_.BulkCopyTimeout),
				// [4]: get DestinationTableName
				(Expression<Func<DB2BulkCopy, string?>>                           )(this_ => this_.DestinationTableName),
				// [5]: get ColumnMappings
				(Expression<Func<DB2BulkCopy, DB2BulkCopyColumnMappingCollection>>)(this_ => this_.ColumnMappings),
				// [6]: set NotifyAfter
				PropertySetter((DB2BulkCopy this_) => this_.NotifyAfter),
				// [7]: set BulkCopyTimeout
				PropertySetter((DB2BulkCopy this_) => this_.BulkCopyTimeout),
				// [8]: set DestinationTableName
				PropertySetter((DB2BulkCopy this_) => this_.DestinationTableName),
				// [9]: set ColumnMappings
				PropertySetter((DB2BulkCopy this_) => this_.ColumnMappings),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(DB2RowsCopied)
			};

			public DB2BulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public DB2BulkCopy(DB2Connection connection, DB2BulkCopyOptions options) => throw new NotImplementedException();

			public void Dispose      ()                       => ((Action<DB2BulkCopy>)CompiledWrappers[0])(this);
#pragma warning disable RS0030 // API mapping must preserve type
			public void WriteToServer(IDataReader dataReader) => ((Action<DB2BulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);
#pragma warning restore RS0030 //  API mapping must preserve type
			public int NotifyAfter
			{
				get => ((Func  <DB2BulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<DB2BulkCopy, int>)CompiledWrappers[6])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func  <DB2BulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<DB2BulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func  <DB2BulkCopy, string?>)CompiledWrappers[4])(this);
				set => ((Action<DB2BulkCopy, string?>)CompiledWrappers[8])(this, value);
			}

			public DB2BulkCopyColumnMappingCollection ColumnMappings
			{
				get => ((Func  <DB2BulkCopy, DB2BulkCopyColumnMappingCollection>)CompiledWrappers[5])(this);
				set => ((Action<DB2BulkCopy, DB2BulkCopyColumnMappingCollection>)CompiledWrappers[9])(this, value);
			}

			private      DB2RowsCopiedEventHandler? _DB2RowsCopied;
			public event DB2RowsCopiedEventHandler?  DB2RowsCopied
			{
				add    => _DB2RowsCopied = (DB2RowsCopiedEventHandler?)Delegate.Combine(_DB2RowsCopied, value);
				remove => _DB2RowsCopied = (DB2RowsCopiedEventHandler?)Delegate.Remove (_DB2RowsCopied, value);
			}
		}

		[Wrapper]
		public class DB2RowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowsCopied
				(Expression<Func<DB2RowsCopiedEventArgs, int>> )(this_ => this_.RowsCopied),
				// [1]: get Abort
				(Expression<Func<DB2RowsCopiedEventArgs, bool>>)(this_ => this_.Abort),
				// [2]: set Abort
				PropertySetter((DB2RowsCopiedEventArgs this_) => this_.Abort),
			};

			public DB2RowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public int RowsCopied => ((Func<DB2RowsCopiedEventArgs, int>)CompiledWrappers[0])(this);

			public bool Abort
			{
				get => ((Func  <DB2RowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => ((Action<DB2RowsCopiedEventArgs, bool>)CompiledWrappers[2])(this, value);
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
				(Expression<Func<DB2BulkCopyColumnMappingCollection, DB2BulkCopyColumnMapping, DB2BulkCopyColumnMapping>>)((this_, column) => this_.Add(column)),
			};

			public DB2BulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
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
			public DB2BulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public DB2BulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
