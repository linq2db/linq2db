﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using SchemaProvider;
	using System.Data;
	using System.Net;
	using SqlQuery;

	public class PostgreSQLSchemaProvider : SchemaProviderBase
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
				new DataTypeInfo { TypeName = "name",                        DataType = typeof(string).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "oid",                         DataType = typeof(int).           AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "xid",                         DataType = typeof(int).           AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "smallint",                    DataType = typeof(short).         AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "int2",                        DataType = typeof(short).         AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "integer",                     DataType = typeof(int).           AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "int4",                        DataType = typeof(int).           AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "bigint",                      DataType = typeof(long).          AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "int8",                        DataType = typeof(long).          AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "real",                        DataType = typeof(float).         AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "float4",                      DataType = typeof(float).         AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "double precision",            DataType = typeof(double).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "float8",                      DataType = typeof(double).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "boolean",                     DataType = typeof(bool).          AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "bool",                        DataType = typeof(bool).          AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "regproc",                     DataType = typeof(object).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "money",                       DataType = typeof(decimal).       AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "text",                        DataType = typeof(string).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "xml",                         DataType = typeof(string).        AssemblyQualifiedName! },

				new DataTypeInfo { TypeName = "bytea",                       DataType = typeof(byte[]).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "uuid",                        DataType = typeof(Guid).          AssemblyQualifiedName! },

				new DataTypeInfo { TypeName = "hstore",                      DataType = typeof(Dictionary<string,string>).AssemblyQualifiedName!},
				new DataTypeInfo { TypeName = "json",                        DataType = typeof(string).        AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "jsonb",                       DataType = typeof(string).        AssemblyQualifiedName! },

				new DataTypeInfo { TypeName = "character varying",           DataType = typeof(string).        AssemblyQualifiedName!, CreateFormat = "character varying({0})",            CreateParameters = "length" },
				new DataTypeInfo { TypeName = "varchar",                     DataType = typeof(string).        AssemblyQualifiedName!, CreateFormat = "character varying({0})",            CreateParameters = "length" },
				new DataTypeInfo { TypeName = "character",                   DataType = typeof(string).        AssemblyQualifiedName!, CreateFormat = "character({0})",                    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "bpchar",                      DataType = typeof(string).        AssemblyQualifiedName!, CreateFormat = "character({0})",                    CreateParameters = "length" },
				new DataTypeInfo { TypeName = "numeric",                     DataType = typeof(decimal).       AssemblyQualifiedName!, CreateFormat = "numeric({0},{1})",                  CreateParameters = "precision,scale" },

				new DataTypeInfo { TypeName = "interval",                    DataType = typeof(TimeSpan).      AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "time with time zone",         DataType = typeof(DateTimeOffset).AssemblyQualifiedName! },
				new DataTypeInfo { TypeName = "time without time zone",      DataType = typeof(TimeSpan).      AssemblyQualifiedName! },

				new DataTypeInfo { TypeName = "timestamptz",                 DataType = typeof(DateTimeOffset).AssemblyQualifiedName!, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" },
				new DataTypeInfo { TypeName = "timestamp with time zone",    DataType = typeof(DateTimeOffset).AssemblyQualifiedName!, CreateFormat = "timestamp ({0}) with time zone",    CreateParameters = "precision" },

				new DataTypeInfo { TypeName = "timestamp",                   DataType = typeof(DateTime).      AssemblyQualifiedName!, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" },
				new DataTypeInfo { TypeName = "timestamp without time zone", DataType = typeof(DateTime).      AssemblyQualifiedName!, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" },
			}.ToList();

			var provider = (PostgreSQLDataProvider)dataConnection.DataProvider;

			list.Add(new DataTypeInfo { TypeName = "inet"                       , DataType = provider.Adapter.NpgsqlInetType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "cidr"                       , DataType = provider.Adapter.NpgsqlInetType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "point"                      , DataType = provider.Adapter.NpgsqlPointType.   AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "line"                       , DataType = provider.Adapter.NpgsqlLineType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "lseg"                       , DataType = provider.Adapter.NpgsqlLSegType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "box"                        , DataType = provider.Adapter.NpgsqlBoxType.     AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "path"                       , DataType = provider.Adapter.NpgsqlPathType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "polygon"                    , DataType = provider.Adapter.NpgsqlPolygonType. AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "circle"                     , DataType = provider.Adapter.NpgsqlCircleType.  AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "date"                       , DataType = provider.Adapter.NpgsqlDateType.    AssemblyQualifiedName!, ProviderSpecific = true });
			list.Add(new DataTypeInfo { TypeName = "interval"                   , DataType = provider.Adapter.NpgsqlTimeSpanType.AssemblyQualifiedName!, ProviderSpecific = true, CreateFormat = "interval({0})"                    , CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "timestamptz"                , DataType = provider.Adapter.NpgsqlDateTimeType.AssemblyQualifiedName!, ProviderSpecific = true, CreateFormat = "timestamp ({0}) with time zone"   , CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "timestamp with time zone"   , DataType = provider.Adapter.NpgsqlDateTimeType.AssemblyQualifiedName!, ProviderSpecific = true, CreateFormat = "timestamp ({0}) with time zone"   , CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "timestamp"                  , DataType = provider.Adapter.NpgsqlDateTimeType.AssemblyQualifiedName!, ProviderSpecific = true, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "timestamp without time zone", DataType = provider.Adapter.NpgsqlDateTimeType.AssemblyQualifiedName!, ProviderSpecific = true, CreateFormat = "timestamp ({0}) without time zone", CreateParameters = "precision" });


			list.Add(new DataTypeInfo { TypeName = "inet"                   , DataType = typeof(IPAddress).      AssemblyQualifiedName!       });
			list.Add(new DataTypeInfo { TypeName = "cidr"                   , DataType = "System.ValueTuple`2[[System.Net.IPAddress, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" });
			list.Add(new DataTypeInfo { TypeName = "date"                   , DataType = typeof(DateTime).       AssemblyQualifiedName! });
			list.Add(new DataTypeInfo { TypeName = "timetz"                 , DataType = typeof(DateTimeOffset). AssemblyQualifiedName!, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "time without time zone" , DataType = typeof(TimeSpan).       AssemblyQualifiedName!, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "time"                   , DataType = typeof(TimeSpan).       AssemblyQualifiedName!, CreateFormat = "time ({0}) without time zone",      CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "time with time zone"    , DataType = typeof(DateTimeOffset). AssemblyQualifiedName!, CreateFormat = "time ({0}) with time zone",         CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "interval"               , DataType = typeof(TimeSpan).       AssemblyQualifiedName!, CreateFormat = "interval({0})",                     CreateParameters = "precision" });
			list.Add(new DataTypeInfo { TypeName = "macaddr"                , DataType = typeof(PhysicalAddress).AssemblyQualifiedName! });
			list.Add(new DataTypeInfo { TypeName = "macaddr8"               , DataType = typeof(PhysicalAddress).AssemblyQualifiedName! });
			list.Add(new DataTypeInfo { TypeName = "bit"                    , DataType = typeof(BitArray).       AssemblyQualifiedName!, CreateFormat = "bit({0})",                          CreateParameters = "size" });
			list.Add(new DataTypeInfo { TypeName = "bit varying"            , DataType = typeof(BitArray).       AssemblyQualifiedName!, CreateFormat = "bit varying({0})",                  CreateParameters = "size" });
			list.Add(new DataTypeInfo { TypeName = "varbit"                 , DataType = typeof(BitArray).       AssemblyQualifiedName!, CreateFormat = "bit varying({0})",                  CreateParameters = "size" });

			return list;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var defaultSchema = ToDatabaseLiteral(dataConnection, options?.DefaultSchema ?? "public");
			
			var sql = $@"
				SELECT
					t.table_catalog || '.' || t.table_schema || '.' || t.table_name            as TableID,
					t.table_catalog                                                            as CatalogName,
					t.table_schema                                                             as SchemaName,
					t.table_name                                                               as TableName,
					t.table_schema = {defaultSchema}                                           as IsDefaultSchema,
					t.table_type = 'VIEW'                                                      as IsView,
					(
						SELECT pgd.description
						FROM
							pg_catalog.pg_statio_all_tables as st
							JOIN pg_catalog.pg_description pgd ON pgd.objoid = st.relid
						WHERE t.table_schema = st.schemaname AND t.table_name=st.relname
						LIMIT 1
					)                                                                          as Description,
					left(t.table_schema, 3) = 'pg_' OR t.table_schema = 'information_schema'   as IsProviderSpecific
				FROM
					information_schema.tables t
				WHERE {GenerateSchemaFilter(dataConnection, "table_schema")}";

			// materialized views supported starting from pgsql 9.3
			var version = dataConnection.Query<int>("SHOW  server_version_num").Single();
			if (version >= 90300)
			{
				// materialized views are not exposed to information_schema
				sql += $@"
			UNION ALL
				SELECT
					current_database() || '.' || v.schemaname || '.' || v.matviewname          as TableID,
					current_database()                                                         as CatalogName,
					v.schemaname                                                               as SchemaName,
					v.matviewname                                                              as TableName,
					v.schemaname = {defaultSchema}                                             as IsDefaultSchema,
					true                                                                       as IsView,
					(
						SELECT pgd.description
							FROM pg_catalog.pg_class
								INNER JOIN pg_catalog.pg_namespace       ON pg_class.relnamespace = pg_namespace.oid
								INNER JOIN pg_catalog.pg_description pgd ON pgd.objoid = pg_class.oid
						WHERE pg_class.relkind = 'm' AND pgd.objsubid = 0 AND pg_namespace.nspname = v.schemaname AND pg_class.relname = v.matviewname
						LIMIT 1
					)                                                                          as Description,
					false                                                                      as IsProviderSpecific
				FROM pg_matviews v
				WHERE {GenerateSchemaFilter(dataConnection, "v.schemaname")}";
			}

			return dataConnection.Query<TableInfo>(sql).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return
				dataConnection.Query<PrimaryKeyInfo>($@"
					SELECT
						current_database() || '.' || pg_namespace.nspname || '.' || pg_class.relname as TableID,
						pg_constraint.conname                                                        as PrimaryKeyName,
						attname                                                                      as ColumnName,
						attnum                                                                       as Ordinal
					FROM
						pg_attribute
							JOIN pg_constraint ON pg_attribute.attrelid = pg_constraint.conrelid AND pg_attribute.attnum = ANY(pg_constraint.conkey)
							JOIN pg_class      ON pg_class.oid = pg_constraint.conrelid
							JOIN pg_namespace  ON pg_class.relnamespace = pg_namespace.oid
					WHERE
						pg_constraint.contype = 'p'
						AND {GenerateSchemaFilter(dataConnection, "pg_namespace.nspname")}")
				.ToList();
		}

		static string ToDatabaseLiteral(DataConnection dataConnection, string? str)
		{
			var sb = new StringBuilder();
			dataConnection.MappingSchema.ValueToSqlConverter.Convert(sb, SqlDataType.DbText, str);
			return sb.ToString();
		}

		string GenerateSchemaFilter(DataConnection dataConnection, string schemaColumnName)
		{
			var excludeSchemas =
				new HashSet<string?>(
					ExcludedSchemas.Where(s => !s.IsNullOrEmpty()).Union(new[] { "pg_catalog", "information_schema" }),
					StringComparer.OrdinalIgnoreCase);
			
			var includeSchemas = new HashSet<string?>(IncludedSchemas.Where(s => !s.IsNullOrEmpty()), StringComparer.OrdinalIgnoreCase);
			
			if (includeSchemas.Count > 0)
			{
				foreach (var toInclude in IncludedSchemas)
		{
					excludeSchemas.Remove(toInclude);
				}
			}
			
			if (excludeSchemas.Count == 0 && IncludedSchemas.Count == 0)
				return "1 = 1";
			
			var schemaFilter = "";

			if (excludeSchemas.Count > 0)
			{
				var schemasToExcludeStr =
					string.Join(", ", excludeSchemas.Select(s => ToDatabaseLiteral(dataConnection, s)));
				schemaFilter = $@"{schemaColumnName} NOT IN ({schemasToExcludeStr})";
			}

			if (includeSchemas.Count > 0)
			{
				var schemasToIncludeStr =
					string.Join(", ", includeSchemas.Select(s => ToDatabaseLiteral(dataConnection, s)));
				if (!schemaFilter.IsNullOrEmpty())
					schemaFilter += " AND ";
				schemaFilter += $@"{schemaColumnName} IN ({schemasToIncludeStr})";
			}

			return schemaFilter;
			}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var version = dataConnection.Query<int>("SHOW  server_version_num").Single();

			var isIdentityExpr = "false";
			if (version >= 100000)
				isIdentityExpr = "attr.attidentity IN ('a', 'd')";

			var sql = $@"
				SELECT columns.TableID,
				       columns.Name,
				       columns.IsNullable,
				       columns.Ordinal,
				       columns.DataType,
				       columns.ArrayDimensions,
				       columns.Length,
				       columns.Precision,
				       columns.Scale,
				       columns.IsIdentity OR COALESCE(columns.DefaultValue ~* 'nextval', false) AS IsIdentity,
				       columns.SkipOnInsert,
				       columns.SkipOnUpdate,
				       columns.Description
				FROM (
				         SELECT current_database() || '.' || ns.nspname || '.' || cls.relname                            AS TableID,
				                attr.attname                                                                             AS Name,
				                NOT (attr.attnotnull OR typ.typtype = 'd'::""char"" AND typ.typnotnull)                    AS IsNullable,
				                attr.attnum                                                                              AS Ordinal,
				                CASE
				                    WHEN typ.typtype = 'd'::""char"" THEN
				                        CASE
				                            WHEN nbt.nspname = 'pg_catalog'::name THEN format_type(typ.typbasetype, attr.atttypmod)
				                            ELSE 'USER-DEFINED'::text
				                            END
				                    ELSE
				                        CASE
				                            WHEN nt.nspname = 'pg_catalog'::name THEN format_type(attr.atttypid, attr.atttypmod)
				                            ELSE 'USER-DEFINED'::text
				                            END
				                    END                                                                                  AS DataType,
				                attr.attndims                                                                            AS ArrayDimensions,
				                information_schema._pg_char_max_length(information_schema._pg_truetypid(attr.*, typ.*),
				                                                       information_schema._pg_truetypmod(attr.*, typ.*)) AS Length,
				                COALESCE(information_schema._pg_numeric_precision(
				                                 information_schema._pg_truetypid(attr.*, typ.*),
				                                 information_schema._pg_truetypmod(attr.*, typ.*)),
				                         information_schema._pg_datetime_precision(
				                                 information_schema._pg_truetypid(attr.*, typ.*),
				                                 information_schema._pg_truetypmod(attr.*, typ.*))
				                    )                                                                                    AS Precision,
				                information_schema._pg_numeric_scale(attr.atttypid, attr.atttypmod)                      AS Scale,
				                {isIdentityExpr}                                                                         AS IsIdentity,
				                cls.relkind IN ('v', 'm')                                                                AS SkipOnInsert,
				                NOT (cls.relkind = 'r'::""char"" OR cls.relkind = 'v'::""char""
				                    AND (EXISTS(SELECT 1
				                                FROM pg_rewrite
				                                WHERE pg_rewrite.ev_class = cls.oid
				                                  AND pg_rewrite.ev_type = '2'::""char""
				                                  AND pg_rewrite.is_instead))
				                    AND
				                                                  (EXISTS(SELECT 1
				                                                          FROM pg_rewrite
				                                                          WHERE pg_rewrite.ev_class = cls.oid
				                                                            AND pg_rewrite.ev_type = '4'::""char""
				                                                            AND pg_rewrite.is_instead))
				                    )                                                                                    AS SkipOnUpdate,
				                des.description                                                                          AS Description,
				                CASE
				                    WHEN atthasdef THEN (SELECT pg_get_expr(adbin, cls.oid)
				                                         FROM pg_attrdef
				                                         WHERE adrelid = cls.oid
				                                           AND adnum = attr.attnum)
				                    END                                                                                  AS DefaultValue

				         FROM pg_catalog.pg_class cls
				                  JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
				                  LEFT JOIN pg_attribute AS attr ON attr.attrelid = cls.oid
				                  LEFT JOIN pg_type AS typ ON attr.atttypid = typ.oid
				                  LEFT JOIN pg_proc ON pg_proc.oid = typ.typreceive
				                  LEFT JOIN pg_description AS des ON des.objoid = cls.oid AND des.objsubid = attr.attnum
				                  LEFT JOIN pg_collation AS coll ON coll.oid = attr.attcollation
				                  JOIN pg_namespace nt ON typ.typnamespace = nt.oid
				                  LEFT JOIN (pg_type bt
				                 JOIN pg_namespace nbt ON bt.typnamespace = nbt.oid)
				                            ON typ.typtype = 'd'::""char"" AND typ.typbasetype = bt.oid
				         WHERE cls.relkind IN ('r', 'v', 'm')
				           AND attr.attnum > 0
				           AND NOT attr.attisdropped
				           AND {GenerateSchemaFilter(dataConnection, "ns.nspname")}
				     ) columns;";

			var result = dataConnection
					.Query(rd =>
					{
						var dataType = rd.GetString(4);
						// null - not array
						// 0 - array with unknown dimensions (unknown for views)
						// >0 - array with specified dimensions (known for tables)
						var arrayDimensions = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
						if (arrayDimensions != null)
						{
							if (arrayDimensions > 0)
							{
								// first brackets already there
								--arrayDimensions;
								while (arrayDimensions > 0)
								{
									dataType += "[]";
									arrayDimensions--;
								}
							}
						}

						return new ColumnInfo()
						{
							TableID      = rd.GetString(0),
							Name         = rd.GetString(1),
							IsNullable   = rd.GetBoolean(2),
							Ordinal      = rd.GetInt32(3),
							DataType     = dataType,
							Length       = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6),
							Precision    = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
							Scale        = rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8),
							IsIdentity   = rd.GetBoolean(9),
							SkipOnInsert = rd.GetBoolean(10),
							SkipOnUpdate = rd.GetBoolean(11),
							Description  = rd.IsDBNull(12) ? null : rd.GetString(12),
						};
					}, sql)
					.ToList();

			return result;
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var data = dataConnection.Query(
				rd => new
				{
					name         = rd[0],
					thisTable    = rd[1],
					otherTable   = rd[2],
					thisColumns  = new[] { rd[ 3], rd[ 4], rd[ 5], rd[ 6], rd[ 7], rd[ 8], rd[ 9], rd[10], rd[11], rd[12], rd[13], rd[14], rd[15], rd[16], rd[17], rd[18] },
					otherColumns = new[] { rd[19], rd[20], rd[21], rd[22], rd[23], rd[24], rd[25], rd[26], rd[27], rd[28], rd[29], rd[30], rd[31], rd[32], rd[33], rd[34] },
				}, $@"
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
					pg_constraint.contype = 'f'
					AND {GenerateSchemaFilter(dataConnection, "this_schema.nspname")}")
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
					ThisColumn   = Convert.ToString(col.thisColumn)!,
					OtherColumn  = Convert.ToString(col.otherColumn)!,
					Ordinal      = col.ordinal
				}
			).ToList();
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			if (dataType == null)
				return DataType.Undefined;
			dataType = SimplifyDataType(dataType);
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
				case "time without time zone"      : return DataType.Time;
				case "interval"                    : return DataType.Interval;
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
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType)
			{
				case "timestamp"                   :
				case "timestamptz"                 :
				case "timestamp with time zone"    :
				case "timestamp without time zone" : return _provider.Adapter.NpgsqlDateTimeType.Name;
				case "date"                        : return _provider.Adapter.NpgsqlDateType    .Name;
				case "point"                       : return _provider.Adapter.NpgsqlPointType   .Name;
				case "lseg"                        : return _provider.Adapter.NpgsqlLSegType    .Name;
				case "box"                         : return _provider.Adapter.NpgsqlBoxType     .Name;
				case "circle"                      : return _provider.Adapter.NpgsqlCircleType  .Name;
				case "path"                        : return _provider.Adapter.NpgsqlPathType    .Name;
				case "polygon"                     : return _provider.Adapter.NpgsqlPolygonType .Name;
				case "line"                        : return _provider.Adapter.NpgsqlLineType    .Name;
				case "cidr"                        :
				case "inet"                        : return _provider.Adapter.NpgsqlInetType    .Name;
				case "geometry"                    : return "PostgisGeometry";
			}

			return base.GetProviderSpecificType(dataType);
		}

		static Regex _matchArray = new Regex(@"^(.*)(\[\]){1}$", RegexOptions.Compiled);
		static Regex _matchType  = new Regex(@"^(.*)(\(\d+(,\s*\d+)?\))(.*){1}$", RegexOptions.Compiled);

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo,
			long? length, int? precision, int? scale, GetSchemaOptions options)
		{
			var foundType = base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
			if (foundType != null)
				return foundType;

			if (dataType != null)
			{
				dataType = dataType.Trim();
				var match = _matchArray.Match(dataType);
				if (match.Success)
				{
					var elementType = GetSystemType(match.Groups[1].Value, null, GetDataType(match.Groups[1].Value, options), null, null, null, options);
					if (elementType != null)
					{
						foundType = elementType.MakeArrayType();
					}
				}
				else
				{
					var simplified = SimplifyDataType(dataType);
					if (simplified != dataType)
					{
						foundType = GetSystemType(simplified, null, GetDataType(simplified, options), null, null, null, options);
						if (foundType != null)
							return foundType;
					}
				}
			}
						
			return foundType;
		}

		protected override DataTypeInfo? GetDataType(string? typeName, GetSchemaOptions options)
		{
			if (typeName == null)
				return null;

			var typInfo = base.GetDataType(typeName, options);
			if (typInfo == null)
			{
				var simplified = SimplifyDataType(typeName);
				if (simplified != typeName)
					typInfo = base.GetDataType(simplified, options);
			}

			return typInfo;
				}
		
		static string SimplifyDataType(string dataType)
		{
			var typeMatch = _matchType.Match(dataType);
			if (typeMatch.Success)
			{
				// ignore generated length, precision, scale
				dataType = typeMatch.Groups[1].Value.Trim();
				var suffix = typeMatch.Groups[4].Value?.Trim();
				if (!suffix.IsNullOrEmpty())
					dataType = dataType + " " + suffix;
			}

			return dataType;
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
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
							IsDefaultSchema     = schema == (options.DefaultSchema ?? "public"),
							ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[4]),
							// result of function has dynamic form and vary per call if function return type is 'record'
							// only exception is function with out/inout parameters, where we know that record contains those parameters
							IsResultDynamic     = Converter.ChangeTypeTo<string>(rd[8]) == "record" && Converter.ChangeTypeTo<int>(rd[9]) == 0
						};
					}, $@"
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
			ON r.SPECIFIC_SCHEMA = outp.SPECIFIC_SCHEMA AND r.SPECIFIC_NAME = outp.SPECIFIC_NAME
		WHERE {GenerateSchemaFilter(dataConnection, "n.nspname")}")
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
							IsDefaultSchema     = schema == (options.DefaultSchema ?? "public"),
							ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[3]),
							IsResultDynamic     = Converter.ChangeTypeTo<string>(rd[7]) == "record" && Converter.ChangeTypeTo<int>(rd[8]) == 0
						};
					}, $@"
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
			ON r.SPECIFIC_SCHEMA = outp.SPECIFIC_SCHEMA AND r.SPECIFIC_NAME = outp.SPECIFIC_NAME
		WHERE {GenerateSchemaFilter(dataConnection, "n.nspname")}")
					.ToList();
			}
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
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
						Length        = null,
						IsNullable    = true
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
					Length        = null,
					IsNullable    = true
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
				if (parameter.SchemaType == "USER-DEFINED")
				{ }
				else if (parameter.SchemaType == "anyarray")
					commandText += "::int[]";
				else if (parameter.SchemaType == "anyelement")
					commandText += "::int";
				else if (parameter.SchemaType != "ARRAY")
					// otherwise it will fail on overrides with same parameter count
					commandText += "::" + parameter.SchemaType;
			}

			commandText += ")";

			return commandText;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
				(
					from r in resultTable.AsEnumerable()

					let columnName   = r.Field<string>("ColumnName")
					let columnType   = Converter.ChangeTypeTo<string>(r["DataTypeName"])
					let dataType     = GetDataType(columnType, options)
					// AllowDBNull not set even with KeyInfo behavior suggested here:
					// https://github.com/npgsql/npgsql/issues/1693
					let isNullable   = r.IsNull("AllowDBNull")      ? true       : r.Field<bool>("AllowDBNull")
					// see https://github.com/npgsql/npgsql/issues/2243
					let length       = r.IsNull("ColumnSize")       ? (int?)null : (r.Field<int>("ColumnSize") == -1 && columnType == "character" ? 1 : r.Field<int>("ColumnSize"))
					let precision    = r.IsNull("NumericPrecision") ? (int?)null : r.Field<int>("NumericPrecision")
					let scale        = r.IsNull("NumericScale")     ? (int?)null : r.Field<int>("NumericScale")
					let providerType = r.IsNull("DataType")         ? null       : r.Field<Type>("DataType")
					let systemType   =  GetSystemType(columnType, null, dataType, length, precision, scale, options) ?? providerType ?? typeof(object)

					select new ColumnSchema
					{
						ColumnName           = columnName,
						ColumnType           = GetDbType(options, columnType, dataType, length, precision, scale, null, null, null),
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
