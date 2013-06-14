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

	class DB2SchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

			return dts.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("SQL_TYPE_NAME"),
					DataType         = t.Field<string>("FRAMEWORK_TYPE"),
					CreateParameters = t.Field<string>("CREATE_PARAMS"),
				})
				.ToList();
		}

		static readonly string[] _systemSchemas = new[]
		{
			"SYSPUBLIC", "SYSIBM", "SYSCAT", "SYSIBMADM", "SYSSTAT", "SYSTOOLS"
		};

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where
					new[] {"TABLE", "VIEW"}.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				where
					ExcludedSchemas.Length != 0 || IncludedSchemas.Length != 0 ||
					ExcludedSchemas.Length == 0 && IncludedSchemas.Length == 0 && !_systemSchemas.Contains(schema)
				select new TableInfo
				{
					TableID         = catalog + '.' + schema + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema.IsNullOrEmpty(),
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description     = t.Field<string>("REMARKS"),
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
						id   = dataConnection.Connection.Database + "." + rd[0] + "." + rd[1],
						name = rd.GetString(2),
						cols = rd.GetString(3).Split('+').Skip(1).ToArray(),
					},@"
					SELECT
						TABSCHEMA,
						TABNAME,
						INDNAME,
						COLNAMES
					FROM
						SYSCAT.INDEXES
					WHERE
						UNIQUERULE = 'P'")
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
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return _columns =
			(
				from c in cs.AsEnumerable()
				let schema = c.Field<string>("TABLE_SCHEMA")
				let table  = c.Field<string>("TABLE_NAME")
				let name   = c.Field<string>("COLUMN_NAME")
				join c2 in 
					dataConnection.Query(
						rd => new
						{
							schema     = rd.GetString(0),
							table      = rd.GetString(1),
							name       = rd.GetString(2),
							length     = Converter.ChangeTypeTo<int>(rd[3]),
							scale      = Converter.ChangeTypeTo<int>(rd[4]),
							isIdentity = rd.GetString(5) == "Y"
						},@"
						SELECT
							TABSCHEMA,
							TABNAME,
							COLNAME,
							LENGTH,
							SCALE,
							IDENTITY
						FROM
							SYSCAT.COLUMNS").ToList()
				on new { schema, table, name } equals new { c2.schema, c2.table, c2.name }
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + schema + "." + table,
					Name         = name,
					IsNullable   = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal      = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType     = c.Field<string>("DATA_TYPE_NAME"),
					Length       = c2.length,
					Precision    = c2.length,
					Scale        = c2.scale,
					IsIdentity   = c2.isIdentity,
					SkipOnInsert = c2.isIdentity,
					SkipOnUpdate = c2.isIdentity,
					Description  = c.Field<string>("REMARKS"),
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			return dataConnection
				.Query(rd => new
				{
					name         = rd.GetString(0),
					thisTable    = dataConnection.Connection.Database + "." + rd.GetString(1)  + "." + rd.GetString(2),
					thisColumns  = rd.GetString(3),
					otherTable   = dataConnection.Connection.Database + "." + rd.GetString(4)  + "." + rd.GetString(5),
					otherColumns = rd.GetString(6),
				},@"
					SELECT
						CONSTNAME,
						TABSCHEMA,
						TABNAME,
						FK_COLNAMES,
						REFTABSCHEMA,
						REFTABNAME,
						PK_COLNAMES
					FROM SYSCAT.REFERENCES")
				.SelectMany(fk =>
				{
					var thisTable    = _columns.Where(c => c.TableID == fk.thisTable). OrderByDescending(c => c.Length).ToList();
					var otherTable   = _columns.Where(c => c.TableID == fk.otherTable).OrderByDescending(c => c.Length).ToList();
					var thisColumns  = fk.thisColumns. Trim();
					var otherColumns = fk.otherColumns.Trim();

					var list = new List<ForeingKeyInfo>();

					for (var i = 0; thisColumns.Length > 0; i++)
					{
						var thisColumn  = thisTable. First(c => thisColumns. StartsWith(c.Name));
						var otherColumn = otherTable.First(c => otherColumns.StartsWith(c.Name));

						list.Add(new ForeingKeyInfo
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

		protected override string GetDbType(string columnType, DataTypeInfo dataType, int length, int prec, int scale)
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

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "XML"                       : return DataType.Xml;      // Xml             System.String
				case "DECFLOAT"                  : return DataType.Decimal;  // DecimalFloat    System.Decimal
				case "DBCLOB"                    : return DataType.Text;     // DbClob          System.String
				case "CLOB"                      : return DataType.Text;     // Clob            System.String
				case "BLOB"                      : return DataType.Blob;     // Blob            System.Byte[]
				case "LONG VARGRAPHIC"           : return DataType.Text;     // LongVarGraphic  System.String
				case "VARGRAPHIC"                : return DataType.Text;     // VarGraphic      System.String
				case "GRAPHIC"                   : return DataType.Text;     // Graphic         System.String
				case "BIGINT"                    : return DataType.Int64;    // BigInt          System.Int64
				case "LONG VARCHAR FOR BIT DATA" : return DataType.VarBinary;// LongVarBinary   System.Byte[]
				case "VARCHAR () FOR BIT DATA"   : return DataType.VarBinary;// VarBinary       System.Byte[]
				case "CHAR () FOR BIT DATA"      : return DataType.Binary;   // Binary          System.Byte[]
				case "LONG VARCHAR"              : return DataType.VarChar;  // LongVarChar     System.String
				case "CHAR"                      : return DataType.Char;     // Char            System.String
				case "DECIMAL"                   : return DataType.Decimal;  // Decimal         System.Decimal
				case "INTEGER"                   : return DataType.Int32;    // Integer         System.Int32
				case "SMALLINT"                  : return DataType.Int16;    // SmallInt        System.Int16
				case "REAL"                      : return DataType.Single;   // Real            System.Single
				case "DOUBLE"                    : return DataType.Double;   // Double          System.Double
				case "VARCHAR"                   : return DataType.VarChar;  // VarChar         System.String
				case "DATE"                      : return DataType.Date;     // Date            System.DateTime
				case "TIME"                      : return DataType.Time;     // Time            System.TimeSpan
				case "TIMESTAMP"                 : return DataType.Timestamp;// Timestamp       System.DateTime
			}

			return DataType.Undefined;
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
			return dataConnection
				.Query(rd =>
				{
					var schema = rd.GetString(0).Trim();
					var name   = rd.GetString(1).Trim();

					return new ProcedureInfo
					{
						ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + name,
						CatalogName   = dataConnection.Connection.Database,
						SchemaName    = schema,
						ProcedureName = name,
					};
				},
				@"SELECT PROCSCHEMA, PROCNAME FROM SYSCAT.PROCEDURES")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			return dataConnection
				.Query(rd =>
				{
					var schema   = rd.GetString(0).Trim();
					var procname = rd.GetString(1).Trim();
					var length   = ConvertTo<int>.From(rd["LENGTH"]);
					var mode     = rd.GetString(4);

					return new ProcedureParameterInfo
					{
						ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + procname,
						ParameterName = rd.GetString(2),
						DataType      = rd.GetString(3),
						Ordinal       = ConvertTo<int>.From(rd["ORDINAL"]),
						Length        = length,
						Precision     = length,
						Scale         = ConvertTo<int>.From(rd["SCALE"]),
						IsIn          = mode.Contains("IN"),
						IsOut         = mode.Contains("OUT"),
						IsResult      = false
					};
				}, @"
					SELECT
						PROCSCHEMA,
						PROCNAME,
						PARMNAME,
						TYPENAME,
						PARM_MODE,

						ORDINAL,
						LENGTH,
						SCALE
					FROM SYSCAT.PROCPARMS")
				.ToList();
		}
	}
}
