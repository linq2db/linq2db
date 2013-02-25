using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Data;

using Microsoft.SqlServer.Types;

namespace LinqToDB.DataProvider
{
	using Data;
	using SchemaProvider;

	class SqlServerSchemaProvider : SchemaProviderBase
	{
		public override DatabaseSchema GetSchema(DataConnection dataConnection)
		{
			var dbConnection = (SqlConnection)dataConnection.Connection;
			var dataTypes    = dbConnection.GetSchema("DataTypes");
			var tables       = dataConnection.Query(
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
							major_id = t.object_id and
							minor_id = 0           and
							class    = 1           and
							name     = N'microsoft_database_tools_support'
					) IS NULL")
				.Select(t => new TableSchema
				{
					CatalogName     = t.catalog,
					SchemaName      = t.schema,
					TableName       = t.name,
					TypeName        = ToValidName(t.name),
					Description     = t.desc,
					IsView          = t.type == "VIEW",
					IsDefaultSchema = t.schema == "dbo",
					Columns         = new List<ColumnSchema>(),
					ForeignKeys     = new List<ForeignKeySchema>(),
				})
				.ToList();

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

			var columns =
				from c  in cols

				join dt in dataTypes.AsEnumerable()
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
				var columnName = column.c.name;
				var columnType = column.c.dataType;

				Type systemType = null;

				if (column.dt != null)
					systemType = Type.GetType(column.dt.Field<string>("DataType"));

				if (systemType == null)
				{
					switch (columnType)
					{
						case "hierarchyid" : systemType = typeof(SqlHierarchyId); break;
						case "geography"   : systemType = typeof(SqlGeography);   break;
						case "geometry"    : systemType = typeof(SqlGeometry);    break;
					}
				}

				var dbType = columnType;

				if (column.dt != null)
				{
					var format = column.dt.Field<string>("CreateFormat");
					var parms  = column.dt.Field<string>("CreateParameters");

					if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(parms))
					{
						var paramNames  = parms.Split(',');
						var paramValues = new object[paramNames.Length];

						for (var i = 0; i < paramNames.Length; i++)
						{
							switch (paramNames[i].Trim())
							{
								case "length"     :
								case "max length" : paramValues[i] = column.c.length; break;
								case "precision"  : paramValues[i] = column.c.prec;   break;
								case "scale"      : paramValues[i] = column.c.scale;  break;
							}
						}

						if (paramValues.All(v => v != null))
							dbType = string.Format(format, paramValues);
					}
				}

				var dataType     = DataType.Undefined;
				var skipOnInsert = false;
				var skipOnUpdate = false;

				switch (columnType)
				{
					case "image"            : dataType = DataType.Image;          break;
					case "text"             : dataType = DataType.Text;           break;
					case "binary"           : dataType = DataType.Binary;         break;
					case "tinyint"          : dataType = DataType.SByte;          break;
					case "date"             : dataType = DataType.Date;           break;
					case "time"             : dataType = DataType.Time;           break;
					case "bit"              : dataType = DataType.Boolean;        break;
					case "smallint"         : dataType = DataType.Int16;          break;
					case "decimal"          : dataType = DataType.Decimal;        break;
					case "int"              : dataType = DataType.Int32;          break;
					case "smalldatetime"    : dataType = DataType.SmallDateTime;  break;
					case "real"             : dataType = DataType.Single;         break;
					case "money"            : dataType = DataType.Money;          break;
					case "datetime"         : dataType = DataType.DateTime;       break;
					case "float"            : dataType = DataType.Double;         break;
					case "numeric"          : dataType = DataType.Decimal;        break;
					case "smallmoney"       : dataType = DataType.SmallMoney;     break;
					case "datetime2"        : dataType = DataType.DateTime2;      break;
					case "bigint"           : dataType = DataType.Int64;          break;
					case "varbinary"        : dataType = DataType.VarBinary;      break;
					case "timestamp"        : dataType = DataType.Timestamp; skipOnInsert = skipOnUpdate = true; break;
					case "sysname"          : dataType = DataType.NVarChar;       break;
					case "nvarchar"         : dataType = DataType.NVarChar;       break;
					case "varchar"          : dataType = DataType.VarChar;        break;
					case "ntext"            : dataType = DataType.NText;          break;
					case "uniqueidentifier" : dataType = DataType.Guid;           break;
					case "datetimeoffset"   : dataType = DataType.DateTimeOffset; break;
					case "sql_variant"      : dataType = DataType.Variant;        break;
					case "xml"              : dataType = DataType.Xml;            break;
					case "char"             : dataType = DataType.Char;           break;
					case "nchar"            : dataType = DataType.NChar;          break;
					case "hierarchyid"      :
					case "geography"        :
					case "geometry"         : dataType = DataType.Udt;            break;
				}

				var isNullable = column.c.isNullable;

				column.t.Columns.Add(new ColumnSchema
				{
					Table           = column.t,
					ColumnName      = columnName,
					ColumnType      = dbType,
					IsNullable      = isNullable,
					MemberName      = ToValidName(columnName),
					MemberType      = ToTypeName(systemType, isNullable),
					SystemType      = systemType ?? typeof(object),
					DataType        = dataType,
					SkipOnInsert    = skipOnInsert || column.c.isIdentity,
					SkipOnUpdate    = skipOnUpdate || column.c.isIdentity,
					IsPrimaryKey    = column.pk != null,
					PrimaryKeyOrder = column.pk != null ? column.pk.ordinal : -1,
					IsIdentity      = column.c.isIdentity,
					Description     = column.c.desc,
				});
			}

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

			return ProcessSchema(new DatabaseSchema
			{
				DataSource    = dbConnection.DataSource,
				Database      = dbConnection.Database,
				ServerVersion = dbConnection.ServerVersion,
				Tables        = tables
			});
		}
	}
}
