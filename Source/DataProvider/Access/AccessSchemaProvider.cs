﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;
	using SchemaProvider;
	using System.Data.OleDb;

	class AccessSchemaProvider : SchemaProviderBase
	{
		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			var name = base.GetDatabaseName(dbConnection);

			if (name.IsNullOrEmpty())
				name = Path.GetFileNameWithoutExtension(GetDataSourceName(dbConnection));

			return name;
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = base.GetDataTypes(dataConnection);

			if (dts.All(dt => dt.ProviderDbType != 128))
			{
				dts.Add(new DataTypeInfo
				{
					TypeName         = "image",
					DataType         = typeof(byte[]).FullName,
					CreateFormat     = "image({0})",
					CreateParameters = "length",
					ProviderDbType   = 128
				});
			}

			if (dts.All(dt => dt.ProviderDbType != 130))
			{
				dts.Add(new DataTypeInfo
				{
					TypeName         = "text",
					DataType         = typeof(string).FullName,
					CreateFormat     = "text({0})",
					CreateParameters = "length",
					ProviderDbType   = 130
				});
			}

			return dts;
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
#if !NETSTANDARD
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where new[] {"TABLE", "VIEW"}.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				let system  = t.Field<string>("TABLE_TYPE") == "SYSTEM TABLE" || t.Field<string>("TABLE_TYPE") == "ACCESS TABLE"
				select new TableInfo
				{
					TableID            = catalog + '.' + schema + '.' + name,
					CatalogName        = catalog,
					SchemaName         = schema,
					TableName          = name,
					IsDefaultSchema    = schema.IsNullOrEmpty(),
					IsView             = t.Field<string>("TABLE_TYPE") == "VIEW",
					IsProviderSpecific = system
				}
			).ToList();
#else
			return new List<TableInfo>();
#endif
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
#if !NETSTANDARD
			var idxs = ((DbConnection)dataConnection.Connection).GetSchema("Indexes");

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
#else
			return new List<PrimaryKeyInfo>();
#endif
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
#if !NETSTANDARD
			var cs = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return
			(
				from c in cs.AsEnumerable()
				join dt in DataTypes on c.Field<int>("DATA_TYPE") equals dt.ProviderDbType
				//into gdt
				//from dt in gdt.DefaultIfEmpty()
				select new ColumnInfo
				{
					TableID    = c.Field<string>("TABLE_CATALOG") + "." + c.Field<string>("TABLE_SCHEMA") + "." + c.Field<string>("TABLE_NAME"),
					Name       = c.Field<string>("COLUMN_NAME"),
					IsNullable = c.Field<bool>  ("IS_NULLABLE"),
					Ordinal    = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType   = dt != null ? dt.TypeName : null,
					Length     = Converter.ChangeTypeTo<long?>(c["CHARACTER_MAXIMUM_LENGTH"]),
					Precision  = Converter.ChangeTypeTo<int?> (c["NUMERIC_PRECISION"]),
					Scale      = Converter.ChangeTypeTo<int?> (c["NUMERIC_SCALE"]),
					IsIdentity = Converter.ChangeTypeTo<int>  (c["COLUMN_FLAGS"]) == 90 && (dt == null || dt.TypeName == null || dt.TypeName.ToLower() != "boolean"),
				}
			).ToList();
#else 
			return new List<ColumnInfo>();
#endif
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var data = ((OleDbConnection)dataConnection.Connection)
				.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new object[] { null, null });

			var q = from fk in data.AsEnumerable()
					select new ForeingKeyInfo
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

		protected override string GetProviderSpecificTypeNamespace()
		{
			return null;
		}

		List<ProcedureInfo> _procedures;

		protected override List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
#if !NETSTANDARD
			var ps = ((DbConnection)dataConnection.Connection).GetSchema("Procedures");

			return _procedures =
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
#else
			return new List<ProcedureInfo>();
#endif
		}

		static Regex _paramsExp;

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			var list = new List<ProcedureParameterInfo>();

			foreach (var procedure in _procedures)
			{
				if (_paramsExp == null)
					_paramsExp = new Regex(@"PARAMETERS ((\[(?<name>[^\]]+)\]|(?<name>[^\s]+))\s(?<type>[^,;\s]+(\s\([^\)]+\))?)[,;]\s)*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
						DataType      = dataType
					});
				}
			}

			return list;
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (dataTypeInfo == null)
			{
				switch (dataType.ToLower())
				{
					case "text" : return typeof(string);
					default     : throw new InvalidOperationException();
				}
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}

		protected override DataType GetDataType(string dataType, string columnType, long? length, int? prec, int? scale)
		{
			switch (dataType.ToLower())
			{
				case "short"      : return DataType.Int16;
				case "long"       : return DataType.Int32;
				case "single"     : return DataType.Single;
				case "double"     : return DataType.Double;
				case "currency"   : return DataType.Money;
				case "datetime"   : return DataType.DateTime;
				case "bit"        : return DataType.Boolean;
				case "byte"       : return DataType.Byte;
				case "guid"       : return DataType.Guid;
				case "bigbinary"  : return DataType.Binary;
				case "longbinary" : return DataType.Binary;
				case "varbinary"  : return DataType.VarBinary;
				case "text"       : return DataType.NText;
				case "longtext"   : return DataType.NText;
				case "varchar"    : return DataType.VarChar;
				case "decimal"    : return DataType.Decimal;
			}

			return DataType.Undefined;
		}
	}
}
