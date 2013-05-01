using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;

namespace LinqToDB.DataProvider.Firebird
{
	using System.IO;

	using Common;
	using Data;
	using SchemaProvider;

	class FirebirdSchemaProvider : SchemaProviderBase
	{
		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(base.GetDatabaseName(dbConnection));
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where !ConvertTo<bool>.From(t["IS_SYSTEM_TABLE"])
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("OWNER_NAME")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + '.' + t.Field<string>("TABLE_SCHEMA") + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema == "SYSDBA",
					IsView          = false,
					Description     = t.Field<string>("DESCRIPTION")
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			var pks = ((DbConnection)dataConnection.Connection).GetSchema("PrimaryKeys");

			return
			(
				from pk in pks.AsEnumerable()
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("TABLE_CATALOG") + "." + pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("PK_NAME"),
					ColumnName     = pk.Field<string>("COLUMN_NAME"),
					Ordinal        = ConvertTo<int>.From(pk["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				join dt in DataTypes on c.Field<string>("COLUMN_DATA_TYPE") equals dt.TypeName
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					DataType     = dt.TypeName,
					IsNullable   = Converter.ChangeTypeTo<bool>(c["IS_NULLABLE"]),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					Length       = Converter.ChangeTypeTo<int> (c["COLUMN_SIZE"]),
					Precision    = Converter.ChangeTypeTo<int> (c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int> (c["NUMERIC_SCALE"]),
					IsIdentity   = false,
					Description  = c.Field<string>("DESCRIPTION"),
					SkipOnInsert = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
					SkipOnUpdate = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var fks  = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeys");
			var cols = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeyColumns");

			return new List<ForeingKeyInfo>();
		}

		List<ProcedureInfo> _procedures;

		/*
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
					ProcedureID         = catalog + "." + schema + "." + name,
					CatalogName         = catalog,
					SchemaName          = schema,
					ProcedureName       = name,
					IsDefaultSchema     = schema.IsNullOrEmpty(),
					ProcedureDefinition = p.Field<string>("PROCEDURE_DEFINITION")
				}
			).ToList();
		}
		*/

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType)
		{
//			if (dataType == null)
//			{
//				switch (columnType.ToLower())
//				{
//					case "text" : return typeof(string);
//					default     : throw new InvalidOperationException();
//				}
//			}

			return base.GetSystemType(columnType, dataType);
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType.ToLower())
			{
				case "array"            : return DataType.VarBinary;
				case "bigint"           : return DataType.Int64;
				case "blob"             : return DataType.VarBinary;
				case "char"             : return DataType.NChar;
				case "date"             : return DataType.Date;
				case "decimal"          : return DataType.Decimal;
				case "double precision" : return DataType.Double;
				case "float"            : return DataType.Single;
				case "integer"          : return DataType.Int32;
				case "numeric"          : return DataType.Decimal;
				case "smallint"         : return DataType.Int16;
				case "blob sub_type 1"  : return DataType.Text;
				case "time"             : return DataType.Time;
				case "timestamp"        : return DataType.DateTime;
				case "varchar"          : return DataType.NVarChar;
			}

			return DataType.Undefined;
		}
	}
}
