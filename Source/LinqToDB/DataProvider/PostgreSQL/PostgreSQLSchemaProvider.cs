using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using SchemaProvider;
	using System.Data;
	using System.Data.Common;

	class PostgreSQLSchemaProvider : SchemaProviderBase
	{
		private readonly PostgreSQLDataProvider _provider;

		public PostgreSQLSchemaProvider(PostgreSQLDataProvider provider)
		{
			_provider = provider;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var list = new[]
			{
				new DataTypeInfo { TypeName = "name",                        DataType = typeof(string).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "oid",                         DataType = typeof(int).           AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "xid",                         DataType = typeof(int).           AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "smallint",                    DataType = typeof(short).         AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "int2",                        DataType = typeof(short).         AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "integer",                     DataType = typeof(int).           AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "int4",                        DataType = typeof(int).           AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "bigint",                      DataType = typeof(long).          AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "int8",                        DataType = typeof(long).          AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "real",                        DataType = typeof(float).         AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "float4",                      DataType = typeof(float).         AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "double precision",            DataType = typeof(double).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "float8",                      DataType = typeof(double).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "boolean",                     DataType = typeof(bool).          AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "bool",                        DataType = typeof(bool).          AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "regproc",                     DataType = typeof(object).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "money",                       DataType = typeof(decimal).       AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "text",                        DataType = typeof(string).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "xml",                         DataType = typeof(string).        AssemblyQualifiedName },
				
				new DataTypeInfo { TypeName = "bytea",                       DataType = typeof(byte[]).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "uuid",                        DataType = typeof(Guid).          AssemblyQualifiedName },

				new DataTypeInfo { TypeName = "hstore",                      DataType = typeof(Dictionary<string,string>).AssemblyQualifiedName},
				new DataTypeInfo { TypeName = "json",                        DataType = typeof(string).        AssemblyQualifiedName },
				new DataTypeInfo { TypeName = "jsonb",                       DataType = typeof(string).        AssemblyQualifiedName },

				new DataTypeInfo { TypeName = "character varying",           DataType = typeof(string).        AssemblyQualifiedName, CreateFormat = "character varying({0})",            CreateParameters = "length" },
				new DataTypeInfo { TypeName = "varchar",                     DataType = typeof(string).        AssemblyQualifiedName, CreateFormat = "character varying({0})",            CreateParameters = "length" },
				new DataTypeInfo { TypeName = "character",                   DataType = typeof(string).        AssemblyQualifiedName, CreateFormat = "character({0})",                    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "bpchar",                      DataType = typeof(string).        AssemblyQualifiedName, CreateFormat = "character({0})",                    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "numeric",                     DataType = typeof(decimal).       AssemblyQualifiedName, CreateFormat = "numeric({0},{1})",                  CreateParameters = "precision,scale" },
				
				new DataTypeInfo { TypeName = "timestamptz",                 DataType = typeof(DateTimeOffset).AssemblyQualifiedName, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" },
				new DataTypeInfo { TypeName = "timestamp with time zone",    DataType = typeof(DateTimeOffset).AssemblyQualifiedName, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" },
				
				new DataTypeInfo { TypeName = "timestamp",                   DataType = typeof(DateTime).      AssemblyQualifiedName, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" },
				new DataTypeInfo { TypeName = "timestamp without time zone", DataType = typeof(DateTime).      AssemblyQualifiedName, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" },
			}.ToList();

			var provider = (PostgreSQLDataProvider)dataConnection.DataProvider;

			if (provider.NpgsqlInetType       != null) list.Add(new DataTypeInfo { TypeName = "inet"                       , DataType = provider.NpgsqlInetType.      AssemblyQualifiedName });
			if (provider.NpgsqlInetType       != null) list.Add(new DataTypeInfo { TypeName = "cidr"                       , DataType = provider.NpgsqlInetType.      AssemblyQualifiedName });
			if (provider.NpgsqlPointType      != null) list.Add(new DataTypeInfo { TypeName = "point"                      , DataType = provider.NpgsqlPointType.     AssemblyQualifiedName });
			if (provider.NpgsqlLineType       != null) list.Add(new DataTypeInfo { TypeName = "line"                       , DataType = provider.NpgsqlLineType.      AssemblyQualifiedName });
			if (provider.NpgsqlLSegType       != null) list.Add(new DataTypeInfo { TypeName = "lseg"                       , DataType = provider.NpgsqlLSegType.      AssemblyQualifiedName });
			if (provider.NpgsqlBoxType        != null) list.Add(new DataTypeInfo { TypeName = "box"                        , DataType = provider.NpgsqlBoxType.       AssemblyQualifiedName });
			if (provider.NpgsqlPathType       != null) list.Add(new DataTypeInfo { TypeName = "path"                       , DataType = provider.NpgsqlPathType.      AssemblyQualifiedName });
			if (provider.NpgsqlPolygonType    != null) list.Add(new DataTypeInfo { TypeName = "polygon"                    , DataType = provider.NpgsqlPolygonType.   AssemblyQualifiedName });
			if (provider.NpgsqlCircleType     != null) list.Add(new DataTypeInfo { TypeName = "circle"                     , DataType = provider.NpgsqlCircleType.    AssemblyQualifiedName });
			if (provider.NpgsqlIntervalType   != null) list.Add(new DataTypeInfo { TypeName = "interval"                   , DataType = provider.NpgsqlIntervalType.  AssemblyQualifiedName, CreateFormat = "interval({0})",                     CreateParameters = "precision" });
			if (provider.NpgsqlDateType       != null) list.Add(new DataTypeInfo { TypeName = "date"                       , DataType = provider.NpgsqlDateType.      AssemblyQualifiedName });
			else                                       list.Add(new DataTypeInfo { TypeName = "date"                       , DataType = typeof(DateTime).             AssemblyQualifiedName });
			if (provider.NpgsqlTimeType       != null) list.Add(new DataTypeInfo { TypeName = "time with time zone"        , DataType = provider.NpgsqlTimeType.      AssemblyQualifiedName, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "time with time zone"        , DataType = typeof(DateTimeOffset).       AssemblyQualifiedName, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			if (provider.NpgsqlTimeType       != null) list.Add(new DataTypeInfo { TypeName = "timetz"                     , DataType = provider.NpgsqlTimeType.      AssemblyQualifiedName, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "timetz"                     , DataType = typeof(DateTimeOffset).       AssemblyQualifiedName, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			if (provider.NpgsqlTimeTZType     != null) list.Add(new DataTypeInfo { TypeName = "time without time zone"     , DataType = provider.NpgsqlTimeTZType.    AssemblyQualifiedName, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "time without time zone"     , DataType = typeof(TimeSpan).             AssemblyQualifiedName, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });
			if (provider.NpgsqlTimeTZType     != null) list.Add(new DataTypeInfo { TypeName = "time"                       , DataType = provider.NpgsqlTimeTZType.    AssemblyQualifiedName, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "time"                       , DataType = typeof(TimeSpan).             AssemblyQualifiedName, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });

			list.Add(new DataTypeInfo { TypeName = "macaddr",     DataType = (provider.NpgsqlMacAddressType ?? typeof(PhysicalAddress)).AssemblyQualifiedName });
			list.Add(new DataTypeInfo { TypeName = "macaddr8",    DataType = (provider.NpgsqlMacAddressType ?? typeof(PhysicalAddress)).AssemblyQualifiedName });
			list.Add(new DataTypeInfo { TypeName = "bit",         DataType = (provider.BitStringType        ?? typeof(BitArray)).       AssemblyQualifiedName, CreateFormat = "bit({0})",         CreateParameters = "size" });
			list.Add(new DataTypeInfo { TypeName = "bit varying", DataType = (provider.BitStringType        ?? typeof(BitArray)).       AssemblyQualifiedName, CreateFormat = "bit varying({0})", CreateParameters = "size" });
			list.Add(new DataTypeInfo { TypeName = "varbit",      DataType = (provider.BitStringType        ?? typeof(BitArray)).       AssemblyQualifiedName, CreateFormat = "bit varying({0})", CreateParameters = "size" });

			return list;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var sql = (@"
				SELECT
					table_catalog || '.' || table_schema || '.' || table_name            as TableID,
					table_catalog                                                        as CatalogName,
					table_schema                                                         as SchemaName,
					table_name                                                           as TableName,
					table_schema = 'public'                                              as IsDefaultSchema,
					table_type = 'VIEW'                                                  as IsView,
					left(table_schema, 3) = 'pg_' OR table_schema = 'information_schema' as IsProviderSpecific
				FROM
					information_schema.tables");

			if (ExcludedSchemas.Count == 0 && IncludedSchemas.Count == 0)
				sql += @"
				WHERE
					table_schema NOT IN ('pg_catalog','information_schema')";

			return dataConnection.Query<TableInfo>(sql).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
				dataConnection.Query<PrimaryKeyInfo>(@"
					SELECT
						current_database() || '.' || pg_namespace.nspname || '.' || pg_class.relname as TableID,
						pg_constraint.conname                                                        as PrimaryKeyName,
						attname                                                                      as ColumnName,
						attnum                                                                       as Ordinal
					FROM
						pg_attribute
							JOIN pg_constraint ON pg_attribute.attrelid = pg_constraint.conrelid AND pg_attribute.attnum = ANY(pg_constraint.conkey)
							JOIN pg_class ON pg_class.oid = pg_constraint.conrelid
								JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid
					WHERE
						pg_constraint.contype = 'p'")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var sql = @"
					SELECT
						table_catalog || '.' || table_schema || '.' || table_name           as TableID,
						column_name                                                         as Name,
						is_nullable = 'YES'                                                 as IsNullable,
						ordinal_position                                                    as Ordinal,
						data_type                                                           as DataType,
						character_maximum_length                                            as Length,
						COALESCE(
							numeric_precision::integer,
							datetime_precision::integer,
							interval_precision::integer)                                    as Precision,
						numeric_scale                                                       as Scale,
						is_identity = 'YES' OR COALESCE(column_default ~* 'nextval', false) as IsIdentity,
						is_generated <> 'NEVER'                                             as SkipOnInsert,
						is_updatable = 'NO'                                                 as SkipOnUpdate
					FROM
						information_schema.columns";

			if (ExcludedSchemas.Count == 0 || IncludedSchemas.Count == 0)
				sql += @"
					WHERE
						table_schema NOT IN ('pg_catalog','information_schema')";

			return dataConnection.Query<ColumnInfo>(sql).ToList();
		}

		protected override List<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var data = dataConnection.Query(
				rd => new
				{
					name         = rd[0],
					thisTable    = rd[1],
					otherTable   = rd[2],
					thisColumns  = new[] { rd[ 3], rd[ 4], rd[ 5], rd[ 6], rd[ 7], rd[ 8], rd[ 9], rd[10], rd[11], rd[12], rd[13], rd[14], rd[15], rd[16], rd[17], rd[18] },
					otherColumns = new[] { rd[19], rd[20], rd[21], rd[22], rd[23], rd[24], rd[25], rd[26], rd[27], rd[28], rd[29], rd[30], rd[31], rd[32], rd[33], rd[34] },
				}, @"
				SELECT
					pg_constraint.conname,
					current_database() || '.' || this_schema.nspname  || '.' || this_table.relname,
					current_database() || '.' || other_schema.nspname || '.' || other_table.relname,
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[16]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[16])
				FROM
					pg_constraint
						JOIN pg_class as this_table ON this_table.oid = pg_constraint.conrelid
							JOIN pg_namespace as this_schema ON this_table.relnamespace = this_schema.oid
						JOIN pg_class as other_table ON other_table.oid = pg_constraint.confrelid
							JOIN pg_namespace as other_schema ON other_table.relnamespace = other_schema.oid
				WHERE
					pg_constraint.contype = 'f'")
				.ToList();

			return
			(
				from item in data

				let name         = Convert.ToString(item.name)
				let thisTableID  = Convert.ToString (item.thisTable)
				let otherTableID = Convert.ToString (item.otherTable)

				from col in item.thisColumns
					.Zip(item.otherColumns, (thisColumn,otherColumn) => new { thisColumn, otherColumn })
					.Select((cs,i) => new { cs.thisColumn, cs.otherColumn, ordinal = i})
				where col.thisColumn != null && !(col.thisColumn is DBNull)
				select new ForeignKeyInfo
				{
					Name         = name,
					ThisTableID  = thisTableID,
					OtherTableID = otherTableID,
					ThisColumn   = Convert.ToString(col.thisColumn),
					OtherColumn  = Convert.ToString(col.otherColumn),
					Ordinal      = col.ordinal
				}
			).ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType)
			{
				case "bpchar"                      :
				case "character"                   : return DataType.NChar;
				case "text"                        : return DataType.Text;
				case "int2"                        :
				case "smallint"                    : return DataType.Int16;
				case "oid"                         :
				case "xid"                         :
				case "int4"                        :
				case "integer"                     : return DataType.Int32;
				case "int8"                        :
				case "bigint"                      : return DataType.Int64;
				case "float4"                      :
				case "real"                        : return DataType.Single;
				case "float8"                      :
				case "double precision"            : return DataType.Double;
				case "bytea"                       : return DataType.Binary;
				case "bool"                        :
				case "boolean"                     : return DataType.Boolean;
				case "numeric"                     : return DataType.Decimal;
				case "money"                       : return DataType.Money;
				case "uuid"                        : return DataType.Guid;
				case "varchar"                     :
				case "character varying"           : return DataType.NVarChar;
				case "timestamptz"                 :
				case "timestamp with time zone"    : return DataType.DateTimeOffset;
				case "timestamp"                   :
				case "timestamp without time zone" : return DataType.DateTime2;
				case "timetz"                      :
				case "time with time zone"         :
				case "time"                        :
				case "time without time zone"      :
				case "interval"                    : return DataType.Time;
				case "date"                        : return DataType.Date;
				case "xml"                         : return DataType.Xml;
				case "point"                       :
				case "lseg"                        :
				case "box"                         :
				case "circle"                      :
				case "path"                        :
				case "line"                        :
				case "polygon"                     :
				case "inet"                        :
				case "cidr"                        :
				case "macaddr"                     :
				case "macaddr8"                    :
				case "ARRAY"                       :
				case "anyarray"                    :
				case "anyelement"                  :
				case "USER-DEFINED"                : return DataType.Udt;
				case "bit"                         :
				case "bit varying"                 :
				case "varbit"                      : return DataType.BitArray;
				case "hstore"                      : return DataType.Dictionary;
				case "json"                        : return DataType.Json;
				case "jsonb"                       : return DataType.BinaryJson;
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return "NpgsqlTypes";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType)
			{
				case "interval"                    : return _provider.NpgsqlIntervalType?.Name;
				case "time"                        :
				case "time without time zone"      : return _provider.NpgsqlTimeType    ?.Name;
				case "timetz"                      :
				case "time with time zone"         : return _provider.NpgsqlTimeTZType  ?.Name;
				case "timestamp"                   :
				case "timestamptz"                 :
				case "timestamp with time zone"    :
				case "timestamp without time zone" :
				case "date"                        : return _provider.NpgsqlDateType    ?.Name;
				case "point"                       : return _provider.NpgsqlPointType   ?.Name;
				case "lseg"                        : return _provider.NpgsqlLSegType    ?.Name;
				case "box"                         : return _provider.NpgsqlBoxType     ?.Name;
				case "circle"                      : return _provider.NpgsqlCircleType  ?.Name;
				case "path"                        : return _provider.NpgsqlPathType    ?.Name;
				case "polygon"                     : return _provider.NpgsqlPolygonType ?.Name;
				case "line"                        : return _provider.NpgsqlLineType    ?.Name;
				case "cidr"                        :
				case "inet"                        : return _provider.NpgsqlInetType    ?.Name;
				case "geometry "                   : return "PostgisGeometry";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			// because information schema doesn't contain information about function kind like aggregate or table function
			// we need to query additional data from pg_proc
			// in postgresql 11 pg_proc some requred columns changed, so we need to execute different queries for pre-11 and 11+ versions
			var version = dataConnection.Query<int>("SHOW  server_version_num").Single();

			if (version < 110000)
			{
				return dataConnection
					.Query(rd =>
					{
						var catalog       = rd.GetString(0);
						var schema        = rd.GetString(1);
						var isTableResult = Converter.ChangeTypeTo<bool>(rd[7]);

						return new ProcedureInfo()
						{
							ProcedureID         = catalog + "." + schema + "." + rd.GetString(5),
							CatalogName         = catalog,
							SchemaName          = schema,
							ProcedureName       = rd.GetString(2),
							// versions prior 11 doesn't support procedures but support functions with void return type
							// still, we report them as function in metadata. Just without return parameter
							IsFunction          = rd.GetString(3) == "FUNCTION",
							IsTableFunction     = isTableResult,
							IsAggregateFunction = Converter.ChangeTypeTo<bool>(rd[6]),
							IsDefaultSchema     = schema == "public",
							ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[4]),
							// result of function has dynamic form and vary per call if function return type is 'record'
							// only exception is function with out/inout parameters, where we know that record contains those parameters
							IsResultDynamic     = Converter.ChangeTypeTo<string>(rd[8]) == "record" && Converter.ChangeTypeTo<int>(rd[9]) == 0
						};
					}, @"
SELECT	r.ROUTINE_CATALOG,
		r.ROUTINE_SCHEMA,
		r.ROUTINE_NAME,
		r.ROUTINE_TYPE,
		r.ROUTINE_DEFINITION,
		r.SPECIFIC_NAME,
		p.proisagg,
		p.proretset,
		r.DATA_TYPE,
		outp.cnt
	FROM INFORMATION_SCHEMA.ROUTINES r
		LEFT JOIN pg_catalog.pg_namespace n ON r.ROUTINE_SCHEMA = n.nspname
		LEFT JOIN pg_catalog.pg_proc p ON p.pronamespace = n.oid AND r.SPECIFIC_NAME = p.proname || '_' || p.oid
		LEFT JOIN (SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, COUNT(*) as cnt FROM INFORMATION_SCHEMA.parameters WHERE parameter_mode IN('OUT', 'INOUT') GROUP BY SPECIFIC_SCHEMA, SPECIFIC_NAME) as outp
			ON r.SPECIFIC_SCHEMA = outp.SPECIFIC_SCHEMA AND r.SPECIFIC_NAME = outp.SPECIFIC_NAME")
					.ToList();
			}
			else
			{
				return dataConnection
					.Query(rd =>
					{
						var catalog       = rd.GetString(0);
						var schema        = rd.GetString(1);
						var isTableResult = Converter.ChangeTypeTo<bool>(rd[6]);
						var kind          = Converter.ChangeTypeTo<char>(rd[5]);

						return new ProcedureInfo()
						{
							ProcedureID         = catalog + "." + schema + "." + rd.GetString(4),
							CatalogName         = catalog,
							SchemaName          = schema,
							ProcedureName       = rd.GetString(2),
							// versions prior 11 doesn't support procedures but support functions with void return type
							// still, we report them as function in metadata. Just without return parameter
							IsFunction          = kind != 'p',
							IsTableFunction     = isTableResult,
							// this is only diffrence starting from v11
							IsAggregateFunction = kind == 'a',
							IsWindowFunction    = kind == 'w',
							IsDefaultSchema     = schema == "public",
							ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[3]),
							IsResultDynamic     = Converter.ChangeTypeTo<string>(rd[7]) == "record" && Converter.ChangeTypeTo<int>(rd[8]) == 0
						};
					}, @"
SELECT	r.ROUTINE_CATALOG,
		r.ROUTINE_SCHEMA,
		r.ROUTINE_NAME,
		r.ROUTINE_DEFINITION,
		r.SPECIFIC_NAME,
		p.prokind,
		p.proretset,
		r.DATA_TYPE,
		outp.cnt
	FROM INFORMATION_SCHEMA.ROUTINES r
		LEFT JOIN pg_catalog.pg_namespace n ON r.ROUTINE_SCHEMA = n.nspname
		LEFT JOIN pg_catalog.pg_proc p ON p.pronamespace = n.oid AND r.SPECIFIC_NAME = p.proname || '_' || p.oid
		LEFT JOIN (SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, COUNT(*)as cnt FROM INFORMATION_SCHEMA.parameters WHERE parameter_mode IN('OUT', 'INOUT') GROUP BY SPECIFIC_SCHEMA, SPECIFIC_NAME) as outp
			ON r.SPECIFIC_SCHEMA = outp.SPECIFIC_SCHEMA AND r.SPECIFIC_NAME = outp.SPECIFIC_NAME")
					.ToList();
			}
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			return dataConnection
				.Query(rd =>
				{
					var mode = Converter.ChangeTypeTo<string>(rd[4]);
					return new ProcedureParameterInfo()
					{
						ProcedureID   = rd.GetString(0) + "." + rd.GetString(1) + "." + rd.GetString(2),
						ParameterName = Converter.ChangeTypeTo<string>(rd[5]),
						IsIn          = mode == "IN"  || mode == "INOUT",
						IsOut         = mode == "OUT" || mode == "INOUT",
						Ordinal       = Converter.ChangeTypeTo<int>(rd[3]),
						IsResult      = false,
						DataType      = rd.GetString(6),
						// those fields not supported by pgsql on parameter level
						Precision     = null,
						Scale         = null,
						Length        = null
					};
				}, "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.parameters")
				// populare return parameters for functions
				.ToList()
				// there is no separate result parameter for functions (maybe it will be for procedures in v11), so
				// we need to read data_type of function itself if it is not table/void function to define return parameter
				.Concat(dataConnection.Query(rd => new ProcedureParameterInfo()
				{
					ProcedureID   = rd.GetString(0) + "." + rd.GetString(1) + "." + rd.GetString(2),
					ParameterName = null,
					IsIn          = false,
					IsOut         = false,
					Ordinal       = 0,
					IsResult      = true,
					DataType      = rd.GetString(3),
					// not supported by pgsql
					Precision     = null,
					Scale         = null,
					Length        = null
				}, @"SELECT r.SPECIFIC_CATALOG, r.SPECIFIC_SCHEMA, r.SPECIFIC_NAME, r.DATA_TYPE
	FROM INFORMATION_SCHEMA.ROUTINES r
		LEFT JOIN pg_catalog.pg_namespace n ON r.ROUTINE_SCHEMA = n.nspname
		LEFT JOIN pg_catalog.pg_proc p ON p.pronamespace = n.oid AND r.SPECIFIC_NAME = p.proname || '_' || p.oid
		LEFT JOIN (SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, COUNT(*)as cnt FROM INFORMATION_SCHEMA.parameters WHERE parameter_mode IN('OUT', 'INOUT') GROUP BY SPECIFIC_SCHEMA, SPECIFIC_NAME) as outp
			ON r.SPECIFIC_SCHEMA = outp.SPECIFIC_SCHEMA AND r.SPECIFIC_NAME = outp.SPECIFIC_NAME
	WHERE r.DATA_TYPE <> 'record' AND r.DATA_TYPE <> 'void' AND p.proretset = false AND (outp.cnt IS NULL OR outp.cnt = 0)"))
				.ToList();
		}

		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			commandText = "SELECT * FROM " + commandText + "(";

			var first = true;
			foreach (var parameter in procedure.Parameters.Where(p => p.IsIn))
			{
				if (!first)
					commandText += ",";
				else
					first = false;

				commandText += "NULL";

				// we don't have proper support for any* yet
				if (parameter.SchemaType == "anyarray")
					commandText += "::int[]";
				if (parameter.SchemaType == "anyelement")
					commandText += "::int";
				else if (parameter.SchemaType != "ARRAY")
					// otherwise it will fail on overrides with same parameter count
					commandText += "::" + parameter.SchemaType;
			}

			commandText += ")";

			return commandText;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
		{
			return
				(
					from r in resultTable.AsEnumerable()

					let columnName   = r.Field<string>("ColumnName")
					let columnType   = Converter.ChangeTypeTo<string>(r["DataTypeName"])
					let dataType     = DataTypes.FirstOrDefault(t => t.TypeName == columnType)
					// AllowDBNull not set even with KeyInfo behavior suggested here:
					// https://github.com/npgsql/npgsql/issues/1693
					let isNullable   = r.IsNull("AllowDBNull")      ? true       : r.Field<bool>("AllowDBNull")
					// see https://github.com/npgsql/npgsql/issues/2243
					let length       = r.IsNull("ColumnSize")       ? (int?)null : (r.Field<int>("ColumnSize") == -1 && columnType == "character" ? 1 : r.Field<int>("ColumnSize"))
					let precision    = r.IsNull("NumericPrecision") ? (int?)null : r.Field<int>("NumericPrecision")
					let scale        = r.IsNull("NumericScale")     ? (int?)null : r.Field<int>("NumericScale")
					let providerType = r.IsNull("DataType")         ? null       : r.Field<Type>("DataType")
					let systemType   =  GetSystemType(columnType, null, dataType, length, precision, scale) ?? providerType ?? typeof(object)

					select new ColumnSchema
					{
						ColumnName           = columnName,
						ColumnType           = GetDbType(columnType, dataType, length, precision, scale),
						IsNullable           = isNullable,
						MemberName           = ToValidName(columnName),
						MemberType           = ToTypeName(systemType, isNullable),
						ProviderSpecificType = GetProviderSpecificType(columnType),
						SystemType           = systemType,
						DataType             = GetDataType(columnType, null, length, precision, scale),
					}
				).ToList();
		}
	}
}
