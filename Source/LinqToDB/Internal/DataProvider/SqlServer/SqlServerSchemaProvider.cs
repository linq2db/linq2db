using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerSchemaProvider : SchemaProviderBase
	{
		private bool IsAzure;
		private int  CompatibilityLevel;

		private readonly SqlServerDataProvider Provider;

		public SqlServerSchemaProvider(SqlServerDataProvider provider)
		{
			Provider = provider;
		}

		protected override void InitProvider(DataConnection dataConnection, GetSchemaOptions options)
		{
			var version = dataConnection.Execute<string>("select @@version");

			IsAzure            = version.Contains("Azure", StringComparison.Ordinal);
			CompatibilityLevel = dataConnection.Execute<int>("SELECT compatibility_level FROM sys.databases WHERE name = db_name()");
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var withTemporal        = CompatibilityLevel >= 130;
			var temporalFilterStart = !withTemporal || !options.IgnoreSystemHistoryTables ? string.Empty : "(";
			var temporalFilterEnd   = !withTemporal || !options.IgnoreSystemHistoryTables ? string.Empty : @"
					) AND t.temporal_type <> 1
";

			return dataConnection.Query<TableInfo>(
					IsAzure
					? 
						$$"""

						SELECT
							TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
							TABLE_CATALOG                                                                  as CatalogName,
							TABLE_SCHEMA                                                                   as SchemaName,
							TABLE_NAME                                                                     as TableName,
							CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END                                as IsView,
							''                                                                             as Description,
							CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END                               as IsDefaultSchema
						FROM
							INFORMATION_SCHEMA.TABLES s
							LEFT JOIN
								sys.tables t
							ON
								OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id
						WHERE
							{{temporalFilterStart}}t.object_id IS NULL OR t.is_ms_shipped <> 1{{temporalFilterEnd}}
						"""
					:
						$$"""

						SELECT
							TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
							TABLE_CATALOG                                                                  as CatalogName,
							TABLE_SCHEMA                                                                   as SchemaName,
							TABLE_NAME                                                                     as TableName,
							CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END                                as IsView,
							ISNULL(CONVERT(varchar(8000), x.value), '')                                    as Description,
							CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END                               as IsDefaultSchema
						FROM
							INFORMATION_SCHEMA.TABLES s
							LEFT JOIN
								sys.tables t
							ON
								OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id
							LEFT JOIN
								sys.extended_properties x
							ON
								OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
								x.minor_id = 0 AND
								x.name = 'MS_Description'
						WHERE
							{{temporalFilterStart}}t.object_id IS NULL OR
							t.is_ms_shipped <> 1 AND
							(
								SELECT
									major_id
								FROM
									sys.extended_properties
								WHERE
									major_id = t.object_id AND
									minor_id = 0           AND
									class    = 1           AND
									name     = N'microsoft_database_tools_support'
							) IS NULL{{temporalFilterEnd}}
						"""
				)
				.ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query<PrimaryKeyInfo>(
					"""
					SELECT
						k.TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + k.TABLE_SCHEMA + '.' + k.TABLE_NAME as TableID,
						k.CONSTRAINT_NAME                                                                    as PrimaryKeyName,
						k.COLUMN_NAME                                                                        as ColumnName,
						k.ORDINAL_POSITION                                                                   as Ordinal
					FROM
						INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
						JOIN
							INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
						ON
							k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
							k.CONSTRAINT_SCHEMA  = c.CONSTRAINT_SCHEMA AND
							k.CONSTRAINT_NAME    = c.CONSTRAINT_NAME
					WHERE
						c.CONSTRAINT_TYPE='PRIMARY KEY'
					"""
				)
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var withTemporal = CompatibilityLevel >= 130;

			// column is from/to field (GeneratedAlwaysType)
			// or belongs to SYSTEM_VERSIONED_TEMPORAL_TABLE
			var temporalClause = !withTemporal ? string.Empty : @"
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'GeneratedAlwaysType') <> 0
						OR t.temporal_type = 1
