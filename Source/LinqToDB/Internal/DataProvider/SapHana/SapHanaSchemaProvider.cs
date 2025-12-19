using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public class SapHanaSchemaProvider : SchemaProviderBase
	{
		protected string                DefaultSchema                { get; private set; } = null!;
		protected GetHanaSchemaOptions? HanaSchemaOptions            { get; private set; }
		protected bool                  HasAccessForCalculationViews { get; private set; }
		protected string?               SchemasFilter                { get; private set; }

		protected override bool GetProcedureSchemaExecutesProcedure => true;

		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
		{
			HanaSchemaOptions            = options as GetHanaSchemaOptions;
			DefaultSchema                = dataConnection.Execute<string>("SELECT CURRENT_SCHEMA FROM DUMMY");
			HasAccessForCalculationViews = CheckAccessForCalculationViews(dataConnection);
			SchemasFilter                = BuildSchemaFilter(options, DefaultSchema, SapHanaMappingSchema.ConvertStringToSql);

			return base.GetSchema(dataConnection, options);
		}

		private bool CheckAccessForCalculationViews(DataConnection dataConnection)
		{
			try
			{
				dataConnection.Execute("SELECT 1 FROM _SYS_BI.BIMC_ALL_CUBES LIMIT 1");
				return true;
			}
			catch
			{
				var options = HanaSchemaOptions ?? new GetHanaSchemaOptions();
				if (options.ThrowExceptionIfCalculationViewsNotAuthorized)
				{
					throw;
				}
			}

			return false;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = dataConnection.OpenDbConnection().GetSchema("DataTypes");

			var dt = dts.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("TypeName")!,
					DataType         = t.Field<string>("DataType")!,
					CreateFormat     = t.Field<string>("CreateFormat"),
					CreateParameters = t.Field<string>("CreateParameters"),
					ProviderDbType   = Converter.ChangeTypeTo<int>(t["ProviderDbType"]),
				}).ToList();

			var otherTypes = dt.Where(x => x.TypeName.Contains("VAR", StringComparison.Ordinal)).Select(x => new DataTypeInfo
			{
				DataType         = x.DataType,
				CreateFormat     = x.CreateFormat?.Replace("VAR", "", StringComparison.Ordinal),
				CreateParameters = x.CreateParameters,
				ProviderDbType   = x.ProviderDbType,
				TypeName         = x.TypeName.Replace("VAR", "", StringComparison.Ordinal),
			}).ToList();

			dt.AddRange(otherTypes);

			return dt;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<TableInfo>();

			var combinedQuery = dataConnection.Query(x =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var schemaName = x.GetString(0);
				var tableName  = x.GetString(1);
				var comments   = x.IsDBNull(2) ? null : x.GetString(2);
				var isTable    = x.GetBoolean(3);

				return new TableInfo
				{
					CatalogName     = null,
					Description     = comments,
					IsDefaultSchema = string.Equals(schemaName, DefaultSchema, StringComparison.Ordinal),
					IsView          = !isTable,
					SchemaName      = schemaName,
					TableID         = schemaName + '.' + tableName,
					TableName       = tableName,
				};
			}, GetTablesQuery());

			return combinedQuery.ToList();
		}

		private string GetTablesQuery()
		{
			var result =
				$"""
				SELECT
					s.SCHEMA_NAME,
					TABLE_NAME,
					COMMENTS,
					IS_TABLE
				FROM
				(
					SELECT
						t.SCHEMA_NAME,
						t.TABLE_NAME,
						t.COMMENTS,
						CAST(1 AS TINYINT) AS IS_TABLE
					FROM SYS.TABLES AS t
					WHERE t.SCHEMA_NAME {SchemasFilter}
					UNION ALL
					SELECT
						v.SCHEMA_NAME,
						v.VIEW_NAME AS TABLE_NAME,
						v.COMMENTS,
						CAST(0 AS TINYINT) AS IS_TABLE
					FROM
					(
						SELECT *
						FROM SYS.VIEWS AS v
						WHERE v.IS_VALID = 'TRUE'
						AND v.VIEW_TYPE NOT IN ('HIERARCHY', 'CALC')
						AND v.SCHEMA_NAME {SchemasFilter}
				""";

			if (HasAccessForCalculationViews)
			{
				result +=
					$"""
					
					UNION ALL
					SELECT v.*
					FROM SYS.VIEWS AS v
					JOIN _SYS_BI.BIMC_ALL_CUBES AS c ON c.VIEW_NAME = v.VIEW_NAME
					LEFT JOIN
					(
						SELECT COUNT(p.CUBE_NAME) AS ParamCount, p.CUBE_NAME
						FROM _SYS_BI.BIMC_VARIABLE AS p
						GROUP BY p.CUBE_NAME
					) AS p ON c.CUBE_NAME = p.CUBE_NAME
					WHERE v.VIEW_TYPE = 'CALC' AND v.IS_VALID = 'TRUE' AND p.CUBE_NAME IS NULL AND v.SCHEMA_NAME {SchemasFilter}
					""";
			}

			result +=
				"""

					) AS v
				) AS combined
				JOIN SYS.SCHEMAS AS s ON combined.SCHEMA_NAME = s.SCHEMA_NAME
				WHERE s.HAS_PRIVILEGES = 'TRUE'
				""";

			return result;
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var pks = dataConnection.OpenDbConnection().GetSchema("IndexColumns");

			return
			(
				from pk in pks.AsEnumerable()
				where string.Equals(pk.Field<string>("CONSTRAINT"), "PRIMARY KEY", StringComparison.Ordinal)
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("INDEX_NAME")!,
					ColumnName     = pk.Field<string>("COLUMN_NAME")!,
					Ordinal        = Converter.ChangeTypeTo<int>(pk["POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<ColumnInfo>();

			var sqlText = 
				$"""
				SELECT
					combined.SCHEMA_NAME,
					TABLE_NAME,
					COLUMN_NAME,
					CAST(CASE WHEN IS_NULLABLE = 'TRUE' THEN 1 ELSE 0 END AS TINYINT) AS IS_NULLABLE,
					POSITION,
					DATA_TYPE_NAME,
					LENGTH,
					SCALE,
					COMMENTS,
					CAST(CASE WHEN GENERATION_TYPE = 'BY DEFAULT AS IDENTITY' THEN 1 ELSE 0 END AS TINYINT) AS IS_IDENTITY
				FROM
					(SELECT
						SCHEMA_NAME,
						TABLE_NAME,
						COLUMN_NAME,
						IS_NULLABLE,
						POSITION,
						DATA_TYPE_NAME,
						LENGTH,
						SCALE,
						COMMENTS,
						GENERATION_TYPE
					FROM SYS.TABLE_COLUMNS
					WHERE SCHEMA_NAME {SchemasFilter}
					UNION ALL
					SELECT
						SCHEMA_NAME,
						VIEW_NAME  AS TABLE_NAME,
						COLUMN_NAME,
						IS_NULLABLE,
						POSITION,
						DATA_TYPE_NAME,
						LENGTH,
						SCALE,
						COMMENTS,
						GENERATION_TYPE
					FROM SYS.VIEW_COLUMNS
					WHERE SCHEMA_NAME {SchemasFilter}
				) AS combined
				JOIN SYS.SCHEMAS AS s ON combined.SCHEMA_NAME = s.SCHEMA_NAME
				WHERE s.HAS_PRIVILEGES = 'TRUE'
				""";

			var query = dataConnection.Query(x =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var schemaName   = x.GetString(0);
				var tableName    = x.GetString(1);
				var columnName   = x.GetString(2);
				var isNullable   = x.GetBoolean(3);
				var position     = x.GetInt32(4);
				var dataTypeName = x.GetString(5);
				var length       = (int?)x.GetInt32(6);
				var scale        = x.IsDBNull(7) ? null : (int?)x.GetInt32(7);
				var comments     = x.IsDBNull(8) ? null : x.GetString(8);
				var isIdentity   = x.GetBoolean(9);
				var tableId      = schemaName + '.' + tableName;

				// decfloat detect
				if (string.Equals(dataTypeName, "DECIMAL", StringComparison.Ordinal) && scale == null)
					length = null;

				return new ColumnInfo
				{
					DataType    = dataTypeName,
					Description = comments,
					IsIdentity  = isIdentity,
					IsNullable  = isNullable,
					Length      = length,
					Name        = columnName,
					Ordinal     = position,
					Precision   = length,
					Scale       = scale,
					TableID     = tableId,
				};
			}, sqlText);

			return query.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<ForeignKeyInfo>();

			return dataConnection
				.Query<ForeignKeyInfo>(
					$"""
					SELECT
						CONSTRAINT_NAME AS "Name",
						SCHEMA_NAME || '.' || TABLE_NAME AS "ThisTableID",
						COLUMN_NAME AS "ThisColumn",
						SCHEMA_NAME || '.' || REFERENCED_TABLE_NAME AS "OtherTableID",
						REFERENCED_COLUMN_NAME AS "OtherColumn",
						POSITION AS "Ordinal"
					FROM REFERENTIAL_CONSTRAINTS
					WHERE SCHEMA_NAME {SchemasFilter}
					"""
				)
				.ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return null;

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						var schema          = rd.GetString(0);
						var procedure       = rd.GetString(1);
						var isFunction      = rd.GetBoolean(2);
						var isTableFunction = rd.GetBoolean(3);
						var definition      = rd.IsDBNull(4) ? null : rd.GetString(4);
						return new ProcedureInfo
						{
							ProcedureID         = string.Concat(schema, '.', procedure),
							CatalogName         = null,
							IsAggregateFunction = false,
							IsDefaultSchema     = string.Equals(schema, DefaultSchema, StringComparison.Ordinal),
							IsFunction          = isFunction,
							IsTableFunction     = isTableFunction,
							ProcedureDefinition = definition,
							ProcedureName       = procedure,
							SchemaName          = schema,
						};
					},
					$"""
					SELECT
						SCHEMA_NAME,
						PROCEDURE_NAME,
						0 AS IS_FUNCTION,
						0 AS IS_TABLE_FUNCTION,
						DEFINITION
					FROM PROCEDURES
					WHERE SCHEMA_NAME {SchemasFilter}
					UNION ALL
					SELECT
						F.SCHEMA_NAME,
						F.FUNCTION_NAME AS PROCEDURE_NAME,
						1 AS IS_FUNCTION,
						CASE WHEN F.FUNCTION_USAGE_TYPE = 'TABLE' THEN 1 ELSE 0 END AS IS_TABLE_FUNCTION,
						DEFINITION
					FROM FUNCTIONS AS F
					WHERE F.SCHEMA_NAME {SchemasFilter}
					"""
				)
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<ProcedureParameterInfo>();

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						var schema     = rd.GetString(0);
						var procedure  = rd.GetString(1);
						var parameter  = rd.GetString(2);
						var dataType   = rd.IsDBNull(3) ? null : rd.GetString(3);
						var position   = rd.GetInt32(4);
						var paramType  = rd.GetString(5);
						var isResult   = rd.GetBoolean(6);
						var length     = (int?)rd.GetInt32(7);
						var scale      = (int?)rd.GetInt32(8);
						var isNullable = string.Equals(rd.GetString(9), "TRUE", StringComparison.Ordinal);

						// detect decfloat
						if (string.Equals(dataType, "DECIMAL", StringComparison.Ordinal) && length == 65535)
							scale = length = null;

						return new ProcedureParameterInfo
						{
							ProcedureID   = string.Concat(schema, '.', procedure),
							DataType      = dataType,
							IsIn          = paramType.Contains("IN", StringComparison.Ordinal),
							IsOut         = paramType.Contains("OUT", StringComparison.Ordinal),
							IsResult      = isResult,
							Length        = length,
							Ordinal       = position,
							ParameterName = parameter,
							Precision     = length,
							Scale         = scale,
							IsNullable    = isNullable,
						};
					}, 
					$"""
					SELECT
						SCHEMA_NAME,
						PROCEDURE_NAME,
						PARAMETER_NAME,
						DATA_TYPE_NAME,
						POSITION,
						PARAMETER_TYPE,
						0 AS IS_RESULT,
						LENGTH,
						SCALE,
						IS_NULLABLE
					FROM PROCEDURE_PARAMETERS
					WHERE SCHEMA_NAME {SchemasFilter}
					UNION ALL
					SELECT
						SCHEMA_NAME,
						FUNCTION_NAME AS PROCEDURE_NAME,
						PARAMETER_NAME,
						DATA_TYPE_NAME,
						POSITION,
						PARAMETER_TYPE,
						CASE WHEN PARAMETER_TYPE = 'RETURN' THEN 1 ELSE 0 END AS IS_RESULT,
						LENGTH,
						SCALE,
						IS_NULLABLE
					FROM FUNCTION_PARAMETERS
					WHERE NOT (PARAMETER_TYPE = 'RETURN' AND DATA_TYPE_NAME = 'TABLE_TYPE') AND SCHEMA_NAME {SchemasFilter}
					ORDER BY SCHEMA_NAME, PROCEDURE_NAME, POSITION
					"""
				)
				.ToList();
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let systemType   = r.Field<Type>("DataType")
				let columnName   = GetEmptyStringIfInvalidColumnName(r.Field<string>("ColumnName")!)
				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType     = GetDataTypeByProviderDbType(providerType, options)
				let columnType   = dataType?.TypeName
				let length       = r.Field<int>("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let isNullable   = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])

				select new ColumnSchema
				{
					ColumnType           = GetDbType(options, columnType, dataType, length, precision, scale, null, null, null),
					ColumnName           = columnName,
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
				}
			).ToList();
		}

		private static string GetEmptyStringIfInvalidColumnName(string columnName)
		{
			var invalidCharacters = new[] {':', '(', '"', ' '};
			return columnName.IndexOfAny(invalidCharacters) > -1 ? string.Empty : columnName;
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType switch
			{
				"TINYINT" => typeof(byte),

				"ST_GEOMETRY" or
				"ST_GEOMETRYCOLLECTION" or
				"ST_POINT" or
				"ST_MULTIPOINT" or
				"ST_LINESTRING" or
				"ST_MULTILINESTRING" or
				"ST_POLYGON" or
				"ST_MULTIPOLYGON" or
				"ST_CIRCULARSTRING" =>
					typeof(byte[]),

				_ => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType switch
			{
				"BIGINT"       => DataType.Int64,
				"SMALLINT"     => DataType.Int16,
				"DECIMAL"      => scale == null ? DataType.DecFloat : DataType.Decimal,
				"SMALLDECIMAL" => DataType.SmallDecFloat,
				"INTEGER"      => DataType.Int32,
				"TINYINT"      => DataType.Byte,
				"DOUBLE"       => DataType.Double,
				"REAL"         => DataType.Single,

				"DATE"         => DataType.Date,
				"TIME"         => DataType.Time,
				"SECONDDATE"   => DataType.SmallDateTime,
				"TIMESTAMP"    => DataType.Timestamp,

				"CHAR"         => DataType.Char,
				"VARCHAR"      => DataType.VarChar,
				"TEXT"         => DataType.Text,
				"NCHAR"        => DataType.NChar,

				"ALPHANUM"     or
				"SHORTTEXT"    or
				"NVARCHAR"     => DataType.NVarChar,

				"BINARY"       => DataType.Binary,
				"VARBINARY"    => DataType.VarBinary,

				"BLOB"         => DataType.Blob,
				"CLOB"         => DataType.Text,
				"NCLOB"        or
				"BINTEXT"      => DataType.NText,
				"REAL_VECTOR"  => DataType.Vector32,

				"ST_GEOMETRY"          or
				"ST_GEOMETRYCOLLECTION"or
				"ST_POINT"             or
				"ST_MULTIPOINT"        or
				"ST_LINESTRING"        or
				"ST_MULTILINESTRING"   or
				"ST_POLYGON"           or
				"ST_MULTIPOLYGON"      or
				"ST_CIRCULARSTRING"    => DataType.Udt,

				_ => DataType.Undefined,
			};
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		protected override void LoadProcedureTableSchema(DataConnection dataConnection, GetSchemaOptions options, ProcedureSchema procedure, string commandText,
			List<TableSchema> tables)
		{
			CommandType     commandType;
			DataParameter[] parameters;

			if (procedure.IsTableFunction)
			{
				commandText = "SELECT * FROM " + commandText + "(";
				commandText += string.JoinStrings(',', procedure.Parameters.Select(p => 
					p.SystemType == typeof (DateTime)
						? string.Create(CultureInfo.InvariantCulture, $"'{DateTime.Now}'")
						: string.Create(CultureInfo.InvariantCulture, $"{(DefaultValue.GetValue(p.SystemType ?? typeof(object)) ?? "''")}")));

				commandText += ")";
				commandType = CommandType.Text;
				parameters  = [];
			}
			else
			{
				commandType = CommandType.StoredProcedure;
				parameters = HanaSchemaOptions != null
					? (HanaSchemaOptions.GetStoredProcedureParameters(procedure) ??
					  GetStoredProcedureDataParameters(procedure))
					: GetStoredProcedureDataParameters(procedure);
			}

			try
			{
				var st = GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);

				procedure.IsLoaded = true;

				if (st != null)
				{
					procedure.ResultTable = new TableSchema()
					{
						IsProcedureResult = true,
						TypeName          = ToValidName(procedure.ProcedureName + "Result"),
						ForeignKeys       = new List<ForeignKeySchema>(),
						Columns           = GetProcedureResultColumns(st, options),
					};

					foreach (var column in procedure.ResultTable.Columns)
						column.Table = procedure.ResultTable;

					procedure.SimilarTables =
						(
							from t in tables
							where t.Columns.Count == procedure.ResultTable.Columns.Count
							let zip = t.Columns.Zip(procedure.ResultTable.Columns, (c1, c2) => new { c1, c2 })
							where zip.All(z => string.Equals(z.c1.ColumnName, z.c2.ColumnName, StringComparison.Ordinal) && z.c1.SystemType == z.c2.SystemType)
							select t
							).ToList();
				}
			}
			catch (Exception ex)
			{
				procedure.ResultException = ex;
			}
		}

		private static DataParameter[] GetStoredProcedureDataParameters(ProcedureSchema procedure)
		{
			return procedure.Parameters.Select(p =>
				new DataParameter
				{
					Name = p.ParameterName,
					Value =
						p.SystemType == typeof (string)
							? ""
							: p.SystemType == typeof (DateTime)
								? DateTime.Now
								: DefaultValue.GetValue(p.SystemType ?? typeof(object)),
					DataType = p.DataType,
					Size = (int?)p.Size,
					Direction =
						p.IsIn
							? p.IsOut
								? ParameterDirection.InputOutput
								: ParameterDirection.Input
							: ParameterDirection.Output,
				}).ToArray();
		}

		private IEnumerable<TableInfo> GetViewsWithParameters(DataConnection dataConnection)
		{
			if (SchemasFilter == null)
				return new List<TableInfo>();

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						var schemaName = rd.GetString(0);
						var tableName  = rd.GetString(1);
						return new TableInfo
						{
							CatalogName     = null,
							Description     = rd.IsDBNull(2) ? null : rd.GetString(2),
							IsDefaultSchema = string.Equals(schemaName, DefaultSchema, StringComparison.Ordinal),
							IsView          = true,
							SchemaName      = schemaName,
							TableID         = schemaName + '.' + tableName,
							TableName       = tableName,
						};
					},
					$"""
					SELECT
						v.SCHEMA_NAME,
						v.VIEW_NAME AS TABLE_NAME,
						v.COMMENTS
					FROM SYS.VIEWS AS v
					JOIN _SYS_BI.BIMC_ALL_CUBES AS c ON c.VIEW_NAME = v.VIEW_NAME
					JOIN (
						SELECT COUNT(p.CUBE_NAME) AS ParamCount, p.CUBE_NAME
						FROM _SYS_BI.BIMC_VARIABLE AS p
						GROUP BY p.CUBE_NAME
					) AS p ON c.CUBE_NAME = p.CUBE_NAME
					WHERE v.VIEW_TYPE = 'CALC' AND v.IS_VALID = 'TRUE' AND v.SCHEMA_NAME
					{SchemasFilter}
					"""
				)
				.ToList();
		}

		private IEnumerable<ProcedureParameterInfo> GetParametersForViews(DataConnection dataConnection)
		{
			if (SchemasFilter == null)
				return new List<ProcedureParameterInfo>();

			return dataConnection
				.Query(
					rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						var schema           = rd.GetString(0);
						var view             = rd.GetString(1);
						var parameterName    = rd.GetString(2);
						var sqlDataType      = rd.GetString(3);
						var sqlDataTypeParts = sqlDataType.Split('(');
						var dataType         = sqlDataTypeParts[0];
						var length           = 0;
						var scale            = 0;

						if (sqlDataTypeParts.Length == 2)
						{
							var infoStr = sqlDataTypeParts[1].Substring(0, sqlDataTypeParts[1].Length - 1);
							var splited = infoStr.Split(',');
							length = Convert.ToInt32(splited[0], CultureInfo.InvariantCulture);
							if (splited.Length == 2)
							{
								scale = Convert.ToInt32(splited[1], CultureInfo.InvariantCulture);
							}
						}

						var isMandatory = rd.GetBoolean(4);
						var position    = rd.GetInt32(5);

						return new ProcedureParameterInfo
						{
							ProcedureID   = string.Concat(schema, '.', view),
							DataType      = dataType,
							IsIn          = isMandatory,
							IsOut         = false,
							IsResult      = false,
							Length        = length,
							Ordinal       = position,
							ParameterName = parameterName,
							Precision     = length,
							Scale         = scale,
							IsNullable    = true,
						};
					},
					$"""
					SELECT
						v.SCHEMA_NAME,
						v.VIEW_NAME,
						p.VARIABLE_NAME,
						p.COLUMN_SQL_TYPE,
						p.MANDATORY,
						p."ORDER"
					FROM SYS.VIEWS AS v
					JOIN _SYS_BI.BIMC_ALL_CUBES AS c ON c.VIEW_NAME = v.VIEW_NAME
					JOIN _SYS_BI.BIMC_VARIABLE AS p ON c.CUBE_NAME = p.CUBE_NAME
					WHERE c.CATALOG_NAME = p.CATALOG_NAME AND v.VIEW_TYPE = 'CALC' AND v.SCHEMA_NAME {SchemasFilter}
					ORDER BY v.VIEW_NAME, p."ORDER"
					"""
				)
				.ToList();
		}

		protected override List<TableSchema> GetProviderSpecificTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (!HasAccessForCalculationViews)
				return new List<TableSchema>();

			var parameters = GetParametersForViews(dataConnection).ToLookup(p => p.ProcedureID, StringComparer.Ordinal);

			var result = GetViewsWithParameters(dataConnection)
				.Where(v => 
					(IncludedSchemas .Count == 0 ||  IncludedSchemas .Contains(v.SchemaName)) &&
					(ExcludedSchemas .Count == 0 || !ExcludedSchemas .Contains(v.SchemaName)) &&
					(IncludedCatalogs.Count == 0 ||  IncludedCatalogs.Contains(v.CatalogName!)) &&
					(ExcludedCatalogs.Count == 0 || !ExcludedCatalogs.Contains(v.CatalogName!))
				)
				.Select(v => new ViewWithParametersTableSchema
				{
					ID              = v.TableID,
					CatalogName     = v.CatalogName,
					SchemaName      = v.SchemaName,
					TableName       = v.TableName,
					Description     = v.Description,
					IsDefaultSchema = v.IsDefaultSchema,
					IsView          = v.IsView,
					TypeName        = ToValidName(v.TableName),
					Columns         = [],
					ForeignKeys     = [],
					Parameters      = (
						from pr in parameters[v.TableID]
						orderby pr.Ordinal
						let dt         = GetDataType(pr.DataType, null, options)
						let systemType = GetSystemType(pr.DataType, null, dt, pr.Length ?? 0, pr.Precision, pr.Scale, options)
						select new ParameterSchema
						{
							SchemaName           = pr.ParameterName,
							SchemaType           = GetDbType(options, pr.DataType, dt, pr.Length ?? 0, pr.Precision, pr.Scale, pr.UDTCatalog, pr.UDTSchema, pr.UDTName),
							IsIn                 = pr.IsIn,
							IsOut                = pr.IsOut,
							IsResult             = pr.IsResult,
							Size                 = pr.Length,
							ParameterName        = ToValidName(pr.ParameterName!),
							ParameterType        = ToTypeName(systemType, !pr.IsIn),
							SystemType           = systemType,
							DataType             = GetDataType(pr.DataType, null, pr.Length, pr.Precision, pr.Scale),
							ProviderSpecificType = GetProviderSpecificType(pr.DataType),
							IsNullable           = pr.IsNullable,
						}
					).ToList(),
				})
				.Cast<TableSchema>()
				.ToList();

			var columns = GetColumns(dataConnection, options).ToLookup(c => c.TableID, StringComparer.Ordinal);

			foreach (var v in result)
			{
				v.Columns = (
					from c in columns[v.ID!]
					orderby c.Ordinal
					let dt         = GetDataType(c.DataType, null, options)
					let systemType = GetSystemType(c.DataType, c.ColumnType, dt, c.Length, c.Precision, c.Scale, options)
					select new ColumnSchema
					{
						Table                = v,
						ColumnName           = c.Name,
						ColumnType           = c.ColumnType ?? GetDbType(options, c.DataType, dt, c.Length, c.Precision, c.Scale, null, null, null),
						IsNullable           = c.IsNullable,
						MemberName           = ToValidName(c.Name),
						MemberType           = ToTypeName(systemType, c.IsNullable),
						SystemType           = systemType,
						DataType             = GetDataType(c.DataType, c.ColumnType, c.Length, c.Precision, c.Scale),
						ProviderSpecificType = GetProviderSpecificType(c.DataType),
						SkipOnInsert         = c.SkipOnInsert || c.IsIdentity,
						SkipOnUpdate         = c.SkipOnUpdate || c.IsIdentity,
						IsPrimaryKey         = false,
						PrimaryKeyOrder      = -1,
						IsIdentity           = c.IsIdentity,
						Description          = c.Description,
						Ordinal              = c.Ordinal,
					}
				).ToList();
			}

			return result;
		}
	}
}
