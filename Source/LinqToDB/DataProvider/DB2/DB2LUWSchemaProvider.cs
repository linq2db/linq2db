using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using Common;
	using Data;
	using SchemaProvider;

	class DB2LUWSchemaProvider : SchemaProviderBase
	{
		private readonly DB2DataProvider _provider;

		public DB2LUWSchemaProvider(DB2DataProvider provider)
		{
			_provider = provider;
		}

		readonly HashSet<string?> _systemSchemas =
			GetHashSet(new [] {"SYSCAT", "SYSFUN", "SYSIBM", "SYSIBMADM", "SYSPROC", "SYSPUBLIC", "SYSSTAT", "SYSTOOLS" },
				StringComparer.OrdinalIgnoreCase);

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			DataTypesSchema = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

			return DataTypesSchema.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("SQL_TYPE_NAME"),
					DataType         = t.Field<string>("FRAMEWORK_TYPE"),
					CreateParameters = t.Field<string>("CREATE_PARAMS"),
				})
				.Union(
				new[]
				{
					new DataTypeInfo { TypeName = "CHARACTER", CreateParameters = "LENGTH", DataType = "System.String" }
				})
				.ToList();
		}

		protected string? CurrentSchema { get; private set; }

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			LoadCurrentSchema(dataConnection);

			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where
					new[] { "TABLE", "VIEW" }.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = dataConnection.Connection.Database
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE"
				where IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || schema == CurrentSchema
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
					CatalogName        = catalog,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = schema.IsNullOrEmpty(),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description        = t.Field<string>("REMARKS"),
					IsProviderSpecific = system || _systemSchemas.Contains(schema)
				}
			).ToList();
		}

		protected void LoadCurrentSchema(DataConnection dataConnection)
		{
			if (CurrentSchema == null)
				CurrentSchema = dataConnection.Execute<string>("select current_schema from sysibm.sysdummy1");
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			return
			(
				from pk in dataConnection.Query(
					rd => new
					{
						id   = dataConnection.Connection.Database + "." + rd.ToString(0) + "." + rd.ToString(1),
						name = rd.ToString(2),
						cols = rd.ToString(3)!.Split('+').Skip(1).ToArray(),
					},@"
SELECT
	TABSCHEMA,
	TABNAME,
	INDNAME,
	COLNAMES
FROM
	SYSCAT.INDEXES
WHERE
	UNIQUERULE = 'P' AND " + GetSchemaFilter("TABSCHEMA"))
				from col in pk.cols.Select((c,i) => new { c, i })
				select new PrimaryKeyInfo
				{
					TableID        = pk.id,
					PrimaryKeyName = pk.name,
					ColumnName     = col.c,
					Ordinal        = col.i
				}
			).ToList();
		}

		List<ColumnInfo>? _columns;

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var sql = @"
SELECT
	TABSCHEMA,
	TABNAME,
	COLNAME,
	LENGTH,
	SCALE,
	NULLS,
	IDENTITY,
	COLNO,
	TYPENAME,
	REMARKS,
	CODEPAGE
FROM
	SYSCAT.COLUMNS
WHERE
	" + GetSchemaFilter("TABSCHEMA");

			return _columns = dataConnection.Query(rd =>
				{
					var typeName = rd.ToString(8);
					var cp   = Converter.ChangeTypeTo<int>(rd[10]);

					     if (typeName == "CHARACTER" && cp == 0) typeName = "CHAR () FOR BIT DATA";
					else if (typeName == "VARCHAR"   && cp == 0) typeName = "VARCHAR () FOR BIT DATA";

					var ci = new ColumnInfo
					{
						TableID     = dataConnection.Connection.Database + "." + rd.GetString(0) + "." + rd.GetString(1),
						Name        = rd.ToString(2)!,
						IsNullable  = rd.ToString(5) == "Y",
						IsIdentity  = rd.ToString(6) == "Y",
						Ordinal     = Converter.ChangeTypeTo<int>(rd[7]),
						DataType    = typeName,
						Description = rd.ToString(9),
					};

					SetColumnParameters(ci, Converter.ChangeTypeTo<long?>(rd[3]), Converter.ChangeTypeTo<int?> (rd[4]));

					return ci;
				},
				sql).ToList();
		}

		static void SetColumnParameters(ColumnInfo ci, long? size, int? scale)
		{
			switch (ci.DataType)
			{
				case "DECIMAL"                   :
				case "DECFLOAT"                  :
					if (size  > 0) ci.Precision = (int?)size;
					if (scale > 0) ci.Scale     = scale;
					break;

				case "DBCLOB"                    :
				case "CLOB"                      :
				case "BLOB"                      :
				case "LONG VARGRAPHIC"           :
				case "VARGRAPHIC"                :
				case "GRAPHIC"                   :
				case "LONG VARCHAR FOR BIT DATA" :
				case "VARCHAR () FOR BIT DATA"   :
				case "VARBIN"                    :
				case "BINARY"                    :
				case "CHAR () FOR BIT DATA"      :
				case "LONG VARCHAR"              :
				case "CHARACTER"                 :
				case "CHAR"                      :
				case "VARCHAR"                   :
					ci.Length = size;
					break;
			}
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			return dataConnection
				.Query(rd => new
				{
					name         = rd.ToString(0)!,
					thisTable    = dataConnection.Connection.Database + "." + rd.ToString(1)  + "." + rd.ToString(2),
					thisColumns  = rd.ToString(3)!,
					otherTable   = dataConnection.Connection.Database + "." + rd.ToString(4)  + "." + rd.ToString(5),
					otherColumns = rd.ToString(6)!,
				},@"
SELECT
	CONSTNAME,
	TABSCHEMA,
	TABNAME,
	FK_COLNAMES,
	REFTABSCHEMA,
	REFTABNAME,
	PK_COLNAMES
FROM
	SYSCAT.REFERENCES
WHERE
	" + GetSchemaFilter("TABSCHEMA"))
				.SelectMany(fk =>
				{
					var thisTable    = _columns.Where(c => c.TableID == fk.thisTable). OrderByDescending(c => c.Name.Length).ToList();
					var otherTable   = _columns.Where(c => c.TableID == fk.otherTable).OrderByDescending(c => c.Name.Length).ToList();
					var thisColumns  = fk.thisColumns. Trim();
					var otherColumns = fk.otherColumns.Trim();

					var list = new List<ForeignKeyInfo>();

					for (var i = 0; thisColumns.Length > 0; i++)
					{
						var thisColumn  = thisTable. FirstOrDefault(c => thisColumns. StartsWith(c.Name));
						var otherColumn = otherTable.FirstOrDefault(c => otherColumns.StartsWith(c.Name));

						if (thisColumn == null || otherColumn == null)
							break;

						list.Add(new ForeignKeyInfo
						{
							Name         = fk.name,
							ThisTableID  = fk.thisTable,
							OtherTableID = fk.otherTable,
							Ordinal      = i,
							ThisColumn   = thisColumn. Name,
							OtherColumn  = otherColumn.Name,
						});

						thisColumns  = thisColumns. Substring(thisColumn. Name.Length).Trim();
						otherColumns = otherColumns.Substring(otherColumn.Name.Length).Trim();
					}

					return list;
				})
				.ToList();
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, long? length, int? prec, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var type = GetDataType(columnType, options);

			if (type != null)
			{
				if (type.CreateParameters == null)
					length = prec = scale = 0;
				else
				{
					if (type.CreateParameters == "LENGTH")
						prec = scale = 0;
					else
						length = 0;

					if (type.CreateFormat == null)
					{
						if (type.TypeName.IndexOf("()") >= 0)
						{
							type.CreateFormat = type.TypeName.Replace("()", "({0})");
						}
						else
						{
							var format = string.Join(",",
								type.CreateParameters
									.Split(',')
									.Select((p,i) => "{" + i + "}")
									.ToArray());

							type.CreateFormat = type.TypeName + "(" + format + ")";
						}
					}
				}
			}

			return base.GetDbType(options, columnType, dataType, length, prec, scale, udtCatalog, udtSchema, udtName);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			switch (dataType)
			{
				case "XML"                       : return DataType.Xml;       // Xml             System.String
				case "DECFLOAT"                  : return DataType.Decimal;   // DecimalFloat    System.Decimal
				case "DBCLOB"                    : return DataType.Text;      // DbClob          System.String
				case "CLOB"                      : return DataType.Text;      // Clob            System.String
				case "BLOB"                      : return DataType.Blob;      // Blob            System.Byte[]
				case "LONG VARGRAPHIC"           : return DataType.Text;      // LongVarGraphic  System.String
				case "VARGRAPHIC"                : return DataType.Text;      // VarGraphic      System.String
				case "GRAPHIC"                   : return DataType.Text;      // Graphic         System.String
				case "BIGINT"                    : return DataType.Int64;     // BigInt          System.Int64
				case "LONG VARCHAR FOR BIT DATA" : return DataType.VarBinary; // LongVarBinary   System.Byte[]
				case "VARCHAR () FOR BIT DATA"   : return DataType.VarBinary; // VarBinary       System.Byte[]
				case "VARBIN"                    : return DataType.VarBinary; // VarBinary       System.Byte[]
				case "BINARY"                    : return DataType.Binary;    // Binary          System.Byte[]
				case "CHAR () FOR BIT DATA"      : return DataType.Binary;    // Binary          System.Byte[]
				case "LONG VARCHAR"              : return DataType.VarChar;   // LongVarChar     System.String
				case "CHARACTER"                 : return DataType.Char;      // Char            System.String
				case "CHAR"                      : return DataType.Char;      // Char            System.String
				case "DECIMAL"                   : return DataType.Decimal;   // Decimal         System.Decimal
				case "INTEGER"                   : return DataType.Int32;     // Integer         System.Int32
				case "SMALLINT"                  : return DataType.Int16;     // SmallInt        System.Int16
				case "REAL"                      : return DataType.Single;    // Real            System.Single
				case "DOUBLE"                    : return DataType.Double;    // Double          System.Double
				case "VARCHAR"                   : return DataType.VarChar;   // VarChar         System.String
				case "DATE"                      : return DataType.Date;      // Date            System.DateTime
				case "TIME"                      : return DataType.Time;      // Time            System.TimeSpan
				case "TIMESTAMP"                 : return DataType.Timestamp; // Timestamp       System.DateTime
				case "TIMESTMP"                  : return DataType.Timestamp; // Timestamp       System.DateTime
				case "ROWID"                     : return DataType.Undefined; // RowID           System.Byte[]
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType)
			{
				case "XML"                       : return _provider.Adapter.DB2XmlType         .Name;
				case "DECFLOAT"                  : return _provider.Adapter.DB2DecimalFloatType.Name;
				case "DBCLOB"                    :
				case "CLOB"                      : return _provider.Adapter.DB2ClobType        .Name;
				case "BLOB"                      : return _provider.Adapter.DB2BlobType        .Name;
				case "BIGINT"                    : return _provider.Adapter.DB2Int64Type       .Name;
				case "LONG VARCHAR FOR BIT DATA" :
				case "VARCHAR () FOR BIT DATA"   :
				case "VARBIN"                    :
				case "BINARY"                    :
				case "CHAR () FOR BIT DATA"      : return _provider.Adapter.DB2BinaryType      .Name;
				case "LONG VARGRAPHIC"           :
				case "VARGRAPHIC"                :
				case "GRAPHIC"                   :
				case "LONG VARCHAR"              :
				case "CHARACTER"                 :
				case "VARCHAR"                   :
				case "CHAR"                      : return _provider.Adapter.DB2StringType      .Name;
				case "DECIMAL"                   : return _provider.Adapter.DB2DecimalType     .Name;
				case "INTEGER"                   : return _provider.Adapter.DB2Int32Type       .Name;
				case "SMALLINT"                  : return _provider.Adapter.DB2Int16Type       .Name;
				case "REAL"                      : return _provider.Adapter.DB2RealType        .Name;
				case "DOUBLE"                    : return _provider.Adapter.DB2DoubleType      .Name;
				case "DATE"                      : return _provider.Adapter.DB2DateType        .Name;
				case "TIME"                      : return _provider.Adapter.DB2TimeType        .Name;
				case "TIMESTMP"                  :
				case "TIMESTAMP"                 : return _provider.Adapter.DB2TimeStampType   .Name;
				case "ROWID"                     : return _provider.Adapter.DB2RowIdType       .Name;
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override string GetDataSourceName(DataConnection connection)
		{
			var str = ((DbConnection)connection.Connection).ConnectionString;

			var host = str?.Split(';')
				.Select(s =>
				{
					var ss = s.Split('=');
					return new { key = ss.Length == 2 ? ss[0] : "", value = ss.Length == 2 ? ss[1] : "" };
				})
				.Where (s => s.key.ToUpper() == "SERVER")
				.Select(s => s.value)
				.FirstOrDefault();

			if (host != null)
				return host;

			return base.GetDataSourceName(connection);
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			LoadCurrentSchema(dataConnection);

			var sql = @"
SELECT
	PROCSCHEMA,
	PROCNAME
FROM
	SYSCAT.PROCEDURES
WHERE
	" + GetSchemaFilter("PROCSCHEMA");

			if (IncludedSchemas.Count == 0)
				sql += " AND PROCSCHEMA NOT IN ('SYSPROC', 'SYSIBMADM', 'SQLJ', 'SYSIBM')";

			return dataConnection
				.Query(rd =>
					{
						var schema = rd.ToString(0);
						var name   = rd.ToString(1)!;

						return new ProcedureInfo
						{
							ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + name,
							CatalogName   = dataConnection.Connection.Database,
							SchemaName    = schema,
							ProcedureName = name,
						};
					},
					sql)
				.Where(p => IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || p.SchemaName == CurrentSchema)
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			return dataConnection
				.Query(rd =>
				{
					var schema   = rd.ToString(0);
					var procname = rd.ToString(1);
					var length   = ConvertTo<long?>.From(rd["LENGTH"]);
					var scale    = ConvertTo<int?>. From(rd["SCALE"]);
					var mode     = ConvertTo<string>.From(rd[4]);

					var ppi = new ProcedureParameterInfo
					{
						ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + procname,
						ParameterName = rd.ToString(2),
						DataType      = rd.ToString(3),
						Ordinal       = ConvertTo<int>.From(rd["ORDINAL"]),
						IsIn          = mode.Contains("IN"),
						IsOut         = mode.Contains("OUT"),
						IsResult      = false,
						IsNullable    = true
					};

					var ci = new ColumnInfo { DataType = ppi.DataType };

					SetColumnParameters(ci, length, scale);

					ppi.Length    = ci.Length;
					ppi.Precision = ci.Precision;
					ppi.Scale     = ci.Scale;

					return ppi;
				},@"
SELECT
	PROCSCHEMA,
	PROCNAME,
	PARMNAME,
	TYPENAME,
	PARM_MODE,

	ORDINAL,
	LENGTH,
	SCALE
FROM
	SYSCAT.PROCPARMS
WHERE
	" + GetSchemaFilter("PROCSCHEMA"))
				.ToList();
		}

		protected string GetSchemaFilter(string schemaNameField)
		{
			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				var sql = schemaNameField;

				if (IncludedSchemas.Count != 0)
				{
					sql += string.Format(" IN ({0})", IncludedSchemas.Select(n => '\'' + n + '\'') .Aggregate((s1,s2) => s1 + ',' + s2));

					if (ExcludedSchemas.Count != 0)
						sql += " AND " + schemaNameField;
				}

				if (ExcludedSchemas.Count != 0)
					sql += string.Format(" NOT IN ({0})", ExcludedSchemas.Select(n => '\'' + n + '\'') .Aggregate((s1,s2) => s1 + ',' + s2));

				return sql;
			}

			return $"{schemaNameField} = '{CurrentSchema}'";
		}
	}

	static class DB2Extensions
	{
		public static string? ToString(this IDataReader reader, int i)
		{
			var value = Converter.ChangeTypeTo<string?>(reader[i]);
			return value?.TrimEnd();
		}
	}
}
