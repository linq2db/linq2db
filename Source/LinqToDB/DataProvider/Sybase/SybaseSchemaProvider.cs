using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.DataProvider.Sybase
{
	sealed class SybaseSchemaProvider : SchemaProviderBase
	{
		private readonly SybaseDataProvider _provider;

		public SybaseSchemaProvider(SybaseDataProvider provider)
		{
			_provider = provider;
		}

		private int _uniCharSize = 2;
		private int _nCharSize   = 3;

		protected override void InitProvider(DataConnection dataConnection, GetSchemaOptions options)
		{
			base.InitProvider(dataConnection, options);

			_uniCharSize = dataConnection.Execute<int>("select @@unicharsize");
			_nCharSize   = dataConnection.Execute<int>("select @@ncharsize");
		}

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
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
				case "decimal"           :
				case "numeric"           : return DataType.Decimal;
				case "time"              :
				case "bigtime"           : return DataType.Time;
				case "date"              : return DataType.Date;
				case "datetime"          :
				case "bigdatetime"       : return DataType.DateTime;
				case "smalldatetime"     : return DataType.SmallDateTime;
				case "sql_variant"       : return DataType.Variant;
				case "xml"               : return DataType.Xml;
				case "varchar"           : return DataType.VarChar;
				case "char"              : return DataType.Char;
				case "nchar"             :
				case "unichar"           : return DataType.NChar;
				case "nvarchar"          :
				case "univarchar"        : return DataType.NVarChar;
				case "varbinary"         : return DataType.VarBinary;
				case "uniqueidentifier"  : return DataType.Guid;
			}

			return DataType.Undefined;
		}

		protected override string? GetProviderSpecificTypeNamespace() => null;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
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

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			return dataConnection.Query<ColumnInfo>(@"
SELECT
	o.id                                             as TableID,
	c.name                                           as Name,
	Convert(bit, c.status & 0x08)                    as IsNullable,
	c.colid                                          as Ordinal,
	t.name                                           as DataType,
	CASE
		WHEN t.name IN ('nvarchar', 'nchar') THEN c.length / @@ncharsize
		WHEN t.name IN ('univarchar', 'unichar') THEN c.length / @@unicharsize
		ELSE c.length
	END                                              as Length,
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

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

			string? sql = null;

			for (var i = 1; i <= 16; i++)
			{
				if (sql != null)
					sql += "\nUNION ALL";

				sql += string.Format(CultureInfo.InvariantCulture, baseSql, i);
			}

			sql = "SELECT * FROM (" + sql + ") as t WHERE ThisColumn IS NOT NULL";

			return dataConnection.Query<ForeignKeyInfo>(sql).ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			using (var reader = dataConnection.ExecuteReader(
				"sp_oledb_stored_procedures",
				CommandType.StoredProcedure,
				CommandBehavior.Default))
			{
				return reader.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var catalog = rd.GetString(0);
					var schema  = rd.GetString(1);
					var name    = rd.GetString(2).Split(';')[0];

					return new ProcedureInfo()
					{
						ProcedureID     = catalog + "." + schema + "." + name,
						CatalogName     = catalog,
						SchemaName      = schema,
						ProcedureName   = name,
						IsFunction      = rd.GetInt16(3) == 2,
						IsDefaultSchema = schema == "dbo"
					};
				}).ToList();
			}
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			// otherwise GetSchema will throw AseException
			if (dataConnection.Transaction != null && GetProcedureSchemaExecutesProcedure)
				throw new LinqToDBException("Cannot read schema with GetSchemaOptions.GetProcedures = true from transaction. Remove transaction or set GetSchemaOptions.GetProcedures to false");

			using (var reader = dataConnection.ExecuteReader(
				"sp_oledb_getprocedurecolumns",
				CommandType.StoredProcedure,
				CommandBehavior.Default))
			{
				return reader.Query(rd =>
				{
					// IMPORTANT: reader calls must be ordered to support SequentialAccess
					var catalog    = rd.GetString(0);
					var schema     = rd.GetString(1);
					var name       = rd.GetString(2);
					var pName      = rd.GetString(3).TrimStart('@');
					var ordinal    = rd.GetInt32(4);
					var direction  = rd.IsDBNull(5) ? (short?)null : rd.GetInt16(5);
					var isNullable = rd.GetBoolean(8);
					var length     = rd.IsDBNull(10) ? (int?)null : rd.GetInt32(10);
					var scale      = rd.IsDBNull(13) ? (int?)null : rd.GetInt32(13);
					var type       = rd.GetString(15);

					if (type == "nchar" || type == "nvarchar")
						length /= _nCharSize;
					else if (type == "unichar" || type == "univarchar")
						length /= _uniCharSize;

					return new ProcedureParameterInfo()
					{
						ProcedureID   = catalog + "." + schema + "." + name,
						ParameterName = pName,
						IsIn          = direction == 1 || direction == 2,
						IsOut         = direction == 3 || direction == 2,
						Length        = length,
						Precision     = length, // this is also correct...
						Scale         = scale,
						Ordinal       = ordinal,
						IsResult      = direction == 4,
						DataType      = type,
						IsNullable    = isNullable
					};
				}).ToList();
			}
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			DataTable? dt;

			dataConnection.Execute("SET FMTONLY ON");

			if (dataConnection.DataProvider.Name == ProviderName.SybaseManaged)
			{
				// https://github.com/DataAction/AdoNetCore.AseClient/issues/189
				using (var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.Default, parameters))
					dt = rd.Reader!.GetSchemaTable();
			}
			else
				dt = base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);

			dataConnection.Execute("SET FMTONLY OFF");

			return dt?.AsEnumerable().Any() == true ? dt : null;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			var dataTypeNameColumn = "DataTypeName";
			if (!resultTable.Columns.Contains("DataTypeName"))
				dataTypeNameColumn = "NativeDataType";

			return
			(
				from r in resultTable.AsEnumerable()

				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>  ("AllowDBNull")

				let systemType = r.Field<Type>("DataType")
				let length     = r.Field<int> ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let columnType = r.Field<string>(dataTypeNameColumn)
				let dt         = GetDataType(columnType, null, options)

				let Length = columnType is "nchar" or "nvarchar"
					? length / _nCharSize
					: columnType is "unichar" or "univarchar"
						? length / _uniCharSize
						: length

				select new ColumnSchema
				{
					ColumnName = columnName,
					IsNullable = isNullable,
					MemberName = ToValidName(columnName),
					MemberType = ToTypeName(systemType, isNullable),
					SystemType = systemType,
					IsIdentity = r.Field<bool>("IsIdentity"),
					ColumnType = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
				}
			).ToList();
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			// native provider actually supports GetSchema("DataTypes") call, but it return less types than sybase knowns
			// so we will use manually defined type list for native provider too

			// ProviderDbType values copied from values, returned by native prover, but they doesn't make sense to
			// me as they doesn't match AseDbType enum
			return new List<DataTypeInfo>()
			{
				new DataTypeInfo { TypeName = "smallint"        , DataType = typeof(short)   .FullName!, CreateFormat = "smallint"         , ProviderDbType = 16                                                                   },
				new DataTypeInfo { TypeName = "int"             , DataType = typeof(int)     .FullName!, CreateFormat = "int"              , ProviderDbType = 8                                                                    },
				new DataTypeInfo { TypeName = "real"            , DataType = typeof(float)   .FullName!, CreateFormat = "real"             , ProviderDbType = 13                                                                   },
				new DataTypeInfo { TypeName = "float"           , DataType = typeof(double)  .FullName!, CreateFormat = "float({0})"       , ProviderDbType = 6   , CreateParameters = "number of bits used to store the mantissa" },
				new DataTypeInfo { TypeName = "money"           , DataType = typeof(decimal) .FullName!, CreateFormat = "money"            , ProviderDbType = 9                                                                    },
				new DataTypeInfo { TypeName = "smallmoney"      , DataType = typeof(decimal) .FullName!, CreateFormat = "smallmoney"       , ProviderDbType = 17                                                                   },
				new DataTypeInfo { TypeName = "bit"             , DataType = typeof(bool)    .FullName!, CreateFormat = "bit"              , ProviderDbType = 2                                                                    },
				new DataTypeInfo { TypeName = "tinyint"         , DataType = typeof(sbyte)   .FullName!, CreateFormat = "tinyint"          , ProviderDbType = 20                                                                   },
				new DataTypeInfo { TypeName = "bigint"          , DataType = typeof(long)    .FullName!, CreateFormat = "bigint"           , ProviderDbType = 0                                                                    },
				new DataTypeInfo { TypeName = "timestamp"       , DataType = typeof(byte[])  .FullName!, CreateFormat = "timestamp"        , ProviderDbType = 19                                                                   },
				new DataTypeInfo { TypeName = "binary"          , DataType = typeof(byte[])  .FullName!, CreateFormat = "binary({0})"      , ProviderDbType = 1   , CreateParameters = "length"                                    },
				new DataTypeInfo { TypeName = "image"           , DataType = typeof(byte[])  .FullName!, CreateFormat = "image"            , ProviderDbType = 7                                                                    },
				new DataTypeInfo { TypeName = "text"            , DataType = typeof(string)  .FullName!, CreateFormat = "text"             , ProviderDbType = 18                                                                   },
				new DataTypeInfo { TypeName = "ntext"           , DataType = typeof(string)  .FullName!, CreateFormat = "ntext"            , ProviderDbType = 11                                                                   },
				new DataTypeInfo { TypeName = "decimal"         , DataType = typeof(decimal) .FullName!, CreateFormat = "decimal({0}, {1})", ProviderDbType = 5   , CreateParameters = "precision,scale"                           },
				new DataTypeInfo { TypeName = "numeric"         , DataType = typeof(decimal) .FullName!, CreateFormat = "numeric({0}, {1})", ProviderDbType = 5   , CreateParameters = "precision,scale"                           },
				new DataTypeInfo { TypeName = "datetime"        , DataType = typeof(DateTime).FullName!, CreateFormat = "datetime"         , ProviderDbType = 4                                                                    },
				new DataTypeInfo { TypeName = "smalldatetime"   , DataType = typeof(DateTime).FullName!, CreateFormat = "smalldatetime"    , ProviderDbType = 15                                                                   },
				new DataTypeInfo { TypeName = "sql_variant"     , DataType = typeof(object)  .FullName!, CreateFormat = "sql_variant"      , ProviderDbType = 23                                                                   },
				new DataTypeInfo { TypeName = "xml"             , DataType = typeof(string)  .FullName!, CreateFormat = "xml"              , ProviderDbType = 25                                                                   },
				new DataTypeInfo { TypeName = "varchar"         , DataType = typeof(string)  .FullName!, CreateFormat = "varchar({0})"     , ProviderDbType = 22  , CreateParameters = "max length"                                },
				new DataTypeInfo { TypeName = "char"            , DataType = typeof(string)  .FullName!, CreateFormat = "char({0})"        , ProviderDbType = 3   , CreateParameters = "length"                                    },
				new DataTypeInfo { TypeName = "nchar"           , DataType = typeof(string)  .FullName!, CreateFormat = "nchar({0})"       , ProviderDbType = 10  , CreateParameters = "length"                                    },
				new DataTypeInfo { TypeName = "nvarchar"        , DataType = typeof(string)  .FullName!, CreateFormat = "nvarchar({0})"    , ProviderDbType = 12  , CreateParameters = "max length"                                },
				new DataTypeInfo { TypeName = "varbinary"       , DataType = typeof(byte[])  .FullName!, CreateFormat = "varbinary({0})"   , ProviderDbType = 21  , CreateParameters = "max length"                                },
				new DataTypeInfo { TypeName = "uniqueidentifier", DataType = typeof(Guid)    .FullName!, CreateFormat = "uniqueidentifier" , ProviderDbType = 14                                                                   },

				new DataTypeInfo { TypeName = "usmallint"       , DataType = typeof(ushort)  .FullName!, CreateFormat = "usmallint"        , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "uint"            , DataType = typeof(uint)    .FullName!, CreateFormat = "uint"             , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "ubigint"         , DataType = typeof(ulong)   .FullName!, CreateFormat = "ubigint"          , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "bigdatetime"     , DataType = typeof(DateTime).FullName!, CreateFormat = "bigdatetime"      , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "date"            , DataType = typeof(DateTime).FullName!, CreateFormat = "date"             , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "time"            , DataType = typeof(TimeSpan).FullName!, CreateFormat = "time"             , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "bigtime"         , DataType = typeof(TimeSpan).FullName!, CreateFormat = "bigtime"          , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "unitext"         , DataType = typeof(string)  .FullName!, CreateFormat = "unitext"          , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "unichar"         , DataType = typeof(string)  .FullName!, CreateFormat = "unichar"          , ProviderDbType = -1                                                                   },
				new DataTypeInfo { TypeName = "univarchar"      , DataType = typeof(string)  .FullName!, CreateFormat = "univarchar"       , ProviderDbType = -1                                                                   },
			};
		}
	}
}
