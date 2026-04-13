using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerSchemaProvider(SqlServerDataProvider provider) : SchemaProviderBase
	{
		private int  _compatibilityLevel;

		private readonly SqlServerDataProvider _provider = provider;

		protected sealed override void InitProvider(DataConnection dataConnection, GetSchemaOptions options)
		{
			_compatibilityLevel = dataConnection.Execute<int>("SELECT compatibility_level FROM sys.databases WHERE name = db_name()");
		}

		protected sealed override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			var excludeTemporal = _compatibilityLevel >= 130 && options.IgnoreSystemHistoryTables;

			return (
					from o in dataConnection.GetTable<Object>()
					where !o.IsMsShipped
					where o.Type.In("U", "V")
					let databaseName = SqlFn.DbName()
					let s = o.Schema
					let t = o.Table
					where excludeTemporal ? t == null || t.TemporalType != 1 : true
					where !dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == o.ObjectId)
						.Where(ep => ep.MinorId == 0)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "microsoft_database_tools_support")
						.Any()
					from ep in dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == o.ObjectId)
						.Where(ep => ep.MinorId == 0)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "MS_Description")
						.DefaultIfEmpty()
					select new TableInfo
					{
						TableID         = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + s.Name + "." + o.Name),
						CatalogName     = databaseName,
						SchemaName      = s.Name,
						TableName       = o.Name,
						IsView          = Sql.ToSql(o.Type == "V"),
						Description     = SqlFn.IsNull(Sql.Convert<string, object?>(ep.Value), ""),
						IsDefaultSchema = Sql.ToSql(s.Name == "dbo"),
					}
				)
				.ToList();
		}

		protected sealed override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(
			DataConnection           dataConnection,
			IEnumerable<TableSchema> tables,
			GetSchemaOptions         options
		)
		{
			return (
					from kc in dataConnection.GetTable<KeyConstraint>()
					where !kc.IsMsShipped
					where kc.Type == "PK"
					let databaseName = SqlFn.DbName()
					let s = kc.Schema
					let t = kc.ParentTable
					where !dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == t.ObjectId)
						.Where(ep => ep.MinorId == 0)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "microsoft_database_tools_support")
						.Any()
					from ic in kc.IndexColumns
					select new PrimaryKeyInfo
					{
						TableID        = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + s.Name + "." + t.Name),
						PrimaryKeyName = kc.Name,
						ColumnName     = ic.Column.Name,
						Ordinal        = ic.KeyOrdinal,
					}
				)
				.ToList();
		}

		protected sealed override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			var checkTemporal = _compatibilityLevel >= 130;

			return (
					from c in dataConnection.GetTable<Column>()
					let o = c.Object
					let s = o.Schema
					let databaseName = SqlFn.DbName()
					where !dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == c.ObjectId)
						.Where(ep => ep.MinorId == 0)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "microsoft_database_tools_support")
						.Any()
					from ep in dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == c.ObjectId)
						.Where(ep => ep.MinorId == c.ColumnId)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "MS_Description")
						.DefaultIfEmpty()
					let isComputed = c.IsComputed
									 || (
										 checkTemporal
										 && (
											 c.GeneratedAlwaysType != 0
											 || (c.Table != null && c.Table!.TemporalType == 1)
										 )
									 )
					select new ColumnInfo
					{
						TableID    = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + s.Name + "." + o.Name),
						Name       = c.Name,
						IsNullable = c.IsNullable,
						Ordinal    = SqlFn.ColumnProperty(c.ObjectId, c.Name, SqlFn.ColumnPropertyName.Ordinal)!.Value,
						DataType   = SqlFn.IsNull(SqlFn.TypeName(c.UserTypeId == 255 ? c.UserTypeId : c.SystemTypeId), c.Type.Name),
						Length     = SqlFn.ColumnProperty(c.ObjectId, c.Name, SqlFn.ColumnPropertyName.CharMaxLen),
						Precision =
							c.SystemTypeId.In(48, 52, 56, 59, 60, 62, 106, 108, 122, 127) ? c.Precision :
							c.SystemTypeId.In(40, 41, 42, 43, 58, 61)                     ? OdbcScale(c.SystemTypeId, c.Scale) :
																							null,
						Scale =
							c.SystemTypeId.In(40, 41, 42, 43, 58, 61) ? null :
								OdbcScale(c.SystemTypeId, c.Scale),
						Description  = SqlFn.IsNull(Sql.Convert<string, object?>(ep.Value), ""),
						IsIdentity   = c.IsIdentity,
						SkipOnInsert = isComputed,
						SkipOnUpdate = isComputed,
					}
				)
				.AsEnumerable()
				.Select(c =>
				{
					var dti = GetDataType(c.DataType, null, options);

					if (dti != null)
					{
						switch (dti.CreateParameters)
						{
							case null:
								c.Length    = null;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "scale":
								c.Length = null;

								if (c.Scale.HasValue)
									c.Precision = null;

								break;

							case "precision,scale":
								c.Length = null;
								break;

							case "max length":
								if (c.Length < 0)
									c.Length = int.MaxValue;
								c.Precision = null;
								c.Scale     = null;
								break;

							case "length":
								c.Precision = null;
								c.Scale     = null;
								break;

							case "number of bits used to store the mantissa":
								break;

							default:
								break;
						}
					}

					switch (c.DataType)
					{
						case "geometry":
						case "geography":
						case "hierarchyid":
						case "float":
							c.Length    = null;
							c.Precision = null;
							c.Scale     = null;
							break;
						case "vector":
							// Convert binary vector storage size (8-byte header + 4 bytes per float element) to logical dimension count
							c.Length = (c.Length - 8) / 4;
							break;
					}

					return c;
				})
				.ToList();
		}

		protected sealed override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection,
			IEnumerable<TableSchema>                                                                tables, GetSchemaOptions options)
		{
			return (
					from fkc in dataConnection.GetTable<ForeignKeyColumn>()
					let databaseName = SqlFn.DbName()
					select new ForeignKeyInfo
					{
						Name         = fkc.ForeignKey.Name,
						Ordinal      = fkc.ConstraintColumnId,
						ThisTableID  = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + fkc.ThisTable.Schema.Name + "." + fkc.ThisTable.Name),
						ThisColumn   = fkc.ThisColumn.Name,
						OtherTableID = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + fkc.OtherTable.Schema.Name + "." + fkc.OtherTable.Name),
						OtherColumn  = fkc.OtherColumn.Name,
					}
				)
				.ToList();
		}

		protected sealed override List<ProcedureInfo> GetProcedures(DataConnection dataConnection, GetSchemaOptions options)
		{
			return (
					from o in dataConnection.GetTable<Object>()
					where !o.IsMsShipped
					where o.Type.In("P", "FN", "TF", "IF", "AF", "FT", "IS", "PC", "FS")
					let databaseName = SqlFn.DbName()
					from ep in dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == o.ObjectId)
						.Where(ep => ep.MinorId == 0)
						.Where(ep => ep.Class   == 1)
						.Where(ep => ep.Name    == "MS_Description")
						.DefaultIfEmpty()
					select new ProcedureInfo
					{
						ProcedureID         = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + o.Schema.Name + "." + o.Name),
						CatalogName         = databaseName,
						SchemaName          = o.Schema.Name,
						ProcedureName       = o.Name,
						Description         = SqlFn.IsNull(Sql.Convert<string, object?>(ep.Value), ""),
						IsFunction          = !o.Type.In("P", "PC"),
						IsTableFunction     = o.Type.In("TF", "IF", "FT"),
						IsAggregateFunction = o.Type.In("AF"),
						IsDefaultSchema     = Sql.ToSql(o.Schema.Name == "dbo"),
					}
				)
				.ToList();
		}

		[SuppressMessage("Style", "IDE0078:Use pattern matching", Justification = "False Positive")]
		protected sealed override List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options)
		{
			// TODO: RECHECK:
			// SQL25 CTP2.1 returns vector parameter type as varbinary(len), e.g. for vector(3) : varbinary(20)
			// and sp_describe_first_result_set returns ntext
			return (
					from p in dataConnection.GetTable<Parameter>()
					let o = p.Object
					where !o.IsMsShipped
					where o.Type.In("P", "FN", "TF", "IF", "AF", "FT", "IS", "PC", "FS")
					let t = p.Type
					let databaseName = SqlFn.DbName()
					from ep in dataConnection.GetTable<ExtendedProperty>()
						.Where(ep => ep.MajorId == p.ObjectId)
						.Where(ep => ep.MinorId == p.ParameterId)
						.Where(ep => ep.Class   == 2)
						.Where(ep => ep.Name    == "MS_Description")
						.DefaultIfEmpty()
					select new ProcedureParameterInfo()
					{
						ProcedureID   = Sql.ToSql(SqlFn.Collate(databaseName, "DATABASE_DEFAULT") + "." + o.Schema.Name + "." + o.Name),
						Ordinal       = p.ParameterId,
						ParameterName = p.Name,
						DataType      = SqlFn.IsNull(SqlFn.TypeName(p.UserTypeId == 255 ? p.UserTypeId : p.SystemTypeId), p.Type.Name),
						Length        = SqlFn.ColumnProperty(p.ObjectId, p.Name, SqlFn.ColumnPropertyName.CharMaxLen),
						Precision =
							p.SystemTypeId.In(48, 52, 56, 59, 60, 62, 106, 108, 122, 127) ? p.Precision :
							p.SystemTypeId.In(40, 41, 42, 43, 58, 61)                     ? OdbcScale(p.SystemTypeId, p.Scale) :
																							null,
						Scale =
							p.SystemTypeId.In(40, 41, 42, 43, 58, 61) ? null :
								OdbcScale(p.SystemTypeId, p.Scale),
						IsIn        = !(p.ParameterId == 0 || p.IsOutput),
						IsOut       = p.ParameterId == 0 || p.IsOutput,
						IsResult    = p.ParameterId == 0,
						UDTCatalog  = t.SchemaId != 4 ? databaseName : null,
						UDTSchema   = t.SchemaId != 4 ? t.Schema.Name : null,
						UDTName     = t.SchemaId != 4 ? t.Name : null,
						IsNullable  = true,
						Description = SqlFn.IsNull(Sql.Convert<string, object?>(ep.Value), ""),
					}
				)
				.ToList();
		}

		protected sealed override DataType GetDataType(string? dataType, string? columnType, int? length, int? precision, int? scale)
		{
			return dataType switch
			{
				"json"             => DataType.Json,
				"vector"           => DataType.Vector32,
				"image"            => DataType.Image,
				"text"             => DataType.Text,
				"binary"           => DataType.Binary,
				"tinyint"          => DataType.Byte,
				"date"             => DataType.Date,
				"time"             => DataType.Time,
				"bit"              => DataType.Boolean,
				"smallint"         => DataType.Int16,
				"decimal"          => DataType.Decimal,
				"int"              => DataType.Int32,
				"smalldatetime"    => DataType.SmallDateTime,
				"real"             => DataType.Single,
				"money"            => DataType.Money,
				"datetime"         => DataType.DateTime,
				"float"            => DataType.Double,
				"numeric"          => DataType.Decimal,
				"smallmoney"       => DataType.SmallMoney,
				"datetime2"        => DataType.DateTime2,
				"bigint"           => DataType.Int64,
				"varbinary"        => DataType.VarBinary,
				"timestamp"        => DataType.Timestamp,
				"sysname"          => DataType.NVarChar,
				"nvarchar"         => DataType.NVarChar,
				"varchar"          => DataType.VarChar,
				"ntext"            => DataType.NText,
				"uniqueidentifier" => DataType.Guid,
				"datetimeoffset"   => DataType.DateTimeOffset,
				"sql_variant"      => DataType.Variant,
				"xml"              => DataType.Xml,
				"char"             => DataType.Char,
				"nchar"            => DataType.NChar,
				"hierarchyid" or
					"geography" or
					"geometry" => DataType.Udt,
				"table type" => DataType.Structured,
				_            => DataType.Undefined,
			};
		}

		// TODO: we should support multiple namespaces, as e.g. sql server also could have
		// spatial types (which is handled by T4 template for now)
		protected sealed override string GetProviderSpecificTypeNamespace() => SqlTypes.TypesNamespace;

		protected sealed override string? GetProviderSpecificType(string? dataType)
		{
			return dataType switch
			{
				"varbinary" or
					"timestamp" or
					"rowversion" or
					"image" or
					"binary" => nameof(SqlBinary),
				"tinyint" => nameof(SqlByte),
				"date" or
					"smalldatetime" or
					"datetime" or
					"datetime2" => nameof(SqlDateTime),
				"bit"      => nameof(SqlBoolean),
				"smallint" => nameof(SqlInt16),
				"numeric" or
					"decimal" => nameof(SqlDecimal),
				"int"   => nameof(SqlInt32),
				"real"  => nameof(SqlSingle),
				"float" => nameof(SqlDouble),
				"smallmoney" or
					"money" => nameof(SqlMoney),
				"bigint" => nameof(SqlInt64),
				"text" or
					"nvarchar" or
					"char" or
					"nchar" or
					"varchar" or
					"ntext" => nameof(SqlString),
				"uniqueidentifier" => nameof(SqlGuid),
				"xml"              => nameof(SqlXml),
				"hierarchyid"      => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlHierarchyIdType}",
				"geography"        => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeographyType}",
				"geometry"         => $"{SqlServerTypes.TypesNamespace}.{SqlServerTypes.SqlGeometryType}",
				"json"             => $"{SqlServerProviderAdapter.TypesNamespace}.SqlJson",
				"vector"           => $"{SqlServerProviderAdapter.TypesNamespace}.SqlVector<float>",
				_                  => base.GetProviderSpecificType(dataType),
			};
		}

		protected sealed override Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			return dataType switch
			{
				"json"    => (options.PreferProviderSpecificTypes ? _provider.Adapter.SqlJsonType : null)   ?? typeof(string),
				"vector"  => (options.PreferProviderSpecificTypes ? _provider.Adapter.SqlVectorType : null) ?? typeof(float[]),
				"tinyint" => typeof(byte),
				"hierarchyid" or
					"geography" or
					"geometry" => _provider.GetUdtTypeByName(dataType),
				"table type" => typeof(DataTable),
				_            => base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale, options),
			};
		}

		protected sealed override string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			// database name for udt not supported by sql server
			if (udtName != null)
				return (udtSchema != null ? SqlServerTools.QuoteIdentifier(udtSchema) + '.' : null) + SqlServerTools.QuoteIdentifier(udtName);

			return base.GetDbType(options, columnType, dataType, length, precision, scale, udtCatalog, udtSchema, udtName);
		}

		protected sealed override DataParameter BuildProcedureParameter(ParameterSchema p)
		{
			return p.DataType switch
			{
				DataType.Structured => new DataParameter
				{
					Name     = p.ParameterName,
					DataType = p.DataType,
					Direction =
						(p.IsIn, p.IsOut) switch
						{
							(true, true) => ParameterDirection.InputOutput,
							(true, _)    => ParameterDirection.Input,
							_            => ParameterDirection.Output,
						},
					DbType = p.SchemaType,
				},

				_ => base.BuildProcedureParameter(p),
			};
		}

		protected sealed override string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			var sql = base.BuildTableFunctionLoadTableSchemaCommand(procedure, commandText);

			// TODO: refactor method to use query as parameter instead of manual escaping...
			// https://github.com/linq2db/linq2db/issues/1921
			if (_compatibilityLevel >= 140)
				sql = $"EXEC('{sql.Replace("'", "''", StringComparison.Ordinal)}')";

			return sql;
		}

		protected sealed override DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			switch (dataConnection.DataProvider.Name)
			{
				case ProviderName.SqlServer2005:
				case ProviderName.SqlServer2008:
					return CallBase();
			}

			if (options.UseSchemaOnly || commandType == CommandType.Text)
				return CallBase();

			try
			{
				var tsql  = $"exec {commandText} {string.Join(", ", parameters.Select(p => p.Name))}";
				var parms = string.Join(", ", parameters.Select(p => $"{p.Name} {p.DbType}"));

				var dt = new DataTable();

				dt.Columns.AddRange(new[]
				{
					new DataColumn { ColumnName = "DataTypeName",     DataType = typeof(string) },
					new DataColumn { ColumnName = "ColumnName",       DataType = typeof(string) },
					new DataColumn { ColumnName = "AllowDBNull",      DataType = typeof(bool)   },
					new DataColumn { ColumnName = "ColumnSize",       DataType = typeof(int)    },
					new DataColumn { ColumnName = "NumericPrecision", DataType = typeof(int)    },
					new DataColumn { ColumnName = "NumericScale",     DataType = typeof(int)    },
					new DataColumn { ColumnName = "IsIdentity",       DataType = typeof(bool)   },
				});  

				foreach (var item in dataConnection
							 .QueryProc(
								 new
								 {
									 name               = "",
									 is_nullable        = false,
									 system_type_name   = "",
									 max_length         = 0,
									 precision          = 0,
									 scale              = 0,
									 is_identity_column = false,
								 },
								 "sp_describe_first_result_set",
								 new DataParameter("tsql",   tsql),
								 new DataParameter("params", parms)
							 )
						)
				{
					var row = dt.NewRow();

					row["DataTypeName"] = item.system_type_name.Split('(')[0];
					row["ColumnName"] = item.name ?? "";
					row["AllowDBNull"] = item.is_nullable;
					row["ColumnSize"] = item.system_type_name.Contains("nchar", StringComparison.Ordinal) || item.system_type_name.Contains("nvarchar", StringComparison.Ordinal) ? item.max_length / 2 : item.max_length;
					row["NumericPrecision"] = item.precision;
					row["NumericScale"] = item.scale;
					row["IsIdentity"] = item.is_identity_column;

					dt.Rows.Add(row);
				}

				return dt.Rows.Count == 0 ? null : dt;
			}
			catch
			{
				return CallBase();
			}

			DataTable? CallBase()
			{
				return base.GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);
			}
		}

		protected sealed override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var list = new List<DataTypeInfo>()
			{
				// Initial table is hard-coded in Microsoft.Data.SqlClient (https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Resources/Microsoft.Data.SqlClient.SqlMetaData.xml)
				// System.Data.SqlClient table looks equal
				new() { TypeName = "smallint",         DataType = "System.Int16",          ProviderDbType = 16, CreateFormat = "smallint",            CreateParameters = null },
				new() { TypeName = "int",              DataType = "System.Int32",          ProviderDbType = 8,  CreateFormat = "int",                 CreateParameters = null },
				new() { TypeName = "real",             DataType = "System.Single",         ProviderDbType = 13, CreateFormat = "real",                CreateParameters = null },
				new() { TypeName = "float",            DataType = "System.Double",         ProviderDbType = 6,  CreateFormat = "float({0})",          CreateParameters = "number of bits used to store the mantissa" },
				new() { TypeName = "money",            DataType = "System.Decimal",        ProviderDbType = 9,  CreateFormat = "money",               CreateParameters = null },
				new() { TypeName = "smallmoney",       DataType = "System.Decimal",        ProviderDbType = 17, CreateFormat = "smallmoney",          CreateParameters = null },
				new() { TypeName = "bit",              DataType = "System.Boolean",        ProviderDbType = 2,  CreateFormat = "bit",                 CreateParameters = null },
				new() { TypeName = "tinyint",          DataType = "System.Byte",           ProviderDbType = 20, CreateFormat = "tinyint",             CreateParameters = null },
				new() { TypeName = "bigint",           DataType = "System.Int64",          ProviderDbType = 0,  CreateFormat = "bigint",              CreateParameters = null },
				new() { TypeName = "timestamp",        DataType = "System.Byte[]",         ProviderDbType = 19, CreateFormat = "timestamp",           CreateParameters = null },
				new() { TypeName = "binary",           DataType = "System.Byte[]",         ProviderDbType = 1,  CreateFormat = "binary({0})",         CreateParameters = "length" },
				new() { TypeName = "image",            DataType = "System.Byte[]",         ProviderDbType = 7,  CreateFormat = "image",               CreateParameters = null },
				new() { TypeName = "text",             DataType = "System.String",         ProviderDbType = 18, CreateFormat = "text",                CreateParameters = null },
				new() { TypeName = "ntext",            DataType = "System.String",         ProviderDbType = 11, CreateFormat = "ntext",               CreateParameters = null },
				new() { TypeName = "decimal",          DataType = "System.Decimal",        ProviderDbType = 5,  CreateFormat = "decimal({0}, {1})",   CreateParameters = "precision,scale" },
				new() { TypeName = "numeric",          DataType = "System.Decimal",        ProviderDbType = 5,  CreateFormat = "numeric({0}, {1})",   CreateParameters = "precision,scale" },
				new() { TypeName = "datetime",         DataType = "System.DateTime",       ProviderDbType = 4,  CreateFormat = "datetime",            CreateParameters = null },
				new() { TypeName = "smalldatetime",    DataType = "System.DateTime",       ProviderDbType = 15, CreateFormat = "smalldatetime",       CreateParameters = null },
				new() { TypeName = "sql_variant",      DataType = "System.Object",         ProviderDbType = 23, CreateFormat = "sql_variant",         CreateParameters = null },
				new() { TypeName = "xml",              DataType = "System.String",         ProviderDbType = 25, CreateFormat = "xml",                 CreateParameters = null },
				new() { TypeName = "varchar",          DataType = "System.String",         ProviderDbType = 22, CreateFormat = "varchar({0})",        CreateParameters = "max length" },
				new() { TypeName = "char",             DataType = "System.String",         ProviderDbType = 3,  CreateFormat = "char({0})",           CreateParameters = null },
				new() { TypeName = "nchar",            DataType = "System.String",         ProviderDbType = 10, CreateFormat = "nchar({0})",          CreateParameters = null },
				new() { TypeName = "nvarchar",         DataType = "System.String",         ProviderDbType = 12, CreateFormat = "nvarchar({0})",       CreateParameters = "max length" },
				new() { TypeName = "varbinary",        DataType = "System.Byte[]",         ProviderDbType = 21, CreateFormat = "varbinary({0})",      CreateParameters = "max length" },
				new() { TypeName = "uniqueidentifier", DataType = "System.Guid",           ProviderDbType = 14, CreateFormat = "uniqueidentifier",    CreateParameters = null },
				new() { TypeName = "date",             DataType = "System.DateTime",       ProviderDbType = 31, CreateFormat = "date",                CreateParameters = null },
				new() { TypeName = "time",             DataType = "System.TimeSpan",       ProviderDbType = 32, CreateFormat = "time({0})",           CreateParameters = "scale" },
				new() { TypeName = "datetime2",        DataType = "System.DateTime",       ProviderDbType = 33, CreateFormat = "datetime2({0})",      CreateParameters = "scale" },
				new() { TypeName = "datetimeoffset",   DataType = "System.DateTimeOffset", ProviderDbType = 34, CreateFormat = "datetimeoffset({0})", CreateParameters = "scale" },
			};

			// user defined types

			list.AddRange(
				(
					from a in dataConnection.GetTable<Assembly>()
					from at in a.AssemblyTypes
					select new
					{
						at.AssemblyClass,
						a.Name,
						VersionMajor    = Sql.Convert<string?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.VersionMajor)),
						VersionMinor    = Sql.Convert<string?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.VersionMinor)),
						VersionBuild    = Sql.Convert<string?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.VersionBuild)),
						VersionRevision = Sql.Convert<string?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.VersionRevision)),
						CultureInfo     = Sql.Convert<string?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.CultureInfo)),
						PublicKey       = Sql.Convert<byte[]?, object?>(SqlFn.AssemblyProperty(a.Name, SqlFn.AssemblyPropertyName.PublicKey)),
					}
				)
					.AsEnumerable()
					.Select(x => new DataTypeInfo()
					{
						TypeName =
							$"{x.AssemblyClass}, {x.Name}, Version={x.VersionMajor}.{x.VersionMinor}.{x.VersionBuild}.{x.VersionRevision}"
							+ (string.IsNullOrWhiteSpace(x.CultureInfo) ? "" : $", Culture={x.CultureInfo}")
#if NET9_0_OR_GREATER
							+ (x.PublicKey == null ? "" : $", PublicKeyToken={Convert.ToHexStringLower(x.PublicKey)}")
#elif NET8_0_OR_GREATER
							+ (x.PublicKey == null ? "" : $", PublicKeyToken={Convert.ToHexString(x.PublicKey).ToLowerInvariant()}")
#else
							+ (x.PublicKey == null ? "" : $", PublicKeyToken={BitConverter.ToString(x.PublicKey).Replace("-", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant()}")
#endif
						,
						ProviderDbType = (int)SqlDbType.Udt,
					})
			);

			// table-valued parameters
			list.AddRange(
				dataConnection.GetTable<SqlDataType>()
					.Where(t => t.IsTableType)
					.Select(t => new DataTypeInfo
					{
						TypeName       = t.Name,
						ProviderDbType = (int)SqlDbType.Structured,
					})
			);

			if (list.TrueForAll(t => !string.Equals(t.DataType, "json", StringComparison.Ordinal)))
			{
				var type = _provider.Adapter.SqlJsonType ?? typeof(string);

				list.Add(new DataTypeInfo
				{
					TypeName         = "json",
					DataType         = type.FullName!,
					ProviderSpecific = _provider.Adapter.SqlJsonType is not null,
					ProviderDbType   = 35,
				});
			}

			if (list.TrueForAll(t => !string.Equals(t.DataType, "vector", StringComparison.Ordinal)))
			{
				var type = _provider.Adapter.SqlVectorType ?? typeof(float[]);

				list.Add(new DataTypeInfo
				{
					TypeName         = "vector",
					DataType         = type.FullName!,
					CreateFormat     = "vector({0})",
					CreateParameters = "length",
					ProviderSpecific = true,
					ProviderDbType   = 36,
				});
			}

			return list;
		}

		#region Mapping
		// https://learn.microsoft.com/en-us/sql/relational-databases/system-catalog-views/object-catalog-views-transact-sql?view=sql-server-ver17

		[Table("assemblies", Schema = "sys")]
		class Assembly
		{
			[Column("name",        CanBeNull = false)] public string Name       { get; set; } = default!;
			[Column("assembly_id", CanBeNull = false)] public int    AssemblyId { get; set; } = default!;

			[Association(ThisKey = "AssemblyId", OtherKey = "AssemblyId", CanBeNull = true)]
			public List<AssemblyType> AssemblyTypes { get; set; } = default!;
		}  

		[Table("assembly_types", Schema = "sys")]
		class AssemblyType
		{
			[Column("assembly_qualified_name", CanBeNull = false)] public string AssemblyQualifiedName { get; set; } = default!;
			[Column("assembly_class",          CanBeNull = false)] public string AssemblyClass         { get; set; } = default!;
			[Column("assembly_id",             CanBeNull = false)] public int    AssemblyId            { get; set; } = default!;
			[Column("is_nullable",             CanBeNull = false)] public bool   IsNullable            { get; set; } = default!;
			[Column("is_fixed_length",         CanBeNull = false)] public bool   IsFixedLength         { get; set; } = default!;
			[Column("max_length",              CanBeNull = false)] public short  MaxLength             { get; set; } = default!;
		}

		[Table("columns", Schema = "sys")]
		sealed class Column
		{
			[Column("object_id",             CanBeNull = false)] public int    ObjectId            { get; set; } = default!;
			[Column("name",                  CanBeNull = false)] public string Name                { get; set; } = default!;
			[Column("column_id",             CanBeNull = false)] public int    ColumnId            { get; set; } = default!;
			[Column("system_type_id",        CanBeNull = false)] public int    SystemTypeId        { get; set; } = default!;
			[Column("user_type_id",          CanBeNull = false)] public int    UserTypeId          { get; set; } = default!;
			[Column("max_length",            CanBeNull = false)] public short  MaxLength           { get; set; } = default!;
			[Column("precision",             CanBeNull = false)] public short  Precision           { get; set; } = default!;
			[Column("scale",                 CanBeNull = false)] public short  Scale               { get; set; } = default!;
			[Column("is_nullable",           CanBeNull = false)] public bool   IsNullable          { get; set; } = default!;
			[Column("is_identity",           CanBeNull = false)] public bool   IsIdentity          { get; set; } = default!;
			[Column("is_computed",           CanBeNull = false)] public bool   IsComputed          { get; set; } = default!;
			[Column("generated_always_type", CanBeNull = false)] public byte   GeneratedAlwaysType { get; set; } = default!;

			[Association(ThisKey = "ObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public Object Object { get; set; } = default!;

			[Association(ThisKey = "ObjectId", OtherKey = "ObjectId", CanBeNull = true)]
			public Table? Table { get; set; } = default!;

			[Association(ThisKey = "UserTypeId", OtherKey = "UserTypeId", CanBeNull = true)]
			public SqlDataType Type { get; set; } = default!;
		}

		[Table("extended_properties", Schema = "sys")]
		sealed class ExtendedProperty
		{
			[Column("major_id", CanBeNull = false)] public int     MajorId { get; set; } = default!;
			[Column("minor_id", CanBeNull = false)] public int     MinorId { get; set; } = default!;
			[Column("name",     CanBeNull = false)] public string  Name    { get; set; } = default!;
			[Column("class",    CanBeNull = false)] public byte    Class   { get; set; } = default!;
			[Column("value",    CanBeNull = true)]  public object? Value   { get; set; } = default!;
		}

		[Table("foreign_keys", Schema = "sys")]
		sealed class ForeignKey : Object
		{
			[Column("referenced_object_id", CanBeNull = false)] public int ReferencedObjectId { get; set; } = default!;
			[Column("key_index_id",         CanBeNull = false)] public int KeyIndexId         { get; set; } = default!;
		}

		[Table("foreign_key_columns", Schema = "sys")]
		sealed class ForeignKeyColumn
		{
			[Column("constraint_object_id", CanBeNull = false)] public int ConstraintObjectId { get; set; } = default!;
			[Column("constraint_column_id", CanBeNull = false)] public int ConstraintColumnId { get; set; } = default!;
			[Column("parent_object_id",     CanBeNull = false)] public int ParentObjectId     { get; set; } = default!;
			[Column("parent_column_id",     CanBeNull = false)] public int ParentColumnId     { get; set; } = default!;
			[Column("referenced_object_id", CanBeNull = false)] public int ReferencedObjectId { get; set; } = default!;
			[Column("referenced_column_id", CanBeNull = false)] public int ReferencedColumnId { get; set; } = default!;

			[Association(ThisKey = "ConstraintObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public ForeignKey ForeignKey { get; set; } = default!;

			[Association(ThisKey = "ParentObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public Table ThisTable { get; set; } = default!;

			[Association(ThisKey = "ParentObjectId, ParentColumnId", OtherKey = "ObjectId, ColumnId", CanBeNull = false)]
			public Column ThisColumn { get; set; } = default!;

			[Association(ThisKey = "ReferencedObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public Table OtherTable { get; set; } = default!;

			[Association(ThisKey = "ReferencedObjectId, ReferencedColumnId", OtherKey = "ObjectId, ColumnId", CanBeNull = false)]
			public Column OtherColumn { get; set; } = default!;
		}

		[Table("indexes", Schema = "sys")]
		sealed class Index
		{
			[Column("object_id", CanBeNull = false)] public int    ObjectId { get; set; } = default!;
			[Column("name",      CanBeNull = false)] public string Name     { get; set; } = default!;
			[Column("index_id",  CanBeNull = false)] public int    IndexId  { get; set; } = default!;
			[Column("type",      CanBeNull = false)] public string Type     { get; set; } = default!;
		}

		[Table("index_columns", Schema = "sys")]
		sealed class IndexColumn
		{
			[Column("object_id",       CanBeNull = false)] public int  ObjectId      { get; set; } = default!;
			[Column("index_id",        CanBeNull = false)] public int  IndexId       { get; set; } = default!;
			[Column("index_column_id", CanBeNull = false)] public int  IndexColumnId { get; set; } = default!;
			[Column("column_id",       CanBeNull = false)] public int  ColumnId      { get; set; } = default!;
			[Column("key_ordinal",     CanBeNull = false)] public byte KeyOrdinal    { get; set; } = default!;

			[Association(ThisKey = "ObjectId, ColumnId", OtherKey = "ObjectId, ColumnId", CanBeNull = false)]
			public Column Column { get; set; } = default!;
		}

		[Table("key_constraints", Schema = "sys")]
		sealed class KeyConstraint : Object
		{
			[Column("unique_index_id", CanBeNull = false)] public int UniqueIndexId { get; set; } = default!;

			[Association(ThisKey = "ParentObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public Table ParentTable { get; set; } = default!;

			[Association(ThisKey = "ParentObjectId, UniqueIndexId", OtherKey = "ObjectId, IndexId", CanBeNull = false)]
			public Index Index { get; set; } = default!;

			[Association(ThisKey = "ParentObjectId, UniqueIndexId", OtherKey = "ObjectId, IndexId")]
			public List<IndexColumn> IndexColumns { get; set; } = default!;
		}

		[Table("objects", Schema = "sys")]
		class Object
		{
			[Column("object_id",        CanBeNull = false)] public int    ObjectId       { get; set; } = default!;
			[Column("schema_id",        CanBeNull = false)] public int    SchemaId       { get; set; } = default!;
			[Column("parent_object_id", CanBeNull = true)]  public int?   ParentObjectId { get; set; } = default!;
			[Column("name",             CanBeNull = false)] public string Name           { get; set; } = default!;
			[Column("type",             CanBeNull = false)] public string Type           { get; set; } = default!;
			[Column("is_ms_shipped",    CanBeNull = false)] public bool   IsMsShipped    { get; set; } = default!;

			[Association(ThisKey = "ObjectId", OtherKey = "ObjectId", CanBeNull = true)]
			public Table? Table { get; set; } = default!;

			[Association(ThisKey = "SchemaId", OtherKey = "SchemaId", CanBeNull = false)]
			public Schema Schema { get; set; } = default!;
		}

		[Table("parameters", Schema = "sys")]
		sealed class Parameter
		{
			[Column("object_id",      CanBeNull = false)] public int    ObjectId     { get; set; } = default!;
			[Column("name",           CanBeNull = false)] public string Name         { get; set; } = default!;
			[Column("parameter_id",   CanBeNull = false)] public int    ParameterId  { get; set; } = default!;
			[Column("system_type_id", CanBeNull = false)] public int    SystemTypeId { get; set; } = default!;
			[Column("user_type_id",   CanBeNull = false)] public int    UserTypeId   { get; set; } = default!;
			[Column("max_length",     CanBeNull = false)] public short  MaxLength    { get; set; } = default!;
			[Column("precision",      CanBeNull = false)] public short  Precision    { get; set; } = default!;
			[Column("scale",          CanBeNull = false)] public short  Scale        { get; set; } = default!;
			[Column("is_nullable",    CanBeNull = false)] public bool   IsNullable   { get; set; } = default!;
			[Column("is_output",      CanBeNull = false)] public bool   IsOutput     { get; set; } = default!;

			[Association(ThisKey = "ObjectId", OtherKey = "ObjectId", CanBeNull = false)]
			public Object Object { get; set; } = default!;

			[Association(ThisKey = "UserTypeId", OtherKey = "UserTypeId", CanBeNull = true)]
			public SqlDataType Type { get; set; } = default!;
		}

		[Table("schemas", Schema = "sys")]
		sealed class Schema
		{
			[Column("schema_id", CanBeNull = false)] public int    SchemaId { get; set; } = default!;
			[Column("name",      CanBeNull = false)] public string Name     { get; set; } = default!;
		}

		[Table("types", Schema = "sys")]
		sealed class SqlDataType
		{
			[Column("system_type_id", CanBeNull = false)] public int    SystemTypeId { get; set; } = default!;
			[Column("user_type_id",   CanBeNull = false)] public int    UserTypeId   { get; set; } = default!;
			[Column("schema_id",      CanBeNull = false)] public int    SchemaId     { get; set; } = default!;
			[Column("name",           CanBeNull = false)] public string Name         { get; set; } = default!;
			[Column("is_table_type",  CanBeNull = false)] public bool   IsTableType  { get; set; } = default!;

			[Association(ThisKey = "SchemaId", OtherKey = "SchemaId", CanBeNull = false)]
			public Schema Schema { get; set; } = default!;
		}

		[Table("tables", Schema = "sys")]
		sealed class Table : Object
		{
			[Column("temporal_type", CanBeNull = false)] public byte TemporalType { get; set; } = default!;
		}

		// undocumented function used by `INFORMATION_SCHEMA.COLUMNS`
		[Sql.Function(ProviderName.SqlServer, "ODBCSCALE", ServerSideOnly = true)]
		private static int? OdbcScale(int? typeId, int? scale)
			=> throw new ServerSideOnlyException(nameof(OdbcScale));
		#endregion
	}
}
