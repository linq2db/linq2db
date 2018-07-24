﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Data;
	using SchemaProvider;

	class MySqlSchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			return base.GetDataTypes(dataConnection)
				.Select(dt =>
				{
					if (dt.CreateFormat != null && dt.CreateFormat.EndsWith(" UNSIGNED", StringComparison.OrdinalIgnoreCase))
					{
						return new DataTypeInfo
						{
							TypeName         = dt.CreateFormat,
							DataType         = dt.DataType,
							CreateFormat     = dt.CreateFormat,
							CreateParameters = dt.CreateParameters,
							ProviderDbType   = dt.ProviderDbType,
						};
					}

					return dt;
				})
				.ToList();
		}

		// mysql provider will execute procedure
		protected override bool GetProcedureSchemaExecutesProcedure => true;

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var restrictions = string.IsNullOrEmpty(dataConnection.Connection.Database) ? new [] { (string)null} : new[] { null, dataConnection.Connection.Database };

			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");
			var views  = ((DbConnection)dataConnection.Connection).GetSchema("Views", restrictions);

			return
			(
				from t in tables.AsEnumerable()
				let catalog = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE"
				select new TableInfo
				{
					TableID            = catalog + ".." + name,
					CatalogName        = catalog,
					SchemaName         = "",
					TableName          = name,
					IsDefaultSchema    = true,
					IsView             = false,
					IsProviderSpecific = system || catalog.Equals("sys", StringComparison.OrdinalIgnoreCase)
				}
			).Concat(
				from t in views.AsEnumerable()
				let catalog = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + ".." + name,
					CatalogName     = catalog,
					SchemaName      = "",
					TableName       = name,
					IsDefaultSchema = true,
					IsView          = true,
					IsProviderSpecific = catalog.Equals("sys", StringComparison.OrdinalIgnoreCase)
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			var dbConnection = (DbConnection)dataConnection.Connection;
			var pks          = dbConnection.GetSchema("IndexColumns");
			var idxs         = dbConnection.GetSchema("Indexes");

			return
			(
				from pk  in pks. AsEnumerable()
				join idx in idxs.AsEnumerable()
					on
						pk. Field<string>("INDEX_CATALOG") + "." +
						pk. Field<string>("INDEX_SCHEMA")  + "." +
						pk. Field<string>("INDEX_NAME")    + "." +
						pk. Field<string>("TABLE_NAME")
					equals
						idx.Field<string>("INDEX_CATALOG") + "." +
						idx.Field<string>("INDEX_SCHEMA")  + "." +
						idx.Field<string>("INDEX_NAME")    + "." +
						idx.Field<string>("TABLE_NAME")
				where idx.Field<bool>("PRIMARY")
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("INDEX_SCHEMA") + ".." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("INDEX_NAME"),
					ColumnName     = pk.Field<string>("COLUMN_NAME"),
					Ordinal        = pk.Field<int>   ("ORDINAL_POSITION"),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var tcs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");
//			var vcs = ((DbConnection)dataConnection.Connection).GetSchema("ViewColumns");

			var ret =
			(
				from c in tcs.AsEnumerable()
				let dataType = c.Field<string>("DATA_TYPE")
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_SCHEMA") + ".." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					IsNullable   = c.Field<string>("IS_NULLABLE") == "YES",
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					DataType     = dataType,
					Length       = Converter.ChangeTypeTo<long?>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision    = Converter.ChangeTypeTo<int?> (c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int?> (c["NUMERIC_SCALE"]),
					ColumnType   = c.Field<string>("COLUMN_TYPE"),
					IsIdentity   = c.Field<string>("EXTRA") == "auto_increment",
				}
			)
//			.Concat(
//				from c in vcs.AsEnumerable()
//				let dataType = c.Field<string>("DATA_TYPE")
//				select new ColumnInfo
//				{
//					TableID      = c.Field<string>("VIEW_SCHEMA") + ".." + c.Field<string>("VIEW_NAME"),
//					Name         = c.Field<string>("COLUMN_NAME"),
//					IsNullable   = c.Field<string>("IS_NULLABLE") == "YES",
//					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
//					DataType     = dataType,
//					Length       = Converter.ChangeTypeTo<long?>(c["CHARACTER_MAXIMUM_LENGTH"]),
//					Precision    = Converter.ChangeTypeTo<int?> (c["NUMERIC_PRECISION"]),
//					Scale        = Converter.ChangeTypeTo<int?> (c["NUMERIC_SCALE"]),
//					ColumnType   = c.Field<string>("COLUMN_TYPE"),
//					IsIdentity   = c.Field<string>("EXTRA") == "auto_increment",
//				}
//			)
			.Select(ci =>
			{
				switch (ci.DataType)
				{
					case "bit"        :
					case "date"       :
					case "datetime"   :
					case "timestamp"  :
					case "time"       :
					case "tinyint"    :
					case "smallint"   :
					case "int"        :
					case "year"       :
					case "mediumint"  :
					case "bigint"     :
					case "tiny int"   :
						ci.Precision = null;
						ci.Scale     = null;
						break;
				}

				return ci;
			})
			.ToList();

			return ret;
		}

		protected override List<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var fks = ((DbConnection)dataConnection.Connection).GetSchema("Foreign Key Columns");

			return
			(
				from fk in fks.AsEnumerable()
				select new ForeignKeyInfo
				{
					Name         = fk.Field<string>("CONSTRAINT_NAME"),
					ThisTableID  = fk.Field<string>("TABLE_SCHEMA")   + ".." + fk.Field<string>("TABLE_NAME"),
					ThisColumn   = fk.Field<string>("COLUMN_NAME"),
					OtherTableID = fk.Field<string>("REFERENCED_TABLE_SCHEMA") + ".." + fk.Field<string>("REFERENCED_TABLE_NAME"),
					OtherColumn  = fk.Field<string>("REFERENCED_COLUMN_NAME"),
					Ordinal      = Converter.ChangeTypeTo<int>(fk["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "bit"        : return DataType.UInt64;
				case "blob"       : return DataType.Blob;
				case "tinyblob"   : return DataType.Binary;
				case "mediumblob" : return DataType.Binary;
				case "longblob"   : return DataType.Binary;
				case "binary"     : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "date"       : return DataType.Date;
				case "datetime"   : return DataType.DateTime;
				case "timestamp"  : return DataType.Timestamp;
				case "time"       : return DataType.Time;
				case "char"       : return DataType.Char;
				case "nchar"      : return DataType.NChar;
				case "varchar"    : return DataType.VarChar;
				case "nvarchar"   : return DataType.NVarChar;
				case "set"        : return DataType.NVarChar;
				case "enum"       : return DataType.NVarChar;
				case "tinytext"   : return DataType.Text;
				case "text"       : return DataType.Text;
				case "mediumtext" : return DataType.Text;
				case "longtext"   : return DataType.Text;
				case "double"     : return DataType.Double;
				case "float"      : return DataType.Single;
				case "tinyint"    : return columnType == "tinyint(1)" ? DataType.Boolean : DataType.SByte;
				case "smallint"   : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt16 : DataType.Int16;
				case "int"        : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "year"       : return DataType.Int32;
				case "mediumint"  : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt32 : DataType.Int32;
				case "bigint"     : return columnType != null && columnType.Contains("unsigned") ? DataType.UInt64 : DataType.Int64;
				case "decimal"    : return DataType.Decimal;
				case "tiny int"   : return DataType.Byte;
			}

			return DataType.Undefined;
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			// GetSchema("PROCEDURES") not used, as for MySql 5.7 (but not mariadb/mysql 5.6) it returns procedures from
			// sys database too
			return dataConnection
				.Query(rd =>
				{
					var catalog = Converter.ChangeTypeTo<string>(rd[0]);
					var name    = Converter.ChangeTypeTo<string>(rd[1]);
					return new ProcedureInfo()
					{
						ProcedureID         = catalog + "." + name,
						CatalogName         = catalog,
						SchemaName          = null,
						ProcedureName       = name,
						IsFunction          = Converter.ChangeTypeTo<string>(rd[2]) == "FUNCTION",
						IsTableFunction     = false,
						IsAggregateFunction = false,
						IsDefaultSchema     = true,
						ProcedureDefinition = Converter.ChangeTypeTo<string>(rd[3])
				};
				}, "SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.routines WHERE ROUTINE_SCHEMA = database()")
				.ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			// don't use GetSchema("PROCEDURE PARAMETERS") as MySql provider's implementation does strange stuff
			// instead of just quering of INFORMATION_SCHEMA view. It returns incorrect results and breaks provider
			return dataConnection
				.Query(rd =>
				{
					var mode = Converter.ChangeTypeTo<string>(rd[2]);
					return new ProcedureParameterInfo()
					{
						ProcedureID   = rd.GetString(0) + "." + rd.GetString(1),
						ParameterName = Converter.ChangeTypeTo<string>(rd[4]),
						IsIn          = mode == "IN"  || mode == "INOUT",
						IsOut         = mode == "OUT" || mode == "INOUT",
						Precision     = Converter.ChangeTypeTo<int?>(rd["NUMERIC_PRECISION"]),
						Scale         = Converter.ChangeTypeTo<int?>(rd["NUMERIC_SCALE"]),
						Ordinal       = Converter.ChangeTypeTo<int>(rd["ORDINAL_POSITION"]),
						IsResult      = mode == null,
						DataType      = rd.GetString(7).ToUpper(),
					};
				}, "SELECT SPECIFIC_SCHEMA, SPECIFIC_NAME, PARAMETER_MODE, ORDINAL_POSITION, PARAMETER_NAME, NUMERIC_PRECISION, NUMERIC_SCALE, DATA_TYPE FROM INFORMATION_SCHEMA.parameters WHERE SPECIFIC_SCHEMA = database()")
				.ToList();
		}

		protected override DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			var rv = base.GetProcedureSchema(dataConnection, commandText, commandType, parameters);

			// for no good reason if procedure doesn't return table data but have output parameters, MySql provider
			// returns fake schema with output parameters as columns
			// we can detect it by column name prefix
			// https://github.com/mysql/mysql-connector-net/blob/5864e6b21a8b32f5154b53d1610278abb3cb1cee/Source/MySql.Data/StoredProcedure.cs#L42
			if (rv != null && rv.AsEnumerable().Any(r => r.Field<string>("ColumnName").StartsWith("@_cnet_param_")))
				rv = null;

			return rv;
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
		{
#if !NETSTANDARD
			return
			(
				from r in resultTable.AsEnumerable()

				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType = DataTypes.FirstOrDefault(t => t.ProviderDbType == providerType)
				let columnType = dataType == null ? null : dataType.TypeName

				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>("AllowDBNull")

				let length = r.Field<int>("ColumnSize")
				let precision = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale = Converter.ChangeTypeTo<int>(r["NumericScale"])

				let systemType = GetSystemType(columnType, null, dataType, length, precision, scale)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(columnType, dataType, length, precision, scale),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.IsNull("IsIdentity") ? false : r.Field<bool>("IsIdentity")
				}
			).ToList();
#else
			return new List<ColumnSchema>();
#endif
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return "MySql.Data.Types";
		}

		protected override string GetProviderSpecificType(string dataType)
		{
			switch (dataType.ToLower())
			{
				case "geometry"  : return "MySqlGeometry";
				case "decimal"   : return "MySqlDecimal";
				case "date"      :
				case "newdate"   :
				case "datetime"  :
				case "timestamp" : return "MySqlDateTime";
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (columnType != null && columnType.Contains("unsigned"))
			{
				switch (dataType.ToLower())
				{
					case "smallint"   : return typeof(UInt16);
					case "int"        : return typeof(UInt32);
					case "mediumint"  : return typeof(UInt32);
					case "bigint"     : return typeof(UInt64);
					case "tiny int"   : return typeof(Byte);
				}
			}

			switch (dataType)
			{
				case "tinyint"   :
					if (columnType == "tinyint(1)")
						return typeof(Boolean);
					break;
				case "datetime2" : return typeof(DateTime);
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}
	}
}
