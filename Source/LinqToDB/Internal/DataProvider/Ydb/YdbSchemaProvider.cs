using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	// YDB exposes no SQL-queryable schema catalog (no information_schema; .sys views are monitoring-only).
	// Table/column metadata is read through the provider's ADO GetSchema collections (which the Ydb.Sdk driver
	// implements over the gRPC scheme/table service); primary keys are read from the open connection's gRPC
	// table description via the data provider (GetSchema "Columns" doesn't expose PK). Foreign keys and
	// procedures don't exist in YDB.
	public class YdbSchemaProvider : SchemaProviderBase
	{
		protected override string GetServerVersion(DataConnection dbConnection)
		{
			return dbConnection.Execute<string>("SELECT Version();");
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var dt     = dataConnection.OpenDbConnection().GetSchema("Tables");
			var tables = new List<TableInfo>();

			foreach (DataRow row in dt.Rows)
			{
				var name = (string)row["table_name"];
				var type = row["table_type"] as string;

				if (IsSystemPath(name) || string.Equals(type, "SYSTEM_TABLE", StringComparison.Ordinal))
					continue;

				// table_name is the path relative to the database root; the directory prefix is the YDB
				// "schema" and the leaf is the table name (mirrors YdbSqlBuilder.BuildObjectName: /Database/Schema/Name).
				var (schemaName, leaf) = SplitPath(name);

				tables.Add(new TableInfo()
				{
					TableID         = name,
					SchemaName      = schemaName,
					TableName       = leaf,
					IsView          = string.Equals(type, "VIEW", StringComparison.Ordinal),
					IsDefaultSchema = schemaName == null,
				});
			}

			return tables;
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var dt      = dataConnection.OpenDbConnection().GetSchema("Columns");
			var columns = new List<ColumnInfo>();

			foreach (DataRow row in dt.Rows)
			{
				var table = (string)row["table_name"];

				if (IsSystemPath(table))
					continue;

				var raw                          = (string)row["data_type"];
				var (typeName, precision, scale) = ParseType(raw);

				columns.Add(new ColumnInfo()
				{
					TableID    = table,
					Name       = (string)row["column_name"],
					Ordinal    = Convert.ToInt32(row["ordinal_position"], CultureInfo.InvariantCulture),
					IsNullable = string.Equals(row["is_nullable"] as string, "YES", StringComparison.OrdinalIgnoreCase),
					DataType   = typeName,
					ColumnType = raw,
					Precision  = precision,
					Scale      = scale,
				});
			}

			return columns;
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var tableIds = tables.Where(t => t.ID != null).Select(t => t.ID!).ToList();
			if (tableIds.Count == 0)
				return [];

			// one describe pass for the whole batch (the gRPC driver is resolved once)
			var pkByTable = YdbProviderAdapter.Instance.GetPrimaryKeys(dataConnection.OpenDbConnection(), tableIds);
			var result    = new List<PrimaryKeyInfo>();

			foreach (var pk in pkByTable)
			{
				var ordinal = 0;
				foreach (var column in pk.Value)
					result.Add(new PrimaryKeyInfo()
					{
						TableID    = pk.Key,
						ColumnName = column,
						Ordinal    = ordinal++,
					});
			}

			return result;
		}

		// YDB has no foreign keys.
		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options) => [];

		static bool IsSystemPath(string path) => path.StartsWith(".sys", StringComparison.Ordinal);

		// splits a YDB table path into (directory schema, leaf table name):
		// "dir/sub/Customers" => ("dir/sub", "Customers");  "Customers" => (null, "Customers")
		static (string? schema, string name) SplitPath(string path)
		{
			var idx = path.LastIndexOf('/');
			return idx < 0 ? (null, path) : (path[..idx], path[(idx + 1)..]);
		}

		// parses "Decimal(22, 9)" => ("Decimal", 22, 9); "Int32" => ("Int32", null, null)
		static (string name, int? precision, int? scale) ParseType(string raw)
		{
			var open = raw.IndexOf('(', StringComparison.Ordinal);
			if (open < 0)
				return (raw, null, null);

			var name  = raw.Substring(0, open);
			var inner = raw.Substring(open + 1, raw.Length - open - 2);
			var parts = inner.Split(',');

			int? precision = null, scale = null;
			if (parts.Length > 0 && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var p))
				precision = p;
			if (parts.Length > 1 && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
				scale = s;

			return (name, precision, scale);
		}

		#region Types

		protected override string? GetProviderSpecificTypeNamespace() => null;

		// this provider doesn't depend on GetDataTypes API
		static readonly List<DataTypeInfo> _dataTypes = [];
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName) => null;

		protected override DataTypeInfo? GetDataType(string? typeName, DataType? dataType, GetSchemaOptions options) => null;

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale) => GetTypeMapping(dataType).dataType;

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options) =>
			GetTypeMapping(dataType).type;

		static (DataType dataType, Type? type) GetTypeMapping(string? dataType)
		{
			if (dataType == null)
				return (DataType.Undefined, null);

			var (name, _, _) = ParseType(dataType);

			return _typeMap.TryGetValue(name, out var mapping) ? mapping : (DataType.Undefined, null);
		}

		// YQL type names as reported by the driver (SchemaUtils.YqlTableType): Utf8 => "Text", String => "Bytes",
		// Decimal => "Decimal(p, s)", everything else => primitive enum name.
		static readonly IReadOnlyDictionary<string, (DataType dataType, Type type)> _typeMap =
			new Dictionary<string, (DataType dataType, Type type)>(StringComparer.Ordinal)
			{
				{ "Bool"        , (DataType.Boolean    , typeof(bool    )) },
				{ "Int8"        , (DataType.SByte      , typeof(sbyte   )) },
				{ "Int16"       , (DataType.Int16      , typeof(short   )) },
				{ "Int32"       , (DataType.Int32      , typeof(int     )) },
				{ "Int64"       , (DataType.Int64      , typeof(long    )) },
				{ "Uint8"       , (DataType.Byte       , typeof(byte    )) },
				{ "Uint16"      , (DataType.UInt16     , typeof(ushort  )) },
				{ "Uint32"      , (DataType.UInt32     , typeof(uint    )) },
				{ "Uint64"      , (DataType.UInt64     , typeof(ulong   )) },
				{ "Float"       , (DataType.Single     , typeof(float   )) },
				{ "Double"      , (DataType.Double     , typeof(double  )) },
				{ "Decimal"     , (DataType.Decimal    , typeof(decimal )) },
				{ "DyNumber"    , (DataType.DecFloat   , typeof(decimal )) },
				{ "Text"        , (DataType.NVarChar   , typeof(string  )) },
				{ "Bytes"       , (DataType.VarBinary  , typeof(byte[]  )) },
				{ "Json"        , (DataType.Json       , typeof(string  )) },
				{ "JsonDocument", (DataType.BinaryJson , typeof(string  )) },
				{ "Yson"        , (DataType.Yson       , typeof(string  )) },
				{ "Uuid"        , (DataType.Guid       , typeof(Guid    )) },
				{ "Date"        , (DataType.Date       , typeof(DateTime)) },
				{ "Datetime"    , (DataType.DateTime   , typeof(DateTime)) },
				{ "Timestamp"   , (DataType.DateTime2  , typeof(DateTime)) },
				{ "Date32"      , (DataType.Date32     , typeof(DateTime)) },
				{ "Datetime64"  , (DataType.DateTime64 , typeof(DateTime)) },
				{ "Timestamp64" , (DataType.Timestamp64, typeof(DateTime)) },
				{ "Interval"    , (DataType.Interval   , typeof(TimeSpan)) },
				{ "Interval64"  , (DataType.Interval64 , typeof(TimeSpan)) },
			};

		#endregion
	}
}
