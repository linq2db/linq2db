﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Data;
	using SchemaProvider;

	class SqlCeSchemaProvider : SchemaProviderBase
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
			var cs           = dbConnection.GetSchema("IndexColumns");

			return
			(
				from idx in idxs.AsEnumerable()
				join c   in cs.  AsEnumerable()
					on idx.Field<string>("TABLE_NAME") equals c.Field<string>("TABLE_NAME")
				select new PrimaryKeyInfo
				{
					TableID        = idx.Field<string>("TABLE_CATALOG") + "." + idx.Field<string>("TABLE_SCHEMA") + "." + idx.Field<string>("TABLE_NAME"),
					PrimaryKeyName = idx.Field<string>("INDEX_NAME"),
					ColumnName     = c.  Field<string>("COLUMN_NAME"),
					Ordinal        = ConvertTo<int>.From(c["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				select new ColumnInfo
				{
					TableID    = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name       = c.Field<string>("COLUMN_NAME"),
					IsNullable = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal    = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					DataType   = c.Field<string>("DATA_TYPE"),
					Length     = Converter.ChangeTypeTo<long>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision  = Converter.ChangeTypeTo<int> (c["NUMERIC_PRECISION"]),
					Scale      = Converter.ChangeTypeTo<int> (c["NUMERIC_SCALE"]),
					IsIdentity = false,
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			//var fks = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeys");

			return new List<ForeingKeyInfo>();
		}

		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(dbConnection.Database);
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "tinyint" : return typeof(byte);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "smallint"         : return DataType.Int16;
				case "int"              : return DataType.Int32;
				case "real"             : return DataType.Single;
				case "float"            : return DataType.Double;
				case "money"            : return DataType.Money;
				case "bit"              : return DataType.Boolean;
				case "tinyint"          : return DataType.Byte;
				case "bigint"           : return DataType.Int64;
				case "uniqueidentifier" : return DataType.Guid;
				case "varbinary"        : return DataType.VarBinary;
				case "binary"           : return DataType.Binary;
				case "image"            : return DataType.Image;
				case "nvarchar"         : return DataType.NVarChar;
				case "nchar"            : return DataType.NChar;
				case "ntext"            : return DataType.NText;
				case "numeric"          : return DataType.Decimal;
				case "datetime"         : return DataType.DateTime;
				case "rowversion"       : return DataType.Timestamp;
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return "System.Data.SqlTypes";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType)
			{
				case "varbinary"        :
				case "rowversion"       :
				case "image"            : return "SqlBinary";
				case "binary"           : return "SqlBinary";
				case "tinyint"          : return "SqlByte";
				case "datetime"         : return "SqlDateTime";
				case "bit"              : return "SqlBoolean";
				case "smallint"         : return "SqlInt16";
				case "numeric"          :
				case "decimal"          : return "SqlDecimal";
				case "int"              : return "SqlInt32";
				case "real"             : return "SqlSingle";
				case "float"            : return "SqlDouble";
				case "money"            : return "SqlMoney";
				case "bigint"           : return "SqlInt64";
				case "nvarchar"         :
				case "nchar"            :
				case "ntext"            : return "SqlString";
				case "uniqueidentifier" : return "SqlGuid";
			}

			return base.GetProviderSpecificType(dataType);
		}
	}
}
