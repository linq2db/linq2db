using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Common;
	using Data;
	using SchemaProvider;

	// 1. Foregign keys and procedures: not supported by ClickHouse
	// 2. Functions and table functions: we can get only name from ClickHouse (no parameters information) so it is useless
	// 3. Loading of schema for non-current database: not implemented for now
	sealed class ClickHouseSchemaProvider : SchemaProviderBase
	{
		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query(
				rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var table       = rd.GetString(0);
					var name        = rd.GetString(1);
					var type        = rd.GetString(2);
					var ordinal     = Convert.ToUInt64(rd.GetValue(3), CultureInfo.InvariantCulture);
					var description = rd.GetString(4);
					var length      = rd.IsDBNull(5) ? null : (ulong?)Convert.ToUInt64(rd.GetValue(5), CultureInfo.InvariantCulture);
					var precision   = rd.IsDBNull(6) ? null : (ulong?)Convert.ToUInt64(rd.GetValue(6), CultureInfo.InvariantCulture);
					var scale       = rd.IsDBNull(7) ? null : (ulong?)Convert.ToUInt64(rd.GetValue(7), CultureInfo.InvariantCulture);
					var readOnly    = rd.GetBoolean(8);

					(type, var isNullable, _) = PreParseTypeName(type);

					return new ColumnInfo()
					{
						TableID      = table,
						Name         = name,
						IsNullable   = isNullable,
						Ordinal      = checked((int)ordinal),
						DataType     = type,
						ColumnType   = type,
						// this is probably only possible failue point with checked casts
						// but I don't think anyone will hit it as such huge FixedString columns are impractical
						Length       = checked((int?)length),
						Precision    = checked((int?)precision),
						Scale        = checked((int?)scale),
						Description  = string.IsNullOrWhiteSpace(description) ? null : description,
						SkipOnUpdate = readOnly
					};
				},
				@"
SELECT
	table,
	name,
	type,
	position,
	comment,
	multiIf(type LIKE '%FixedString%', character_octet_length, NULL),
	multiIf(type LIKE '%DateTime64%', datetime_precision, numeric_precision_radix = 10, numeric_precision, NULL),
	multiIf(numeric_precision_radix = 10, numeric_scale, NULL),
	is_in_primary_key
FROM system.columns
WHERE database = database() and default_kind <> 'ALIAS'")
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options) => [];

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query(
				rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var name      = rd.GetString(0);
					var keyFields = rd.GetString(1);
					var fields    = keyFields.Split(',');

					return fields.Select((f, i) => new PrimaryKeyInfo()
					{
						TableID    = name,
						ColumnName = f.Trim(),
						Ordinal    = i
					});
				},
				"select name, primary_key from system.tables where is_temporary = 0 and database = database() and primary_key <> ''")
				.SelectMany(_ => _).ToList();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query(
				rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var name        = rd.GetString(0);
					var description = rd.GetString(1);
					var isView      = rd.GetBoolean(2);

					return new TableInfo()
					{
						TableID         = name,
						TableName       = name,
						Description     = string.IsNullOrWhiteSpace(description) ? null : description,
						IsDefaultSchema = true,
						IsView          = isView
					};
				},
				"select name, comment, engine LIKE '%View' from system.tables where is_temporary = 0 and database = database()")
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
			var lowCardinality = type.StartsWith("LowCardinality(") && type.EndsWith(")");
			if (lowCardinality)
				type = type.Substring(15, type.Length - 16);

			var isNullable = type.StartsWith("Nullable(") && type.EndsWith(")");
			if (isNullable)
				type = type.Substring(9, type.Length - 10);

			// some special cases
			if (type == "Object('json')")
				type = "JSON";

			if (type == "Object(Nullable('json'))")
			{
				type = "JSON";
				isNullable = true;
			}

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
			if (dataType.StartsWith("Enum8("))       return (DataType.Enum8     , typeof(sbyte));
			if (dataType.StartsWith("Enum16("))      return (DataType.Enum16    , typeof(short));

			if (dataType.StartsWith("FixedString("))
				return (DataType.NChar, length is 1 ? typeof(char) : typeof(string));

			if (dataType.StartsWith("DateTime64("))  return (DataType.DateTime64, typeof(DateTimeOffset));

			// ClickHouse actually return Decimal( instead of DecimalX( in schema, but better to implement it
			if (dataType.StartsWith("Decimal32("))   return (DataType.Decimal32 , typeof(decimal));
			// types could store values that doesn't fit decimal
			if (dataType.StartsWith("Decimal64("))   return (DataType.Decimal64 , typeof(decimal));
			if (dataType.StartsWith("Decimal128("))  return (DataType.Decimal128, typeof(decimal));
			if (dataType.StartsWith("Decimal256("))  return (DataType.Decimal256, typeof(decimal));

			if (dataType.StartsWith("Decimal("))
			{
				return precision switch
				{
					< 10 => (DataType.Decimal32,  typeof(decimal)),
					< 19 => (DataType.Decimal64,  typeof(decimal)),
					< 38 => (DataType.Decimal128, typeof(decimal)),
					_    => (DataType.Decimal256, typeof(decimal))
				};
			}

			return (DataType.Undefined, null);
		}

		// contains only types with fixed name
		private static readonly IReadOnlyDictionary<string, (DataType dataType, Type type)> _typeMap = new Dictionary<string, (DataType dataType, Type type)>()
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
	}
}
