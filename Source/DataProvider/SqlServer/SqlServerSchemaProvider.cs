using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using SchemaProvider;

	class SqlServerSchemaProvider : SchemaProviderBase
	{
		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			return dataConnection.Query<TableInfo>(@"
				SELECT
					TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					TABLE_CATALOG                                         as CatalogName,
					TABLE_SCHEMA                                          as SchemaName,
					TABLE_NAME                                            as TableName,
					CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END       as IsView,
					ISNULL(CONVERT(varchar(8000), x.Value), '')           as Description,
					CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END      as IsDefaultSchema
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
				.ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return dataConnection.Query<PrimaryKeyInfo>(@"
				SELECT
					k.TABLE_CATALOG + '.' + k.TABLE_SCHEMA + '.' + k.TABLE_NAME as TableID,
					k.CONSTRAINT_NAME                                           as PrimaryKeyName,
					k.COLUMN_NAME                                               as ColumnName,
					k.ORDINAL_POSITION                                          as Ordinal
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
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			return dataConnection.Query<ColumnInfo>(@"
				SELECT
					TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					COLUMN_NAME                                           as Name,
					CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END       as IsNullable,
					ORDINAL_POSITION                                      as Ordinal,
					c.DATA_TYPE                                           as DataType,
					CHARACTER_MAXIMUM_LENGTH                              as Length,
					ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)         as [Precision],
					NUMERIC_SCALE                                         as Scale,
					ISNULL(CONVERT(varchar(8000), x.Value), '')           as [Description],
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c
					LEFT JOIN
						sys.extended_properties x
					ON
						OBJECT_ID(TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME) = x.major_id AND
						ORDINAL_POSITION = x.minor_id AND
						x.name = 'MS_Description'")
				.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			return dataConnection.Query<ForeingKeyInfo>(@"
				SELECT
					rc.CONSTRAINT_NAME                                             as Name,
					fk.TABLE_CATALOG + '.' + fk.TABLE_SCHEMA + '.' + fk.TABLE_NAME as ThisTableID,
					fk.COLUMN_NAME                                                 as ThisColumn,
					pk.TABLE_CATALOG + '.' + pk.TABLE_SCHEMA + '.' + pk.TABLE_NAME as OtherTableID,
					pk.COLUMN_NAME                                                 as OtherColumn,
					cu.ORDINAL_POSITION                                            as Ordinal
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
					ThisTableID,
					Ordinal")
				.ToList();
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			return dataConnection.Query<ProcedureInfo>(@"
				SELECT
					SPECIFIC_CATALOG + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME                as ProcedureID,
					SPECIFIC_CATALOG                                                              as CatalogName,
					SPECIFIC_SCHEMA                                                               as SchemaName,
					SPECIFIC_NAME                                                                 as ProcedureName,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION'                         THEN 1 ELSE 0 END as IsFunction,
					CASE WHEN ROUTINE_TYPE = 'FUNCTION' AND DATA_TYPE = 'TABLE' THEN 1 ELSE 0 END as IsTableFunction,
					CASE WHEN SPECIFIC_SCHEMA = 'dbo'                           THEN 1 ELSE 0 END as IsDefaultSchema
				FROM
					INFORMATION_SCHEMA.ROUTINES")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			return dataConnection.Query<ProcedureParameterInfo>(@"
				SELECT
					SPECIFIC_CATALOG + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME                 as ProcedureID,
					ORDINAL_POSITION                                                               as Ordinal,
					PARAMETER_MODE                                                                 as Mode,
					PARAMETER_NAME                                                                 as ParameterName,
					DATA_TYPE                                                                      as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                       as Length,
					NUMERIC_PRECISION                                                              as [Precision],
					NUMERIC_SCALE                                                                  as Scale,
					CASE WHEN PARAMETER_MODE = 'IN'  OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END as IsIn,
					CASE WHEN PARAMETER_MODE = 'OUT' OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END as IsOut,
					CASE WHEN IS_RESULT      = 'YES'                             THEN 1 ELSE 0 END as IsResult
				FROM
					INFORMATION_SCHEMA.PARAMETERS")
				.ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
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

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType)
		{
			switch (columnType)
			{
				case "hierarchyid" :
				case "geography"   :
				case "geometry"    : return SqlServerDataProvider.GetUdtType(columnType);
			}

			return base.GetSystemType(columnType, dataType);
		}
	}
}
