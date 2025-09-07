using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSchemaProvider : SchemaProviderBase
	{
		static Regex _extract = new (@"^(\w+)(\((\d+)(,\s*(\d+))?\))?$", RegexOptions.Compiled);

		static IReadOnlyDictionary<string, (string dotnetType, DataType dataType)> _typeMappings = new Dictionary<string, (string dotnetType, DataType dataType)>(StringComparer.OrdinalIgnoreCase)
		{
			// affinities
			{ "blob",    ( "System.byte[]",  DataType.VarBinary) },
			{ "integer", ( "System.Int64",   DataType.Int64)     },
			{ "text",    ( "System.String",  DataType.NVarChar)  },
			{ "real",    ( "System.Double",  DataType.Double)    },
			{ "numeric", ( "System.Decimal", DataType.Decimal)   },

			// types
			// note that it doesn't make sense to use too specific DataType values for SQLite and better to stick to affinity/more generic types
			{ "smallint",         ( "System.Int16",                 DataType.Int16)     },
			{ "int",              ( "System.Int32",                 DataType.Int32)     },
			{ "mediumint",        ( "System.Int32",                 DataType.Int32)     },
			{ "single",           ( "System.Single",                DataType.Single)    },
			{ "float",            ( "System.Double",                DataType.Double)    },
			{ "double",           ( "System.Double",                DataType.Double)    },
			{ "money",            ( "System.Decimal",               DataType.Decimal)   },
			{ "currency",         ( "System.Decimal",               DataType.Decimal)   },
			{ "decimal",          ( "System.Decimal",               DataType.Decimal)   },
			{ "bit",              ( "System.Boolean",               DataType.Boolean)   },
			{ "yesno",            ( "System.Boolean",               DataType.Boolean)   },
			{ "logical",          ( "System.Boolean",               DataType.Boolean)   },
			{ "bool",             ( "System.Boolean",               DataType.Boolean)   },
			{ "boolean",          ( "System.Boolean",               DataType.Boolean)   },
			{ "tinyint",          ( "System.Byte",                  DataType.Byte)      },
			{ "counter",          ( "System.Int64",                 DataType.Int64)     },
			{ "autoincrement",    ( "System.Int64",                 DataType.Int64)     },
			{ "identity",         ( "System.Int64",                 DataType.Int64)     },
			{ "long",             ( "System.Int64",                 DataType.Int64)     },
			{ "bigint",           ( "System.Int64",                 DataType.Int64)     },
			{ "binary",           ( "System.Byte[]",                DataType.VarBinary) },
			{ "varbinary",        ( "System.Byte[]",                DataType.VarBinary) },
			{ "image",            ( "System.Byte[]",                DataType.VarBinary) },
			{ "general",          ( "System.Byte[]",                DataType.VarBinary) },
			{ "oleobject",        ( "System.Byte[]",                DataType.VarBinary) },
			{ "varchar",          ( "System.String",                DataType.NVarChar)  },
			{ "nvarchar",         ( "System.String",                DataType.NVarChar)  },
			{ "memo",             ( "System.String",                DataType.NVarChar)  },
			{ "longtext",         ( "System.String",                DataType.NVarChar)  },
			{ "note",             ( "System.String",                DataType.NVarChar)  },
			{ "ntext",            ( "System.String",                DataType.NVarChar)  },
			{ "string",           ( "System.String",                DataType.NVarChar)  },
			{ "char",             ( "System.String",                DataType.NVarChar)  },
			{ "nchar",            ( "System.String",                DataType.NVarChar)  },
			{ "datetime",         ( "System.DateTime",              DataType.DateTime2) },
			{ "smalldate",        ( "System.DateTime",              DataType.DateTime2) },
			{ "timestamp",        ( "System.DateTime",              DataType.DateTime2) },
			{ "date",             ( "System.DateTime",              DataType.Date)      },
			{ "time",             ( "System.TimeSpan",              DataType.Time)      },
			{ "uniqueidentifier", ( "System.Guid",                  DataType.Guid)      },
			{ "guid",             ( "System.Guid",                  DataType.Guid)      },
		};

		// sqlite types are not useful as sqlite has only type affinity, but not types
		static readonly List<DataTypeInfo> _dataTypes = [];
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			return dataConnection.Query<TableInfo>(@"
				SELECT
					t.schema || '..' || t.name AS TableID,
					''                         AS CatalogName,
					t.schema                   AS SchemaName,
					t.name                     AS TableName,
					t.schema = 'main'          AS IsDefaultSchema,
					t.type = 'view'            AS IsView
				FROM pragma_table_list() t
				WHERE t.type IN ('table', 'view')
			").ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			// https://www.sqlite.org/pragma.html#pragma_index_list
			return dataConnection.Query<PrimaryKeyInfo>(@"
				SELECT
					t.schema || '..' || t.name AS TableID,
					i.name                     AS PrimaryKeyName,
					c.name                     AS ColumnName,
					c.pk - 1                   AS Ordinal
				FROM pragma_table_list() t
					LEFT OUTER JOIN pragma_table_info(t.name) c
					LEFT OUTER JOIN pragma_index_list(t.name) i ON i.origin = 'pk'
				WHERE t.type IN ('table', 'view') AND c.pk != 0
			").ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			// https://www.sqlite.org/schematab.html
			// https://www.sqlite.org/autoinc.html
			return dataConnection
				.Query<ColumnInfo>(@"
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
					WHERE t.type IN ('table', 'view')
				")
				.Select(x =>
				{
					// length/precision/scale actually doesn't make sense for SQLite, we just extract numbers from type name, used on column creation
					(x.Length, x.Precision, x.Scale, x.Type, x.ColumnType) = InferTypeInformation(x.DataType);

					x.SkipOnInsert = x.IsIdentity;
					x.SkipOnUpdate = x.IsIdentity;

					return x;
				})
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://www.sqlite.org/pragma.html#pragma_table_list
			// https://www.sqlite.org/pragma.html#pragma_foreign_key_list
			// https://www.sqlite.org/pragma.html#pragma_table_info
			return dataConnection.Query<ForeignKeyInfo>(@"
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
				WHERE tThis.type IN ('table', 'view')
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

		protected override string? GetProviderSpecificTypeNamespace() => null;

		// we cannot get column type (except for strict tables) in sqlite and all we have is a type name string, used to define column
		// from this string we just try to guess user's intention for column data type using some well known db type names
		// plus type information from SDS client DataTypes schema table, which contains 45 mappings for some well known types
		static (int? length, int? precision, int? scale, DataType? dataType, string? dotnetTypeName) InferTypeInformation(string? typeName)
		{
			if (string.IsNullOrWhiteSpace(typeName))
				typeName = "blob";

			var m = _extract.Match(typeName);

			// extract type facets
			int? facet1 = null;
			int? facet2 = null;

			if (m.Success)
			{
				typeName = m.Groups[1].Value;
				if (m.Groups[3].Success)
					facet2 = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
				if (m.Groups[5].Success)
					facet2 = int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture);
			}

			int? length            = null;
			int? precision         = null;
			int? scale             = null;
			DataType? dataType     = null;
			string? dotnetTypeName = null;

			if (!_typeMappings.TryGetValue(typeName, out var typeInfo))
			{
				typeInfo = GetTypeByAffinity(typeName);
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
			static (string dotnetTypeName, DataType dataType) GetTypeByAffinity(string dataType)
			{
				// (3) If the data type is null or whitespace, it's considered BLOB.
				if (string.IsNullOrWhiteSpace(dataType))
				{
					return _typeMappings["blob"];
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
				return _typeMappings["numeric"];
			}
		}
	}
}
