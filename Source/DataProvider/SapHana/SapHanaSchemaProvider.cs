using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{

    using Common;
	using Data;
	using SchemaProvider;

    class SapHanaSchemaProvider : SchemaProviderBase
    {
        protected String DefaultSchema;
        protected GetHanaSchemaOptions HanaSchemaOptions;

        public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null)
        {
            HanaSchemaOptions = options as GetHanaSchemaOptions;
            DefaultSchema = dataConnection.Execute<string>("SELECT CURRENT_SCHEMA FROM DUMMY");
            return base.GetSchema(dataConnection, options);
        }

        protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
        {
            var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

            var dt = dts.AsEnumerable()
                .Select(t => new DataTypeInfo
                {
                    TypeName = t.Field<string>("TypeName"),
                    DataType = t.Field<string>("DataType"),
                    CreateFormat = t.Field<string>("CreateFormat"),
                    CreateParameters = t.Field<string>("CreateParameters"),
                    ProviderDbType = Converter.ChangeTypeTo<int>(t["ProviderDbType"]),
                }).ToList();
            var otherTypes = dt.Where(x => x.TypeName.Contains("VAR")).Select(x => new DataTypeInfo
            {
                DataType = x.DataType,
                CreateFormat = x.CreateFormat.Replace("VAR", ""),
                CreateParameters = x.CreateParameters,
                ProviderDbType = x.ProviderDbType,
                TypeName = x.TypeName.Replace("VAR", "")
            }).ToList();
            dt.AddRange(otherTypes);
            return dt;
        }

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
		    var combinedQuery = dataConnection.Query(x =>
		    {
                var schemaName = x.GetString(0);
		        var tableName = x.GetString(1);
		        var comments = x.IsDBNull(2) ? null : x.GetString(2);
		        var isTable = x.GetBoolean(3);
		        return new TableInfo
		        {
		            CatalogName = null,
                    Description = comments,
                    IsDefaultSchema = schemaName == DefaultSchema,
                    IsView = !isTable,
                    SchemaName = schemaName,
                    TableID = schemaName + '.' + tableName,
                    TableName = tableName
		        };
            }, @"SELECT 
                    s.SCHEMA_NAME,
                    TABLE_NAME,
                    COMMENTS,
                    IS_TABLE	
                FROM (
                    SELECT 
                        t.SCHEMA_NAME,
                        t.TABLE_NAME,
                        t.COMMENTS,
                        CAST(1 AS TINYINT) AS IS_TABLE
                    FROM SYS.TABLES AS t
                    WHERE t.SCHEMA_NAME != '_SYS_BIC' AND t.IS_USER_DEFINED_TYPE = 'FALSE'
                    UNION ALL
                    SELECT 
                        v.SCHEMA_NAME,
                        v.VIEW_NAME AS TABLE_NAME,
                        v.COMMENTS,
                        CAST(0 AS TINYINT) AS IS_TABLE	
                    FROM 
                    (SELECT *
		                FROM SYS.VIEWS AS v
		                WHERE v.IS_VALID = 'TRUE' 
		                AND v.VIEW_TYPE NOT IN ('HIERARCHY', 'CALC')
		                UNION ALL
		                SELECT v.*
		                FROM SYS.VIEWS AS v
		                JOIN _SYS_BI.BIMC_ALL_CUBES AS c ON c.VIEW_NAME = v.VIEW_NAME
		                LEFT JOIN (
			                SELECT COUNT(p.CUBE_NAME) AS ParamCount, p.CUBE_NAME 
			                FROM _SYS_BI.BIMC_VARIABLE AS p
			                GROUP BY p.CUBE_NAME
		                ) AS p ON c.CUBE_NAME = p.CUBE_NAME
		                WHERE v.VIEW_TYPE = 'CALC' AND v.IS_VALID = 'TRUE' AND p.CUBE_NAME IS NULL
                    ) AS v
                ) AS combined
                JOIN SYS.SCHEMAS AS s ON combined.SCHEMA_NAME = s.SCHEMA_NAME
                WHERE s.HAS_PRIVILEGES = 'TRUE' 
                    AND s.SCHEMA_NAME NOT IN ('SYS', '_SYS_BI', '_SYS_REPO', '_SYS_STATISTICS')");
		    return combinedQuery.ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
		    var pks = ((DbConnection) dataConnection.Connection).GetSchema("IndexColumns");

		    return
		        (
		            from pk in pks.AsEnumerable()
		            where pk.Field<string>("CONSTRAINT") == "PRIMARY KEY" 
			        select new PrimaryKeyInfo
			        {
				        TableID        = pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
				        PrimaryKeyName = pk.Field<string>("INDEX_NAME"),
				        ColumnName     = pk.Field<string>("COLUMN_NAME"),
				        Ordinal        = Converter.ChangeTypeTo<int>(pk["POSITION"]),
			        }
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
		    const string sqlText = @"SELECT
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
            ) AS combined
            JOIN SYS.SCHEMAS AS s ON combined.SCHEMA_NAME = s.SCHEMA_NAME
            WHERE s.HAS_PRIVILEGES = 'TRUE' 
            AND s.SCHEMA_NAME NOT IN ('SYS', '_SYS_BI', '_SYS_REPO', '_SYS_STATISTICS')";
		    var query = dataConnection.Query(x =>
		    {
		        var schemaName = x.GetString(0);
		        var tableName = x.GetString(1);
		        var columnName = x.GetString(2);
		        var isNullable = x.GetBoolean(3);
		        var position = x.GetInt32(4);
		        var dataTypeName = x.GetString(5);
		        var length = x.GetInt32(6);
		        var scale = x.IsDBNull(7) ? 0 : x.GetInt32(7);
		        var comments = x.IsDBNull(8) ? null : x.GetString(8);
		        var isIdentity = x.GetBoolean(9);
		        var tableId = schemaName + '.' + tableName;
		        return new ColumnInfo
		        {
		            DataType = dataTypeName,
		            Description = comments,
		            IsIdentity = isIdentity,
		            IsNullable = isNullable,
		            Length = length,
		            Name = columnName,
		            Ordinal = position,
		            Precision = length,
                    Scale = scale,
		            TableID = tableId
		        };
		    }, sqlText);
		    return query.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
            return dataConnection.Query<ForeingKeyInfo>(@"SELECT 
	            CONSTRAINT_NAME AS ""Name"",
	            SCHEMA_NAME || '.' || TABLE_NAME AS ""ThisTableID"",
	            COLUMN_NAME AS ""ThisColumn"",
	            SCHEMA_NAME || '.' || REFERENCED_TABLE_NAME AS ""OtherTableID"",
	            REFERENCED_COLUMN_NAME AS ""OtherColumn"",	
	            POSITION AS ""Ordinal""
                FROM REFERENTIAL_CONSTRAINTS
            ").ToList();
		}

        protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
        {
            return dataConnection.Query(rd => 
            {
                var schema = rd.GetString(0);
                var procedure = rd.GetString(1);
                var isFunction = rd.GetBoolean(2);
                var isTableFunction = rd.GetBoolean(3);
                var definition = rd.GetString(4);
                return new ProcedureInfo
                {
                    ProcedureID = String.Concat(schema, '.', procedure),
                    CatalogName = null,
                    IsAggregateFunction = false,
                    IsDefaultSchema = schema == DefaultSchema,
                    IsFunction = isFunction,
                    IsTableFunction = isTableFunction,
                    ProcedureDefinition = definition,
                    ProcedureName = procedure,
                    SchemaName = schema
                };
            }, @"SELECT 
	            SCHEMA_NAME,
	            PROCEDURE_NAME,
	            0 AS IS_FUNCTION,
	            0 AS IS_TABLE_FUNCTION,
	            DEFINITION
            FROM PROCEDURES
            UNION ALL
            SELECT 
	            F.SCHEMA_NAME,
	            F.FUNCTION_NAME AS PROCEDURE_NAME,
	            1 AS IS_FUNCTION,
	            CASE WHEN FP.DATA_TYPE_NAME = 'TABLE_TYPE' THEN 1 ELSE 0 END AS IS_TABLE_FUNCTION,
	            DEFINITION	
            FROM FUNCTIONS AS F
            JOIN FUNCTION_PARAMETERS AS FP ON F.FUNCTION_OID = FP.FUNCTION_OID
            WHERE FP.PARAMETER_TYPE = 'RETURN'").ToList();
        }

        protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
        {

            return dataConnection.Query(rd =>
            {
                var schema = rd.GetString(0);
                var procedure = rd.GetString(1);
                var parameter = rd.GetString(2);
                var dataType = rd.IsDBNull(3) ? null : rd.GetString(3);
                var position = rd.GetInt32(4);
                var paramType = rd.GetString(5);
                var isResult = rd.GetBoolean(6);
                var length = rd.GetInt32(7);
                var scale = rd.GetInt32(8);
                return new ProcedureParameterInfo
                {
                    ProcedureID = String.Concat(schema, '.', procedure),
                    DataType = dataType,
                    IsIn = paramType.Contains("IN"),
                    IsOut = paramType.Contains("OUT"),
                    IsResult = isResult,
                    Length = length,
                    Ordinal = position,
                    ParameterName = parameter,
                    Precision = length,
                    Scale = scale,
                };
            }, @"SELECT 
	                SCHEMA_NAME,
	                PROCEDURE_NAME,
	                PARAMETER_NAME,
	                DATA_TYPE_NAME,
	                POSITION,
	                PARAMETER_TYPE,
	                0 AS IS_RESULT,
	                LENGTH,
	                SCALE
                FROM PROCEDURE_PARAMETERS
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
	                SCALE
                FROM FUNCTION_PARAMETERS
                WHERE NOT (PARAMETER_TYPE = 'RETURN' AND DATA_TYPE_NAME = 'TABLE_TYPE')
                ORDER BY SCHEMA_NAME, PROCEDURE_NAME, POSITION").ToList();
        }


        protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
        {
            return
            (
                from r in resultTable.AsEnumerable()

                let systemType = r.Field<Type>("DataType")
                let columnName = GetEmptyStringIfInvalidColumnName(r.Field<string>("ColumnName"))
                let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
                let dataType = DataTypes.FirstOrDefault(t => t.ProviderDbType == providerType)
                let columnType = dataType == null ? null : dataType.TypeName
                let length = r.Field<int>("ColumnSize")
                let precision = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
                let scale = Converter.ChangeTypeTo<int>(r["NumericScale"])
                let isNullable = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])

                select new ColumnSchema
                {
                    ColumnType = GetDbType(columnType, dataType, length, precision, scale),
                    ColumnName = columnName,
                    IsNullable = isNullable,
                    MemberName = ToValidName(columnName),
                    MemberType = ToTypeName(systemType, isNullable),
                    SystemType = systemType ?? typeof(object),
                    DataType = GetDataType(columnType, null),
                }
            ).ToList();
        }

        private static string GetEmptyStringIfInvalidColumnName(string columnName)
        {
            var invalidCharacters = new[] {':', '(', '"', ' '};
            return columnName.IndexOfAny(invalidCharacters) > -1 ? String.Empty : columnName;
        }

        protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long length, int precision, int scale)
        {
            if (dataType != null)
            {
                switch (dataType.ToLower())
                {
                    case "tinyint": return typeof(byte);
                }
            }
            return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
        }


	    protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
                case "BIGINT": return DataType.Int64;
                case "SMALLINT": return DataType.Int16;
                case "DECIMAL": 
                case "SMALLDECIMAL":return DataType.Decimal;
                case "INTEGER": return DataType.Int32;
                case "TINYINT": return DataType.Byte;
                case "DOUBLE": return DataType.Double;
                case "REAL": return DataType.Single;

                case "DATE": return DataType.Date;
                case "TIME": return DataType.Time;
                case "SECONDDATE": return DataType.SmallDateTime;
                case "TIMESTAMP": return DataType.Timestamp;

                case "CHAR": return DataType.Char;
                case "VARCHAR": return DataType.VarChar;
                case "TEXT": return DataType.Text;                
                case "NCHAR": return DataType.NChar;
                case "ALPHANUM":
                case "SHORTTEXT":                 
                case "NVARCHAR": return DataType.NVarChar;
                

                case "BINARY": return DataType.Binary;
                case "VARBINARY": return DataType.VarBinary;
                
                case "BLOB": return DataType.Blob;
                case "CLOB": return DataType.Text;
                case "NCLOB": 
                case "BINTEXT": return DataType.NText;

                case "ST_POINT":
                case "ST_GEOMETRY":
                case "ST_POINTZ":
			        return DataType.Udt;
            }
			return DataType.Undefined;
		}

        protected override void LoadProcedureTableSchema(DataConnection dataConnection, ProcedureSchema procedure, string commandText,
            List<TableSchema> tables)
        {
            CommandType commandType;
            DataParameter[] parameters;

            if (procedure.IsTableFunction)
            {
                commandText = "SELECT * FROM " + commandText + "(";
                commandText += String.Join(",", procedure.Parameters.Select(p => (
                    p.SystemType == typeof (DateTime)
                        ? "'" + DateTime.Now + "'"
                        : DefaultValue.GetValue(p.SystemType)) ?? "''"));

                commandText += ")";
                commandType = CommandType.Text;
                parameters = new DataParameter[0];
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
                var st = GetProcedureSchema(dataConnection, commandText, commandType, parameters);
                procedure.IsLoaded = true;
                if (st != null)
                {
                    procedure.ResultTable = new TableSchema
                    {
                        IsProcedureResult = true,
                        TypeName = ToValidName(procedure.ProcedureName + "Result"),
                        ForeignKeys = new List<ForeignKeySchema>(),
                        Columns = GetProcedureResultColumns(st)
                    };

                    foreach (var column in procedure.ResultTable.Columns)
                        column.Table = procedure.ResultTable;

                    procedure.SimilarTables =
                        (
                            from t in tables
                            where t.Columns.Count == procedure.ResultTable.Columns.Count
                            let zip = t.Columns.Zip(procedure.ResultTable.Columns, (c1, c2) => new { c1, c2 })
                            where zip.All(z => z.c1.ColumnName == z.c2.ColumnName && z.c1.SystemType == z.c2.SystemType)
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
                                : DefaultValue.GetValue(p.SystemType),
                    DataType = p.DataType,
                    Size = p.Size,
                    Direction =
                        p.IsIn
                            ? p.IsOut
                                ? ParameterDirection.InputOutput
                                : ParameterDirection.Input
                            : ParameterDirection.Output
                }).ToArray();
        }

        protected override DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
        {
            //bug in drivers, SchemaOnly executes statement
            dataConnection.BeginTransaction();
            try
            {
                using (var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.SchemaOnly, parameters))
                {
                    return rd.Reader.GetSchemaTable();
                }
            }
            finally
            {
                dataConnection.RollbackTransaction();
            }
        }


        private IEnumerable<TableInfo> GetViewsWithParameters(DataConnection dataConnection)
        {
            var query = dataConnection.Query(x =>
            {
                var schemaName = x.GetString(0);
                var tableName = x.GetString(1);
                return new TableInfo
                {
                    CatalogName = null,
                    Description = x.IsDBNull(2) ? null : x.GetString(2),
                    IsDefaultSchema = schemaName == DefaultSchema,
                    IsView = true,
                    SchemaName = schemaName,
                    TableID = schemaName + '.' + tableName,
                    TableName = tableName
                };
            }, @"SELECT 
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
                WHERE v.VIEW_TYPE = 'CALC' AND v.IS_VALID = 'TRUE'");
            return query.ToList();
        }

        private static IEnumerable<ProcedureParameterInfo> GetParametersForViews(DataConnection dataConnection)
        {
            var query = dataConnection.Query(rd =>
            {
                var schema = rd.GetString(0);
                var view = rd.GetString(1);
                var parameterName = rd.GetString(2);
                var sqlDataType = rd.GetString(3);
                var sqlDataTypeParts = sqlDataType.Split('(');                
                var dataType = sqlDataTypeParts[0];
                var length = 0;
                var scale = 0;
                if (sqlDataTypeParts.Length == 2)
                {
                    var infoStr = sqlDataTypeParts[1].Substring(0, sqlDataTypeParts[1].Length - 1);
                    var splited = infoStr.Split(',');
                    length = Convert.ToInt32(splited[0]);
                    if (splited.Length == 2)
                    {
                        scale = Convert.ToInt32(splited[1]);
                    }
                }
                var isMandatory = rd.GetBoolean(4);
                var position = rd.GetInt32(5);

                return new ProcedureParameterInfo
                {
                    ProcedureID = String.Concat(schema, '.', view),
                    DataType = dataType,
                    IsIn = isMandatory,
                    IsOut = false,
                    IsResult = false,
                    Length = length,
                    Ordinal = position,
                    ParameterName = parameterName,
                    Precision = length,
                    Scale = scale,
                };
            }, @"SELECT 
	            v.SCHEMA_NAME,
	            v.VIEW_NAME,
	            p.VARIABLE_NAME,
	            p.COLUMN_SQL_TYPE,
	            p.MANDATORY,
	            p.""ORDER""
            FROM SYS.VIEWS AS v
            JOIN _SYS_BI.BIMC_ALL_CUBES AS c ON c.VIEW_NAME = v.VIEW_NAME
            JOIN _SYS_BI.BIMC_VARIABLE AS p ON c.CUBE_NAME = p.CUBE_NAME
            WHERE c.CATALOG_NAME = p.CATALOG_NAME AND v.VIEW_TYPE = 'CALC'
            ORDER BY v.VIEW_NAME, p.""ORDER""");
            return query.ToList();
        }


        protected override List<TableSchema> GetProviderSpecificTables(DataConnection dataConnection)
        {
            var result =
            (
                from v in GetViewsWithParameters(dataConnection)
                join p in GetParametersForViews(dataConnection) on v.TableID equals p.ProcedureID
                into pgroup
                where
                    (IncludedSchemas.Length == 0 || IncludedSchemas.Contains(v.SchemaName)) &&
                    (ExcludedSchemas.Length == 0 || !ExcludedSchemas.Contains(v.SchemaName))
                select new ViewWithParametersTableSchema
                {
                    ID = v.TableID,
                    CatalogName = v.CatalogName,
                    SchemaName = v.SchemaName,
                    TableName = v.TableName,
                    Description = v.Description,
                    IsDefaultSchema = v.IsDefaultSchema,
                    IsView = v.IsView,
                    TypeName = ToValidName(v.TableName),
                    Columns = new List<ColumnSchema>(),
                    ForeignKeys = new List<ForeignKeySchema>(),
                    Parameters = (
                        from pr in pgroup
                        join dt in DataTypes
                            on pr.DataType equals dt.TypeName into g1
                        from dt in g1.DefaultIfEmpty()
                        let systemType = GetSystemType(pr.DataType, null, dt, pr.Length ?? 0, pr.Precision, pr.Scale)
                        orderby pr.Ordinal
                        select new ParameterSchema
                        {
                            SchemaName = pr.ParameterName,
                            SchemaType = GetDbType(pr.DataType, dt, pr.Length ?? 0, pr.Precision, pr.Scale),
                            IsIn = pr.IsIn,
                            IsOut = pr.IsOut,
                            IsResult = pr.IsResult,
                            Size = pr.Length,
                            ParameterName = ToValidName(pr.ParameterName),
                            ParameterType = ToTypeName(systemType, !pr.IsIn),
                            SystemType = systemType ?? typeof(object),
                            DataType = GetDataType(pr.DataType, null)
                        }
                    ).ToList()
                }
            ).ToList();

            var columns =
                from c in GetColumns(dataConnection)
                join v in result on c.TableID equals v.ID
                orderby c.Ordinal
                select new {v, c, dt = GetDataType(c.DataType) };

            foreach (var column in columns)
            {
                var dataType = column.c.DataType;
                var systemType = GetSystemType(dataType, column.c.ColumnType, column.dt, column.c.Length, column.c.Precision, column.c.Scale);
                var isNullable = column.c.IsNullable;
                column.v.Columns.Add(new ColumnSchema
                {
                    Table = column.v,
                    ColumnName = column.c.Name,
                    ColumnType = column.c.ColumnType ?? GetDbType(dataType, column.dt, column.c.Length, column.c.Precision, column.c.Scale),
                    IsNullable = isNullable,
                    MemberName = ToValidName(column.c.Name),
                    MemberType = ToTypeName(systemType, isNullable),
                    SystemType = systemType ?? typeof(object),
                    DataType = GetDataType(dataType, column.c.ColumnType),
                    SkipOnInsert = column.c.SkipOnInsert || column.c.IsIdentity,
                    SkipOnUpdate = column.c.SkipOnUpdate || column.c.IsIdentity,
                    IsPrimaryKey = false,
                    PrimaryKeyOrder = -1,
                    IsIdentity = column.c.IsIdentity,
                    Description = column.c.Description,
                });
            }
            return result.Cast<TableSchema>().ToList();
        }


	}
}
