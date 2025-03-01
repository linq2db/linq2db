using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;
using LinqToDB.Internal.Conversion;

namespace LinqToDB.Internal.DataProvider.MySql
{
	sealed class MySqlSchemaProvider : SchemaProviderBase
	{
		private readonly MySqlDataProvider _provider;

		public MySqlSchemaProvider(MySqlDataProvider provider)
		{
			_provider = provider;
		}

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

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/tables-table.html
			// all selected columns are not nullable
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var catalog = rd.GetString(0);
					var name    = rd.GetString(1);
					// BASE TABLE/VIEW/SYSTEM VIEW
					var type    = rd.GetString(2);
					return new TableInfo()
					{
						// The latest MySql returns FK information with lowered schema names.
						//
						TableID            = catalog.ToLowerInvariant() + ".." + name,
						CatalogName        = catalog,
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

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/columns-table.html
			// nullable columns:
			// CHARACTER_MAXIMUM_LENGTH
			// NUMERIC_PRECISION
			// NUMERIC_SCALE
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var dataType   = rd.GetString(0);
					var columnType = rd.GetString(1);
					var tableId    = rd.GetString(2).ToLowerInvariant() + ".." + rd.GetString(3);
					var name       = rd.GetString(4);
					var isNullable = rd.GetString(5) == "YES";
					var ordinal    = Converter.ChangeTypeTo<int>(rd[6]);
					var length     = Converter.ChangeTypeTo<long?>(rd[7]);
					var precision  = Converter.ChangeTypeTo<long?>(rd[8]);
					var scale      = Converter.ChangeTypeTo<long?>(rd[9]);
					var extra      = rd.GetString(10);

					return new ColumnInfo()
					{
						TableID      = tableId,
						Name         = name,
						IsNullable   = isNullable,
						Ordinal      = ordinal,
						DataType     = dataType,
						// length could be > int.MaxLength for LONGBLOB/LONGTEXT types, but they always have fixed length and it cannot be used in type name
						Length       = length > int.MaxValue ? null : (int?)length,
						Precision    = (int?)precision,
						Scale        = (int?)scale,
						ColumnType   = columnType,
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// https://dev.mysql.com/doc/refman/8.0/en/key-column-usage-table.html
			// https://dev.mysql.com/doc/refman/8.0/en/table-constraints-table.html
			// nullable columns:
			// REFERECED_* columns could be null, but not for FK
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					return new ForeignKeyInfo()
					{
						ThisTableID  = rd.GetString(0).ToLowerInvariant() + ".." + rd.GetString(1),
						Name         = rd.GetString(2),
						ThisColumn   = rd.GetString(3),
						OtherTableID = rd.GetString(4).ToLowerInvariant() + ".." + rd.GetString(5),
						OtherColumn  = rd.GetString(6),
						Ordinal      = Converter.ChangeTypeTo<int>(rd[7]),
					};
				}, @"
SELECT
		c.TABLE_SCHEMA,
		c.TABLE_NAME,
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

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToLowerInvariant() switch
			{
				"tinyint unsigned"  => DataType.Byte,
				"smallint unsigned" => DataType.UInt16,
				"mediumint unsigned"=> DataType.UInt32,
				"int unsigned"      => DataType.UInt32,
				"bigint unsigned"   => DataType.UInt64,
				"bool"              => DataType.SByte, // tinyint(1) alias
				"bit"               => DataType.BitArray,
				"blob"              => DataType.Blob,
				"tinyblob"          => DataType.Blob,
				"mediumblob"        => DataType.Blob,
				"longblob"          => DataType.Blob,
				"binary"            => DataType.Binary,
				"varbinary"         => DataType.VarBinary,
				"date"              => DataType.Date,
				"datetime"          => DataType.DateTime,
				"timestamp"         => DataType.DateTime,
				"time"              => DataType.Time,
				"char"              => DataType.Char,
				"varchar"           => DataType.VarChar,
				"set"               => DataType.VarChar,
				"enum"              => DataType.VarChar,
				"tinytext"          => DataType.Text,
				"text"              => DataType.Text,
				"mediumtext"        => DataType.Text,
				"longtext"          => DataType.Text,
				"double"            => DataType.Double,
				"float"             => DataType.Single,
				"tinyint"           => columnType != null && columnType.Contains("unsigned") ? DataType.Byte   : DataType.SByte,
				"smallint"          => columnType != null && columnType.Contains("unsigned") ? DataType.UInt16 : DataType.Int16,
				"int"               => columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32,
				"year"              => DataType.Int32,
				"mediumint"         => columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32,
				"bigint"            => columnType != null && columnType.Contains("unsigned") ? DataType.UInt64 : DataType.Int64,
				"decimal"           => DataType.Decimal,
				"json"              => DataType.Json,
				_                   => DataType.Undefined,
			};
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			// GetSchema("PROCEDURES") not used, as for MySql 5.7 (but not mariadb/mysql 5.6) it returns procedures from
			// sys database too
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var catalog = Converter.ChangeTypeTo<string>(rd[0]);
					var name    = Converter.ChangeTypeTo<string>(rd[1]);

					return new ProcedureInfo()
					{
						ProcedureID         = catalog + "." + name,
						CatalogName         = catalog,
						ProcedureName       = name,
						IsFunction          = Converter.ChangeTypeTo<string>(rd[2]) == "FUNCTION",
						IsDefaultSchema     = true,
						ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[3]),
						Description         = Converter.ChangeTypeTo<string>(rd[4]),
					};
				}, "SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION, ROUTINE_COMMENT FROM INFORMATION_SCHEMA.routines WHERE ROUTINE_TYPE IN ('PROCEDURE', 'FUNCTION') AND ROUTINE_SCHEMA = database()")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			// don't use GetSchema("PROCEDURE PARAMETERS") as MySql provider's implementation does strange stuff
			// instead of just quering of INFORMATION_SCHEMA view. It returns incorrect results and breaks provider
			return dataConnection
				.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var procId    = rd.GetString(0) + "." + rd.GetString(1);
					var mode      = Converter.ChangeTypeTo<string>(rd[2]);
					var ordinal   = Converter.ChangeTypeTo<int>(rd[3]);
					var name      = Converter.ChangeTypeTo<string>(rd[4]);
					var precision = Converter.ChangeTypeTo<int?>(rd[5]);
					var scale     = Converter.ChangeTypeTo<long?>(rd[6]);
					var type      = rd.GetString(7).ToUpperInvariant();
					var length    = Converter.ChangeTypeTo<long?>(rd[8]);

					return new ProcedureParameterInfo()
					{
						ProcedureID   = procId,
						ParameterName = name,
						IsIn          = mode == "IN"  || mode == "INOUT",
						IsOut         = mode == "OUT" || mode == "INOUT",
						Precision     = precision,
						Scale         = (int?)scale,
						Ordinal       = ordinal,
						IsResult      = mode == null,
						DataType      = type,
						Length        = length > int.MaxValue ? null : (int?)length,
						DataTypeExact = Converter.ChangeTypeTo<string>(rd[9]),
						IsNullable    = true
					};
				}, "SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, PARAMETER_MODE, ORDINAL_POSITION, PARAMETER_NAME, NUMERIC_PRECISION, NUMERIC_SCALE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, DTD_IDENTIFIER FROM INFORMATION_SCHEMA.parameters WHERE SPECIFIC_SCHEMA = database()")
				.ToList();
		}

		protected override DataParameter BuildProcedureParameter(ParameterSchema p)
		{
			var param = base.BuildProcedureParameter(p);

			// mysql procedure parameters are nullable so better to pass NULL, as at least JSON parameters
			// doesn't work with empty string (and we cannot detect json-typed parameters for MariaDB)
			param.Value = null;

			return param;
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			var rv = base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);

			// for no good reason if procedure doesn't return table data but have output parameters, MySql provider
			// returns fake schema with output parameters as columns
			// we can detect it by column name prefix
			// https://github.com/mysql/mysql-connector-net/blob/5864e6b21a8b32f5154b53d1610278abb3cb1cee/Source/MySql.Data/StoredProcedure.cs#L42
			// UPDATE:
			// now we have similar issue with MySqlConnector
			// https://github.com/mysql-net/MySqlConnector/issues/722
			if (rv != null && rv.AsEnumerable()
					.Any(r => r.Field<string>("ColumnName")!.StartsWith("@_cnet_param_")
						||    r.Field<string>("ColumnName") == "\ue001\b\v"))
				rv = null;

			return rv;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dt           = GetDataTypeByProviderDbType(providerType, options)
				let dataType     = dt == null ? null : dt.TypeName
				let columnName   = r.Field<string>("ColumnName")
				let isNullable   = r.Field<bool>("AllowDBNull")
				let length       = r.Field<int>("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int>(r["NumericScale"])

				let systemType = GetSystemType(dataType, null, dt, length, precision, scale, options)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, dataType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName.Trim('`')),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(dataType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(dataType),
					IsIdentity           = r.IsNull("IsIdentity") ? false : r.Field<bool>("IsIdentity")
				}
			).ToList();
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType?.ToLowerInvariant())
			{
				case "geometry"  : return _provider.Adapter.MySqlGeometryType.Name;
				case "decimal"   : return _provider.Adapter.MySqlDecimalType?.Name;
				case "date"      :
				case "newdate"   :
				case "datetime"  :
				case "timestamp" : return _provider.Adapter.MySqlDateTimeType.Name;
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			switch (dataType?.ToLowerInvariant())
			{
				case "bit"               :
					{
						// C - "Consistency"
						var size = precision > 0 ? precision : length;
						if (size ==  1) return typeof(bool);
						if (size <=  8) return typeof(byte);
						if (size <= 16) return typeof(ushort);
						if (size <= 32) return typeof(uint);
						return typeof(ulong);
					}
				case "tinyint unsigned"  : return typeof(byte);
				case "smallint unsigned" : return typeof(ushort);
				case "mediumint unsigned": return typeof(uint);
				case "int unsigned"      : return typeof(uint);
				case "bigint unsigned"   : return typeof(ulong);
				case "tinyint"           :
				{
					var size = precision > 0 ? precision : length;
					if (columnType == "tinyint(1)" || size == 1)
						return typeof(bool);
					return columnType?.Contains("unsigned") == true ? typeof(byte) : typeof(sbyte);
				}
				//case "tinyint"           : return columnType?.Contains("unsigned") == true ? typeof(byte)   : typeof(sbyte);
				case "smallint"          : return columnType?.Contains("unsigned") == true ? typeof(ushort) : typeof(short);
				case "mediumint"         :
				case "int"               : return columnType?.Contains("unsigned") == true ? typeof(uint)   : typeof(int);
				case "bigint"            : return columnType?.Contains("unsigned") == true ? typeof(ulong)  : typeof(long);
				case "json"              :
				case "longtext"          : return typeof(string);
				case "timestamp"         : return typeof(DateTime);
				//case "bool"              : return typeof(sbyte);
				case "bool"              : return typeof(bool);
				case "point"             :
				case "linestring"        :
				case "polygon"           :
				case "multipoint"        :
				case "multipolygon"      :
				case "multilinestring"   :
				case "geomcollection"    :
				case "geometrycollection":
				case "geometry"          : return typeof(byte[]);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
		}

		protected override StringComparison ForeignKeyColumnComparison(string column)
		{
			// The latest MySql returns FK information with lowered schema names.
			//
			return column.All(char.IsLower) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		}
	}
}
