using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Data;
	using SchemaProvider;

	class FirebirdSchemaProvider : SchemaProviderBase
	{
		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(base.GetDatabaseName(dbConnection));
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where !ConvertTo<bool>.From(t["IS_SYSTEM_TABLE"])
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("OWNER_NAME")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo
				{
					TableID         = catalog + '.' + t.Field<string>("TABLE_SCHEMA") + '.' + name,
					CatalogName     = catalog,
					SchemaName      = null, //schema,
					TableName       = name,
					IsDefaultSchema = schema == "SYSDBA",
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description     = t.Field<string>("DESCRIPTION")
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			var pks = ((DbConnection)dataConnection.Connection).GetSchema("PrimaryKeys");

			return
			(
				from pk in pks.AsEnumerable()
				select new PrimaryKeyInfo
				{
					TableID        = pk.Field<string>("TABLE_CATALOG") + "." + pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("PK_NAME"),
					ColumnName     = pk.Field<string>("COLUMN_NAME"),
					Ordinal        = ConvertTo<int>.From(pk["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var tcs  = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in tcs.AsEnumerable()
				join dt in DataTypes on c.Field<string>("COLUMN_DATA_TYPE") equals dt.TypeName
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					DataType     = dt.TypeName,
					IsNullable   = Converter.ChangeTypeTo<bool>(c["IS_NULLABLE"]),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					Length       = Converter.ChangeTypeTo<long>(c["COLUMN_SIZE"]),
					Precision    = Converter.ChangeTypeTo<int> (c["NUMERIC_PRECISION"]),
					Scale        = Converter.ChangeTypeTo<int> (c["NUMERIC_SCALE"]),
					IsIdentity   = false,
					Description  = c.Field<string>("DESCRIPTION"),
					SkipOnInsert = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
					SkipOnUpdate = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
				}
			).ToList();
		}

		protected override List<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var cols = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeyColumns");

			return
			(
				from c in cols.AsEnumerable()
				select new ForeignKeyInfo
				{
					Name         = c.Field<string>("CONSTRAINT_NAME"),
					ThisTableID  = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					ThisColumn   = c.Field<string>("COLUMN_NAME"),
					OtherTableID = c.Field<string>("REFERENCED_TABLE_CATALOG") + "." + c.Field<string>("REFERENCED_TABLE_SCHEMA") + "." + c.Field<string>("REFERENCED_TABLE_NAME"),
					OtherColumn  = c.Field<string>("REFERENCED_COLUMN_NAME"),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

			return
			(
				from p in ps.AsEnumerable()
				let catalog = p.Field<string>("PROCEDURE_CATALOG")
				let schema  = p.Field<string>("PROCEDURE_SCHEMA")
				let name    = p.Field<string>("PROCEDURE_NAME")
				select new ProcedureInfo
				{
					ProcedureID         = catalog + "." + schema + "." + name,
					CatalogName         = catalog,
					SchemaName          = null, //schema,
					ProcedureName       = name,
					IsDefaultSchema     = schema.IsNullOrEmpty(),
					ProcedureDefinition = p.Field<string>("SOURCE")
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			var pps = ((DbConnection)dataConnection.Connection).GetSchema("ProcedureParameters");

			return
			(
				from pp in pps.AsEnumerable()
				let catalog   = pp.Field<string>("PROCEDURE_CATALOG")
				let schema    = pp.Field<string>("PROCEDURE_SCHEMA")
				let name      = pp.Field<string>("PROCEDURE_NAME")
				let direction = ConvertTo<int>.From(pp["PARAMETER_DIRECTION"])
				select new ProcedureParameterInfo
				{
					ProcedureID   = catalog + "." + schema + "." + name,
					ParameterName = pp.Field<string>("PARAMETER_NAME"),
					DataType      = pp.Field<string>("PARAMETER_DATA_TYPE"),
					Ordinal       = Converter.ChangeTypeTo<int>(pp["ORDINAL_POSITION"]) + (direction - 1) * 1000,
					Length        = Converter.ChangeTypeTo<int>(pp["PARAMETER_SIZE"]),
					Precision     = Converter.ChangeTypeTo<int>(pp["NUMERIC_PRECISION"]),
					Scale         = Converter.ChangeTypeTo<int>(pp["NUMERIC_SCALE"]),
					IsIn          = direction == 1,
					IsOut         = direction == 2,
				}
			).ToList();
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let systemType   = r.Field<Type>("DataType")
				let columnName   = r.Field<string>("ColumnName")
				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType     = DataTypes.FirstOrDefault(t => t.ProviderDbType == providerType)
				let columnType   = dataType == null ? null : dataType.TypeName
				let length       = r.Field<int> ("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int> (r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int> (r["NumericScale"])
				let isNullable   = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])

				select new ColumnSchema
				{
					ColumnType           = GetDbType(columnType, dataType, length, precision, scale),
					ColumnName           = columnName,
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
				}
			).ToList();
		}

		protected override DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			try
			{
				return base.GetProcedureSchema(dataConnection, commandText, commandType, parameters);
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("SQL error code = -84")) // procedure XXX does not return any values
					return null;
				throw;
			}
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dataTypes = base.GetDataTypes(dataConnection);

			foreach (var dataType in dataTypes)
			{
				if (dataType.CreateFormat.IsNullOrEmpty() && !dataType.CreateParameters.IsNullOrEmpty())
				{
					dataType.CreateFormat =
						dataType.TypeName + "(" +
						string.Join(",", dataType.CreateParameters.Split(',').Select((_,i) => "{" + i + "}")) +
						")";
				}
			}

			return dataTypes;
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "array"            : return DataType.VarBinary;
				case "bigint"           : return DataType.Int64;
				case "blob"             : return DataType.Blob;
				case "char"             : return DataType.NChar;
				case "date"             : return DataType.Date;
				case "decimal"          : return DataType.Decimal;
				case "double precision" : return DataType.Double;
				case "float"            : return DataType.Single;
				case "integer"          : return DataType.Int32;
				case "numeric"          : return DataType.Decimal;
				case "smallint"         : return DataType.Int16;
				case "blob sub_type 1"  : return DataType.Text;
				case "time"             : return DataType.Time;
				case "timestamp"        : return DataType.DateTime;
				case "varchar"          : return DataType.NVarChar;
			}

			return DataType.Undefined;
		}

		protected override string GetProviderSpecificTypeNamespace()
		{
			return null;
		}
	}
}