";
			var temporalJoin = !withTemporal ? string.Empty : @"
					LEFT JOIN sys.tables t ON OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id";

			return dataConnection.Query<ColumnInfo>(
				IsAzure ? @"
				SELECT
					TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME                      as TableID,
					COLUMN_NAME                                                                                         as Name,
					CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END                                                     as IsNullable,
					ORDINAL_POSITION                                                                                    as Ordinal,
					c.DATA_TYPE                                                                                         as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                            as Length,
					ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)                                                       as [Precision],
					NUMERIC_SCALE                                                                                       as Scale,
					''                                                                                                  as [Description],
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c" + temporalJoin
				: @"
				SELECT
					TABLE_CATALOG COLLATE DATABASE_DEFAULT + '.' + TABLE_SCHEMA + '.' + TABLE_NAME                      as TableID,
					COLUMN_NAME                                                                                         as Name,
					CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END                                                     as IsNullable,
					ORDINAL_POSITION                                                                                    as Ordinal,
					c.DATA_TYPE                                                                                         as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                            as Length,
					ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)                                                       as [Precision],
					NUMERIC_SCALE                                                                                       as Scale,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                                         as [Description],
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp'
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1" + temporalClause + @"
						THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c
					LEFT JOIN
						sys.extended_properties x
					ON
						--OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						COLUMNPROPERTY(OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'ColumnID') = x.minor_id AND
						x.name = 'MS_Description' AND x.class = 1" + temporalJoin)
				.Select(c =>
				{
					var dti = GetDataType(c.DataType, null, options);

					if (dti != null)
					{
						switch (dti.CreateParameters)
						{
							case null :
								c.Length    = null;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "scale" :
								c.Length = null;

								if (c.Scale.HasValue)
									c.Precision = null;

								break;

							case "precision,scale" :
								c.Length = null;
								break;

							case "max length" :
								if (c.Length < 0)
									c.Length = int.MaxValue;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "length"     :
								c.Precision = null;
								c.Scale     = null;
								break;

							case "number of bits used to store the mantissa":
								break;

							default :
								break;
						}
					}

					switch (c.DataType)
					{
						case "geometry"    :
						case "geography"   :
						case "hierarchyid" :
						case "float"       :
							c.Length    = null;
							c.Precision = null;
							c.Scale     = null;
							break;
						case "vector"      :
							// Convert binary vector storage size (8-byte header + 4 bytes per float element) to logical dimension count
							c.Length = (c.Length - 8) / 4;
							break;
					}

					return c;
				})
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query<ForeignKeyInfo>(@"
				SELECT
					fk.name                                                     as Name,
					DB_NAME() + '.' + SCHEMA_NAME(po.schema_id) + '.' + po.name as ThisTableID,
					pc.name                                                     as ThisColumn,
					DB_NAME() + '.' + SCHEMA_NAME(fo.schema_id) + '.' + fo.name as OtherTableID,
					fc.name                                                     as OtherColumn,
					fkc.constraint_column_id                                    as Ordinal
				FROM sys.foreign_keys fk
					inner join sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
					inner join sys.columns             pc  ON fkc.parent_column_id = pc.column_id and fkc.parent_object_id = pc.object_id
					inner join sys.objects             po  ON fk.parent_object_id = po.object_id
					inner join sys.columns             fc  ON fkc.referenced_column_id = fc.column_id and fkc.referenced_object_id = fc.object_id
					inner join sys.objects             fo  ON fk.referenced_object_id = fo.object_id
				ORDER BY
					ThisTableID,
					Ordinal")
				.ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query<ProcedureInfo>(
				@"SELECT
					SPECIFIC_CATALOG COLLATE DATABASE_DEFAULT + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME as ProcedureID,
					SPECIFIC_CATALOG                                                                        as CatalogName,
					SPECIFIC_SCHEMA                                                                         as SchemaName,
					SPECIFIC_NAME                                                                           as ProcedureName,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION'                         THEN 1 ELSE 0 END           as IsFunction,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION' AND DATA_TYPE = 'TABLE' THEN 1 ELSE 0 END           as IsTableFunction,
					CASE WHEN EXISTS(SELECT * FROM sys.objects where name = SPECIFIC_NAME AND type='AF')
					                                                            THEN 1 ELSE 0 END           as IsAggregateFunction,
					CASE WHEN SPECIFIC_SCHEMA = 'dbo'                           THEN 1 ELSE 0 END           as IsDefaultSchema,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                             as Description
				FROM
					INFORMATION_SCHEMA.ROUTINES
					LEFT JOIN sys.extended_properties x
						ON OBJECT_ID('[' + SPECIFIC_SCHEMA + '].[' + SPECIFIC_NAME + ']') = x.major_id AND
							x.name = 'MS_Description' AND x.class = 1
				ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			// TODO: RECHECK:
			// SQL25 CTP2.1 returns vector parameter type as varbinary(len), e.g. for vector(3) : varbinary(20)
			// and sp_describe_first_result_set returns ntext
			return dataConnection.Query<ProcedureParameterInfo>(
				@"SELECT
					SPECIFIC_CATALOG COLLATE DATABASE_DEFAULT + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME as ProcedureID,
					ORDINAL_POSITION                                                                        as Ordinal,
					PARAMETER_MODE                                                                          as Mode,
					PARAMETER_NAME                                                                          as ParameterName,
					DATA_TYPE                                                                               as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                as Length,
					NUMERIC_PRECISION                                                                       as [Precision],
					NUMERIC_SCALE                                                                           as Scale,
					CASE WHEN PARAMETER_MODE = 'IN'  OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsIn,
					CASE WHEN PARAMETER_MODE = 'OUT' OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsOut,
					CASE WHEN IS_RESULT      = 'YES'                             THEN 1 ELSE 0 END          as IsResult,
					USER_DEFINED_TYPE_CATALOG                                                               as UDTCatalog,
					USER_DEFINED_TYPE_SCHEMA                                                                as UDTSchema,
					USER_DEFINED_TYPE_NAME                                                                  as UDTName,
					1                                                                                       as IsNullable,
					ISNULL(CONVERT(varchar(8000), x.value), '')                                             as Description
				FROM
					INFORMATION_SCHEMA.PARAMETERS
					LEFT JOIN sys.extended_properties x
						ON OBJECT_ID('[' + SPECIFIC_SCHEMA + '].[' + SPECIFIC_NAME + ']') = x.major_id AND
							ORDINAL_POSITION = x.minor_id AND
							x.name = 'MS_Description' AND x.class = 2")
				.ToList();
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType switch
			{
				"json"             => DataType.Json,
				"vector"           => DataType.Vector32,
				"image"            => DataType.Image,
				"text"             => DataType.Text,
				"binary"           => DataType.Binary,
				"tinyint"          => DataType.Byte,
				"date"             => DataType.Date,
				"time"             => DataType.Time,
				"bit"              => DataType.Boolean,
				"smallint"         => DataType.Int16,
				"decimal"          => DataType.Decimal,
				"int"              => DataType.Int32,
				"smalldatetime"    => DataType.SmallDateTime,
				"real"             => DataType.Single,
				"money"            => DataType.Money,
				"datetime"         => DataType.DateTime,
				"float"            => DataType.Double,
				"numeric"          => DataType.Decimal,
				"smallmoney"       => DataType.SmallMoney,
				"datetime2"        => DataType.DateTime2,
				"bigint"           => DataType.Int64,
				"varbinary"        => DataType.VarBinary,
				"timestamp"        => DataType.Timestamp,
				"sysname"          => DataType.NVarChar,
				"nvarchar"         => DataType.NVarChar,
				"varchar"          => DataType.VarChar,
				"ntext"            => DataType.NText,
				"uniqueidentifier" => DataType.Guid,
				"datetimeoffset"   => DataType.DateTimeOffset,
				"sql_variant"      => DataType.Variant,
				"xml"              => DataType.Xml,
				"char"             => DataType.Char,
				"nchar"            => DataType.NChar,
				"hierarchyid"      or
				"geography"        or
				"geometry"         => DataType.Udt,
				"table type"       => DataType.Structured,
				_                  => DataType.Undefined,
			};
			}

		// TODO: we should support multiple namespaces, as e.g. sql server also could have
		// spatial types (which is handled by T4 template for now)
		protected override string GetProviderSpecificTypeNamespace() => SqlTypes.TypesNamespace;

		protected override string? GetProviderSpecificType(string? dataType)
		{
			return dataType switch
			{
				"varbinary"        or
				"timestamp"        or
				"rowversion"       or
				"image"            or
				"binary"           => nameof(SqlBinary),
				"tinyint"          => nameof(SqlByte),
				"date"             or
				"smalldatetime"    or
				"datetime"         or
				"datetime2"        => nameof(SqlDateTime),
				"bit"              => nameof(SqlBoolean),
				"smallint"         => nameof(SqlInt16),
				"numeric"          or
				"decimal"          => nameof(SqlDecimal),
				"int"              => nameof(SqlInt32),
				"real"             => nameof(SqlSingle),
				"float"            => nameof(SqlDouble),
				"smallmoney"       or
				"money"            => nameof(SqlMoney),
				"bigint"           => nameof(SqlInt64),
				"text"             or
				"nvarchar"         or
				"char"             or
				"nchar"            or
				"varchar"          or
				"ntext"            => nameof(SqlString),
				"uniqueidentifier" => nameof(SqlGuid),
				"xml"              => nameof(SqlXml),
				"hierarchyid"      => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlHierarchyIdType}",
				"geography"        => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeographyType}",
				"geometry"         => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeometryType}",
				"json"             => $"{SqlServerProviderAdapter.TypesNamespace}.SqlJson",
				"vector"           => $"{SqlServerProviderAdapter.TypesNamespace}.SqlVector<float>",
				_                  => base.GetProviderSpecificType(dataType),
			};
			}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType switch
			{
				"json"        => (options.PreferProviderSpecificTypes ? Provider.Adapter.SqlJsonType   : null) ?? typeof(string),
				"vector"      => (options.PreferProviderSpecificTypes ? Provider.Adapter.SqlVectorType : null) ?? typeof(float[]),
				"tinyint"     => typeof(byte),
				"hierarchyid" or
				"geography"   or
				"geometry"    => Provider.GetUdtTypeByName(dataType),
				"table type"  => typeof(DataTable),
				_             => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			// database name for udt not supported by sql server
			if (udtName != null)
				return (udtSchema != null ? SqlServerTools.QuoteIdentifier(udtSchema) + '.' : null) + SqlServerTools.QuoteIdentifier(udtName);

			return base.GetDbType(options, columnType, dataType, length, precision, scale, udtCatalog, udtSchema, udtName);
		}

		protected override DataParameter BuildProcedureParameter(ParameterSchema p)
		{
			return p.DataType switch
			{
				DataType.Structured => new DataParameter
				{
					Name = p.ParameterName,
					DataType = p.DataType,
					Direction =
						p.IsIn ?
							p.IsOut ?
								ParameterDirection.InputOutput :
								ParameterDirection.Input :
							ParameterDirection.Output,
					DbType = p.SchemaType,
				},

				_ => base.BuildProcedureParameter(p),
			};
		}

		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			var sql = base.BuildTableFunctionLoadTableSchemaCommand(procedure, commandText);

			// TODO: refactor method to use query as parameter instead of manual escaping...
			// https://github.com/linq2db/linq2db/issues/1921
			if (CompatibilityLevel >= 140)
				sql = $"EXEC('{sql.Replace("'", "''", StringComparison.Ordinal)}')";

			return sql;
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			switch (dataConnection.DataProvider.Name)
			{
				case ProviderName.SqlServer2005 :
				case ProviderName.SqlServer2008 :
					return CallBase();
			}

			if (options.UseSchemaOnly || commandType == CommandType.Text)
				return CallBase();

			try
			{
				var tsql  = $"exec {commandText} {string.Join(", ", parameters.Select(p => p.Name))}";
				var parms = string.Join(", ", parameters.Select(p => $"{p.Name} {p.DbType}"));

				var dt = new DataTable();

				dt.Columns.AddRange(new[]
				{
					new DataColumn { ColumnName = "DataTypeName",     DataType = typeof(string) },
					new DataColumn { ColumnName = "ColumnName",       DataType = typeof(string) },
					new DataColumn { ColumnName = "AllowDBNull",      DataType = typeof(bool)   },
					new DataColumn { ColumnName = "ColumnSize",       DataType = typeof(int)    },
					new DataColumn { ColumnName = "NumericPrecision", DataType = typeof(int)    },
					new DataColumn { ColumnName = "NumericScale",     DataType = typeof(int)    },
					new DataColumn { ColumnName = "IsIdentity",       DataType = typeof(bool)   },
				});

				foreach (var item in dataConnection.QueryProc(new
					{
						name               = "",
						is_nullable        = false,
						system_type_name   = "",
						max_length         = 0,
						precision          = 0,
						scale              = 0,
						is_identity_column = false,
					},
					"sp_describe_first_result_set",
					new DataParameter("tsql", tsql),
					new DataParameter("params", parms)
					)
				)
				{
					var row = dt.NewRow();

					row["DataTypeName"]     = item.system_type_name.Split('(')[0];
					row["ColumnName"]       = item.name ?? "";
					row["AllowDBNull"]      = item.is_nullable;
					row["ColumnSize"]       = item.system_type_name.Contains("nchar", StringComparison.Ordinal) || item.system_type_name.Contains("nvarchar", StringComparison.Ordinal) ? item.max_length / 2 : item.max_length;
					row["NumericPrecision"] = item.precision;
					row["NumericScale"]     = item.scale;
					row["IsIdentity"]       = item.is_identity_column;

					dt.Rows.Add(row);
				}

				return dt.Rows.Count == 0 ? null : dt;
			}
			catch
			{
				return CallBase();
			}

			DataTable? CallBase()
			{
				return base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);
			}
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var list = base.GetDataTypes(dataConnection);

			if (list.TrueForAll(t => !string.Equals(t.DataType, "json", StringComparison.Ordinal)))
			{
				var type = Provider.Adapter.SqlJsonType ?? typeof(string);

				list.Add(new DataTypeInfo
				{
					TypeName         = "json",
					DataType         = type.FullName!,
					ProviderSpecific = Provider.Adapter.SqlJsonType is not null,
					ProviderDbType   = 35,
				});
			}

			if (list.TrueForAll(t => !string.Equals(t.DataType, "vector", StringComparison.Ordinal)))
			{
				var type = Provider.Adapter.SqlVectorType ?? typeof(float[]);

				list.Add(new DataTypeInfo
				{
					TypeName         = "vector",
					DataType         = type.FullName!,
					CreateFormat     = "vector({0})",
					CreateParameters = "length",
					ProviderSpecific = true,
					ProviderDbType   = 36,
				});
			}

			return list;
		}
	}
}
