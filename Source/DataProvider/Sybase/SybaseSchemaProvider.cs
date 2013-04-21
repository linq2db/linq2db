using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using Data;
	using SchemaProvider;

	class SybaseSchemaProvider : SchemaProviderBase
	{
		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "smallint"          : return DataType.Int16;
				case "unsigned smallint" : return DataType.UInt16;
				case "int"               : return DataType.Int32;
				case "unsigned int"      : return DataType.UInt32;
				case "real"              : return DataType.Single;
				case "float"             : return DataType.Double;
				case "money"             : return DataType.Money;
				case "smallmoney"        : return DataType.SmallMoney;
				case "bit"               : return DataType.Boolean;
				case "tinyint"           : return DataType.SByte;
				case "bigint"            : return DataType.Int64;
				case "unsigned bigint"   : return DataType.UInt64;
				case "timestamp"         : return DataType.Timestamp;
				case "binary"            : return DataType.Binary;
				case "image"             : return DataType.Image;
				case "text"              : return DataType.Text;
				case "unitext"           :
				case "ntext"             : return DataType.NText;
				case "decimal"           : return DataType.Decimal;
				case "numeric"           : return DataType.Decimal;
				case "datetime"          : return DataType.DateTime;
				case "smalldatetime"     : return DataType.SmallDateTime;
				case "sql_variant"       : return DataType.Variant;
				case "xml"               : return DataType.Xml;
				case "varchar"           : return DataType.VarChar;
				case "char"              : return DataType.Char;
				case "nchar"             : return DataType.NChar;
				case "nvarchar"          : return DataType.NVarChar;
				case "varbinary"         : return DataType.VarBinary;
				case "uniqueidentifier"  : return DataType.Guid;
			}

			return DataType.Undefined;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			return dataConnection.Query<TableInfo>(@"
				SELECT
					id                                                 as TableID,
					@db                                                as CatalogName,
					USER_NAME(uid)                                     as SchemaName,
					name                                               as TableName,
					CASE WHEN type = 'V' THEN 1 ELSE 0 END             as IsView,
					CASE WHEN USER_NAME(uid) = 'dbo' THEN 1 ELSE 0 END as IsDefaultSchema
				FROM
					sysobjects
				WHERE
					type IN ('U','V')",
				new { @db = dataConnection.Connection.Database})
				.ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return dataConnection.Query<PrimaryKeyInfo>(@"
				SELECT
					i.id                                                              as TableID,
					i.name                                                            as PrimaryKeyName,
					INDEX_COL(USER_NAME(o.uid) + '.' + o.name, i.indid, c.colid)      as ColumnName,
					INDEX_COLORDER(USER_NAME(o.uid) + '.' + o.name, i.indid, c.colid),
					c.colid                                                           as Ordinal
				FROM
					sysindexes i
						JOIN sysobjects o ON i.id = o.id
						JOIN syscolumns c ON i.id = c.id
				WHERE
					i.status2 & 2 = 2 AND
					i.status & 2048 = 2048 AND
					i.indid > 0 AND
					c.colid < i.keycnt + CASE WHEN i.indid = 1 THEN 1 ELSE 0 END")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			return dataConnection.Query<ColumnInfo>(@"
				SELECT
					o.id                                             as TableID,
					c.name                                           as Name,
					Convert(bit, c.status & 0x08)                    as isNullable,
					c.colid                                          as Ordinal,
					t.name                                           as DataType,
					c.length                                         as Length,
					c.prec                                           as [Precision],
					c.scale                                          as Scale,
					Convert(bit, c.status & 0x80)                    as IsIdentity,
					CASE WHEN t.name = 'timestamp' THEN 1 ELSE 0 END as SkipOnInsert,
					CASE WHEN t.name = 'timestamp' THEN 1 ELSE 0 END as SkipOnUpdate
				FROM
					syscolumns c
						JOIN sysobjects o ON c.id       = o.id
						JOIN systypes   t ON c.usertype = t.usertype
				WHERE
					o.type IN ('U','V')")
				.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			const string baseSql = @"
				SELECT
					o.name                           as Name,
					c.tableid                        as ThisTableID,
					r.reftabid                       as OtherTableID,
					COL_NAME(c.tableid,  r.fokey{0})   as ThisColumn,
					COL_NAME(r.reftabid, r.refkey{0})  as OtherColumn,
					{0}                              as Ordinal
				FROM
					sysreferences r
						JOIN sysconstraints c ON r.constrid = c.constrid
							JOIN sysobjects o  ON c.constrid = o.id
							JOIN sysobjects o3 ON c.tableid  = o3.id
						LEFT JOIN sysobjects o2 ON r.reftabid = o2.id
						JOIN sysreferences r2 ON r.constrid = r2.constrid
							LEFT JOIN sysindexes i ON r2.indexid = i.indid AND r2.reftabid = i.id
				WHERE
					c.status = 64";

			string sql = null;

			for (var i = 1; i <= 16; i++)
			{
				if (sql != null)
					sql += "\nUNION ALL";

				sql += string.Format(baseSql, i);
			}

			sql = "SELECT * FROM (" + sql + ") as t WHERE ThisColumn IS NOT NULL";

			return dataConnection.Query<ForeingKeyInfo>(sql).ToList();
		}
	}
}
