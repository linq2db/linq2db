using System;
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			// there is no direct access to FK from ODBC GetSchema API
			// and .net provider doesn't expose access to native ODBC API like SQLForeignKeys
			// we can try to load non-unique indexes and link them to primary keys
			// but taking into account that we cannot distinguish primary keys from unique indexes
			// this will be profanation
			return Array<ForeignKeyInfo>.Empty;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			// tables and views has same schema, only difference in TABLE_TYPE
			// views also include stored procedures...
			var tables = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Tables"));
			var views  = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Views"));

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
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
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

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			// probably we just need to disable PK support
			//return Array<ForeignKeyInfo>.Empty;

			// restriction[2] (TABLE_NAME) required for ODBC which is super effective...
			// actually, this is garbage. All records have TYPE=3 and only difference is that primary keys will
			// have NON_UNIQUE = 0, but it also could be UNIQUE index
			var pks = new List<PrimaryKeyInfo>();
			foreach (var tableName in tables.Where(t => !t.IsView).Select(t => t.TableName!))
			{
				DataTable idxs;
				try
				{
					idxs = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Indexes", new string?[] { null, null, tableName }));
				}
				catch (Exception)
				{
					// This actually doesn't make much sense - you show table in Tables schema, but don't allow
					// to read details about it
					// ERROR [42000] [Microsoft][ODBC Microsoft Access Driver] Could not read definitions; no read definitions permission for table or query 'MSysACEs'.
					continue;
				}

				pks.AddRange
				(
					from idx in idxs.AsEnumerable()
					where idx.Field<short>("NON_UNIQUE") == 0
					select new PrimaryKeyInfo
					{
						TableID        = idx.Field<string>("TABLE_CAT") + "." + idx.Field<string>("TABLE_SCHEM") + "." + idx.Field<string>("TABLE_NAME"),
						PrimaryKeyName = idx.Field<string>("INDEX_NAME"),
						ColumnName     = idx.Field<string>("COLUMN_NAME"),
						Ordinal        = ConvertTo<int>.From(idx["ORDINAL_POSITION"]),
					}
				);
			}

			return pks;
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Columns"));

			return
			(
				from c in cs.AsEnumerable()
				let dt = GetDataTypeByProviderDbType(c.Field<short>("DATA_TYPE"), options)
				select new ColumnInfo
				{
					TableID     = c.Field<string>("TABLE_CAT") + "." + c.Field<string>("TABLE_SCHEM") + "." + c.Field<string>("TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME"),
					IsNullable  = c.Field<short> ("NULLABLE") == 1,
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = dt?.TypeName,
					Length      = Converter.ChangeTypeTo<int?>(c["COLUMN_SIZE"]),
					Precision   = Converter.ChangeTypeTo<int?> (c["NUM_PREC_RADIX"]),
					Scale       = Converter.ChangeTypeTo<int?> (c["DECIMAL_DIGITS"]),
					IsIdentity  = c.Field<string>("TYPE_NAME") == "COUNTER",
					Description = c.Field<string>("REMARKS")
				}
			).ToList();
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var ps = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Procedures"));

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

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures)
		{
			var ps = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("ProcedureParameters"));
			return
			(
				from p in ps.AsEnumerable()
				let catalog = p.Field<string>("PROCEDURE_CAT")
				let schema  = p.Field<string>("PROCEDURE_SCHEM")
				let name    = p.Field<string>("PROCEDURE_NAME")
				select new ProcedureParameterInfo()
				{
					ProcedureID   = catalog + "." + schema + "." + name,
					ParameterName = p.Field<string>("COLUMN_NAME").TrimStart('[').TrimEnd(']'),
					IsIn          = true,
					IsOut         = false,
					Length        = p.Field<int?>("COLUMN_SIZE"),
					Precision     = p.Field<short?>("NUM_PREC_RADIX"),
					Scale         = p.Field<short?>("DECIMAL_DIGITS"),
					Ordinal       = p.Field<int>("ORDINAL_POSITION"),
					IsResult      = false,
					DataType      = p.Field<string>("TYPE_NAME"),
					IsNullable    = p.Field<short>("NULLABLE") == 1 // allways true
				}
			).ToList();
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			commandText = $"CALL {commandText} ({string.Join(", ", Enumerable.Range(0, parameters.Length).Select(_ => "?"))})";
			using (var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.SchemaOnly, parameters))
				return rd.Reader!.GetSchemaTable();
		}
	}
}
