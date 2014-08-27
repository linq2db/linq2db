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
        protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
        {
            var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

            return dts.AsEnumerable()
                .Select(t => new DataTypeInfo
                {
                    TypeName = t.Field<string>("TypeName"),
                    DataType = t.Field<string>("DataType"),
                    CreateFormat = t.Field<string>("CreateFormat"),
                    CreateParameters = t.Field<string>("CreateParameters"),
                    ProviderDbType = Converter.ChangeTypeTo<int>(t["ProviderDbType"]),
                })
                .ToList();
        }

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views  = ((DbConnection)dataConnection.Connection).GetSchema("Views");

			return
			(
				from t in tables.AsEnumerable()
				let catalog = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + "." + name,
					CatalogName     = catalog,
					SchemaName      = catalog,
					TableName       = name,
					IsDefaultSchema = catalog == "SYS",
					IsView          = false,
				}
			).Concat(
				from t in views.AsEnumerable()
				let catalog = t.Field<string>("VIEW_SCHEMA")
				let name    = t.Field<string>("VIEW_NAME")
				select new TableInfo
				{
					TableID         = catalog + "." + name,
					CatalogName     = catalog,
                    SchemaName      = catalog,
					TableName       = name,
                    IsDefaultSchema = catalog == "SYS",
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
	            REFERENCED_CONSTRAINT_NAME AS ""Name"",
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
            var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

            return
            (
                from p in ps.AsEnumerable()
                let catalog = p.Field<string>("ROUTINE_SCHEMA")
                let schema = p.Field<string>("ROUTINE_SCHEMA")
                let name = p.Field<string>("ROUTINE_NAME")
                select new ProcedureInfo
                {
                    IsFunction = p.Field<string>("ROUTINE_TYPE") == "FUNCTION",
                    ProcedureID = catalog + "." + schema + "." + name,
                    CatalogName = catalog,
                    SchemaName = schema,
                    ProcedureName = name,
                    IsDefaultSchema = schema == "SYS"
                }
            ).ToList();
        }

        protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
        {
            var ps = ((DbConnection)dataConnection.Connection).GetSchema("ProcedureParameters");

            return
            (
                from p in ps.AsEnumerable()
                let schema = p.Field<String>("PROCEDURE_SCHEMA")
                let length = Converter.ChangeTypeTo<int>(p["LENGTH"])
                let paramType = p.Field<String>("PARAMETER_TYPE")
                select new ProcedureParameterInfo
                {
                    ProcedureID = schema + "." + schema + "." + p.Field<String>("PROCEDURE_NAME"),
                    ParameterName = p.Field<String>("PARAMETER_NAME"),
                    DataType = p.Field<string>("DATA_TYPE_NAME"),
                    Ordinal = Converter.ChangeTypeTo<int>(p["POSITION"]),
                    IsIn = paramType.Contains("IN"),
                    IsOut = paramType.Contains("OUT"),
                    IsResult = false,
                    Length = length, 
                    Precision = length,
                    Scale = Converter.ChangeTypeTo<int>(p["SCALE"])
                }
            ).ToList();
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

	}
}
