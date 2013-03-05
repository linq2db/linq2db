using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Data;
	using SchemaProvider;

	class SqlServerSchemaProvider : SchemaProviderBase
	{
		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (options == null)
				options = new GetSchemaOptions();

			var dbConnection = (DbConnection)dataConnection.Connection;
			var dataTypes    = dbConnection.GetSchema("DataTypes").AsEnumerable().ToList();

			List<TableSchema>     tables;
			List<ProcedureSchema> procedures;

			if (options.GetTables)
			{
				#region Tables

				tables = dataConnection.Query(
					new { catalog = "", schema = "", name = "", type = "", desc = "" }, @"
					SELECT
						TABLE_CATALOG as catalog,
						TABLE_SCHEMA  as [schema],
						TABLE_NAME    as name,
						TABLE_TYPE    as type,
						ISNULL(CONVERT(varchar(8000), x.Value), '') as [desc]
					FROM
						INFORMATION_SCHEMA.TABLES s
						LEFT JOIN
							sys.tables t
						ON
							OBJECT_ID(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) = t.object_id
						LEFT JOIN
							sys.extended_properties x
						ON
							OBJECT_ID(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) = x.major_id AND
							x.minor_id = 0 AND 
							x.name = 'MS_Description'
					WHERE
						t.object_id IS NULL OR
						t.is_ms_shipped <> 1 AND
						(
							SELECT
								major_id
							FROM
								sys.extended_properties
							WHERE
								major_id = t.object_id AND
								minor_id = 0           AND
								class    = 1           AND
								name     = N'microsoft_database_tools_support'
						) IS NULL")
					.Select(t => new TableSchema
					{
						CatalogName     = t.catalog,
						SchemaName      = t.schema,
						TableName       = t.name,
						TypeName        = ToValidName(t.name),
						Description     = t.desc,
						IsView          = t.type   == "VIEW",
						IsDefaultSchema = t.schema == "dbo",
						Columns         = new List<ColumnSchema>(),
						ForeignKeys     = new List<ForeignKeySchema>(),
					})
					.ToList();
			
				#endregion

				#region PKs

				var pks = dataConnection.Query(
					new { id = "", pkName = "", columnName = "", ordinal = 0 }, @"
					SELECT
						(k.TABLE_CATALOG + '.' + k.TABLE_SCHEMA + '.' + k.TABLE_NAME) as id,
						k.CONSTRAINT_NAME                                             as pkName,
						k.COLUMN_NAME                                                 as columnName,
						k.ORDINAL_POSITION                                            as ordinal
					FROM
						INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
						JOIN
							INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
						ON
							k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
							k.CONSTRAINT_SCHEMA  = c.CONSTRAINT_SCHEMA AND
							k.CONSTRAINT_NAME    = c.CONSTRAINT_NAME
					WHERE
						c.CONSTRAINT_TYPE='PRIMARY KEY'")
					.ToList();

				#endregion

				#region Columns

				var cols = dataConnection.Query(
					new { id = "", name = "", isNullable = false, ordinal = 0, dataType = "", length = 0, prec = 0, scale = 0, desc = "", isIdentity = false }, @"
					SELECT
						(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) as id,
						COLUMN_NAME                                             as name,
						(CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END)       as isNullable,
						ORDINAL_POSITION                                        as ordinal,
						c.DATA_TYPE                                             as dataType,
						CHARACTER_MAXIMUM_LENGTH                                as length,
						ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)           as prec,
						NUMERIC_SCALE                                           as scale,
						ISNULL(CONVERT(varchar(8000), x.Value), '')             as [desc],
						COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as isIdentity
					FROM
						INFORMATION_SCHEMA.COLUMNS c
						LEFT JOIN
							sys.extended_properties x
						ON
							OBJECT_ID(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) = x.major_id AND
							ORDINAL_POSITION = x.minor_id AND
							x.name = 'MS_Description'")
					.ToList();

				var columns =
					from c  in cols

					join dt in dataTypes
						on c.dataType equals dt.Field<string>("TypeName") into g1
					from dt in g1.DefaultIfEmpty()

					join pk in pks
						on c.id + "." + c.name equals pk.id + "." + pk.columnName into g2
					from pk in g2.DefaultIfEmpty()

					join t  in tables on c.id equals t.CatalogName + "." + t.SchemaName + "." + t.TableName

					orderby c.ordinal
					select new { t, c, dt, pk };

				foreach (var column in columns)
				{
					var columnType = column.c.dataType;
					var systemType = GetSystemType(columnType, column.dt);
					var isNullable = column.c.isNullable;

					var skipOnInsert = false;
					var skipOnUpdate = false;

					switch (columnType)
					{
						case "timestamp" : skipOnInsert = skipOnUpdate = true; break;
					}

					column.t.Columns.Add(new ColumnSchema
					{
						Table           = column.t,
						ColumnName      = column.c.name,
						ColumnType      = GetDbType(columnType, column.dt, column.c.length, column.c.prec, column.c.scale),
						IsNullable      = isNullable,
						MemberName      = ToValidName(column.c.name),
						MemberType      = ToTypeName(systemType, isNullable),
						SystemType      = systemType ?? typeof(object),
						DataType        = GetDataType(columnType),
						SkipOnInsert    = skipOnInsert || column.c.isIdentity,
						SkipOnUpdate    = skipOnUpdate || column.c.isIdentity,
						IsPrimaryKey    = column.pk != null,
						PrimaryKeyOrder = column.pk != null ? column.pk.ordinal : -1,
						IsIdentity      = column.c.isIdentity,
						Description     = column.c.desc,
					});
				}

				#endregion

				#region FK

				var fks = dataConnection.Query(
					new { name = "", thisTable = "", thisColumn = "", otherTable = "", otherColumn = "", ordinal = 0 }, @"
					SELECT
						rc.CONSTRAINT_NAME                                             as name,
						fk.TABLE_CATALOG + '.' + fk.TABLE_SCHEMA + '.' + fk.TABLE_NAME as thisTable,
						fk.COLUMN_NAME                                                 as thisColumn,
						pk.TABLE_CATALOG + '.' + pk.TABLE_SCHEMA + '.' + pk.TABLE_NAME as otherTable,
						pk.COLUMN_NAME                                                 as otherColumn,
						cu.ORDINAL_POSITION                                            as ordinal
					FROM
						INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
						JOIN
							INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE fk
						ON
							rc.CONSTRAINT_CATALOG = fk.CONSTRAINT_CATALOG AND
							rc.CONSTRAINT_SCHEMA  = fk.CONSTRAINT_SCHEMA  AND
							rc.CONSTRAINT_NAME    = fk.CONSTRAINT_NAME
						JOIN
							INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE pk
						ON
							rc.UNIQUE_CONSTRAINT_CATALOG = pk.CONSTRAINT_CATALOG AND
							rc.UNIQUE_CONSTRAINT_SCHEMA  = pk.CONSTRAINT_SCHEMA AND
							rc.UNIQUE_CONSTRAINT_NAME    = pk.CONSTRAINT_NAME
						JOIN
							INFORMATION_SCHEMA.KEY_COLUMN_USAGE cu
						ON
							rc.CONSTRAINT_CATALOG = cu.CONSTRAINT_CATALOG AND
							rc.CONSTRAINT_SCHEMA  = cu.CONSTRAINT_SCHEMA  AND
							rc.CONSTRAINT_NAME    = cu.CONSTRAINT_NAME
					ORDER BY
						ThisTable,
						Ordinal")
					.ToList();

				foreach (var fk in fks)
				{
					var thisTable   = (from t in tables             where t.ID         == fk.thisTable   select t).Single();
					var otherTable  = (from t in tables             where t.ID         == fk.otherTable  select t).Single();
					var thisColumn  = (from c in thisTable. Columns where c.ColumnName == fk.thisColumn  select c).Single();
					var otherColumn = (from c in otherTable.Columns where c.ColumnName == fk.otherColumn select c).Single();

					var key = thisTable.ForeignKeys.FirstOrDefault(f => f.KeyName == fk.name);

					if (key == null)
					{
						key = new ForeignKeySchema
						{
							KeyName      = fk.name,
							MemberName   = fk.name,
							ThisTable    = thisTable,
							OtherTable   = otherTable,
							ThisColumns  = new List<ColumnSchema>(),
							OtherColumns = new List<ColumnSchema>(),
							CanBeNull    = true,
						};
						thisTable.ForeignKeys.Add(key);
					}

					key.ThisColumns. Add(thisColumn);
					key.OtherColumns.Add(otherColumn);
				}

				#endregion
			}
			else
				tables = new List<TableSchema>();

			if (options.GetProcedures)
			{
				#region Procedures

				var procs = dataConnection.Query(
					new { catalog = "", schema = "", name = "", type = "", dataType = "" }, @"
					SELECT
						SPECIFIC_CATALOG as catalog,
						SPECIFIC_SCHEMA  as [schema],
						SPECIFIC_NAME    as name,
						ROUTINE_TYPE     as type,
						DATA_TYPE        as dataType
					FROM
						INFORMATION_SCHEMA.ROUTINES")
					.ToList();

				var procPparams = dataConnection.Query(
					new
					{
						catalog = "", schema = "", procName = "", ordinal = 0, mode = "", isResult = "",
						paramName = "", dataType = "", length = (int?)null, precision = 0, scale = 0
					}, @"
					SELECT
						SPECIFIC_CATALOG         as catalog,
						SPECIFIC_SCHEMA          as [schema],
						SPECIFIC_NAME            as procName,
						ORDINAL_POSITION         as ordinal,
						PARAMETER_MODE           as mode,
						IS_RESULT                as isResult,
						PARAMETER_NAME           as paramName,
						DATA_TYPE                as dataType,
						CHARACTER_MAXIMUM_LENGTH as length,
						NUMERIC_PRECISION        as precision,
						NUMERIC_SCALE            as scale
					FROM
						INFORMATION_SCHEMA.PARAMETERS")
					.ToList();

				procedures =
				(
					from sp in procs
					join p  in procPparams
						on     sp.catalog + '.' + sp.schema + '.' + sp.name
						equals p. catalog + '.' + p. schema + '.' + p. procName
					into gr
					select new ProcedureSchema
					{
						CatalogName     = sp.catalog,
						SchemaName      = sp.schema,
						ProcedureName   = sp.name,
						MemberName      = ToValidName(sp.name),
						IsFunction      = sp.type == "FUNCTION",
						IsTableFunction = sp.type == "FUNCTION" && sp.dataType == "TABLE",
						IsDefaultSchema = sp.schema == "dbo",
						Parameters      =
						(
							from pr in gr

							join dt in dataTypes
								on pr.dataType equals dt.Field<string>("TypeName") into g1
							from dt in g1.DefaultIfEmpty()

							let systemType = GetSystemType(pr.dataType, dt)

							orderby pr.ordinal
							select new ParameterSchema
							{
								SchemaName    = pr.paramName,
								SchemaType    = GetDbType(pr.dataType, dt, pr.length ?? 0, pr.precision, pr.scale),
								IsIn          = pr.mode == "IN"  || pr.mode == "INOUT",
								IsOut         = pr.mode == "OUT" || pr.mode == "INOUT",
								IsResult      = pr.isResult == "YES",
								Size          = pr.length,
								ParameterName = ToValidName(pr.paramName),
								ParameterType = ToTypeName(systemType, true),
								SystemType    = systemType ?? typeof(object),
								DataType      = GetDataType(pr.dataType)
							}
						).ToList()
					} into ps
					where ps.Parameters.All(p => p.SchemaType != "table type")
					select ps
				).ToList();

				var current = 1;

				foreach (var procedure in procedures)
				{
					if ((!procedure.IsFunction || procedure.IsTableFunction) && options.LoadProcedure(procedure))
					{
						var commandText = "[{0}].[{1}].[{2}]".Args(
							procedure.CatalogName, procedure.SchemaName, procedure.ProcedureName);

						CommandType     commandType;
						DataParameter[] parameters;

						if (procedure.IsTableFunction)
						{
							commandText = "SELECT * FROM " + commandText + "(";

							for (var i = 0; i < procedure.Parameters.Count; i++)
							{
								if (i != 0)
									commandText += ",";
								commandText += "NULL";
							}

							commandText += ")";
							commandType = CommandType.Text;
							parameters  = new DataParameter[0];
						}
						else
						{
							commandType = CommandType.StoredProcedure;
							parameters  = procedure.Parameters.Select(p =>
								new DataParameter
								{
									Name      = p.ParameterName,
									Value     = DBNull.Value,
									DataType  = p.DataType,
									Size      = p.Size,
									Direction =
										p.IsIn ?
											p.IsOut ?
												ParameterDirection.InputOutput :
												ParameterDirection.Input :
											ParameterDirection.Output
								}).ToArray();
						}

						{
							try
							{
								using (var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.SchemaOnly, parameters))
								{
									var st = rd.Reader.GetSchemaTable();

									if (st == null)
										continue;

									procedure.ResultTable = new TableSchema
									{
										IsProcedureResult = true,
										TypeName          = ToValidName(procedure.ProcedureName + "Result"),
										ForeignKeys       = new List<ForeignKeySchema>(),
										Columns           =
										(
											from r in st.AsEnumerable()

											let columnType = r.Field<string>("DataTypeName")
											let columnName = r.Field<string>("ColumnName")
											let isNullable = r.Field<bool>  ("AllowDBNull")

											join dt in dataTypes
												on columnType equals dt.Field<string>("TypeName") into g1
											from dt in g1.DefaultIfEmpty()

											let systemType = GetSystemType(columnType, dt)
											let length     = r.Field<int> ("ColumnSize")
											let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
											let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])

											select new ColumnSchema
											{
												ColumnName = columnName,
												ColumnType = GetDbType(columnType, dt, length, precision, scale),
												IsNullable = isNullable,
												MemberName = ToValidName(columnName),
												MemberType = ToTypeName(systemType, isNullable),
												SystemType = systemType ?? typeof(object),
												DataType   = GetDataType(columnType),
												IsIdentity = r.Field<bool>("IsIdentity"),
											}
										).ToList()
									};

									foreach (var column in procedure.ResultTable.Columns)
										column.Table = procedure.ResultTable;

									procedure.SimilarTables =
									(
										from t in tables
										where t.Columns.Count == procedure.ResultTable.Columns.Count
										let zip = t.Columns.Zip(procedure.ResultTable.Columns, (c1, c2) => new { c1, c2 })
										where zip.All(z => z.c1.ColumnName == z.c2.ColumnName && z.c1.SystemType == z.c2.SystemType)
										select t
									).ToList();
								}
							}
							catch (Exception ex)
							{
								procedure.ResultException = ex;
							}
						}
					}

					options.ProcedureLoadingProgress(procedures.Count, current++);
				}

				#endregion
			}
			else
				procedures = new List<ProcedureSchema>();

			return ProcessSchema(new DatabaseSchema
			{
				DataSource    = dbConnection.DataSource,
				Database      = dbConnection.Database,
				ServerVersion = dbConnection.ServerVersion,
				Tables        = tables,
				Procedures    = procedures,
			});
		}

		static string GetDbType(string columnType, DataRow dataType, int length, int prec, int scale)
		{
			var dbType = columnType;

			if (dataType != null)
			{
				var format = dataType.Field<string>("CreateFormat");
				var parms  = dataType.Field<string>("CreateParameters");

				if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(parms))
				{
					var paramNames  = parms.Split(',');
					var paramValues = new object[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim())
						{
							case "length"     :
							case "max length" : paramValues[i] = length; break;
							case "precision"  : paramValues[i] = prec;   break;
							case "scale"      : paramValues[i] = scale;  break;
						}
					}

					if (paramValues.All(v => v != null))
						dbType = format.Args(paramValues);
				}
			}

			return dbType;
		}

		static DataType GetDataType(string columnType)
		{
			switch (columnType)
			{
				case "image"            : return DataType.Image;
				case "text"             : return DataType.Text;
				case "binary"           : return DataType.Binary;
				case "tinyint"          : return DataType.SByte;
				case "date"             : return DataType.Date;
				case "time"             : return DataType.Time;
				case "bit"              : return DataType.Boolean;
				case "smallint"         : return DataType.Int16;
				case "decimal"          : return DataType.Decimal;
				case "int"              : return DataType.Int32;
				case "smalldatetime"    : return DataType.SmallDateTime;
				case "real"             : return DataType.Single;
				case "money"            : return DataType.Money;
				case "datetime"         : return DataType.DateTime;
				case "float"            : return DataType.Double;
				case "numeric"          : return DataType.Decimal;
				case "smallmoney"       : return DataType.SmallMoney;
				case "datetime2"        : return DataType.DateTime2;
				case "bigint"           : return DataType.Int64;
				case "varbinary"        : return DataType.VarBinary;
				case "timestamp"        : return DataType.Timestamp;
				case "sysname"          : return DataType.NVarChar;
				case "nvarchar"         : return DataType.NVarChar;
				case "varchar"          : return DataType.VarChar;
				case "ntext"            : return DataType.NText;
				case "uniqueidentifier" : return DataType.Guid;
				case "datetimeoffset"   : return DataType.DateTimeOffset;
				case "sql_variant"      : return DataType.Variant;
				case "xml"              : return DataType.Xml;
				case "char"             : return DataType.Char;
				case "nchar"            : return DataType.NChar;
				case "hierarchyid"      :
				case "geography"        :
				case "geometry"         : return DataType.Udt;
			}

			return DataType.Undefined;
		}

		static Type GetSystemType(string columnType, DataRow dataType)
		{
			Type systemType = null;

			if (dataType != null)
				systemType = Type.GetType(dataType.Field<string>("DataType"));

			if (systemType != null)
				return systemType;

			switch (columnType)
			{
				case "hierarchyid" :
				case "geography"   :
				case "geometry"    : return SqlServerDataProvider.GetUdtType(columnType);
			}

			return null;
		}
	}
}
