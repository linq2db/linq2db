using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;
	using SchemaProvider;

	class PostgreSQLSchemaProvider : SchemaProviderBase
	{
		PostgreSQLDataProvider _dataProvider;

		public PostgreSQLSchemaProvider(PostgreSQLDataProvider dataProvider)
		{
			_dataProvider = dataProvider;
		}
//		protected override string GetDataSourceName(DbConnection dbConnection)
//		{
//			return ((dynamic)dbConnection).HostName;
//		}
//
//		protected override string GetDatabaseName(DbConnection dbConnection)
//		{
//			return ((dynamic)dbConnection).DatabaseName;
//		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return new[]
			{
				new DataTypeInfo { TypeName = "name",    DataType = typeof(string).FullName, CreateFormat = "", CreateParameters = "" },
				new DataTypeInfo { TypeName = "oid",     DataType = typeof(int).   FullName, CreateFormat = "", CreateParameters = "" },
				new DataTypeInfo { TypeName = "xid",     DataType = typeof(int).   FullName, CreateFormat = "", CreateParameters = "" },
				new DataTypeInfo { TypeName = "regproc", DataType = typeof(object).FullName, CreateFormat = "", CreateParameters = "" },
				new DataTypeInfo { TypeName = "text",    DataType = typeof(string).FullName, CreateFormat = "", CreateParameters = "" },
			}.ToList();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var sql = (@"
				SELECT
					table_catalog || '.' || table_schema || '.' || table_name as TableID,
					table_catalog                                             as CatalogName,
					table_schema                                              as SchemaName,
					table_name                                                as TableName,
					table_schema = 'public'                                   as IsDefaultSchema,
					table_type = 'VIEW'                                       as IsView
				FROM
					information_schema.tables");

			if (ExcludedSchemas.Length != 0 || IncludedSchemas.Length != 0)
				sql += @"
				WHERE
					table_schema NOT IN ('pg_catalog','information_schema')";

			return dataConnection.Query<TableInfo>(sql).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
				dataConnection.Query<PrimaryKeyInfo>(@"
					SELECT
						current_database() || '.' || pg_namespace.nspname || '.' || pg_class.relname as TableID,
						pg_constraint.conname                                                        as PrimaryKeyName,
						(select attname from pg_attribute where attrelid = pg_constraint.conrelid and attnum = pg_constraint.conkey[1])
						                                                                             as ColumnName,
						pg_constraint.conkey[1]                                                      as Ordinal
					FROM
						pg_constraint
							JOIN pg_class ON pg_class.oid = pg_constraint.conrelid
								JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid
					WHERE
						pg_constraint.contype = 'p'")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var sql = @"
					SELECT
						table_catalog || '.' || table_schema || '.' || table_name           as TableID,
						column_name                                                         as Name,
						is_nullable = 'YES'                                                 as IsNullable,
						ordinal_position                                                    as Ordinal,
						data_type                                                           as DataType,
						character_maximum_length                                            as Length,
						COALESCE(numeric_precision, datetime_precision, interval_precision) as Precision,
						numeric_scale                                                       as Scale,
						is_identity = 'YES' OR COALESCE(column_default ~* 'nextval', false) as IsIdentity,
						is_generated <> 'NEVER'                                             as SkipOnInsert,
						is_updatable = 'NO'                                                 as SkipOnUpdate
					FROM
						information_schema.columns";

			if (ExcludedSchemas.Length != 0 || IncludedSchemas.Length != 0)
				sql += @"
					WHERE
						table_schema NOT IN ('pg_catalog','information_schema')";

			return dataConnection.Query<ColumnInfo>(sql).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var data = dataConnection.Query(
				rd => new
				{
					name         = rd[0],
					thisTable    = rd[1],
					otherTable   = rd[2],
					thisColumns  = new[] { rd[ 3], rd[ 4], rd[ 5], rd[ 6], rd[ 7], rd[ 8], rd[ 9], rd[10], rd[11], rd[12], rd[13], rd[14], rd[15], rd[16], rd[17], rd[18] },
					otherColumns = new[] { rd[19], rd[20], rd[21], rd[22], rd[23], rd[24], rd[25], rd[26], rd[27], rd[28], rd[29], rd[30], rd[31], rd[32], rd[33], rd[34] },
				}, @"
				SELECT
					pg_constraint.conname,
					current_database() || '.' || this_schema.nspname  || '.' || this_table.relname,
					current_database() || '.' || other_schema.nspname || '.' || other_table.relname,
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[16]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[16])
				FROM
					pg_constraint
						JOIN pg_class as this_table ON this_table.oid = pg_constraint.conrelid
							JOIN pg_namespace as this_schema ON this_table.relnamespace = this_schema.oid
						JOIN pg_class as other_table ON other_table.oid = pg_constraint.confrelid
							JOIN pg_namespace as other_schema ON other_table.relnamespace = other_schema.oid
				WHERE
					pg_constraint.contype = 'f'")
				.ToList();

			return
			(
				from item in data

				let name         = Convert.ToString(item.name)
				let thisTableID  = Convert.ToString (item.thisTable)
				let otherTableID = Convert.ToString (item.otherTable)

				from col in item.thisColumns
					.Zip(item.otherColumns, (thisColumn,otherColumn) => new { thisColumn, otherColumn })
					.Select((cs,i) => new { cs.thisColumn, cs.otherColumn, ordinal = i})
				where col.thisColumn != null && !(col.thisColumn is DBNull)
				select new ForeingKeyInfo
				{
					Name         = name,
					ThisTableID  = thisTableID,
					OtherTableID = otherTableID,
					ThisColumn   = Convert.ToString(col.thisColumn),
					OtherColumn  = Convert.ToString(col.otherColumn),
					Ordinal      = col.ordinal
				}
			).ToList();
		}

		protected override string GetDbType(string columnType, DataTypeInfo dataType, int length, int prec, int scale)
		{
switch (columnType)
{
	case "name" :
	case "regproc" :
	case "text" :
	case "oid"  : break;
	default:
		break;
}

//			switch (columnType)
//			{
//				case "NUMBER" :
//					if (prec == 0) return columnType;
//					break;
//			}

			return base.GetDbType(columnType, dataType, length, prec, scale);
		}

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType, int length, int precision, int scale)
		{
switch (columnType)
{
	case "name" :
	case "regproc" :
	case "text" :
	case "oid"  : break;
	default:
		break;
}

//			if (columnType == "NUMBER" && precision > 0 && scale == 0)
//			{
//				if (precision <  3) return typeof(sbyte);
//				if (precision <  5) return typeof(short);
//				if (precision < 10) return typeof(int);
//				if (precision < 20) return typeof(long);
//			}
//
//			if (columnType.StartsWith("TIMESTAMP"))
//				return columnType.EndsWith("TIME ZONE") ? typeof(DateTimeOffset) : typeof(DateTime);

			return base.GetSystemType(columnType, dataType, length, precision, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "name"                           :
				case "regproc"                        :
				case "oid"                            : break;
				case "text"                           : return DataType.Text;
//				case "BFILE"                          : return DataType.VarBinary;
//				case "BINARY_DOUBLE"                  : return DataType.Double;
//				case "BINARY_FLOAT"                   : return DataType.Single;
//				case "BLOB"                           : return DataType.Binary;
//				case "CHAR"                           : return DataType.Char;
//				case "CLOB"                           : return DataType.Text;
//				case "DATE"                           : return DataType.DateTime;
//				case "FLOAT"                          : return DataType.Decimal;
//				case "INTERVAL DAY TO SECOND"         : return DataType.Time;
//				case "INTERVAL YEAR TO MONTH"         : return DataType.Int64;
//				case "LONG"                           : return DataType.Text;
//				case "LONG RAW"                       : return DataType.Binary;
//				case "NCHAR"                          : return DataType.NChar;
//				case "NCLOB"                          : return DataType.NText;
//				case "NUMBER"                         : return DataType.Decimal;
//				case "NVARCHAR2"                      : return DataType.NVarChar;
//				case "RAW"                            : return DataType.Binary;
//				case "VARCHAR2"                       : return DataType.VarChar;
//				case "XMLTYPE"                        : return DataType.Xml;
//				case "ROWID"                          : return DataType.VarChar;
				default:
//					if (dataType.StartsWith("TIMESTAMP"))
//						return dataType.EndsWith("TIME ZONE") ? DataType.DateTimeOffset : DataType.DateTime2;
//
					break;
			}

/*
"char"
ARRAY
USER-DEFINED
abstime
anyarray
bigint
bit
boolean
box
bytea
character
character varying
circle
date
double precision
inet
integer
interval
lseg
macaddr
money
numeric
path
pg_node_tree
point
polygon
real
smallint
time with time zone
time without time zone
timestamp with time zone
timestamp without time zone
uuid
xid
xml
*/
			return DataType.Undefined;
		}
	}
}
