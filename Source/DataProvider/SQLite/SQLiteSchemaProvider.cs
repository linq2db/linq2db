using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Data;
	using SchemaProvider;

	class SQLiteSchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where t.Field<string>("TABLE_TYPE") != "SYSTEM_TABLE"
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
					on pk.Field<string>("CONSTRAINT_NAME") equals idx.Field<string>("INDEX_NAME")
				where idx.Field<bool>("PRIMARY_KEY")
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("TABLE_CATALOG") + "." + pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("CONSTRAINT_NAME"),
					ColumnName     = pk.Field<string>("COLUMN_NAME"),
					Ordinal        = pk.Field<int>   ("ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				let tschema  = c.Field<string>("TABLE_SCHEMA")
				let schema   = tschema == "sqlite_default_schema" ? "" : tschema
				let dataType = c.Field<string>("DATA_TYPE")
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + schema + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					IsNullable   = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal      = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType     = dataType,
					Length       = Converter.ChangeTypeTo<int>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					IsIdentity   = c.Field<bool>  ("AUTOINCREMENT"),
					SkipOnInsert = dataType == "timestamp",
					SkipOnUpdate = dataType == "timestamp",
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var fks = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeys");

			return
			(
				from fk in fks.AsEnumerable()
				where fk.Field<string>("CONSTRAINT_TYPE") == "FOREIGN KEY"
				select new ForeingKeyInfo
				{
					Name         = fk.Field<string>("CONSTRAINT_NAME"),
					ThisTableID  = fk.Field<string>("TABLE_CATALOG")   + "." + fk.Field<string>("TABLE_SCHEMA")   + "." + fk.Field<string>("TABLE_NAME"),
					ThisColumn   = fk.Field<string>("FKEY_FROM_COLUMN"),
					OtherTableID = fk.Field<string>("FKEY_TO_CATALOG") + "." + fk.Field<string>("FKEY_TO_SCHEMA") + "." + fk.Field<string>("FKEY_TO_TABLE"),
					OtherColumn  = fk.Field<string>("FKEY_TO_COLUMN"),
					Ordinal      = fk.Field<int>("FKEY_FROM_ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return dbConnection.DataSource;
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "smallint"         : return DataType.Int16;
				case "int"              : return DataType.Int32;
				case "real"             : return DataType.Single;
				case "float"            : return DataType.Double;
				case "double"           : return DataType.Double;
				case "money"            : return DataType.Money;
				case "currency"         : return DataType.Money;
				case "decimal"          : return DataType.Decimal;
				case "numeric"          : return DataType.Decimal;
				case "bit"              : return DataType.Boolean;
				case "yesno"            : return DataType.Boolean;
				case "logical"          : return DataType.Boolean;
				case "bool"             : return DataType.Boolean;
				case "boolean"          : return DataType.Boolean;
				case "tinyint"          : return DataType.Byte;
				case "integer"          : return DataType.Int64;
				case "counter"          : return DataType.Int64;
				case "autoincrement"    : return DataType.Int64;
				case "identity"         : return DataType.Int64;
				case "long"             : return DataType.Int64;
				case "bigint"           : return DataType.Int64;
				case "binary"           : return DataType.Binary;
				case "varbinary"        : return DataType.VarBinary;
				case "blob"             : return DataType.VarBinary;
				case "image"            : return DataType.Image;
				case "general"          : return DataType.VarBinary;
				case "oleobject"        : return DataType.VarBinary;
				case "varchar"          : return DataType.VarChar;
				case "nvarchar"         : return DataType.NVarChar;
				case "memo"             : return DataType.Text;
				case "longtext"         : return DataType.Text;
				case "note"             : return DataType.Text;
				case "text"             : return DataType.Text;
				case "ntext"            : return DataType.NText;
				case "string"           : return DataType.Char;
				case "char"             : return DataType.Char;
				case "nchar"            : return DataType.NChar;
				case "datetime"         : return DataType.DateTime;
				case "datetime2"        : return DataType.DateTime2;
				case "smalldate"        : return DataType.SmallDateTime;
				case "timestamp"        : return DataType.Timestamp;
				case "date"             : return DataType.Date;
				case "time"             : return DataType.Time;
				case "uniqueidentifier" : return DataType.Guid;
				case "guid"             : return DataType.Guid;
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
