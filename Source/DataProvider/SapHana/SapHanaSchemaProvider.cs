using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
    using System.Text;

    using Common;
	using Data;
	using SchemaProvider;

    using SqlProvider;

    using SqlQuery;

    class SapHanaSchemaProvider : SchemaProviderBase
    {
        private String _defaultSchema;

        protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
        {
            _defaultSchema = dataConnection.Execute<string>("SELECT CURRENT_SCHEMA FROM DUMMY");

            var test = ((DbConnection)dataConnection.Connection).GetSchema();
            if (test == null) return null;
            var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

            var dt =  dts.AsEnumerable()
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
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views  = ((DbConnection)dataConnection.Connection).GetSchema("Views");

			return
			(
				from t in tables.AsEnumerable()
				let schema = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
                    TableID = schema + "." + name,
					CatalogName     = null,
					SchemaName      = schema,
					TableName       = name,
                    IsDefaultSchema = schema == _defaultSchema,
					IsView          = false,
				}
			).Concat(
				from t in views.AsEnumerable()
				let schema = t.Field<string>("VIEW_SCHEMA")
				let name    = t.Field<string>("VIEW_NAME")
				select new TableInfo
				{
                    TableID = schema + "." + name,
                    CatalogName     = null,
                    SchemaName      = schema,
					TableName       = name,
                    IsDefaultSchema = schema == _defaultSchema,
					IsView          = true,
				}
			).ToList();
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
		    var tblCs= dataConnection.Query<ColumnInfo>(@"SELECT 
	            SCHEMA_NAME || '.' || TABLE_NAME AS ""TableID"",
	            COLUMN_NAME AS ""Name"",
	            CAST(CASE WHEN IS_NULLABLE = 'TRUE' THEN 1 ELSE 0 END AS TINYINT) AS ""IsNullable"",
	            ""POSITION"" AS ""Ordinal"",
	            DATA_TYPE_NAME AS ""DataType"",
	            LENGTH AS ""Length"",
	            LENGTH AS ""Precision"",
	            SCALE AS ""Scale"",
	            COMMENTS AS ""Description"",
	            CAST(CASE WHEN GENERATION_TYPE = 'BY DEFAULT AS IDENTITY' THEN 1 ELSE 0 END AS TINYINT) AS ""IsIdentity""
            FROM 
            SYS.TABLE_COLUMNS").ToList();
            var viewCs = dataConnection.Query<ColumnInfo>(@"SELECT 
	            SCHEMA_NAME || '.' || VIEW_NAME AS ""TableID"",
	            COLUMN_NAME AS ""Name"",
	            CAST(CASE WHEN IS_NULLABLE = 'TRUE' THEN 1 ELSE 0 END AS TINYINT) AS ""IsNullable"",
	            ""POSITION"" AS ""Ordinal"",
	            DATA_TYPE_NAME AS ""DataType"",
	            LENGTH AS ""Length"",
	            LENGTH AS ""Precision"",
	            SCALE AS ""Scale"",
	            COMMENTS AS ""Description"",
	            CAST(CASE WHEN GENERATION_TYPE = 'BY DEFAULT AS IDENTITY' THEN 1 ELSE 0 END AS TINYINT) AS ""IsIdentity""
            FROM 
            SYS.VIEW_COLUMNS").ToList();

		    return tblCs.Concat(viewCs).ToList();
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
                    IsDefaultSchema = schema == _defaultSchema,
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
                var dataType = rd.GetString(3);
                var position = rd.GetInt32(4);
                var paramType = rd.GetString(5);
                var isResult = rd.GetBoolean(6);
                var length = rd.GetInt32(7);
                var scale = rd.GetInt32(8);
                return new ProcedureParameterInfo()
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
	                1 AS IS_RESULT,
	                LENGTH,
	                SCALE
                FROM FUNCTION_PARAMETERS
                WHERE PARAMETER_TYPE <> 'RETURN'
                ORDER BY SCHEMA_NAME, PROCEDURE_NAME, POSITION").ToList();
        }


        protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
        {
            return
            (
                from r in resultTable.AsEnumerable()

                let systemType = r.Field<Type>("DataType")
                let columnName = r.Field<string>("ColumnName")
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


        protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long length, int precision, int scale)
        {
            switch (dataType.ToLower())
            {
                case "tinyint": return typeof(byte);
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
                case "VARCHAR": 
                case "ALPHANUM": return DataType.VarChar;
                case "TEXT": return DataType.Text;                
                case "NCHAR": return DataType.NChar;
                case "SHORTTEXT": 
                case "NVARCHAR": return DataType.NVarChar;
                

                case "BINARY": return DataType.Binary;
                case "VARBINARY": return DataType.VarBinary;
                
                case "BLOB": return DataType.Blob;
                case "CLOB": return DataType.Text;
                case "NCLOB": return DataType.NText;
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

                for (var i = 0; i < procedure.Parameters.Count; i++)
                {
                    if (i != 0)
                        commandText += ",";
                    object value = null;
                    var t = procedure.Parameters[i].SystemType;
                    if (t.IsValueType)
                        value = Activator.CreateInstance(t);
                    commandText += "" + value;
                }

                commandText += ")";
                commandType = CommandType.Text;
                parameters = new DataParameter[0];
            }
            else
            {
                commandType = CommandType.StoredProcedure;
                parameters = procedure.Parameters.Select(p =>
                    new DataParameter
                    {
                        Name = p.ParameterName,
                        Value =
                            p.SystemType == typeof(string)
                                ? ""
                                : p.SystemType == typeof(DateTime)
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

            try
            {
                var st = GetProcedureSchema(dataConnection, commandText, commandType, parameters);

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




	}
}
