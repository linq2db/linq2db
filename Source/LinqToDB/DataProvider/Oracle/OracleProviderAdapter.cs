using System;
using System.Data;

namespace LinqToDB.DataProvider.Oracle
{
	using System.Linq.Expressions;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	public class OracleProviderAdapter : IDynamicProviderAdapter
	{
		const int NanosecondsPerTick = 100;

#if NET45 || NET46
		private static readonly object _nativeSyncRoot = new object();

		public const string NativeAssemblyName        = "Oracle.DataAccess";
		public const string NativeProviderFactoryName = "Oracle.DataAccess.Client";
		public const string NativeClientNamespace     = "Oracle.DataAccess.Client";
		public const string NativeTypesNamespace      = "Oracle.DataAccess.Types";

		private static OracleProviderAdapter? _nativeAdapter;
#endif

		private static readonly object _managedSyncRoot = new object();

		public const string ManagedAssemblyName    = "Oracle.ManagedDataAccess";
		public const string ManagedClientNamespace = "Oracle.ManagedDataAccess.Client";
		public const string ManagedTypesNamespace  = "Oracle.ManagedDataAccess.Types";

		private static OracleProviderAdapter? _managedAdapter;

		private OracleProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			MappingSchema mappingSchema,

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
			Type oracleTimeStampLTZType,
			Type oracleTimeStampTZType,
			Type oracleXmlTypeType,
			Type oracleXmlStreamType,
			Type oracleRefCursorType,
			Type? oracleRefType,

			Func<string, OracleConnection> connectionCreator,

			string typesNamespace,

			Action<IDbDataParameter, OracleDbType> dbTypeSetter,
			Func  <IDbDataParameter, OracleDbType> dbTypeGetter,

			Func<IDbConnection, string> hostNameGetter,
			Func<IDbConnection, string> databaseNameGetter,

			Action<IDbCommand, bool> bindByNameSetter,
			Action<IDbCommand, int> arrayBindCountSetter,
			Action<IDbCommand, int> initialLONGFetchSizeSetter,

			Func<DateTimeOffset, string, object> createOracleTimeStampTZ,

			Expression<Func<IDataReader, int, DateTimeOffset>> readDateTimeOffsetFromOracleTimeStampTZ,
			Expression<Func<IDataReader, int, DateTimeOffset>> readDateTimeOffsetFromOracleTimeStampLTZ,
			Expression<Func<IDataReader, int, decimal>> readOracleDecimalToDecimalAdv,
			Expression<Func<IDataReader, int, int>> readOracleDecimalToInt,
			Expression<Func<IDataReader, int, long>> readOracleDecimalToLong,
			Expression<Func<IDataReader, int, decimal>> readOracleDecimalToDecimal,

			BulkCopyAdapter? bulkCopy)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			MappingSchema = mappingSchema;

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

			_connectionCreator = connectionCreator;

			ProviderTypesNamespace = typesNamespace;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			GetHostName     = hostNameGetter;
			GetDatabaseName = databaseNameGetter;

			SetBindByName           = bindByNameSetter;
			SetArrayBindCount       = arrayBindCountSetter;
			SetInitialLONGFetchSize = initialLONGFetchSizeSetter;

			_createOracleTimeStampTZ = createOracleTimeStampTZ;

			ReadDateTimeOffsetFromOracleTimeStampTZ  = readDateTimeOffsetFromOracleTimeStampTZ;
			ReadDateTimeOffsetFromOracleTimeStampLTZ = readDateTimeOffsetFromOracleTimeStampLTZ;
			ReadOracleDecimalToDecimalAdv            = readOracleDecimalToDecimalAdv;
			ReadOracleDecimalToInt                   = readOracleDecimalToInt;
			ReadOracleDecimalToLong                  = readOracleDecimalToLong;
			ReadOracleDecimalToDecimal               = readOracleDecimalToDecimal;

