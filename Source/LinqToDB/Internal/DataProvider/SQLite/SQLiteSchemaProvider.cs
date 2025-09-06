using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query<TableInfo>(@"
				SELECT
					t.schema || '..' || t.name as TableID,
					'' as CatalogName,
					t.schema as SchemaName,
					t.name as TableName,
					t.schema = 'main' as IsDefaultSchema
				FROM pragma_table_list() t
				WHERE t.type IN ('table', 'view')
				;
			").ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query<PrimaryKeyInfo>(@"
				SELECT
					t.schema || '..' || t.name as TableID,
					i.name AS PrimaryKeyName,
					c.name AS ColumnName,
					c.pk - 1 AS Ordinal
				FROM pragma_table_list() t
				LEFT OUTER JOIN pragma_table_info(t.name) c
				LEFT OUTER JOIN pragma_index_list(t.name) i ON i.origin = 'pk'
				WHERE t.type IN ('table', 'view') AND c.pk != 0
				;
			").ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection
				.Query<ColumnInfo>(@"
					WITH pk_counts AS (
						SELECT t.name AS table_name, COUNT(*) AS pk_count
						FROM pragma_table_list() t
						JOIN pragma_table_info(t.name) c
						WHERE c.pk > 0
						GROUP BY t.name
					)
					SELECT
						t.schema || '..' || t.name as TableID,
						c.name as Name,
						c.[notnull] = 0 as IsNullable,
						c.cid as Ordinal,
						c.type as DataType,
						(pk.pk_count = 1 AND c.pk = 1 AND UPPER(c.type) = 'INTEGER' AND m.sql LIKE '%AUTOINCREMENT%') as [IsIdentity]						
					FROM pragma_table_list() t
					LEFT OUTER JOIN pragma_table_info(t.name) c
					INNER JOIN sqlite_master m ON m.tbl_name = t.name AND m.type IN ('table', 'view')
					LEFT JOIN pk_counts pk ON pk.table_name = t.name
					WHERE t.type IN ('table', 'view');
				")
				.Select(x =>
				{
					var (length, precision, scale) = GetLengthPrecisionScale(x.DataType!);
					x.Length = length;
					x.Precision = precision;
					x.Scale = scale;
					return x;
				})
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query<ForeignKeyInfo>(@"
				SELECT
					'FK_' || tThis.name || '_' || f.id || '_' || f.seq as Name,
					tThis.schema || '..' || tThis.name as ThisTableID,
					f.[from] as ThisColumn,
					tOther.schema || '..' || tOther.name as OtherTableID,
					coalesce(f.[to], cOther.name) as OtherColumn,
					f.seq as Ordinal
				FROM pragma_table_list() tThis
				LEFT OUTER JOIN pragma_foreign_key_list(tThis.name) f
				INNER JOIN pragma_table_list() tOther on f.[table] = tOther.name
				LEFT JOIN pragma_table_info(tOther.name) cOther ON (cOther.pk -1) == f.seq
				WHERE tThis.type IN ('table', 'view');
			").ToList();
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return dbConnection.OpenDbConnection().DataSource;
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			// note that sqlite doesn't have types (it has facets) so type name will contain anything
			// user specified in create table statement
			// here we just map some well-known database types (non-sqlite specific) but this list
			// will never be complete
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
				"object"           => DataType.Variant,
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

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType switch
			{
				"object"    => typeof(object),
				"datetime2" => typeof(DateTime),
				_ => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		static (int? length, int? precision, int? scale) GetLengthPrecisionScale(string dataType)
		{
			int? length    = null;
			int? precision = null;
			int? scale     = null;

			if (string.IsNullOrWhiteSpace(dataType))
			{
				return (null, null, null);
			}

			string affinityType = GetAffinityType(dataType);

			if (affinityType == "NUMERIC")
			{
				// For NUMERIC affinity, try to parse both precision and scale.
				// E.g., "DECIMAL(10, 2)"
				var match = Regex.Match(dataType, @"\((?<precision>\d+)\s*,\s*(?<scale>\d+)\)");
				if (match.Success)
				{
					precision = int.Parse(match.Groups["precision"].Value, CultureInfo.InvariantCulture);
					scale     = int.Parse(match.Groups["scale"].Value,     CultureInfo.InvariantCulture);
				}
			}
			else
			{
				// For all other affinities, try to parse a single length value.
				// The first number is considered the length.
				// E.g., "VARCHAR(16)"
				var match = Regex.Match(dataType, @"\((?<length>\d+)\)");
				if (match.Success)
				{
					length = int.Parse(match.Groups["length"].Value, CultureInfo.InvariantCulture);
				}
			}

			return (length, precision, scale);
		}

		// https://www.sqlite.org/datatype3.html Point 3.1
		static string GetAffinityType(string dataType)
		{
			// If the data type is null or whitespace, it's considered BLOB.
			if (string.IsNullOrWhiteSpace(dataType))
			{
				return "BLOB";
			}

			// Convert the data type to uppercase for case-insensitive comparison.
			var upperDataType = dataType.ToUpper(CultureInfo.InvariantCulture);

			// 1. Check for INTEGER affinity.
			if (upperDataType.Contains("INT"))
			{
				return "INTEGER";
			}

			// 2. Check for TEXT affinity.
			if (upperDataType.Contains("CHAR") || upperDataType.Contains("CLOB") || upperDataType.Contains("TEXT"))
			{
				return "TEXT";
			}

			// 3. Check for BLOB affinity.
			if (upperDataType.Contains("BLOB"))
			{
				return "BLOB";
			}

			// 4. Check for REAL affinity.
			if (upperDataType.Contains("REAL") || upperDataType.Contains("FLOA") || upperDataType.Contains("DOUB"))
			{
				return "REAL";
			}

			// 5. Otherwise, the affinity is NUMERIC.
			return "NUMERIC";
		}
	}
}
