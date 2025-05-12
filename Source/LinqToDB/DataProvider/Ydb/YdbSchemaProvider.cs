using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.Ydb
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
			var conn = GetOpenConnection(dataConnection, out var created);
			try
			{
				LoadCollections(conn);
				if (!Has("Tables")) return new();

				using var t = conn.GetSchema("Tables");
				var res = new List<TableInfo>();

				foreach (DataRow r in t.Rows)
				{
					string name    = Invariant(r["TABLE_NAME"]);
					string? schema = t.Columns.Contains("TABLE_SCHEMA")  ? Invariant(r["TABLE_SCHEMA"])  : null;
					string? cat    = t.Columns.Contains("TABLE_CATALOG") ? Invariant(r["TABLE_CATALOG"]) : null;
					string? type   = t.Columns.Contains("TABLE_TYPE")    ? Invariant(r["TABLE_TYPE"])    : null;

					if (!string.IsNullOrEmpty(schema) && schema!.StartsWith(".sys", StringComparison.OrdinalIgnoreCase))
						continue;
					if (name.StartsWith(".sys", StringComparison.OrdinalIgnoreCase))
						continue;

					if (!IsSchemaAllowed(options, schema) || !IsCatalogAllowed(options, cat)) continue;

					res.Add(new TableInfo
					{
						TableID = MakeTableId(schema, name),
						CatalogName = cat,
						SchemaName = schema,
						TableName = name,
						IsView = type != null && type.Equals("VIEW", StringComparison.OrdinalIgnoreCase),
						IsDefaultSchema = string.IsNullOrEmpty(schema) ||
											 (!string.IsNullOrEmpty(options.DefaultSchema) &&
											  options.StringComparer.Equals(schema, options.DefaultSchema)),
						IsProviderSpecific = false
					});
				}

				return res;
			}
			finally { if (created) conn.Dispose(); }
		}

		#endregion

		#region columns ---------------------------------------------------------------

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var conn = GetOpenConnection(dataConnection, out var created);
			try
			{
				LoadCollections(conn);
				if (!Has("Columns")) return new();

				using var c = conn.GetSchema("Columns");
				var res  = new List<ColumnInfo>();
				_pkMap = new Dictionary<string, List<string>>(options.StringComparer);

				foreach (DataRow r in c.Rows)
				{
					string tbl   = Invariant(r["TABLE_NAME"]);
					string col   = Invariant(r["COLUMN_NAME"]);
					string? sch  = c.Columns.Contains("TABLE_SCHEMA") ? Invariant(r["TABLE_SCHEMA"]) : null;

					if (!IsSchemaAllowed(options, sch)) continue;

					string id     = MakeTableId(sch, tbl);
					int    ord    = c.Columns.Contains("ORDINAL_POSITION")
						? Convert.ToInt32(r["ORDINAL_POSITION"], CultureInfo.InvariantCulture)
						: 0;
					bool nullable = !c.Columns.Contains("IS_NULLABLE") ||
									!Invariant(r["IS_NULLABLE"]).Equals("NO", StringComparison.OrdinalIgnoreCase);

					string dt     = c.Columns.Contains("DATA_TYPE") ? Invariant(r["DATA_TYPE"]) : string.Empty;

					int? len = null, prec = null, scale = null;
					if (c.Columns.Contains("CHARACTER_MAXIMUM_LENGTH") &&
						int.TryParse(Invariant(r["CHARACTER_MAXIMUM_LENGTH"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int l))
						len = l;
					if (c.Columns.Contains("NUMERIC_PRECISION") &&
						int.TryParse(Invariant(r["NUMERIC_PRECISION"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
						prec = p;
					if (c.Columns.Contains("NUMERIC_SCALE") &&
						int.TryParse(Invariant(r["NUMERIC_SCALE"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s))
						scale = s;

					res.Add(new ColumnInfo
					{
						TableID = id,
						Name = col,
						Ordinal = ord,
						IsNullable = nullable,
						DataType = dt,
						ColumnType = ComposeColumnType(dt, len, prec, scale),
						Type = GetDataType(dt, null, len, prec, scale),
						Length = len,
						Precision = prec,
						Scale = scale,
						IsIdentity = false
					});

					if (c.Columns.Contains("COLUMN_KEY") &&
						Invariant(r["COLUMN_KEY"]).Equals("PRI", StringComparison.OrdinalIgnoreCase))
					{
						if (!_pkMap.TryGetValue(id, out var lpk))
							_pkMap[id] = lpk = new List<string>();
						lpk.Add(col);
					}
				}

				if (Has("PrimaryKeys"))
				{
					using var pk = conn.GetSchema("PrimaryKeys");
					foreach (DataRow r in pk.Rows)
					{
						string tbl   = Invariant(r["TABLE_NAME"]);
						string? sch  = pk.Columns.Contains("TABLE_SCHEMA") ? Invariant(r["TABLE_SCHEMA"]) : null;
						string col   = Invariant(r["COLUMN_NAME"]);
						string id    = MakeTableId(sch, tbl);

						if (!_pkMap.TryGetValue(id, out var lpk))
							_pkMap[id] = lpk = new List<string>();
						if (!lpk.Contains(col, options.StringComparer))
							lpk.Add(col);
					}
				}

				return res;
			}
			finally { if (created) conn.Dispose(); }
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
			Array.Empty<ForeignKeyInfo>();         // FK не поддерживаются

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			if (string.IsNullOrEmpty(dataType)) return DataType.Undefined;
			return dataType!.ToLowerInvariant() switch
			{
				"bool" => DataType.Boolean,
				"int8" => DataType.SByte,
				"uint8" => DataType.Byte,
				"int16" => DataType.Int16,
				"uint16" => DataType.UInt16,
				"int32" => DataType.Int32,
				"uint32" => DataType.UInt32,
				"int64" => DataType.Int64,
				"uint64" => DataType.UInt64,
				"float" => DataType.Single,
				"double" => DataType.Double,
				"decimal" => DataType.Decimal,
				"dynumber" => DataType.VarChar,
				"string" => DataType.Blob,
				"utf8" => DataType.NText,
				"json" => DataType.Json,
				"jsondocument" => DataType.BinaryJson,
				"yson" => DataType.VarBinary,
				"uuid" => DataType.Guid,
				"date" => DataType.Date,
				"datetime" => DataType.DateTime,
				"timestamp" => DataType.DateTime2,
				"interval" => DataType.Time,
				_ => DataType.Undefined
			};
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
