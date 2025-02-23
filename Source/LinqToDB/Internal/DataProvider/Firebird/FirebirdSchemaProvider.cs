using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Firebird
{
	sealed class FirebirdSchemaProvider : SchemaProviderBase
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
			var tables = dataConnection.Connection.GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where !ConvertTo<bool>.From(t["IS_SYSTEM_TABLE"])
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("OWNER_NAME")
				let name    = t.Field<string>("TABLE_NAME")
				select new TableInfo()
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
			var pks = dataConnection.Connection.GetSchema("PrimaryKeys");

			return
			(
				from pk in pks.AsEnumerable()
				select new PrimaryKeyInfo()
				{
					TableID        = pk.Field<string>("TABLE_CATALOG") + "." + pk.Field<string>("TABLE_SCHEMA") + "." + pk.Field<string>("TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("PK_NAME")!,
					ColumnName     = pk.Field<string>("COLUMN_NAME")!,
					Ordinal        = ConvertTo<int>.From(pk["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var tcs  = dataConnection.Connection.GetSchema("Columns");

			return
			(
				from c in tcs.AsEnumerable()
				let type      = c.Field<string>("COLUMN_DATA_TYPE")
				let dt        = GetDataType(type, null, options)
				let precision = Converter.ChangeTypeTo<int>(c["NUMERIC_PRECISION"])
				select new ColumnInfo()
				{
					TableID      = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name         = c.Field<string>("COLUMN_NAME")!,
					DataType     = dt?.TypeName,
					IsNullable   = Converter.ChangeTypeTo<bool>(c["IS_NULLABLE"]),
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
					Length       = (type != "char" && type != "varchar") ? null : Converter.ChangeTypeTo<int>(c["COLUMN_SIZE"]),
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
			var cols = dataConnection.Connection.GetSchema("ForeignKeyColumns");

			return
			(
				from c in cols.AsEnumerable()
				select new ForeignKeyInfo()
				{
					Name         = c.Field<string>("CONSTRAINT_NAME")!,
					ThisTableID  = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					ThisColumn   = c.Field<string>("COLUMN_NAME")!,
					OtherTableID = c.Field<string>("REFERENCED_TABLE_CATALOG") + "." + c.Field<string>("REFERENCED_TABLE_SCHEMA") + "." + c.Field<string>("REFERENCED_TABLE_NAME"),
					OtherColumn  = c.Field<string>("REFERENCED_COLUMN_NAME")!,
					Ordinal      = Converter.ChangeTypeTo<int> (c["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			string sql;
			if (_provider.Version > FirebirdVersion.v25)
			{
				sql = @"
SELECT * FROM (
	SELECT
		RDB$PACKAGE_NAME                                        AS PackageName,
		RDB$PROCEDURE_NAME                                      AS ProcedureName,
		RDB$DESCRIPTION                                         AS Description,
		RDB$PROCEDURE_SOURCE                                    AS Source,
		CASE WHEN RDB$PROCEDURE_TYPE = 1 THEN 'TF' ELSE 'P' END AS Type
	FROM RDB$PROCEDURES
	WHERE RDB$SYSTEM_FLAG = 0 AND (RDB$PRIVATE_FLAG IS NULL OR RDB$PRIVATE_FLAG = 0) AND RDB$PROCEDURE_TYPE IS NOT NULL
	UNION ALL
	SELECT
		RDB$PACKAGE_NAME,
		RDB$FUNCTION_NAME,
		RDB$DESCRIPTION,
		RDB$FUNCTION_SOURCE,
		'F'
	FROM RDB$FUNCTIONS
	WHERE RDB$SYSTEM_FLAG = 0  AND (RDB$PRIVATE_FLAG IS NULL OR RDB$PRIVATE_FLAG = 0)
) ORDER BY PackageName, ProcedureName";
			}
			else
			{
				sql = @"
SELECT * FROM (
	SELECT
		NULL                                                    AS PackageName,
		RDB$PROCEDURE_NAME                                      AS ProcedureName,
		RDB$DESCRIPTION                                         AS Description,
		RDB$PROCEDURE_SOURCE                                    AS Source,
		CASE WHEN RDB$PROCEDURE_TYPE = 1 THEN 'TF' ELSE 'P' END AS Type
	FROM RDB$PROCEDURES
	WHERE RDB$SYSTEM_FLAG = 0 AND RDB$PROCEDURE_TYPE IS NOT NULL
	UNION ALL
	SELECT
		NULL,
		RDB$FUNCTION_NAME,
		RDB$DESCRIPTION,
		NULL,
		'F'
	FROM RDB$FUNCTIONS
	WHERE RDB$SYSTEM_FLAG = 0
) ORDER BY ProcedureName";
			}

			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				// TrimEnd added to remove padding from union text columns
				var packageName   = rd.IsDBNull(0) ? null : rd.GetString(0).TrimEnd();
				var procedureName = rd.GetString(1).TrimEnd();
				var description   = rd.IsDBNull(2) ? null : rd.GetString(2).TrimEnd();
				var source        = rd.IsDBNull(3) ? null : rd.GetString(3).TrimEnd();
				var procedureType = rd.GetString(4).TrimEnd();

				return new ProcedureInfo()
				{
					ProcedureID         = $"{packageName}.{procedureName}",
					PackageName         = packageName,
					ProcedureName       = procedureName,
					IsFunction          = procedureType != "P",
					IsTableFunction     = procedureType == "TF",
					IsDefaultSchema     = true,
					ProcedureDefinition = source,
					Description         = description
				};
			},
				sql).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			string sql;
			if (_provider.Version > FirebirdVersion.v25)
			{
				sql = @"SELECT
	p.RDB$PACKAGE_NAME                                   AS PackageName,
	p.RDB$PROCEDURE_NAME                                 AS ProcedureName,
	p.RDB$PARAMETER_NAME                                 AS ParameterName,
	p.RDB$PARAMETER_NUMBER                               AS Ordinal,
	p.RDB$PARAMETER_TYPE                                 AS Direction,
	p.RDB$DESCRIPTION                                    AS Decsription,
	f.RDB$FIELD_TYPE                                     AS Type,
	f.RDB$FIELD_SUB_TYPE                                 AS SubType,
	COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH) AS Length,
	f.RDB$FIELD_PRECISION                                AS ""precision"",
	f.RDB$FIELD_SCALE                                    AS Scale,
	COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)           AS IsNullable
FROM RDB$PROCEDURE_PARAMETERS p
	INNER JOIN RDB$PROCEDURES pr ON p.RDB$PROCEDURE_NAME = pr.RDB$PROCEDURE_NAME
		AND (p.RDB$PACKAGE_NAME = pr.RDB$PACKAGE_NAME OR (p.RDB$PACKAGE_NAME IS NULL AND pr.RDB$PACKAGE_NAME IS NULL))
	LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
WHERE p.RDB$SYSTEM_FLAG = 0 AND (pr.RDB$PROCEDURE_TYPE <> 1 OR p.RDB$PARAMETER_TYPE <> 1)
UNION ALL
SELECT
	p.RDB$PACKAGE_NAME,
	p.RDB$FUNCTION_NAME,
	p.RDB$ARGUMENT_NAME,
	p.RDB$ARGUMENT_POSITION,
	CASE WHEN fn.RDB$RETURN_ARGUMENT = p.RDB$ARGUMENT_POSITION THEN 2 ELSE 0 END,
	p.RDB$DESCRIPTION,
	COALESCE(f.RDB$FIELD_TYPE, p.RDB$FIELD_TYPE),
	COALESCE(f.RDB$FIELD_SUB_TYPE, p.RDB$FIELD_TYPE),
	COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH, p.RDB$CHARACTER_LENGTH, p.RDB$FIELD_LENGTH),
	COALESCE(f.RDB$FIELD_PRECISION, p.RDB$FIELD_PRECISION),
	COALESCE(f.RDB$FIELD_SCALE, p.RDB$FIELD_SCALE),
	COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)
	FROM RDB$FUNCTION_ARGUMENTS p
		INNER JOIN RDB$FUNCTIONS fn ON p.RDB$FUNCTION_NAME = fn.RDB$FUNCTION_NAME
			AND (p.RDB$PACKAGE_NAME = fn.RDB$PACKAGE_NAME OR (p.RDB$PACKAGE_NAME IS NULL AND fn.RDB$PACKAGE_NAME IS NULL))
		LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
WHERE p.RDB$SYSTEM_FLAG = 0";
			}
			else
			{
				sql = @"SELECT
	NULL                                                 AS PackageName,
	p.RDB$PROCEDURE_NAME                                 AS ProcedureName,
	p.RDB$PARAMETER_NAME                                 AS ParameterName,
	p.RDB$PARAMETER_NUMBER                               AS Ordinal,
	p.RDB$PARAMETER_TYPE                                 AS Direction,
	p.RDB$DESCRIPTION                                    AS Decsription,
	f.RDB$FIELD_TYPE                                     AS Type,
	f.RDB$FIELD_SUB_TYPE                                 AS SubType,
	COALESCE(f.RDB$CHARACTER_LENGTH, f.RDB$FIELD_LENGTH) AS Length,
	f.RDB$FIELD_PRECISION                                AS ""precision"",
	f.RDB$FIELD_SCALE                                    AS Scale,
	COALESCE(f.RDB$NULL_FLAG, p.RDB$NULL_FLAG)           AS IsNullable
FROM RDB$PROCEDURE_PARAMETERS p
	INNER JOIN RDB$PROCEDURES pr ON p.RDB$PROCEDURE_NAME = pr.RDB$PROCEDURE_NAME
	LEFT JOIN RDB$FIELDS f ON p.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
WHERE p.RDB$SYSTEM_FLAG = 0 AND (pr.RDB$PROCEDURE_TYPE <> 1 OR p.RDB$PARAMETER_TYPE <> 1)
UNION ALL
SELECT
	NULL,
	p.RDB$FUNCTION_NAME,
	NULL,
	p.RDB$ARGUMENT_POSITION,
	CASE WHEN fn.RDB$RETURN_ARGUMENT = p.RDB$ARGUMENT_POSITION THEN 2 ELSE 0 END,
	NULL,
	p.RDB$FIELD_TYPE,
	p.RDB$FIELD_TYPE,
	COALESCE(p.RDB$CHARACTER_LENGTH, p.RDB$FIELD_LENGTH),
	p.RDB$FIELD_PRECISION,
	p.RDB$FIELD_SCALE,
	NULL
FROM RDB$FUNCTION_ARGUMENTS p
		INNER JOIN RDB$FUNCTIONS fn ON p.RDB$FUNCTION_NAME = fn.RDB$FUNCTION_NAME";
			}

			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				// TrimEnd added to remove padding from union text columns
				var packageName   = rd.IsDBNull(0) ? null : rd.GetString(0).TrimEnd();
				var procedureName = rd.GetString(1).TrimEnd();
				var parameterName = rd.IsDBNull(2) ? null : rd.GetString(2).TrimEnd();
				var ordinal       = rd.GetInt32(3);
				// 2: return, 0: in, 1: out
				var direction     = rd.GetInt32(4);
				var description   = rd.IsDBNull(5) ? null : rd.GetString(5).TrimEnd();
				var type          = rd.GetInt32(6);
				var subType       = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7);
				var length        = rd.GetInt32(8);
				var precision     = rd.IsDBNull(9) ? (int?)null : rd.GetInt32(9);
				var scale         = rd.GetInt32(10);
				var isNullable    = rd.IsDBNull(11) ? true : rd.GetInt32(11) != 1;

				return new ProcedureParameterInfo()
				{
					ProcedureID   = $"{packageName}.{procedureName}",
					// input/output parameters in procedure have non-shared ordinals
					Ordinal       = direction == 1 ? ordinal + 1000 : ordinal,
					ParameterName = parameterName,
					Length        = length,
					Precision     = precision,
					Scale         = scale,
					IsIn          = direction == 0,
					IsOut         = direction == 1,
					IsResult      = direction == 2,
					IsNullable    = isNullable,
					Description   = description,
					DataType      = CreateTypeName(type, subType ?? 0, scale)
				};
			},
				sql).ToList();
		}

		private static string CreateTypeName(int type, int subType, int scale)
		{
			// translation table based on logic from
			// https://github.com/FirebirdSQL/NETProvider/blob/master/src/FirebirdSql.Data.FirebirdClient/Common/TypeHelper.cs
			return (type, subType, scale) switch
			{
				(37, _, _) or (38, _, _)                                            => "VARCHAR",
				(14, _, _) or (15, _, _) or (40, _, _) or (41, _, _)                => "CHAR",
				(7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, 2, _)
					or (7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, not 1 or 2, < 0) => "DECIMAL",
				(7 or 8 or 9 or 16 or 45 or 11 or 27 or 26, 1, _)                   => "NUMERIC",
				(7, not 1 or 2, >= 0)                                               => "SMALLINT",
				(8, not 1 or 2, >= 0)                                               => "INTEGER",
				(9 or 16 or 45, not 1 or 2, >= 0)                                   => "BIGINT",
				(10, _, _)                                                          => "FLOAT",
				(11 or 27, not 1 or 2, >= 0)                                        => "DOUBLE PRECISION",
				(261, 1, _)                                                         => "BLOB SUB_TYPE 1", // Text
				(261, not 1, _)                                                     => "BLOB",
				(35, _, _)                                                          => "TIMESTAMP",
				(13, _, _)                                                          => "TIME",
				(12, _, _)                                                          => "DATE",
				(23, _, _)                                                          => "BOOLEAN",
				(29 or 31, _, _)                                                    => "TIMESTAMP WITH TIME ZONE",
				(28 or 39, _, _)                                                    => "TIME WITH TIME ZONE",
				(24 or 25, _, _)                                                    => "DECFLOAT",
				(26, not 1 or 2, >= 0)                                              => "INT128",
				_                                                                   => "unknown"
			};
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
					SystemType           = systemType,
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
				// procedure XXX does not return any values
				if (ex.Message.Contains("SQL error code = -84")
					// SchemaOnly doesn't work for non-selectable procedures in FB
					|| ex.Message.Contains("is not selectable"))
					return null;
				throw;
			}
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dataTypes = base.GetDataTypes(dataConnection);

			var knownTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var dataType in dataTypes)
			{
				knownTypes.Add(dataType.TypeName);
				if (string.IsNullOrEmpty(dataType.CreateFormat) && !string.IsNullOrEmpty(dataType.CreateParameters))
				{
					dataType.CreateFormat =
						dataType.TypeName + "(" +
						string.Join(",", dataType.CreateParameters!.Split(',').Select((_,i) => FormattableString.Invariant($"{{{i}}}"))) +
						")";
				}
			}

			// provider doesn't add new FB4 types to DATATYPES schema API and older boolean type
			// https://github.com/FirebirdSQL/NETProvider/blob/master/Provider/src/FirebirdSql.Data.FirebirdClient/Schema/FbMetaData.xml
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

		protected override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType?.ToLowerInvariant() switch
			{
				"array"                    => DataType.VarBinary,
				"bigint"                   => DataType.Int64,
				"blob"                     => DataType.Blob,
				"boolean"                  => DataType.Boolean,
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
			switch (dataType?.ToLowerInvariant())
			{
				case "decfloat"                : return _provider.Adapter.FbDecFloatType?.Name;
				case "timestamp with time zone": return _provider.Adapter.FbZonedDateTimeType?.Name;
				case "time with time zone"     : return _provider.Adapter.FbZonedTimeType?.Name;
			}

			return base.GetProviderSpecificType(dataType);
		}

		protected override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			switch (dataType?.ToLowerInvariant())
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

		/// <summary>
		/// Builds table function call command.
		/// </summary>
		protected override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			commandText = "SELECT * FROM " + commandText;

			if (procedure.Parameters.Count > 0)
			{
				commandText += "(";

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					if (i != 0)
						commandText += ",";
					commandText += "NULL";
				}

				commandText += ")";
			}

			return commandText;
		}
	}
}
