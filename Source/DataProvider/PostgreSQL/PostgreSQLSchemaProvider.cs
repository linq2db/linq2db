using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Data;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Data;
	using SchemaProvider;

	class PostgreSQLSchemaProvider : SchemaProviderBase
	{
//		protected override string GetDataSourceName(DbConnection dbConnection)
//		{
//			return ((dynamic)dbConnection).HostName;
//		}
//
//		protected override string GetDatabaseName(DbConnection dbConnection)
//		{
//			return ((dynamic)dbConnection).DatabaseName;
//		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return new[]
			{
				new DataTypeInfo
				{
					TypeName = "", DataType = null, CreateFormat = "", CreateParameters = "", ProviderDbType = 0,
				}
				
			}.ToList();
		}

		string _currentUser;

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			return
				dataConnection.Query<TableInfo>(@"
					SELECT
						table_catalog || '.' || table_schema || '.' || table_name as TableID,
						table_catalog                                             as CatalogName,
						table_schema                                              as SchemaName,
						table_name                                                as TableName,
						table_schema = 'public'                                   as IsDefaultSchema,
						table_type = 'VIEW'                                       as IsView
					FROM
						information_schema.tables
					WHERE
						table_schema NOT IN ('pg_catalog','information_schema')")
				.ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
				dataConnection.Query<PrimaryKeyInfo>(@"
					SELECT
						current_database() || '.' || pg_namespace.nspname || '.' || pg_class.relname as TableID,
						pg_constraint.conname                                                        as PrimaryKeyName,
						(select attname from pg_attribute where attrelid = pg_constraint.conrelid and attnum = pg_constraint.conkey[1])
						                                                                             as ColumnName,
						pg_constraint.conkey[1]                                                      as Ordinal
					FROM
						pg_constraint
							JOIN pg_class ON pg_class.oid = pg_constraint.conrelid
								JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid
					WHERE
						pg_constraint.contype = 'p'")
				.ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			return
				dataConnection.Query<ColumnInfo>(@"
					SELECT
						table_catalog || '.' || table_schema || '.' || table_name           as TableID,
						column_name                                                         as Name,
						is_nullable = 'YES'                                                 as IsNullable,
						ordinal_position                                                    as Ordinal,
						data_type                                                           as DataType,
						character_maximum_length                                            as Length,
						COALESCE(numeric_precision, datetime_precision, interval_precision) as Precision,
						numeric_scale                                                       as Scale,
						is_identity = 'YES' OR COALESCE(column_default ~* 'nextval', false) as IsIdentity,
						is_generated <> 'NEVER'                                             as SkipOnInsert,
						is_updatable = 'NO'                                                 as SkipOnUpdate
					FROM
						information_schema.columns
					WHERE
						table_schema NOT IN ('pg_catalog','information_schema')")
				.ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var data = dataConnection.Query(
				rd => new
				{
					name         = rd[0],
					thisTable    = rd[1],
					otherTable   = rd[2],
					thisColumns  = new[] { rd[ 3], rd[ 4], rd[ 5], rd[ 6], rd[ 7], rd[ 8], rd[ 9], rd[10], rd[11], rd[12], rd[13], rd[14], rd[15], rd[16], rd[17], rd[18] },
					otherColumns = new[] { rd[19], rd[20], rd[21], rd[22], rd[23], rd[24], rd[25], rd[26], rd[27], rd[28], rd[29], rd[30], rd[31], rd[32], rd[33], rd[34] },
				}, @"
				SELECT
					pg_constraint.conname,
					pg_constraint.conrelid,
					pg_constraint.confrelid,
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.conrelid  and attnum = pg_constraint.conkey[16]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[01]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[02]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[03]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[04]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[05]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[06]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[07]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[08]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[09]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[10]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[11]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[12]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[13]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[14]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[15]),
					(select attname from pg_attribute where attrelid = pg_constraint.confrelid and attnum = pg_constraint.confkey[16])
				FROM
					pg_constraint
				WHERE
					pg_constraint.contype = 'f'")
				.ToList();


			{
				foreach (var item in data)
				{
					/*
					var name            = Convert.ToString(item.name);
					var thisTableID     = Convert.ToInt32 (rd["ThisTable"]);
					var otherTableID    = Convert.ToInt32 (rd["OtherTable"]);

					var thisTable   = (from t in tables  where t.ID == thisTableID  select t.Table).Single();
					var otherTable  = (from t in tables  where t.ID == otherTableID select t.Table).Single();

					thisTable.ForeignKeys.Add(name, new ForeignKey { KeyName = name, MemberName = name, OtherTable = otherTable });

					for (int i = 1; i <= 16; i++)
					{
						if (rd.IsDBNull(rd.GetOrdinal("ThisColumn"  + i)))
							break;

						var thisColumnName  = Convert.ToString(rd["ThisColumn"  + i]);
						var otherColumnName = Convert.ToString(rd["OtherColumn" + i]);

						var thisColumn  = (from c in columns where c.ID == thisTableID  && c.Column.ColumnName == thisColumnName  select c.Column).Single();
						var otherColumn = (from c in columns where c.ID == otherTableID && c.Column.ColumnName == otherColumnName select c.Column).Single();

						var key = thisTable.ForeignKeys[name];

						key.ThisColumns. Add(thisColumn);
						key.OtherColumns.Add(otherColumn);
					}
					*/
				}
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
				where schema == _currentUser
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
				where schema == _currentUser
				select new ProcedureParameterInfo
				{
					ProcedureID   = schema + "." + name,
					ParameterName = pp.Field<string>("ARGUMENT_NAME"),
					DataType      = pp.Field<string>("DATA_TYPE"),
					Ordinal       = Converter.ChangeTypeTo<int>(pp["POSITION"]),
					Length        = Converter.ChangeTypeTo<int>(pp["DATA_LENGTH"]),
					Precision     = Converter.ChangeTypeTo<int>(pp["DATA_PRECISION"]),
					Scale         = Converter.ChangeTypeTo<int>(pp["DATA_SCALE"]),
					IsIn          = direction.StartsWith("IN"),
					IsOut         = direction.EndsWith("OUT"),
				}
			).ToList();
		}

		protected override string GetDbType(string columnType, DataTypeInfo dataType, int length, int prec, int scale)
		{
			switch (columnType)
			{
				case "NUMBER" :
					if (prec == 0) return columnType;
					break;
			}

			return base.GetDbType(columnType, dataType, length, prec, scale);
		}

		protected override Type GetSystemType(string columnType, DataTypeInfo dataType, int length, int precision, int scale)
		{
			if (columnType == "NUMBER" && precision > 0 && scale == 0)
			{
				if (precision <  3) return typeof(sbyte);
				if (precision <  5) return typeof(short);
				if (precision < 10) return typeof(int);
				if (precision < 20) return typeof(long);
			}

			if (columnType.StartsWith("TIMESTAMP"))
				return columnType.EndsWith("TIME ZONE") ? typeof(DateTimeOffset) : typeof(DateTime);

			return base.GetSystemType(columnType, dataType, length, precision, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			switch (dataType)
			{
				case "OBJECT"                         : return DataType.Variant;
				case "BFILE"                          : return DataType.VarBinary;
				case "BINARY_DOUBLE"                  : return DataType.Double;
				case "BINARY_FLOAT"                   : return DataType.Single;
				case "BLOB"                           : return DataType.Binary;
				case "CHAR"                           : return DataType.Char;
				case "CLOB"                           : return DataType.Text;
				case "DATE"                           : return DataType.DateTime;
				case "FLOAT"                          : return DataType.Decimal;
				case "INTERVAL DAY TO SECOND"         : return DataType.Time;
				case "INTERVAL YEAR TO MONTH"         : return DataType.Int64;
				case "LONG"                           : return DataType.Text;
				case "LONG RAW"                       : return DataType.Binary;
				case "NCHAR"                          : return DataType.NChar;
				case "NCLOB"                          : return DataType.NText;
				case "NUMBER"                         : return DataType.Decimal;
				case "NVARCHAR2"                      : return DataType.NVarChar;
				case "RAW"                            : return DataType.Binary;
				case "VARCHAR2"                       : return DataType.VarChar;
				case "XMLTYPE"                        : return DataType.Xml;
				case "ROWID"                          : return DataType.VarChar;
				default:
					if (dataType.StartsWith("TIMESTAMP"))
						return dataType.EndsWith("TIME ZONE") ? DataType.DateTimeOffset : DataType.DateTime2;

					break;
			}

			return DataType.Undefined;
		}
	}
}
