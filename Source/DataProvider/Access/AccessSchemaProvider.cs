using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using SchemaProvider;

	class AccessSchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where new[] {"TABLE", "VIEW"}.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + '.' + schema + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema.IsNullOrEmpty(),
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW"
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			var dbConnection = (DbConnection)dataConnection.Connection;
			var idxs         = dbConnection.GetSchema("Indexes");

			return
			(
				from idx in idxs.AsEnumerable()
				where idx.Field<bool>("PRIMARY_KEY")
				select new PrimaryKeyInfo
				{
					TableID        = idx.Field<string>("TABLE_CATALOG") + "." + idx.Field<string>("TABLE_SCHEMA") + "." + idx.Field<string>("TABLE_NAME"),
					PrimaryKeyName = idx.Field<string>("INDEX_NAME"),
					ColumnName     = idx.Field<string>("COLUMN_NAME"),
					Ordinal        = ConvertTo<int>.From(idx["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				join dt in DataTypes on c.Field<int>("DATA_TYPE") equals dt.ProviderDbType
				select new ColumnInfo
				{
					TableID    = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name       = c.Field<string>("COLUMN_NAME"),
					IsNullable = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal    = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType   = dt.TypeName,
					Length     = Converter.ChangeTypeTo<int>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision  = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"]),
					Scale      = Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					IsIdentity = Converter.ChangeTypeTo<int>(c["COLUMN_FLAGS"]) == 90,
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			return new List<ForeingKeyInfo>();
		}

		/*
		List<ProcedureInfo> _procedures;

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

			return _procedures =
			(
				from p in ps.AsEnumerable()
				let catalog = p.Field<string>("PROCEDURE_CATALOG")
				let schema  = p.Field<string>("PROCEDURE_SCHEMA")
				let name    = p.Field<string>("PROCEDURE_NAME")
				select new ProcedureInfo
				{
					ProcedureID     = catalog + "." + schema + "." + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					ProcedureName   = name,
					IsDefaultSchema = schema.IsNullOrEmpty()
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			var list = new List<ProcedureParameterInfo>();

			foreach (var procedure in _procedures)
			{
				var cmd = (OleDbCommand)dataConnection.Command;

				cmd.CommandText = procedure.ProcedureName;
				cmd.CommandType = CommandType.StoredProcedure;

				OleDbCommandBuilder.DeriveParameters(cmd);

				var n = 0;

				list.AddRange(
					from OleDbParameter parameter in cmd.Parameters
					select new ProcedureParameterInfo
					{
						ProcedureID   = procedure.ProcedureID,
						ParameterName = parameter.ParameterName,
						IsIn          = parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Input,
						IsOut         = parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.Output,
						Length        = parameter.Size,
						Precision     = parameter.Precision,
						Scale         = parameter.Scale,
						Ordinal       = ++n,
						IsResult      = parameter.Direction == ParameterDirection.ReturnValue,
						DataType      = "Short"
					});
			}

			return list;
		}
		*/

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "short"      : return DataType.Int16;
				case "long"       : return DataType.Int32;
				case "single"     : return DataType.Single;
				case "double"     : return DataType.Double;
				case "currency"   : return DataType.Money;
				case "datetime"   : return DataType.DateTime;
				case "bit"        : return DataType.Boolean;
				case "byte"       : return DataType.Byte;
				case "guid"       : return DataType.Guid;
				case "bigbinary"  : return DataType.Binary;
				case "longbinary" : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "longtext"   : return DataType.NText;
				case "varchar"    : return DataType.VarChar;
				case "decimal"    : return DataType.Decimal;
			}

			return DataType.Undefined;
		}
	}
}
