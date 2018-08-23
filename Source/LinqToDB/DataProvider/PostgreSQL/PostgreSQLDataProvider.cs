using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;
	using Expressions;
	using Mapping;
	using SqlProvider;
	using Extensions;

	public class PostgreSQLDataProvider : DynamicDataProviderBase
	{
		public PostgreSQLDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
			: this(
				GetProviderName(version),
				new PostgreSQLMappingSchema(),
				version)
		{
		}

		public PostgreSQLDataProvider(string providerName, PostgreSQLVersion version)
			: this(providerName, new PostgreSQLMappingSchema(), version)
		{
		}

		protected PostgreSQLDataProvider(string name, MappingSchema mappingSchema, PostgreSQLVersion version = PostgreSQLVersion.v92)
			: base(name, mappingSchema)
		{
			Version = version;

			SqlProviderFlags.IsApplyJoinSupported              = version != PostgreSQLVersion.v92;
			SqlProviderFlags.IsInsertOrUpdateSupported         = version == PostgreSQLVersion.v95;
			SqlProviderFlags.IsUpdateSetTableAliasSupported    = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;

			SetCharFieldToType<char>("bpchar", (r, i) => DataTools.GetChar(r, i));

			SetCharField("bpchar", (r,i) => r.GetString(i).TrimEnd(' '));

			_sqlOptimizer = new PostgreSQLSqlOptimizer(SqlProviderFlags);
		}

		public PostgreSQLVersion Version { get; private set; }

		internal Type BitStringType;
		internal Type NpgsqlIntervalType;
		internal Type NpgsqlInetType;
		internal Type NpgsqlTimeType;
		internal Type NpgsqlTimeTZType;
		internal Type NpgsqlPointType;
		internal Type NpgsqlLineType;
		internal Type NpgsqlLSegType;
		internal Type NpgsqlBoxType;
		internal Type NpgsqlPathType;
		internal Type NpgsqlPolygonType;
		internal Type NpgsqlCircleType;
		internal Type NpgsqlMacAddressType;
		internal Type NpgsqlDateType;
		internal Type NpgsqlDateTimeType;

		internal bool HasMacAddr8 { get; private set; }

		/// <summary>
		/// PostgreSQL parameter type enum type.
		/// </summary>
		internal Type NpgsqlDbType;

		/// <summary>
		/// Map of canonical PostgreSQL type name to NpgsqlDbType enumeration value.
		/// This map shouldn't be used directly, you should resolve PostgreSQL types using
		/// <see cref="GetNativeType(string)"/> method, which takes into account different type aliases.
		/// </summary>
		private IDictionary<string, object> _npgsqlTypeMap = new Dictionary<string, object>();
		private int _npgsqlTypeArrayFlag;
		private int _npgsqlTypeRangeFlag;

		Type _npgsqlTimeStamp;
		Type _npgsqlTimeStampTZ;

		CommandBehavior _commandBehavior = CommandBehavior.Default;

		private static string GetProviderName(PostgreSQLVersion version)
		{
			switch (version)
			{
				case PostgreSQLVersion.v92:
					return ProviderName.PostgreSQL92;
				case PostgreSQLVersion.v93:
					return ProviderName.PostgreSQL93;
				default:
					return ProviderName.PostgreSQL95;
			}
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			var npgSql = connectionType.AssemblyEx();

			// NpgsqlInterval was renamed to NpgsqlTimeSpan
			NpgsqlIntervalType   = npgSql.GetType("NpgsqlTypes.NpgsqlInterval"   , false);
			NpgsqlIntervalType   = NpgsqlIntervalType ?? npgSql.GetType("NpgsqlTypes.NpgsqlTimeSpan"   , false);

			BitStringType        = npgSql.GetType("NpgsqlTypes.BitString"        , false);
			NpgsqlInetType       = npgSql.GetType("NpgsqlTypes.NpgsqlInet"       , true);
			NpgsqlTimeType       = npgSql.GetType("NpgsqlTypes.NpgsqlTime"       , false);
			NpgsqlTimeTZType     = npgSql.GetType("NpgsqlTypes.NpgsqlTimeTZ"     , false);
			NpgsqlPointType      = npgSql.GetType("NpgsqlTypes.NpgsqlPoint"      , true);
			NpgsqlLineType       = npgSql.GetType("NpgsqlTypes.NpgsqlLine"       , false);
			NpgsqlLSegType       = npgSql.GetType("NpgsqlTypes.NpgsqlLSeg"       , true);
			NpgsqlBoxType        = npgSql.GetType("NpgsqlTypes.NpgsqlBox"        , true);
			NpgsqlPathType       = npgSql.GetType("NpgsqlTypes.NpgsqlPath"       , true);
			_npgsqlTimeStamp     = npgSql.GetType("NpgsqlTypes.NpgsqlTimeStamp"  , false);
			_npgsqlTimeStampTZ   = npgSql.GetType("NpgsqlTypes.NpgsqlTimeStampTZ", false);
			NpgsqlDateType       = npgSql.GetType("NpgsqlTypes.NpgsqlDate"       , true);
			NpgsqlDateTimeType   = npgSql.GetType("NpgsqlTypes.NpgsqlDateTime"   , false);
			NpgsqlMacAddressType = npgSql.GetType("NpgsqlTypes.NpgsqlMacAddress" , false);
			NpgsqlCircleType     = npgSql.GetType("NpgsqlTypes.NpgsqlCircle"     , true);
			NpgsqlPolygonType    = npgSql.GetType("NpgsqlTypes.NpgsqlPolygon"    , true);
			NpgsqlDbType         = npgSql.GetType("NpgsqlTypes.NpgsqlDbType"     , true);

			// https://www.postgresql.org/docs/current/static/datatype.html
			// not all types are supported now
			// numeric types
			TryAddType("smallint"                     , "Smallint");
			TryAddType("integer"                      , "Integer");
			TryAddType("bigint"                       , "Bigint");
			TryAddType("numeric"                      , "Numeric");
			TryAddType("real"                         , "Real");
			TryAddType("double precision"             , "Double");
			// monetary types
			TryAddType("money"                        , "Money");
			// character types
			TryAddType("character"                    , "Char");
			TryAddType("character varying"            , "Varchar");
			TryAddType("text"                         , "Text");
			TryAddType("name"                         , "Name");
			TryAddType("char"                         , "InternalChar");
			// binary types
			TryAddType("bytea"                        , "Bytea");
			// date/time types (reltime missing from enum)
			TryAddType("timestamp"                    , "Timestamp");
			if (!TryAddType("timestamp with time zone", "TimestampTz"))
				TryAddType("timestamp with time zone" , "TimestampTZ");
			TryAddType("date"                         , "Date");
			TryAddType("time"                         , "Time");
			if (!TryAddType("time with time zone"     , "TimeTz"))
				TryAddType("time with time zone"      , "TimeTZ");
			TryAddType("interval"                     , "Interval");
			TryAddType("abstime"                      , "Abstime");
			// boolean type
			TryAddType("boolean"                      , "Boolean");
			// geometric types
			TryAddType("point"                        , "Point");
			TryAddType("line"                         , "Line");
			TryAddType("lseg"                         , "LSeg");
			TryAddType("box"                          , "Box");
			TryAddType("path"                         , "Path");
			TryAddType("polygon"                      , "Polygon");
			TryAddType("circle"                       , "Circle");
			// network address types
			TryAddType("cidr"                         , "Cidr");
			TryAddType("inet"                         , "Inet");
			TryAddType("macaddr"                      , "MacAddr");
			HasMacAddr8 = TryAddType("macaddr8"       , "MacAddr8");
			// bit string types
			TryAddType("bit"                          , "Bit");
			TryAddType("bit varying"                  , "Varbit");
			// text search types
			TryAddType("tsvector"                     , "TsVector");
			TryAddType("tsquery"                      , "TsQuery");
			// UUID type
			TryAddType("uuid"                         , "Uuid");
			// XML type
			TryAddType("xml"                          , "Xml");
			// JSON types
			TryAddType("json"                         , "Json");
			TryAddType("jsonb"                        , "Jsonb");
			// Object Identifier Types (only supported by npgsql)
			TryAddType("oid"                          , "Oid");
			TryAddType("regtype"                      , "Regtype");
			TryAddType("xid"                          , "Xid");
			TryAddType("cid"                          , "Cid");
			TryAddType("tid"                          , "Tid");
			// other types
			TryAddType("citext"                       , "Citext");
			TryAddType("hstore"                       , "Hstore");
			TryAddType("refcursor"                    , "Refcursor");
			TryAddType("oidvector"                    , "Oidvector");
			TryAddType("int2vector"                   , "Int2Vector");

			_npgsqlTypeArrayFlag = (int)Enum.Parse(NpgsqlDbType, "Array");
			_npgsqlTypeRangeFlag = (int)Enum.Parse(NpgsqlDbType, "Range");

			// https://github.com/linq2db/linq2db/pull/718
			//if (npgSql.GetName().Version >= new Version(3, 1, 9))
			//{
			//	_commandBehavior = CommandBehavior.KeyInfo;
			//}

			if (BitStringType        != null) SetProviderField(BitStringType       , BitStringType,        "GetBitString");
			if (NpgsqlIntervalType   != null) SetProviderField(NpgsqlIntervalType  , NpgsqlIntervalType,   "GetInterval");
			if (NpgsqlTimeType       != null) SetProviderField(NpgsqlTimeType      , NpgsqlTimeType,       "GetTime");
			if (NpgsqlTimeTZType     != null) SetProviderField(NpgsqlTimeTZType    , NpgsqlTimeTZType,     "GetTimeTZ");
			if (_npgsqlTimeStamp     != null) SetProviderField(_npgsqlTimeStamp    , _npgsqlTimeStamp,     "GetTimeStamp");
			if (_npgsqlTimeStampTZ   != null) SetProviderField(_npgsqlTimeStampTZ  , _npgsqlTimeStampTZ,   "GetTimeStampTZ");
			if (NpgsqlMacAddressType != null) SetProviderField(NpgsqlMacAddressType, NpgsqlMacAddressType, "GetProviderSpecificValue");
			if (NpgsqlDateTimeType   != null) SetProviderField(NpgsqlDateTimeType  , NpgsqlDateTimeType,   "GetTimeStamp");

			SetProviderField(NpgsqlInetType, NpgsqlInetType, "GetProviderSpecificValue");
			SetProviderField(NpgsqlDateType, NpgsqlDateType, "GetDate");

			if (_npgsqlTimeStampTZ != null)
			{
				// SetProviderField2<NpgsqlDataReader,DateTimeOffset,NpgsqlTimeStampTZ>((r,i) => (NpgsqlTimeStampTZ)r.GetProviderSpecificValue(i));

				var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
				var indexParameter = Expression.Parameter(typeof(int), "i");

				ReaderExpressions[new ReaderInfo { ToType = typeof(DateTimeOffset), ProviderFieldType = _npgsqlTimeStampTZ }] =
					Expression.Lambda(
						Expression.Convert(
							Expression.Call(dataReaderParameter, "GetProviderSpecificValue", null, indexParameter),
							_npgsqlTimeStampTZ),
						dataReaderParameter,
						indexParameter);
			}

			_setMoney     = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Money");
			_setVarBinary = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Bytea");
			_setBoolean   = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Boolean");
			_setXml       = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Xml");
			_setText      = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Text");
			_setBit       = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Bit");
			_setHstore    = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Hstore");
			_setJson      = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Json");
			_setJsonb     = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Jsonb");

			if (BitStringType        != null) MappingSchema.AddScalarType(BitStringType);
			if (NpgsqlTimeType       != null) MappingSchema.AddScalarType(NpgsqlTimeType);
			if (NpgsqlTimeTZType     != null) MappingSchema.AddScalarType(NpgsqlTimeTZType);
			if (_npgsqlTimeStamp     != null) MappingSchema.AddScalarType(_npgsqlTimeStamp);
			if (_npgsqlTimeStampTZ   != null) MappingSchema.AddScalarType(_npgsqlTimeStampTZ);
			if (NpgsqlMacAddressType != null) MappingSchema.AddScalarType(NpgsqlMacAddressType);

			AddUdtType(NpgsqlIntervalType);
			AddUdtType(NpgsqlDateType);
			AddUdtType(NpgsqlDateTimeType);

			AddUdtType(NpgsqlInetType);
			AddUdtType(typeof(IPAddress));
			AddUdtType(typeof(PhysicalAddress));

			AddUdtType(NpgsqlPointType);
			AddUdtType(NpgsqlLSegType);
			AddUdtType(NpgsqlBoxType);
			AddUdtType(NpgsqlPathType);
			AddUdtType(NpgsqlCircleType);
			AddUdtType(NpgsqlPolygonType);
			AddUdtType(NpgsqlLineType);

			if (_npgsqlTimeStampTZ != null)
			{
				// SetConvertExpression<NpgsqlTimeStampTZ,DateTimeOffset>(
				//     d => new DateTimeOffset(d.Year, d.Month, d.Day, d.Hours, d.Minutes, d.Seconds, d.Milliseconds,
				//         new TimeSpan(d.TimeZone.Hours, d.TimeZone.Minutes, d.TimeZone.Seconds)));

				var p = Expression.Parameter(_npgsqlTimeStampTZ, "p");

				MappingSchema.SetConvertExpression(_npgsqlTimeStampTZ, typeof(DateTimeOffset),
					Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(0L, new TimeSpan())),
							Expression.PropertyOrField(p, "Ticks"),
							Expression.New(
								MemberHelper.ConstructorOf(() => new TimeSpan(0, 0, 0)),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Hours"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Minutes"),
								Expression.PropertyOrField(Expression.PropertyOrField(p, "TimeZone"), "Seconds"))),
						p
					));
			}

			if (NpgsqlDateTimeType != null)
			{
				var p = Expression.Parameter(NpgsqlDateTimeType, "p");
				var pi = p.Type.GetPropertyEx("DateTime");

				Expression expr;

				if (pi != null)
					expr = Expression.Property(p, pi);
				else
					expr = Expression.Call(p, "ToDateTime", null);

				MappingSchema.SetConvertExpression(NpgsqlDateTimeType, typeof(DateTimeOffset),
					Expression.Lambda(
						Expression.New(
							MemberHelper.ConstructorOf(() => new DateTimeOffset(new DateTime())),
							expr), p));
			}
		}

		private void AddUdtType(Type type)
		{
			if (type == null)
				return;

			if (!type.IsValueTypeEx())
				MappingSchema.AddScalarType(type, null, true, DataType.Udt);
			else
			{
				MappingSchema.AddScalarType(type, DataType.Udt);
				MappingSchema.AddScalarType(type.AsNullable(), null, true, DataType.Udt);
			}
		}

		private bool TryAddType(string dbType, string enumName)
		{
			try
			{
				_npgsqlTypeMap.Add(dbType, Enum.Parse(NpgsqlDbType, enumName));
				return true;
			}
			catch
			{
				// different versions of npgsql have different members
				return false;
			}
		}

		public    override string ConnectionNamespace => "Npgsql";
		protected override string ConnectionTypeName  => "Npgsql.NpgsqlConnection, Npgsql";
		protected override string DataReaderTypeName  => "Npgsql.NpgsqlDataReader, Npgsql";

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new PostgreSQLSqlBuilder(this, GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

#if !NETSTANDARD1_6
		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new PostgreSQLSchemaProvider(this);
		}
