using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Numerics;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	// 1. Foregign keys and procedures: not supported by ClickHouse
	// 2. Functions and table functions: we can get only name from ClickHouse (no parameters information) so it is useless
	// 3. Loading of schema for non-current database: not implemented for now
	public class ClickHouseSchemaProvider : SchemaProviderBase
	{
		protected override string GetServerVersion(DataConnection dbConnection)
		{
			return dbConnection.Execute<string>("SELECT version()");
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var query = from r in dataConnection.GetTable<Column>()
						where r.Database == Functions.CurrentDatabaseName && r.Kind != ColumnKind.Alias
						let typeInfo = PreParseTypeName(r.Type)
						select new ColumnInfo()
						{
							TableID      = r.Table,
							Name         = r.Name,
							IsNullable   = typeInfo.isNullable,
							// TODO: v7 : extend types where we need downcast
							Ordinal      = (int)r.Position,
							DataType     = typeInfo.type,
							ColumnType   = typeInfo.type,
							Length       = r.Type.Contains("FixedString") ? (int?)r.CharacterOctetLength : null,
							Precision    = r.Type.Contains("DateTime64")
								? (int?)r.DateTimePrecision
								: r.NumericPrecisionRadix == 10
									? (int?)r.NumericPrecision
									: null,
							Scale        = r.NumericPrecisionRadix == 10 ? (int?)r.NumericScale : null,
							Description  = string.IsNullOrWhiteSpace(r.Comment) ? null : r.Comment,
							SkipOnUpdate = r.IsInPrimaryKey,
						};
			return query.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options) => [];

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.GetTable<Table>()
				.Where(r => !r.IsTemporary && r.Database == Functions.CurrentDatabaseName && r.PrimaryKey != string.Empty)
				.Select(r => new
				{
					r.Name,
					PrimaryKeyFields = r.PrimaryKey.Split(','),
				})
				.AsEnumerable()
				.SelectMany(r => r.PrimaryKeyFields.Select((f, i) => new PrimaryKeyInfo()
				{
					TableID    = r.Name,
					ColumnName = f.Trim(),
					Ordinal    = i,
				}))
				.ToList();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.GetTable<Table>()
				.Where(r => !r.IsTemporary && r.Database == Functions.CurrentDatabaseName)
				.Select(r => new TableInfo()
				{
					TableID         = r.Name,
					TableName       = r.Name,
					Description     = string.IsNullOrWhiteSpace(r.Comment) ? null : r.Comment,
					IsDefaultSchema = true,
					IsView          = r.Engine.EndsWith("View"),
				})
				.ToList();
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return dbConnection.Execute<string>("select database()");
		}

		protected override string GetDataSourceName(DataConnection dbConnection)
		{
			return dbConnection.Execute<string>("select hostName()");
		}

		#region Types

		protected override string? GetProviderSpecificTypeNamespace() => null;

		// this provider doesn't depend on GetDataTypes API
		static readonly List<DataTypeInfo> _dataTypes = [];
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName) => null;

		protected override DataTypeInfo? GetDataType(string? typeName, DataType? dataType, GetSchemaOptions options) => null;

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale) => GetTypeMapping(dataType, precision, length).dataType;

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options) =>
			GetTypeMapping(dataType, precision, length).type;

		// currently we doesn't handle nested types like tuples and arrays
		private static (string type, bool isNullable, bool lowCardinality) PreParseTypeName(string type)
		{
			// we don't need this information currently, so it is not returned
			var lowCardinality = type.StartsWith("LowCardinality(", StringComparison.Ordinal) && type.EndsWith(')');
			if (lowCardinality)
				type = type.Substring(15, type.Length - 16);

			var isNullable = type.StartsWith("Nullable(", StringComparison.Ordinal) && type.EndsWith(')');
			if (isNullable)
				type = type.Substring(9, type.Length - 10);

			return (type, isNullable, lowCardinality);
		}

		private static (DataType dataType, Type? type) GetTypeMapping(string? dataType, int? precision, int? length)
		{
			if (dataType == null)
				return (DataType.Undefined, null);

			(dataType, _, _) = PreParseTypeName(dataType);

			// fixed-name types
			if (_typeMap.TryGetValue(dataType, out var mapping))
				return mapping;

			// types with parameters
			if (dataType.StartsWith("Enum8(", StringComparison.Ordinal))       return (DataType.Enum8     , typeof(sbyte));
			if (dataType.StartsWith("Enum16(", StringComparison.Ordinal))      return (DataType.Enum16    , typeof(short));

			if (dataType.StartsWith("FixedString(", StringComparison.Ordinal))
				return (DataType.NChar, length is 1 ? typeof(char) : typeof(string));

			if (dataType.StartsWith("DateTime64(", StringComparison.Ordinal))  return (DataType.DateTime64, typeof(DateTimeOffset));

			// ClickHouse actually return Decimal( instead of DecimalX( in schema, but better to implement it
			if (dataType.StartsWith("Decimal32(", StringComparison.Ordinal))   return (DataType.Decimal32 , typeof(decimal));
			// types could store values that doesn't fit decimal
			if (dataType.StartsWith("Decimal64(", StringComparison.Ordinal))   return (DataType.Decimal64 , typeof(decimal));
			if (dataType.StartsWith("Decimal128(", StringComparison.Ordinal))  return (DataType.Decimal128, typeof(decimal));
			if (dataType.StartsWith("Decimal256(", StringComparison.Ordinal))  return (DataType.Decimal256, typeof(decimal));

			if (dataType.StartsWith("Decimal(", StringComparison.Ordinal))
			{
				return precision switch
				{
					< 10 => (DataType.Decimal32,  typeof(decimal)),
					< 19 => (DataType.Decimal64,  typeof(decimal)),
					< 38 => (DataType.Decimal128, typeof(decimal)),
					_    => (DataType.Decimal256, typeof(decimal)),
				};
			}

			return (DataType.Undefined, null);
		}

		// contains only types with fixed name
		private static readonly IReadOnlyDictionary<string, (DataType dataType, Type type)> _typeMap = 
			new Dictionary<string, (DataType dataType, Type type)>(StringComparer.Ordinal)
			{
				// also could store binary data
				{ "String"    , (DataType.NVarChar  , typeof(string        )) },
				{ "JSON"      , (DataType.Json      , typeof(string        )) },
				{ "UUID"      , (DataType.Guid      , typeof(Guid          )) },
				{ "IPv4"      , (DataType.IPv4      , typeof(IPAddress     )) },
				{ "IPv6"      , (DataType.IPv6      , typeof(IPAddress     )) },
				{ "Date"      , (DataType.Date      , typeof(DateTime      )) },
				{ "Date32"    , (DataType.Date32    , typeof(DateTime      )) },
				{ "DateTime"  , (DataType.DateTime  , typeof(DateTimeOffset)) },
				{ "Bool"      , (DataType.Boolean   , typeof(bool          )) },
				{ "Int8"      , (DataType.SByte     , typeof(sbyte         )) },
				{ "UInt8"     , (DataType.Byte      , typeof(byte          )) },
				{ "Int16"     , (DataType.Int16     , typeof(short         )) },
				{ "UInt16"    , (DataType.UInt16    , typeof(ushort        )) },
				{ "Int32"     , (DataType.Int32     , typeof(int           )) },
				{ "UInt32"    , (DataType.UInt32    , typeof(uint          )) },
				{ "Int64"     , (DataType.Int64     , typeof(long          )) },
				{ "UInt64"    , (DataType.UInt64    , typeof(ulong         )) },
				{ "Int128"    , (DataType.Int128    , typeof(BigInteger    )) },
				{ "UInt128"   , (DataType.UInt128   , typeof(BigInteger    )) },
				{ "Int256"    , (DataType.Int256    , typeof(BigInteger    )) },
				{ "UInt256"   , (DataType.UInt256   , typeof(BigInteger    )) },
				{ "Float32"   , (DataType.Single    , typeof(float         )) },
				{ "Float64"   , (DataType.Double    , typeof(double        )) },
			};
		#endregion

		#region Mappings
		// https://clickhouse.com/docs/operations/system-tables/tables
		[Table("tables", Database = "system")]
		sealed class Table
		{
			[Column("name", CanBeNull = false)       ] public string Name        { get; set; } = default!;
			[Column("comment", CanBeNull = false)    ] public string Comment     { get; set; } = default!;
			[Column("engine", CanBeNull = false)     ] public string Engine      { get; set; } = default!;
			[Column("database", CanBeNull = false)   ] public string Database    { get; set; } = default!;
			[Column("primary_key", CanBeNull = false)] public string PrimaryKey  { get; set; } = default!;
			[Column("is_temporary")                  ] public bool   IsTemporary { get; set; }
		}

		// https://clickhouse.com/docs/operations/system-tables/columns
		[Table("columns", Database = "system")]
		sealed class Column
		{
			[Column("database", CanBeNull = false)               ] public string     Database              { get; set; } = default!;
			[Column("table", CanBeNull = false)                  ] public string     Table                 { get; set; } = default!;
			[Column("name", CanBeNull = false)                   ] public string     Name                  { get; set; } = default!;
			[Column("default_kind", CanBeNull = false)           ] public ColumnKind Kind                  { get; set; }
			[Column("type", CanBeNull = false)                   ] public string     Type                  { get; set; } = default!;
			[Column("comment", CanBeNull = false)                ] public string     Comment               { get; set; } = default!;
			[Column("position")                                  ] public ulong      Position              { get; set; }
			[Column("character_octet_length")                    ] public ulong?     CharacterOctetLength  { get; set; }
			[Column("datetime_precision")                        ] public ulong?     DateTimePrecision     { get; set; }
			[Column("numeric_precision_radix")                   ] public ulong?     NumericPrecisionRadix { get; set; }
			[Column("numeric_precision")                         ] public ulong?     NumericPrecision      { get; set; }
			[Column("numeric_scale")                             ] public ulong?     NumericScale          { get; set; }
			[Column("is_in_primary_key")                         ] public bool       IsInPrimaryKey        { get; set; }
		}

		enum ColumnKind
		{
			[MapValue("", IsDefault = true)]
			Empty = 0,
			[MapValue("DEFAULT")]
			Default,
			[MapValue("MATERIALIZED")]
			Materialized,
			[MapValue("ALIAS")]
			Alias,
		}

		static class Functions
		{
			[Sql.Function("database", ServerSideOnly = true, CanBeNull = false)]
			public static string CurrentDatabaseName => throw new ServerSideOnlyException($"{nameof(Functions)}.{nameof(CurrentDatabaseName)}");
		}
		#endregion
	}
}
