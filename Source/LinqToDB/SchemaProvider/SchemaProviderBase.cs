using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data;

namespace LinqToDB.SchemaProvider
{
	public abstract class SchemaProviderBase : ISchemaProvider
	{
		protected abstract DataType                            GetDataType   (string? dataType, string? columnType, int? length, int? precision, int? scale);
		protected abstract List<TableInfo>                     GetTables     (DataConnection dataConnection, GetSchemaOptions options);
		protected abstract IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options);
		protected abstract List<ColumnInfo>                    GetColumns    (DataConnection dataConnection, GetSchemaOptions options);
		protected abstract IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options);
		protected abstract string?                             GetProviderSpecificTypeNamespace();

		protected virtual List<ProcedureInfo>?          GetProcedures         (DataConnection dataConnection, GetSchemaOptions options) => null;
		protected virtual List<ProcedureParameterInfo>? GetProcedureParameters(DataConnection dataConnection, IEnumerable<ProcedureInfo> procedures, GetSchemaOptions options) => null;

		protected HashSet<string?>   IncludedSchemas  = null!;
		protected HashSet<string?>   ExcludedSchemas  = null!;
		protected HashSet<string?>   IncludedCatalogs = null!;
		protected HashSet<string?>   ExcludedCatalogs = null!;
		protected bool               GenerateChar1AsString;
		protected DataTable          DataTypesSchema  = null!;

		private Dictionary<string, DataTypeInfo> DataTypesDic = null!;
		private Dictionary<string, DataTypeInfo> ProviderSpecificDataTypesDic = null!;

		private Dictionary<int, DataTypeInfo> DataTypesByProviderDbTypeDic = null!;
		private Dictionary<int, DataTypeInfo> ProviderSpecificDataTypesByProviderDbTypeDic = null!;

		/// <summary>
		/// If true, provider doesn't support schema-only procedure execution and will execute procedure for real.
		/// </summary>
		protected virtual bool GetProcedureSchemaExecutesProcedure => false;

		protected string? BuildSchemaFilter(GetSchemaOptions? options, string defaultSchema, Action<StringBuilder, string> stringLiteralBuilder)
		{
			var schemas = new HashSet<string>();

			schemas.Add(defaultSchema);

			if (options != null)
			{
				if (options.IncludedSchemas != null && options.IncludedSchemas.Length > 0)
				{
					schemas.Clear();
					foreach (var schema in options.IncludedSchemas)
						if (!string.IsNullOrEmpty(schema))
							schemas.Add(schema!);
				}

				if (options.ExcludedSchemas != null && options.ExcludedSchemas.Length > 0)
					foreach (var schema in options.ExcludedSchemas)
						if (!string.IsNullOrEmpty(schema))
							schemas.Remove(schema!);
			}

			if (schemas.Count == 0)
				return null;

			var first = true;

			using var sb = Pools.StringBuilder.Allocate();
			sb.Value.Append("IN (");

			foreach (var schema in schemas)
			{
				if (!first)
					sb.Value.Append(", ");
				else
					first = false;

				stringLiteralBuilder(sb.Value, schema);
			}

			sb.Value.Append(')');

			return sb.Value.ToString();
		}

		public virtual DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions? options = null)
		{
			options ??= new GetSchemaOptions();

			IncludedSchemas       = GetHashSet(options.IncludedSchemas,  options.StringComparer);
			ExcludedSchemas       = GetHashSet(options.ExcludedSchemas,  options.StringComparer);
			IncludedCatalogs      = GetHashSet(options.IncludedCatalogs, options.StringComparer);
			ExcludedCatalogs      = GetHashSet(options.ExcludedCatalogs, options.StringComparer);
			GenerateChar1AsString = options.GenerateChar1AsString;

			var dbConnection = dataConnection.OpenDbConnection();

			InitProvider(dataConnection, options);

			DataTypesDic                                 = new Dictionary<string,DataTypeInfo>(StringComparer.OrdinalIgnoreCase);
			ProviderSpecificDataTypesDic                 = new Dictionary<string,DataTypeInfo>(StringComparer.OrdinalIgnoreCase);
			DataTypesByProviderDbTypeDic                 = new Dictionary<int   ,DataTypeInfo>();
			ProviderSpecificDataTypesByProviderDbTypeDic = new Dictionary<int   ,DataTypeInfo>();

			foreach (var dt in GetDataTypes(dataConnection))
				if (dt.ProviderSpecific)
				{
#if NET6_0_OR_GREATER
					ProviderSpecificDataTypesDic.TryAdd(dt.TypeName, dt);
					ProviderSpecificDataTypesByProviderDbTypeDic.TryAdd(dt.ProviderDbType, dt);
#else
					if (!ProviderSpecificDataTypesDic.ContainsKey(dt.TypeName))
						ProviderSpecificDataTypesDic.Add(dt.TypeName, dt);
					if (!ProviderSpecificDataTypesByProviderDbTypeDic.ContainsKey(dt.ProviderDbType))
						ProviderSpecificDataTypesByProviderDbTypeDic.Add(dt.ProviderDbType, dt);
#endif
				}
				else
				{
#if NET6_0_OR_GREATER
					DataTypesDic.TryAdd(dt.TypeName, dt);
					DataTypesByProviderDbTypeDic.TryAdd(dt.ProviderDbType, dt);
#else
					if (!DataTypesDic.ContainsKey(dt.TypeName))
						DataTypesDic.Add(dt.TypeName, dt);
					if (!DataTypesByProviderDbTypeDic.ContainsKey(dt.ProviderDbType))
						DataTypesByProviderDbTypeDic.Add(dt.ProviderDbType, dt);
#endif
				}

			List<TableSchema>     tables;
			List<ProcedureSchema> procedures;

			if (options.GetTables)
			{
				tables =
				(
					from t in GetTables(dataConnection, options)
					where
						(IncludedSchemas .Count == 0 ||  IncludedSchemas .Contains(t.SchemaName))  &&
						(ExcludedSchemas .Count == 0 || !ExcludedSchemas .Contains(t.SchemaName))  &&
						(IncludedCatalogs.Count == 0 ||  IncludedCatalogs.Contains(t.CatalogName)) &&
						(ExcludedCatalogs.Count == 0 || !ExcludedCatalogs.Contains(t.CatalogName)) &&
						(options.LoadTable == null   ||  options.LoadTable(new LoadTableData(t)))
					select new TableSchema
					{
						ID                 = t.TableID,
						CatalogName        = t.CatalogName,
						SchemaName         = t.SchemaName,
						TableName          = t.TableName,
						Description        = t.Description,
						IsDefaultSchema    = t.IsDefaultSchema,
						IsView             = t.IsView,
						TypeName           = ToValidName(t.TableName),
						Columns            = new List<ColumnSchema>(),
						ForeignKeys        = new List<ForeignKeySchema>(),
						IsProviderSpecific = t.IsProviderSpecific
					}
				).ToList();

				var pks = GetPrimaryKeys(dataConnection, tables, options);

				#region Columns

				var columns =
					from c  in GetColumns(dataConnection, options)

					join pk in pks
						on c.TableID + "." + c.Name equals pk.TableID + "." + pk.ColumnName into g2
					from pk in g2.DefaultIfEmpty()

					join t  in tables on c.TableID equals t.ID

					orderby c.Ordinal
					select new { t, c, dt = GetDataType(c.DataType, c.Type, options), pk };

				foreach (var column in columns)
				{
					var dataType   = column.c.DataType;
					var systemType = GetSystemType(dataType, column.c.ColumnType, column.dt, column.c.Length, column.c.Precision, column.c.Scale, options);
					var isNullable = column.c.IsNullable;
					var columnType = column.c.ColumnType ?? GetDbType(options, dataType, column.dt, column.c.Length, column.c.Precision, column.c.Scale, null, null, null);

					column.t.Columns.Add(new ColumnSchema
					{
						Table                = column.t,
						ColumnName           = column.c.Name,
						ColumnType           = columnType,
						IsNullable           = isNullable,
						MemberName           = ToValidName(column.c.Name),
						MemberType           = ToTypeName(systemType, isNullable),
						SystemType           = systemType,
						DataType             = column.c.Type ?? GetDataType(dataType, column.c.ColumnType, column.c.Length, column.c.Precision, column.c.Scale),
						ProviderSpecificType = GetProviderSpecificType(dataType),
						SkipOnInsert         = column.c.SkipOnInsert || column.c.IsIdentity,
						SkipOnUpdate         = column.c.SkipOnUpdate || column.c.IsIdentity,
						IsPrimaryKey         = column.pk != null,
						PrimaryKeyOrder      = column.pk?.Ordinal ?? -1,
						IsIdentity           = column.c.IsIdentity,
						Description          = column.c.Description,
						Length               = column.c.Length,
						Precision            = column.c.Precision,
						Scale                = column.c.Scale,
						Ordinal              = column.c.Ordinal,
					});
				}

				#endregion

				#region FK

				var fks = options.GetForeignKeys ? GetForeignKeys(dataConnection, tables, options) : [];

				foreach (var fk in fks.OrderBy(f => f.Ordinal))
				{
					var thisTable  = (from t in tables where t.ID == fk.ThisTableID  select t).FirstOrDefault();
					var otherTable = (from t in tables where t.ID == fk.OtherTableID select t).FirstOrDefault();

					if (thisTable == null || otherTable == null)
						continue;

					var stringComparison = ForeignKeyColumnComparison(fk.OtherColumn);

					var thisColumn  = (from c in thisTable. Columns where c.ColumnName == fk.ThisColumn   select c).SingleOrDefault();
					var otherColumn =
					(
						from c in otherTable.Columns
						where string.Equals(c.ColumnName, fk.OtherColumn, stringComparison)
						select c
					).SingleOrDefault();

					if (thisColumn == null || otherColumn == null)
						continue;

					var key = thisTable.ForeignKeys.FirstOrDefault(f => f.KeyName == fk.Name);

					if (key == null)
					{
						key = new ForeignKeySchema
						{
							KeyName      = fk.Name,
							MemberName   = ToValidName(fk.Name),
							ThisTable    = thisTable,
							OtherTable   = otherTable,
							ThisColumns  = new List<ColumnSchema>(),
							OtherColumns = new List<ColumnSchema>(),
							CanBeNull    = true,
						};

						thisTable.ForeignKeys.Add(key);
					}

					key.ThisColumns. Add(thisColumn);
					key.OtherColumns.Add(otherColumn);
				}

				#endregion

				var pst = GetProviderSpecificTables(dataConnection, options);

				if (pst != null)
					tables.AddRange(pst);
			}
			else
				tables = new List<TableSchema>();

			if (options.GetProcedures)
			{
				#region Procedures

				var sqlProvider = dataConnection.DataProvider.CreateSqlBuilder(dataConnection.MappingSchema, dataConnection.Options);
				var procs       = GetProcedures(dataConnection, options);
				var n           = 0;

				if (procs != null)
				{
					var procParams = (IEnumerable<ProcedureParameterInfo>?)GetProcedureParameters(dataConnection, procs, options) ?? [];

					procedures =
					(
						from sp in procs
						where
							(IncludedSchemas .Count == 0 ||  IncludedSchemas .Contains(sp.SchemaName))  &&
							(ExcludedSchemas .Count == 0 || !ExcludedSchemas .Contains(sp.SchemaName))  &&
							(IncludedCatalogs.Count == 0 ||  IncludedCatalogs.Contains(sp.CatalogName)) &&
							(ExcludedCatalogs.Count == 0 || !ExcludedCatalogs.Contains(sp.CatalogName))
						join p  in procParams on sp.ProcedureID equals p.ProcedureID
						into gr
						select new ProcedureSchema
						{
							CatalogName         = sp.CatalogName,
							SchemaName          = sp.SchemaName,
							PackageName         = sp.PackageName,
							ProcedureName       = sp.ProcedureName,
							MemberName          = ToValidName($"{sp.PackageName}{(sp.PackageName != null ? "_" : null)}{sp.ProcedureName}"),
							IsFunction          = sp.IsFunction,
							IsTableFunction     = sp.IsTableFunction,
							IsResultDynamic     = sp.IsResultDynamic,
							IsAggregateFunction = sp.IsAggregateFunction,
							IsDefaultSchema     = sp.IsDefaultSchema,
							Description         = sp.Description,
							Parameters          =
							(
								from pr in gr

								let dt         = GetDataType(pr.DataType, null, options)

								let systemType = GetSystemType(pr.DataType, pr.DataTypeExact, dt, pr.Length, pr.Precision, pr.Scale, options)

								orderby pr.Ordinal
								select new ParameterSchema
								{
									SchemaName           = pr.ParameterName,
									SchemaType           = GetDbType(options, pr.DataType, dt, pr.Length, pr.Precision, pr.Scale, pr.UDTCatalog, pr.UDTSchema, pr.UDTName),
									IsIn                 = pr.IsIn,
									IsOut                = pr.IsOut,
									IsResult             = pr.IsResult,
									Size                 = pr.Length,
									ParameterName        = ToValidName(pr.ParameterName ?? FormattableString.Invariant($"par{++n}")),
									ParameterType        = ToTypeName(systemType, true),
									SystemType           = systemType,
									DataType             = GetDataType(pr.DataType, pr.DataTypeExact, pr.Length, pr.Precision, pr.Scale),
									ProviderSpecificType = GetProviderSpecificType(pr.DataType),
									IsNullable           = pr.IsNullable,
									Description          = pr.Description
								}
							).ToList()
						} into ps
						select ps
					).ToList();

					var current = 1;

					var isActiveTransaction = dataConnection.Transaction != null;

					if (GetProcedureSchemaExecutesProcedure && isActiveTransaction)
						throw new LinqToDBException("Cannot read schema with GetSchemaOptions.GetProcedures = true from transaction. Remove transaction or set GetSchemaOptions.GetProcedures to false");

					if (!isActiveTransaction)
						dataConnection.BeginTransaction();

					try
					{
						foreach (var procedure in procedures)
						{
							if (!procedure.IsResultDynamic && (!procedure.IsFunction || procedure.IsTableFunction) && options.LoadProcedure(procedure))
							{
								var commandText = sqlProvider.BuildObjectName(
									new (),
									new (procedure.ProcedureName, Database: procedure.CatalogName, Schema: procedure.SchemaName, Package: procedure.PackageName)).ToString();

								LoadProcedureTableSchema(dataConnection, options, procedure, commandText, tables);
							}

							options.ProcedureLoadingProgress(procedures.Count, current++);
						}
					}
					finally
					{
						if (!isActiveTransaction)
							dataConnection.RollbackTransaction();
					}
				}
				else
					procedures = new List<ProcedureSchema>();

				var psp = GetProviderSpecificProcedures(dataConnection);

				if (psp != null)
					procedures.AddRange(psp);

				#endregion
			}
			else
				procedures = new List<ProcedureSchema>();

			return ProcessSchema(new DatabaseSchema
			{
				DataSource                    = GetDataSourceName(dataConnection),
				Database                      = GetDatabaseName  (dataConnection),
				ServerVersion                 = dbConnection.ServerVersion,
				Tables                        = tables,
				Procedures                    = procedures,
				ProviderSpecificTypeNamespace = GetProviderSpecificTypeNamespace(),
				DataTypesSchema               = DataTypesSchema,

			}, options);
		}

		protected virtual StringComparison ForeignKeyColumnComparison(string column) => StringComparison.Ordinal;

		protected static HashSet<string?> GetHashSet(string?[]? data, IEqualityComparer<string?> comparer)
		{
			var set = new HashSet<string?>(comparer ?? StringComparer.OrdinalIgnoreCase);

			if (data == null)
				return set;

			foreach (var s in data)
				set.Add(s);

			return set;
		}

		protected virtual List<TableSchema>?     GetProviderSpecificTables    (DataConnection dataConnection, GetSchemaOptions options) => null;
		protected virtual List<ProcedureSchema>? GetProviderSpecificProcedures(DataConnection dataConnection) => null;

		/// <summary>
		/// Builds table function call command.
		/// </summary>
		protected virtual string BuildTableFunctionLoadTableSchemaCommand(ProcedureSchema procedure, string commandText)
		{
			commandText = "SELECT * FROM " + commandText + "(";

			for (var i = 0; i < procedure.Parameters.Count; i++)
			{
				if (i != 0)
					commandText += ",";
				commandText += "NULL";
			}

			commandText += ")";

			return commandText;
		}

		protected virtual void LoadProcedureTableSchema(
			DataConnection    dataConnection,
			GetSchemaOptions  options,
			ProcedureSchema   procedure,
			string            commandText,
			List<TableSchema> tables)
		{
			CommandType     commandType;
			DataParameter[] parameters;

			if (procedure.IsTableFunction)
			{
				commandText = BuildTableFunctionLoadTableSchemaCommand(procedure, commandText);
				commandType = CommandType.Text;
				parameters  = [];
			}
			else
			{
				commandType = CommandType.StoredProcedure;
				parameters = procedure.Parameters.Select(BuildProcedureParameter).ToArray();
			}

			try
			{
				var st = GetProcedureSchema(dataConnection, commandText, commandType, parameters, options);

				procedure.IsLoaded = true;

				if (st != null && st.Columns.Count > 0)
				{
					var columns = GetProcedureResultColumns(st, options);

					if (columns.Count > 0)
					{
						procedure.ResultTable = new TableSchema
						{
							IsProcedureResult = true,
							TypeName          = ToValidName(procedure.ProcedureName + "Result"),
							ForeignKeys       = new List<ForeignKeySchema>(),
							Columns           = columns
						};

						foreach (var column in procedure.ResultTable.Columns)
							column.Table = procedure.ResultTable;

						procedure.SimilarTables =
						(
							from t in tables
							where t.Columns.Count == procedure.ResultTable.Columns.Count
							let zip = t.Columns.Zip(procedure.ResultTable.Columns, (c1, c2) => new { c1, c2 })
							where zip.All(z => z.c1.ColumnName == z.c2.ColumnName && z.c1.SystemType == z.c2.SystemType)
							select t
						).ToList();
					}
				}
			}
			catch (Exception ex)
			{
				procedure.ResultException = ex;
			}
		}

		protected virtual DataParameter BuildProcedureParameter(ParameterSchema p)
		{
			return new DataParameter
			{
				Name      = p.ParameterName,
				Value     =
					p.SystemType == typeof(string) ?
						"" :
						p.SystemType == typeof(DateTime) ?
							// use fixed value to generate stable baselines
							new DateTime(2020, 09, 23) :
							DefaultValue.GetValue(p.SystemType ?? typeof(object)),
				DataType  = p.DataType,
				DbType    = p.SchemaType,
				Size      = (int?)p.Size,
				Direction =
					p.IsIn ?
						p.IsOut ?
							ParameterDirection.InputOutput :
							ParameterDirection.Input :
						ParameterDirection.Output
			};
		}

		protected virtual string? GetProviderSpecificType(string? dataType) => null;

		protected virtual DataTypeInfo? GetDataType(string? typeName, DataType? dataType, GetSchemaOptions options)
		{
			if (typeName == null)
				return null;

			return
				options.PreferProviderSpecificTypes
				? (ProviderSpecificDataTypesDic.TryGetValue(typeName, out var dt) ? dt : DataTypesDic                .TryGetValue(typeName, out dt) ? dt : null)
				: (DataTypesDic                .TryGetValue(typeName, out dt)     ? dt : ProviderSpecificDataTypesDic.TryGetValue(typeName, out dt) ? dt : null);
		}

		protected DataTypeInfo? GetDataTypeByProviderDbType(int typeId, GetSchemaOptions options)
		{
			return
				options.PreferProviderSpecificTypes
				? (ProviderSpecificDataTypesByProviderDbTypeDic.TryGetValue(typeId, out var dt) ? dt : DataTypesByProviderDbTypeDic                .TryGetValue(typeId, out dt) ? dt : null)
				: (DataTypesByProviderDbTypeDic                .TryGetValue(typeId, out dt)     ? dt : ProviderSpecificDataTypesByProviderDbTypeDic.TryGetValue(typeId, out dt) ? dt : null);
		}

		protected virtual DataTable? GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters, GetSchemaOptions options)
		{
			using var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.SchemaOnly, parameters);
			return rd.Reader!.GetSchemaTable();
		}

		protected virtual List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable, GetSchemaOptions options)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let columnType = r.Field<string>("DataTypeName")
				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>  ("AllowDBNull")
				let dt         = GetDataType(columnType, null, options)
				let length     = r.Field<int?>  ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let systemType = GetSystemType(columnType, null, dt, length, precision, scale, options)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(options, columnType, dt, length, precision, scale, null, null, null),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType,
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.Field<bool>("IsIdentity"),
				}
			).ToList();
		}

		protected virtual string GetDataSourceName(DataConnection dbConnection) => dbConnection.OpenDbConnection().DataSource;
		protected virtual string GetDatabaseName  (DataConnection dbConnection) => dbConnection.OpenDbConnection().Database;

		protected virtual void InitProvider(DataConnection dataConnection, GetSchemaOptions options)
		{
		}

		/// <summary>
		/// Returns list of database data types.
		/// </summary>
		/// <param name="dataConnection">Database connection instance.</param>
		/// <returns>List of database data types.</returns>
		protected virtual List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			DataTypesSchema = dataConnection.OpenDbConnection().GetSchema("DataTypes");

			return DataTypesSchema.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("TypeName")!,
					DataType         = t.Field<string>("DataType")!,
					CreateFormat     = t.Field<string>("CreateFormat"),
					CreateParameters = t.Field<string>("CreateParameters"),
					ProviderDbType   = t.Field<int>   ("ProviderDbType"),
				})
				.ToList();
		}

		protected virtual Type? GetSystemType(string? dataType, string? columnType, DataTypeInfo? dataTypeInfo, int? length, int? precision, int? scale, GetSchemaOptions options)
		{
			var systemType = dataTypeInfo != null ? Type.GetType(dataTypeInfo.DataType) : null;

			if (length == 1 && !GenerateChar1AsString && systemType == typeof(string))
				systemType = typeof(char);

			return systemType;
		}

		protected virtual string? GetDbType(GetSchemaOptions options, string? columnType, DataTypeInfo? dataType, int? length, int? precision, int? scale, string? udtCatalog, string? udtSchema, string? udtName)
		{
			var dbType = columnType ?? dataType?.TypeName;

			if (dataType != null)
			{
				var format = dataType.CreateFormat;
				var parms  = dataType.CreateParameters;

				if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(parms))
				{
					var paramNames  = parms!.Split(',');
					var paramValues = new object?[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim().ToLowerInvariant())
						{
							case "size"       :
							case "length"     : paramValues[i] = length; break;
							// -1: https://learn.microsoft.com/en-us/sql/relational-databases/system-information-schema-views/parameters-transact-sql
							case "max length" : paramValues[i] = length is int.MaxValue or -1 ? "max" : length?.ToString(NumberFormatInfo.InvariantInfo); break;
							case "precision"  : paramValues[i] = precision;   break;
							case "scale"      : paramValues[i] = scale.HasValue || paramNames.Length == 2 ? scale : precision; break;
						}
					}

					if (paramValues.All(v => v != null))
						dbType = string.Format(CultureInfo.InvariantCulture, format, paramValues);
				}
			}

			return dbType;
		}

		private static readonly char[] _nameSeparators = new [] {' ', '\t'};

		// TODO: use proper C# identifier validation procedure
		public static string ToValidName(string name)
		{
			if (name.Contains(" ") || name.Contains("\t"))
			{
				var ss = name.Split(_nameSeparators, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1));

				name = string.Concat(ss);
			}

			if (name.Length > 0 && char.IsDigit(name[0]))
				name = "_" + name;

			return name
				.Replace('$',  '_')
				.Replace('#',  '_')
				.Replace('-',  '_')
				.Replace('/',  '_')
				.Replace('\\', '_')
				.Replace('\r', '_')
				.Replace('\n', '_')
				.Replace('\t', '_')
				.Replace(':' , '_')
				.Replace('`' , '_')
				;
		}

		public static string ToTypeName(Type? type, bool isNullable)
		{
			if (type == null)
				type = typeof(object);

			var memberType = type.Name;

			if (type.IsArray)
				memberType = ToTypeName(type.GetElementType(), false) + "[]";

			switch (memberType)
			{
				case "Boolean" : memberType = "bool";    break;
				case "Byte"    : memberType = "byte";    break;
				case "SByte"   : memberType = "sbyte";   break;
				case "Int16"   : memberType = "short";   break;
				case "Int32"   : memberType = "int";     break;
				case "Int64"   : memberType = "long";    break;
				case "UInt16"  : memberType = "ushort";  break;
				case "UInt32"  : memberType = "uint";    break;
				case "UInt64"  : memberType = "ulong";   break;
				case "Decimal" : memberType = "decimal"; break;
				case "Single"  : memberType = "float";   break;
				case "Double"  : memberType = "double";  break;
				case "String"  : memberType = "string";  break;
				case "Char"    : memberType = "char";    break;
				case "Object"  : memberType = "object";  break;
			}

			if (type.IsGenericType)
				memberType = $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(t => ToTypeName(t, false)))}>";

			if (type.IsValueType && isNullable)
				memberType += "?";

			return memberType;
		}

		protected virtual DatabaseSchema ProcessSchema(DatabaseSchema databaseSchema, GetSchemaOptions schemaOptions)
		{
			foreach (var t in databaseSchema.Tables)
			{
				foreach (var key in t.ForeignKeys.ToList())
				{
					if (!key.KeyName.EndsWith("_BackReference"))
					{
						key.OtherTable.ForeignKeys.Add(
							key.BackReference = new ForeignKeySchema
							{
								KeyName         = key.KeyName    + "_BackReference",
								MemberName      = key.MemberName + "_BackReference",
								AssociationType = AssociationType.Auto,
								OtherTable      = t,
								ThisColumns     = key.OtherColumns,
								OtherColumns    = key.ThisColumns,
								CanBeNull       = true,
							});
					}
				}
			}

			foreach (var t in databaseSchema.Tables)
			{
				foreach (var key in t.ForeignKeys)
				{
					if (key.BackReference != null && key.AssociationType == AssociationType.Auto)
					{
						if (key.ThisColumns.All(_ => _.IsPrimaryKey))
						{
							if (t.Columns.Count(_ => _.IsPrimaryKey) == key.ThisColumns.Count)
								key.AssociationType = AssociationType.OneToOne;
							else
								key.AssociationType = AssociationType.ManyToOne;
						}
						else
							key.AssociationType = AssociationType.ManyToOne;

						key.CanBeNull = key.ThisColumns.All(_ => _.IsNullable);
					}
				}

				foreach (var key in t.ForeignKeys)
				{
					SetForeignKeyMemberName(schemaOptions, t, key);
				}
			}

			return databaseSchema;
		}

		private static void SetForeignKeyMemberName(GetSchemaOptions schemaOptions, TableSchema table, ForeignKeySchema key)
		{
			string? name = null;

			if (schemaOptions.GetAssociationMemberName != null)
			{
				name = schemaOptions.GetAssociationMemberName(key);

				if (name != null)
					key.MemberName = ToValidName(name);
			}

			if (name == null)
			{
				name = key.MemberName;

				if (key.BackReference != null && key.ThisColumns.Count == 1 && key.ThisColumns[0].MemberName.ToLowerInvariant().EndsWith("id"))
				{
					name = key.ThisColumns[0].MemberName;
					name = name.Substring(0, name.Length - "id".Length).TrimEnd('_');

					if (table.ForeignKeys.Select(_ => _.MemberName). Concat(
						table.Columns.    Select(_ => _.MemberName)).Concat(
						new[] { table.TypeName }).Any(_ => _ == name))
					{
						name = key.MemberName;
					}
				}

				if (name == key.MemberName)
				{
					if (name.StartsWith("FK_"))
						name = name.Substring(3);

					if (name.EndsWith("_BackReference"))
						name = name.Substring(0, name.Length - "_BackReference".Length);

					name = string.Concat(name
						.Split('_')
						.Where(_ =>
							_.Length > 0 && _ != table.TableName &&
							(table.SchemaName == null || table.IsDefaultSchema || _ != table.SchemaName)));

					var digitEnd = 0;
					for (var i = name.Length - 1; i >= 0; i--)
					{
						if (char.IsDigit(name[i]))
							digitEnd++;
						else
							break;
					}

					if (digitEnd > 0)
						name = name.Substring(0, name.Length - digitEnd);
				}

				if (string.IsNullOrEmpty(name))
					name = key.OtherTable != key.ThisTable ? key.OtherTable.TableName! : key.KeyName;

				if (table.ForeignKeys.Select(_ => _.MemberName). Concat(
					table.Columns.    Select(_ => _.MemberName)).Concat(
					new[] { table.TypeName }).All(_ => _ != name))
				{
					key.MemberName = ToValidName(name);
				}
			}
		}
	}
}
