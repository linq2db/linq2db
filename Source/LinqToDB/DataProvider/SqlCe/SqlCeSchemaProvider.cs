﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

/*

	https://blog.sqlauthority.com/2011/10/02/sql-server-ce-list-of-information_schema-system-tables/

-- Get all the columns of the database
SELECT *
FROM INFORMATION_SCHEMA.COLUMNS
-- Get all the indexes of the database
SELECT *
FROM INFORMATION_SCHEMA.INDEXES
-- Get all the indexes and columns of the database
SELECT *
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
-- Get all the datatypes of the database
SELECT *
FROM INFORMATION_SCHEMA.PROVIDER_TYPES
-- Get all the tables of the database
SELECT *
FROM INFORMATION_SCHEMA.TABLES
-- Get all the constraint of the database
SELECT *
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
-- Get all the foreign keys of the database
SELECT *
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
*/
namespace LinqToDB.DataProvider.SqlCe
{
	using System.Data.SqlTypes;
	using Common;
	using Data;
	using SchemaProvider;

	class SqlCeSchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
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

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var data = dataConnection.Query<PrimaryKeyInfo>(
				@"
SELECT
	COALESCE(TABLE_CATALOG, '') + '.' + COALESCE(TABLE_SCHEMA, '') + '.' + TABLE_NAME AS TableID,
	INDEX_NAME                                            AS PrimaryKeyName,
	COLUMN_NAME                                           AS ColumnName,
	ORDINAL_POSITION                                      AS Ordinal
FROM INFORMATION_SCHEMA.INDEXES
WHERE PRIMARY_KEY = 1");

			return data.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var data = dataConnection.Query<ForeignKeyInfo>(
				@"
SELECT
	COALESCE(rc.CONSTRAINT_CATALOG,        '') + '.' + COALESCE(rc.CONSTRAINT_SCHEMA,        '') + '.' + rc.CONSTRAINT_TABLE_NAME        ThisTableID,
	COALESCE(rc.UNIQUE_CONSTRAINT_CATALOG, '') + '.' + COALESCE(rc.UNIQUE_CONSTRAINT_SCHEMA, '') + '.' + rc.UNIQUE_CONSTRAINT_TABLE_NAME OtherTableID,
	rc.CONSTRAINT_NAME                                                                                                                   Name,
	tc.COLUMN_NAME                                                                                                                       ThisColumn,
	oc.COLUMN_NAME                                                                                                                       OtherColumn
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE tc ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE oc ON oc.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME");

			return data.ToList();
		}

		protected override string GetDatabaseName(DataConnection connection)
		{
			return Path.GetFileNameWithoutExtension(((DbConnection)connection.Connection).Database);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, long? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return (dataType?.ToLower()) switch
			{
				"tinyint" => typeof(byte),
				_         => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			return dataType?.ToLower() switch
			{
				"smallint"         => DataType.Int16,
				"int"              => DataType.Int32,
				"real"             => DataType.Single,
				"float"            => DataType.Double,
				"money"            => DataType.Money,
				"bit"              => DataType.Boolean,
				"tinyint"          => DataType.Byte,
				"bigint"           => DataType.Int64,
				"uniqueidentifier" => DataType.Guid,
				"varbinary"        => DataType.VarBinary,
				"binary"           => DataType.Binary,
				"image"            => DataType.Image,
				"nvarchar"         => DataType.NVarChar,
				"nchar"            => DataType.NChar,
				"ntext"            => DataType.NText,
				"numeric"          => DataType.Decimal,
				"datetime"         => DataType.DateTime,
				"rowversion"       => DataType.Timestamp,
				_           	   => DataType.Undefined,
			};
		}

		protected override string GetProviderSpecificTypeNamespace() => SqlTypes.TypesNamespace;

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType)
			{
				case "varbinary"        :
				case "rowversion"       :
				case "image"            :
				case "binary"           : return nameof(SqlBinary);
				case "tinyint"          : return nameof(SqlByte);
				case "datetime"         : return nameof(SqlDateTime);
				case "bit"              : return nameof(SqlBoolean);
				case "smallint"         : return nameof(SqlInt16);
				case "numeric"          :
				case "decimal"          : return nameof(SqlDecimal);
				case "int"              : return nameof(SqlInt32);
				case "real"             : return nameof(SqlSingle);
				case "float"            : return nameof(SqlDouble);
				case "money"            : return nameof(SqlMoney);
				case "bigint"           : return nameof(SqlInt64);
				case "nvarchar"         :
				case "nchar"            :
				case "ntext"            : return nameof(SqlString);
				case "uniqueidentifier" : return nameof(SqlGuid);
			}

			return base.GetProviderSpecificType(dataType);
		}
	}
}
