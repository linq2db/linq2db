using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;

	class SqlServer2000SchemaProvider : SqlServerSchemaProvider
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
					CASE WHEN TABLE_SCHEMA = 'dbo' THEN 1 ELSE 0 END      as IsDefaultSchema
				FROM
					INFORMATION_SCHEMA.TABLES s")
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
					COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') as IsIdentity,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN c.DATA_TYPE = 'timestamp' THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					INFORMATION_SCHEMA.COLUMNS c")
				.ToList();
		}
	}
}
