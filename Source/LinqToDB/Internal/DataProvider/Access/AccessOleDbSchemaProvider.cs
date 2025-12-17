using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Access
{
	public partial class AccessOleDbSchemaProvider : AccessSchemaProviderBase
	{
		private readonly AccessDataProvider _provider;

		protected override bool GetProcedureSchemaExecutesProcedure => true;

		public AccessOleDbSchemaProvider(AccessDataProvider provider)
		{
			_provider = provider;
		}

		// see https://github.com/linq2db/linq2db.LINQPad/issues/10
		// we create separate connection for GetSchema calls to workaround provider bug
		// logic not applied if active transaction present - user must remove transaction if he has issues
		// also it could fail to work when context uses external connection instance
		private static TResult ExecuteOnNewConnection<TResult>(DataConnection dataConnection, Func<DataConnection, TResult> action)
		{
			if (dataConnection.Transaction != null)
				return action(dataConnection);

			using var newConnection = new DataConnection(dataConnection.Options);

			// user configured external connection: use it
			// there is no big value in support for such edge case
			if (newConnection.OpenDbConnection() == dataConnection.OpenDbConnection())
				return action(dataConnection);

			return action(newConnection);
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var connection = _provider.TryGetProviderConnection(dataConnection, dataConnection.OpenDbConnection());
			if (connection == null)
				return [];

			// this method (GetOleDbSchemaTable) could crash application hard with AV:
			// https://github.com/linq2db/linq2db.LINQPad/issues/23
			// we cannot do anything about it as it is not exception you can handle without hacks (and it doesn't make sense anyways)
			var data = _provider.Adapter.GetOleDbSchemaTable!(connection, OleDbProviderAdapter.OleDbSchemaGuid.Foreign_Keys, null);

			var q = from fk in data.AsEnumerable()
				select new ForeignKeyInfo
				{
					Name         = fk.Field<string>("FK_NAME")!,
					ThisColumn   = fk.Field<string>("FK_COLUMN_NAME")!,
					OtherColumn  = fk.Field<string>("PK_COLUMN_NAME")!,
					ThisTableID  = fk.Field<string>("FK_TABLE_CATALOG") + "." + fk.Field<string>("FK_TABLE_SCHEMA") + "." + fk.Field<string>("FK_TABLE_NAME"),
					OtherTableID = fk.Field<string>("PK_TABLE_CATALOG") + "." + fk.Field<string>("PK_TABLE_SCHEMA") + "." + fk.Field<string>("PK_TABLE_NAME"),
					Ordinal      = ConvertTo<int>.From(fk.Field<long>("ORDINAL")),
				};

			return q.ToList();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var tables = ExecuteOnNewConnection(dataConnection, cn => cn.OpenDbConnection().GetSchema("Tables"));

			return
			(
				from t in tables.AsEnumerable()
				let catalog = t.Field<string>("TABLE_CATALOG") // empty
				let schema  = t.Field<string>("TABLE_SCHEMA") // empty
				let name    = t.Field<string>("TABLE_NAME") // object name
				// TABLE/VIEW/SYSTEM TABLE/ACCESS TABLE
				let system  = t.Field<string>("TABLE_TYPE") is "SYSTEM TABLE" or "ACCESS TABLE"
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
					CatalogName        = catalog,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = string.IsNullOrEmpty(schema),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsProviderSpecific = system,
					Description        = t.Field<string>("DESCRIPTION"),
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			var idxs = ExecuteOnNewConnection(dataConnection, cn => cn.OpenDbConnection().GetSchema("Indexes"));

			return
			(
				from idx in idxs.AsEnumerable()
				where idx.Field<bool>("PRIMARY_KEY")
				select new PrimaryKeyInfo
				{
					TableID        = idx.Field<string>("TABLE_CATALOG") + "." + idx.Field<string>("TABLE_SCHEMA") + "." + idx.Field<string>("TABLE_NAME"),
					PrimaryKeyName = idx.Field<string>("INDEX_NAME")!,
					ColumnName     = idx.Field<string>("COLUMN_NAME")!,
					Ordinal        = ConvertTo<int>.From(idx["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = ExecuteOnNewConnection(dataConnection, cn => cn.OpenDbConnection().GetSchema("Columns"));

			return
			(
				from c in cs.AsEnumerable()
				let flags          = Converter.ChangeTypeTo<OleDbProviderAdapter.ColumnFlags>(c["COLUMN_FLAGS"])
				let providerDbType = CorrectDataTypeFromFlags(c.Field<int>("DATA_TYPE"), flags)
				let dt             = GetDataTypeByProviderDbType(providerDbType, options)
				select new ColumnInfo
				{
					TableID     = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME")!,
					IsNullable  = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = dt?.TypeName,
					Length      = dt?.CreateParameters != null && dt.CreateParameters.Contains("max length") ? Converter.ChangeTypeTo<int?>(c["CHARACTER_MAXIMUM_LENGTH"]) : null,
					Precision   = dt?.CreateParameters != null && dt.CreateParameters.Contains("precision")  ? Converter.ChangeTypeTo<int?>(c["NUMERIC_PRECISION"])        : null,
					Scale       = dt?.CreateParameters != null && dt.CreateParameters.Contains("scale")      ? Converter.ChangeTypeTo<int?>(c["NUMERIC_SCALE"])            : null,
					// ole db provider returns incorrect flags (reports INT NOT NULL columns as identity)
					// https://github.com/linq2db/linq2db/issues/3149
					IsIdentity  = false,
					Description = c.Field<string>("DESCRIPTION"),
				}
			).ToList();
		}

		private static int CorrectDataTypeFromFlags(int providerDbType, OleDbProviderAdapter.ColumnFlags flags)
		{
			switch (providerDbType)
			{
				case 130: // AdWChar
					if (flags.HasFlag(OleDbProviderAdapter.ColumnFlags.IsLong))
						return 203;
					else if (!flags.HasFlag(OleDbProviderAdapter.ColumnFlags.IsFixedLength))
						return 202;
					break;

				case 128: // AdBinary
					if (flags.HasFlag(OleDbProviderAdapter.ColumnFlags.IsLong))
						return 205;
					else if (!flags.HasFlag(OleDbProviderAdapter.ColumnFlags.IsFixedLength))
						return 204;

					break;
			}

			return providerDbType;
		}

		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			var ps = ExecuteOnNewConnection(dataConnection, cn => cn.OpenDbConnection().GetSchema("Procedures"));

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
					IsDefaultSchema     = string.IsNullOrEmpty(schema),
					ProcedureDefinition = p.Field<string>("PROCEDURE_DEFINITION"),
				}
			).ToList();
		}

		private const string ParametersPattern = /* lang=regex */ @"PARAMETERS ((\[(?<name>[^\]]+)\]|(?<name>[^\s]+))\s(?<type>[^,;\s]+(\s\([^\)]+\))?)[,;]\s)*";
#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(ParametersPattern, RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1)]
		private static partial Regex ParametersRegex();
#else
		static readonly Regex _paramsExp = new (ParametersPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(1));
		private static Regex ParametersRegex() => _paramsExp;
#endif

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			var list = new List<ProcedureParameterInfo>();

			foreach (var procedure in procedures)
			{
				if (procedure.ProcedureDefinition == null)
					continue;

				var match      = ParametersRegex().Match(procedure.ProcedureDefinition);
				var names      = match.Groups["name"].Captures;
				var types      = match.Groups["type"].Captures;
				var separators = new[] {' ', '(', ',', ')'};

				for (var i = 0; i < names.Count; ++i)
				{
					var  paramName = names[i].Value;
					var  rawType   = types[i].Value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					var  dataType  = rawType[0];
					int? size      = null;
					int? precision = null;
					int? scale     = null;

					if (rawType.Length > 2)
					{
						precision = ConvertTo<int?>.From(rawType[1]);
						scale     = ConvertTo<int?>.From(rawType[2]);
					}
					else if (rawType.Length > 1)
					{
						size      = ConvertTo<int?>.From(rawType[1]);
					}

					list.Add(new ProcedureParameterInfo
					{
						ProcedureID   = procedure.ProcedureID,
						ParameterName = paramName,
						IsIn          = true,
						IsOut         = false,
						Length        = size,
						Precision     = precision,
						Scale         = scale,
						Ordinal       = i + 1,
						IsResult      = false,
						DataType      = dataType,
						IsNullable    = true,
					});
				}
			}

			return list;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = ExecuteOnNewConnection(dataConnection, base.GetDataTypes);

			if (dts.All(dt => dt.ProviderDbType != 128))
			{
				dts.Add(new DataTypeInfo()
				{
					TypeName         = "BINARY",
					DataType         = typeof(byte[]).FullName!,
					CreateParameters = "max length",
					ProviderDbType   = 128,
				});
			}

			if (dts.All(dt => dt.ProviderDbType != 130))
			{
				dts.Add(new DataTypeInfo()
				{
					TypeName         = "CHAR",
					DataType         = typeof(string).FullName!,
					CreateParameters = "max length",
					ProviderDbType   = 130,
				});
			}

			dts = dts.AsEnumerable().Where(t => t.ProviderDbType != 204).ToList();
			dts.Add(new DataTypeInfo()
			{
				TypeName         = "VARBINARY",
				DataType         = typeof(byte[]).FullName!,
				CreateParameters = "max length",
				ProviderDbType   = 204,
			});

			return dts;
		}

		protected override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var dbType = columnType ?? dataType?.TypeName;

			if (dataType != null)
			{
				var parms = dataType.CreateParameters;

				if (!string.IsNullOrWhiteSpace(parms))
				{
					var paramNames  = parms!.Split(',');
					var paramValues = new object?[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim().ToLowerInvariant())
						{
							case "max length": paramValues[i] = length;    break;
							case "precision" : paramValues[i] = precision; break;
							case "scale"     : paramValues[i] = scale;     break;
						}
					}

					if (paramValues.All(v => v != null))
					{
						var format = $"{dbType}({string.Join(", ", Enumerable.Range(0, paramValues.Length).Select(i => string.Create(CultureInfo.InvariantCulture, $"{{{i}}}")))})";
						dbType     = string.Format(CultureInfo.InvariantCulture, format, paramValues);
					}
				}
			}

			return dbType;
		}

		protected override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			// KeyInfo used, as SchemaOnly doesn't return schema
			// required GetProcedureSchemaExecutesProcedure = true
			using var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.KeyInfo, parameters);
			return rd.Reader!.GetSchemaTable();
		}

		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let columnName   = r.Field<string>("ColumnName")
				let dt           = GetDataTypeByProviderDbType(r.Field<int>("ProviderType"), options)
				let length       = r.Field<int?>("ColumnSize")
				let precision    = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale        = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let isNullable   = r.Field<bool>("AllowDBNull")
				let systemType   = r.Field<Type>("DataType")
				let columnType   = GetDbType(options, null, dt, length, precision, scale, null, null, null)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = columnType,
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = GetSystemType(columnType, null, dt, length, precision, scale, options) ?? systemType,
					DataType             = GetDataType(dt?.TypeName, columnType, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.Field<bool>("IsAutoIncrement"),
				}
			).ToList();
		}
	}
}
