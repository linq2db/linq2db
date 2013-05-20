using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using SchemaProvider;

	class MySqlSchemaProvider : SchemaProviderBase
	{
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
					TableID         = catalog + ".." + name,
					CatalogName     = catalog,
					SchemaName      = "",
					TableName       = name,
					IsDefaultSchema = true,
					IsView          = false,
				}
			).Concat(
				from t in views.AsEnumerable()
				let catalog = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + ".." + name,
					CatalogName     = catalog,
					SchemaName      = "",
					TableName       = name,
					IsDefaultSchema = true,
					IsView          = true,
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			var dbConnection = (DbConnection)dataConnection.Connection;
			var pks          = dbConnection.GetSchema("IndexColumns");
			var idxs         = dbConnection.GetSchema("Indexes");

			return
			(
				from pk  in pks. AsEnumerable()
				join idx in idxs.AsEnumerable()
					on
						pk. Field<string>("INDEX_CATALOG") + "." +
						pk. Field<string>("INDEX_SCHEMA")  + "." +
						pk. Field<string>("INDEX_NAME")    + "." +
						pk. Field<string>("TABLE_NAME")
					equals
						idx.Field<string>("INDEX_CATALOG") + "." +
						idx.Field<string>("INDEX_SCHEMA")  + "." +
						idx.Field<string>("INDEX_NAME")    + "." +
						idx.Field<string>("TABLE_NAME")
				where idx.Field<bool>("PRIMARY")
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("INDEX_SCHEMA") + ".." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("INDEX_NAME"),
					ColumnName     = pk.Field<string>("COLUMN_NAME"),
					Ordinal        = pk.Field<int>   ("ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var tcs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");
			var vcs = ((DbConnection)dataConnection.Connection).GetSchema("ViewColumns");

			return
			(
				from c in tcs.AsEnumerable()
				let dataType = c.Field<string>("DATA_TYPE")
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_SCHEMA") + ".." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					IsNullable   = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal      = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType     = dataType,
					Length       = Converter.ChangeTypeTo<int>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					ColumnType   = c.Field<string>("COLUMN_TYPE"),
					IsIdentity   = c.Field<string>("EXTRA") == "auto_increment",
				}
			).Concat(
				from c in vcs.AsEnumerable()
				let dataType = c.Field<string>("DATA_TYPE")
				select new ColumnInfo
				{
					TableID      = c.Field<string>("VIEW_SCHEMA") + ".." + c.Field<string>("VIEW_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					IsNullable   = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal      = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType     = dataType,
					Length       = Converter.ChangeTypeTo<int>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					ColumnType   = c.Field<string>("COLUMN_TYPE"),
					IsIdentity   = c.Field<string>("EXTRA") == "auto_increment",
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var fks = ((DbConnection)dataConnection.Connection).GetSchema("Foreign Key Columns");

			return
			(
				from fk in fks.AsEnumerable()
				select new ForeingKeyInfo
				{
					Name         = fk.Field<string>("CONSTRAINT_NAME"),
					ThisTableID  = fk.Field<string>("TABLE_SCHEMA")   + ".." + fk.Field<string>("TABLE_NAME"),
					ThisColumn   = fk.Field<string>("COLUMN_NAME"),
					OtherTableID = fk.Field<string>("REFERENCED_TABLE_SCHEMA") + ".." + fk.Field<string>("REFERENCED_TABLE_NAME"),
					OtherColumn  = fk.Field<string>("REFERENCED_COLUMN_NAME"),
					Ordinal      = Converter.ChangeTypeTo<int>(fk["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType.ToUpper())
			{
				case "bit"        : return DataType.UInt64;
				case "blob"       : return DataType.Blob;
				case "tinyblob"   : return DataType.Binary;
				case "mediumblob" : return DataType.Binary;
				case "longblob"   : return DataType.Binary;
				case "binary"     : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "date"       : return DataType.Date;
				case "datetime"   : return DataType.DateTime;
				case "timestamp"  : return DataType.Timestamp;
				case "time"       : return DataType.Time;
				case "char"       : return DataType.Char;
				case "nchar"      : return DataType.NChar;
				case "varchar"    : return DataType.VarChar;
				case "nvarchar"   : return DataType.NVarChar;
				case "set"        : return DataType.NVarChar;
				case "enum"       : return DataType.NVarChar;
				case "tinytext"   : return DataType.Text;
				case "text"       : return DataType.Text;
				case "mediumtext" : return DataType.Text;
				case "longtext"   : return DataType.Text;
				case "double"     : return DataType.Double;
				case "float"      : return DataType.Single;
				case "tinyint"    : return DataType.SByte;
				case "smallint"   : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt16 : DataType.Int16;
				case "int"        : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "year"       : return DataType.Int32;
				case "mediumint"  : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "bigint"     : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt64 : DataType.Int64;
				case "decimal"    : return DataType.Decimal;
				case "tiny int"   : return DataType.Byte;
			}

			return DataType.Undefined;
		}

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType, int length, int precision, int scale)
		{
			switch (columnType)
			{
				case "datetime2" : return typeof(DateTime);
			}

			return base.GetSystemType(columnType, dataType, length, precision, scale);
		}
	}
}
