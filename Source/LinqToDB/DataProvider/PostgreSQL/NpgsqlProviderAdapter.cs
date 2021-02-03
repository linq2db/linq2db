using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	public class NpgsqlProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static NpgsqlProviderAdapter? _instance;

		public const string AssemblyName    = "Npgsql";
		public const string ClientNamespace = "Npgsql";
		public const string TypesNamespace  = "NpgsqlTypes";

		// maps mapped enum value to numeric value, defined in currently used provider
		private readonly IDictionary<NpgsqlDbType, int> _knownDbTypes = new Dictionary<NpgsqlDbType, int>();

		private NpgsqlProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Type dbTypeType,

			MappingSchema mappingSchema,

			Type npgsqlDateType,
			Type npgsqlPointType,
			Type npgsqlLSegType,
			Type npgsqlBoxType,
			Type npgsqlCircleType,
			Type npgsqlPathType,
			Type npgsqlPolygonType,
			Type npgsqlLineType,
			Type npgsqlInetType,
			Type npgsqlTimeSpanType,
			Type npgsqlDateTimeType,
			Type npgsqlRangeTType,

			Func<string, NpgsqlConnection> connectionCreator,

			Action<IDbDataParameter, NpgsqlDbType> dbTypeSetter,
			Func  <IDbDataParameter, NpgsqlDbType> dbTypeGetter,

			Func<IDbConnection, string, NpgsqlBinaryImporter> beginBinaryImport)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			NpgsqlDateType     = npgsqlDateType;
			NpgsqlPointType    = npgsqlPointType;
			NpgsqlLSegType     = npgsqlLSegType;
			NpgsqlBoxType      = npgsqlBoxType;
			NpgsqlCircleType   = npgsqlCircleType;
			NpgsqlPathType     = npgsqlPathType;
			NpgsqlPolygonType  = npgsqlPolygonType;
			NpgsqlLineType     = npgsqlLineType;
			NpgsqlInetType     = npgsqlInetType;
			NpgsqlTimeSpanType = npgsqlTimeSpanType;
			NpgsqlDateTimeType = npgsqlDateTimeType;
			NpgsqlRangeTType   = npgsqlRangeTType;

			MappingSchema      = mappingSchema;
			_connectionCreator = connectionCreator;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;

			BeginBinaryImport          = beginBinaryImport;

			// because NpgsqlDbType enumeration changes often (compared to other providers)
			// we should create lookup list of mapped fields, defined in used npgsql version
			var dbTypeKnownNames    = Enum.GetNames(dbTypeType);
			var dbMappedDbTypeNames = Enum.GetNames(typeof(NpgsqlDbType));
			foreach (var knownTypeName in from nType in dbTypeKnownNames
										  join mType in dbMappedDbTypeNames on nType equals mType
										  select nType)
			{
				// use setter([]) instead of Add() because enum contains duplicate fields with same values
				_knownDbTypes[(NpgsqlDbType)Enum.Parse(typeof(NpgsqlDbType), knownTypeName)] = (int)Enum.Parse(dbTypeType, knownTypeName);
			}
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Type NpgsqlDateType     { get; }
		public Type NpgsqlPointType    { get; }
		public Type NpgsqlLSegType     { get; }
		public Type NpgsqlBoxType      { get; }
		public Type NpgsqlCircleType   { get; }
		public Type NpgsqlPathType     { get; }
		public Type NpgsqlPolygonType  { get; }
		public Type NpgsqlLineType     { get; }
		public Type NpgsqlInetType     { get; }
		public Type NpgsqlTimeSpanType { get; }
		public Type NpgsqlDateTimeType { get; }
		public Type NpgsqlRangeTType   { get; }

		public string GetIntervalReaderMethod => "GetInterval";
		public string GetTimeStampReaderMethod => "GetTimeStamp";
		public string GetDateReaderMethod => "GetDate";

		public string ProviderTypesNamespace => TypesNamespace;

		public Action<IDbDataParameter, NpgsqlDbType> SetDbType { get; }
		public Func  <IDbDataParameter, NpgsqlDbType> GetDbType { get; }

		public bool IsDbTypeSupported(NpgsqlDbType type) => _knownDbTypes.ContainsKey(type);

		public NpgsqlDbType ApplyDbTypeFlags(NpgsqlDbType type, bool isArray, bool isRange, bool convertAlways)
		{
			// don't apply conversions if flags not applied, otherwise it could return incorrect results
			if (!isArray && !isRange)
				return convertAlways ? (NpgsqlDbType)_knownDbTypes[type] : type;

			// a bit of magic to properly handle different numeric values for enums in npgsql 3 and 4
			var result = _knownDbTypes[type];
			if (isArray) result |= _knownDbTypes[NpgsqlDbType.Array];
			if (isRange) result |= _knownDbTypes[NpgsqlDbType.Range];

			// because resulting value will not map to any prefefined value, enum conversion will be performed
			// by value
			return (NpgsqlDbType)result;
		}

		private readonly Func<string, NpgsqlConnection> _connectionCreator;
		public NpgsqlConnection CreateConnection(string connectionString) => _connectionCreator(connectionString);

		public Func<IDbConnection, string, NpgsqlBinaryImporter> BeginBinaryImport { get; }

		public MappingSchema MappingSchema { get; }

		public static NpgsqlProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType     = assembly.GetType($"{ClientNamespace}.NpgsqlConnection"  , true)!;
						var parameterType      = assembly.GetType($"{ClientNamespace}.NpgsqlParameter"   , true)!;
						var dataReaderType     = assembly.GetType($"{ClientNamespace}.NpgsqlDataReader"  , true)!;
						var commandType        = assembly.GetType($"{ClientNamespace}.NpgsqlCommand"     , true)!;
						var transactionType    = assembly.GetType($"{ClientNamespace}.NpgsqlTransaction" , true)!;
						var dbType             = assembly.GetType($"{TypesNamespace}.NpgsqlDbType"       , true)!;
						var npgsqlDateType     = assembly.GetType($"{TypesNamespace}.NpgsqlDate"         , true)!;
						var npgsqlPointType    = assembly.GetType($"{TypesNamespace}.NpgsqlPoint"        , true)!;
						var npgsqlLSegType     = assembly.GetType($"{TypesNamespace}.NpgsqlLSeg"         , true)!;
						var npgsqlBoxType      = assembly.GetType($"{TypesNamespace}.NpgsqlBox"          , true)!;
						var npgsqlCircleType   = assembly.GetType($"{TypesNamespace}.NpgsqlCircle"       , true)!;
						var npgsqlPathType     = assembly.GetType($"{TypesNamespace}.NpgsqlPath"         , true)!;
						var npgsqlPolygonType  = assembly.GetType($"{TypesNamespace}.NpgsqlPolygon"      , true)!;
						var npgsqlLineType     = assembly.GetType($"{TypesNamespace}.NpgsqlLine"         , true)!;
						var npgsqlInetType     = assembly.GetType($"{TypesNamespace}.NpgsqlInet"         , true)!;
						var npgsqlTimeSpanType = assembly.GetType($"{TypesNamespace}.NpgsqlTimeSpan"     , true)!;
						var npgsqlDateTimeType = assembly.GetType($"{TypesNamespace}.NpgsqlDateTime"     , true)!;
						var npgsqlRangeTType   = assembly.GetType($"{TypesNamespace}.NpgsqlRange`1"      , true)!;

						var npgsqlBinaryImporterType = assembly.GetType($"{ClientNamespace}.NpgsqlBinaryImporter", true)!;

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<NpgsqlConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<NpgsqlParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<NpgsqlDbType>(dbType);
						typeMapper.RegisterTypeWrapper<NpgsqlBinaryImporter>(npgsqlBinaryImporterType);
						typeMapper.FinalizeMappings();

						var paramMapper   = typeMapper.Type<NpgsqlParameter>();
						var dbTypeBuilder = paramMapper.Member(p => p.NpgsqlDbType);

						var pConnection = Expression.Parameter(typeof(IDbConnection));
						var pCommand    = Expression.Parameter(typeof(string));

						var beginBinaryImport = Expression.Lambda<Func<IDbConnection, string, NpgsqlBinaryImporter>>(
								typeMapper.MapExpression((IDbConnection conn, string command) => typeMapper.Wrap<NpgsqlBinaryImporter>(((NpgsqlConnection)conn).BeginBinaryImport(command)), pConnection, pCommand),
								pConnection, pCommand)
							.Compile();

						// create mapping schema
						var mappingSchema = new MappingSchema();

						// date/time types
						AddUdtType(npgsqlDateType);
						AddUdtType(npgsqlDateTimeType);
						mappingSchema.SetDataType(npgsqlTimeSpanType, DataType.Interval);
						mappingSchema.SetDataType(npgsqlTimeSpanType.AsNullable(), DataType.Interval);
						// NpgsqlDateTimeType => DateTimeOffset
						{
							var p = Expression.Parameter(npgsqlDateTimeType, "p");
							var pi = p.Type.GetProperty("DateTime");

							Expression expr;

							if (pi != null)
								// < 3.2.0
								// https://github.com/npgsql/npgsql/commit/3894175f970b611f6428757a932b6393749da958#diff-c792076ac0455dd0f2852822ea38b0aaL166
								expr = Expression.Property(p, pi);
							else
								// 3.2.0+
								expr = Expression.Call(p, "ToDateTime", null);

							var npgsqlDateTimeToDateTimeOffsetMapper = Expression.Lambda(
								Expression.New(
									MemberHelper.ConstructorOf(() => new DateTimeOffset(new DateTime())),
									expr),
								p);
							mappingSchema.SetConvertExpression(npgsqlDateTimeType, typeof(DateTimeOffset), npgsqlDateTimeToDateTimeOffsetMapper);
						}

						// inet types
						AddUdtType(npgsqlInetType);
						AddUdtType(typeof(IPAddress));
						AddUdtType(typeof(PhysicalAddress));
						// npgsql4 obsoletes NpgsqlInetType and returns ValueTuple<IPAddress, int>
						// still while it is here, we should be able to map it properly
						// (IPAddress, int) => NpgsqlInet
						{
							var valueTypeType = Type.GetType("System.ValueTuple`2", false);
							if (valueTypeType != null)
							{
								var inetTupleType = valueTypeType.MakeGenericType(typeof(IPAddress), typeof(int));
								var p = Expression.Parameter(inetTupleType, "p");

								var tupleToInetTypeMapper = Expression.Lambda(
										Expression.New(
											npgsqlInetType.GetConstructor(new[] { typeof(IPAddress), typeof(int) })!,
											ExpressionHelper.Field(p, "Item1"),
											ExpressionHelper.Field(p, "Item2")),
										p);
								mappingSchema.SetConvertExpression(inetTupleType!, npgsqlInetType, tupleToInetTypeMapper);
							}
						}

						// ranges
						AddUdtType(npgsqlRangeTType);
						{
							void SetRangeConversion<T>(string? fromDbType = null, DataType fromDataType = DataType.Undefined, string? toDbType = null, DataType toDataType = DataType.Undefined)
							{
								var rangeType  = npgsqlRangeTType.MakeGenericType(typeof(T));
								var fromType   = new DbDataType(rangeType, fromDataType, fromDbType);
								var toType     = new DbDataType(typeof(DataParameter), toDataType, toDbType);
								var rangeParam = Expression.Parameter(rangeType, "p");

								mappingSchema.SetConvertExpression(fromType, toType,
									Expression.Lambda(
										Expression.New(
											MemberHelper.ConstructorOf(
												() => new DataParameter("", null, DataType.Undefined, toDbType)),
											Expression.Constant(""),
											Expression.Convert(rangeParam, typeof(object)),
											Expression.Constant(toDataType),
											Expression.Constant(toDbType, typeof(string))
										)
										, rangeParam)
								);
							}

							SetRangeConversion<byte>();
							SetRangeConversion<int>();
							SetRangeConversion<double>();
							SetRangeConversion<float>();
							SetRangeConversion<decimal>();

							SetRangeConversion<DateTime>(fromDbType: "daterange", toDbType: "daterange");

							SetRangeConversion<DateTime>(fromDbType: "tsrange", toDbType: "tsrange");
							SetRangeConversion<DateTime>(toDbType: "tsrange");

							SetRangeConversion<DateTime>(fromDbType: "tstzrange", toDbType: "tstzrange");

							SetRangeConversion<DateTimeOffset>("tstzrange");
						}

						// spatial types
						AddUdtType(npgsqlPointType);
						AddUdtType(npgsqlLSegType);
						AddUdtType(npgsqlBoxType);
						AddUdtType(npgsqlPathType);
						AddUdtType(npgsqlCircleType);
						AddUdtType(npgsqlPolygonType);
						AddUdtType(npgsqlLineType);

						_instance = new NpgsqlProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							dbType,

							mappingSchema,

							npgsqlDateType,
							npgsqlPointType,
							npgsqlLSegType,
							npgsqlBoxType,
							npgsqlCircleType,
							npgsqlPathType,
							npgsqlPolygonType,
							npgsqlLineType,
							npgsqlInetType,
							npgsqlTimeSpanType,
							npgsqlDateTimeType,
							npgsqlRangeTType,

							typeMapper.BuildWrappedFactory((string connectionString) => new NpgsqlConnection(connectionString)),

							dbTypeBuilder.BuildSetter<IDbDataParameter>(),
							dbTypeBuilder.BuildGetter<IDbDataParameter>(),

							beginBinaryImport);

						void AddUdtType(Type type)
						{
							if (!type.IsValueType)
								mappingSchema.AddScalarType(type, null, true, DataType.Udt);
							else
							{
								mappingSchema.AddScalarType(type, DataType.Udt);
								mappingSchema.AddScalarType(type.AsNullable(), null, true, DataType.Udt);
							}
						}
					}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		private class NpgsqlParameter
		{
			public NpgsqlDbType NpgsqlDbType { get; set; }
		}

		// Npgsql 4 changed numerical values for fields, so we should be careful when work with
		// flag-like fields Range and Array.
		[Wrapper]
		public enum NpgsqlDbType
		{
			Abstime                        = 33,
			Array                          = -2147483648,
			Bigint                         = 1,
			Bit                            = 25,
			Boolean                        = 2,
			Box                            = 3,
			Bytea                          = 4,
			Char                           = 6,
			Cid                            = 43,
			Cidr                           = 44,
			Circle                         = 5,
			/// <summary>
			/// Npgsql 3.0.?.
			/// </summary>
			Citext                         = 51,
			Date                           = 7,
			Double                         = 8,
			/// <summary>
			/// Npgsql 4.0.0+.
			/// </summary>
			Geography                      = 55,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Geometry                       = 50,
			Hstore                         = 37,
			Inet                           = 24,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Int2Vector                     = 52,
			Integer                        = 9,
			InternalChar                   = 38,
			Interval                       = 30,
			Json                           = 35,
			Jsonb                          = 36,
			Line                           = 10,
			LSeg                           = 11,
			MacAddr                        = 34,
			/// <summary>
			/// Npgsql 3.2.7+.
			/// </summary>
			MacAddr8                       = 54,
			Money                          = 12,
			Name                           = 32,
			Numeric                        = 13,
			Oid                            = 41,
			Oidvector                      = 29,
			Path                           = 14,
			Point                          = 15,
			Polygon                        = 16,
			Range                          = 1073741824,
			Real                           = 17,
			Refcursor                      = 23,
			/// <summary>
			/// Npgsql 4.0.3+.
			/// </summary>
			Regconfig                      = 56,
			/// <summary>
			/// Npgsql 3.0.2.
			/// </summary>
			Regtype                        = 49,
			Smallint                       = 18,
			Text                           = 19,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Tid                            = 53,
			Time                           = 20,
			Timestamp                      = 21,
			/// <summary>
			/// Added as alias to <see cref="TimestampTZ"/> in npgsql 4.0.0.
			/// Don't use it, as it will not work with 3.x.
			/// </summary>
			[Obsolete("Marked obsolete to avoid unintentional use")]
			TimestampTz                    = 26,
			// members with same name but different case
			[CLSCompliant(false)]
			TimestampTZ                    = 26,
			/// <summary>
			/// Added as alias to <see cref="TimeTZ"/> in npgsql 4.0.0.
			/// Don't use it, as it will not work with 3.x.
			/// </summary>
			[Obsolete("Marked obsolete to avoid unintentional use")]
			TimeTz                         = 31,
			// members with same name but different case
			[CLSCompliant(false)]
			TimeTZ                         = 31,
			TsQuery                        = 46,
			TsVector                       = 45,
			Unknown                        = 40,
			Uuid                           = 27,
			Varbit                         = 39,
			Varchar                        = 22,
			Xid                            = 42,
			Xml                            = 28,
			// v5+
			JsonPath                       = 57,
			LQuery                         = 61,
			LTree                          = 60,
			LTxtQuery                      = 62,
			PgLsn                          = 59

		}

		[Wrapper]
		public class NpgsqlConnection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get PostgreSqlVersion
				(Expression<Func<NpgsqlConnection, Version>>)((NpgsqlConnection this_) => this_.PostgreSqlVersion),
				// [1]: Open
				(Expression<Action<NpgsqlConnection>>       )((NpgsqlConnection this_) => this_.Open()),
				// [2]: Dispose
				(Expression<Action<NpgsqlConnection>>       )((NpgsqlConnection this_) => this_.Dispose()),
			};

			public NpgsqlConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public NpgsqlConnection(string connectionString) => throw new NotImplementedException();

			public Version PostgreSqlVersion => ((Func<NpgsqlConnection, Version>)CompiledWrappers[0])(this);
			public void    Open()            => ((Action<NpgsqlConnection>)CompiledWrappers[1])(this);
			public void    Dispose()         => ((Action<NpgsqlConnection>)CompiledWrappers[2])(this);

			// not implemented, as it is not called from wrapper
			internal NpgsqlBinaryImporter BeginBinaryImport(string copyFromCommand) => throw new NotImplementedException();
		}

		#region BulkCopy
		[Wrapper]
		public class NpgsqlBinaryImporter : TypeWrapper
		{
			private static object[] Wrappers {get;}
				= new object[]
			{
				// depending on npgsql version, [0] or [1] will fail to compile and CompiledWrappers will contain null
				// [0]: Cancel
				new Tuple<LambdaExpression, bool>((Expression<Action<NpgsqlBinaryImporter>>)((NpgsqlBinaryImporter this_) => this_.Cancel()), true),
				// [1]: Complete: pre-v5
				new Tuple<LambdaExpression, bool>((Expression<Action<NpgsqlBinaryImporter>>)((NpgsqlBinaryImporter this_) => this_.Complete()), true),
				// [2]: Complete: v5+
				new Tuple<LambdaExpression, bool>((Expression<Func<NpgsqlBinaryImporter, ulong>>)((NpgsqlBinaryImporter this_) => this_.Complete5()), true),
				// [3]: Dispose
				(Expression<Action<NpgsqlBinaryImporter>>                                  )((NpgsqlBinaryImporter this_) => this_.Dispose()),
				// [4]: StartRow
				(Expression<Action<NpgsqlBinaryImporter>>                                  )((NpgsqlBinaryImporter this_) => this_.StartRow()),
#if !NETFRAMEWORK
				// [5]: CompleteAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, CancellationToken, ValueTask<ulong>>>)((NpgsqlBinaryImporter this_, CancellationToken token) => this_.CompleteAsync(token)),         true),
				// [6]: DisposeAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, ValueTask                          >>)((NpgsqlBinaryImporter this_)                          => this_.DisposeAsync()),               true),
#else
				// [5]: CompleteAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, CancellationToken, Task<ulong>                >>)((NpgsqlBinaryImporter this_, CancellationToken token) => this_.CompleteAsync(token)),         true),
				// [6]: DisposeAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, Task                                          >>)((NpgsqlBinaryImporter this_)                          => this_.DisposeAsync()),               true),
#endif
				// [7]: StartRowAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, CancellationToken, Task                       >>)((NpgsqlBinaryImporter this_, CancellationToken token) => this_.StartRowAsync(token)), true),
				// [8]: WriteAsync
				new Tuple<LambdaExpression, bool>
				((Expression<Func<NpgsqlBinaryImporter, object?, NpgsqlDbType, CancellationToken, Task>>)((NpgsqlBinaryImporter this_, object? value, NpgsqlDbType type, CancellationToken token) => this_.WriteAsync(value, type, token)), true),
				// [9]: Write
				(Expression<Action<NpgsqlBinaryImporter, object?, NpgsqlDbType                        >>)((NpgsqlBinaryImporter this_, object? value, NpgsqlDbType type) => this_.Write(value, type)),
			};

			public NpgsqlBinaryImporter(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			/// <summary>
			/// Npgsql 3.x provides Cancel method.
			/// Npgsql 4.x uses Complete method.
			/// https://github.com/npgsql/npgsql/issues/1646.
			/// </summary>
			public void Cancel()   => ((Action<NpgsqlBinaryImporter>)CompiledWrappers[0])(this);
			public void Complete() => ((Action<NpgsqlBinaryImporter>)CompiledWrappers[1])(this);
			[CLSCompliant(false)]
			[TypeWrapperName("Complete")]
			public ulong Complete5() => ((Func<NpgsqlBinaryImporter, ulong>)CompiledWrappers[2])(this);
			public void Dispose()  => ((Action<NpgsqlBinaryImporter>)CompiledWrappers[3])(this);
			public void StartRow() => ((Action<NpgsqlBinaryImporter>)CompiledWrappers[4])(this);
			public void Write<T>(T value, NpgsqlDbType npgsqlDbType) => ((Action<NpgsqlBinaryImporter, object?, NpgsqlDbType>)CompiledWrappers[9])(this, value, npgsqlDbType);

#if !NETFRAMEWORK
#pragma warning disable CS3002 // Return type is not CLS-compliant
			public ValueTask<ulong> CompleteAsync(CancellationToken cancellationToken) 
				=> ((Func<NpgsqlBinaryImporter, CancellationToken, ValueTask<ulong>>)CompiledWrappers[5])(this, cancellationToken);
#pragma warning restore CS3002 // Return type is not CLS-compliant
			public ValueTask DisposeAsync()
				=> ((Func<NpgsqlBinaryImporter, ValueTask>)CompiledWrappers[6])(this);
			public Task StartRowAsync(CancellationToken cancellationToken) 
				=> ((Func<NpgsqlBinaryImporter, CancellationToken, Task>)CompiledWrappers[7])(this, cancellationToken);

#else
#pragma warning disable CS3002 // Return type is not CLS-compliant
			[return: CustomMapper(typeof(ValueTaskToTaskMapper))]
			public Task<ulong> CompleteAsync(CancellationToken cancellationToken)
				=> ((Func<NpgsqlBinaryImporter, CancellationToken, Task<ulong>>)CompiledWrappers[5])(this, cancellationToken);
#pragma warning restore CS3002 // Return type is not CLS-compliant
			[return: CustomMapper(typeof(ValueTaskToTaskMapper))]
			public Task DisposeAsync()
				=> ((Func<NpgsqlBinaryImporter, Task>)CompiledWrappers[6])(this);
			public Task StartRowAsync(CancellationToken cancellationToken)
				=> ((Func<NpgsqlBinaryImporter, CancellationToken, Task>)CompiledWrappers[7])(this, cancellationToken);
#endif

			public Task WriteAsync<T>(T value, NpgsqlDbType npgsqlDbType, CancellationToken cancellationToken)
				=> ((Func<NpgsqlBinaryImporter, object?, NpgsqlDbType, CancellationToken, Task>)CompiledWrappers[8])(this, value, npgsqlDbType, cancellationToken);

			public bool HasComplete => CompiledWrappers[1] != null;
			public bool HasComplete5 => CompiledWrappers[2] != null;

			public bool SupportsAsync => CompiledWrappers[5] != null && CompiledWrappers[6] != null && CompiledWrappers[7] != null && CompiledWrappers[8] != null;
		}

		#endregion
		#endregion
	}
}
