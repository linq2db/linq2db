using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Data;
	using SchemaProvider;

	class SqlServer2000SchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var dbConnection  = (DbConnection)dataConnection.Connection;
			var ignoredTables = new[] { "sysconstraints", "syssegments" };

			return
			(
				from t in dbConnection.GetSchema("Tables").AsEnumerable()
				let name    = t.Field<string>("TABLE_NAME")
				where ignoredTables.Contains(name)
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				select new TableInfo
				{
					TableID         = catalog + '.' + schema + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsDefaultSchema = schema == "dbo",
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
			var dbConnection = (DbConnection)dataConnection.Connection;

			return
			(
				from c in dbConnection.GetSchema("Columns").AsEnumerable()
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
			var dbConnection = (DbConnection)dataConnection.Connection;

			return
			(
				from fk in dbConnection.GetSchema("ForeignKeys").AsEnumerable()
				where fk.Field<string>("CONSTRAINT_TYPE") == "FOREIGN_KEY"
				select new ForeingKeyInfo
				{
					Name         = fk.Field<string>("CONSTRAINT_NAME"),
					ThisTableID  = fk.Field<string>("TABLE_CATALOG")   + "." + fk.Field<string>("TABLE_SCHEMA")   + "." + fk.Field<string>("TABLE_NAME"),
					ThisColumn   = fk.Field<string>("FKEY_FROM_COLUMN"),
					OtherTableID = fk.Field<string>("FKEY_TO_CATALOG") + "." + fk.Field<string>("FKEY_TO_SCHEMA") + "." + fk.Field<string>("FKEY_TO_NAME"),
					OtherColumn  = fk.Field<string>("FKEY_TO_COLUMN"),
					Ordinal      = fk.Field<int>("FKEY_FROM_ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override DataType GetDataType(string columnType)
		{
			switch (columnType)
			{
				case "image"            : return DataType.Image;
				case "text"             : return DataType.Text;
				case "binary"           : return DataType.Binary;
				case "tinyint"          : return DataType.SByte;
				case "date"             : return DataType.Date;
				case "time"             : return DataType.Time;
				case "bit"              : return DataType.Boolean;
				case "smallint"         : return DataType.Int16;
				case "decimal"          : return DataType.Decimal;
				case "int"              : return DataType.Int32;
				case "smalldatetime"    : return DataType.SmallDateTime;
				case "real"             : return DataType.Single;
				case "money"            : return DataType.Money;
				case "datetime"         : return DataType.DateTime;
				case "float"            : return DataType.Double;
				case "numeric"          : return DataType.Decimal;
				case "smallmoney"       : return DataType.SmallMoney;
				case "datetime2"        : return DataType.DateTime2;
				case "bigint"           : return DataType.Int64;
				case "varbinary"        : return DataType.VarBinary;
				case "timestamp"        : return DataType.Timestamp;
				case "sysname"          : return DataType.NVarChar;
				case "nvarchar"         : return DataType.NVarChar;
				case "varchar"          : return DataType.VarChar;
				case "ntext"            : return DataType.NText;
				case "uniqueidentifier" : return DataType.Guid;
				case "datetimeoffset"   : return DataType.DateTimeOffset;
				case "sql_variant"      : return DataType.Variant;
				case "xml"              : return DataType.Xml;
				case "char"             : return DataType.Char;
				case "nchar"            : return DataType.NChar;
				case "hierarchyid"      :
				case "geography"        :
				case "geometry"         : return DataType.Udt;
			}

			return DataType.Undefined;
		}
	}
}
