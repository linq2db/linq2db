using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Data;
	using SchemaProvider;

	class OracleSchemaProvider : SchemaProviderBase
	{
		public OracleSchemaProvider(string providerName)
		{
			_providerName = providerName;
		}

		readonly string _providerName;

		protected override string GetDataSourceName(DbConnection dbConnection)
		{
			return ((dynamic)dbConnection).HostName;
		}

		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return ((dynamic)dbConnection).DatabaseName;
		}

		string _currentUser;

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			_currentUser = dataConnection.Execute<string>("select user from dual");

			if (IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0)
			{
				// This is very slow
				return dataConnection.Query<TableInfo>(
					@"
					SELECT
						d.OWNER || '.' || d.NAME                         as TableID,
						d.OWNER                                          as SchemaName,
						d.NAME                                           as TableName,
						d.IsView                                         as IsView,
						CASE :CurrentUser WHEN d.OWNER THEN 1 ELSE 0 END as IsDefaultSchema,
						tc.COMMENTS                                      as Description
					FROM
					(
						SELECT t.OWNER, t.TABLE_NAME NAME, 0 as IsView FROM ALL_TABLES t
							UNION ALL
							SELECT v.OWNER, v.VIEW_NAME NAME, 1 as IsView FROM ALL_VIEWS v
					) d
						JOIN ALL_TAB_COMMENTS tc ON
							d.OWNER = tc.OWNER AND
							d.NAME  = tc.TABLE_NAME
					ORDER BY TableID, isView
					",
					new { CurrentUser = _currentUser })
				.ToList();
			}
			else
			{
				// This is significally faster
				return dataConnection.Query<TableInfo>(
					@"
					SELECT
						:CurrentUser || '.' || d.NAME as TableID,
						:CurrentUser                  as SchemaName,
						d.NAME                        as TableName,
						d.IsView                      as IsView,
						1                             as IsDefaultSchema,
						tc.COMMENTS                   as Description
					FROM
					(
						SELECT t.TABLE_NAME NAME, 0 as IsView FROM USER_TABLES t
							UNION ALL
							SELECT v.VIEW_NAME NAME, 1 as IsView FROM USER_VIEWS v
					) d
						JOIN USER_TAB_COMMENTS tc ON
							d.NAME = tc.TABLE_NAME
					ORDER BY TableID, isView
					",
					new { CurrentUser = _currentUser })
				.ToList();
			}
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
				dataConnection.Query<PrimaryKeyInfo>(@"
					SELECT
						FKCOLS.OWNER || '.' || FKCOLS.TABLE_NAME as TableID,
						FKCOLS.CONSTRAINT_NAME                   as PrimaryKeyName,
						FKCOLS.COLUMN_NAME                       as ColumnName,
						FKCOLS.POSITION                          as Ordinal
					FROM
						ALL_CONS_COLUMNS FKCOLS,
						ALL_CONSTRAINTS FKCON
					WHERE
						FKCOLS.OWNER           = FKCON.OWNER and
						FKCOLS.TABLE_NAME      = FKCON.TABLE_NAME and
						FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME AND
						FKCON.CONSTRAINT_TYPE  = 'P'")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			if (IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0)
			{
				// This is very slow
				return dataConnection.Query<ColumnInfo>(@"
					SELECT
						c.OWNER || '.' || c.TABLE_NAME             as TableID,
						c.COLUMN_NAME                              as Name,
						c.DATA_TYPE                                as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END as IsNullable,
						c.COLUMN_ID                                as Ordinal,
						c.DATA_LENGTH                              as Length,
						c.DATA_PRECISION                           as Precision,
						c.DATA_SCALE                               as Scale,
						0                                          as IsIdentity,
						cc.COMMENTS                                as Description
					FROM ALL_TAB_COLUMNS c
						JOIN ALL_COL_COMMENTS cc ON
							c.OWNER       = cc.OWNER      AND
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					ORDER BY TableID, Ordinal
					")
				.ToList();
			}
			else
			{
				// This is significally faster
				return dataConnection.Query<ColumnInfo>(@"
					SELECT 
						(SELECT USER FROM DUAL) || '.' || c.TABLE_NAME as TableID,
						c.COLUMN_NAME                                  as Name,
						c.DATA_TYPE                                    as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END     as IsNullable,
						c.COLUMN_ID                                    as Ordinal,
						c.DATA_LENGTH                                  as Length,
						c.DATA_PRECISION                               as Precision,
						c.DATA_SCALE                                   as Scale,
						0                                              as IsIdentity,
						cc.COMMENTS                                    as Description
					FROM USER_TAB_COLUMNS c
						JOIN USER_COL_COMMENTS cc ON
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					ORDER BY TableID, Ordinal
					")
				.ToList();
			}
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			if (IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0)
			{
				// This is very slow
				return
					dataConnection.Query<ForeingKeyInfo>(@"
						SELECT
							FKCON.CONSTRAINT_NAME                  as Name,
							FKCON.OWNER || '.' || FKCON.TABLE_NAME as ThisTableID,
							FKCOLS.COLUMN_NAME                     as ThisColumn,
							PKCON.OWNER || '.' || PKCON.TABLE_NAME as OtherTableID,
							PKCOLS.COLUMN_NAME                     as OtherColumn,
							FKCOLS.POSITION                        as Ordinal
						FROM
							ALL_CONSTRAINTS FKCON
								JOIN ALL_CONS_COLUMNS FKCOLS ON
									FKCOLS.OWNER           = FKCON.OWNER      AND
									FKCOLS.TABLE_NAME      = FKCON.TABLE_NAME AND
									FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME
							JOIN
							ALL_CONSTRAINTS  PKCON
								JOIN ALL_CONS_COLUMNS PKCOLS ON
									PKCOLS.OWNER           = PKCON.OWNER      AND
									PKCOLS.TABLE_NAME      = PKCON.TABLE_NAME AND
									PKCOLS.CONSTRAINT_NAME = PKCON.CONSTRAINT_NAME
							ON
								PKCON.OWNER           = FKCON.R_OWNER AND
								PKCON.CONSTRAINT_NAME = FKCON.R_CONSTRAINT_NAME
						WHERE 
							FKCON.CONSTRAINT_TYPE = 'R'          AND
							FKCOLS.POSITION       = PKCOLS.POSITION
						")
					.ToList();
			}
			else
			{
				// This is significally faster
				return
					dataConnection.Query<ForeingKeyInfo>(@"
						SELECT
							FKCON.CONSTRAINT_NAME                    as Name,
							FKCON.OWNER || '.' || FKCON.TABLE_NAME   as ThisTableID,
							FKCOLS.COLUMN_NAME                       as ThisColumn,
							PKCOLS.OWNER || '.' || PKCOLS.TABLE_NAME as OtherTableID,
							PKCOLS.COLUMN_NAME                       as OtherColumn,
							FKCOLS.POSITION                          as Ordinal
						FROM
							USER_CONSTRAINTS FKCON
								JOIN USER_CONS_COLUMNS FKCOLS ON
									FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME
								JOIN USER_CONS_COLUMNS PKCOLS ON
									PKCOLS.CONSTRAINT_NAME = FKCON.R_CONSTRAINT_NAME
						WHERE 
							FKCON.CONSTRAINT_TYPE = 'R' AND
							FKCOLS.POSITION       = PKCOLS.POSITION
						ORDER BY Ordinal, Name
						")
						.ToList();
			}
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

			return
			(
				from p in ps.AsEnumerable()
				let schema  = p.Field<string>("OWNER")
				let name    = p.Field<string>("OBJECT_NAME")
				where IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0 || schema == _currentUser
				select new ProcedureInfo
				{
					ProcedureID     = schema + "." + name,
					SchemaName      = schema,
					ProcedureName   = name,
					IsDefaultSchema = schema == _currentUser,
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			var pps = ((DbConnection)dataConnection.Connection).GetSchema("ProcedureParameters");

			return
			(
				from pp in pps.AsEnumerable()
				let schema    = pp.Field<string>("OWNER")
				let name      = pp.Field<string>("OBJECT_NAME")
				let direction = pp.Field<string>("IN_OUT")
				where IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0 || schema == _currentUser
				select new ProcedureParameterInfo
				{
					ProcedureID   = schema + "." + name,
					ParameterName = pp.Field<string>("ARGUMENT_NAME"),
					DataType      = pp.Field<string>("DATA_TYPE"),
					Ordinal       = Converter.ChangeTypeTo<int>  (pp["POSITION"]),
					Length        = Converter.ChangeTypeTo<long?>(pp["DATA_LENGTH"]),
					Precision     = Converter.ChangeTypeTo<int?> (pp["DATA_PRECISION"]),
					Scale         = Converter.ChangeTypeTo<int?> (pp["DATA_SCALE"]),
					IsIn          = direction.StartsWith("IN"),
					IsOut         = direction.EndsWith("OUT"),
				}
			).ToList();
		}

		protected override string GetDbType(string columnType, DataTypeInfo dataType, long? length, int? prec, int? scale)
		{
			switch (columnType)
			{
				case "NUMBER" :
					if (prec == 0) return columnType;
					break;
			}

			return base.GetDbType(columnType, dataType, length, prec, scale);
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (dataType == "NUMBER" && precision > 0 && (scale ?? 0) == 0)
			{
				if (precision <  3) return typeof(sbyte);
				if (precision <  5) return typeof(short);
				if (precision < 10) return typeof(int);
				if (precision < 20) return typeof(long);
			}

			if (dataType.StartsWith("TIMESTAMP"))
				return dataType.EndsWith("TIME ZONE") ? typeof(DateTimeOffset) : typeof(DateTime);

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType)
			{
				case "OBJECT"                 : return DataType.Variant;
				case "BFILE"                  : return DataType.VarBinary;
				case "BINARY_DOUBLE"          : return DataType.Double;
				case "BINARY_FLOAT"           : return DataType.Single;
				case "BLOB"                   : return DataType.Blob;
				case "CHAR"                   : return DataType.Char;
				case "CLOB"                   : return DataType.Text;
				case "DATE"                   : return DataType.DateTime;
				case "FLOAT"                  : return DataType.Decimal;
				case "INTERVAL DAY TO SECOND" : return DataType.Time;
				case "INTERVAL YEAR TO MONTH" : return DataType.Int64;
				case "LONG"                   : return DataType.Text;
				case "LONG RAW"               : return DataType.Binary;
				case "NCHAR"                  : return DataType.NChar;
				case "NCLOB"                  : return DataType.NText;
				case "NUMBER"                 : return DataType.Decimal;
				case "NVARCHAR2"              : return DataType.NVarChar;
				case "RAW"                    : return DataType.Binary;
				case "VARCHAR2"               : return DataType.VarChar;
				case "XMLTYPE"                : return DataType.Xml;
				case "ROWID"                  : return DataType.VarChar;
				default:
					if (dataType.StartsWith("TIMESTAMP"))
						return dataType.EndsWith("TIME ZONE") ? DataType.DateTimeOffset : DataType.DateTime2;
					break;
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return _providerName == ProviderName.OracleManaged ? "Oracle.ManagedDataAccess.Types" : "Oracle.DataAccess.Types";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType)
			{
				case "BFILE"                          : return "OracleBFile";
				case "RAW"                            :
				case "LONG RAW"                       : return "OracleBinary";
				case "BLOB"                           : return "OracleBlob";
				case "CLOB"                           : return "OracleClob";
				case "DATE"                           : return "OracleDate";
				case "BINARY_DOUBLE"                  :
				case "BINARY_FLOAT"                   :
				case "NUMBER"                         : return "OracleDecimal";
				case "INTERVAL DAY TO SECOND"         : return "OracleIntervalDS";
				case "INTERVAL YEAR TO MONTH"         : return "OracleIntervalYM";
				case "NCHAR"                          :
				case "LONG"                           :
				case "ROWID"                          :
				case "CHAR"                           : return "OracleString";
				case "TIMESTAMP"                      : return "OracleTimeStamp";
				case "TIMESTAMP WITH LOCAL TIME ZONE" : return "OracleTimeStampLTZ";
				case "TIMESTAMP WITH TIME ZONE"       : return "OracleTimeStampTZ";
				case "XMLTYPE"                        : return "OracleXmlType";
			}

			return base.GetProviderSpecificType(dataType);
		}
	}
}
