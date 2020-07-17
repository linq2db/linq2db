﻿using System;
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
		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
		{
			// TODO: Connection.GetSchema is not supported by MS provider, so we need to implement direct read of metadata
			if (dataConnection.DataProvider.Name == ProviderName.SQLiteMS)
				return new DatabaseSchema()
				{
					DataSource      = string.Empty,
					Database        = string.Empty,
					ServerVersion   = string.Empty,
					Tables          = new List<TableSchema>(),
					Procedures      = new List<ProcedureSchema>(),
					DataTypesSchema = new DataTable()
				};

			return base.GetSchema(dataConnection, options);
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views =  ((DbConnection)dataConnection.Connection).GetSchema("Views");

			return Enumerable
				.Empty<TableInfo>()
				.Concat
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
				)
				.Concat(
					from t in views.AsEnumerable()
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
						IsView          = true,
					}
				).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				let tschema  = c.Field<string>("TABLE_SCHEMA")
				let schema   = tschema == "sqlite_default_schema" ? "" : tschema
				let dataType = c.Field<string>("DATA_TYPE").Trim()
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + schema + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					IsNullable   = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					DataType     = dataType,
					Length       = Converter.ChangeTypeTo<long>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int> (c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int> (c["NUMERIC_SCALE"]),
					IsIdentity   = c.Field<bool>  ("AUTOINCREMENT"),
					SkipOnInsert = dataType == "timestamp",
					SkipOnUpdate = dataType == "timestamp",
				}
			).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var fks = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeys");

			var result =
			(
				from fk in fks.AsEnumerable()
				where fk.Field<string>("CONSTRAINT_TYPE") == "FOREIGN KEY"
				select new ForeignKeyInfo
				{
					Name         = fk.Field<string>("CONSTRAINT_NAME"           ),
					ThisTableID  = fk.Field<string>("TABLE_CATALOG"             ) + "." + fk.Field<string>("TABLE_SCHEMA")   + "." + fk.Field<string>("TABLE_NAME"),
					ThisColumn   = fk.Field<string>("FKEY_FROM_COLUMN"          ),
					OtherTableID = fk.Field<string>("FKEY_TO_CATALOG"           ) + "." + fk.Field<string>("FKEY_TO_SCHEMA") + "." + fk.Field<string>("FKEY_TO_TABLE"),
					OtherColumn  = fk.Field<string>("FKEY_TO_COLUMN"            ),
					Ordinal      = fk.Field<int>   ("FKEY_FROM_ORDINAL_POSITION"),
				}
			).ToList();

			// Handle case where Foreign Key reference does not include a column name (Issue #784)
			if (result.Any(fk => string.IsNullOrEmpty(fk.OtherColumn)))
			{
				var pks = GetPrimaryKeys(dataConnection, tables, options).ToDictionary(pk => string.Format("{0}:{1}", pk.TableID, pk.Ordinal), pk => pk.ColumnName);
				foreach (var f in result.Where(fk => string.IsNullOrEmpty(fk.OtherColumn)))
				{
					var k = string.Format("{0}:{1}", f.OtherTableID, f.Ordinal);
					if (pks.ContainsKey(k))
						f.OtherColumn = pks[k];
				}
			}
			return result;
		}

		protected override string GetDatabaseName(DataConnection connection)
		{
			return ((DbConnection)connection.Connection).DataSource;
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			return dataType switch
			{
				"smallint"         => DataType.Int16,
				"int"              => DataType.Int32,
				"real"             => DataType.Single,
				"float"            => DataType.Double,
				"double"           => DataType.Double,
				"money"            => DataType.Money,
				"currency"         => DataType.Money,
				"decimal"          => DataType.Decimal,
				"numeric"          => DataType.Decimal,
				"bit"              => DataType.Boolean,
				"yesno"            => DataType.Boolean,
				"logical"          => DataType.Boolean,
				"bool"             => DataType.Boolean,
				"boolean"          => DataType.Boolean,
				"tinyint"          => DataType.Byte,
				"integer"          => DataType.Int64,
				"counter"          => DataType.Int64,
				"autoincrement"    => DataType.Int64,
				"identity"         => DataType.Int64,
				"long"             => DataType.Int64,
				"bigint"           => DataType.Int64,
				"binary"           => DataType.Binary,
				"varbinary"        => DataType.VarBinary,
				"blob"             => DataType.VarBinary,
				"image"            => DataType.Image,
				"general"          => DataType.VarBinary,
				"oleobject"        => DataType.VarBinary,
				"varchar"          => DataType.VarChar,
				"nvarchar"         => DataType.NVarChar,
				"memo"             => DataType.Text,
				"longtext"         => DataType.Text,
				"note"             => DataType.Text,
				"text"             => DataType.Text,
				"ntext"            => DataType.NText,
				"string"           => DataType.Char,
				"char"             => DataType.Char,
				"nchar"            => DataType.NChar,
				"datetime"         => DataType.DateTime,
				"datetime2"        => DataType.DateTime2,
				"smalldate"        => DataType.SmallDateTime,
				"timestamp"        => DataType.Timestamp,
				"date"             => DataType.Date,
				"time"             => DataType.Time,
				"uniqueidentifier" => DataType.Guid,
				"guid"             => DataType.Guid,
				_                  => DataType.Undefined,
			};
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, long? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType switch
			{
				"datetime2" => typeof(DateTime),
				_ => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}
	}
}
