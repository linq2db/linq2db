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
		readonly HashSet<string> _systemSchemas =
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

		protected string CurrenSchema;

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			CurrenSchema = dataConnection.Execute<string>("select current_schema from sysibm.sysdummy1");

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
				where IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || schema == CurrenSchema
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

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
			(
				from pk in dataConnection.Query(
					rd => new
					{
						id   = dataConnection.Connection.Database + "." + rd.ToString(0) + "." + rd.ToString(1),
						name = rd.ToString(2),
						cols = rd.ToString(3).Split('+').Skip(1).ToArray(),
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

		List<ColumnInfo> _columns;

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
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
						Name        = rd.ToString(2),
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
					if ((size  ?? 0) > 0) ci.Precision = (int?)size.Value;
					if ((scale ?? 0) > 0) ci.Scale     = scale;
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

		protected override List<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			return dataConnection
				.Query(rd => new
				{
					name         = rd.ToString(0),
					thisTable    = dataConnection.Connection.Database + "." + rd.ToString(1)  + "." + rd.ToString(2),
					thisColumns  = rd.ToString(3),
					otherTable   = dataConnection.Connection.Database + "." + rd.ToString(4)  + "." + rd.ToString(5),
					otherColumns = rd.ToString(6),
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
					var thisTable    = _columns.Where(c => c.TableID == fk.thisTable). OrderByDescending(c => c.Length).ToList();
					var otherTable   = _columns.Where(c => c.TableID == fk.otherTable).OrderByDescending(c => c.Length).ToList();
					var thisColumns  = fk.thisColumns. Trim();
					var otherColumns = fk.otherColumns.Trim();

					var list = new List<ForeignKeyInfo>();

					for (var i = 0; thisColumns.Length > 0; i++)
					{
						var thisColumn  = thisTable. FirstOrDefault(c => thisColumns. StartsWith(c.Name));
						if (thisColumn  == null)
							continue;

						var otherColumn = otherTable.FirstOrDefault(c => otherColumns.StartsWith(c.Name));
						if (otherColumn == null)
							continue;


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

		protected override string GetDbType(string columnType, DataTypeInfo dataType, long? length, int? prec, int? scale)
		{
			var type = DataTypes.FirstOrDefault(dt => dt.TypeName == columnType);

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

			return base.GetDbType(columnType, dataType, length, prec, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
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
			return "IBM.Data.DB2Types";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType)
			{
				case "XML"                       : return "DB2Xml";
				case "DECFLOAT"                  : return "DB2DecimalFloat";
				case "DBCLOB"                    :
				case "CLOB"                      : return "DB2Clob";
				case "BLOB"                      : return "DB2Blob";
				case "BIGINT"                    : return "DB2Int64";
				case "LONG VARCHAR FOR BIT DATA" :
				case "VARCHAR () FOR BIT DATA"   :
				case "VARBIN"                    :
				case "BINARY"                    :
				case "CHAR () FOR BIT DATA"      : return "DB2Binary";
				case "LONG VARGRAPHIC"           :
				case "VARGRAPHIC"                :
				case "GRAPHIC"                   :
				case "LONG VARCHAR"              :
				case "CHARACTER"                 :
				case "VARCHAR"                   :
				case "CHAR"                      : return "DB2String";
				case "DECIMAL"                   : return "DB2Decimal";
				case "INTEGER"                   : return "DB2Int32";
				case "SMALLINT"                  : return "DB2Int16";
				case "REAL"                      : return "DB2Real";
				case "DOUBLE"                    : return "DB2Double";
				case "DATE"                      : return "DB2Date";
				case "TIME"                      : return "DB2Time";
				case "TIMESTMP"                  :
				case "TIMESTAMP"                 : return "DB2TimeStamp";
				case "ROWID"                     : return "DB2RowId";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override string GetDataSourceName(DbConnection dbConnection)
		{
			var str = dbConnection.ConnectionString;

			if (str != null)
			{
				var host = str.Split(';')
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
			}

			return base.GetDataSourceName(dbConnection);
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var sql = @"
				SELECT
					PROCSCHEMA,
					PROCNAME
				FROM
					SYSCAT.PROCEDURES
				WHERE
					" + GetSchemaFilter("PROCSCHEMA");

			if (IncludedSchemas.Count == 0)
				sql += " AND PROCSCHEMA NOT IN ('SYSPROC', 'SYSIBMADM', 'SQLJ', 'ADMINISTRATOR', 'SYSIBM')";

			return dataConnection
				.Query(rd =>
					{
						var schema = rd.ToString(0);
						var name   = rd.ToString(1);

						return new ProcedureInfo
						{
							ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + name,
							CatalogName   = dataConnection.Connection.Database,
							SchemaName    = schema,
							ProcedureName = name,
						};
					},
					sql)
				.Where(p => IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || p.SchemaName == CurrenSchema)
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
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
						IsResult      = false
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

			return string.Format("{0} = '{1}'", schemaNameField, CurrenSchema);
		}
	}

	static class DB2Extensions
	{
		public static string ToString(this IDataReader reader, int i)
		{
			var value = Converter.ChangeTypeTo<string>(reader[i]);
			return value?.TrimEnd();
		}
	}
}
