using System;
using System.Data;

namespace LinqToDB.DataProvider.Oracle
{
	using System.Data.Common;
	using System.Linq.Expressions;
	using System.Reflection;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	internal static class OracleWrappers
	{
#if NET45 || NET46
		public static string NativeAssemblyName = "Oracle.DataAccess";
#endif
		public static string ManagedAssemblyName = "Oracle.ManagedDataAccess";

		private static object _nativeSyncRoot   = new object();
		private static object _managedSyncRoot  = new object();

		private static IOracleWrapper? _nativeWrapper;
		private static IOracleWrapper? _managedWrapper;

		internal interface IOracleWrapper
		{
			Type ParameterType   { get; }
			Type DataReaderType  { get; }
			Type CommandType     { get; }
			Type ConnectionType  { get; }
			Type TransactionType { get; }

			Action<IDbDataParameter, OracleDbType> TypeSetter { get; }
			Func<IDbDataParameter, OracleDbType>   TypeGetter { get; }

			Func<IDbConnection, string> HostNameGetter     { get; }
			Func<IDbConnection, string> DatabaseNameGetter { get; }

			Action<IDbCommand, bool> BindByNameSetter           { get; }
			Action<IDbCommand, int>  ArrayBindCountSetter       { get; }
			Action<IDbCommand, int>  InitialLONGFetchSizeSetter { get; }

			IBulkCopyWrapper? BulkCopy { get; }

			Type  OracleBFileType        { get; }
			Type  OracleBinaryType       { get; }
			Type  OracleBlobType         { get; }
			Type  OracleClobType         { get; }
			Type  OracleDateType         { get; }
			Type  OracleDecimalType      { get; }
			Type  OracleIntervalDSType   { get; }
			Type  OracleIntervalYMType   { get; }
			Type  OracleStringType       { get; }
			Type  OracleTimeStampType    { get; }
			Type  OracleTimeStampLTZType { get; }
			Type  OracleTimeStampTZType  { get; }
			Type  OracleXmlTypeType      { get; }
			Type  OracleXmlStreamType    { get; }
			Type  OracleRefCursorType    { get; }
			Type? OracleRefType          { get; }

			Expression<Func<IDataReader, int, DateTimeOffset>> ReadDateTimeOffsetFromOracleTimeStampTZ  { get; }
			Expression<Func<IDataReader, int, DateTimeOffset>> ReadDateTimeOffsetFromOracleTimeStampLTZ { get; }
			Expression<Func<IDataReader, int, decimal>>        ReadOracleDecimalToDecimalAdv            { get; }
			Expression<Func<IDataReader, int, int>>            ReadOracleDecimalToInt                   { get; }
			Expression<Func<IDataReader, int, long>>           ReadOracleDecimalToLong                  { get; }
			Expression<Func<IDataReader, int, decimal>>        ReadOracleDecimalToDecimal               { get; }

			object CreateOracleTimeStampTZ(DateTimeOffset dto, string offset);
		}

		internal interface IBulkCopyWrapper
		{
			OracleBulkCopy              CreateBulkCopy             (IDbConnection connection, OracleBulkCopyOptions options);
			OracleBulkCopyColumnMapping CreateBulkCopyColumnMapping(int source, string destination);
		}

		class BulkCopyWrapper : IBulkCopyWrapper
		{
			private readonly TypeMapper _typeMapper;

			internal BulkCopyWrapper(TypeMapper typeMapper)
			{
				_typeMapper = typeMapper;
			}

			OracleBulkCopy IBulkCopyWrapper.CreateBulkCopy(IDbConnection connection, OracleBulkCopyOptions options)
				=> _typeMapper!.CreateAndWrap(() => new OracleBulkCopy((OracleConnection)connection, options))!;
			OracleBulkCopyColumnMapping IBulkCopyWrapper.CreateBulkCopyColumnMapping(int source, string destination)
				=> _typeMapper!.CreateAndWrap(() => new OracleBulkCopyColumnMapping(source, destination))!;
		}

		class OracleWrapper : IOracleWrapper
		{
			private readonly TypeMapper _typeMapper;

			private readonly Type _connectionType;
			private readonly Type _transactionTypeType;
			private readonly Type _dataReaderType;
			private readonly Type _parameterType;
			private readonly Type _commandType;

			private readonly Action<IDbDataParameter, OracleDbType> _typeSetter;
			private readonly Func<IDbDataParameter, OracleDbType>   _typeGetter;

			private readonly Func<IDbConnection, string> _hostNameGetter;
			private readonly Func<IDbConnection, string> _databaseNameGetter;

			private readonly IBulkCopyWrapper? _bulkCopy;

			private readonly Action<IDbCommand, bool> _bindByNameSetter;
			private readonly Action<IDbCommand, int>  _arrayBindCountSetter;
			private readonly Action<IDbCommand, int>  _initialLONGFetchSizeSetter;

			private readonly Expression<Func<IDataReader, int, DateTimeOffset>> _readDateTimeOffsetFromOracleTimeStampTZ;
			private readonly Expression<Func<IDataReader, int, DateTimeOffset>> _readDateTimeOffsetFromOracleTimeStampLTZ;
			private readonly Expression<Func<IDataReader, int, decimal>>        _readOracleDecimalToDecimalAdv;
			private readonly Expression<Func<IDataReader, int, int>>            _readOracleDecimalToInt;
			private readonly Expression<Func<IDataReader, int, long>>           _readOracleDecimalToLong;
			private readonly Expression<Func<IDataReader, int, decimal>>        _readOracleDecimalToDecimal;


			private readonly Type _oracleBFileType;
			private readonly Type _oracleBinaryType;
			private readonly Type _oracleBlobType;
			private readonly Type _oracleClobType;
			private readonly Type _oracleDateType;
			private readonly Type _oracleDecimalType;
			private readonly Type _oracleIntervalDSType;
			private readonly Type _oracleIntervalYMType;
			private readonly Type _oracleStringType;
			private readonly Type _oracleTimeStampType;
			private readonly Type _oracleTimeStampLTZType;
			private readonly Type _oracleTimeStampTZType;
			private readonly Type _oracleXmlTypeType;
			private readonly Type _oracleXmlStreamType;
			private readonly Type _oracleRefCursorType;
			private readonly Type? _oracleRefType;

			OracleWrapper(
				TypeMapper typeMapper,
				Type connectionType,
				Type parameterType,
				Type dataReaderType,
				Type transactionTypeType,
				Type commandType,
				Action<IDbDataParameter, OracleDbType> typeSetter,
				Func<IDbDataParameter  , OracleDbType> typeGetter,
				Func<IDbConnection, string> hostNameGetter,
				Func<IDbConnection, string> databaseNameGetter,
				IBulkCopyWrapper? bulkCopy,

				Action<IDbCommand, bool> bindByNameSetter,
				Action<IDbCommand, int>  arrayBindCountSetter,
				Action<IDbCommand, int>  initialLONGFetchSizeSetter,

				Type  oracleBFileType,
				Type  oracleBinaryType,
				Type  oracleBlobType,
				Type  oracleClobType,
				Type  oracleDateType,
				Type  oracleDecimalType,
				Type  oracleIntervalDSType,
				Type  oracleIntervalYMType,
				Type  oracleStringType,
				Type  oracleTimeStampType,
				Type  oracleTimeStampLTZType,
				Type  oracleTimeStampTZType,
				Type  oracleXmlTypeType,
				Type  oracleXmlStreamType,
				Type  oracleRefCursorType,
				Type? oracleRefType)
			{
				_typeMapper          = typeMapper;

				_connectionType      = connectionType;
				_dataReaderType      = dataReaderType;
				_transactionTypeType = transactionTypeType;
				_parameterType       = parameterType;
				_commandType         = commandType;
				_typeSetter          = typeSetter;
				_typeGetter          = typeGetter;
				_hostNameGetter      = hostNameGetter;
				_databaseNameGetter  = databaseNameGetter;
				_bulkCopy            = bulkCopy;

				_bindByNameSetter           = bindByNameSetter;
				_arrayBindCountSetter       = arrayBindCountSetter;
				_initialLONGFetchSizeSetter = initialLONGFetchSizeSetter;

				_oracleBFileType        = oracleBFileType;
				_oracleBinaryType       = oracleBinaryType;
				_oracleBlobType         = oracleBlobType;
				_oracleClobType         = oracleClobType;
				_oracleDateType         = oracleDateType;
				_oracleDecimalType      = oracleDecimalType;
				_oracleIntervalDSType   = oracleIntervalDSType;
				_oracleIntervalYMType   = oracleIntervalYMType;
				_oracleStringType       = oracleStringType;
				_oracleTimeStampType    = oracleTimeStampType;
				_oracleTimeStampLTZType = oracleTimeStampLTZType;
				_oracleTimeStampTZType  = oracleTimeStampTZType;
				_oracleXmlTypeType      = oracleXmlTypeType;
				_oracleXmlStreamType    = oracleXmlStreamType;
				_oracleRefCursorType    = oracleRefCursorType;
				_oracleRefType          = oracleRefType;

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
				var body         = generator.Build();
				_readDateTimeOffsetFromOracleTimeStampTZ = (Expression<Func<IDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

				// rd.GetOracleTimeStampLTZ(i) => DateTimeOffset
				generator    = new ExpressionGenerator(typeMapper);
				tstzExpr     = generator.MapExpression((IDataReader rd, int i) => ((OracleDataReader)rd).GetOracleTimeStampLTZ(i).ToOracleTimeStampTZ(), rdParam, indexParam);
				tstzVariable = generator.AssignToVariable(tstzExpr, "tstz");
				expr         = generator.MapExpression((OracleTimeStampTZ tstz) => new DateTimeOffset(
					tstz.Year, tstz.Month, tstz.Day,
					tstz.Hour, tstz.Minute, tstz.Second,
					tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick), tstzVariable);
				generator.AddExpression(expr);
				body         = generator.Build();
				_readDateTimeOffsetFromOracleTimeStampLTZ = (Expression<Func<IDataReader, int, DateTimeOffset>>)Expression.Lambda(body, rdParam, indexParam);

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
								Expression.Break(label, decimalVar),
								Expression.Constant(0)),
							Expression.Catch(
								typeof(OverflowException),
								Expression.Block(
									Expression.IfThen(
										Expression.LessThanOrEqual(Expression.SubtractAssign(precision, Expression.Constant(1)), Expression.Constant(26)),
										Expression.Rethrow()),
									Expression.Constant(0)))),
						label));

