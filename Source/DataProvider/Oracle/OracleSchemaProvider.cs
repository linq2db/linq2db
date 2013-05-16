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

			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views  = ((DbConnection)dataConnection.Connection).GetSchema("Views");

			return
			(
				from t in tables.AsEnumerable()
				let schema  = t.Field<string>("OWNER")
				let name    = t.Field<string>("TABLE_NAME")
				where IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0 || schema == _currentUser
				select new TableInfo
				{
					TableID         = schema + '.' + name,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema == _currentUser,
					IsView          = false
				}
			).Concat
			(
				from t in views.AsEnumerable()
				let schema  = t.Field<string>("OWNER")
				let name    = t.Field<string>("VIEW_NAME")
				where IncludedSchemas.Length != 0 || ExcludedSchemas.Length != 0 || schema == _currentUser
				select new TableInfo
				{
					TableID         = schema + '.' + name,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema == _currentUser,
					IsView          = true
				}
			).ToList();
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
			var tcs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in tcs.AsEnumerable()
				select new ColumnInfo
				{
					TableID      = c.Field<string>("OWNER") + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					DataType     = c.Field<string>("DATATYPE"),
					IsNullable   = Converter.ChangeTypeTo<string>(c["NULLABLE"]) == "Y",
					Ordinal      = Converter.ChangeTypeTo<int> (c["ID"]),
					Length       = Converter.ChangeTypeTo<int> (c["LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int> (c["PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int> (c["SCALE"]),
					IsIdentity   = false,
				}
			).ToList();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
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
