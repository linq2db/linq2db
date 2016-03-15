using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using SchemaProvider;

	class SqlServerSchemaProvider : SchemaProviderBase
	{
		bool _isAzure;

		protected override void InitProvider(DataConnection dataConnection)
		{
			var version = dataConnection.Execute<string>("select @@version");

			_isAzure = version.IndexOf("Azure", StringComparison.Ordinal) >= 0;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			return dataConnection.Query<TableInfo>(
				_isAzure ? @"
				SELECT
					TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					TABLE_CATALOG                                         as CatalogName,
					TABLE_SCHEMA                                          as SchemaName,
					TABLE_NAME                                            as TableName,
					CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END       as IsView,
					''                                                    as Description,
					CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END      as IsDefaultSchema
				FROM
					INFORMATION_SCHEMA.TABLES s
					LEFT JOIN
						sys.tables t
					ON
						OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id
				WHERE
					t.object_id IS NULL OR t.is_ms_shipped <> 1"
				: @"
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
						OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = t.object_id
					LEFT JOIN
						sys.extended_properties x
					ON
						OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
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
			return dataConnection.Query<ColumnInfo>(
				_isAzure ? @"
				SELECT
					TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					COLUMN_NAME                                           as Name,
					CASE WHEN IS_NULLABLE = 'YES' THEN 1 ELSE 0 END       as IsNullable,
					ORDINAL_POSITION                                      as Ordinal,
					c.DATA_TYPE                                           as DataType,
					CHARACTER_MAXIMUM_LENGTH                              as Length,
					ISNULL(NUMERIC_PRECISION, DATETIME_PRECISION)         as [Precision],
					NUMERIC_SCALE                                         as Scale,
					''                                                    as [Description],
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c"
				: @"
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
						--OBJECT_ID('[' + TABLE_CATALOG + '].[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']') = x.major_id AND
						COLUMNPROPERTY(OBJECT_ID('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'ColumnID') = x.minor_id AND
						x.name = 'MS_Description'")
				.Select(c =>
				{
					DataTypeInfo dti;

					if (DataTypesDic.TryGetValue(c.DataType, out dti))
					{
						switch (dti.CreateParameters)
						{
							case null :
								c.Length    = null;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "scale" :
								c.Length = null;

								if (c.Scale.HasValue)
									c.Precision = null;

								break;

							case "precision,scale" :
								c.Length = null;
								break;

							case "max length" :
								if (c.Length < 0)
									c.Length = int.MaxValue;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "length"     :
								c.Precision = null;
								c.Scale     = null;
								break;

							case "number of bits used to store the mantissa":
								break;

							default :
								break;
						}
					}

					switch (c.DataType)
					{
						case "geometry"    :
						case "geography"   :
						case "hierarchyid" :
						case "float"       :
							c.Length    = null;
							c.Precision = null;
							c.Scale     = null;
							break;
					}

					return c;
				})
				.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			return dataConnection.Query<ForeingKeyInfo>(@"
				SELECT
					fk.name                                                     as Name,
					DB_NAME() + '.' + SCHEMA_NAME(po.schema_id) + '.' + po.name as ThisTableID,
					pc.name                                                     as ThisColumn,
					DB_NAME() + '.' + SCHEMA_NAME(fo.schema_id) + '.' + fo.name as OtherTableID,
					fc.name                                                     as OtherColumn,
					fkc.constraint_column_id                                    as Ordinal
				FROM sys.foreign_keys fk
					inner join sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
					inner join sys.columns             pc  ON fkc.parent_column_id = pc.column_id and fkc.parent_object_id = pc.object_id
					inner join sys.objects             po  ON fk.parent_object_id = po.object_id
					inner join sys.columns             fc  ON fkc.referenced_column_id = fc.column_id and fkc.referenced_object_id = fc.object_id
					inner join sys.objects             fo  ON fk.referenced_object_id = fo.object_id
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
					CASE WHEN EXISTS(SELECT * FROM sys.objects where name = SPECIFIC_NAME AND type='AF') 
					                                                            THEN 1 ELSE 0 END as IsAggregateFunction,
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
				case "tinyint"          : return DataType.Byte;
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

		protected override string GetProviderSpecificTypeNamespace()
		{
			return "System.Data.SqlTypes";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType)
			{
				case "varbinary"        :
				case "timestamp"        :
				case "rowversion"       :
				case "image"            : return "SqlBinary";
				case "binary"           : return "SqlBinary";
				case "tinyint"          : return "SqlByte";
				case "date"             :
				case "smalldatetime"    :
				case "datetime"         :
				case "datetime2"        : return "SqlDateTime";
				case "bit"              : return "SqlBoolean";
				case "smallint"         : return "SqlInt16";
				case "numeric"          :
				case "decimal"          : return "SqlDecimal";
				case "int"              : return "SqlInt32";
				case "real"             : return "SqlSingle";
				case "float"            : return "SqlDouble";
				case "smallmoney"       :
				case "money"            : return "SqlMoney";
				case "bigint"           : return "SqlInt64";
				case "text"             :
				case "nvarchar"         :
				case "char"             :
				case "nchar"            :
				case "varchar"          :
				case "ntext"            : return "SqlString";
				case "uniqueidentifier" : return "SqlGuid";
				case "xml"              : return "SqlXml";
				case "hierarchyid"      : return "Microsoft.SqlServer.Types.SqlHierarchyId";
				case "geography"        : return "Microsoft.SqlServer.Types.SqlGeography";
				case "geometry"         : return "Microsoft.SqlServer.Types.SqlGeometry";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			switch (dataType)
			{
				case "tinyint"     : return typeof(byte);
				case "hierarchyid" :
				case "geography"   :
				case "geometry"    : return SqlServerDataProvider.GetUdtType(dataType);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}
	}
}
