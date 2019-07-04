using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using SchemaProvider;

	class MySqlSchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return base.GetDataTypes(dataConnection)
				.Select(dt =>
				{
					if (dt.CreateFormat != null && dt.CreateFormat.EndsWith(" UNSIGNED", StringComparison.OrdinalIgnoreCase))
					{
						return new DataTypeInfo
						{
							TypeName         = dt.CreateFormat,
							DataType         = dt.DataType,
							CreateFormat     = dt.CreateFormat,
							CreateParameters = dt.CreateParameters,
							ProviderDbType   = dt.ProviderDbType,
						};
					}

					return dt;
				})
				.ToList();
		}

		// mysql provider will execute procedure
		protected override bool GetProcedureSchemaExecutesProcedure => true;

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/tables-table.html
			// all selected columns are not nullable
			return dataConnection
				.Query(rd =>
				{
					var catalog = rd.GetString(0);
					var name    = rd.GetString(1);
					// BASE TABLE/VIEW/SYSTEM VIEW
					var type    = rd.GetString(2);
					return new TableInfo()
					{
						// The latest MySql returns FK information with lowered schema names.
						//
						TableID            = catalog.ToLower() + ".." + name,
						CatalogName        = catalog,
						SchemaName         = string.Empty,
						TableName          = name,
						IsDefaultSchema    = true,
						IsView             = type == "VIEW" || type == "SYSTEM VIEW",
						IsProviderSpecific = type == "SYSTEM VIEW" || catalog.Equals("sys", StringComparison.OrdinalIgnoreCase),
						Description        = rd.GetString(3)
					};
				}, @"
SELECT
		TABLE_SCHEMA,
		TABLE_NAME,
		TABLE_TYPE,
		TABLE_COMMENT
	FROM INFORMATION_SCHEMA.TABLES
	WHERE TABLE_SCHEMA = DATABASE()")
				.ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return dataConnection.Query<PrimaryKeyInfo>(@"
			SELECT
					CONCAT(lower(k.CONSTRAINT_SCHEMA),'..',k.TABLE_NAME) as TableID,
					k.CONSTRAINT_NAME                                    as PrimaryKeyName,
					k.COLUMN_NAME                                        as ColumnName,
					k.ORDINAL_POSITION                                   as Ordinal
				FROM
					INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
					JOIN
						INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
					ON
						k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
						k.CONSTRAINT_SCHEMA  = c.CONSTRAINT_SCHEMA AND
						k.CONSTRAINT_NAME    = c.CONSTRAINT_NAME AND
						k.TABLE_NAME         = c.TABLE_NAME
				WHERE
					c.CONSTRAINT_TYPE   ='PRIMARY KEY' AND
					c.CONSTRAINT_SCHEMA = database()")
			.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/columns-table.html
			// nullable columns:
			// CHARACTER_MAXIMUM_LENGTH
			// NUMERIC_PRECISION
			// NUMERIC_SCALE
			return dataConnection
				.Query(rd =>
				{
					var extra      = rd.GetString(10);
					return new ColumnInfo()
					{
						TableID      = rd.GetString(2).ToLower() + ".." + rd.GetString(3),
						Name         = rd.GetString(4),
						IsNullable   = rd.GetString(5) == "YES",
						Ordinal      = Converter.ChangeTypeTo<int>(rd[6]),
						DataType     = rd.GetString(0),
						Length       = Converter.ChangeTypeTo<long?>(rd[7]),
						Precision    = Converter.ChangeTypeTo<int?>(rd[8]),
						Scale        = Converter.ChangeTypeTo<int?>(rd[9]),
						ColumnType   = rd.GetString(1),
						IsIdentity   = extra.Contains("auto_increment"),
						Description  = rd.GetString(11),
						// also starting from 5.1 we can utilise provileges column for skip properties
						// but it sounds like a bad idea
						SkipOnInsert = extra.Contains("VIRTUAL STORED") || extra.Contains("VIRTUAL GENERATED"),
						SkipOnUpdate = extra.Contains("VIRTUAL STORED") || extra.Contains("VIRTUAL GENERATED")
					};
				}, @"
SELECT
		DATA_TYPE,
		COLUMN_TYPE,
		TABLE_SCHEMA,
		TABLE_NAME,
		COLUMN_NAME,
		IS_NULLABLE,
		ORDINAL_POSITION,
		CHARACTER_MAXIMUM_LENGTH,
		NUMERIC_PRECISION,
		NUMERIC_SCALE,
		EXTRA,
		COLUMN_COMMENT
	FROM INFORMATION_SCHEMA.COLUMNS
	WHERE TABLE_SCHEMA = DATABASE()")
				.ToList();
		}

		protected override List<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/key-column-usage-table.html
			// https://dev.mysql.com/doc/refman/8.0/en/table-constraints-table.html
			// nullable columns:
			// REFERECED_* columns could be null, but not for FK
			return dataConnection
				.Query(rd =>
				{
					return new ForeignKeyInfo()
					{
						Name         = rd.GetString(2),
						ThisTableID  = rd.GetString(1).ToLower() + ".." + rd.GetString(0),
						ThisColumn   = rd.GetString(3),
						OtherTableID = rd.GetString(4).ToLower() + ".." + rd.GetString(5),
						OtherColumn  = rd.GetString(6),
						Ordinal      = Converter.ChangeTypeTo<int>(rd[7]),
					};
				}, @"
SELECT
		c.TABLE_NAME,
		c.TABLE_SCHEMA,
		c.CONSTRAINT_NAME,
		c.COLUMN_NAME,
		c.REFERENCED_TABLE_SCHEMA,
		c.REFERENCED_TABLE_NAME,
		c.REFERENCED_COLUMN_NAME,
		c.ORDINAL_POSITION
	FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE c
		INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
			ON c.CONSTRAINT_SCHEMA    = tc.CONSTRAINT_SCHEMA
				AND c.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
				AND c.TABLE_SCHEMA    = tc.TABLE_SCHEMA
				AND c.TABLE_NAME      = tc.TABLE_NAME
	WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
		AND c.TABLE_SCHEMA   = DATABASE()")
				.ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "bit"        : return DataType.UInt64;
				case "blob"       : return DataType.Blob;
				case "tinyblob"   : return DataType.Binary;
				case "mediumblob" : return DataType.Binary;
				case "longblob"   : return DataType.Binary;
				case "binary"     : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "date"       : return DataType.Date;
				case "datetime"   : return DataType.DateTime;
				case "timestamp"  : return DataType.Timestamp;
				case "time"       : return DataType.Time;
				case "char"       : return DataType.Char;
				case "nchar"      : return DataType.NChar;
				case "varchar"    : return DataType.VarChar;
				case "nvarchar"   : return DataType.NVarChar;
				case "set"        : return DataType.NVarChar;
				case "enum"       : return DataType.NVarChar;
				case "tinytext"   : return DataType.Text;
				case "text"       : return DataType.Text;
				case "mediumtext" : return DataType.Text;
				case "longtext"   : return DataType.Text;
				case "double"     : return DataType.Double;
				case "float"      : return DataType.Single;
				case "tinyint"    : return columnType == "tinyint(1)" ? DataType.Boolean : DataType.SByte;
				case "smallint"   : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt16 : DataType.Int16;
				case "int"        : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "year"       : return DataType.Int32;
				case "mediumint"  : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "bigint"     : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt64 : DataType.Int64;
				case "decimal"    : return DataType.Decimal;
				case "tiny int"   : return DataType.Byte;
			}

			return DataType.Undefined;
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			// GetSchema("PROCEDURES") not used, as for MySql 5.7 (but not mariadb/mysql 5.6) it returns procedures from
			// sys database too
			return dataConnection
				.Query(rd =>
				{
					var catalog = Converter.ChangeTypeTo<string>(rd[0]);
					var name    = Converter.ChangeTypeTo<string>(rd[1]);
					return new ProcedureInfo()
					{
						ProcedureID         = catalog + "." + name,
						CatalogName         = catalog,
						SchemaName          = null,
						ProcedureName       = name,
						IsFunction          = Converter.ChangeTypeTo<string>(rd[2]) == "FUNCTION",
						IsTableFunction     = false,
						IsAggregateFunction = false,
						IsDefaultSchema     = true,
						ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[3])
				};
				}, "SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.routines WHERE ROUTINE_SCHEMA = database()")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			// don't use GetSchema("PROCEDURE PARAMETERS") as MySql provider's implementation does strange stuff
			// instead of just quering of INFORMATION_SCHEMA view. It returns incorrect results and breaks provider
			return dataConnection
				.Query(rd =>
				{
					var mode = Converter.ChangeTypeTo<string>(rd[2]);
					return new ProcedureParameterInfo()
					{
						ProcedureID   = rd.GetString(0) + "." + rd.GetString(1),
						ParameterName = Converter.ChangeTypeTo<string>(rd[4]),
						IsIn          = mode == "IN"  || mode == "INOUT",
						IsOut         = mode == "OUT" || mode == "INOUT",
						Precision     = Converter.ChangeTypeTo<int?>(rd["NUMERIC_PRECISION"]),
						Scale         = Converter.ChangeTypeTo<int?>(rd["NUMERIC_SCALE"]),
						Ordinal       = Converter.ChangeTypeTo<int>(rd["ORDINAL_POSITION"]),
						IsResult      = mode == null,
						DataType      = rd.GetString(7).ToUpper(),
						IsNullable    = true
					};
				}, "SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, PARAMETER_MODE, ORDINAL_POSITION, PARAMETER_NAME, NUMERIC_PRECISION, NUMERIC_SCALE, DATA_TYPE FROM INFORMATION_SCHEMA.parameters WHERE SPECIFIC_SCHEMA = database()")
				.ToList();
		}

		protected override DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			var rv = base.GetProcedureSchema(dataConnection, commandText, commandType, parameters);

			// for no good reason if procedure doesn't return table data but have output parameters, MySql provider
			// returns fake schema with output parameters as columns
			// we can detect it by column name prefix
			// https://github.com/mysql/mysql-connector-net/blob/5864e6b21a8b32f5154b53d1610278abb3cb1cee/Source/MySql.Data/StoredProcedure.cs#L42
			if (rv != null && rv.AsEnumerable().Any(r => r.Field<string>("ColumnName").StartsWith("@_cnet_param_")))
				rv = null;

			return rv;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
		{
#if !NETSTANDARD
			return
			(
				from r in resultTable.AsEnumerable()

				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType = DataTypes.FirstOrDefault(t => t.ProviderDbType == providerType)
				let columnType = dataType == null ? null : dataType.TypeName

				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>("AllowDBNull")

				let length = r.Field<int>("ColumnSize")
				let precision = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale = Converter.ChangeTypeTo<int>(r["NumericScale"])

				let systemType = GetSystemType(columnType, null, dataType, length, precision, scale)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(columnType, dataType, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.IsNull("IsIdentity") ? false : r.Field<bool>("IsIdentity")
				}
			).ToList();
#else
			return new List<ColumnSchema>();
#endif
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return "MySql.Data.Types";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType.ToLower())
			{
				case "geometry"  : return "MySqlGeometry";
				case "decimal"   : return "MySqlDecimal";
				case "date"      :
				case "newdate"   :
				case "datetime"  :
				case "timestamp" : return "MySqlDateTime";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (columnType != null && columnType.Contains("unsigned"))
			{
				switch (dataType.ToLower())
				{
					case "smallint"   : return typeof(UInt16);
					case "int"        : return typeof(UInt32);
					case "mediumint"  : return typeof(UInt32);
					case "bigint"     : return typeof(UInt64);
					case "tiny int"   : return typeof(Byte);
				}
			}

			switch (dataType)
			{
				case "tinyint"   :
					if (columnType == "tinyint(1)")
						return typeof(Boolean);
					break;
				case "datetime2" : return typeof(DateTime);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}

		protected override StringComparison ForeignKeyColumnComparison(string column)
		{
			// The latest MySql returns FK information with lowered schema names.
			//
			return column.All(char.IsLower) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		}
	}
}
