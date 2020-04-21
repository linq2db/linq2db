using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Access
{
	using System;
	using System.Data.Common;
	using System.Text.RegularExpressions;
	using Common;
	using Data;
	using SchemaProvider;

	class AccessOleDbSchemaProvider : AccessSchemaProviderBase
	{
		private readonly AccessOleDbDataProvider _provider;

		public AccessOleDbSchemaProvider(AccessOleDbDataProvider provider)
		{
			_provider = provider;
		}

		// see https://github.com/linq2db/linq2db.LINQPad/issues/10
		// we create separate connection for GetSchema calls to workaround provider bug
		// logic not applied if active transaction present - user must remove transaction if he has issues
		protected override TResult ExecuteOnNewConnection<TResult>(DataConnection dataConnection, Func<DataConnection, TResult> action)
		{
			if (dataConnection.Transaction != null)
				return action(dataConnection);
			else using (var newConnection = (DataConnection)dataConnection.Clone())
					return action(newConnection);
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			var connection = _provider.TryGetProviderConnection(dataConnection.Connection, dataConnection.MappingSchema);
			if (connection == null)
				return Array<ForeignKeyInfo>.Empty;

			// this method (GetOleDbSchemaTable) could crash application hard with AV:
			// https://github.com/linq2db/linq2db.LINQPad/issues/23
			// we cannot do anything about it as it is not exception you can handle without hacks (and it doesn't make sense anyways)
			var data = _provider.Adapter.GetOleDbSchemaTable(connection, OleDbProviderAdapter.OleDbSchemaGuid.Foreign_Keys, null);

			var q = from fk in data.AsEnumerable()
					select new ForeignKeyInfo
					{
						Name         = fk.Field<string>("FK_NAME"),
						ThisColumn   = fk.Field<string>("FK_COLUMN_NAME"),
						OtherColumn  = fk.Field<string>("PK_COLUMN_NAME"),
						ThisTableID  = fk.Field<string>("FK_TABLE_CATALOG") + "." + fk.Field<string>("FK_TABLE_SCHEMA") + "." + fk.Field<string>("FK_TABLE_NAME"),
						OtherTableID = fk.Field<string>("PK_TABLE_CATALOG") + "." + fk.Field<string>("PK_TABLE_SCHEMA") + "." + fk.Field<string>("PK_TABLE_NAME"),
						Ordinal      = ConvertTo<int>.From(fk.Field<long>("ORDINAL")),
					};

			return q.ToList();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Tables"));

			return
			(
				from t in tables.AsEnumerable()
				let catalog = t.Field<string>("TABLE_CATALOG") // empty
				let schema  = t.Field<string>("TABLE_SCHEMA") // empty
				let name    = t.Field<string>("TABLE_NAME") // object name
				// TABLE/VIEW/SYSTEM TABLE/ACCESS TABLE
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE" || t.Field<string>("TABLE_TYPE") == "ACCESS TABLE"
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
					CatalogName        = catalog,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = schema.IsNullOrEmpty(),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsProviderSpecific = system,
					Description        = t.Field<string>("DESCRIPTION")
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables)
		{
			var idxs = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Indexes"));

			return
			(
				from idx in idxs.AsEnumerable()
				where idx.Field<bool>("PRIMARY_KEY")
				select new PrimaryKeyInfo
				{
					TableID        = idx.Field<string>("TABLE_CATALOG") + "." + idx.Field<string>("TABLE_SCHEMA") + "." + idx.Field<string>("TABLE_NAME"),
					PrimaryKeyName = idx.Field<string>("INDEX_NAME"),
					ColumnName     = idx.Field<string>("COLUMN_NAME"),
					Ordinal        = ConvertTo<int>.From(idx["ORDINAL_POSITION"]),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var cs = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Columns"));

			return
			(
				from c in cs.AsEnumerable()
				let dt = GetDataTypeByProviderDbType(c.Field<int>("DATA_TYPE"), options)
				select new ColumnInfo
				{
					TableID     = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME"),
					IsNullable  = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = dt?.TypeName,
					Length      = Converter.ChangeTypeTo<long?>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision   = Converter.ChangeTypeTo<int?> (c["NUMERIC_PRECISION"]),
					Scale       = Converter.ChangeTypeTo<int?> (c["NUMERIC_SCALE"]),
					IsIdentity  = Converter.ChangeTypeTo<int>  (c["COLUMN_FLAGS"]) == 90 && (dt == null || dt.TypeName == null || (dt.TypeName.ToLower() != "boolean" && dt.TypeName.ToLower() != "bit")),
					Description = c.Field<string>("DESCRIPTION")
				}
			).ToList();
		}

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			var ps = ExecuteOnNewConnection(dataConnection, cn => ((DbConnection)cn.Connection).GetSchema("Procedures"));

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
					ProcedureDefinition = p.Field<string>("PROCEDURE_DEFINITION")
				}
			).ToList();
		}

		static readonly Regex _paramsExp = new Regex(@"PARAMETERS ((\[(?<name>[^\]]+)\]|(?<name>[^\s]+))\s(?<type>[^,;\s]+(\s\([^\)]+\))?)[,;]\s)*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures)
		{
			var list = new List<ProcedureParameterInfo>();

			foreach (var procedure in procedures)
			{
				var match      = _paramsExp.Match(procedure.ProcedureDefinition);
				var names      = match.Groups["name"].Captures;
				var types      = match.Groups["type"].Captures;
				var separators = new[] {' ', '(', ',', ')'};

				for (var i = 0; i < names.Count; ++i)
				{
					var   paramName = names[i].Value;
					var   rawType   = types[i].Value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					var   dataType  = rawType[0];
					long? size      = null;
					int?  precision = null;
					int?  scale     = null;

					if (rawType.Length > 2)
					{
						precision = ConvertTo<int?>.From(rawType[1]);
						scale     = ConvertTo<int?>.From(rawType[2]);
					}
					else if (rawType.Length > 1)
					{
						size      = ConvertTo<long?>.From(rawType[1]);
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
						IsNullable    = true
					});
				}
			}

			return list;
		}
	}
}
