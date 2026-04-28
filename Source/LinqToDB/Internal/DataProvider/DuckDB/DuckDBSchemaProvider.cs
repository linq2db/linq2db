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
		readonly string[] SystemCatalogs = ["system", "temp"];
		const string DEFAULT_SCHEMA = "main";

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
			var tables = dataConnection.GetTable<InformationSchemaTable>()
				.Where(t => !t.TableCatalog.In(SystemCatalogs))
				.OrderBy(t => t.TableSchema).ThenBy(t => t.TableName)
				.Select(t => new TableInfo
				{
					TableID         = t.TableCatalog + "." + t.TableSchema + "." + t.TableName,
					CatalogName     = t.TableCatalog,
					SchemaName      = t.TableSchema,
					TableName       = t.TableName,
					IsDefaultSchema = t.TableSchema == DEFAULT_SCHEMA,
					IsView          = t.TableType == TableType.View,
					Description     = t.Comment,
				})
				.ToList();

			return tables;
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var constraints = dataConnection.GetTable<InformationSchemaConstraintColumnUsage>()
				.Where(r => !r.TableCatalog.In(SystemCatalogs) && r.ConstraintType == ConstraintType.PrimaryKey)
				.Select(r => new { r.TableCatalog, r.TableSchema, r.TableName, r.ConstraintName })
				.Distinct();
			var keyColumns  = dataConnection.GetTable<InformationSchemaKeyColumnUsage>();

			return (
				from tc in constraints
				join kcu in keyColumns on new { tc.TableCatalog, tc.TableSchema, tc.TableName, tc.ConstraintName }
					equals new { kcu.TableCatalog, kcu.TableSchema, kcu.TableName, kcu.ConstraintName }
				select new PrimaryKeyInfo
				{
					TableID        = kcu.TableCatalog + "." + kcu.TableSchema + "." + kcu.TableName,
					PrimaryKeyName = tc.ConstraintName,
					ColumnName     = kcu.ColumnName,
					Ordinal        = kcu.OrdinalPosition,
				})
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var columns = dataConnection.GetTable<InformationSchemaColumn>()
				.Where(r => !r.TableCatalog.In(SystemCatalogs))
				.OrderBy(c => c.TableSchema).ThenBy(c => c.TableName).ThenBy(c => c.OrdinalPosition)
				.Select(c => new ColumnInfo
				{
					TableID     = c.TableCatalog + "." + c.TableSchema + "." + c.TableName,
					Name        = c.ColumnName,
					IsNullable  = c.IsNullable == IsNullable.Yes,
					Ordinal     = c.OrdinalPosition,
					DataType    = c.DataType,
					Precision   = c.NumericPrecision,
					Scale       = c.NumericScale,
					IsIdentity  = c.ColumnDefault != null && c.ColumnDefault.Contains("nextval"),
					Description = c.Comment,
				})
				.ToList();

			foreach (var col in columns)
			{
				if (col.DataType?.StartsWith("DECIMAL(", StringComparison.Ordinal) == true)
				{
					col.DataType = "DECIMAL";
				}
			}

			return columns;
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var refConstraints = dataConnection.GetTable<InformationSchemaConstraintColumnUsage>()
				.Where(r => !r.TableCatalog.In(SystemCatalogs) && r.ConstraintType == ConstraintType.ForeignKey)
				.Select(r => new { r.TableCatalog, r.TableSchema, r.TableName, r.ConstraintCatalog, r.ConstraintSchema, r.ConstraintName })
				.Distinct();
			var keyColumns     = dataConnection.GetTable<InformationSchemaKeyColumnUsage>();
			var keyMap         = dataConnection.GetTable<InformationSchemaReferentialConstraints>();

			return (
				from rc in refConstraints
				join map in keyMap on new { rc.ConstraintCatalog, rc.ConstraintSchema, rc.ConstraintName }
					equals new { map.ConstraintCatalog, map.ConstraintSchema, map.ConstraintName }
				join fk in keyColumns on new { rc.TableCatalog, rc.TableSchema, rc.TableName, rc.ConstraintName }
					equals new { fk.TableCatalog, fk.TableSchema, fk.TableName, fk.ConstraintName }
				join pk in keyColumns on new
					{
						ConstraintCatalog = map.TargetConstraintCatalog,
						ConstraintSchema  = map.TargetConstraintSchema,
						ConstraintName    = map.TargetConstraintName,
						OrdinalPosition   = fk.OrdinalPosition,
					}
					equals new { pk.ConstraintCatalog, pk.ConstraintSchema, pk.ConstraintName, pk.OrdinalPosition }
				select new ForeignKeyInfo
				{
					Name         = rc.ConstraintName,
					ThisTableID  = fk.TableCatalog + "." + fk.TableSchema + "." + fk.TableName,
					ThisColumn   = fk.ColumnName,
					OtherTableID = pk.TableCatalog + "." + pk.TableSchema + "." + pk.TableName,
					OtherColumn  = pk.ColumnName,
					Ordinal      = fk.OrdinalPosition,
				})
				.ToList();
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
			[Column("table_catalog", CanBeNull = false)] public string    TableCatalog { get; set; } = default!;
			[Column("table_schema",  CanBeNull = false)] public string    TableSchema  { get; set; } = default!;
			[Column("table_name",    CanBeNull = false)] public string    TableName    { get; set; } = default!;
			[Column("table_type",    CanBeNull = false)] public TableType TableType    { get; set; } = default!;
			[Column("TABLE_COMMENT")                   ] public string?   Comment      { get; set; }
		}

		enum TableType
		{
			[MapValue("BASE TABLE")]
			Table,
			[MapValue("VIEW")]
			View,
			[MapValue("LOCAL TEMPORARY")]
			Temp,
			[MapValue("UNKNOWN", IsDefault = true)]
			Other,
		}

		[Table("columns", Schema = "information_schema")]
		sealed class InformationSchemaColumn
		{
			[Column("table_catalog",             CanBeNull = false)] public string     TableCatalog           { get; set; } = default!;
			[Column("table_schema",              CanBeNull = false)] public string     TableSchema            { get; set; } = default!;
			[Column("table_name",                CanBeNull = false)] public string     TableName              { get; set; } = default!;
			[Column("column_name",               CanBeNull = false)] public string     ColumnName             { get; set; } = default!;
			[Column("ordinal_position")                            ] public int        OrdinalPosition        { get; set; }
			[Column("column_default")                              ] public string?    ColumnDefault          { get; set; }
			[Column("is_nullable",               CanBeNull = false)] public IsNullable IsNullable             { get; set; } = default!;
			[Column("data_type",                 CanBeNull = false)] public string     DataType               { get; set; } = default!;
			[Column("numeric_precision")                           ] public int?       NumericPrecision       { get; set; }
			[Column("numeric_scale")                               ] public int?       NumericScale           { get; set; }
			[Column("COLUMN_COMMENT")                              ] public string?    Comment                { get; set; }
			// not implemented and contain NULL:
			// - character_maximum_length
			// - datetime_precision
		}

		enum IsNullable
		{
			[MapValue("NO")]
			No,
			[MapValue("YES")]
			Yes,
			[MapValue("UNKNOWN", IsDefault = true)]
			Other,
		}

		[Table("key_column_usage", Schema = "information_schema")]
		sealed class InformationSchemaKeyColumnUsage
		{
			[Column("constraint_catalog", CanBeNull = false)] public string ConstraintCatalog { get; set; } = default!;
			[Column("constraint_schema",  CanBeNull = false)] public string ConstraintSchema  { get; set; } = default!;
			[Column("constraint_name",    CanBeNull = false)] public string ConstraintName    { get; set; } = default!;
			[Column("table_catalog",      CanBeNull = false)] public string TableCatalog      { get; set; } = default!;
			[Column("table_schema",       CanBeNull = false)] public string TableSchema       { get; set; } = default!;
			[Column("table_name",         CanBeNull = false)] public string TableName         { get; set; } = default!;
			[Column("column_name",        CanBeNull = false)] public string ColumnName        { get; set; } = default!;
			[Column("ordinal_position")                     ] public int    OrdinalPosition   { get; set; }
		}

		[Table("constraint_column_usage", Schema = "information_schema")]
		sealed class InformationSchemaConstraintColumnUsage
		{
			[Column("table_catalog",             CanBeNull = false)] public string          TableCatalog          { get; set; } = default!;
			[Column("table_schema",              CanBeNull = false)] public string          TableSchema           { get; set; } = default!;
			[Column("table_name",                CanBeNull = false)] public string          TableName             { get; set; } = default!;
			[Column("column_name",               CanBeNull = false)] public string          ColumnName            { get; set; } = default!;
			[Column("constraint_catalog",        CanBeNull = false)] public string         ConstraintCatalog      { get; set; } = default!;
			[Column("constraint_schema",         CanBeNull = false)] public string         ConstraintSchema       { get; set; } = default!;
			[Column("constraint_name",           CanBeNull = false)] public string                 ConstraintName { get; set; } = default!;
			[Column("constraint_type",           CanBeNull = false)] public ConstraintType ConstraintType         { get; set; } = default!;
		}

		enum ConstraintType
		{
			[MapValue("PRIMARY KEY")]
			PrimaryKey,
			[MapValue("FOREIGN KEY")]
			ForeignKey,
			[MapValue("UNKNOWN", IsDefault = true)]
			Other,
		}

		[Table("referential_constraints", Schema = "information_schema")]
		sealed class InformationSchemaReferentialConstraints
		{
			[Column("constraint_catalog", CanBeNull = false)       ] public string ConstraintCatalog       { get; set; } = default!;
			[Column("constraint_schema", CanBeNull = false)        ] public string ConstraintSchema        { get; set; } = default!;
			[Column("constraint_name", CanBeNull = false)          ] public string ConstraintName          { get; set; } = default!;
			[Column("unique_constraint_catalog", CanBeNull = false)] public string TargetConstraintCatalog { get; set; } = default!;
			[Column("unique_constraint_schema", CanBeNull = false) ] public string TargetConstraintSchema  { get; set; } = default!;
			[Column("unique_constraint_name", CanBeNull = false)   ] public string TargetConstraintName    { get; set; } = default!;
		}

		#endregion
	}
}
