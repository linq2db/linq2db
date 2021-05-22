using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Numerics;
	using Common;
	using Data;
	using SchemaProvider;

	class FirebirdSchemaProvider : SchemaProviderBase
	{
		private readonly FirebirdDataProvider _provider;

		public FirebirdSchemaProvider(FirebirdDataProvider provider)
		{
			_provider = provider;
		}

		protected override string GetDatabaseName(DataConnection dbConnection)
		{
			return Path.GetFileNameWithoutExtension(base.GetDatabaseName(dbConnection));
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
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
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema == "SYSDBA",
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description     = t.Field<string>("DESCRIPTION")
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var tcs  = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in tcs.AsEnumerable()
				let type      = c.Field<string>("COLUMN_DATA_TYPE")
				let dt        = GetDataType(type, options)
				let precision = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"])
				select new ColumnInfo
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME"),
					DataType     = dt?.TypeName,
					IsNullable   = Converter.ChangeTypeTo<bool>(c["IS_NULLABLE"]),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					Length       = (type != "char" && type != "varchar") ? null : Converter.ChangeTypeTo<long>(c["COLUMN_SIZE"]),
					Precision    = precision == 0 ? null : precision,
					Scale        = (type != "decimal" && type != "numeric") ? null : Converter.ChangeTypeTo<int>(c["NUMERIC_SCALE"]),
					IsIdentity   = false,
					Description  = c.Field<string>("DESCRIPTION"),
					SkipOnInsert = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
					SkipOnUpdate = Converter.ChangeTypeTo<bool>(c["IS_READONLY"]),
				}
			).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
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

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
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
					SchemaName          = schema,
					ProcedureName       = name,
					IsDefaultSchema     = schema.IsNullOrEmpty(),
					ProcedureDefinition = p.Field<string>("SOURCE")
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
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
					IsNullable    = Converter.ChangeTypeTo<bool>(pp["IS_NULLABLE"])
				}
			).ToList();
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let systemType   = r.Field<Type>("DataType")
				let columnName   = r.Field<string>("ColumnName")
				let providerType = Converter.ChangeTypeTo<int>(r["ProviderType"])
				let dataType     = GetDataTypeByProviderDbType(providerType, options)
				let columnType   = dataType == null ? null : dataType.TypeName
				let length       = r.Field<int> ("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int> (r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int> (r["NumericScale"])
				let isNullable   = Converter.ChangeTypeTo<bool>(r["AllowDBNull"])

				select new ColumnSchema
				{
					ColumnType           = GetDbType(options, columnType, dataType, length, precision, scale, null, null, null),
					ColumnName           = columnName,
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					Precision            = providerType == 21 ? 16 : null
				}
			).ToList();
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			try
			{
				return base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);
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

			var knownTypes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (var dataType in dataTypes)
			{
				knownTypes.Add(dataType.TypeName);
				if (dataType.CreateFormat.IsNullOrEmpty() && !dataType.CreateParameters.IsNullOrEmpty())
				{
					dataType.CreateFormat =
						dataType.TypeName + "(" +
						string.Join(",", dataType.CreateParameters.Split(',').Select((_,i) => "{" + i + "}")) +
						")";
				}
			}

			// as on 8.0.1 version provider doesn't add new FB4 types to DATATYPES schema API and older boolean type
			if (!knownTypes.Contains("boolean"))
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = false, TypeName = "boolean", DataType = "System.Boolean", ProviderDbType = 3 });
			if (!knownTypes.Contains("int128"))
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = false, TypeName = "int128", DataType = "System.Numerics.BigInteger", ProviderDbType = 23 });
			if (!knownTypes.Contains("decfloat"))
			{
				// decfloat(16)
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "decfloat", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbDecFloat", CreateFormat = "DECFLOAT({0})", ProviderDbType = 21 });
				// decfloat(34)
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "decfloat", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbDecFloat", CreateFormat = null, ProviderDbType = 22 });
			}
			if (!knownTypes.Contains("timestamp with time zone"))
			{
				// tstz
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "timestamp with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedDateTime", ProviderDbType = 17 });
				// tstzEx
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "timestamp with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedDateTime", ProviderDbType = 18 });
			}
			if (!knownTypes.Contains("time with time zone"))
			{
				//ttz
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "time with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedTime", ProviderDbType = 19 });
				//ttzEx
				dataTypes.Add(new DataTypeInfo { ProviderSpecific = true, TypeName = "time with time zone", DataType = $"{FirebirdProviderAdapter.TypesNamespace}.FbZonedTime", ProviderDbType = 20 });
			}

			return dataTypes;
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			return dataType?.ToLower() switch
			{
				"array"                    => DataType.VarBinary,
				"bigint"                   => DataType.Int64,
				"blob"                     => DataType.Blob,
				"char"                     => DataType.NChar,
				"date"                     => DataType.Date,
				"decimal"                  => DataType.Decimal,
				"double precision"         => DataType.Double,
				"float"                    => DataType.Single,
				"integer"                  => DataType.Int32,
				"numeric"                  => DataType.Decimal,
				"smallint"                 => DataType.Int16,
				"blob sub_type 1"          => DataType.Text,
				"time"                     => DataType.Time,
				"timestamp"                => DataType.DateTime,
				"varchar"                  => DataType.NVarChar,
				"int128"                   => DataType.Int128,
				"decfloat"                 => DataType.DecFloat,
				"timestamp with time zone" => DataType.DateTimeOffset,
				"time with time zone"      => DataType.TimeTZ,
				_                          => DataType.Undefined,
			};
		}

		protected override string? GetProviderSpecificTypeNamespace()
		{
			return _provider.Adapter.ProviderTypesNamespace;
		}

		protected override string? GetProviderSpecificType(string? dataType)
		{
			switch (dataType?.ToLower())
			{
				case "decfloat"                : return _provider.Adapter.FbDecFloatType?.Name;
				case "timestamp with time zone": return _provider.Adapter.FbZonedDateTimeType?.Name;
				case "time with time zone"     : return _provider.Adapter.FbZonedTimeType?.Name;
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, long? length, int? precision, int? scale, GetSchemaOptions options)
		{
			switch (dataType?.ToLower())
			{
				case "int128"                  : return typeof(BigInteger);
				case "decfloat"                : return _provider.Adapter.FbDecFloatType;
				case "timestamp with time zone": return _provider.Adapter.FbZonedDateTimeType;
				case "time with time zone"     : return _provider.Adapter.FbZonedTimeType;
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options);
		}

		protected override void LoadProcedureTableSchema(DataConnection dataConnection, GetSchemaOptions options, ProcedureSchema procedure, string commandText, List<TableSchema> tables)
		{
			base.LoadProcedureTableSchema(dataConnection, options, procedure, commandText, tables);

			// remove output parameters, defined for return columns if `FOR SELECT` procedures
			if (procedure.ResultTable != null)
				foreach (var col in procedure.ResultTable.Columns)
					for (var i = 0; i < procedure.Parameters.Count; i++)
						if (procedure.Parameters[i].IsOut && col.ColumnName == procedure.Parameters[i].ParameterName)
						{
							procedure.Parameters.RemoveAt(i);
							break;
						}
		}
	}
}
