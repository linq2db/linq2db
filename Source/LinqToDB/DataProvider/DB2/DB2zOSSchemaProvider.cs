using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using Common;
	using Data;
	using SchemaProvider;

	class DB2zOSSchemaProvider : DB2LUWSchemaProvider
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return new List<DataTypeInfo>
			{
				new DataTypeInfo { TypeName = "XML",                       CreateParameters = null,               DataType = "System.String"   },
				new DataTypeInfo { TypeName = "DECFLOAT",                  CreateParameters = "PRECISION",        DataType = "System.Decimal"  },
				new DataTypeInfo { TypeName = "DBCLOB",                    CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "CLOB",                      CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "BLOB",                      CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "LONG VARGRAPHIC",           CreateParameters = null,               DataType = "System.String"   },
				new DataTypeInfo { TypeName = "VARGRAPHIC",                CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "GRAPHIC",                   CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "BIGINT",                    CreateParameters = null,               DataType = "System.Int64"    },
				new DataTypeInfo { TypeName = "LONG VARCHAR FOR BIT DATA", CreateParameters = null,               DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "VARCHAR () FOR BIT DATA",   CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "VARBIN",                    CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "BINARY",                    CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "CHAR () FOR BIT DATA",      CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
				new DataTypeInfo { TypeName = "LONG VARCHAR",              CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "CHARACTER",                 CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "CHAR",                      CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "DECIMAL",                   CreateParameters = "PRECISION,SCALE",  DataType = "System.Decimal"  },
				new DataTypeInfo { TypeName = "INTEGER",                   CreateParameters = null,               DataType = "System.Int32"    },
				new DataTypeInfo { TypeName = "SMALLINT",                  CreateParameters = null,               DataType = "System.Int16"    },
				new DataTypeInfo { TypeName = "REAL",                      CreateParameters = null,               DataType = "System.Single"   },
				new DataTypeInfo { TypeName = "DOUBLE",                    CreateParameters = null,               DataType = "System.Double"   },
				new DataTypeInfo { TypeName = "VARCHAR",                   CreateParameters = "LENGTH",           DataType = "System.String"   },
				new DataTypeInfo { TypeName = "DATE",                      CreateParameters = null,               DataType = "System.DateTime" },
				new DataTypeInfo { TypeName = "TIME",                      CreateParameters = null,               DataType = "System.TimeSpan" },
				new DataTypeInfo { TypeName = "TIMESTAMP",                 CreateParameters = null,               DataType = "System.DateTime" },
				new DataTypeInfo { TypeName = "TIMESTMP",                  CreateParameters = null,               DataType = "System.DateTime" },
				new DataTypeInfo { TypeName = "ROWID",                     CreateParameters = "LENGTH",           DataType = "System.Byte[]"   },
			};
		}

		List<PrimaryKeyInfo> _primaryKeys;

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return _primaryKeys = dataConnection.Query(
				rd => new PrimaryKeyInfo
				{
					TableID        = dataConnection.Connection.Database + "." + rd.ToString(0) + "." + rd.ToString(1),
					PrimaryKeyName = rd.ToString(2),
					ColumnName     = rd.ToString(3),
					Ordinal        = Converter.ChangeTypeTo<int>(rd[4])
				},@"
					SELECT
						col.TBCREATOR,
						col.TBNAME,
						idx.NAME,
						col.NAME,
						col.KEYSEQ
					FROM
						SYSIBM.SYSCOLUMNS col
							JOIN SYSIBM.SYSINDEXES idx ON
								col.TBCREATOR = idx.TBCREATOR AND
								col.TBNAME    = idx.TBNAME
					WHERE
						col.KEYSEQ > 0 AND idx.UNIQUERULE = 'P' AND " + GetSchemaFilter("col.TBCREATOR") + @"
					ORDER BY
						col.TBCREATOR, col.TBNAME, col.KEYSEQ")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var sql = @"
				SELECT
					TBCREATOR,
					TBNAME,
					NAME,
					LENGTH,
					SCALE,
					NULLS,
					CASE WHEN DEFAULT IN ('I', 'J') THEN 'Y' ELSE 'N' END,
					COLNO,
					COLTYPE,
					REMARKS
				FROM
					SYSIBM.SYSCOLUMNS
				WHERE
					" + GetSchemaFilter("TBCREATOR");

			return dataConnection.Query(rd =>
				{
					var ci = new ColumnInfo
					{
						TableID     = dataConnection.Connection.Database + "." + rd.GetString(0) + "." + rd.GetString(1),
						Name        = rd.ToString(2),
						IsNullable  = rd.ToString(5) == "Y",
						IsIdentity  = rd.ToString(6) == "Y",
						Ordinal     = Converter.ChangeTypeTo<int> (rd[7]),
						DataType    = rd.ToString(8),
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
			return
			(
				from fk in dataConnection.Query(rd => new
				{
					name        = rd.ToString(0),
					thisTable   = dataConnection.Connection.Database + "." + rd.ToString(1)  + "." + rd.ToString(2),
					thisColumn  = rd.ToString(3),
					ordinal     = Converter.ChangeTypeTo<int>(rd[4]),
					otherTable  = dataConnection.Connection.Database + "." + rd.ToString(5)  + "." + rd.ToString(6),
				},@"
					SELECT
						A.RELNAME,
						A.CREATOR,
						A.TBNAME,
						B.COLNAME,
						B.COLSEQ,
						A.REFTBCREATOR,
						A.REFTBNAME
					FROM
						SYSIBM.SYSRELS A
							JOIN SYSIBM.SYSFOREIGNKEYS B ON
								A.CREATOR = B.CREATOR AND
								A.TBNAME  = B.TBNAME  AND
								A.RELNAME = B.RELNAME
					WHERE
						" + GetSchemaFilter("A.CREATOR") + @"
					ORDER BY
						A.CREATOR,
						A.RELNAME,
						B.COLSEQ")
				let   otherColumn = _primaryKeys.Where(pk => pk.TableID == fk.otherTable).ElementAtOrDefault(fk.ordinal - 1)
				where otherColumn != null
				select new ForeignKeyInfo
				{
					Name = fk.name,
					ThisTableID  = fk.thisTable,
					ThisColumn   = fk.thisColumn,
					Ordinal      = fk.ordinal,
					OtherTableID = fk.otherTable,
					OtherColumn  = otherColumn.ColumnName
				}
			).ToList();
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			return dataConnection
				.Query(rd =>
				{
					var schema     = rd.ToString(0);
					var name       = rd.ToString(1);
					var isFunction = rd.ToString(2) == "F";

					return new ProcedureInfo
					{
						ProcedureID   = dataConnection.Connection.Database + "." + schema + "." + name,
						CatalogName   = dataConnection.Connection.Database,
						SchemaName    = schema,
						ProcedureName = name,
						IsFunction    = isFunction,
					};
				},@"
					SELECT
						SCHEMA,
						NAME,
						ROUTINETYPE
					FROM
						SYSIBM.SYSROUTINES
					WHERE
						" + GetSchemaFilter("SCHEMA"))
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
					var length   = ConvertTo<long?>. From(rd["LENGTH"]);
					var scale    = ConvertTo<int?>.  From(rd["SCALE"]);
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
						SCHEMA,
						NAME,
						PARMNAME,
						TYPENAME,
						CASE ROWTYPE
							WHEN 'P' THEN 'IN'
							WHEN 'O' THEN 'OUT'
							WHEN 'B' THEN 'INOUT'
							WHEN 'S' THEN 'IN'
						END,

						ORDINAL,
						LENGTH,
						SCALE
					FROM
						SYSIBM.SYSPARMS
					WHERE
						" + GetSchemaFilter("SCHEMA"))
				.ToList();
		}
	}
}
