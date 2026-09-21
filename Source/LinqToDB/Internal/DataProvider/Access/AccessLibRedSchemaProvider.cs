using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	// LibRed implements neither GetSchema nor GetSchemaTable, so all metadata is read with SQL from the
	// engine's own INFORMATION_SCHEMA pseudo-tables. The identifier has to be bracket-quoted as a whole —
	// the dotted INFORMATION_SCHEMA.TABLES form does not parse.
	public class AccessLibRedSchemaProvider : AccessSchemaProviderBase
	{
		public AccessLibRedSchemaProvider()
		{
		}

		// [INFORMATION_SCHEMA.COLUMNS].DATA_TYPE carries the Access store type name, and the base class
		// maps exactly these names in GetDataType. The base GetDataTypes reads GetSchema("DataTypes"),
		// which LibRed does not implement.
		static readonly List<DataTypeInfo> _dataTypes =
		[
			new() { TypeName = "bit",        DataType = "System.Boolean"                                                              },
			new() { TypeName = "byte",       DataType = "System.Byte"                                                                 },
			new() { TypeName = "smallint",   DataType = "System.Int16"                                                                },
			new() { TypeName = "short",      DataType = "System.Int16"                                                                },
			new() { TypeName = "integer",    DataType = "System.Int32"                                                                },
			new() { TypeName = "long",       DataType = "System.Int32"                                                                },
			new() { TypeName = "counter",    DataType = "System.Int32"                                                                },
			new() { TypeName = "single",     DataType = "System.Single"                                                               },
			new() { TypeName = "real",       DataType = "System.Single"                                                               },
			new() { TypeName = "double",     DataType = "System.Double"                                                               },
			new() { TypeName = "currency",   DataType = "System.Decimal"                                                              },
			new() { TypeName = "decimal",    DataType = "System.Decimal", CreateFormat = "DECIMAL({0}, {1})", CreateParameters = "precision,scale" },
			new() { TypeName = "datetime",   DataType = "System.DateTime"                                                             },
			new() { TypeName = "guid",       DataType = "System.Guid"                                                                 },
			new() { TypeName = "char",       DataType = "System.String",  CreateFormat = "CHAR({0})",         CreateParameters = "length" },
			new() { TypeName = "varchar",    DataType = "System.String",  CreateFormat = "VARCHAR({0})",      CreateParameters = "length" },
			new() { TypeName = "text",       DataType = "System.String",  CreateFormat = "VARCHAR({0})",      CreateParameters = "length" },
			new() { TypeName = "longchar",   DataType = "System.String"                                                               },
			new() { TypeName = "longtext",   DataType = "System.String"                                                               },
			new() { TypeName = "binary",     DataType = "System.Byte[]",  CreateFormat = "BINARY({0})",       CreateParameters = "length" },
			new() { TypeName = "varbinary",  DataType = "System.Byte[]",  CreateFormat = "VARBINARY({0})",    CreateParameters = "length" },
			new() { TypeName = "longbinary", DataType = "System.Byte[]"                                                               },
			new() { TypeName = "bigbinary",  DataType = "System.Byte[]"                                                               },
		];

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		// A view's columns are discovered together with the view itself, because the only source for them
		// is an empty-set read of the query and running that twice would double the cost of every schema load.
		readonly List<ColumnInfo> _viewColumns = [];

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// Access has no catalogs or schemas, so the unqualified table name is the identity used to
			// join tables, columns, primary and foreign keys together.
			var tables = dataConnection
				.Query<TableRow>("SELECT TABLE_NAME, TABLE_TYPE FROM [INFORMATION_SCHEMA.TABLES]")
				.Select(t => new TableInfo
				{
					TableID            = t.Name,
					CatalogName        = null,
					SchemaName         = null,
					TableName          = t.Name,
					IsDefaultSchema    = true,
					IsView             = false,
					IsProviderSpecific = !string.Equals(t.Type, "BASE TABLE", StringComparison.Ordinal),
				})
				.ToList();

			tables.AddRange(GetViews(dataConnection));

			return tables;
		}

		// DAO query types, as MSysObjects.Flags carries them in its low byte
		const int QueryKindMask    = 0xF0;
		const int QuerySelect      = 0x00;
		const int QueryCrosstab    = 0x10;
		const int QuerySetOperaton = 0x80;

		/// <summary>
		/// Access stores queries rather than views and INFORMATION_SCHEMA does not list them, so they are
		/// read from <c>MSysObjects</c>. Only the row-returning query kinds may be probed: an append or
		/// delete query would be <b>executed</b> by the read that discovers its columns.
		/// </summary>
		List<TableInfo> GetViews(DataConnection dataConnection)
		{
			var views      = new List<TableInfo>();
			var sqlBuilder = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, dataConnection.Options);

			var candidates = dataConnection
				.Query<QueryRow>("SELECT Name, Flags FROM MSysObjects WHERE Type = 5")
				.Where(q => (q.Flags & QueryKindMask) is QuerySelect or QueryCrosstab or QuerySetOperaton
					&& !q.Name.StartsWith('~'))
				.ToList();

			foreach (var query in candidates)
			{
				var columns = ReadViewColumns(dataConnection, sqlBuilder, query.Name);

				// a query LibRed cannot bind is not a view; a parameterised one is the usual case
				if (columns == null)
					continue;

				views.Add(new TableInfo
				{
					TableID         = query.Name,
					CatalogName     = null,
					SchemaName      = null,
					TableName       = query.Name,
					IsDefaultSchema = true,
					IsView          = true,
				});

				_viewColumns.AddRange(columns);
			}

			return views;
		}

		List<ColumnInfo>? ReadViewColumns(DataConnection dataConnection, ISqlBuilder sqlBuilder, string name)
		{
			using var sb   = Pools.StringBuilder.Allocate();
			var viewName   = sqlBuilder.BuildObjectName(sb.Value, new SqlObjectName(name), ConvertType.NameToQueryTable).ToString();

			try
			{
				// CommandBehavior.SchemaOnly is not honoured — LibRed executes the query anyway — so the
				// empty result set has to be forced in SQL
				using var rd = dataConnection.ExecuteReader($"SELECT * FROM {viewName} WHERE 1 = 0", CommandType.Text, CommandBehavior.Default);

				var reader  = rd.Reader!;
				var columns = new List<ColumnInfo>(reader.FieldCount);

				for (var i = 0; i < reader.FieldCount; i++)
				{
					// the reader gives a name and a CLR type and nothing else, so neither the exact store
					// type nor nullability is knowable for a view column
					columns.Add(new ColumnInfo
					{
						TableID    = name,
						Name       = reader.GetName(i),
						Ordinal    = i,
						DataType   = _viewColumnTypes.TryGetValue(reader.GetFieldType(i), out var dataType) ? dataType : null,
						IsNullable = true,
					});
				}

				return columns;
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch
#pragma warning restore CA1031 // Do not catch general exception types
			{
				return null;
			}
		}

		static readonly Dictionary<Type, string> _viewColumnTypes = new()
		{
			{ typeof(bool),     "bit"        },
			{ typeof(byte),     "byte"       },
			{ typeof(short),    "smallint"   },
			{ typeof(int),      "integer"    },
			{ typeof(float),    "single"     },
			{ typeof(double),   "double"     },
			{ typeof(decimal),  "decimal"    },
			{ typeof(DateTime), "datetime"   },
			{ typeof(Guid),     "guid"       },
			{ typeof(string),   "varchar"    },
			{ typeof(byte[]),   "longbinary" },
		};

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var columns = dataConnection
				.Query<ColumnRow>(@"
					SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, DATA_TYPE, IS_NULLABLE,
						CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
					FROM [INFORMATION_SCHEMA.COLUMNS]")
				.Select(c => new ColumnInfo
				{
					TableID    = c.TableName,
					Name       = c.Name,
					Ordinal    = c.Ordinal,
					DataType   = c.DataType,
					IsNullable = c.IsNullable,
					Length     = c.Length,
					Precision  = c.Precision,
					Scale      = c.Scale,
					// COUNTER is the Access identity type; IDENTITY_SEED is set for the same columns
					IsIdentity = string.Equals(c.DataType, "counter", StringComparison.OrdinalIgnoreCase),
				})
				.ToList();

			columns.AddRange(_viewColumns);

			return columns;
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var indexes = new HashSet<(string Table, string Index)>();

			foreach (var i in dataConnection.Query<IndexRow>("SELECT TABLE_NAME, INDEX_NAME, INDEX_TYPE FROM [INFORMATION_SCHEMA.INDEXES]"))
				if (string.Equals(i.Type, "PRIMARY", StringComparison.Ordinal))
					indexes.Add((i.TableName, i.Name));

			return dataConnection
				.Query<IndexColumnRow>("SELECT TABLE_NAME, INDEX_NAME, ORDINAL_POSITION, COLUMN_NAME FROM [INFORMATION_SCHEMA.INDEX_COLUMNS]")
				.Where(c => indexes.Contains((c.TableName, c.IndexName)))
				.Select(c => new PrimaryKeyInfo
				{
					TableID        = c.TableName,
					PrimaryKeyName = c.IndexName,
					ColumnName     = c.Name,
					Ordinal        = c.Ordinal,
				})
				.ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// [INFORMATION_SCHEMA.RELATIONS] names both tables but carries no column pairs
			return dataConnection
				.Query<RelationshipRow>(@"
					SELECT szRelationship, szObject, szColumn, szReferencedObject, szReferencedColumn, icolumn
					FROM MSysRelationships")
				.Select(r => new ForeignKeyInfo
				{
					Name         = r.Name,
					ThisTableID  = r.ThisTable,
					ThisColumn   = r.ThisColumn,
					OtherTableID = r.OtherTable,
					OtherColumn  = r.OtherColumn,
					Ordinal      = r.Ordinal,
				})
				.ToList();
		}

		#region metadata rows

		sealed class TableRow
		{
			[Column("TABLE_NAME")] public string  Name { get; set; } = null!;
			[Column("TABLE_TYPE")] public string? Type { get; set; }
		}

		sealed class QueryRow
		{
			[Column("Name")]  public string Name  { get; set; } = null!;
			[Column("Flags")] public int    Flags { get; set; }
		}

		sealed class ColumnRow
		{
			[Column("TABLE_NAME")]               public string  TableName  { get; set; } = null!;
			[Column("COLUMN_NAME")]              public string  Name       { get; set; } = null!;
			[Column("ORDINAL_POSITION")]         public int     Ordinal    { get; set; }
			[Column("DATA_TYPE")]                public string? DataType   { get; set; }
			[Column("IS_NULLABLE")]              public bool    IsNullable { get; set; }
			[Column("CHARACTER_MAXIMUM_LENGTH")] public int?    Length     { get; set; }
			[Column("NUMERIC_PRECISION")]        public int?    Precision  { get; set; }
			[Column("NUMERIC_SCALE")]            public int?    Scale      { get; set; }
		}

		sealed class IndexRow
		{
			[Column("TABLE_NAME")] public string  TableName { get; set; } = null!;
			[Column("INDEX_NAME")] public string  Name      { get; set; } = null!;
			[Column("INDEX_TYPE")] public string? Type      { get; set; }
		}

		sealed class IndexColumnRow
		{
			[Column("TABLE_NAME")]       public string TableName { get; set; } = null!;
			[Column("INDEX_NAME")]       public string IndexName { get; set; } = null!;
			[Column("ORDINAL_POSITION")] public int    Ordinal   { get; set; }
			[Column("COLUMN_NAME")]      public string Name      { get; set; } = null!;
		}

		sealed class RelationshipRow
		{
			[Column("szRelationship")]     public string Name        { get; set; } = null!;
			[Column("szObject")]           public string ThisTable   { get; set; } = null!;
			[Column("szColumn")]           public string ThisColumn  { get; set; } = null!;
			[Column("szReferencedObject")] public string OtherTable  { get; set; } = null!;
			[Column("szReferencedColumn")] public string OtherColumn { get; set; } = null!;
			[Column("icolumn")]            public int    Ordinal     { get; set; }
		}

		#endregion
	}
}
