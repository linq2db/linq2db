using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using SchemaProvider;

	// In theory we can load description field using fn_listextendedproperty table function,
	// but it will require separate call for each table/column/procedure/parameter
	// as SQL Server 2000 doesn't support OUTER APPLY or dynamic parameters for function
	class SqlServer2000SchemaProvider : SqlServerSchemaProvider
	{
		public SqlServer2000SchemaProvider(SqlServerDataProvider provider)
			: base(provider)
		{
		}

		protected override void InitProvider(DataConnection dataConnection)
		{
			IsAzure            = false;
			CompatibilityLevel = dataConnection.Execute<int>("SELECT cmptlevel FROM master.dbo.sysdatabases WHERE name = db_name()");
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query<TableInfo>(@"
				SELECT
					TABLE_CATALOG + '.' + TABLE_SCHEMA + '.' + TABLE_NAME as TableID,
					TABLE_CATALOG                                         as CatalogName,
					TABLE_SCHEMA                                          as SchemaName,
					TABLE_NAME                                            as TableName,
					CASE WHEN TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END       as IsView,
					CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END      as IsDefaultSchema
				FROM
					INFORMATION_SCHEMA.TABLES s")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
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
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp' 
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1
						THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp' 
						OR COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') = 1
						THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c")
				.ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query<ForeignKeyInfo>(@"
				SELECT
					rc.CONSTRAINT_NAME                                             as Name,
					fk.TABLE_CATALOG + '.' + fk.TABLE_SCHEMA + '.' + fk.TABLE_NAME as ThisTableID,
					fk.COLUMN_NAME                                                 as ThisColumn,
					pk.TABLE_CATALOG + '.' + pk.TABLE_SCHEMA + '.' + pk.TABLE_NAME as OtherTableID,
					pk.COLUMN_NAME                                                 as OtherColumn,
					pk.ORDINAL_POSITION                                            as Ordinal
				FROM
					INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
					JOIN
						INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk
					ON
						fk.CONSTRAINT_CATALOG = rc.CONSTRAINT_CATALOG AND
						fk.CONSTRAINT_SCHEMA  = rc.CONSTRAINT_SCHEMA  AND
						fk.CONSTRAINT_NAME    = rc.CONSTRAINT_NAME
					JOIN
						INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk
					ON
						pk.CONSTRAINT_CATALOG = rc.UNIQUE_CONSTRAINT_CATALOG AND
						pk.CONSTRAINT_SCHEMA  = rc.UNIQUE_CONSTRAINT_SCHEMA  AND
						pk.CONSTRAINT_NAME    = rc.UNIQUE_CONSTRAINT_NAME
				WHERE
					fk.ORDINAL_POSITION = pk.ORDINAL_POSITION
				ORDER BY
					ThisTableID,
					Ordinal")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			return dataConnection.Query<ProcedureParameterInfo>(
				@"SELECT
					SPECIFIC_CATALOG COLLATE DATABASE_DEFAULT + '.' + SPECIFIC_SCHEMA + '.' + SPECIFIC_NAME as ProcedureID,
					ORDINAL_POSITION                                                                        as Ordinal,
					PARAMETER_MODE                                                                          as Mode,
					PARAMETER_NAME                                                                          as ParameterName,
					DATA_TYPE                                                                               as DataType,
					CHARACTER_MAXIMUM_LENGTH                                                                as Length,
					NUMERIC_PRECISION                                                                       as [Precision],
					NUMERIC_SCALE                                                                           as Scale,
					CASE WHEN PARAMETER_MODE = 'IN'  OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsIn,
					CASE WHEN PARAMETER_MODE = 'OUT' OR PARAMETER_MODE = 'INOUT' THEN 1 ELSE 0 END          as IsOut,
					CASE WHEN IS_RESULT      = 'YES'                             THEN 1 ELSE 0 END          as IsResult,
					USER_DEFINED_TYPE_CATALOG                                                               as UDTCatalog,
					USER_DEFINED_TYPE_SCHEMA                                                                as UDTSchema,
					USER_DEFINED_TYPE_NAME                                                                  as UDTName,
					1                                                                                       as IsNullable
				FROM
					INFORMATION_SCHEMA.PARAMETERS")
				.ToList();
		}
	}
}
