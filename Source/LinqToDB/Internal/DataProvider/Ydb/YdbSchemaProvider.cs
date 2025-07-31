using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary> Schema-provider YDB </summary>
	sealed class YdbSchemaProvider : SchemaProviderBase
	{
		readonly HashSet<string>              _collections = new(StringComparer.OrdinalIgnoreCase);
		Dictionary<string,List<string>>?      _pkMap;

		#region helpers ---------------------------------------------------------------

		static DbConnection GetOpenConnection(DataConnection dc, out bool created)
		{
			var c = dc.TryGetDbConnection();
			if (c != null)
			{
				created = false;
				if (c.State == ConnectionState.Closed) c.Open();
				return c;
			}

			created = true;
			return dc.OpenDbConnection();
		}

		static string Invariant(object? v) =>
			Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;

		void LoadCollections(DbConnection c)
		{
			if (_collections.Count != 0) return;
			using var t = c.GetSchema("MetaDataCollections");
			foreach (DataRow r in t.Rows)
				_collections.Add(Invariant(r["CollectionName"]));
		}

		bool Has(string name) => _collections.Contains(name);

		static string MakeTableId(string? schema, string name) =>
			string.IsNullOrEmpty(schema)
				? name
				: FormattableString.Invariant($"{schema}.{name}");

		static string ComposeColumnType(string baseType, int? len, int? prec, int? scale)
		{
			if (string.IsNullOrEmpty(baseType)) return string.Empty;
			if (prec is not null)
				return scale is not null
					? FormattableString.Invariant($"{baseType}({prec.Value},{scale.Value})")
					: FormattableString.Invariant($"{baseType}({prec.Value})");
			return len is not null
				? FormattableString.Invariant($"{baseType}({len.Value})")
				: baseType;
		}

		static bool IsSchemaAllowed(GetSchemaOptions o, string? schema)
		{
			if (schema is null) return true;

			var exc = o.ExcludedSchemas;
			if (exc != null && exc.Any(s => o.StringComparer.Equals(s, schema)))
				return false;

			var inc = o.IncludedSchemas;
			if (inc != null && inc.Length > 0 && !inc.Any(s => o.StringComparer.Equals(s, schema)))
				return false;

			return true;
		}

		static bool IsCatalogAllowed(GetSchemaOptions o, string? catalog)
		{
			if (catalog is null) return true;

			var exc = o.ExcludedCatalogs;
			if (exc != null && exc.Any(c => o.StringComparer.Equals(c, catalog)))
				return false;

			var inc = o.IncludedCatalogs;
			if (inc != null && inc.Length > 0 && !inc.Any(c => o.StringComparer.Equals(c, catalog)))
				return false;

			return true;
		}

		#endregion --------------------------------------------------------------------

		protected override string GetDataSourceName(DataConnection dbConnection) => dbConnection.DataProvider.Name;
		protected override string GetDatabaseName(DataConnection dbConnection) => dbConnection.DataProvider.Name;
		protected override string? GetProviderSpecificTypeNamespace() => null;

		#region tables ----------------------------------------------------------------

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var conn    = GetOpenConnection(dataConnection, out var created);
			try
			{
				LoadCollections(conn);
				if (!Has("Tables"))
					return new List<TableInfo>();

				using var schema = conn.GetSchema("Tables", new[] { null, "TABLE" });

				var result = new List<TableInfo>();

				foreach (DataRow row in schema.Rows)
				{
					string name = Invariant(row["TABLE_NAME"]);
					string? schemaName = schema.Columns.Contains("TABLE_SCHEMA")
				? Invariant(row["TABLE_SCHEMA"])
				: null;
					string? catalog = schema.Columns.Contains("TABLE_CATALOG")
				? Invariant(row["TABLE_CATALOG"])
				: null;

					if (schemaName?.StartsWith(".sys", StringComparison.OrdinalIgnoreCase) == true)
						continue;
					if (name.StartsWith(".sys", StringComparison.OrdinalIgnoreCase))
						continue;

					if (!IsSchemaAllowed(options, schemaName) || !IsCatalogAllowed(options, catalog))
						continue;

					result.Add(new TableInfo
					{
						TableID = MakeTableId(schemaName, name),
						CatalogName = catalog,
						SchemaName = schemaName,
						TableName = name,
						IsView = false,
						IsDefaultSchema = string.IsNullOrEmpty(schemaName) ||
											 (!string.IsNullOrEmpty(options.DefaultSchema) &&
											  options.StringComparer.Equals(schemaName, options.DefaultSchema)),
						IsProviderSpecific = false
					});
				}

				return result;
			}
			finally
			{
				if (created)
					conn.Close();
			}
		}

		#endregion

		#region columns ---------------------------------------------------------------

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{

			var conn = GetOpenConnection(dataConnection, out var created);
			try
			{
				LoadCollections(conn);
				if (!Has("Columns"))
				{
					return new();
				}

				using var schemaTable = conn.GetSchema("Columns");

				var result = new List<ColumnInfo>();
				_pkMap = new Dictionary<string, List<string>>(options.StringComparer);

				foreach (DataRow row in schemaTable.Rows)
				{
					string  tableName  = Invariant(row["TABLE_NAME"]);
					string  columnName = Invariant(row["COLUMN_NAME"]);
					string? schemaName = schemaTable.Columns.Contains("TABLE_SCHEMA") ? Invariant(row["TABLE_SCHEMA"]) : null;

					if (!IsSchemaAllowed(options, schemaName))
						continue;

					string tableId = MakeTableId(schemaName, tableName);

					int ordinal = schemaTable.Columns.Contains("ORDINAL_POSITION")
						? Convert.ToInt32(row["ORDINAL_POSITION"], CultureInfo.InvariantCulture)
						: 0;

					bool isNullable = !schemaTable.Columns.Contains("IS_NULLABLE") || !Invariant(row["IS_NULLABLE"]).Equals("NO", StringComparison.OrdinalIgnoreCase);

					// ---- 1. raw datatype names --------------------------------------
					string dataTypeName = string.Empty;
					if (schemaTable.Columns.Contains("TYPE_NAME"))
						dataTypeName = Invariant(row["TYPE_NAME"]);
					else if (schemaTable.Columns.Contains("DATA_TYPE_NAME"))
						dataTypeName = Invariant(row["DATA_TYPE_NAME"]);

					if (string.IsNullOrWhiteSpace(dataTypeName))
						dataTypeName = schemaTable.Columns.Contains("DATA_TYPE") ? Invariant(row["DATA_TYPE"]) : string.Empty;
					if (dataTypeName.All(char.IsDigit))
						dataTypeName = string.Empty;

					// ---- 2. length / precision / scale ------------------------------
					int? length = null;
					if (schemaTable.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") && int.TryParse(Invariant(row["CHARACTER_MAXIMUM_LENGTH"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var len))
						length = len;

					int? precision = null;
					if (schemaTable.Columns.Contains("NUMERIC_PRECISION") && int.TryParse(Invariant(row["NUMERIC_PRECISION"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var prec))
						precision = prec;
					else if (schemaTable.Columns.Contains("COLUMN_SIZE") && int.TryParse(Invariant(row["COLUMN_SIZE"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out prec))
						precision = prec;

					int? scale = null;
					if (schemaTable.Columns.Contains("NUMERIC_SCALE") && int.TryParse(Invariant(row["NUMERIC_SCALE"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sc))
						scale = sc;
					else if (schemaTable.Columns.Contains("DECIMAL_DIGITS") && int.TryParse(Invariant(row["DECIMAL_DIGITS"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out sc))
						scale = sc;

					// ---- 3. compose column type string -----------------------------
					string columnType = ComposeColumnType(dataTypeName, length, precision, scale);

					// ---- 4. map to LinqToDB.DataType -------------------------------
					DataType linq2dbType = GetDataType(dataTypeName, columnType, length, precision, scale);

					// ---- 5. add ColumnInfo -----------------------------------------
					result.Add(new ColumnInfo
					{
						TableID = tableId,
						Name = columnName,
						Ordinal = ordinal,
						IsNullable = isNullable,
						DataType = dataTypeName,
						ColumnType = columnType,
						Type = linq2dbType,
						Length = length,
						Precision = precision,
						Scale = scale,
						IsIdentity = false
					});

					// ---- 6. PK map --------------------------------------------------
					if (schemaTable.Columns.Contains("COLUMN_KEY") && Invariant(row["COLUMN_KEY"]).Equals("PRI", StringComparison.OrdinalIgnoreCase))
					{
						if (!_pkMap.TryGetValue(tableId, out var pkCols))
							_pkMap[tableId] = pkCols = new();
						pkCols.Add(columnName);
					}
				}

				return result;
			}
			finally { if (created) conn.Close(); }
		}

		#endregion

		#region primary keys ----------------------------------------------------------

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(
			DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			if (_pkMap is null || _pkMap.Count == 0) return Array.Empty<PrimaryKeyInfo>();

			var res = new List<PrimaryKeyInfo>();
			foreach (var t in tables)
			{
				if (t.ID == null) continue;
				if (!_pkMap.TryGetValue(t.ID, out var cols)) continue;

				for (int i = 0; i < cols.Count; i++)
				{
					res.Add(new PrimaryKeyInfo
					{
						TableID = t.ID,
						ColumnName = cols[i],
						Ordinal = i,
						PrimaryKeyName = FormattableString.Invariant($"{t.TableName}_pk")
					});
				}
			}

			return res;
		}

		#endregion

		#region foreign keys / datatypes ---------------------------------------------

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(
			DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options) =>
			Array.Empty<ForeignKeyInfo>();

		private static readonly Regex _decimalRegex =
	new(@"^Decimal\(\d+,\s*\d+\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		protected override DataType GetDataType(
			string? dataType,    // INFORMATION_SCHEMA.COLUMNS.DATA_TYPE или TYPE_NAME
			string? columnType,
			int? length,
			int? precision,
			int? scale)
		{
			var baseType = dataType?.Trim() ?? string.Empty;
			int paren = baseType.IndexOf('(');
			if (paren > 0)
				baseType = baseType.Substring(0, paren).Trim();

			columnType = columnType?.Trim() ?? baseType;

			switch (baseType)
			{
				case "Bool": return DataType.Boolean;
				case "Int8": return DataType.SByte;
				case "Uint8": return DataType.Byte;
				case "Int16": return DataType.Int16;
				case "Uint16": return DataType.UInt16;
				case "Int32": return DataType.Int32;
				case "Uint32": return DataType.UInt32;
				case "Int64": return DataType.Int64;
				case "Uint64": return DataType.UInt64;
				case "Float": return DataType.Single;
				case "Double": return DataType.Double;

				case "String":
				case "StringData": return DataType.VarBinary;

				case "Utf8":
				case "Text": return DataType.NVarChar;

				case "Date": return DataType.Date;
				case "Datetime": return DataType.DateTime;
				case "Timestamp": return DataType.DateTime2;
				case "Interval": return DataType.Interval;

				case "Json": return DataType.Json;
				case "Uuid": return DataType.Guid;
				case "DyNumber": return DataType.VarChar;

				case "Decimal":
				case "Numeric": return DataType.Decimal;

				case "Unspecified":
					if (_decimalRegex.IsMatch(columnType))
						return DataType.Decimal;
					if (columnType.Equals("Json", StringComparison.OrdinalIgnoreCase))
						return DataType.Json;
					if (columnType.Equals("Uuid", StringComparison.OrdinalIgnoreCase))
						return DataType.Guid;
					if (columnType.Equals("DyNumber", StringComparison.OrdinalIgnoreCase))
						return DataType.VarChar;
					return DataType.Undefined;

				default:
					return DataType.Undefined;
			}
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		static readonly List<DataTypeInfo> _dataTypes =
		[
			new() { TypeName = "Bool",          DataType = typeof(bool).   AssemblyQualifiedName! },
			new() { TypeName = "Int8",          DataType = typeof(sbyte).  AssemblyQualifiedName! },
			new() { TypeName = "Uint8",         DataType = typeof(byte).   AssemblyQualifiedName! },
			new() { TypeName = "Int16",         DataType = typeof(short).  AssemblyQualifiedName! },
			new() { TypeName = "Uint16",        DataType = typeof(ushort). AssemblyQualifiedName! },
			new() { TypeName = "Int32",         DataType = typeof(int).    AssemblyQualifiedName! },
			new() { TypeName = "Uint32",        DataType = typeof(uint).   AssemblyQualifiedName! },
			new() { TypeName = "Int64",         DataType = typeof(long).   AssemblyQualifiedName! },
			new() { TypeName = "Uint64",        DataType = typeof(ulong).  AssemblyQualifiedName! },
			new() { TypeName = "Float",         DataType = typeof(float).  AssemblyQualifiedName! },
			new() { TypeName = "Double",        DataType = typeof(double). AssemblyQualifiedName! },
			new() { TypeName = "Decimal",       DataType = typeof(decimal).AssemblyQualifiedName! },
			new() { TypeName = "DyNumber",      DataType = typeof(string). AssemblyQualifiedName! },
			new() { TypeName = "String",        DataType = typeof(byte[]). AssemblyQualifiedName! },
			new() { TypeName = "Utf8",          DataType = typeof(string). AssemblyQualifiedName! },
			new() { TypeName = "Json",          DataType = typeof(string). AssemblyQualifiedName! },
			new() { TypeName = "JsonDocument",  DataType = typeof(byte[]). AssemblyQualifiedName! },
			new() { TypeName = "Yson",          DataType = typeof(byte[]). AssemblyQualifiedName! },
			new() { TypeName = "Uuid",          DataType = typeof(Guid).   AssemblyQualifiedName! },
			new() { TypeName = "Date",          DataType = typeof(DateTime).AssemblyQualifiedName! },
			new() { TypeName = "Datetime",      DataType = typeof(DateTime).AssemblyQualifiedName! },
			new() { TypeName = "Timestamp",     DataType = typeof(DateTime).AssemblyQualifiedName! },
			new() { TypeName = "Interval",      DataType = typeof(TimeSpan).AssemblyQualifiedName! },
		];

		#endregion
	}
}