				body = generator.Build();

				_readOracleDecimalToDecimalAdv = (Expression<Func<IDataReader, int, decimal>>)Expression.Lambda(body, rdParam, indexParam);
				// workaround for mapper issue with complex reader expressions handling
				// https://github.com/linq2db/linq2db/issues/2032
				var compiledReader             = _readOracleDecimalToDecimalAdv.Compile();
				_readOracleDecimalToDecimalAdv = (Expression<Func<IDataReader, int, decimal>>)Expression.Lambda(
					Expression.Invoke(Expression.Constant(compiledReader), rdParam, indexParam),
					rdParam,
					indexParam);

				_readOracleDecimalToInt = (Expression<Func<IDataReader, int, int>>)    typeMapper.MapLambda<IDataReader, int, int    >((rd, i) => (int) (decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));
				_readOracleDecimalToLong    = (Expression<Func<IDataReader, int, long>>)   typeMapper.MapLambda<IDataReader, int, long   >((rd, i) => (long)(decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));
				_readOracleDecimalToDecimal = (Expression<Func<IDataReader, int, decimal>>)typeMapper.MapLambda<IDataReader, int, decimal>((rd, i) =>       (decimal)OracleDecimal.SetPrecision(((OracleDataReader)rd).GetOracleDecimal(i), 27));
			}

			private static DateTimeOffset ToDateTimeOffset(object value)
			{
				dynamic tstz = value;

				return new DateTimeOffset(
					tstz.Year, tstz.Month, tstz.Day,
					tstz.Hour, tstz.Minute, tstz.Second,
					tstz.GetTimeZoneOffset()).AddTicks(tstz.Nanosecond / NanosecondsPerTick);
			}

			IBulkCopyWrapper? IOracleWrapper.BulkCopy => _bulkCopy;

			Type IOracleWrapper.ConnectionType  => _connectionType;
			Type IOracleWrapper.TransactionType => _transactionTypeType;
			Type IOracleWrapper.DataReaderType  => _dataReaderType;
			Type IOracleWrapper.ParameterType   => _parameterType;
			Type IOracleWrapper.CommandType     => _commandType;

			Action<IDbDataParameter, OracleDbType> IOracleWrapper.TypeSetter => _typeSetter;
			Func<IDbDataParameter, OracleDbType> IOracleWrapper.TypeGetter   => _typeGetter;

			Func<IDbConnection, string> IOracleWrapper.HostNameGetter     => _hostNameGetter;
			Func<IDbConnection, string> IOracleWrapper.DatabaseNameGetter => _databaseNameGetter;

			Action<IDbCommand, bool> IOracleWrapper.BindByNameSetter           => _bindByNameSetter;
			Action<IDbCommand, int>  IOracleWrapper.ArrayBindCountSetter       => _arrayBindCountSetter;
			Action<IDbCommand, int>  IOracleWrapper.InitialLONGFetchSizeSetter => _initialLONGFetchSizeSetter;

			Expression<Func<IDataReader, int, DateTimeOffset>> IOracleWrapper.ReadDateTimeOffsetFromOracleTimeStampTZ  => _readDateTimeOffsetFromOracleTimeStampTZ;
			Expression<Func<IDataReader, int, DateTimeOffset>> IOracleWrapper.ReadDateTimeOffsetFromOracleTimeStampLTZ => _readDateTimeOffsetFromOracleTimeStampLTZ;
			Expression<Func<IDataReader, int, decimal>>        IOracleWrapper.ReadOracleDecimalToDecimalAdv            => _readOracleDecimalToDecimalAdv;
			Expression<Func<IDataReader, int, int>>            IOracleWrapper.ReadOracleDecimalToInt                   => _readOracleDecimalToInt;
			Expression<Func<IDataReader, int, long>>           IOracleWrapper.ReadOracleDecimalToLong                  => _readOracleDecimalToLong;
			Expression<Func<IDataReader, int, decimal>>        IOracleWrapper.ReadOracleDecimalToDecimal               => _readOracleDecimalToDecimal;

			Type IOracleWrapper.OracleBFileType        => _oracleBFileType;
			Type IOracleWrapper.OracleBinaryType       => _oracleBinaryType;
			Type IOracleWrapper.OracleBlobType         => _oracleBlobType;
			Type IOracleWrapper.OracleClobType         => _oracleClobType;
			Type IOracleWrapper.OracleDateType         => _oracleDateType;
			Type IOracleWrapper.OracleDecimalType      => _oracleDecimalType;
			Type IOracleWrapper.OracleIntervalDSType   => _oracleIntervalDSType;
			Type IOracleWrapper.OracleIntervalYMType   => _oracleIntervalYMType;
			Type IOracleWrapper.OracleStringType       => _oracleStringType;
			Type IOracleWrapper.OracleTimeStampType    => _oracleTimeStampType;
			Type IOracleWrapper.OracleTimeStampLTZType => _oracleTimeStampLTZType;
			Type IOracleWrapper.OracleTimeStampTZType  => _oracleTimeStampTZType;
			Type IOracleWrapper.OracleXmlTypeType      => _oracleXmlTypeType;
			Type IOracleWrapper.OracleXmlStreamType    => _oracleXmlStreamType;
			Type IOracleWrapper.OracleRefCursorType    => _oracleRefCursorType;
			Type? IOracleWrapper.OracleRefType         => _oracleRefType;

			const int DecimalPrecision   = 29;
			const int NanosecondsPerTick = 100;

			object IOracleWrapper.CreateOracleTimeStampTZ(DateTimeOffset dto, string offset)
			{
				return _typeMapper.CreateAndWrap(() => new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, GetDateTimeOffsetNanoseconds(dto), offset))!.instance_!;
			}

			private static int GetDateTimeOffsetNanoseconds(DateTimeOffset value)
			{
				var tmp = new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

				return Convert.ToInt32((value.Ticks - tmp.Ticks) * NanosecondsPerTick);
			}

			internal static IOracleWrapper Initialize(bool native, MappingSchema mappingSchema)
			{
				var assemblyName = native ? "Oracle.DataAccess" : "Oracle.ManagedDataAccess";
				var clientNamespace = assemblyName + ".Client";
				var typesNamespace  = assemblyName + ".Types";

				Assembly assembly;
#if NET45 || NET46
				if (native)
				{
					assembly = Type.GetType($"{clientNamespace}.OracleConnection, {assemblyName}", false)?.Assembly
							?? DbProviderFactories.GetFactory("Oracle.DataAccess.Client").GetType().Assembly;
				}
				else
#endif
				{
					assembly = Type.GetType($"{clientNamespace}.OracleConnection, {assemblyName}", true).Assembly;
				}

				var connectionType  = assembly.GetType($"{clientNamespace}.OracleConnection", true);
				var parameterType   = assembly.GetType($"{clientNamespace}.OracleParameter", true);
				var dataReaderType  = assembly.GetType($"{clientNamespace}.OracleDataReader", true);
				var transactionType = assembly.GetType($"{clientNamespace}.OracleTransaction", true);
				var dbType          = assembly.GetType($"{clientNamespace}.OracleDbType", true);
				var commandType     = assembly.GetType($"{clientNamespace}.OracleCommand", true);

				// do not set default conversion for BFile as it could be converted to file name, byte[], Stream and we don't know what user needs
				var oracleBFileType        = loadType("OracleBFile"       , DataType.BFile                  , skipConvertExpression: true)!;
				var oracleBinaryType       = loadType("OracleBinary"      , DataType.VarBinary              )!;
				var oracleBlobType         = loadType("OracleBlob"        , DataType.Blob                   )!;
				var oracleClobType         = loadType("OracleClob"        , DataType.NText                  )!;
				var oracleDateType         = loadType("OracleDate"        , DataType.DateTime               )!;
				var oracleDecimalType      = loadType("OracleDecimal"     , DataType.Decimal                )!;
				var oracleIntervalDSType   = loadType("OracleIntervalDS"  , DataType.Time                   )!;
				var oracleIntervalYMType   = loadType("OracleIntervalYM"  , DataType.Date                   )!;
				var oracleStringType       = loadType("OracleString"      , DataType.NVarChar               )!;
				var oracleTimeStampType    = loadType("OracleTimeStamp"   , DataType.DateTime2              )!;
				var oracleTimeStampLTZType = loadType("OracleTimeStampLTZ", DataType.DateTimeOffset         )!;
				var oracleTimeStampTZType  = loadType("OracleTimeStampTZ" , DataType.DateTimeOffset         )!;
				var oracleXmlTypeType      = loadType("OracleXmlType"     , DataType.Xml                    )!;
				var oracleXmlStreamType    = loadType("OracleXmlStream"   , DataType.Xml, true, false       )!;
				var oracleRefCursorType    = loadType("OracleRefCursor"   , DataType.Binary, hasValue: false)!;
				var oracleRefType          = loadType("OracleRef"         , DataType.Binary, true           );

				IBulkCopyWrapper? bulkCopy = null;
				TypeMapper typeMapper;
				if (native)
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

					bulkCopy = new BulkCopyWrapper(typeMapper);
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

				if (native)
				{
					// bulk copy types
					typeMapper.RegisterWrapper<OracleBulkCopy>();
					typeMapper.RegisterWrapper<OracleBulkCopyOptions>();
					typeMapper.RegisterWrapper<OracleRowsCopiedEventHandler>();
					typeMapper.RegisterWrapper<OracleBulkCopyColumnMapping>();
					typeMapper.RegisterWrapper<OracleBulkCopyColumnMappingCollection>();
					typeMapper.RegisterWrapper<OracleRowsCopiedEventArgs>();
				}

				var paramMapper   = typeMapper.Type<OracleParameter>();
				var dbTypeBuilder = paramMapper.Member(p => p.OracleDbType);

				var connectionMapper = typeMapper.Type<OracleConnection>();

				var commandMapper = typeMapper.Type<OracleCommand>();

				return new OracleWrapper(
					typeMapper,
					connectionType,
					parameterType,
					dataReaderType,
					transactionType,
					commandType,
					dbTypeBuilder.BuildSetter<IDbDataParameter>(),
					dbTypeBuilder.BuildGetter<IDbDataParameter>(),
					connectionMapper.Member(c => c.HostName)    .BuildGetter<IDbConnection>(),
					connectionMapper.Member(c => c.DatabaseName).BuildGetter<IDbConnection>(),
					bulkCopy,

					commandMapper.Member(p => p.BindByName          ).BuildSetter<IDbCommand>(),
					commandMapper.Member(p => p.ArrayBindCount      ).BuildSetter<IDbCommand>(),
					commandMapper.Member(p => p.InitialLONGFetchSize).BuildSetter<IDbCommand>(),

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
					oracleRefType);

				Type? loadType(string typeName, DataType dataType, bool optional = false, bool hasNull = true, bool hasValue = true, bool skipConvertExpression = false)
				{
					var type = assembly.GetType($"{typesNamespace}.{typeName}", !optional);
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
		}

		internal static IOracleWrapper Initialize(OracleDataProvider provider)
		{
			if (provider.Name == ProviderName.OracleNative)
			{
				if (_nativeWrapper == null)
				{
					lock (_nativeSyncRoot)
					{
						if (_nativeWrapper == null)
						{
							_nativeWrapper = OracleWrapper.Initialize(true, provider.MappingSchema);
						}
					}
				}

				return _nativeWrapper;
			}
			else
			{
				if (_managedWrapper == null)
				{
					lock (_managedSyncRoot)
					{
						if (_managedWrapper == null)
						{
							_managedWrapper = OracleWrapper.Initialize(false, provider.MappingSchema);
						}
					}
				}

				return _managedWrapper;
			}
		}

		[Wrapper]
		internal class OracleParameter
		{
			public OracleDbType OracleDbType { get; set; }
		}

		[Wrapper]
		internal class OracleDataReader
		{
			public OracleTimeStampTZ  GetOracleTimeStampTZ (int i) => throw new NotImplementedException();
			public OracleTimeStampLTZ GetOracleTimeStampLTZ(int i) => throw new NotImplementedException();
			public OracleDecimal      GetOracleDecimal     (int i) => throw new NotImplementedException();
		}

		[Wrapper]
		internal enum OracleDbType
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
		internal class OracleConnection : TypeWrapper
		{
			public string HostName     => this.Wrap(t => t.HostName);
			public string DatabaseName => this.Wrap(t => t.DatabaseName);
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

			public int    Year       => throw new NotImplementedException();
			public int    Month      => throw new NotImplementedException();
			public int    Day        => throw new NotImplementedException();
			public int    Hour       => throw new NotImplementedException();
			public int    Minute     => throw new NotImplementedException();
			public int    Second     => throw new NotImplementedException();
			public int    Nanosecond => throw new NotImplementedException();
			public string TimeZone   => throw new NotImplementedException();

			public TimeSpan GetTimeZoneOffset() => throw new NotImplementedException();
		}

		#region BulkCopy
		[Wrapper]
		internal class OracleBulkCopy : TypeWrapper, IDisposable
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
		internal delegate void OracleRowsCopiedEventHandler(object sender, OracleRowsCopiedEventArgs e);

		[Wrapper]
		internal class OracleBulkCopyColumnMappingCollection : TypeWrapper
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
		internal class OracleBulkCopyColumnMapping : TypeWrapper
		{
			public OracleBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public OracleBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
