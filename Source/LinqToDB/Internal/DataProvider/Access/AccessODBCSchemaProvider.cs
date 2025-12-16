using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Access
{
	// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/odbc-schema-collections
	// unused tables:
	// DataSourceInformation - database settings
	// ReservedWords - reserved words
	public class AccessODBCSchemaProvider : AccessSchemaProviderBase
	{
		public AccessODBCSchemaProvider()
		{
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://github.com/dotnet/runtime/issues/35442
			return [];
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// tables and views has same schema, only difference in TABLE_TYPE
			// views also include SELECT procedures, including procedures with parameters(!)
			var dbConnection = dataConnection.OpenDbConnection();
			var tables       = dbConnection.GetSchema("Tables");
			var views        = dbConnection.GetSchema("Views");
			var procs        = dbConnection.GetSchema("Procedures");

			var procIds = new HashSet<string>(
				procs.AsEnumerable().Select(p => $"{p.Field<string>("PROCEDURE_CAT")}.{p.Field<string>("PROCEDURE_SCHEM")}.{p.Field<string>("PROCEDURE_NAME")}"));

			return
			(
				from t in tables.AsEnumerable().Concat(views.AsEnumerable())
				let catalog = t.Field<string>("TABLE_CAT") // path to db file
				let schema  = t.Field<string>("TABLE_SCHEM") // empty
				let name    = t.Field<string>("TABLE_NAME") // table name
				// Compared to OleDb:
				// no separate ACCESS TABLE type, SYSTEM TABLE type used
				// VIEW is in separate schema table
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE"
				let id      = catalog + '.' + schema + '.' + name
				where !procIds.Contains(id)
				select new TableInfo
				{
					TableID            = id,
					CatalogName        = null,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = string.IsNullOrEmpty(schema),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsProviderSpecific = system,
					Description        = t.Field<string>("REMARKS"),
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://github.com/dotnet/runtime/issues/35442
			return [];
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = dataConnection.OpenDbConnection().GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				let typeName = c.Field<string>("TYPE_NAME")
				let dt       = GetDataType(typeName, null, options)
				let size     = Converter.ChangeTypeTo<int?>(c["COLUMN_SIZE"])
				let scale    = Converter.ChangeTypeTo<int?>(c["DECIMAL_DIGITS"])
				select new ColumnInfo
				{
					TableID     = c.Field<string>("TABLE_CAT") + "." + c.Field<string>("TABLE_SCHEM") + "." + c.Field<string>("TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME")!,
					IsNullable  = c.Field<short> ("NULLABLE") == 1,
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = dt?.TypeName ?? typeName,
					Length      = dt?.CreateParameters != null && dt.CreateParameters.Contains("length") && size != 0 ? size  : null,
					Precision   = dt?.CreateParameters != null && dt.CreateParameters.Contains("precision")           ? size  : null,
					Scale       = dt?.CreateParameters != null && dt.CreateParameters.Contains("scale")               ? scale : null,
					IsIdentity  = typeName == "COUNTER",
					Description = c.Field<string>("REMARKS"),
				}
			).ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			var ps = dataConnection.OpenDbConnection().GetSchema("Procedures");

			return
			(
				from p in ps.AsEnumerable()
				let catalog = p.Field<string>("PROCEDURE_CAT")
				let schema  = p.Field<string>("PROCEDURE_SCHEM")
				let name    = p.Field<string>("PROCEDURE_NAME")
				select new ProcedureInfo
				{
					ProcedureID         = catalog + "." + schema + "." + name,
					CatalogName         = null,
					SchemaName          = schema,
					ProcedureName       = name,
					IsDefaultSchema     = string.IsNullOrEmpty(schema),
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			var ps = dataConnection.OpenDbConnection().GetSchema("ProcedureParameters");
			return
			(
				from p in ps.AsEnumerable()
				let catalog  = p.Field<string>("PROCEDURE_CAT")
				let schema   = p.Field<string>("PROCEDURE_SCHEM")
				let name     = p.Field<string>("PROCEDURE_NAME")
				let size     = p.Field<int?>("COLUMN_SIZE")
				let typeName = p.Field<string>("TYPE_NAME")
				let dt       = GetDataType(typeName, null, options)
				select new ProcedureParameterInfo()
				{
					ProcedureID   = catalog + "." + schema + "." + name,
					ParameterName = p.Field<string>("COLUMN_NAME")!.TrimStart('[').TrimEnd(']'),
					IsIn          = true,
					IsOut         = false,
					Length        = dt.CreateParameters != null && dt.CreateParameters.Contains("length")    ? size : null,
					Precision     = dt.CreateParameters != null && dt.CreateParameters.Contains("precision") ? size : null,
					Scale         = dt.CreateParameters != null && dt.CreateParameters.Contains("scale")     ? p.Field<short?>("DECIMAL_DIGITS") : null,
					Ordinal       = p.Field<int>("ORDINAL_POSITION"),
					IsResult      = false,
					DataType      = typeName,
					IsNullable    = p.Field<short>("NULLABLE") == 1, // allways true
				}
			).ToList();
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			return dataConnection.OpenDbConnection().GetSchema("ProcedureColumns", new[] { null, null, commandText.TrimStart('[').TrimEnd(']') });
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var dbType = columnType ?? dataType?.TypeName;

			if (dataType != null)
			{
				var parms = dataType.CreateParameters;

				if (!string.IsNullOrWhiteSpace(parms))
				{
					var paramNames = parms!.Split(',');
					var paramValues = new object?[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim().ToLowerInvariant())
						{
							case "length"   : paramValues[i] = length   ; break;
							case "precision": paramValues[i] = precision; break;
							case "scale"    : paramValues[i] = scale    ; break;
						}
					}

					if (paramValues.All(v => v != null))
					{
						var format = $"{dbType}({string.Join(", ", Enumerable.Range(0, paramValues.Length).Select(i => string.Create(CultureInfo.InvariantCulture, $"{{{i}}}")))})";
						dbType     = string.Format(CultureInfo.InvariantCulture, format, paramValues);
					}
				}
			}

			return dbType;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = base.GetDataTypes(dataConnection);

			// https://docs.microsoft.com/en-us/sql/odbc/microsoft/microsoft-access-data-types?view=sql-server-ver15
			if (dts.All(dt => dt.TypeName != "BIGBINARY"))
			{
				dts.Add(new DataTypeInfo()
				{
					TypeName         = "BIGBINARY",
					DataType         = typeof(byte[]).FullName!,
					ProviderDbType   = 9,
				});
			}

			if (dts.All(dt => dt.TypeName != "DECIMAL"))
			{
				dts.Add(new DataTypeInfo()
				{
					TypeName         = "DECIMAL",
					DataType         = typeof(decimal).FullName!,
					CreateParameters = "precision,scale",
					ProviderDbType   = 7,
				});
			}

			return dts;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let columnType = r.Field<string>("TYPE_NAME")
				let columnName = r.Field<string>("COLUMN_NAME")
				let isNullable = r.Field<short> ("NULLABLE") == 1
				let dt         = GetDataType(columnType, null, options)
				let length     = r.Field<int?>  ("COLUMN_SIZE")
				let precision  = length
				let scale      = Converter.ChangeTypeTo<int>(r["DECIMAL_DIGITS"])
				let systemType = GetSystemType(columnType, null, dt, length, precision, scale, options)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = columnType == "COUNTER",
				}
			).ToList();
		}
	}
}
