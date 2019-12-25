using System;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Net;
	using System.Net.NetworkInformation;
	using System.Text;
	using LinqToDB.Common;
	using LinqToDB.Data;
	using LinqToDB.Expressions;
	using LinqToDB.Extensions;
	using LinqToDB.Mapping;
	using LinqToDB.SqlProvider;
	using LinqToDB.SqlQuery;

	internal static class PostgreSQLWrappers
	{
		public static readonly string AssemblyName = "Npgsql";

		private static object _syncRoot = new object();

		private static IPostgreSQLWrapper? _wrapper;

		internal interface IPostgreSQLWrapper
		{
			Type ParameterType  { get; }
			Type DataReaderType { get; }
			Type ConnectionType { get; }

			Action<IDbDataParameter, NpgsqlDbType> TypeSetter { get; }
			Func<IDbDataParameter, NpgsqlDbType>   TypeGetter { get; }

			bool BinaryImporterHasComplete { get; }
			Func<IDbConnection, string, NpgsqlBinaryImporter> BeginBinaryImport { get; }
			Action<MappingSchema, NpgsqlBinaryImporter, ColumnDescriptor[], TEntity> GetBinaryImportRowWriter<TEntity>(
				PostgreSQLDataProvider provider,
				BasicSqlBuilder sqlBuilder,
				ColumnDescriptor[] columns,
				MappingSchema mappingSchema);

			Type NpgsqlDateType     { get; }
			Type NpgsqlPointType    { get; }
			Type NpgsqlLSegType     { get; }
			Type NpgsqlBoxType      { get; }
			Type NpgsqlCircleType   { get; }
			Type NpgsqlPathType     { get; }
			Type NpgsqlPolygonType  { get; }
			Type NpgsqlLineType     { get; }
			Type NpgsqlInetType     { get; }
			Type NpgsqlTimeSpanType { get; }
			Type NpgsqlDateTimeType { get; }
			Type NpgsqlRangeTType   { get; }

			bool IsDbTypeSupported(NpgsqlDbType type);

			NpgsqlDbType ApplyFlags(NpgsqlDbType type, bool isArray, bool isRange, bool convertAlways);

			void SetupMappingSchema(MappingSchema mappingSchema);

			NpgsqlConnection CreateNpgsqlConnection(string connectionString);
		}

		class NpgsqlWrapper : IPostgreSQLWrapper
		{
			private readonly Type _connectionType;
			private readonly Type _dataReaderType;
			private readonly Type _parameterType;
			private readonly Type _dbTypeType;

			private readonly Action<IDbDataParameter, NpgsqlDbType> _typeSetter;
			private readonly Func<IDbDataParameter, NpgsqlDbType> _typeGetter;

			private readonly bool _binaryImporterHasComplete;
			private readonly Func<IDbConnection, string, NpgsqlBinaryImporter> _beginBinaryImport;

			private readonly Type _npgsqlDateType;
			private readonly Type _npgsqlPointType;
			private readonly Type _npgsqlLSegType;
			private readonly Type _npgsqlBoxType;
			private readonly Type _npgsqlCircleType;
			private readonly Type _npgsqlPathType;
			private readonly Type _npgsqlPolygonType;
			private readonly Type _npgsqlLineType;
			private readonly Type _npgsqlInetType;
			private readonly Type _npgsqlTimeSpanType;
			private readonly Type _npgsqlDateTimeType;
			private readonly Type _npgsqlRangeTType;

			// maps mapped enum value to numeric value, defined in currently used provider
			private readonly IDictionary<NpgsqlDbType, int> _knownDbTypes = new Dictionary<NpgsqlDbType, int>();

			private readonly Type? _inetTupleType;
			private readonly LambdaExpression? _tupleToInetTypeMapper;
			private readonly LambdaExpression _npgsqlDateTimeToDateTimeOffsetMapper;

			private readonly Type _npgsqlBinaryImporterType;

			private readonly TypeMapper _typeMapper;

			NpgsqlWrapper(
				TypeMapper typeMapper,
				Type connectionType,
				Type parameterType,
				Type dataReaderType,
				Type dbTypeType,
				Action<IDbDataParameter, NpgsqlDbType> typeSetter,
				Func<IDbDataParameter, NpgsqlDbType> typeGetter,

				Type npgsqlBinaryImporterType,
				Func<IDbConnection, string, NpgsqlBinaryImporter> beginBinaryImport,
				bool binaryImporterHasComplete,

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
				Type npgsqlRangeTType)
			{
				_typeMapper = typeMapper;

				_connectionType = connectionType;
				_dataReaderType = dataReaderType;
				_parameterType = parameterType;
				_dbTypeType = dbTypeType;

				_typeSetter = typeSetter;
				_typeGetter = typeGetter;

				_npgsqlBinaryImporterType = npgsqlBinaryImporterType;
				_beginBinaryImport = beginBinaryImport;
				_binaryImporterHasComplete = binaryImporterHasComplete;

				_npgsqlDateType = npgsqlDateType;
				_npgsqlPointType = npgsqlPointType;
				_npgsqlLSegType = npgsqlLSegType;
				_npgsqlBoxType = npgsqlBoxType;
				_npgsqlCircleType = npgsqlCircleType;
				_npgsqlPathType = npgsqlPathType;
				_npgsqlPolygonType = npgsqlPolygonType;
				_npgsqlLineType = npgsqlLineType;
				_npgsqlInetType = npgsqlInetType;
				_npgsqlTimeSpanType = npgsqlTimeSpanType;
				_npgsqlDateTimeType = npgsqlDateTimeType;
				_npgsqlRangeTType = npgsqlRangeTType;

				// because NpgsqlDbType enumeration changes often (compared to other providers)
				// we should create lookup list of mapped fields, defined in used npgsql version
				var dbTypeKnownNames = Enum.GetNames(dbTypeType);
				var dbMappedDbTypeNames = Enum.GetNames(typeof(NpgsqlDbType));
				foreach (var knownTypeName in from nType in dbTypeKnownNames
											  join mType in dbMappedDbTypeNames on nType equals mType
											  select nType)
				{
					// use [] because enum contains duplicate fields with same values
					_knownDbTypes[(NpgsqlDbType)Enum.Parse(typeof(NpgsqlDbType), knownTypeName)] = (int)Enum.Parse(dbTypeType, knownTypeName);
				}


				// npgsql4 obsoletes NpgsqlInetType and returns ValueTuple<IPAddress, int>
				// still while it is here, we should be able to map it properly
				// (IPAddress, int) => NpgsqlInet
				{
					var valueTypeType = Type.GetType("System.ValueTuple`2", false);
					if (valueTypeType != null)
					{
						_inetTupleType = valueTypeType.MakeGenericType(typeof(IPAddress), typeof(int));
						var p = Expression.Parameter(_inetTupleType, "p");

						_tupleToInetTypeMapper = Expression.Lambda(
								Expression.New(
									_npgsqlInetType.GetConstructor(new[] { typeof(IPAddress), typeof(int) }),
									Expression.Field(p, "Item1"),
									Expression.Field(p, "Item2")),
								p);
					}
				}

				// NpgsqlDateTimeType => DateTimeOffset
				{
					var p = Expression.Parameter(_npgsqlDateTimeType, "p");
					var pi = p.Type.GetProperty("DateTime");

					Expression expr;

					if (pi != null)
						// < 3.2.0
						// https://github.com/npgsql/npgsql/commit/3894175f970b611f6428757a932b6393749da958#diff-c792076ac0455dd0f2852822ea38b0aaL166
						expr = Expression.Property(p, pi);
					else
						// 3.2.0+
						expr = Expression.Call(p, "ToDateTime", null);

					_npgsqlDateTimeToDateTimeOffsetMapper = Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(new DateTime())),
							expr),
						p);
				}
			}


			Type IPostgreSQLWrapper.ConnectionType => _connectionType;
			Type IPostgreSQLWrapper.DataReaderType => _dataReaderType;
			Type IPostgreSQLWrapper.ParameterType => _parameterType;

			Action<IDbDataParameter, NpgsqlDbType> IPostgreSQLWrapper.TypeSetter => _typeSetter;
			Func<IDbDataParameter, NpgsqlDbType> IPostgreSQLWrapper.TypeGetter => _typeGetter;

			Func<IDbConnection, string, NpgsqlBinaryImporter> IPostgreSQLWrapper.BeginBinaryImport => _beginBinaryImport;
			bool IPostgreSQLWrapper.BinaryImporterHasComplete => _binaryImporterHasComplete;

			NpgsqlConnection IPostgreSQLWrapper.CreateNpgsqlConnection(string connectionString)
				=> _typeMapper!.CreateAndWrap(() => new NpgsqlConnection(connectionString))!;

			Action<MappingSchema, NpgsqlBinaryImporter, ColumnDescriptor[], TEntity> IPostgreSQLWrapper.GetBinaryImportRowWriter<TEntity>(
				PostgreSQLDataProvider provider,
				BasicSqlBuilder sqlBuilder,
				ColumnDescriptor[] columns,
				MappingSchema mappingSchema)
			{
				var generator = new ExpressionGenerator(_typeMapper);

				var pMapping  = Expression.Parameter(typeof(MappingSchema));
				var pWriterIn = Expression.Parameter(typeof(NpgsqlBinaryImporter));
				var pColumns  = Expression.Parameter(typeof(ColumnDescriptor[]));
				var pEntity   = Expression.Parameter(typeof(TEntity));

				var pWriter  = generator.AddVariable(Expression.Parameter(_npgsqlBinaryImporterType));
				generator.Assign(pWriter, Expression.Convert(Expression.PropertyOrField(pWriterIn, "instance_"), _npgsqlBinaryImporterType));

				generator.AddExpression(generator.MapAction((NpgsqlBinaryImporter importer) => importer.StartRow(), pWriter));

				for (var i = 0; i < columns.Length; i++)
				{
					var npgsqlType = provider.GetNativeType(columns[i].DbType, true);
					if (npgsqlType == null)
					{
						var columnType = columns[i].DataType != DataType.Undefined ? new SqlDataType(columns[i]) : null;

						if (columnType == null || columnType.DataType == DataType.Undefined)
							columnType = mappingSchema.GetDataType(columns[i].StorageType);

						var sb = new StringBuilder();
						sqlBuilder.BuildTypeName(sb, columnType);
						npgsqlType = provider.GetNativeType(sb.ToString(), true);
					}

					if (npgsqlType == null)
						throw new LinqToDBException($"Cannot guess PostgreSQL type for column {columns[i].ColumnName}. Specify type explicitly in column mapping.");

					// don't use WriteNull because Write already handle both null and DBNull values properly
					// also use object as type parameter, as it is not important for npgsql now
					generator.AddExpression(
						Expression.Call(
							pWriter,
							"Write",
							new[] { typeof(object) },
							//columns[idx].GetValue(mappingSchema, entity)
							Expression.Call(Expression.ArrayIndex(pColumns, Expression.Constant(i)), "GetValue", Array<Type>.Empty, pMapping, pEntity),
							Expression.Convert(Expression.Constant(npgsqlType.Value), _dbTypeType)));
				}

				var ex = Expression.Lambda<Action<MappingSchema, NpgsqlBinaryImporter, ColumnDescriptor[], TEntity>>(
						generator.Build(),
						pMapping, pWriterIn, pColumns, pEntity);

				return ex.Compile();
			}

			Type IPostgreSQLWrapper.NpgsqlDateType => _npgsqlDateType;
			Type IPostgreSQLWrapper.NpgsqlPointType => _npgsqlPointType;
			Type IPostgreSQLWrapper.NpgsqlLSegType => _npgsqlLSegType;
			Type IPostgreSQLWrapper.NpgsqlBoxType => _npgsqlBoxType;
			Type IPostgreSQLWrapper.NpgsqlCircleType => _npgsqlCircleType;
			Type IPostgreSQLWrapper.NpgsqlPathType => _npgsqlPathType;
			Type IPostgreSQLWrapper.NpgsqlPolygonType => _npgsqlPolygonType;
			Type IPostgreSQLWrapper.NpgsqlLineType => _npgsqlLineType;
			Type IPostgreSQLWrapper.NpgsqlInetType => _npgsqlInetType;
			Type IPostgreSQLWrapper.NpgsqlTimeSpanType => _npgsqlTimeSpanType;
			Type IPostgreSQLWrapper.NpgsqlDateTimeType => _npgsqlDateTimeType;
			Type IPostgreSQLWrapper.NpgsqlRangeTType => _npgsqlRangeTType;

			bool IPostgreSQLWrapper.IsDbTypeSupported(NpgsqlDbType type) => _knownDbTypes.ContainsKey(type);

			NpgsqlDbType IPostgreSQLWrapper.ApplyFlags(NpgsqlDbType type, bool isArray, bool isRange, bool convertAlways)
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

			// TODO: refactor wrappers to have own small mapping schema and add it to provider's mapping schema on
			// provider setup
			void IPostgreSQLWrapper.SetupMappingSchema(MappingSchema mappingSchema)
			{
				// date/time types
				AddUdtType(_npgsqlDateType);
				AddUdtType(_npgsqlDateTimeType);
				mappingSchema.SetDataType(_npgsqlTimeSpanType, DataType.Interval);
				mappingSchema.SetDataType(_npgsqlTimeSpanType.AsNullable(), DataType.Interval);
				mappingSchema.SetConvertExpression(_npgsqlDateTimeType, typeof(DateTimeOffset), _npgsqlDateTimeToDateTimeOffsetMapper);


				// inet types
				AddUdtType(_npgsqlInetType);
				AddUdtType(typeof(IPAddress));
				AddUdtType(typeof(PhysicalAddress));
				if (_tupleToInetTypeMapper != null)
					mappingSchema.SetConvertExpression(_inetTupleType!, _npgsqlInetType, _tupleToInetTypeMapper);

				// spatial types
				AddUdtType(_npgsqlPointType);
				AddUdtType(_npgsqlLSegType);
				AddUdtType(_npgsqlBoxType);
				AddUdtType(_npgsqlPathType);
				AddUdtType(_npgsqlCircleType);
				AddUdtType(_npgsqlPolygonType);
				AddUdtType(_npgsqlLineType);

				// ranges
				AddUdtType(_npgsqlRangeTType);
				{
					void SetRangeConversion<T>(string? fromDbType = null, DataType fromDataType = DataType.Undefined, string? toDbType = null, DataType toDataType = DataType.Undefined)
					{
						var rangeType = _npgsqlRangeTType.MakeGenericType(typeof(T));
						var fromType = new DbDataType(rangeType, fromDataType, fromDbType);
						var toType = new DbDataType(typeof(DataParameter), toDataType, toDbType);
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

			internal static IPostgreSQLWrapper Initialize()
			{
				var assembly = Type.GetType("Npgsql.NpgsqlConnection, Npgsql", true).Assembly;

				var connectionType = assembly.GetType("Npgsql.NpgsqlConnection", true);
				var parameterType  = assembly.GetType("Npgsql.NpgsqlParameter", true);
				var dataReaderType = assembly.GetType("Npgsql.NpgsqlDataReader", true);
				var dbType         = assembly.GetType("NpgsqlTypes.NpgsqlDbType", true);

				var npgsqlDateType     = assembly.GetType("NpgsqlTypes.NpgsqlDate", true);
				var npgsqlPointType    = assembly.GetType("NpgsqlTypes.NpgsqlPoint", true);
				var npgsqlLSegType     = assembly.GetType("NpgsqlTypes.NpgsqlLSeg", true);
				var npgsqlBoxType      = assembly.GetType("NpgsqlTypes.NpgsqlBox", true);
				var npgsqlCircleType   = assembly.GetType("NpgsqlTypes.NpgsqlCircle", true);
				var npgsqlPathType     = assembly.GetType("NpgsqlTypes.NpgsqlPath", true);
				var npgsqlPolygonType  = assembly.GetType("NpgsqlTypes.NpgsqlPolygon", true);
				var npgsqlLineType     = assembly.GetType("NpgsqlTypes.NpgsqlLine", true);
				var npgsqlInetType     = assembly.GetType("NpgsqlTypes.NpgsqlInet", true);
				var npgsqlTimeSpanType = assembly.GetType("NpgsqlTypes.NpgsqlTimeSpan", true);
				var npgsqlDateTimeType = assembly.GetType("NpgsqlTypes.NpgsqlDateTime", true);
				var npgsqlRangeTType   = assembly.GetType("NpgsqlTypes.NpgsqlRange`1", true);

				var npgsqlBinaryImporterType = assembly.GetType("Npgsql.NpgsqlBinaryImporter", true);

				var typeMapper = new TypeMapper(
					connectionType, parameterType, dbType, dataReaderType,
					npgsqlBinaryImporterType);

				typeMapper.RegisterWrapper<NpgsqlConnection>();
				typeMapper.RegisterWrapper<NpgsqlParameter>();
				typeMapper.RegisterWrapper<NpgsqlDbType>();

				typeMapper.RegisterWrapper<NpgsqlBinaryImporter>();

				var paramMapper   = typeMapper.Type<NpgsqlParameter>();
				var dbTypeBuilder = paramMapper.Member(p => p.NpgsqlDbType);

				var pConnection = Expression.Parameter(typeof(IDbConnection));
				var pCommand    = Expression.Parameter(typeof(string));

				var beginBinaryImport = Expression.Lambda<Func<IDbConnection, string, NpgsqlBinaryImporter>>(
						typeMapper.MapExpression((IDbConnection conn, string command) => typeMapper.Wrap<NpgsqlBinaryImporter>(((NpgsqlConnection)conn).BeginBinaryImport(command)), pConnection, pCommand),
						pConnection, pCommand)
					.Compile();

				return new NpgsqlWrapper(
					typeMapper,
					connectionType,
					parameterType,
					dataReaderType,
					dbType,
					dbTypeBuilder.BuildSetter<IDbDataParameter>(),
					dbTypeBuilder.BuildGetter<IDbDataParameter>(),

					npgsqlBinaryImporterType,
					beginBinaryImport,
					npgsqlBinaryImporterType.GetMethod("Complete") != null,

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
					npgsqlRangeTType);
			}
		}

		internal static IPostgreSQLWrapper Initialize()
		{
			if (_wrapper == null)
				lock (_syncRoot)
					if (_wrapper == null)
						_wrapper = NpgsqlWrapper.Initialize();

			return _wrapper;
		}

		[Wrapper]
		internal class NpgsqlParameter
		{
			public NpgsqlDbType NpgsqlDbType { get; set; }
		}

		// Npgsql 4 changed numerical values for fields, so we should be careful when work with
		// flag-like fields Range and Array.
		[Wrapper]
		internal enum NpgsqlDbType
		{
			Abstime = 33,
			Array = -2147483648,
			Bigint = 1,
			Bit = 25,
			Boolean = 2,
			Box = 3,
			Bytea = 4,
			Char = 6,
			Cid = 43,
			Cidr = 44,
			Circle = 5,
			/// <summary>
			/// Npgsql 3.0.?.
			/// </summary>
			Citext = 51,
			Date = 7,
			Double = 8,
			/// <summary>
			/// Npgsql 4.0.0+.
			/// </summary>
			Geography = 55,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Geometry = 50,
			Hstore = 37,
			Inet = 24,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Int2Vector = 52,
			Integer = 9,
			InternalChar = 38,
			Interval = 30,
			Json = 35,
			Jsonb = 36,
			Line = 10,
			LSeg = 11,
			MacAddr = 34,
			/// <summary>
			/// Npgsql 3.2.7+.
			/// </summary>
			MacAddr8 = 54,
			Money = 12,
			Name = 32,
			Numeric = 13,
			Oid = 41,
			Oidvector = 29,
			Path = 14,
			Point = 15,
			Polygon = 16,
			Range = 1073741824,
			Real = 17,
			Refcursor = 23,
			/// <summary>
			/// Npgsql 4.0.3+.
			/// </summary>
			Regconfig = 56,
			/// <summary>
			/// Npgsql 3.0.2.
			/// </summary>
			Regtype = 49,
			Smallint = 18,
			Text = 19,
			/// <summary>
			/// Npgsql 3.1.0+.
			/// </summary>
			Tid = 53,
			Time = 20,
			Timestamp = 21,
			/// <summary>
			/// Added as alias to <see cref="TimestampTZ"/> in npgsql 4.0.0.
			/// Don't use it, as it will not work with 3.x.
			/// </summary>
			[Obsolete("Marked obsolete to avoid unintentional use")]
			TimestampTz = 26,
			TimestampTZ = 26,
			/// <summary>
			/// Added as alias to <see cref="TimeTZ"/> in npgsql 4.0.0.
			/// Don't use it, as it will not work with 3.x.
			/// </summary>
			[Obsolete("Marked obsolete to avoid unintentional use")]
			TimeTz = 31,
			TimeTZ = 31,
			TsQuery = 46,
			TsVector = 45,
			Unknown = 40,
			Uuid = 27,
			Varbit = 39,
			Varchar = 22,
			Xid = 42,
			Xml = 28
		}

		[Wrapper]
		internal class NpgsqlConnection : TypeWrapper, IDisposable
		{
			public NpgsqlConnection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public NpgsqlConnection(string connectionString) => throw new NotImplementedException();

			public Version PostgreSqlVersion => this.Wrap(t => t.PostgreSqlVersion);

			public void Open() => this.WrapAction(c => c.Open());

			public void Dispose() => this.WrapAction(t => t.Dispose());

			// not implemented, as it is not called from wrapper
			public NpgsqlBinaryImporter BeginBinaryImport(string copyFromCommand) => throw new NotImplementedException();
		}

		#region BulkCopy
		[Wrapper]
		internal class NpgsqlBinaryImporter : TypeWrapper
		{
			public NpgsqlBinaryImporter(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			/// <summary>
			/// Npgsql 3.x.
			/// </summary>
			public void Cancel() => this.WrapAction(t => t.Cancel());
			/// <summary>
			/// Npgsql 4.x.
			/// https://github.com/npgsql/npgsql/issues/1646.
			/// </summary>
			public void Complete() => this.WrapAction(t => t.Complete());

			public void Dispose() => this.WrapAction(t => t.Dispose());

			public void StartRow() => this.WrapAction(t => t.StartRow());

			public void Write<T>(T value, NpgsqlDbType npgsqlDbType) => this.WrapAction(t => t.Write(value, npgsqlDbType));
		}

		#endregion
	}
}
