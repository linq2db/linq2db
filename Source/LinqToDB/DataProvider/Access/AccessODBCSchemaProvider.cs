﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.Access
{
	// https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/odbc-schema-collections
	// unused tables:
	// DataSourceInformation - database settings
	// ReservedWords - reserved words
	class AccessODBCSchemaProvider : AccessSchemaProviderBase
	{
		public AccessODBCSchemaProvider()
		{
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://github.com/dotnet/runtime/issues/35442
			return Array<ForeignKeyInfo>.Empty;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// tables and views has same schema, only difference in TABLE_TYPE
			// views also include SELECT procedures, including procedures with parameters(!)
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views  = ((DbConnection)dataConnection.Connection).GetSchema("Views");
			var procs  = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

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
					IsDefaultSchema    = schema.IsNullOrEmpty(),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsProviderSpecific = system,
					Description        = t.Field<string>("REMARKS")
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://github.com/dotnet/runtime/issues/35442
			return Array<PrimaryKeyInfo>.Empty;
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				let typeName = c.Field<string>("TYPE_NAME")
				let dt       = GetDataType(typeName, options)
				let size     = Converter.ChangeTypeTo<int?>(c["COLUMN_SIZE"])
				let scale    = Converter.ChangeTypeTo<int?>(c["DECIMAL_DIGITS"])
				select new ColumnInfo
				{
					TableID     = c.Field<string>("TABLE_CAT") + "." + c.Field<string>("TABLE_SCHEM") + "." + c.Field<string>("TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME"),
					IsNullable  = c.Field<short> ("NULLABLE") == 1,
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = dt?.TypeName,
					Length      = dt?.CreateParameters != null && dt.CreateParameters.Contains("length") && size != 0 ? size  : null,
					Precision   = dt?.CreateParameters != null && dt.CreateParameters.Contains("precision")           ? size  : null,
					Scale       = dt?.CreateParameters != null && dt.CreateParameters.Contains("scale")               ? scale : null,
					IsIdentity  = typeName == "COUNTER",
					Description = c.Field<string>("REMARKS")
				}
			).ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

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
					IsDefaultSchema     = schema.IsNullOrEmpty()
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("ProcedureParameters");
			return
			(
				from p in ps.AsEnumerable()
				let catalog  = p.Field<string>("PROCEDURE_CAT")
				let schema   = p.Field<string>("PROCEDURE_SCHEM")
				let name     = p.Field<string>("PROCEDURE_NAME")
				let size     = p.Field<int?>("COLUMN_SIZE")
				let typeName = p.Field<string>("TYPE_NAME")
				let dt       = GetDataType(typeName, options)
				select new ProcedureParameterInfo()
				{
					ProcedureID   = catalog + "." + schema + "." + name,
					ParameterName = p.Field<string>("COLUMN_NAME").TrimStart('[').TrimEnd(']'),
					IsIn          = true,
					IsOut         = false,
					Length        = dt.CreateParameters != null && dt.CreateParameters.Contains("length")    ? size : null,
					Precision     = dt.CreateParameters != null && dt.CreateParameters.Contains("precision") ? size : null,
					Scale         = dt.CreateParameters != null && dt.CreateParameters.Contains("scale")     ? p.Field<short?>("DECIMAL_DIGITS") : null,
					Ordinal       = p.Field<int>("ORDINAL_POSITION"),
					IsResult      = false,
					DataType      = typeName,
					IsNullable    = p.Field<short>("NULLABLE") == 1 // allways true
				}
			).ToList();
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			return ((DbConnection)dataConnection.Connection).GetSchema("ProcedureColumns", new[] { null, null, commandText.TrimStart('[').TrimEnd(']') });
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, long? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var dbType = columnType;

			if (dataType != null)
			{
				var parms = dataType.CreateParameters;

				if (!parms.IsNullOrWhiteSpace())
				{
					var paramNames = parms.Split(',');
					var paramValues = new object?[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim().ToLower())
						{
							case "length"   : paramValues[i] = length   ; break;
							case "precision": paramValues[i] = precision; break;
							case "scale"    : paramValues[i] = scale    ; break;
						}
					}

					if (paramValues.All(v => v != null))
					{
						var format = $"{dbType}({string.Join(", ", Enumerable.Range(0, paramValues.Length).Select(i => $"{{{i}}}"))})";
						dbType     = string.Format(format, paramValues);
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
				let dt         = GetDataType(columnType, options)
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
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = columnType == "COUNTER",
				}
			).ToList();
		}
	}
}