#endif

#if NETSTANDARD2_0
		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}
#endif

		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setBoolean;
		static Action<IDbDataParameter> _setXml;
		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setBit;
		static Action<IDbDataParameter> _setHstore;
		static Action<IDbDataParameter> _setJsonb;
		static Action<IDbDataParameter> _setJson;

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is IDictionary && dataType == DataType.Undefined)
			{
				dataType = DataType.Dictionary;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : parameter.DbType = DbType.Int16;            break;
				case DataType.UInt16     : parameter.DbType = DbType.Int32;            break;
				case DataType.UInt32     : parameter.DbType = DbType.Int64;            break;
				case DataType.UInt64     : parameter.DbType = DbType.Decimal;          break;
				case DataType.DateTime2  : parameter.DbType = DbType.DateTime;         break;
				case DataType.VarNumeric : parameter.DbType = DbType.Decimal;          break;
				case DataType.Decimal    : parameter.DbType = DbType.Decimal;          break;
				case DataType.Money      : _setMoney(parameter);                       break;
				case DataType.Image      :
				case DataType.Binary     :
				case DataType.VarBinary  : _setVarBinary(parameter);                   break;
				case DataType.Boolean    : _setBoolean  (parameter);                   break;
				case DataType.Xml        : _setXml      (parameter);                   break;
				case DataType.Text       :
				case DataType.NText      : _setText     (parameter);                   break;
				case DataType.BitArray   : _setBit      (parameter);                   break;
				case DataType.Dictionary : _setHstore(parameter);                      break;
				case DataType.Json       : _setJson(parameter);                        break;
				case DataType.BinaryJson : _setJsonb(parameter);                       break;
				default                  : base.SetParameterType(parameter, dataType); break;
			}
		}

		public override CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return commandBehavior | _commandBehavior;
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			[JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new PostgreSQLBulkCopy(this, GetConnectionType()).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? PostgreSQLTools.DefaultBulkCopyType : options.BulkCopyType,
				dataConnection,
				options,
				source);
		}

		#endregion

		/// <summary>
		/// Returns NpgsqlDbType enumeration value for requested postgresql type or null if type cannot be resolved.
		/// This method expects correct PostgreSQL type as input.
		/// Custom types not supported. Also could fail on some types as PostgreSQL have a lot of ways to write same
		/// type.
		/// </summary>
		internal object GetNativeType(string dbType)
		{
			if (string.IsNullOrWhiteSpace(dbType))
				return null;

			dbType = dbType.ToLower();

			// detect arrays
			var isArray = false;
			var idx = dbType.IndexOf("array");

			if (idx == -1)
				idx = dbType.IndexOf("[");

			if (idx != -1)
			{
				isArray = true;
				dbType = dbType.Substring(0, idx);
			}

			var isRange = false;

			dbType = dbType.Trim();

			// normalize synonyms and parametrized type names
			switch (dbType)
			{
				case "int4range":
					dbType  = "integer";
					isRange = true;
					break;
				case "int8range":
					dbType  = "bigint";
					isRange = true;
					break;
				case "numrange":
					dbType  = "numeric";
					isRange = true;
					break;
				case "tsrange":
					dbType  = "timestamp";
					isRange = true;
					break;
				case "tstzrange":
					dbType  = "timestamp with time zone";
					isRange = true;
					break;
				case "timestamptz":
					dbType = "timestamp with time zone";
					break;
				case "daterange":
					dbType  = "date";
					isRange = true;
					break;
				case "int2":
				case "smallserial":
				case "serial2":
					dbType = "smallint";
					break;
				case "int":
				case "int4":
				case "serial":
				case "serial4":
					dbType = "integer";
					break;
				case "int8":
				case "bigserial":
				case "serial8":
					dbType = "bigint";
					break;
				case "float":
					dbType = "double precision";
					break;
				case "varchar":
					dbType = "character varying";
					break;
				case "varbit":
					dbType = "bit varying";
					break;
			}

			if (dbType.StartsWith("float(") && dbType.EndsWith(")"))
			{
				if (int.TryParse(dbType.Substring("float(".Length, dbType.Length - "float(".Length - 1), out var precision))
				{
					if (precision >= 1 && precision <= 24)
						dbType = "real";
					else if (precision >= 25 && precision <= 53)
						dbType = "real";
					// else bad type
				}
			}

			if (dbType.StartsWith("numeric(") || dbType.StartsWith("decimal"))
				dbType = "numeric";

			if (dbType.StartsWith("varchar varying(") || dbType.StartsWith("varchar("))
				dbType = "varchar varying";

			if (dbType.StartsWith("char(") || dbType.StartsWith("character("))
				dbType = "character";

			if (dbType.StartsWith("interval"))
				dbType = "interval";

			if (dbType.StartsWith("timestamp"))
				dbType = dbType.Contains("with time zone") ? "timestamp with time zone" : "timestamp";

			if (dbType.StartsWith("time(") || dbType.StartsWith("time "))
				dbType = dbType.Contains("with time zone") ? "time with time zone" : "time";

			if (dbType.StartsWith("bit("))
				dbType = "bit";

			if (dbType.StartsWith("bit varying("))
				dbType = "bit varying";

			if (_npgsqlTypeMap.ContainsKey(dbType))
			{
				var result = _npgsqlTypeMap[dbType];

				if (isArray)
					result = Enum.Parse(NpgsqlDbType, ((int)result | _npgsqlTypeArrayFlag).ToString());

				if (isRange)
					result = Enum.Parse(NpgsqlDbType, ((int)result | _npgsqlTypeRangeFlag).ToString());

				return result;
			}

			return null;
		}
	}
}
