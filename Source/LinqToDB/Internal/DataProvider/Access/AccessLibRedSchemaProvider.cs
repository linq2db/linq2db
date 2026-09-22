using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessLibRedSchemaProvider : AccessSchemaProviderBase
	{
		public AccessLibRedSchemaProvider()
		{
		}

		// Access has no catalogs or schemas, so the unqualified object name is the identity that joins
		// tables, columns, keys and procedures together.
		static string ID(DataRow row, string column) => row.Field<string>(column)!;

		// For a PARAMETERS clause Access itself wrote, the reported name keeps its [ ] quoting - the
		// declared [@id] comes back as "[@id]" where a query LibRed wrote reports "@id".
		static string Unquote(string name)
			=> name is ['[', .., ']'] ? name[1..^1] : name;

		// GetSchema("DataTypes") carries no CreateFormat, so the scaffolder could not spell a column type
		// back out; the rest of the row is already covered by AccessSchemaProviderBase.GetDataType.
		static readonly List<DataTypeInfo> _dataTypes =
		[
			new() { TypeName = "Bit",        DataType = "System.Boolean"                                                              },
			new() { TypeName = "Byte",       DataType = "System.Byte"                                                                 },
			new() { TypeName = "Short",      DataType = "System.Int16"                                                                },
			new() { TypeName = "Long",       DataType = "System.Int32"                                                                },
			new() { TypeName = "Single",     DataType = "System.Single"                                                               },
			new() { TypeName = "Double",     DataType = "System.Double"                                                               },
			new() { TypeName = "Currency",   DataType = "System.Decimal"                                                              },
			new() { TypeName = "Decimal",    DataType = "System.Decimal", CreateFormat = "Decimal({0}, {1})", CreateParameters = "precision,scale" },
			new() { TypeName = "DateTime",   DataType = "System.DateTime"                                                             },
			new() { TypeName = "GUID",       DataType = "System.Guid"                                                                 },
			// the CreateFormat casing follows the TypeName so a scaffolded column type reads as the OLE DB
			// flavour spells it - Access itself is case-insensitive here
			new() { TypeName = "Char",       DataType = "System.String",  CreateFormat = "Char({0})",         CreateParameters = "length" },
			new() { TypeName = "VarChar",    DataType = "System.String",  CreateFormat = "VarChar({0})",      CreateParameters = "length" },
			new() { TypeName = "LongText",   DataType = "System.String"                                                               },
			new() { TypeName = "Binary",     DataType = "System.Byte[]",  CreateFormat = "Binary({0})",       CreateParameters = "length" },
			new() { TypeName = "VarBinary",  DataType = "System.Byte[]",  CreateFormat = "VarBinary({0})",    CreateParameters = "length" },
			new() { TypeName = "LongBinary", DataType = "System.Byte[]"                                                               },
			new() { TypeName = "BigBinary",  DataType = "System.Byte[]"                                                               },
		];

		static readonly Dictionary<string,DataTypeInfo> _dataTypesByName =
			_dataTypes.ToDictionary(dt => dt.TypeName!, StringComparer.OrdinalIgnoreCase);

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection) => _dataTypes;

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			return
			(
				from t in dataConnection.OpenDbConnection().GetSchema("Tables").AsEnumerable()
				let name = ID(t, "TABLE_NAME")
				let type = t.Field<string>("TABLE_TYPE")
				select new TableInfo
				{
					TableID            = name,
					TableName          = name,
					IsDefaultSchema    = true,
					IsView             = string.Equals(type, "VIEW", StringComparison.Ordinal),
					// TABLE_TYPE marks the four catalog tables as SYSTEM TABLE but reports MSysAccessStorage
					// and the MSysNavPane* family as ordinary tables, where OLE DB calls them ACCESS TABLE.
					// MSys is Access's reserved prefix for system objects, so the name is the reliable test.
					IsProviderSpecific = string.Equals(type, "SYSTEM TABLE", StringComparison.Ordinal)
						|| name.StartsWith("MSys", StringComparison.OrdinalIgnoreCase),
					Description        = t.Field<string>("DESCRIPTION"),
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			return
			(
				from c in dataConnection.OpenDbConnection().GetSchema("Columns").AsEnumerable()
				let typeName = c.Field<string>("TYPE_NAME")
				let dt       = typeName != null && _dataTypesByName.TryGetValue(typeName, out var info) ? info : null
				let parms    = dt?.CreateParameters
				select new ColumnInfo
				{
					TableID     = ID(c, "TABLE_NAME"),
					Name        = c.Field<string>("COLUMN_NAME")!,
					Ordinal     = Converter.ChangeTypeTo<int>(c["ORDINAL_POSITION"]),
					DataType    = typeName,
					IsNullable  = c.Field<bool>("IS_NULLABLE"),
					IsIdentity  = c.Field<bool>("IS_AUTOINCREMENT"),
					// a length of 0 is reported for the unbounded types, and a precision for every numeric;
					// carrying either where the type cannot spell it would put it in the scaffolded type name
					Length      = parms?.Contains("length",    StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(c["CHARACTER_MAXIMUM_LENGTH"]) : null,
					Precision   = parms?.Contains("precision", StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(c["NUMERIC_PRECISION"])        : null,
					Scale       = parms?.Contains("scale",     StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(c["NUMERIC_SCALE"])            : null,
					Description = c.Field<string>("DESCRIPTION"),
				}
			).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return
			(
				from pk in dataConnection.OpenDbConnection().GetSchema("PrimaryKeys").AsEnumerable()
				select new PrimaryKeyInfo
				{
					TableID        = ID(pk, "TABLE_NAME"),
					PrimaryKeyName = pk.Field<string>("PK_NAME")!,
					ColumnName     = pk.Field<string>("COLUMN_NAME")!,
					Ordinal        = Converter.ChangeTypeTo<int>(pk["ORDINAL"]),
				}
			).ToList();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return
			(
				from fk in dataConnection.OpenDbConnection().GetSchema("ForeignKeys").AsEnumerable()
				select new ForeignKeyInfo
				{
					Name         = fk.Field<string>("FK_NAME")!,
					ThisTableID  = ID(fk, "FK_TABLE_NAME"),
					ThisColumn   = fk.Field<string>("FK_COLUMN_NAME")!,
					OtherTableID = ID(fk, "PK_TABLE_NAME"),
					OtherColumn  = fk.Field<string>("PK_COLUMN_NAME")!,
					Ordinal      = Converter.ChangeTypeTo<int>(fk["ORDINAL"]),
				}
			).ToList();
		}

		// Access stores queries rather than procedures: a row-returning one is reported as a view, and
		// everything else - parameterised selects included - arrives here.
		protected override List<ProcedureInfo>? GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			return
			(
				from p in dataConnection.OpenDbConnection().GetSchema("Procedures").AsEnumerable()
				select new ProcedureInfo
				{
					ProcedureID         = ID(p, "PROCEDURE_NAME"),
					ProcedureName       = ID(p, "PROCEDURE_NAME"),
					IsDefaultSchema     = true,
					ProcedureDefinition = p.Field<string>("PROCEDURE_DEFINITION"),
				}
			).ToList();
		}

		// Shaped after AccessOleDbSchemaProvider rather than the base, so the whole Access family reports a
		// procedure's result columns alike: the member type comes from the reader's own DataType while the
		// system type goes through GetSystemType. The base is also unusable here as-is - it reads an
		// "IsIdentity" column, which is not an ADO standard schema-table name; LibRed carries the standard
		// "IsAutoIncrement".
		protected override List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let columnName = r.Field<string>("ColumnName")
				let columnType = r.Field<string>("DataTypeName")
				let isNullable = r.Field<bool>  ("AllowDBNull")
				let systemType = r.Field<Type>  ("DataType")
				let length     = r.Field<int?>  ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let dt         = GetDataType(columnType, null, options)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = GetSystemType(columnType, null, dt, length, precision, scale, options) ?? systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.Field<bool>("IsAutoIncrement"),
				}
			).ToList();
		}

		protected override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection,
			IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			return
			(
				from p in dataConnection.OpenDbConnection().GetSchema("ProcedureParameters").AsEnumerable()
				let typeName = p.Field<string>("TYPE_NAME")
				let dt       = typeName != null && _dataTypesByName.TryGetValue(typeName, out var info) ? info : null
				let parms    = dt?.CreateParameters
				// the collection is not ordered by position, and Access binds by name anyway
				orderby ID(p, "PROCEDURE_NAME"), Converter.ChangeTypeTo<int>(p["ORDINAL_POSITION"])
				select new ProcedureParameterInfo
				{
					ProcedureID   = ID(p, "PROCEDURE_NAME"),
					ParameterName = Unquote(p.Field<string>("PARAMETER_NAME")!),
					Ordinal       = Converter.ChangeTypeTo<int>(p["ORDINAL_POSITION"]),
					DataType      = typeName,
					IsIn          = true,
					IsOut         = false,
					IsResult      = false,
					IsNullable    = p.Field<bool>("IS_NULLABLE"),
					Length        = parms?.Contains("length",    StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(p["CHARACTER_MAXIMUM_LENGTH"]) : null,
					Precision     = parms?.Contains("precision", StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(p["NUMERIC_PRECISION"])        : null,
					Scale         = parms?.Contains("scale",     StringComparison.Ordinal) == true ? Converter.ChangeTypeTo<int?>(p["NUMERIC_SCALE"])            : null,
					Description   = p.Field<string>("DESCRIPTION"),
				}
			).ToList();
		}
	}
}
