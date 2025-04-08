using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	// Missing features:
	// - function with ref_cursor return type returns object, need to find out how to map it
	sealed class OracleSchemaProvider : SchemaProviderBase
	{
		private readonly OracleDataProvider _provider;
		private int _majorVersion;

		// both managed and native providers will execute procedure
		protected override bool GetProcedureSchemaExecutesProcedure => true;

		private string? SchemasFilter { get; set; }

		public OracleSchemaProvider(OracleDataProvider provider)
		{
			_provider = provider;
		}

		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
		{
			var defaultSchema = dataConnection.Execute<string>("SELECT USER FROM DUAL");
			SchemasFilter     = BuildSchemaFilter(options, defaultSchema, OracleMappingSchema.ConvertStringToSql);
			_majorVersion     = GetMajorVersion(dataConnection);

			return base.GetSchema(dataConnection, options);
		}

		protected override string GetDataSourceName(DataConnection dbConnection)
		{
			var connection = _provider.TryGetProviderConnection(dbConnection, dbConnection.OpenDbConnection());
			if (connection == null)
				return string.Empty;

			return _provider.Adapter.GetHostName?.Invoke(connection) ?? connection.DataSource;
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			var connection = _provider.TryGetProviderConnection(dbConnection, dbConnection.OpenDbConnection());
			if (connection == null)
				return string.Empty;

			return _provider.Adapter.GetDatabaseName?.Invoke(connection) ?? connection.Database;
		}

		private string? _currentUser;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<TableInfo>();

			LoadCurrentUser(dataConnection);

			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				// This is very slow
				return dataConnection.Query<TableInfo>(
					@"
					SELECT
						d.OWNER || '.' || d.NAME                                     as TableID,
						d.OWNER                                                      as SchemaName,
						d.NAME                                                       as TableName,
						d.IsView                                                     as IsView,
						CASE :CurrentUser WHEN d.OWNER THEN 1 ELSE 0 END             as IsDefaultSchema,
						CASE d.MatView WHEN 1 THEN mvc.COMMENTS ELSE tc.COMMENTS END as Description
					FROM
					(
						SELECT t.OWNER, t.TABLE_NAME NAME, 0 as IsView, 0 as MatView FROM ALL_TABLES t
							LEFT JOIN ALL_MVIEWS tm ON t.OWNER = tm.OWNER AND t.TABLE_NAME = tm.CONTAINER_NAME
							WHERE tm.MVIEW_NAME IS NULL AND t.OWNER " + SchemasFilter + @"
						UNION ALL
						SELECT v.OWNER, v.VIEW_NAME NAME, 1 as IsView, 0 as MatView FROM ALL_VIEWS v
							WHERE v.OWNER " + SchemasFilter + @"
						UNION ALL
						SELECT m.OWNER, m.MVIEW_NAME NAME, 1 as IsView, 1 as MatView FROM ALL_MVIEWS m
							WHERE m.OWNER " + SchemasFilter + @"
					) d
						LEFT JOIN ALL_TAB_COMMENTS tc ON
							d.OWNER = tc.OWNER AND
							d.NAME  = tc.TABLE_NAME
						LEFT JOIN ALL_MVIEW_COMMENTS mvc ON
							d.OWNER = mvc.OWNER AND
							d.NAME  = mvc.MVIEW_NAME
					ORDER BY TableID, isView
					",
					new DataParameter("CurrentUser", _currentUser, DataType.VarChar))
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
						d.COMMENTS                    as Description
					FROM
					(
						SELECT NAME, ISVIEW, CASE c.MatView WHEN 1 THEN mvc.COMMENTS ELSE tc.COMMENTS END AS COMMENTS
						FROM
						(
							SELECT t.TABLE_NAME NAME, 0 as IsView, 0 as MatView FROM USER_TABLES t
								LEFT JOIN USER_MVIEWS tm ON t.TABLE_NAME = tm.CONTAINER_NAME
								WHERE tm.MVIEW_NAME IS NULL
							UNION ALL
							SELECT v.VIEW_NAME NAME, 1 as IsView, 0 as MatView FROM USER_VIEWS v
							UNION ALL
							SELECT m.MVIEW_NAME NAME, 1 as IsView, 1 as MatView FROM USER_MVIEWS m
						) c
							LEFT JOIN USER_TAB_COMMENTS tc ON c.NAME = tc.TABLE_NAME
							LEFT JOIN USER_MVIEW_COMMENTS mvc ON c.NAME = mvc.MVIEW_NAME
					) d
					ORDER BY TableID, isView
					",
					new DataParameter("CurrentUser", _currentUser, DataType.VarChar))
				.ToList();
			}
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<PrimaryKeyInfo>();

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
						FKCOLS.OWNER           = FKCON.OWNER AND
						FKCOLS.TABLE_NAME      = FKCON.TABLE_NAME AND
						FKCOLS.CONSTRAINT_NAME = FKCON.CONSTRAINT_NAME AND
						FKCON.CONSTRAINT_TYPE  = 'P' AND
						FKCOLS.OWNER " + SchemasFilter)
				.ToList();
		}

		private static int GetMajorVersion(DataConnection dataConnection)
		{
			var version = dataConnection.Query<string?>("SELECT  VERSION from PRODUCT_COMPONENT_VERSION WHERE ROWNUM = 1").FirstOrDefault();
			if (version != null)
			{
				try
				{
					return int.Parse(version.Split('.')[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
				}
				catch { }
			}

			return 11;
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<ColumnInfo>();

			var isIdentitySql = "0                                              as IsIdentity,";
			if (_majorVersion >= 12)
			{
				isIdentitySql = "CASE c.IDENTITY_COLUMN WHEN 'YES' THEN 1 ELSE 0 END as IsIdentity,";
			}

			string sql;

			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				// This is very slow
				sql = @"
					SELECT
						c.OWNER || '.' || c.TABLE_NAME             as TableID,
						c.COLUMN_NAME                              as Name,
						c.DATA_TYPE                                as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END as IsNullable,
						c.COLUMN_ID                                as Ordinal,
						c.DATA_LENGTH                              as Length,
						c.CHAR_LENGTH                              as CharLength,
						c.DATA_PRECISION                           as Precision,
						c.DATA_SCALE                               as Scale,
						" + isIdentitySql + @"
						cc.COMMENTS                                as Description
					FROM ALL_TAB_COLUMNS c
						JOIN ALL_COL_COMMENTS cc ON
							c.OWNER       = cc.OWNER      AND
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					WHERE c.OWNER " + SchemasFilter;
			}
			else
			{
				// This is significally faster
				sql = @"
					SELECT
						(SELECT USER FROM DUAL) || '.' || c.TABLE_NAME as TableID,
						c.COLUMN_NAME                                  as Name,
						c.DATA_TYPE                                    as DataType,
						CASE c.NULLABLE WHEN 'Y' THEN 1 ELSE 0 END     as IsNullable,
						c.COLUMN_ID                                    as Ordinal,
						c.DATA_LENGTH                                  as Length,
						c.CHAR_LENGTH                                  as CharLength,
						c.DATA_PRECISION                               as Precision,
						c.DATA_SCALE                                   as Scale,
						" + isIdentitySql + @"
						cc.COMMENTS                                    as Description
					FROM USER_TAB_COLUMNS c
						JOIN USER_COL_COMMENTS cc ON
							c.TABLE_NAME  = cc.TABLE_NAME AND
							c.COLUMN_NAME = cc.COLUMN_NAME
					";
			}

			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var tableId    = rd.GetString(0);
				var name       = rd.GetString(1);
				var dataType   = rd.IsDBNull(2) ?       null : rd.GetString(2);
				var isNullable = rd.GetInt32(3) != 0;
				var ordinal    = rd.IsDBNull(4) ? 0 : rd.GetInt32(4);
				var dataLength = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
				var charLength = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6);

				return new ColumnInfo
				{
					TableID     = tableId,
					Name        = name,
					DataType    = dataType,
					IsNullable  = isNullable,
					Ordinal     = ordinal,
					Precision   = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7),
					Scale       = rd.IsDBNull(8) ? (int?)null : rd.GetInt32(8),
					IsIdentity  = rd.GetInt32(9) != 0,
					Description = rd.IsDBNull(10) ? null : rd.GetString(10),
					Length      = dataType == "CHAR" || dataType == "NCHAR" || dataType == "NVARCHAR2" || dataType == "VARCHAR2" || dataType == "VARCHAR"
									? charLength : dataLength
				};
			},
				sql).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new List<ForeignKeyInfo>();

			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				// This is very slow
				return
					dataConnection.Query<ForeignKeyInfo>(@"
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
							FKCOLS.POSITION       = PKCOLS.POSITION AND
							FKCON.OWNER " + SchemasFilter + @" AND
							PKCON.OWNER " + SchemasFilter)
					.ToList();
			}
			else
			{
				// This is significally faster
				return
					dataConnection.Query<ForeignKeyInfo>(@"
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

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return null;

			string sql;
			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				// This could be very slow
				sql = @"SELECT
	p.OWNER                                                                                                                              AS Owner,
	CASE WHEN p.OWNER = USER THEN 1 ELSE 0 END                                                                                           AS IsDefault,
	p.OVERLOAD                                                                                                                           AS Overload,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END                                                                 AS PackageName,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END                                                     AS ProcedureName,
	CASE WHEN a.DATA_TYPE IS NULL THEN 'PROCEDURE' WHEN a.DATA_TYPE = 'TABLE' THEN 'TABLE_FUNCTION' ELSE 'FUNCTION' END AS ProcedureType
FROM ALL_PROCEDURES p
	LEFT OUTER JOIN ALL_ARGUMENTS a ON
		a.OWNER = p.OWNER
			AND ((a.PACKAGE_NAME = p.OBJECT_NAME AND a.OBJECT_NAME = p.PROCEDURE_NAME)
				OR (a.PACKAGE_NAME IS NULL AND p.PROCEDURE_NAME IS NULL AND a.OBJECT_NAME = p.OBJECT_NAME))
			AND a.ARGUMENT_NAME IS NULL
			AND a.DATA_LEVEL = 0
WHERE ((p.OBJECT_TYPE IN ('PROCEDURE', 'FUNCTION') AND PROCEDURE_NAME IS NULL) OR PROCEDURE_NAME IS NOT NULL)
	AND p.OWNER " + SchemasFilter + @"
ORDER BY
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END";
			}
			else
			{
				sql = @"SELECT
	USER                                                                                                                                 AS Owner,
	1                                                                                                                                    AS IsDefault,
	p.OVERLOAD                                                                                                                           AS Overload,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END                                                                 AS PackageName,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END                                                     AS ProcedureName,
	CASE WHEN a.DATA_TYPE IS NULL THEN 'PROCEDURE' WHEN a.DATA_TYPE = 'TABLE' THEN 'TABLE_FUNCTION' ELSE 'FUNCTION' END AS ProcedureType
FROM USER_PROCEDURES p
		LEFT OUTER JOIN USER_ARGUMENTS a ON
			((a.PACKAGE_NAME = p.OBJECT_NAME AND a.OBJECT_NAME = p.PROCEDURE_NAME)
					OR (a.PACKAGE_NAME IS NULL AND p.PROCEDURE_NAME IS NULL AND a.OBJECT_NAME = p.OBJECT_NAME))
				AND a.ARGUMENT_NAME IS NULL
				AND a.DATA_LEVEL = 0
WHERE ((p.OBJECT_TYPE IN ('PROCEDURE', 'FUNCTION') AND PROCEDURE_NAME IS NULL) OR PROCEDURE_NAME IS NOT NULL)
ORDER BY
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.OBJECT_NAME ELSE NULL END,
	CASE WHEN p.OBJECT_TYPE = 'PACKAGE' THEN p.PROCEDURE_NAME ELSE p.OBJECT_NAME END";
			}

			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var schema        = rd.GetString(0);
				var isDefault     = rd.GetInt32(1) != 0;
				var overload      = rd.IsDBNull(2) ? null : rd.GetString(2);
				var packageName   = rd.IsDBNull(3) ? null : rd.GetString(3);
				var procedureName = rd.GetString(4);
				var procedureType = rd.GetString(5);

				return new ProcedureInfo()
				{
					ProcedureID     = $"{schema}.{overload}.{packageName}.{procedureName}",
					SchemaName      = schema,
					PackageName     = packageName,
					ProcedureName   = procedureName,
					IsFunction      = procedureType != "PROCEDURE",
					IsTableFunction = procedureType == "TABLE_FUNCTION",
					IsDefaultSchema = isDefault
				};
			},
				sql).ToList();
		}

		private void LoadCurrentUser(DataConnection dataConnection)
		{
			_currentUser ??= dataConnection.Execute<string>("select user from dual");
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			if (SchemasFilter == null)
				return new();

			// SEQUENCE filter filters-out non-argument records without DATA_TYPE
			// check https://llblgen.com/tinyforum/Messages.aspx?ThreadID=22795
			// DATA_LEVEL filters out sub-types
			string sql;
			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
			{
				sql = @"SELECT
	OWNER          AS Owner,
	PACKAGE_NAME   AS PackageName,
	OBJECT_NAME    AS ProcedureName,
	OVERLOAD       AS Overload,
	IN_OUT         AS Direction,
	DATA_LENGTH    AS DataLength,
	ARGUMENT_NAME  AS Name,
	DATA_TYPE      AS Type,
	POSITION       AS Ordinal,
	DATA_PRECISION AS Precision,
	DATA_SCALE     AS Scale
FROM ALL_ARGUMENTS
WHERE OWNER " + SchemasFilter + @" AND SEQUENCE > 0 AND DATA_LEVEL = 0
	AND (DATA_TYPE <> 'TABLE' OR IN_OUT <> 'OUT' OR POSITION <> 0)";
			}
			else
			{
				sql = @"SELECT
	USER           AS Owner,
	PACKAGE_NAME   AS PackageName,
	OBJECT_NAME    AS ProcedureName,
	OVERLOAD       AS Overload,
	IN_OUT         AS Direction,
	DATA_LENGTH    AS DataLength,
	ARGUMENT_NAME  AS Name,
	DATA_TYPE      AS Type,
	POSITION       AS Ordinal,
	DATA_PRECISION AS Precision,
	DATA_SCALE     AS Scale
FROM ALL_ARGUMENTS
WHERE SEQUENCE > 0 AND DATA_LEVEL = 0 AND OWNER = USER
	AND (DATA_TYPE <> 'TABLE' OR IN_OUT <> 'OUT' OR POSITION <> 0)";

			}

			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var schema        = rd.GetString(0);
				var packageName   = rd.IsDBNull(1) ?       null : rd.GetString(1);
				var procedureName = rd.GetString(2);
				var overload      = rd.IsDBNull(3) ?       null : rd.GetString(3);
				// IN, OUT, IN/OUT
				var direction     = rd.GetString(4);
				var length        = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
				var name          = rd.IsDBNull(6) ?       null : rd.GetString(6);
				var dataType      = rd.GetString(7);
				// 0 - return value
				var ordinal       = rd.GetInt32(8);
				var precision     = rd.IsDBNull(9) ? (int?)null : rd.GetInt32(9);
				var scale         = rd.IsDBNull(10)? (int?)null : rd.GetInt32(10);

				return new ProcedureParameterInfo()
				{
					ProcedureID   = $"{schema}.{overload}.{packageName}.{procedureName}",
					Ordinal       = ordinal,
					ParameterName = name,
					DataType      = dataType,
					Length        = length,
					Precision     = precision,
					Scale         = scale,
					IsIn          = direction.StartsWith("IN"),
					IsOut         = direction.EndsWith("OUT"),
					IsResult      = ordinal == 0,
					IsNullable    = true
				};
			},
				sql).ToList();
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			switch (columnType)
			{
				case "NUMBER" :
					if (precision == 0) return columnType;
					break;
			}

			return base.GetDbType(options, columnType, dataType, length, precision, scale, udtCatalog, udtSchema, udtName);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			if (dataType == "NUMBER" && precision > 0 && (scale ?? 0) == 0)
			{
				if (precision <  3) return typeof(sbyte);
				if (precision <  5) return typeof(short);
				if (precision < 10) return typeof(int);
				if (precision < 20) return typeof(long);
			}

			if (dataType == "BINARY_INTEGER")
				return typeof(int);
			if (dataType?.StartsWith("INTERVAL DAY") == true)
				return typeof(TimeSpan);
			if (dataType?.StartsWith("INTERVAL YEAR") == true)
				return typeof(long);
			if (dataType?.StartsWith("TIMESTAMP") == true)
				return dataType.EndsWith("TIME ZONE") ? typeof(DateTimeOffset) : typeof(DateTime);

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			switch (dataType)
			{
				case "OBJECT"                 : return DataType.Variant;
				case "BFILE"                  : return DataType.VarBinary;
				case "BINARY_DOUBLE"          : return DataType.Double;
				case "BINARY_FLOAT"           : return DataType.Single;
				case "BINARY_INTEGER"         : return DataType.Int32;
				case "BLOB"                   : return DataType.Blob;
				case "CHAR"                   : return DataType.Char;
				case "CLOB"                   : return DataType.Text;
				case "DATE"                   : return DataType.DateTime;
				case "FLOAT"                  : return DataType.Decimal;
				case "LONG"                   : return DataType.Long;
				case "LONG RAW"               : return DataType.LongRaw;
				case "NCHAR"                  : return DataType.NChar;
				case "NCLOB"                  : return DataType.NText;
				case "NUMBER"                 : return DataType.Decimal;
				case "NVARCHAR2"              : return DataType.NVarChar;
				case "RAW"                    : return DataType.Binary;
				case "VARCHAR2"               : return DataType.VarChar;
				case "XMLTYPE"                : return DataType.Xml;
				case "ROWID"                  : return DataType.VarChar;
				case "REF CURSOR"             : return DataType.Cursor;
				default:
					if (dataType?.StartsWith("TIMESTAMP") == true)
						return dataType.EndsWith("TIME ZONE") ? DataType.DateTimeOffset : DataType.DateTime2;
					if (dataType?.StartsWith("INTERVAL DAY") == true)
						return DataType.Time;
					if (dataType?.StartsWith("INTERVAL YEAR") == true)
						return DataType.Int64;
					break;
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType)
			{
				case "BFILE"                          : return _provider.Adapter.OracleBFileType       .Name;
				case "RAW"                            :
				case "LONG RAW"                       : return _provider.Adapter.OracleBinaryType      .Name;
				case "BLOB"                           : return _provider.Adapter.OracleBlobType        .Name;
				case "CLOB"                           : return _provider.Adapter.OracleClobType        .Name;
				case "DATE"                           : return _provider.Adapter.OracleDateType        .Name;
				case "BINARY_DOUBLE"                  :
				case "BINARY_FLOAT"                   :
				case "NUMBER"                         : return _provider.Adapter.OracleDecimalType     .Name;
				case "INTERVAL DAY TO SECOND"         : return _provider.Adapter.OracleIntervalDSType  .Name;
				case "INTERVAL YEAR TO MONTH"         : return _provider.Adapter.OracleIntervalYMType  .Name;
				case "NCHAR"                          :
				case "LONG"                           :
				case "ROWID"                          :
				case "CHAR"                           : return _provider.Adapter.OracleStringType       .Name;
				case "TIMESTAMP"                      : return _provider.Adapter.OracleTimeStampType    .Name;
				case "TIMESTAMP WITH LOCAL TIME ZONE" : return _provider.Adapter.OracleTimeStampLTZType?.Name ?? _provider.Adapter.OracleTimeStampType.Name;
				case "TIMESTAMP WITH TIME ZONE"       : return _provider.Adapter.OracleTimeStampTZType? .Name ?? _provider.Adapter.OracleTimeStampType.Name;
				case "XMLTYPE"                        : return _provider.Adapter.OracleXmlTypeType      .Name;
				case "REF CURSOR"                     : return _provider.Adapter.OracleRefCursorType    .Name;
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			if (procedure.IsTableFunction && _majorVersion <= 11)
			{
				commandText = "SELECT * FROM TABLE(" + commandText + "(";

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					if (i != 0)
						commandText += ",";
					commandText += "NULL";
				}

				commandText += "))";

				return commandText;
			}

			return base.BuildTableFunctionLoadTableSchemaCommand(procedure, commandText);
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let dt         = GetDataTypeByProviderDbType(r.Field<int>("ProviderType"), options)
				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>  ("AllowDBNull")
				let length     = r.Field<int?>  ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let columnType = GetDbType(options, null, dt, length, precision, scale, null, null, null)
				let systemType = GetSystemType(columnType, null, dt, length, precision, scale, options)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType)
				}
			).ToList();
		}
	}
}
