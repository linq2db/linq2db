using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	public class DuckDBSchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return new List<DataTypeInfo>
			{
				new() { TypeName = "BOOLEAN",    DataType = typeof(bool).           AssemblyQualifiedName! },
				new() { TypeName = "TINYINT",    DataType = typeof(sbyte).          AssemblyQualifiedName! },
				new() { TypeName = "SMALLINT",   DataType = typeof(short).          AssemblyQualifiedName! },
				new() { TypeName = "INTEGER",    DataType = typeof(int).            AssemblyQualifiedName! },
				new() { TypeName = "BIGINT",     DataType = typeof(long).           AssemblyQualifiedName! },
				new() { TypeName = "UTINYINT",   DataType = typeof(byte).           AssemblyQualifiedName! },
				new() { TypeName = "USMALLINT",  DataType = typeof(ushort).         AssemblyQualifiedName! },
				new() { TypeName = "UINTEGER",   DataType = typeof(uint).           AssemblyQualifiedName! },
				new() { TypeName = "UBIGINT",    DataType = typeof(ulong).          AssemblyQualifiedName! },
				new() { TypeName = "FLOAT",      DataType = typeof(float).          AssemblyQualifiedName! },
				new() { TypeName = "DOUBLE",     DataType = typeof(double).         AssemblyQualifiedName! },
				new() { TypeName = "DECIMAL",    DataType = typeof(decimal).        AssemblyQualifiedName!, CreateParameters = "precision,scale" },
				new() { TypeName = "VARCHAR",    DataType = typeof(string).         AssemblyQualifiedName!, CreateParameters = "length" },
				new() { TypeName = "BLOB",       DataType = typeof(byte[]).         AssemblyQualifiedName! },
				new() { TypeName = "DATE",       DataType = typeof(DateTime).       AssemblyQualifiedName! },
				new() { TypeName = "TIME",       DataType = typeof(TimeSpan).       AssemblyQualifiedName! },
				new() { TypeName = "TIMESTAMP",  DataType = typeof(DateTime).       AssemblyQualifiedName! },
				new() { TypeName = "TIMESTAMPTZ",DataType = typeof(DateTimeOffset). AssemblyQualifiedName! },
				new() { TypeName = "INTERVAL",   DataType = typeof(TimeSpan).       AssemblyQualifiedName! },
				new() { TypeName = "UUID",       DataType = typeof(Guid).           AssemblyQualifiedName! },
				new() { TypeName = "JSON",       DataType = typeof(string).         AssemblyQualifiedName! },
				new() { TypeName = "HUGEINT",    DataType = typeof(decimal).        AssemblyQualifiedName! },
			};
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.GetTable<InformationSchemaTable>()
				.Where(t => t.TableSchema != "information_schema" && t.TableSchema != "pg_catalog")
				.OrderBy(t => t.TableSchema).ThenBy(t => t.TableName)
				.Select(t => new TableInfo
				{
					TableID         = t.TableCatalog + "." + t.TableSchema + "." + t.TableName,
					CatalogName     = t.TableCatalog,
					SchemaName      = t.TableSchema,
					TableName       = t.TableName,
					IsDefaultSchema = t.TableSchema == "main",
					IsView          = t.TableType == "VIEW",
				})
				.ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// duckdb_constraints() is a table function, not a regular table — must use raw SQL with unnest
			return dataConnection.Query(
				rd => new PrimaryKeyInfo
				{
					TableID        = rd.GetString(0) + "." + rd.GetString(1) + "." + rd.GetString(2),
					PrimaryKeyName = rd.GetString(3),
					ColumnName     = rd.GetString(4),
					Ordinal        = rd.GetInt32(5),
				},
				@"SELECT
	database_name,
	schema_name,
	table_name,
	constraint_text,
	unnest(constraint_column_names) AS column_name,
	generate_subscripts(constraint_column_names, 1) AS ordinal
FROM duckdb_constraints()
WHERE constraint_type = 'PRIMARY KEY'")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.GetTable<InformationSchemaColumn>()
				.Where(c => c.TableSchema != "information_schema" && c.TableSchema != "pg_catalog")
				.OrderBy(c => c.TableSchema).ThenBy(c => c.TableName).ThenBy(c => c.OrdinalPosition)
				.Select(c => new ColumnInfo
				{
					TableID    = c.TableCatalog + "." + c.TableSchema + "." + c.TableName,
					Name       = c.ColumnName,
					IsNullable = c.IsNullable == "YES",
					Ordinal    = c.OrdinalPosition,
					DataType   = c.DataType,
					Length     = c.CharacterMaximumLength,
					Precision  = c.NumericPrecision,
					Scale      = c.NumericScale,
					IsIdentity = c.ColumnDefault != null && c.ColumnDefault.Contains("nextval"),
				})
				.AsEnumerable()
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// DuckDB does not enforce foreign key constraints
			return [];
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return dbConnection.Execute<string>("SELECT current_database()");
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType?.ToUpperInvariant() switch
			{
				"BOOLEAN"    => typeof(bool),
				"TINYINT"    => typeof(sbyte),
				"SMALLINT"   => typeof(short),
				"INTEGER"    => typeof(int),
				"BIGINT"     => typeof(long),
				"UTINYINT"   => typeof(byte),
				"USMALLINT"  => typeof(ushort),
				"UINTEGER"   => typeof(uint),
				"UBIGINT"    => typeof(ulong),
				"FLOAT"      => typeof(float),
				"DOUBLE"     => typeof(double),
				"DECIMAL"    => typeof(decimal),
				"VARCHAR"    => typeof(string),
				"BLOB"       => typeof(byte[]),
				"DATE"       => typeof(DateTime),
				"TIME"       => typeof(TimeSpan),
				"TIMESTAMP"  => typeof(DateTime),
				"TIMESTAMPTZ"=> typeof(DateTimeOffset),
				"INTERVAL"   => typeof(TimeSpan),
				"UUID"       => typeof(Guid),
				"JSON"       => typeof(string),
				"HUGEINT"    => typeof(decimal),
				_            => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToUpperInvariant() switch
			{
				"BOOLEAN"    => DataType.Boolean,
				"TINYINT"    => DataType.SByte,
				"SMALLINT"   => DataType.Int16,
				"INTEGER"    => DataType.Int32,
				"BIGINT"     => DataType.Int64,
				"UTINYINT"   => DataType.Byte,
				"USMALLINT"  => DataType.UInt16,
				"UINTEGER"   => DataType.UInt32,
				"UBIGINT"    => DataType.UInt64,
				"FLOAT"      => DataType.Single,
				"DOUBLE"     => DataType.Double,
				"DECIMAL"    => DataType.Decimal,
				"VARCHAR"    => DataType.NVarChar,
				"BLOB"       => DataType.VarBinary,
				"DATE"       => DataType.Date,
				"TIME"       => DataType.Time,
				"TIMESTAMP"  => DataType.DateTime2,
				"TIMESTAMPTZ"=> DataType.DateTimeOffset,
				"INTERVAL"   => DataType.Interval,
				"UUID"       => DataType.Guid,
				"JSON"       => DataType.Json,
				"HUGEINT"    => DataType.VarNumeric,
				_            => DataType.Undefined,
			};
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		#region DTOs

		[Table("tables", Schema = "information_schema")]
		sealed class InformationSchemaTable
		{
			[Column("table_catalog", CanBeNull = false)] public string TableCatalog { get; set; } = default!;
			[Column("table_schema",  CanBeNull = false)] public string TableSchema  { get; set; } = default!;
			[Column("table_name",    CanBeNull = false)] public string TableName    { get; set; } = default!;
			[Column("table_type",    CanBeNull = false)] public string TableType    { get; set; } = default!;
		}

		[Table("columns", Schema = "information_schema")]
		sealed class InformationSchemaColumn
		{
			[Column("table_catalog",             CanBeNull = false)] public string  TableCatalog           { get; set; } = default!;
			[Column("table_schema",              CanBeNull = false)] public string  TableSchema            { get; set; } = default!;
			[Column("table_name",                CanBeNull = false)] public string  TableName              { get; set; } = default!;
			[Column("column_name",               CanBeNull = false)] public string  ColumnName             { get; set; } = default!;
			[Column("ordinal_position")                            ] public int     OrdinalPosition        { get; set; }
			[Column("column_default")                              ] public string? ColumnDefault          { get; set; }
			[Column("is_nullable",               CanBeNull = false)] public string  IsNullable             { get; set; } = default!;
			[Column("data_type",                 CanBeNull = false)] public string  DataType               { get; set; } = default!;
			[Column("character_maximum_length")                    ] public int?    CharacterMaximumLength { get; set; }
			[Column("numeric_precision")                           ] public int?    NumericPrecision       { get; set; }
			[Column("numeric_scale")                               ] public int?    NumericScale           { get; set; }
		}

		#endregion
	}
}
