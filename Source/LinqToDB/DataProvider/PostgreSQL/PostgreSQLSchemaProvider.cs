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
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var list = new[]
			{
				new DataTypeInfo { TypeName = "name",                        DataType = typeof(string).        FullName },
				new DataTypeInfo { TypeName = "oid",                         DataType = typeof(int).           FullName },
				new DataTypeInfo { TypeName = "xid",                         DataType = typeof(int).           FullName },
				new DataTypeInfo { TypeName = "smallint",                    DataType = typeof(short).         FullName },
				new DataTypeInfo { TypeName = "integer",                     DataType = typeof(int).           FullName },
				new DataTypeInfo { TypeName = "int4",                        DataType = typeof(int).           FullName },
				new DataTypeInfo { TypeName = "bigint",                      DataType = typeof(long).          FullName },
				new DataTypeInfo { TypeName = "real",                        DataType = typeof(float).         FullName },
				new DataTypeInfo { TypeName = "double precision",            DataType = typeof(double).        FullName },
				new DataTypeInfo { TypeName = "boolean",                     DataType = typeof(bool).          FullName },
				new DataTypeInfo { TypeName = "regproc",                     DataType = typeof(object).        FullName },
				new DataTypeInfo { TypeName = "money",                       DataType = typeof(decimal).       FullName },
				new DataTypeInfo { TypeName = "text",                        DataType = typeof(string).        FullName },
				new DataTypeInfo { TypeName = "xml",                         DataType = typeof(string).        FullName },
				new DataTypeInfo { TypeName = "date",                        DataType = typeof(DateTime).      FullName },
				new DataTypeInfo { TypeName = "bytea",                       DataType = typeof(byte[]).        FullName },
				new DataTypeInfo { TypeName = "uuid",                        DataType = typeof(Guid).          FullName },

				new DataTypeInfo { TypeName = "hstore",                      DataType = typeof(Dictionary<string,string>).FullName},
				new DataTypeInfo { TypeName = "json",                        DataType = typeof(string).        FullName },
				new DataTypeInfo { TypeName = "jsonb",                       DataType = typeof(string).        FullName },

				new DataTypeInfo { TypeName = "character varying",           DataType = typeof(string).        FullName, CreateFormat = "character varying({0})",            CreateParameters = "length" },
				new DataTypeInfo { TypeName = "character",                   DataType = typeof(string).        FullName, CreateFormat = "character({0})",                    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "numeric",                     DataType = typeof(decimal).       FullName, CreateFormat = "numeric({0},{1})",                  CreateParameters = "precision,scale" },
				new DataTypeInfo { TypeName = "timestamp with time zone",    DataType = typeof(DateTimeOffset).FullName, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" },
				new DataTypeInfo { TypeName = "timestamp without time zone", DataType = typeof(DateTime).      FullName, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" },
			}.ToList();

			var provider = (PostgreSQLDataProvider)dataConnection.DataProvider;

			if (provider.NpgsqlInetType       != null) list.Add(new DataTypeInfo { TypeName = "inet"                       , DataType = provider.NpgsqlInetType.      FullName });
			if (provider.NpgsqlInetType       != null) list.Add(new DataTypeInfo { TypeName = "cidr"                       , DataType = provider.NpgsqlInetType.      FullName });
			if (provider.NpgsqlPointType      != null) list.Add(new DataTypeInfo { TypeName = "point"                      , DataType = provider.NpgsqlPointType.     FullName });
			if (provider.NpgsqlLineType       != null) list.Add(new DataTypeInfo { TypeName = "line"                       , DataType = provider.NpgsqlLineType.      FullName });
			if (provider.NpgsqlLSegType       != null) list.Add(new DataTypeInfo { TypeName = "lseg"                       , DataType = provider.NpgsqlLSegType.      FullName });
			if (provider.NpgsqlBoxType        != null) list.Add(new DataTypeInfo { TypeName = "box"                        , DataType = provider.NpgsqlBoxType.       FullName });
			if (provider.NpgsqlPathType       != null) list.Add(new DataTypeInfo { TypeName = "path"                       , DataType = provider.NpgsqlPathType.      FullName });
			if (provider.NpgsqlPolygonType    != null) list.Add(new DataTypeInfo { TypeName = "polygon"                    , DataType = provider.NpgsqlPolygonType.   FullName });
			if (provider.NpgsqlCircleType     != null) list.Add(new DataTypeInfo { TypeName = "circle"                     , DataType = provider.NpgsqlCircleType.    FullName });
			if (provider.NpgsqlIntervalType   != null) list.Add(new DataTypeInfo { TypeName = "interval"                   , DataType = provider.NpgsqlIntervalType.  FullName, CreateFormat = "interval({0})",                     CreateParameters = "precision" });
			if (provider.NpgsqlDateType       != null) list.Add(new DataTypeInfo { TypeName = "date"                       , DataType = provider.NpgsqlDateType.      FullName, CreateFormat = "time without time zone({0})",       CreateParameters = "precision" });
			if (provider.NpgsqlDateTimeType   != null) list.Add(new DataTypeInfo { TypeName = "timestamp without time zone", DataType = provider.NpgsqlDateTimeType.  FullName, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" });
			if (provider.NpgsqlDateTimeType   != null) list.Add(new DataTypeInfo { TypeName = "timestamp with time zone"   , DataType = provider.NpgsqlDateTimeType.  FullName, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" });

			if (provider.NpgsqlTimeType       != null) list.Add(new DataTypeInfo { TypeName = "time with time zone"        , DataType = provider.NpgsqlTimeType.      FullName, CreateFormat = "time with time zone({0})",          CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "time with time zone"        , DataType = typeof(TimeSpan).             FullName, CreateFormat = "time with time zone({0})",          CreateParameters = "precision" });
			if (provider.NpgsqlTimeTZType     != null) list.Add(new DataTypeInfo { TypeName = "time without time zone"     , DataType = provider.NpgsqlTimeTZType.    FullName, CreateFormat = "time without time zone({0})",       CreateParameters = "precision" });
			else                                       list.Add(new DataTypeInfo { TypeName = "time without time zone"     , DataType = typeof(DateTimeOffset).       FullName, CreateFormat = "time without time zone({0})",       CreateParameters = "precision" });

			list.Add(new DataTypeInfo { TypeName = "macaddr",     DataType = (provider.NpgsqlMacAddressType ?? typeof(PhysicalAddress)).FullName });
			list.Add(new DataTypeInfo { TypeName = "macaddr8",    DataType = (provider.NpgsqlMacAddressType ?? typeof(PhysicalAddress)).FullName });
			list.Add(new DataTypeInfo { TypeName = "bit",         DataType = (provider.BitStringType        ?? typeof(BitArray)).       FullName, CreateFormat = "bit({0})",         CreateParameters = "size" });
			list.Add(new DataTypeInfo { TypeName = "bit varying", DataType = (provider.BitStringType        ?? typeof(BitArray)).       FullName, CreateFormat = "bit varying({0})", CreateParameters = "size" });

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
				case "character"                   : return DataType.NChar;
				case "text"                        : return DataType.Text;
				case "smallint"                    : return DataType.Int16;
				case "oid"                         :
				case "int4"                        :
				case "integer"                     : return DataType.Int32;
				case "bigint"                      : return DataType.Int64;
				case "real"                        : return DataType.Single;
				case "double precision"            : return DataType.Double;
				case "bytea"                       : return DataType.Binary;
				case "boolean"                     : return DataType.Boolean;
				case "numeric"                     : return DataType.Decimal;
				case "money"                       : return DataType.Money;
				case "uuid"                        : return DataType.Guid;
				case "character varying"           : return DataType.NVarChar;
				case "timestamp with time zone"    : return DataType.DateTimeOffset;
				case "timestamp without time zone" : return DataType.DateTime2;
				case "time with time zone"         : return DataType.Time;
				case "time without time zone"      : return DataType.Time;
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
				case "interval"                    : return "NpgsqlTimeSpan";
				case "time without time zone"      : return "NpgsqlTime";
				case "time with time zone"         : return "NpgsqlTimeTZ";
				case "timestamp with time zone":
				case "timestamp without time zone" :
				case "date"                        : return "NpgsqlDate";
				case "point"                       : return "NpgsqlPoint";
				case "lseg"                        : return "NpgsqlLSeg";
				case "box"                         : return "NpgsqlBox";
				case "circle"                      : return "NpgsqlCircle";
				case "path"                        : return "NpgsqlPath";
				case "polygon"                     : return "NpgsqlPolygon";
				case "line"                        : return "NpgsqlLine";
				case "cidr":
				case "inet"                        : return "NpgsqlInet";
				case "geometry "                   : return "PostgisGeometry";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			// some notes:
			// - postgresql 11 adds procedures support, but it is stil in development and not tested
			// - aggregate function detection based on undocumented behavior
			// - output parameters of functions returned as record by postgresql (makes sense, as it is not procedure)
			return dataConnection
				.Query(rd =>
				{
					var catalog  = rd.GetString(0);
					var schema   = rd.GetString(1);
					var source   = Converter.ChangeTypeTo<string>(rd[4]);
					var dataType = Converter.ChangeTypeTo<string>(rd[6]);
					return new ProcedureInfo()
					{
						ProcedureID         = catalog + "." + schema + "." + rd.GetString(5),
						CatalogName         = catalog,
						SchemaName          = schema,
						ProcedureName       = rd.GetString(2),
						IsFunction          = rd.GetString(3) == "FUNCTION",
						IsTableFunction     = dataType == "record" || (dataType == "USER-DEFINED" && !rd.IsDBNull(7)),
						IsAggregateFunction = source == "aggregate_dummy",
						IsDefaultSchema     = schema == "public",
						ProcedureDefinition = source
					};
				}, @"SELECT r.ROUTINE_CATALOG, r.ROUTINE_SCHEMA, r.ROUTINE_NAME, r.ROUTINE_TYPE, r.ROUTINE_DEFINITION, r.SPECIFIC_NAME, r.DATA_TYPE, t.TABLE_TYPE
	FROM INFORMATION_SCHEMA.ROUTINES r
		LEFT OUTER JOIN INFORMATION_SCHEMA.TABLES t
			ON r.TYPE_UDT_SCHEMA = t.TABLE_SCHEMA AND r.TYPE_UDT_NAME = t.TABLE_NAME")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			// Notes:
			// - IsResult not supported by pgsql
			// - user-defined and array types support not implemented
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
						// not supported by pgsql
						Precision     = null,
						Scale         = null,
						Length        = null
					};
				}, "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.parameters")
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
					let isNullable   = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])
					let dataType     = DataTypes.SingleOrDefault(t => t.TypeName == columnType)
					let providerType = r.IsNull("ProviderType")     ? null       : r.Field<Type>("ProviderType")
					let length       = r.IsNull("ColumnSize")       ? (int?)null : r.Field<int>("ColumnSize")
					let precision    = r.IsNull("NumericPrecision") ? (int?)null : r.Field<int>("NumericPrecision")
					let scale        = r.IsNull("NumericScale")     ? (int?)null : r.Field<int>("NumericScale")

					select new ColumnSchema
					{
						ColumnName           = columnName,
						ColumnType           = GetDbType(columnType, dataType, length, precision, scale),
						IsNullable           = isNullable,
						MemberName           = ToValidName(columnName),
						MemberType           = ToTypeName(providerType, isNullable),
						ProviderSpecificType = GetProviderSpecificType(columnType),
						SystemType           = providerType ?? typeof(object),
						DataType             = GetDataType(columnType, null, length, precision, scale),
					}
				).ToList();
		}
	}
}
