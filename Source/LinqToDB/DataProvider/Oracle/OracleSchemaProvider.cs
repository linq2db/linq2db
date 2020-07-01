﻿using System;
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
		private readonly OracleDataProvider _provider;

		public OracleSchemaProvider(OracleDataProvider provider)
		{
			_provider = provider;
		}

		protected override string GetDataSourceName(DataConnection dataConnection)
		{
			var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
			if (connection == null)
				return string.Empty;

			return _provider.Adapter.GetHostName(connection);
		}

		protected override string GetDatabaseName(DataConnection dataConnection)
		{
			var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
			if (connection == null)
				return string.Empty;

			return _provider.Adapter.GetDatabaseName(connection);
		}

		private string? _currentUser;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			LoadCurrentUser(dataConnection);

			if (IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0)
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
					new DataParameter("CurrentUser", _currentUser, DataType.VarChar))
				.ToList();
			}
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		private int GetMajorVersion(DataConnection dataConnection)
		{
			var version = dataConnection.Query<string>("SELECT VERSION FROM PRODUCT_COMPONENT_VERSION WHERE PRODUCT LIKE 'PL/SQL%'").FirstOrDefault();
			if (version != null)
			{
				try
				{
					return int.Parse(version.Split('.')[0]);
				}
				catch { }
			}

			return 0;
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var isIdentitySql = "0                                              as IsIdentity,";
			if (GetMajorVersion(dataConnection) >= 12)
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
					";
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
				var dataType   = rd.IsDBNull(2) ?       null : rd.GetString(2);
				var dataLength = rd.IsDBNull(5) ? (int?)null : rd.GetInt32(5);
				var charLength = rd.IsDBNull(6) ? (int?)null : rd.GetInt32(6);
				return new ColumnInfo
				{
					TableID     = rd.GetString(0),
					Name        = rd.GetString(1),
					DataType    = rd.IsDBNull(2) ? null : rd.GetString(2),
					IsNullable  = rd.GetInt32(3) != 0,
					Ordinal     = rd.IsDBNull(4) ? 0 : rd.GetInt32(4),
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
							FKCOLS.POSITION       = PKCOLS.POSITION
						")
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
			LoadCurrentUser(dataConnection);

			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

			return
			(
				from p in ps.AsEnumerable()
				let schema = p.Field<string>("OWNER")
				let name   = p.Field<string>("OBJECT_NAME")
				where IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || schema == _currentUser
				select new ProcedureInfo
				{
					ProcedureID     = schema + "." + name,
					SchemaName      = schema,
					ProcedureName   = name,
					IsDefaultSchema = schema == _currentUser,
				}
			).ToList();
		}

		private void LoadCurrentUser(DataConnection dataConnection)
		{
			if (_currentUser == null)
				_currentUser = dataConnection.Execute<string>("select user from dual");
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			// uses ALL_ARGUMENTS view
			// https://docs.oracle.com/cd/B28359_01/server.111/b28320/statviews_1014.htm#REFRN20015
			// SELECT * FROM ALL_ARGUMENTS WHERE DATA_LEVEL = 0 AND (OWNER = :OWNER  OR :OWNER is null) AND (OBJECT_NAME = :OBJECTNAME  OR :OBJECTNAME is null)
			var pps = ((DbConnection)dataConnection.Connection).GetSchema("ProcedureParameters");

			// SEQUENCE filter filters-out non-argument records without DATA_TYPE
			// check https://llblgen.com/tinyforum/Messages.aspx?ThreadID=22795
			return
			(
				from pp in pps.AsEnumerable().Where(_ => Converter.ChangeTypeTo<int>(_["SEQUENCE"]) > 0)
				let schema    = pp.Field<string>("OWNER") // not null
				let name      = pp.Field<string>("OBJECT_NAME") // nullable (???)
				let direction = pp.Field<string>("IN_OUT") // nullable: IN, OUT, IN/OUT
				where IncludedSchemas.Count != 0 || ExcludedSchemas.Count != 0 || schema == _currentUser
				select new ProcedureParameterInfo
				{
					ProcedureID   = schema + "." + name,
					ParameterName = pp.Field<string>("ARGUMENT_NAME"), // nullable
					DataType      = pp.Field<string>("DATA_TYPE"), // nullable, but only for sequence = 0
					Ordinal       = Converter.ChangeTypeTo<int>  (pp["POSITION"]), // not null, 0 - return value
					Length        = Converter.ChangeTypeTo<long?>(pp["DATA_LENGTH"]), // nullable
					Precision     = Converter.ChangeTypeTo<int?> (pp["DATA_PRECISION"]), // nullable
					Scale         = Converter.ChangeTypeTo<int?> (pp["DATA_SCALE"]), // nullable
					IsIn          = direction.StartsWith("IN"),
					IsOut         = direction.EndsWith("OUT"),
					IsNullable    = true
				}
			).ToList();
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, long? length, int? prec, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			switch (columnType)
			{
				case "NUMBER" :
					if (prec == 0) return columnType;
					break;
			}

			return base.GetDbType(options, columnType, dataType, length, prec, scale, udtCatalog, udtSchema, udtName);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, long? length, int? precision, int? scale, GetSchemaOptions options)
		{
			if (dataType == "NUMBER" && precision > 0 && (scale ?? 0) == 0)
			{
				if (precision <  3) return typeof(sbyte);
				if (precision <  5) return typeof(short);
				if (precision < 10) return typeof(int);
				if (precision < 20) return typeof(long);
			}

			if (dataType?.StartsWith("TIMESTAMP") == true)
				return dataType.EndsWith("TIME ZONE") ? typeof(DateTimeOffset) : typeof(DateTime);

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
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
				default:
					if (dataType?.StartsWith("TIMESTAMP") == true)
						return dataType.EndsWith("TIME ZONE") ? DataType.DateTimeOffset : DataType.DateTime2;
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
				case "CHAR"                           : return _provider.Adapter.OracleStringType      .Name;
				case "TIMESTAMP"                      : return _provider.Adapter.OracleTimeStampType   .Name;
				case "TIMESTAMP WITH LOCAL TIME ZONE" : return _provider.Adapter.OracleTimeStampLTZType.Name;
				case "TIMESTAMP WITH TIME ZONE"       : return _provider.Adapter.OracleTimeStampTZType .Name;
				case "XMLTYPE"                        : return _provider.Adapter.OracleXmlTypeType     .Name;
			}

			return base.GetProviderSpecificType(dataType);
		}
	}
}
