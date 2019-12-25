using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	public class PostgreSQLDataProvider : DynamicDataProviderBase
	{
		public PostgreSQLDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
			: this(
				GetProviderName(version),
				version)
		{
		}

		public PostgreSQLDataProvider(string name, PostgreSQLVersion version = PostgreSQLVersion.v92)
			: base(name, null!)
		{
			Version = version;

			SqlProviderFlags.IsApplyJoinSupported              = version != PostgreSQLVersion.v92;
			SqlProviderFlags.IsInsertOrUpdateSupported         = version == PostgreSQLVersion.v95;
			SqlProviderFlags.IsUpdateSetTableAliasSupported    = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryOrderBySupported        = true;
			SqlProviderFlags.IsAllSetOperationsSupported       = true;

			SetCharFieldToType<char>("bpchar"   , (r, i) => DataTools.GetChar(r, i));
			SetCharFieldToType<char>("character", (r, i) => DataTools.GetChar(r, i));

			SetCharField("bpchar"   , (r,i) => r.GetString(i).TrimEnd(' '));
			SetCharField("character", (r,i) => r.GetString(i).TrimEnd(' '));

			switch (version)
			{
				default:
				case PostgreSQLVersion.v92:
					_sqlOptimizer = new PostgreSQLSql92Optimizer(SqlProviderFlags);
					break;
				case PostgreSQLVersion.v93:
					_sqlOptimizer = new PostgreSQLSql93Optimizer(SqlProviderFlags);
					break;
				case PostgreSQLVersion.v95:
					_sqlOptimizer = new PostgreSQLSql95Optimizer(SqlProviderFlags);
					break;
			}

			Wrapper = new Lazy<PostgreSQLWrappers.IPostgreSQLWrapper>(() => Initialize(), true);
		}

		internal readonly Lazy<PostgreSQLWrappers.IPostgreSQLWrapper> Wrapper;

		private PostgreSQLWrappers.IPostgreSQLWrapper Initialize()
		{
			var wrapper = PostgreSQLWrappers.Initialize();

			// https://www.postgresql.org/docs/current/static/datatype.html
			// not all types are supported now
			// numeric types
			mapType("smallint"                , PostgreSQLWrappers.NpgsqlDbType.Smallint);
			mapType("integer"                 , PostgreSQLWrappers.NpgsqlDbType.Integer);
			mapType("bigint"                  , PostgreSQLWrappers.NpgsqlDbType.Bigint);
			mapType("numeric"                 , PostgreSQLWrappers.NpgsqlDbType.Numeric);
			mapType("real"                    , PostgreSQLWrappers.NpgsqlDbType.Real);
			mapType("double precision"        , PostgreSQLWrappers.NpgsqlDbType.Double);
			// monetary types
			mapType("money"                   , PostgreSQLWrappers.NpgsqlDbType.Money);
			// character types
			mapType("character"               , PostgreSQLWrappers.NpgsqlDbType.Char);
			mapType("character varying"       , PostgreSQLWrappers.NpgsqlDbType.Varchar);
			mapType("text"                    , PostgreSQLWrappers.NpgsqlDbType.Text);
			mapType("name"                    , PostgreSQLWrappers.NpgsqlDbType.Name);
			mapType("char"                    , PostgreSQLWrappers.NpgsqlDbType.InternalChar);
			// binary types
			mapType("bytea"                   , PostgreSQLWrappers.NpgsqlDbType.Bytea);
			// date/time types (reltime missing from enum)
			mapType("timestamp"               , PostgreSQLWrappers.NpgsqlDbType.Timestamp);
			mapType("timestamp with time zone", PostgreSQLWrappers.NpgsqlDbType.TimestampTZ);
			mapType("date"                    , PostgreSQLWrappers.NpgsqlDbType.Date);
			mapType("time"                    , PostgreSQLWrappers.NpgsqlDbType.Time);
			mapType("time with time zone"     , PostgreSQLWrappers.NpgsqlDbType.TimeTZ);
			mapType("interval"                , PostgreSQLWrappers.NpgsqlDbType.Interval);
			mapType("abstime"                 , PostgreSQLWrappers.NpgsqlDbType.Abstime);
			// boolean type
			mapType("boolean"                 , PostgreSQLWrappers.NpgsqlDbType.Boolean);
			// geometric types
			mapType("point"                   , PostgreSQLWrappers.NpgsqlDbType.Point);
			mapType("line"                    , PostgreSQLWrappers.NpgsqlDbType.Line);
			mapType("lseg"                    , PostgreSQLWrappers.NpgsqlDbType.LSeg);
			mapType("box"                     , PostgreSQLWrappers.NpgsqlDbType.Box);
			mapType("path"                    , PostgreSQLWrappers.NpgsqlDbType.Path);
			mapType("polygon"                 , PostgreSQLWrappers.NpgsqlDbType.Polygon);
			mapType("circle"                  , PostgreSQLWrappers.NpgsqlDbType.Circle);
			// network address types
			mapType("cidr"                    , PostgreSQLWrappers.NpgsqlDbType.Cidr);
			mapType("inet"                    , PostgreSQLWrappers.NpgsqlDbType.Inet);
			mapType("macaddr"                 , PostgreSQLWrappers.NpgsqlDbType.MacAddr);
			mapType("macaddr8"                , PostgreSQLWrappers.NpgsqlDbType.MacAddr8);
			// bit string types
			mapType("bit"                     , PostgreSQLWrappers.NpgsqlDbType.Bit);
			mapType("bit varying"             , PostgreSQLWrappers.NpgsqlDbType.Varbit);
			// text search types
			mapType("tsvector"                , PostgreSQLWrappers.NpgsqlDbType.TsVector);
			mapType("tsquery"                 , PostgreSQLWrappers.NpgsqlDbType.TsQuery);
			// UUID type
			mapType("uuid"                    , PostgreSQLWrappers.NpgsqlDbType.Uuid);
			// XML type
			mapType("xml"                     , PostgreSQLWrappers.NpgsqlDbType.Xml);
			// JSON types
			mapType("json"                    , PostgreSQLWrappers.NpgsqlDbType.Json);
			mapType("jsonb"                   , PostgreSQLWrappers.NpgsqlDbType.Jsonb);
			// Object Identifier Types (only supported by npgsql)
			mapType("oid"                     , PostgreSQLWrappers.NpgsqlDbType.Oid);
			mapType("regtype"                 , PostgreSQLWrappers.NpgsqlDbType.Regtype);
			mapType("xid"                     , PostgreSQLWrappers.NpgsqlDbType.Xid);
			mapType("cid"                     , PostgreSQLWrappers.NpgsqlDbType.Cid);
			mapType("tid"                     , PostgreSQLWrappers.NpgsqlDbType.Tid);
			// other types
			mapType("citext"                  , PostgreSQLWrappers.NpgsqlDbType.Citext);
			mapType("hstore"                  , PostgreSQLWrappers.NpgsqlDbType.Hstore);
			mapType("refcursor"               , PostgreSQLWrappers.NpgsqlDbType.Refcursor);
			mapType("oidvector"               , PostgreSQLWrappers.NpgsqlDbType.Oidvector);
			mapType("int2vector"              , PostgreSQLWrappers.NpgsqlDbType.Int2Vector);

			wrapper.SetupMappingSchema(MappingSchema);

			SetProviderField(wrapper.NpgsqlTimeSpanType, wrapper.NpgsqlTimeSpanType, "GetInterval"             , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.NpgsqlDateTimeType, wrapper.NpgsqlDateTimeType, "GetTimeStamp"            , dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.NpgsqlInetType    , wrapper.NpgsqlInetType    , "GetProviderSpecificValue", dataReaderType: wrapper.DataReaderType);
			SetProviderField(wrapper.NpgsqlDateType    , wrapper.NpgsqlDateType    , "GetDate"                 , dataReaderType: wrapper.DataReaderType);

			return wrapper;

			bool mapType(string dbType, PostgreSQLWrappers.NpgsqlDbType type)
			{
				if (wrapper.IsDbTypeSupported(type))
				{
					_npgsqlTypeMap.Add(dbType, type);
					return true;
				}
				return false;
			}
		}

		protected override string? NormalizeTypeName(string? typeName)
		{
			if (typeName == null)
				return null;

			if (typeName.StartsWith("character("))
				return "character";

			return typeName;
		}

		public PostgreSQLVersion Version { get; private set; }

		internal bool HasMacAddr8 => Wrapper.Value.IsDbTypeSupported(PostgreSQLWrappers.NpgsqlDbType.MacAddr8);

		/// <summary>
		/// Map of canonical PostgreSQL type name to NpgsqlDbType enumeration value.
		/// This map shouldn't be used directly, you should resolve PostgreSQL types using
		/// <see cref="GetNativeType(string, bool)"/> method, which takes into account different type aliases.
		/// </summary>
		private IDictionary<string, PostgreSQLWrappers.NpgsqlDbType> _npgsqlTypeMap = new Dictionary<string, PostgreSQLWrappers.NpgsqlDbType>();

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
		}

		public    override string ConnectionNamespace => "Npgsql";
		protected override string ConnectionTypeName  => "Npgsql.NpgsqlConnection, Npgsql";
		protected override string DataReaderTypeName  => "Npgsql.NpgsqlDataReader, Npgsql";

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new PostgreSQLSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer() => _sqlOptimizer;

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new PostgreSQLSchemaProvider(this);
		}

