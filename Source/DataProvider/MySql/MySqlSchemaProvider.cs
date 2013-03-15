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
					Ordinal      = fk.Field<int>   ("ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override DataType GetDataType(string columnType)
		{
			switch (columnType)
			{
				case "BIT"        : return DataType.UInt64;
				case "BLOB"       : return DataType.UInt64; //System.Byte[]
				case "TINYBLOB"   : return DataType.UInt64; //System.Byte[]
				case "MEDIUMBLOB" : return DataType.UInt64; //System.Byte[]
				case "LONGBLOB"   : return DataType.UInt64; //System.Byte[]
				case "BINARY"     : return DataType.UInt64; //System.Byte[]
				case "VARBINARY"  : return DataType.UInt64; //System.Byte[]
				case "DATE"       : return DataType.UInt64; //System.DateTime
				case "DATETIME"   : return DataType.UInt64; //System.DateTime
				case "TIMESTAMP"  : return DataType.UInt64; //System.DateTime
				case "TIME"       : return DataType.UInt64; //System.TimeSpan
				case "CHAR"       : return DataType.UInt64; //System.String
				case "NCHAR"      : return DataType.UInt64; //System.String
				case "VARCHAR"    : return DataType.UInt64; //System.String
				case "NVARCHAR"   : return DataType.UInt64; //System.String
				case "SET"        : return DataType.UInt64; //System.String
				case "ENUM"       : return DataType.UInt64; //System.String
				case "TINYTEXT"   : return DataType.UInt64; //System.String
				case "TEXT"       : return DataType.UInt64; //System.String
				case "MEDIUMTEXT" : return DataType.UInt64; //System.String
				case "LONGTEXT"   : return DataType.UInt64; //System.String
				case "DOUBLE"     : return DataType.UInt64; //System.Double
				case "FLOAT"      : return DataType.UInt64; //System.Single
				case "TINYINT"    : return DataType.UInt64; //System.SByte
//				case "SMALLINT"   : return DataType.UInt64; //System.Int16
//				case "INT"        : return DataType.UInt64; //System.Int32
//				case "YEAR"       : return DataType.UInt64; //System.Int32
//				case "MEDIUMINT"  : return DataType.UInt64; //System.Int32
//				case "BIGINT"     : return DataType.UInt64; //System.Int64
//				case "DECIMAL"    : return DataType.UInt64; //System.Decimal
//				case "TINY INT"   : return DataType.UInt64; //System.Byte
//				case "SMALLINT"   : return DataType.UInt64; //System.UInt16
//				case "MEDIUMINT"  : return DataType.UInt64; //System.UInt32
//				case "INT"        : return DataType.UInt64; //System.UInt32
//				case "BIGINT"     : return DataType.UInt64; //System.UInt64
			}

			return DataType.Undefined;
		}

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType)
		{
			switch (columnType)
			{
				case "datetime2" : return typeof(DateTime);
			}

			return base.GetSystemType(columnType, dataType);
		}
	}
}
