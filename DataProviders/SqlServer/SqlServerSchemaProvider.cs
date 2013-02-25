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
			var descriptions = dataConnection.Query(new { id = "", description = "" }, @"
				SELECT
					s.TABLE_CATALOG + '.' + s.TABLE_SCHEMA + '.' + s.TABLE_NAME as id,
					ISNULL(CONVERT(varchar(8000), x.Value), '')                 as description
				FROM
					INFORMATION_SCHEMA.TABLES s
					JOIN 
						sys.extended_properties x 
					ON 
						OBJECT_ID(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) = x.major_id AND 
						x.minor_id = 0 AND 
						x.name = 'MS_Description'")
				.ToList();

			var tables =
			(
				from t in dbConnection.GetSchema("Tables").AsEnumerable()
				where t.Field<string>("TABLE_TYPE") == "BASE TABLE"
				let catalogName = t.Field<string>("TABLE_CATALOG")
				let schemaName  = t.Field<string>("TABLE_SCHEMA")
				let tableName   = t.Field<string>("TABLE_NAME")
				select new TableSchema
				{
					CatalogName     = catalogName,
					SchemaName      = schemaName,
					TableName       = tableName,
					TypeName        = ToValidName(tableName),
					Description     = descriptions
						.Where (d => d.id == catalogName + "." + schemaName + "." + tableName)
						.Select(d => d.description)
						.FirstOrDefault(),
					IsDefaultSchema = schemaName == "dbo",
					Columns         = new List<ColumnSchema>(),
				}
			).ToList();

			var pks = dataConnection.Query(new { id = "", pkName = "", columnName = "", ordinal = 0 }, @"
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

			var colProps = dataConnection.Query(new { id = "", isIdentity = false, description = "" }, @"
				SELECT
					(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME + '.' + COLUMN_NAME)                         as id,
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as isIdentity,
					ISNULL(CONVERT(varchar(8000), x.Value), '')                                                         as description
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
				from c  in dbConnection.GetSchema("Columns").AsEnumerable()
				let cid = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME")
				let cnm = c.Field<string>("COLUMN_NAME")

				join dt in dataTypes.AsEnumerable()
					on c.Field<string>("DATA_TYPE") equals dt.Field<string>("TypeName") into g1
				from dt in g1.DefaultIfEmpty()

				join pk in pks
					on cid + "." + cnm equals pk.id + "." + pk.columnName into g2
				from pk in g2.DefaultIfEmpty()

				join cp in colProps
					on cid + "." + cnm equals cp.id into g3
				from cp in g3.DefaultIfEmpty()

				join t  in tables on cid equals t.CatalogName + "." + t.SchemaName + "." + t.TableName

				orderby c.Field<int>("ORDINAL_POSITION")
				select new { t, c, dt, pk, cp };

			foreach (var column in columns)
			{
				var columnName = column.c.Field<string>("COLUMN_NAME");
				var columnType = column.c.Field<string>("DATA_TYPE");

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
								case "length"     : paramValues[i] = column.c["CHARACTER_OCTET_LENGTH"];   break;
								case "max length" : paramValues[i] = column.c["CHARACTER_MAXIMUM_LENGTH"]; break;
								case "precision"  : paramValues[i] = column.c["NUMERIC_PRECISION"];        break;
								case "scale"      : paramValues[i] = column.c["NUMERIC_SCALE"];            break;
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

				var isNullable = column.c.Field<string>("IS_NULLABLE") == "YES";

				column.t.Columns.Add(new ColumnSchema
				{
					ColumnName      = columnName,
					ColumnType      = dbType,
					IsNullable      = isNullable,
					MemberName      = ToValidName(columnName),
					MemberType      = ToTypeName(systemType, isNullable),
					SystemType      = systemType ?? typeof(object),
					DataType        = dataType,
					SkipOnInsert    = skipOnInsert || (column.cp != null && column.cp.isIdentity),
					SkipOnUpdate    = skipOnUpdate || (column.cp != null && column.cp.isIdentity),
					IsPrimaryKey    = column.pk != null,
					PrimaryKeyOrder = column.pk != null ? column.pk.ordinal : -1,
					IsIdentity      = column.cp != null && column.cp.isIdentity,
					Description     = column.cp != null ? column.cp.description : null,
				});
			}

			return new DatabaseSchema
			{
				DataSource    = dbConnection.DataSource,
				Database      = dbConnection.Database,
				ServerVersion = dbConnection.ServerVersion,
				Tables        = tables
			};
		}
	}
}