#if NETSTANDARD2_0 || NETCOREAPP2_1
		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}
#endif

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			if (value is IDictionary && dataType.DataType == DataType.Undefined)
			{
				dataType = dataType.WithDataType(DataType.Dictionary);
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			// didn't tried to detect and cleanup unnecessary type mappings, as npgsql develops rapidly and
			// it doesn't pay efforts to track changes for each version in this area
			PostgreSQLWrappers.NpgsqlDbType? type = null;
			switch (dataType.DataType)
			{
				case DataType.Money     : type = PostgreSQLWrappers.NpgsqlDbType.Money  ; break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary : type = PostgreSQLWrappers.NpgsqlDbType.Bytea  ; break;
				case DataType.Boolean   : type = PostgreSQLWrappers.NpgsqlDbType.Boolean; break;
				case DataType.Xml       : type = PostgreSQLWrappers.NpgsqlDbType.Xml    ; break;
				case DataType.Text      :
				case DataType.NText     : type = PostgreSQLWrappers.NpgsqlDbType.Text   ; break;
				case DataType.BitArray  : type = PostgreSQLWrappers.NpgsqlDbType.Bit    ; break;
				case DataType.Dictionary: type = PostgreSQLWrappers.NpgsqlDbType.Hstore ; break;
				case DataType.Json      : type = PostgreSQLWrappers.NpgsqlDbType.Json   ; break;
				case DataType.BinaryJson: type = PostgreSQLWrappers.NpgsqlDbType.Jsonb  ; break;
			}

			if (!string.IsNullOrEmpty(dataType.DbType))
			{
				type = GetNativeType(dataType.DbType);
			}

			if (type != null)
			{
				var param = TryConvertParameter(Wrapper.Value.ParameterType, parameter, dataConnection.MappingSchema);
				if (param != null)
				{
					Wrapper.Value.TypeSetter(param, type.Value);
					return;
				}
			}

			switch (dataType.DataType)
			{
				case DataType.SByte     : parameter.DbType = DbType.Int16     ; return;
				case DataType.UInt16    : parameter.DbType = DbType.Int32     ; return;
				case DataType.UInt32    : parameter.DbType = DbType.Int64     ; return;
				case DataType.UInt64    :
				case DataType.VarNumeric: parameter.DbType = DbType.Decimal   ; return;
				case DataType.DateTime2 : parameter.DbType = DbType.DateTime  ; return;
				// fallback mappings
				case DataType.Money     : parameter.DbType = DbType.Currency  ; break;
				case DataType.Xml       : parameter.DbType = DbType.Xml       ; break;
				case DataType.Text      : parameter.DbType = DbType.AnsiString; break;
				case DataType.NText     : parameter.DbType = DbType.String    ; break;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary : parameter.DbType = DbType.Binary    ; break;
				// those types doesn't have fallback DbType
				case DataType.BitArray  : parameter.DbType = DbType.Binary    ; break;
				case DataType.Dictionary: parameter.DbType = DbType.Object    ; break;
				case DataType.Json      : parameter.DbType = DbType.String    ; break;
				case DataType.BinaryJson: parameter.DbType = DbType.String    ; break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return new PostgreSQLBulkCopy(this).BulkCopy(
				options.BulkCopyType == BulkCopyType.Default ? PostgreSQLTools.DefaultBulkCopyType : options.BulkCopyType,
				table,
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
		/// <remarks>
		/// Returned value could be invalid for <see cref="PostgreSQLWrappers.NpgsqlDbType"/> type, if range/array flags
		/// were applied. Don't try to use results of this function for anything except passing it directly to npgsql.
		/// </remarks>
		internal PostgreSQLWrappers.NpgsqlDbType? GetNativeType(string? dbType, bool convertAlways = false)
		{
			if (string.IsNullOrWhiteSpace(dbType))
				return null;

			dbType = dbType!.ToLower();

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

			if (dbType.StartsWith("varchar(") || dbType.StartsWith("character varying("))
				dbType = "character varying";

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

				// because NpgsqlDbType fields numeric values changed in npgsql4,
				// applying flag-like array/range bits is not straightforward process
				result = Wrapper.Value.ApplyFlags(result, isArray, isRange, convertAlways);

				return result;
			}

			return null;
		}

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema PostgreSQL92MappingSchema = new PostgreSQL92MappingSchema();
			public static readonly MappingSchema PostgreSQL93MappingSchema = new PostgreSQL93MappingSchema();
			public static readonly MappingSchema PostgreSQL95MappingSchema = new PostgreSQL95MappingSchema();
		}

		public override MappingSchema MappingSchema
		{
			get
			{
				switch (Version)
				{
					case PostgreSQLVersion.v92: return MappingSchemaInstance.PostgreSQL92MappingSchema;
					case PostgreSQLVersion.v93: return MappingSchemaInstance.PostgreSQL93MappingSchema;
					case PostgreSQLVersion.v95: return MappingSchemaInstance.PostgreSQL95MappingSchema;
				}

				return base.MappingSchema;
			}
		}
	}
}
