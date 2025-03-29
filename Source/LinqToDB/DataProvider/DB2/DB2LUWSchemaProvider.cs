using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.DB2
{
	// Known Issues:
	// - CommandBehavior.SchemaOnly doesn't return schema for stored procedures
	class DB2LUWSchemaProvider : SchemaProviderBase
	{
		private static readonly IReadOnlyList<string> _tableTypes = new[] { "TABLE", "VIEW" };

		private readonly DB2DataProvider _provider;

		public DB2LUWSchemaProvider(DB2DataProvider provider)
		{
			_provider = provider;
		}

		readonly HashSet<string?> _systemSchemas =
			GetHashSet(["SYSCAT", "SYSFUN", "SYSIBM", "SYSIBMADM", "SYSPROC", "SYSPUBLIC", "SYSSTAT", "SYSTOOLS"],
				StringComparer.OrdinalIgnoreCase);

		protected override void InitProvider(DataConnection dataConnection, GetSchemaOptions options)
		{
			base.InitProvider(dataConnection, options);

			DefaultSchema = options.DefaultSchema ?? dataConnection.Execute<string>("select current_schema from sysibm.sysdummy1");
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			DataTypesSchema = dataConnection.EnsureConnection(connect: true).Connection.GetSchema("DataTypes");

			return DataTypesSchema.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("SQL_TYPE_NAME")!,
					DataType         = t.Field<string>("FRAMEWORK_TYPE")!,
					CreateParameters = t.Field<string>("CREATE_PARAMS"),
					ProviderDbType   = t.Field<int   >("PROVIDER_TYPE"),
				})
				.Union(
				new[]
				{
					new DataTypeInfo { TypeName = "CHARACTER", CreateParameters = "LENGTH", DataType = "System.String", ProviderDbType = 12 }
				})
				.ToList();
		}

		protected string? DefaultSchema { get; private set; }

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var connection = dataConnection.EnsureConnection(connect: true).Connection;
			var database   = connection.Database;

			var tables = connection.GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where _tableTypes.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = database
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE"
				where IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || schema == DefaultSchema
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
					CatalogName        = catalog,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = schema                        == DefaultSchema,
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description        = t.Field<string>("REMARKS"),
					IsProviderSpecific = system || _systemSchemas.Contains(schema)
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var database = dataConnection.EnsureConnection(connect: true).Connection.Database;

			return
			(
				from pk in dataConnection.Query(
					rd => new
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						id   = database + "." + rd.ToString(0) + "." + rd.ToString(1),
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

			var database = dataConnection.EnsureConnection(connect: true).Connection.Database;

			return _columns = dataConnection.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var tableId     = database + "." + rd.ToString(0) + "." + rd.ToString(1);
					var name        = rd.ToString(2)!;
					var size        = Converter.ChangeTypeTo<int?>(rd[3]);
					var scale       = Converter.ChangeTypeTo<int?>(rd[4]);
					var isNullable  = rd.ToString(5) == "Y";
					var isIdentity  = rd.ToString(6) == "Y";
					var ordinal     = Converter.ChangeTypeTo<int>(rd[7]);
					var typeName    = rd.ToString(8);
					var description = rd.ToString(9);
					var cp          = Converter.ChangeTypeTo<int>(rd[10]);

					     if (typeName == "CHARACTER" && cp == 0) typeName = "CHAR () FOR BIT DATA";
					else if (typeName == "VARCHAR"   && cp == 0) typeName = "VARCHAR () FOR BIT DATA";

					var ci = new ColumnInfo
					{
						TableID     = tableId,
						Name        = name,
						IsNullable  = isNullable,
						IsIdentity  = isIdentity,
						Ordinal     = ordinal,
						DataType    = typeName,
						Description = description,
					};

					SetColumnParameters(ci, size, scale);

					return ci;
				},
				sql).ToList();
		}

		static void SetColumnParameters(ColumnInfo ci, int? size, int? scale)
		{
			switch (ci.DataType)
			{
				case "DECIMAL"                   :
				case "DECFLOAT"                  :
					if (size  > 0) ci.Precision = size;
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var database = dataConnection.EnsureConnection(connect: true).Connection.Database;

			return dataConnection
				.Query(rd => new
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					name         = rd.ToString(0)!,
					thisTable    = database + "." + rd.ToString(1)  + "." + rd.ToString(2),
					thisColumns  = rd.ToString(3)!,
					otherTable   = database + "." + rd.ToString(4)  + "." + rd.ToString(5),
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
					var thisTable    = _columns!.Where(c => c.TableID == fk.thisTable). OrderByDescending(c => c.Name.Length).ToList();
					var otherTable   = _columns!.Where(c => c.TableID == fk.otherTable).OrderByDescending(c => c.Name.Length).ToList();
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

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var type = GetDataType(columnType, null, options);

			if (type != null)
			{
				if (type.CreateParameters == null)
					length = precision = scale = 0;
				else
				{
					if (type.CreateParameters == "LENGTH")
						precision = scale = 0;
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
									.Select((p,i) => FormattableString.Invariant($"{{{i}}}")));

							type.CreateFormat = type.TypeName + "(" + format + ")";
						}
					}
				}
			}

			return base.GetDbType(options, columnType, dataType, length, precision, scale, udtCatalog, udtSchema, udtName);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType switch
			{
				"XML"                       => DataType.Xml,       // Xml             System.String
				"DECFLOAT"                  => DataType.Decimal,   // DecimalFloat    System.Decimal
				"DBCLOB"                    => DataType.Text,      // DbClob          System.String
				"CLOB"                      => DataType.Text,      // Clob            System.String
				"BLOB"                      => DataType.Blob,      // Blob            System.Byte[]
				"LONG VARGRAPHIC"           => DataType.Text,      // LongVarGraphic  System.String
				"VARGRAPHIC"                => DataType.Text,      // VarGraphic      System.String
				"GRAPHIC"                   => DataType.Text,      // Graphic         System.String
				"BIGINT"                    => DataType.Int64,     // BigInt          System.Int64
				"LONG VARCHAR FOR BIT DATA" => DataType.VarBinary, // LongVarBinary   System.Byte[]
				"VARCHAR () FOR BIT DATA"   => DataType.VarBinary, // VarBinary       System.Byte[]
				"VARBIN"                    => DataType.VarBinary, // VarBinary       System.Byte[]
				"BINARY"                    => DataType.Binary,    // Binary          System.Byte[]
				"CHAR () FOR BIT DATA"      => DataType.Binary,    // Binary          System.Byte[]
				"LONG VARCHAR"              => DataType.VarChar,   // LongVarChar     System.String
				"CHARACTER"                 => DataType.Char,      // Char            System.String
				"CHAR"                      => DataType.Char,      // Char            System.String
				"DECIMAL"                   => DataType.Decimal,   // Decimal         System.Decimal
				"INTEGER"                   => DataType.Int32,     // Integer         System.Int32
				"SMALLINT"                  => DataType.Int16,     // SmallInt        System.Int16
				"REAL"                      => DataType.Single,    // Real            System.Single
				"DOUBLE"                    => DataType.Double,    // Double          System.Double
				"VARCHAR"                   => DataType.VarChar,   // VarChar         System.String
				"DATE"                      => DataType.Date,      // Date            System.DateTime
				"TIME"                      => DataType.Time,      // Time            System.TimeSpan
				"TIMESTAMP"                 => DataType.Timestamp, // Timestamp       System.DateTime
				"TIMESTMP"                  => DataType.Timestamp, // Timestamp       System.DateTime
				"ROWID"                     => DataType.Undefined, // RowID           System.Byte[]
				_                           => DataType.Undefined,
			};
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

		protected override string GetDataSourceName(DataConnection dbConnection)
		{
			var str = dbConnection.EnsureConnection(connect: true).Connection.ConnectionString;

			var host = str?.Split(';')
				.Select(s =>
				{
					var ss = s.Split('=');
					return new { key = ss.Length == 2 ? ss[0] : "", value = ss.Length == 2 ? ss[1] : "" };
				})
				.Where (s => s.key.ToLowerInvariant() == "server")
				.Select(s => s.value)
				.FirstOrDefault();

			if (host != null)
				return host;

			return base.GetDataSourceName(dbConnection);
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			var sql = @"
SELECT * FROM (
	SELECT
		p.SPECIFICNAME,
		p.PROCSCHEMA,
		p.PROCNAME,
		p.TEXT,
		p.REMARKS,
		'P' AS IS_PROCEDURE,
		o.OBJECTMODULENAME,
		CASE WHEN CURRENT SCHEMA = p.PROCSCHEMA THEN 1 ELSE 0 END,
		p.PARM_COUNT
	FROM
		SYSCAT.PROCEDURES p
		LEFT JOIN SYSCAT.MODULEOBJECTS o ON p.SPECIFICNAME = o.SPECIFICNAME
	WHERE " + GetSchemaFilter("p.PROCSCHEMA")
	+ (IncludedSchemas.Count == 0 ? " AND p.PROCSCHEMA NOT IN ('SYSPROC', 'SYSIBMADM', 'SQLJ', 'SYSIBM')" : null)
	+ @"
	UNION ALL
	SELECT
		f.SPECIFICNAME,
		f.FUNCSCHEMA,
		f.FUNCNAME,
		f.BODY,
		f.REMARKS,
		f.TYPE,
		o.OBJECTMODULENAME,
		CASE WHEN CURRENT SCHEMA = f.FUNCSCHEMA THEN 1 ELSE 0 END,
		f.PARM_COUNT
	FROM
		SYSCAT.FUNCTIONS f
		LEFT JOIN SYSCAT.MODULEOBJECTS o ON f.SPECIFICNAME = o.SPECIFICNAME
		WHERE " + GetSchemaFilter("f.FUNCSCHEMA")
	+ (IncludedSchemas.Count == 0 ? " AND f.FUNCSCHEMA NOT IN ('SYSPROC', 'SYSIBMADM', 'SQLJ', 'SYSIBM')" : null);

			if (IncludedSchemas.Count == 0)
				sql += " AND f.FUNCSCHEMA NOT IN ('SYSPROC', 'SYSIBMADM', 'SQLJ', 'SYSIBM')";

			sql += @")
ORDER BY OBJECTMODULENAME, PROCSCHEMA, PROCNAME, PARM_COUNT";

			return dataConnection
				.Query(rd =>
					{
						// IMPORTANT: reader calls must be ordered to support SequentialAccess
						var id        = rd.ToString(0)!;
						var schema    = rd.ToString(1)!;
						var name      = rd.ToString(2)!;
						var source    = rd.ToString(3);
						var desc      = rd.ToString(4);
						// P: procedure, S: scalar function, T: table function
						var type      = rd.ToString(5)!;
						var module    = rd.ToString(6);
						var isDefault = rd.GetInt32(7) == 1;

						return new ProcedureInfo()
						{
							ProcedureID         = $"{schema}.{name}({id})",
							SchemaName          = schema,
							PackageName         = module,
							ProcedureName       = name,
							IsFunction          = type != "P",
							IsTableFunction     = type == "T",
							ProcedureDefinition = source,
							Description         = desc,
							IsDefaultSchema     = isDefault
						};
					},
					sql)
				.Where(p => IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || p.SchemaName == DefaultSchema)
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var id         = rd.ToString(0)!;
					var schema     = rd.ToString(1)!;
					var procname   = rd.ToString(2)!;
					var pName      = rd.ToString(3);
					var dataType   = rd.ToString(4)!;
					// IN OUT INOUT RET
					var mode       = rd.ToString(5)!;
					var ordinal    = ConvertTo<int>.From(rd[6]);
					var length     = ConvertTo<int>.From(rd[7]);
					var scale      = ConvertTo<int>.From(rd[8]);
					var isNullable = rd.ToString(9) == "Y";

					var ppi = new ProcedureParameterInfo()
					{
						ProcedureID   = $"{schema}.{procname}({id})",
						ParameterName = pName,
						DataType      = dataType,
						Ordinal       = ordinal,
						IsIn          = mode.Contains("IN"),
						IsOut         = mode.Contains("OUT"),
						IsResult      = mode == "RET",
						IsNullable    = isNullable
					};

					var ci = new ColumnInfo { DataType = ppi.DataType };

					SetColumnParameters(ci, length, scale);

					ppi.Length    = ci.Length;
					ppi.Precision = ci.Precision;
					ppi.Scale     = ci.Scale;

					return ppi;
				}, @"
SELECT
	SPECIFICNAME,
	PROCSCHEMA,
	PROCNAME,
	PARMNAME,
	TYPENAME,
	PARM_MODE,
	ORDINAL,
	LENGTH,
	SCALE,
	NULLS
FROM
	SYSCAT.PROCPARMS
WHERE " + GetSchemaFilter("PROCSCHEMA") + @"
UNION ALL
SELECT
	SPECIFICNAME,
	FUNCSCHEMA,
	FUNCNAME,
	PARMNAME,
	TYPENAME,
	CASE WHEN ORDINAL = 0 THEN 'RET' ELSE 'IN' END,
	ORDINAL,
	LENGTH,
	SCALE,
	'Y'
FROM
	SYSCAT.FUNCPARMS
	WHERE ROWTYPE <> 'R' AND " + GetSchemaFilter("FUNCSCHEMA"))
				.ToList();
		}

		protected string GetSchemaFilter(string schemaNameField)
		{
			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				var sql = schemaNameField;

				if (IncludedSchemas.Count != 0)
				{
					sql += string.Format(CultureInfo.InvariantCulture, " IN ({0})", string.Join(", ", IncludedSchemas.Select(n => '\'' + n + '\'')));

					if (ExcludedSchemas.Count != 0)
						sql += " AND " + schemaNameField;
				}

				if (ExcludedSchemas.Count != 0)
					sql += string.Format(CultureInfo.InvariantCulture, " NOT IN ({0})", string.Join(", ", ExcludedSchemas.Select(n => '\'' + n + '\'')));

				return sql;
			}

			return $"{schemaNameField} = '{DefaultSchema}'";
		}

		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			if (procedure.IsTableFunction)
			{
				commandText = "SELECT * FROM TABLE(" + commandText + "(";

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					if (i != 0)
						commandText += ",";
					commandText += "NULL";
				}

				commandText += "))";

				return commandText;
			}

			return base.BuildTableFunctionLoadTableSchemaCommand(procedure, commandText);
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let dt          = GetDataTypeByProviderDbType(r.Field<int>("ProviderType"), options)
				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>  ("AllowDBNull")
				let length     = r.Field<int?>  ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let columnType = GetDbType(options, null, dt, length, precision, scale, null, null, null)
				let systemType = GetSystemType(columnType, null, dt, length, precision, scale, options)

				select new ColumnSchema()
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
				}
			).ToList();
		}
	}

	static class DB2Extensions
	{
		public static string? ToString(this DbDataReader reader, int i)
		{
			var value = Converter.ChangeTypeTo<string?>(reader[i]);
			return value?.TrimEnd();
		}
	}
}