			BulkCopy = bulkCopy;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; }

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
		public Type  OracleTimeStampLTZType { get; }
		public Type  OracleTimeStampTZType  { get; }
		public Type  OracleXmlTypeType      { get; }
		public Type  OracleXmlStreamType    { get; }
		public Type  OracleRefCursorType    { get; }
		public Type? OracleRefType          { get; }

		public string  GetOracleBFileReaderMethod        => "GetOracleBFile";
		public string  GetOracleBinaryReaderMethod       => "GetOracleBinary";
		public string  GetOracleBlobReaderMethod         => "GetOracleBlob";
		public string  GetOracleClobReaderMethod         => "GetOracleClob";
		public string  GetOracleDateReaderMethod         => "GetOracleDate";
		public string  GetOracleDecimalReaderMethod      => "GetOracleDecimal";
		public string  GetOracleIntervalDSReaderMethod   => "GetOracleIntervalDS";
		public string  GetOracleIntervalYMReaderMethod   => "GetOracleIntervalYM";
		public string  GetOracleStringReaderMethod       => "GetOracleString";
		public string  GetOracleTimeStampReaderMethod    => "GetOracleTimeStamp";
		public string  GetOracleTimeStampLTZReaderMethod => "GetOracleTimeStampLTZ";
		public string  GetOracleTimeStampTZReaderMethod  => "GetOracleTimeStampTZ";
		public string  GetOracleXmlTypeReaderMethod      => "GetOracleXmlType";
		public string? GetOracleRefReaderMethod          => OracleRefType == null ? null : "GetOracleRef";

		public string ProviderTypesNamespace { get; }

		public Action<IDbDataParameter, OracleDbType> SetDbType { get; }
		public Func  <IDbDataParameter, OracleDbType> GetDbType { get; }

		public Func<IDbConnection, string> GetHostName     { get; }
		public Func<IDbConnection, string> GetDatabaseName { get; }

		public Action<IDbCommand, bool> SetBindByName           { get; }
		public Action<IDbCommand, int>  SetArrayBindCount       { get; }
		public Action<IDbCommand, int>  SetInitialLONGFetchSize { get; }

		public Expression<Func<IDataReader, int, DateTimeOffset>> ReadDateTimeOffsetFromOracleTimeStampTZ  { get; }
		public Expression<Func<IDataReader, int, DateTimeOffset>> ReadDateTimeOffsetFromOracleTimeStampLTZ { get; }
		public Expression<Func<IDataReader, int, decimal>>        ReadOracleDecimalToDecimalAdv            { get; }
		public Expression<Func<IDataReader, int, int>>            ReadOracleDecimalToInt                   { get; }
		public Expression<Func<IDataReader, int, long>>           ReadOracleDecimalToLong                  { get; }
		public Expression<Func<IDataReader, int, decimal>>        ReadOracleDecimalToDecimal               { get; }

		private readonly Func<DateTimeOffset, string, object> _createOracleTimeStampTZ;
		public object CreateOracleTimeStampTZ(DateTimeOffset dto, string offset) => _createOracleTimeStampTZ(dto, offset);

		private readonly Func<string, OracleConnection> _connectionCreator;
		public OracleConnection CreateConnection(string connectionString) => _connectionCreator(connectionString);

		public BulkCopyAdapter? BulkCopy { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<IDbConnection, OracleBulkCopyOptions, OracleBulkCopy> bulkCopyCreator,
				Func<int, string, OracleBulkCopyColumnMapping> bulkCopyColumnMappingCreator)
			{
				Create = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<IDbConnection, OracleBulkCopyOptions, OracleBulkCopy> Create { get; }
			public Func<int, string, OracleBulkCopyColumnMapping> CreateColumnMapping { get; }
		}

		public static OracleProviderAdapter GetInstance(string name)
		{
#if NET45 || NET46
			if (name == ProviderName.OracleNative)
			{
				if (_nativeAdapter == null)
					lock (_nativeSyncRoot)
						if (_nativeAdapter == null)
							_nativeAdapter = CreateAdapter(NativeAssemblyName, NativeClientNamespace, NativeTypesNamespace, NativeProviderFactoryName);

				return _nativeAdapter;
			}
			else
#endif
			{
				if (_managedAdapter == null)
					lock (_managedSyncRoot)
						if (_managedAdapter == null)
							_managedAdapter = CreateAdapter(ManagedAssemblyName, ManagedClientNamespace, ManagedTypesNamespace, null);

				return _managedAdapter;
			}
		}

		private static OracleProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string typesNamespace, string? factoryName)
		{
			var isNative = false;
#if NET45 || NET46
			isNative = assemblyName == NativeAssemblyName;
#endif

			var assembly = Common.Tools.TryLoadAssembly(assemblyName, factoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType  = assembly.GetType($"{clientNamespace}.OracleConnection" , true);
			var parameterType   = assembly.GetType($"{clientNamespace}.OracleParameter"  , true);
			var dataReaderType  = assembly.GetType($"{clientNamespace}.OracleDataReader" , true);
			var transactionType = assembly.GetType($"{clientNamespace}.OracleTransaction", true);
			var dbType          = assembly.GetType($"{clientNamespace}.OracleDbType"     , true);
			var commandType     = assembly.GetType($"{clientNamespace}.OracleCommand"    , true);

			var mappingSchema = new MappingSchema();

			// do not set default conversion for BFile as it could be converted to file name, byte[], Stream and we don't know what user needs
			var oracleBFileType        = loadType("OracleBFile"       , DataType.BFile, skipConvertExpression: true)!;
			var oracleBinaryType       = loadType("OracleBinary"      , DataType.VarBinary)!;
			var oracleBlobType         = loadType("OracleBlob"        , DataType.Blob)!;
			var oracleClobType         = loadType("OracleClob"        , DataType.NText)!;
			var oracleDateType         = loadType("OracleDate"        , DataType.DateTime)!;
			var oracleDecimalType      = loadType("OracleDecimal"     , DataType.Decimal)!;
			var oracleIntervalDSType   = loadType("OracleIntervalDS"  , DataType.Time)!;
			var oracleIntervalYMType   = loadType("OracleIntervalYM"  , DataType.Date)!;
			var oracleStringType       = loadType("OracleString"      , DataType.NVarChar)!;
			var oracleTimeStampType    = loadType("OracleTimeStamp"   , DataType.DateTime2)!;
			var oracleTimeStampLTZType = loadType("OracleTimeStampLTZ", DataType.DateTimeOffset)!;
			var oracleTimeStampTZType  = loadType("OracleTimeStampTZ" , DataType.DateTimeOffset)!;
			var oracleXmlTypeType      = loadType("OracleXmlType"     , DataType.Xml)!;
			var oracleXmlStreamType    = loadType("OracleXmlStream"   , DataType.Xml, true, false)!;
			var oracleRefCursorType    = loadType("OracleRefCursor"   , DataType.Binary, hasValue: false)!;
			var oracleRefType          = loadType("OracleRef"         , DataType.Binary, true);

			BulkCopyAdapter? bulkCopy = null;
			TypeMapper typeMapper;
			if (isNative)
			{
				var bulkCopyType                        = assembly.GetType($"{clientNamespace}.OracleBulkCopy", true);
				var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.OracleBulkCopyOptions", true);
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.OracleRowsCopiedEventHandler", true);
				var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.OracleBulkCopyColumnMapping", true);
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.OracleBulkCopyColumnMappingCollection", true);
				var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.OracleRowsCopiedEventArgs", true);

				typeMapper = new TypeMapper(
					connectionType, parameterType, transactionType, dbType, commandType, dataReaderType,
					oracleTimeStampTZType, oracleTimeStampLTZType, oracleDecimalType,
					bulkCopyType, bulkCopyOptionsType, bulkRowsCopiedEventHandlerType, bulkCopyColumnMappingType, bulkCopyColumnMappingCollectionType, rowsCopiedEventArgsType);

				bulkCopy = new BulkCopyAdapter(
					(IDbConnection connection, OracleBulkCopyOptions options)
						=> typeMapper.CreateAndWrap(() => new OracleBulkCopy((OracleConnection)connection, options))!,
					(int source, string destination)
						=> typeMapper.CreateAndWrap(() => new OracleBulkCopyColumnMapping(source, destination))!);

			}
			else
				typeMapper = new TypeMapper(
					connectionType, parameterType, transactionType, dbType, commandType, dataReaderType,
					oracleTimeStampTZType, oracleTimeStampLTZType, oracleDecimalType);

			typeMapper.RegisterWrapper<OracleConnection>();
			typeMapper.RegisterWrapper<OracleParameter>();
			typeMapper.RegisterWrapper<OracleDbType>();
			typeMapper.RegisterWrapper<OracleTransaction>();
			typeMapper.RegisterWrapper<OracleCommand>();
			typeMapper.RegisterWrapper<OracleDataReader>();

			typeMapper.RegisterWrapper<OracleTimeStampTZ>();
			typeMapper.RegisterWrapper<OracleTimeStampLTZ>();
			typeMapper.RegisterWrapper<OracleDecimal>();

			if (isNative)
			{
				// bulk copy types
				typeMapper.RegisterWrapper<OracleBulkCopy>();
				typeMapper.RegisterWrapper<OracleBulkCopyOptions>();
				typeMapper.RegisterWrapper<OracleRowsCopiedEventHandler>();
				typeMapper.RegisterWrapper<OracleBulkCopyColumnMapping>();
				typeMapper.RegisterWrapper<OracleBulkCopyColumnMappingCollection>();
				typeMapper.RegisterWrapper<OracleRowsCopiedEventArgs>();
			}

			var paramMapper = typeMapper.Type<OracleParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.OracleDbType);

			var connectionMapper = typeMapper.Type<OracleConnection>();

			var commandMapper = typeMapper.Type<OracleCommand>();

			// data reader expressions
			// rd.GetOracleTimeStampTZ(i) => DateTimeOffset
			var generator    = new ExpressionGenerator(typeMapper);
			var rdParam      = Expression.Parameter(typeof(IDataReader), "rd");
			var indexParam   = Expression.Parameter(typeof(int), "i");
			var tstzExpr     = generator.MapExpression((IDataReader rd, int i) => ((OracleDataReader)rd).GetOracleTimeStampTZ(i), rdParam, indexParam);
			var tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			var expr         = generator.MapExpression((OracleTimeStampTZ tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			var body = generator.Build();
			var readDateTimeOffsetFromOracleTimeStampTZ = (Expression<Func<IDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

			// rd.GetOracleTimeStampLTZ(i) => DateTimeOffset
			generator    = new ExpressionGenerator(typeMapper);
			tstzExpr     = generator.MapExpression((IDataReader rd, int i) => ((OracleDataReader)rd).GetOracleTimeStampLTZ(i).ToOracleTimeStampTZ(), rdParam, indexParam);
			tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
			expr         = generator.MapExpression((OracleTimeStampTZ tstz) => new DateTimeOffset(
				tstz.Year, tstz.Month, tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second,
				tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
			generator.AddExpression(expr);
			body = generator.Build();
			var readDateTimeOffsetFromOracleTimeStampLTZ = (Expression<Func<IDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

			// rd.GetOracleDecimal(i) => decimal
			generator            = new ExpressionGenerator(typeMapper);
			var decExpr          = generator.MapExpression((IDataReader rd, int i) => ((OracleDataReader)rd).GetOracleDecimal(i), rdParam, indexParam);
			var oracleDecimalVar = generator.AssignToVariable(decExpr, "dec");
			var precision        = generator.AssignToVariable(Expression.Constant(29), "precision");
			var decimalVar       = generator.AddVariable(Expression.Parameter(typeof(decimal), "dec"));
			var label            = Expression.Label(typeof(decimal));

			generator.AddExpression(
				Expression.Loop(
					Expression.TryCatch(
						Expression.Block(
							Expression.Assign(oracleDecimalVar, generator.MapExpression((OracleDecimal d, int p) => OracleDecimal.SetPrecision(d, p), oracleDecimalVar, precision)),
							Expression.Assign(decimalVar, Expression.Convert(oracleDecimalVar, typeof(decimal))),
							Expression.Break(label, decimalVar)),
						Expression.Catch(
							typeof(OverflowException),
							Expression.Block(
								Expression.IfThen(
									Expression.LessThanOrEqual(Expression.SubtractAssign(precision, Expression.Constant(1)), Expression.Constant(26)),
									Expression.Rethrow())))),
					label));

			body = generator.Build();

			var readOracleDecimalToDecimalAdv = (Expression<Func<IDataReader, int, decimal>>)Expression.Lambda(body, rdParam, indexParam);
			// workaround for mapper issue with complex reader expressions handling
			// https://github.com/linq2db/linq2db/issues/2032
			var compiledReader                = readOracleDecimalToDecimalAdv.Compile();
			readOracleDecimalToDecimalAdv     = (Expression<Func<IDataReader, int, decimal>>)Expression.Lambda(
				Expression.Invoke(Expression.Constant(compiledReader), rdParam, indexParam),
				rdParam,
				indexParam);

			var readOracleDecimalToInt     = (Expression<Func<IDataReader, int, int>>)typeMapper.MapLambda<IDataReader, int, int>((rd, i) => (int)(decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));
			var readOracleDecimalToLong    = (Expression<Func<IDataReader, int, long>>)typeMapper.MapLambda<IDataReader, int, long>((rd, i) => (long)(decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));
			var readOracleDecimalToDecimal = (Expression<Func<IDataReader, int, decimal>>)typeMapper.MapLambda<IDataReader, int, decimal>((rd, i) => (decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));

			return new OracleProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				mappingSchema,

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

				connectionString => typeMapper.CreateAndWrap(() => new OracleConnection(connectionString))!,

				typesNamespace,

				dbTypeBuilder.BuildSetter<IDbDataParameter>(),
				dbTypeBuilder.BuildGetter<IDbDataParameter>(),

				connectionMapper.Member(c => c.HostName).BuildGetter<IDbConnection>(),
				connectionMapper.Member(c => c.DatabaseName).BuildGetter<IDbConnection>(),


				commandMapper.Member(p => p.BindByName).BuildSetter<IDbCommand>(),
				commandMapper.Member(p => p.ArrayBindCount).BuildSetter<IDbCommand>(),
				commandMapper.Member(p => p.InitialLONGFetchSize).BuildSetter<IDbCommand>(),

				(DateTimeOffset dto, string offset)
					=> typeMapper.CreateAndWrap(() => new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset))!.instance_!,

				readDateTimeOffsetFromOracleTimeStampTZ,
				readDateTimeOffsetFromOracleTimeStampLTZ,
				readOracleDecimalToDecimalAdv,
				readOracleDecimalToInt,
				readOracleDecimalToLong,
				readOracleDecimalToDecimal,
				bulkCopy);

			Type? loadType(string typeName, DataType dataType, bool optional = false, bool hasNull = true, bool hasValue = true, bool skipConvertExpression = false)
			{
				var type = assembly!.GetType($"{typesNamespace}.{typeName}", !optional);
				if (type == null)
					return null;

				if (hasNull)
				{
					var getNullValue = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, type, "Null"), typeof(object))).Compile();
					mappingSchema.AddScalarType(type, getNullValue(), true, dataType);
				}
				else
					mappingSchema.AddScalarType(type, null, true, dataType);

				if (skipConvertExpression)
					return type;

				// conversion from provider-specific type
				var valueParam = Expression.Parameter(type);

				Expression memberExpression;
				if (!hasValue)
					memberExpression = valueParam;
				else
					memberExpression = Expression.Property(valueParam, "Value");

				var condition = Expression.Condition(
					Expression.Equal(valueParam, Expression.Field(null, type, "Null")),
					Expression.Constant(null, typeof(object)),
					Expression.Convert(memberExpression, typeof(object)));

				var convertExpression = Expression.Lambda(condition, valueParam);
				mappingSchema.SetConvertExpression(type, typeof(object), convertExpression);

				return type;
			}
		}

		private static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
		{
			var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

			return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
		}

		#region Wrappers

		[Wrapper]
		internal class OracleParameter
		{
			public OracleDbType OracleDbType { get; set; }
		}

		[Wrapper]
		internal class OracleDataReader
		{
			public OracleTimeStampTZ  GetOracleTimeStampTZ(int i)  => throw new NotImplementedException();
			public OracleTimeStampLTZ GetOracleTimeStampLTZ(int i) => throw new NotImplementedException();
			public OracleDecimal      GetOracleDecimal(int i)      => throw new NotImplementedException();
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

			// native provider-only
			Array        = 128,
			Object       = 129,
			Ref          = 130,
		}

		[Wrapper]
		internal class OracleCommand : TypeWrapper
		{
			public int ArrayBindCount
			{
				get => this.Wrap(t => t.ArrayBindCount);
				set => this.SetPropValue(t => t.ArrayBindCount, value);
			}

			public bool BindByName
			{
				get => this.Wrap(t => t.BindByName);
				set => this.SetPropValue(t => t.BindByName, value);
			}

			public int InitialLONGFetchSize
			{
				get => this.Wrap(t => t.InitialLONGFetchSize);
				set => this.SetPropValue(t => t.InitialLONGFetchSize, value);
			}
		}

		[Wrapper]
		public class OracleConnection : TypeWrapper, IDisposable
		{
			public OracleConnection(string connectionString) => throw new NotImplementedException();

			public string HostName => this.Wrap(t => t.HostName);

			public string DatabaseName => this.Wrap(t => t.DatabaseName);

			public void Open() => this.WrapAction(c => c.Open());

			public void Dispose() => this.WrapAction(t => t.Dispose());
		}

		[Wrapper]
		internal class OracleTransaction
		{
		}

		[Wrapper]
		internal class OracleTimeStampLTZ
		{
			public OracleTimeStampTZ ToOracleTimeStampTZ() => throw new NotImplementedException();
		}

		[Wrapper]
		internal class OracleDecimal
		{
			public static OracleDecimal SetPrecision(OracleDecimal value1, int precision) => throw new NotImplementedException();

			public static explicit operator decimal(OracleDecimal value1) => throw new NotImplementedException();
		}

		[Wrapper]
		internal class OracleTimeStampTZ : TypeWrapper
		{
			public OracleTimeStampTZ(object instance, TypeMapper mapper) : base(instance, mapper)
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
		public class OracleBulkCopy : TypeWrapper, IDisposable
		{
			public OracleBulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<OracleBulkCopy, OracleRowsCopiedEventHandler>(nameof(OracleRowsCopied));
			}

			public OracleBulkCopy(OracleConnection connection, OracleBulkCopyOptions options) => throw new NotImplementedException();

			public void Dispose() => this.WrapAction(t => ((IDisposable)t).Dispose());

			public void WriteToServer(IDataReader dataReader) => this.WrapAction(t => t.WriteToServer(dataReader));

			public int NotifyAfter
			{
				get => this.Wrap(t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BatchSize
			{
				get => this.Wrap(t => t.BatchSize);
				set => this.SetPropValue(t => t.BatchSize, value);
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

			public OracleBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event OracleRowsCopiedEventHandler OracleRowsCopied
			{
				add => Events.AddHandler(nameof(OracleRowsCopied), value);
				remove => Events.RemoveHandler(nameof(OracleRowsCopied), value);
			}
		}

		[Wrapper]
		public class OracleRowsCopiedEventArgs : TypeWrapper
		{
			public OracleRowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public long RowsCopied
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
		public delegate void OracleRowsCopiedEventHandler(object sender, OracleRowsCopiedEventArgs e);

		[Wrapper]
		public class OracleBulkCopyColumnMappingCollection : TypeWrapper
		{
			public OracleBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public OracleBulkCopyColumnMapping Add(OracleBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		public enum OracleBulkCopyOptions
		{
			Default                = 0,
			UseInternalTransaction = 1
		}

		[Wrapper]
		public class OracleBulkCopyColumnMapping : TypeWrapper
		{
			public OracleBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public OracleBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion

		#endregion
	}
}
