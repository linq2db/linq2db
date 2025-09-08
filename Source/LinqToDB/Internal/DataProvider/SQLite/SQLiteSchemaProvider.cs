using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSchemaProvider : SchemaProviderBase
	{
		static Regex _extract = new (@"^(\w+)(\((\d+)(,\s*(\d+))?\))?$", RegexOptions.Compiled);

		static IReadOnlyDictionary<string, (Type dotnetType, DataType dataType)> _typeMappings = new Dictionary<string, (Type dotnetType, DataType dataType)>(StringComparer.OrdinalIgnoreCase)
		{
			// affinities
			{ "blob",    ( typeof(byte[]),  DataType.VarBinary) },
			{ "integer", ( typeof(long),    DataType.Int64)     },
			{ "text",    ( typeof(string),  DataType.NVarChar)  },
			{ "real",    ( typeof(double),  DataType.Double)    },
			{ "numeric", ( typeof(decimal), DataType.Decimal)   },

			// types
			// note that it doesn't make sense to use too specific DataType values for SQLite and better to stick to affinity/more generic types
			{ "smallint",         ( typeof(short),    DataType.Int16)     },
			{ "int",              ( typeof(int),      DataType.Int32)     },
			{ "mediumint",        ( typeof(int),      DataType.Int32)     },
			{ "single",           ( typeof(float),    DataType.Single)    },
			{ "float",            ( typeof(double),   DataType.Double)    },
			{ "double",           ( typeof(double),   DataType.Double)    },
			{ "money",            ( typeof(decimal),  DataType.Decimal)   },
			{ "currency",         ( typeof(decimal),  DataType.Decimal)   },
			{ "decimal",          ( typeof(decimal),  DataType.Decimal)   },
			{ "bit",              ( typeof(bool),     DataType.Boolean)   },
			{ "yesno",            ( typeof(bool),     DataType.Boolean)   },
			{ "logical",          ( typeof(bool),     DataType.Boolean)   },
			{ "bool",             ( typeof(bool),     DataType.Boolean)   },
			{ "boolean",          ( typeof(bool),     DataType.Boolean)   },
			{ "tinyint",          ( typeof(byte),     DataType.Byte)      },
			{ "counter",          ( typeof(long),     DataType.Int64)     },
			{ "autoincrement",    ( typeof(long),     DataType.Int64)     },
			{ "identity",         ( typeof(long),     DataType.Int64)     },
			{ "long",             ( typeof(long),     DataType.Int64)     },
			{ "bigint",           ( typeof(long),     DataType.Int64)     },
			{ "binary",           ( typeof(byte[]),   DataType.VarBinary) },
			{ "varbinary",        ( typeof(byte[]),   DataType.VarBinary) },
			{ "image",            ( typeof(byte[]),   DataType.VarBinary) },
			{ "general",          ( typeof(byte[]),   DataType.VarBinary) },
			{ "oleobject",        ( typeof(byte[]),   DataType.VarBinary) },
			{ "varchar",          ( typeof(string),   DataType.NVarChar)  },
			{ "nvarchar",         ( typeof(string),   DataType.NVarChar)  },
			{ "memo",             ( typeof(string),   DataType.NVarChar)  },
			{ "longtext",         ( typeof(string),   DataType.NVarChar)  },
			{ "note",             ( typeof(string),   DataType.NVarChar)  },
			{ "ntext",            ( typeof(string),   DataType.NVarChar)  },
			{ "string",           ( typeof(string),   DataType.NVarChar)  },
			{ "char",             ( typeof(string),   DataType.NVarChar)  },
			{ "nchar",            ( typeof(string),   DataType.NVarChar)  },
			{ "datetime",         ( typeof(DateTime), DataType.DateTime2) },
			{ "smalldate",        ( typeof(DateTime), DataType.DateTime2) },
			{ "timestamp",        ( typeof(DateTime), DataType.DateTime2) },
			{ "date",             ( typeof(DateTime), DataType.Date)      },
			{ "time",             ( typeof(TimeSpan), DataType.Time)      },
			{ "uniqueidentifier", ( typeof(Guid),     DataType.Guid)      },
			{ "guid",             ( typeof(Guid),     DataType.Guid)      },

			// additional mappings
			{ "datetime2",        ( typeof(DateTime), DataType.DateTime2) },
			// affinity-based typing for unknown types doesn't work well with providers
			{ "object",           ( typeof(object),   DataType.Variant)   },
		};

		// sqlite types are not useful as sqlite has only type affinity, but not types
		static readonly List<DataTypeInfo> _dataTypes = [];
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		private string GenerateTableFilter(string t)
		{
			// by default return only main schema tables and don't include system tables to maintain compatibility with
			// System.Data.Sqlite behavior (also by default those tables not expected by user)

			string filter = $" AND {t}.name NOT IN ('sqlite_sequence', 'sqlite_schema')";
			if (IncludedSchemas.Count == 0 && ExcludedSchemas.Count == 0)
			{
				// exclude temp schema
				filter += $" AND {t}.schema = 'main'";
			}

			return filter;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			return dataConnection.Query<TableInfo>(@$"
				SELECT
					t.schema || '..' || t.name AS TableID,
					''                         AS CatalogName,
					t.schema                   AS SchemaName,
					t.name                     AS TableName,
					t.schema = 'main'          AS IsDefaultSchema,
					t.type = 'view'            AS IsView
				FROM pragma_table_list() t
				WHERE t.type IN ('table', 'view'){GenerateTableFilter("t")}
			").ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			// https://www.sqlite.org/pragma.html#pragma_index_list
			return dataConnection.Query<PrimaryKeyInfo>(@$"
				SELECT
					t.schema || '..' || t.name AS TableID,
					i.name                     AS PrimaryKeyName,
					c.name                     AS ColumnName,
					c.pk - 1                   AS Ordinal
				FROM pragma_table_list() t
					LEFT OUTER JOIN pragma_table_info(t.name) c
					LEFT OUTER JOIN pragma_index_list(t.name) i ON i.origin = 'pk'
				WHERE t.type IN ('table', 'view') AND c.pk != 0{GenerateTableFilter("t")}
			").ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			// https://www.sqlite.org/schematab.html
			// https://www.sqlite.org/autoinc.html
			var columns = dataConnection
				.Query<ColumnInfo>(@$"
					WITH pk_counts AS (
						SELECT
							t.name   AS table_name,
							COUNT(*) AS pk_count
						FROM pragma_table_list() t
							JOIN pragma_table_info(t.name) c
						WHERE c.pk > 0
						GROUP BY t.name
					)
					SELECT
						t.schema || '..' || t.name                                                                    AS TableID,
						c.name                                                                                        AS Name,
						c.[notnull] = 0                                                                               AS IsNullable,
						c.cid                                                                                         AS Ordinal,
						c.type                                                                                        AS DataType,
						(pk.pk_count = 1 AND c.pk = 1 AND UPPER(c.type) = 'INTEGER' AND m.sql LIKE '%AUTOINCREMENT%') AS [IsIdentity]
					FROM pragma_table_list() t
						LEFT OUTER JOIN pragma_table_info(t.name) c
						INNER JOIN sqlite_master m ON m.tbl_name = t.name AND m.type IN ('table', 'view')
						LEFT JOIN pk_counts pk ON pk.table_name = t.name
					WHERE t.type IN ('table', 'view'){GenerateTableFilter("t")}
				")
				.Select(x =>
				{
					// length/precision/scale actually doesn't make sense for SQLite, we just extract numbers from type name, used on column creation
					x.ColumnType = x.DataType;
					(x.Length, x.Precision, x.Scale, x.Type, _) = InferTypeInformation(x.DataType);

					x.SkipOnInsert = x.IsIdentity;
					x.SkipOnUpdate = x.IsIdentity;

					return x;
				})
				.ToList();

			// for views query above doesn't provide proper type information and we need to query schema for each view
			var views = dataConnection.Query<TableInfo>(@$"
				SELECT
					t.schema AS SchemaName,
					t.name   AS TableName
				FROM pragma_table_list() t
				WHERE t.type IN ('view'){GenerateTableFilter("t")}
			").ToList();

			if (views.Count > 0)
			{
				var sqlbuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, dataConnection.Options);
				var columnsLookup = columns.ToDictionary(c => (c.TableID, c.Name));

				foreach (var view in views)
				{
					using var sb = Pools.StringBuilder.Allocate();
					var tableName = sqlbuilder.BuildObjectName(sb.Value, new(view.TableName, Schema:view.SchemaName), ConvertType.NameToQueryTable);

					using var rd = dataConnection.ExecuteReader($"SELECT * FROM {tableName}", CommandType.Text, CommandBehavior.SchemaOnly);
					var schema = rd.Reader!.GetSchemaTable()!;

					var tableId = $"{view.SchemaName}..{view.TableName}";
					foreach (DataRow row in schema.Rows)
					{
						if (columnsLookup.TryGetValue((tableId, row.Field<string>("ColumnName")!), out var column))
						{
							// MS provider returns DBNull for expression columns
							if (row["AllowDBNull"] is bool allowDbNull && !allowDbNull)
								column.IsNullable = false;

							// MS provider returns DBNull for expression columns
							if (row["IsAutoIncrement"] is bool isIdentity && isIdentity)
							{
								column.IsIdentity   = true;
								column.SkipOnUpdate = true;
								column.SkipOnInsert = true;
							}

							if (string.IsNullOrEmpty(column.DataType))
							{
								column.ColumnType = column.DataType = row.Field<string>("DataTypeName");
								(column.Length, column.Precision, column.Scale, column.Type, _) = InferTypeInformation(column.DataType);
							}
						}
					}
				}
			}

			return columns;
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_foreign_key_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			return dataConnection.Query<ForeignKeyInfo>(@$"
				SELECT
					'FK_' || tThis.name || '_' || f.id || '_' || f.seq AS Name,
					tThis.schema || '..' || tThis.name                 AS ThisTableID,
					f.[from]                                           AS ThisColumn,
					tOther.schema || '..' || tOther.name               AS OtherTableID,
					coalesce(f.[to], cOther.name)                      AS OtherColumn,
					f.seq                                              AS Ordinal
				FROM pragma_table_list() tThis
					LEFT OUTER JOIN pragma_foreign_key_list(tThis.name) f
					INNER JOIN pragma_table_list() tOther ON f.[table] = tOther.name
					LEFT JOIN pragma_table_info(tOther.name) cOther ON (cOther.pk -1) == f.seq
				WHERE tThis.type IN ('table', 'view'){GenerateTableFilter("tThis")}
			").ToList();
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return dbConnection.OpenDbConnection().DataSource;
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			// shouldn't be called
			throw new NotImplementedException();
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			var type = InferTypeInformation(dataType).dotnetType;

			if (!options.GenerateChar1AsString && length == 1 && type == typeof(string))
				type = typeof(char);

			return type;
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		// we cannot get column type (except for strict tables) in sqlite and all we have is a type name string, used to define column
		// from this string we just try to guess user's intention for column data type using some well known db type names
		// plus type information from SDS client DataTypes schema table, which contains 45 mappings for some well known types
		static (int? length, int? precision, int? scale, DataType dataType, Type dotnetType) InferTypeInformation(string? typeName)
		{
			// should be blob
			// but we use object to avoid errors from provider on value mapping when affinity is not correct
			if (string.IsNullOrWhiteSpace(typeName))
				typeName = "object";

			var m = _extract.Match(typeName);

			// extract type facets
			int? facet1 = null;
			int? facet2 = null;

			if (m.Success)
			{
				typeName = m.Groups[1].Value;
				if (m.Groups[3].Success)
					facet1 = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
				if (m.Groups[5].Success)
					facet2 = int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
			}

			int? length         = null;
			int? precision      = null;
			int? scale          = null;
			DataType dataType;
			Type dotnetTypeName;

			if (!_typeMappings.TryGetValue(typeName!, out var typeInfo))
			{
				typeInfo = GetTypeByAffinity(typeName!);
			}

			dotnetTypeName = typeInfo.dotnetType;
			dataType       = typeInfo.dataType;

			switch (dataType)
			{
				case DataType.Decimal:
					precision = facet1;
					scale = facet2;
					break;
				case DataType.NVarChar:
				case DataType.VarBinary:
					length = facet1;
					break;
			}

			return (length, precision, scale, dataType, dotnetTypeName);

			// https://www.sqlite.org/datatype3.html#determination_of_column_affinity
			static (Type dotnetType, DataType dataType) GetTypeByAffinity(string dataType)
			{
				// (3) If the data type is null or whitespace, it's considered BLOB.
				if (string.IsNullOrWhiteSpace(dataType))
				{
					// should be blob
					// but we use object to avoid errors from provider on value mapping when affinity is not correct
					return _typeMappings["object"];
				}

				// 1. Check for INTEGER affinity.
				if (dataType.Contains("INT", StringComparison.OrdinalIgnoreCase))
				{
					return _typeMappings["integer"];
				}

				// 2. Check for TEXT affinity.
				if (dataType.Contains("CHAR", StringComparison.OrdinalIgnoreCase)
					|| dataType.Contains("CLOB", StringComparison.OrdinalIgnoreCase)
					|| dataType.Contains("TEXT", StringComparison.OrdinalIgnoreCase))
				{
					return _typeMappings["text"];
				}

				// 3. Check for BLOB affinity.
				if (dataType.Contains("BLOB", StringComparison.OrdinalIgnoreCase))
				{
					return _typeMappings["blob"];
				}

				// 4. Check for REAL affinity.
				if (dataType.Contains("REAL", StringComparison.OrdinalIgnoreCase)
					|| dataType.Contains("FLOA", StringComparison.OrdinalIgnoreCase)
					|| dataType.Contains("DOUB", StringComparison.OrdinalIgnoreCase))
				{
					return _typeMappings["real"];
				}

				// 5. Otherwise, the affinity is NUMERIC.
				// but we use object to avoid errors from provider on value mapping when affinity is not correct
				return _typeMappings["object"];
			}
		}
	}
}
