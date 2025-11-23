using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

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
namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeSchemaProvider : SchemaProviderBase
	{
		private static readonly IReadOnlyList<string> _tableTypes = new[] { "TABLE", "VIEW" };

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var tables = dataConnection.OpenDbConnection().GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where _tableTypes.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + '.' + schema + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = string.IsNullOrEmpty(schema),
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW"
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection
				.Query<PrimaryKeyInfo>(
					"""
					SELECT
						COALESCE(TABLE_CATALOG, '') + '.' + COALESCE(TABLE_SCHEMA, '') + '.' + TABLE_NAME AS TableID,
						INDEX_NAME                                            AS PrimaryKeyName,
						COLUMN_NAME                                           AS ColumnName,
						ORDINAL_POSITION                                      AS Ordinal
					FROM INFORMATION_SCHEMA.INDEXES
					WHERE PRIMARY_KEY = 1
					"""
				)
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = dataConnection.OpenDbConnection().GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				let length = Converter.ChangeTypeTo<long>(c["CHARACTER_MAXIMUM_LENGTH"])
				select new ColumnInfo
				{
					TableID    = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name       = c.Field<string>("COLUMN_NAME")!,
					IsNullable = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal    = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					DataType   = c.Field<string>("DATA_TYPE"),
					Length     = length > int.MaxValue ? null : (int?)length,
					Precision  = Converter.ChangeTypeTo<int> (c["NUMERIC_PRECISION"]),
					Scale      = Converter.ChangeTypeTo<int> (c["NUMERIC_SCALE"]),
					IsIdentity = false,
				}
			).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection
				.Query<ForeignKeyInfo>(
					"""
					SELECT
						COALESCE(rc.CONSTRAINT_CATALOG,        '') + '.' + COALESCE(rc.CONSTRAINT_SCHEMA,        '') + '.' + rc.CONSTRAINT_TABLE_NAME        ThisTableID,
						COALESCE(rc.UNIQUE_CONSTRAINT_CATALOG, '') + '.' + COALESCE(rc.UNIQUE_CONSTRAINT_SCHEMA, '') + '.' + rc.UNIQUE_CONSTRAINT_TABLE_NAME OtherTableID,
						rc.CONSTRAINT_NAME                                                                                                                   Name,
						tc.COLUMN_NAME                                                                                                                       ThisColumn,
						oc.COLUMN_NAME                                                                                                                       OtherColumn,
						tc.ORDINAL_POSITION                                                                                                                  Ordinal
					FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
						INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE tc ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
							AND tc.TABLE_NAME = rc.CONSTRAINT_TABLE_NAME
						INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE oc ON oc.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
							AND oc.TABLE_NAME = rc.UNIQUE_CONSTRAINT_TABLE_NAME
							AND tc.ORDINAL_POSITION = oc.ORDINAL_POSITION
					"""
				)
				.ToList();
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(dbConnection.OpenDbConnection().Database);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return (dataType?.ToLowerInvariant()) switch
			{
				"tinyint" => typeof(byte),
				_         => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToLowerInvariant() switch
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
			return dataType switch
			{
				"varbinary"        or
				"rowversion"       or
				"image"            or
				"binary"           => nameof(SqlBinary),
				"tinyint"          => nameof(SqlByte),
				"datetime"         => nameof(SqlDateTime),
				"bit"              => nameof(SqlBoolean),
				"smallint"         => nameof(SqlInt16),
				"numeric"          or
				"decimal"          => nameof(SqlDecimal),
				"int"              => nameof(SqlInt32),
				"real"             => nameof(SqlSingle),
				"float"            => nameof(SqlDouble),
				"money"            => nameof(SqlMoney),
				"bigint"           => nameof(SqlInt64),
				"nvarchar"         or
				"nchar"            or
				"ntext"            => nameof(SqlString),
				"uniqueidentifier" => nameof(SqlGuid),
				_                  => base.GetProviderSpecificType(dataType),
			};
		}
	}
}
