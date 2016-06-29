using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace LinqToDB.SchemaProvider
{
	using Common;
	using Data;
	using SqlProvider;

	public abstract class SchemaProviderBase : ISchemaProvider
	{
		protected abstract DataType             GetDataType   (string dataType, string columnType, long? length, int? prec, int? scale);
		protected abstract List<TableInfo>      GetTables     (DataConnection dataConnection);
		protected abstract List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection);
		protected abstract List<ColumnInfo>     GetColumns    (DataConnection dataConnection);
		protected abstract List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection);
		protected abstract string               GetProviderSpecificTypeNamespace();

		protected virtual List<ProcedureInfo> GetProcedures(DataConnection dataConnection)
		{
			return null;
		}

		protected virtual List<ProcedureParameterInfo> GetProcedureParameters(DataConnection dataConnection)
		{
			return null;
		}

		protected List<DataTypeInfo> DataTypes;
		protected string[]           IncludedSchemas;
		protected string[]           ExcludedSchemas;
		protected bool               GenerateChar1AsString;
		protected DataTable          DataTypesSchema;

		protected Dictionary<string,DataTypeInfo> DataTypesDic;

		public virtual DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null)
		{
			if (options == null)
				options = new GetSchemaOptions();

			IncludedSchemas       = options.IncludedSchemas ?? new string[0];
			ExcludedSchemas       = options.ExcludedSchemas ?? new string[0];
			GenerateChar1AsString = options.GenerateChar1AsString;

			var dbConnection = (DbConnection)dataConnection.Connection;

			InitProvider(dataConnection);

			DataTypes    = GetDataTypes(dataConnection);
			DataTypesDic = new Dictionary<string, DataTypeInfo>(DataTypes.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var dt in DataTypes)
				if (!DataTypesDic.ContainsKey(dt.TypeName))
					DataTypesDic.Add(dt.TypeName, dt);

			List<TableSchema>     tables;
			List<ProcedureSchema> procedures;

			if (options.GetTables)
			{
				tables =
				(
					from t in GetTables(dataConnection)
					where
						(IncludedSchemas.Length == 0 ||  IncludedSchemas.Contains(t.SchemaName)) &&
						(ExcludedSchemas.Length == 0 || !ExcludedSchemas.Contains(t.SchemaName))
					select new TableSchema
					{
						ID              = t.TableID,
						CatalogName     = t.CatalogName,
						SchemaName      = t.SchemaName,
						TableName       = t.TableName,
						Description     = t.Description,
						IsDefaultSchema = t.IsDefaultSchema,
						IsView          = t.IsView,
						TypeName        = ToValidName(t.TableName),
						Columns         = new List<ColumnSchema>(),
						ForeignKeys     = new List<ForeignKeySchema>()
					}
				).ToList();

				var pks = GetPrimaryKeys(dataConnection);

				#region Columns

				var columns =
					from c  in GetColumns(dataConnection)

					join pk in pks
						on c.TableID + "." + c.Name equals pk.TableID + "." + pk.ColumnName into g2
					from pk in g2.DefaultIfEmpty()

					join t  in tables on c.TableID equals t.ID

					orderby c.Ordinal
					select new { t, c, dt = GetDataType(c.DataType), pk };

				foreach (var column in columns)
				{
					var dataType   = column.c.DataType;
					var systemType = GetSystemType(dataType, column.c.ColumnType, column.dt, column.c.Length, column.c.Precision, column.c.Scale);
					var isNullable = column.c.IsNullable;
					var columnType = column.c.ColumnType ?? GetDbType(dataType, column.dt, column.c.Length, column.c.Precision, column.c.Scale);

					column.t.Columns.Add(new ColumnSchema
					{
						Table                = column.t,
						ColumnName           = column.c.Name,
						ColumnType           = columnType,
						IsNullable           = isNullable,
						MemberName           = ToValidName(column.c.Name),
						MemberType           = ToTypeName(systemType, isNullable),
						SystemType           = systemType ?? typeof(object),
						DataType             = GetDataType(dataType, column.c.ColumnType, column.c.Length, column.c.Precision, column.c.Scale),
						ProviderSpecificType = GetProviderSpecificType(dataType),
						SkipOnInsert         = column.c.SkipOnInsert || column.c.IsIdentity,
						SkipOnUpdate         = column.c.SkipOnUpdate || column.c.IsIdentity,
						IsPrimaryKey         = column.pk != null,
						PrimaryKeyOrder      = column.pk != null ? column.pk.Ordinal : -1,
						IsIdentity           = column.c.IsIdentity,
						Description          = column.c.Description,
						Length               = column.c.Length,
						Precision            = column.c.Precision,
						Scale                = column.c.Scale,
					});
				}

				#endregion

				#region FK

				var fks = GetForeignKeys(dataConnection);

				foreach (var fk in fks.OrderBy(f => f.Ordinal))
				{
					var thisTable  = (from t in tables where t.ID == fk.ThisTableID  select t).FirstOrDefault();
					var otherTable = (from t in tables where t.ID == fk.OtherTableID select t).FirstOrDefault();

					if (thisTable == null || otherTable == null)
						continue;

					var thisColumn  = (from c in thisTable. Columns where c.ColumnName == fk.ThisColumn   select c).Single();
					var otherColumn = (from c in otherTable.Columns where c.ColumnName == fk.OtherColumn  select c).Single();

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

				var pst = GetProviderSpecificTables(dataConnection);

				if (pst != null)
					tables.AddRange(pst);
			}
			else
				tables = new List<TableSchema>();

			if (options.GetProcedures)
			{
				#region Procedures

				var sqlProvider = dataConnection.DataProvider.CreateSqlBuilder();
				var procs       = GetProcedures(dataConnection);
				var procPparams = GetProcedureParameters(dataConnection);
				var n           = 0;

				if (procs != null)
				{
					procedures =
					(
						from sp in procs
						where
							(IncludedSchemas.Length == 0 ||  IncludedSchemas.Contains(sp.SchemaName)) &&
							(ExcludedSchemas.Length == 0 || !ExcludedSchemas.Contains(sp.SchemaName))
						join p  in procPparams on sp.ProcedureID equals p.ProcedureID
						into gr
						select new ProcedureSchema
						{
							CatalogName         = sp.CatalogName,
							SchemaName          = sp.SchemaName,
							ProcedureName       = sp.ProcedureName,
							MemberName          = ToValidName(sp.ProcedureName),
							IsFunction          = sp.IsFunction,
							IsTableFunction     = sp.IsTableFunction,
							IsAggregateFunction = sp.IsAggregateFunction,
							IsDefaultSchema     = sp.IsDefaultSchema,
							Parameters          =
							(
								from pr in gr

								join dt in DataTypes
									on pr.DataType equals dt.TypeName into g1
								from dt in g1.DefaultIfEmpty()

								let systemType = GetSystemType(pr.DataType, null, dt, pr.Length, pr.Precision, pr.Scale)

								orderby pr.Ordinal
								select new ParameterSchema
								{
									SchemaName           = pr.ParameterName,
									SchemaType           = GetDbType(pr.DataType, dt, pr.Length, pr.Precision, pr.Scale),
									IsIn                 = pr.IsIn,
									IsOut                = pr.IsOut,
									IsResult             = pr.IsResult,
									Size                 = pr.Length,
									ParameterName        = ToValidName(pr.ParameterName ?? "par" + ++n),
									ParameterType        = ToTypeName(systemType, true),
									SystemType           = systemType ?? typeof(object),
									DataType             = GetDataType(pr.DataType, null, pr.Length, pr.Precision, pr.Scale),
									ProviderSpecificType = GetProviderSpecificType(pr.DataType),
								}
							).ToList()
						} into ps
						where ps.Parameters.All(p => p.SchemaType != "table type")
						select ps
					).ToList();

					var current = 1;

					foreach (var procedure in procedures)
					{
						if ((!procedure.IsFunction || procedure.IsTableFunction) && options.LoadProcedure(procedure))
						{
							var catalog     = procedure.CatalogName   == null ? null : sqlProvider.Convert(procedure.CatalogName,   ConvertType.NameToDatabase).  ToString();
							var schema      = procedure.SchemaName    == null ? null : sqlProvider.Convert(procedure.SchemaName,    ConvertType.NameToOwner).     ToString();
							var procName    = procedure.ProcedureName == null ? null : sqlProvider.Convert(procedure.ProcedureName, ConvertType.NameToQueryTable).ToString();
							var commandText = sqlProvider.BuildTableName(new StringBuilder(), catalog, schema, procName).ToString();

							LoadProcedureTableSchema(dataConnection, procedure, commandText, tables);
						}

						options.ProcedureLoadingProgress(procedures.Count, current++);
					}

				}
				else
					procedures = new List<ProcedureSchema>();

				#endregion

				var psp = GetProviderSpecificProcedures(dataConnection);

				if (psp != null)
					procedures.AddRange(psp);
			}
			else
				procedures = new List<ProcedureSchema>();

			return ProcessSchema(new DatabaseSchema
			{
				DataSource                    = GetDataSourceName(dbConnection),
				Database                      = GetDatabaseName  (dbConnection),
				ServerVersion                 = dbConnection.ServerVersion,
				Tables                        = tables,
				Procedures                    = procedures,
				ProviderSpecificTypeNamespace = GetProviderSpecificTypeNamespace(),
				DataTypesSchema               = DataTypesSchema,

			}, options);
		}

		protected virtual List<TableSchema> GetProviderSpecificTables(DataConnection dataConnection)
		{
			return null;
		}

		protected virtual List<ProcedureSchema> GetProviderSpecificProcedures(DataConnection dataConnection)
		{
			return null;
		}

		protected virtual void LoadProcedureTableSchema(
			DataConnection dataConnection, ProcedureSchema procedure, string commandText, List<TableSchema> tables)
		{
			CommandType     commandType;
			DataParameter[] parameters;

			if (procedure.IsTableFunction)
			{
				commandText = "SELECT * FROM " + commandText + "(";

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					if (i != 0)
						commandText += ",";
					commandText += "NULL";
				}

				commandText += ")";
				commandType = CommandType.Text;
				parameters  = new DataParameter[0];
			}
			else
			{
				commandType = CommandType.StoredProcedure;
				parameters  = procedure.Parameters.Select(p =>
					new DataParameter
					{
						Name      = p.ParameterName,
						Value     =
							p.SystemType == typeof(string)   ? "" :
							p.SystemType == typeof(DateTime) ? DateTime.Now :
								DefaultValue.GetValue(p.SystemType),
						DataType  = p.DataType,
						Size      = (int?)p.Size,
						Direction =
							p.IsIn ?
								p.IsOut ?
									ParameterDirection.InputOutput :
									ParameterDirection.Input :
								ParameterDirection.Output
					}).ToArray();
			}

			try
			{
				var st = GetProcedureSchema(dataConnection, commandText, commandType, parameters);

				procedure.IsLoaded = true;

				if (st != null)
				{
					procedure.ResultTable = new TableSchema
					{
						IsProcedureResult = true,
						TypeName          = ToValidName(procedure.ProcedureName + "Result"),
						ForeignKeys       = new List<ForeignKeySchema>(),
						Columns           = GetProcedureResultColumns(st)
					};

					foreach (var column in procedure.ResultTable.Columns)
						column.Table = procedure.ResultTable;

					procedure.SimilarTables =
					(
						from  t in tables
						where t.Columns.Count == procedure.ResultTable.Columns.Count
						let zip = t.Columns.Zip(procedure.ResultTable.Columns, (c1, c2) => new { c1, c2 })
						where zip.All(z => z.c1.ColumnName == z.c2.ColumnName && z.c1.SystemType == z.c2.SystemType)
						select t
					).ToList();
				}
			}
			catch (Exception ex)
			{
				procedure.ResultException = ex;
			}
		}

		protected virtual string GetProviderSpecificType(string dataType)
		{
			return null;
		}

		protected DataTypeInfo GetDataType(string typeName)
		{
			DataTypeInfo dt;
			return DataTypesDic.TryGetValue(typeName, out dt) ? dt : null;
		}

		protected virtual DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			using (var rd = dataConnection.ExecuteReader(commandText, commandType, CommandBehavior.SchemaOnly, parameters))
			{
				return rd.Reader.GetSchemaTable();
			}
		}

		protected virtual List<ColumnSchema> GetProcedureResultColumns(DataTable resultTable)
		{
			return
			(
				from r in resultTable.AsEnumerable()

				let columnType = r.Field<string>("DataTypeName")
				let columnName = r.Field<string>("ColumnName")
				let isNullable = r.Field<bool>  ("AllowDBNull")

				join dt in DataTypes
					on columnType equals dt.TypeName into g1
				from dt in g1.DefaultIfEmpty()

				let length     = r.Field<int> ("ColumnSize")
				let precision  = Converter.ChangeTypeTo<int>(r["NumericPrecision"])
				let scale      = Converter.ChangeTypeTo<int>(r["NumericScale"])
				let systemType = GetSystemType(columnType, null, dt, length, precision, scale)

				select new ColumnSchema
				{
					ColumnName           = columnName,
					ColumnType           = GetDbType(columnType, dt, length, precision, scale),
					IsNullable           = isNullable,
					MemberName           = ToValidName(columnName),
					MemberType           = ToTypeName(systemType, isNullable),
					SystemType           = systemType ?? typeof(object),
					DataType             = GetDataType(columnType, null, length, precision, scale),
					ProviderSpecificType = GetProviderSpecificType(columnType),
					IsIdentity           = r.Field<bool>("IsIdentity"),
				}
			).ToList();
		}

		protected virtual string GetDataSourceName(DbConnection dbConnection)
		{
			return dbConnection.DataSource;
		}

		protected virtual string GetDatabaseName(DbConnection dbConnection)
		{
			return dbConnection.Database;
		}

		protected virtual void InitProvider(DataConnection dataConnection)
		{
		}

		protected virtual List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			DataTypesSchema = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

			return DataTypesSchema.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("TypeName"),
					DataType         = t.Field<string>("DataType"),
					CreateFormat     = t.Field<string>("CreateFormat"),
					CreateParameters = t.Field<string>("CreateParameters"),
					ProviderDbType   = t.Field<int>   ("ProviderDbType"),
				})
				.ToList();
		}

		protected virtual Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			var systemType = dataTypeInfo != null ? Type.GetType(dataTypeInfo.DataType) : null;

			if (length == 1 && !GenerateChar1AsString && systemType == typeof(string))
				systemType = typeof(char);

			return systemType;
		}

		protected virtual string GetDbType(string columnType, DataTypeInfo dataType, long? length, int? prec, int? scale)
		{
			var dbType = columnType;

			if (dataType != null)
			{
				var format = dataType.CreateFormat;
				var parms  = dataType.CreateParameters;

				if (!string.IsNullOrWhiteSpace(format) && !string.IsNullOrWhiteSpace(parms))
				{
					var paramNames  = parms.Split(',');
					var paramValues = new object[paramNames.Length];

					for (var i = 0; i < paramNames.Length; i++)
					{
						switch (paramNames[i].Trim().ToLower())
						{
							case "size"       :
							case "length"     : paramValues[i] = length;        break;
							case "max length" : paramValues[i] = length == int.MaxValue ? "max" : length.HasValue ? length.ToString() : null; break;
							case "precision"  : paramValues[i] = prec;          break;
							case "scale"      : paramValues[i] = scale.HasValue || paramNames.Length == 2 ? scale : prec; break;
						}
					}

					if (paramValues.All(v => v != null))
						dbType = format.Args(paramValues);
				}
			}

			return dbType;
		}

		protected string ToValidName(string name)
		{
			if (name.Contains(" "))
			{
				var ss = name.Split(' ')
					.Where (s => s.Trim().Length > 0)
					.Select(s => char.ToUpper(s[0]) + s.Substring(1));

				name = string.Join("", ss.ToArray());
			}

			if (name.Length > 0 && char.IsDigit(name[0]))
				name = "_" + name;

			return name
				.Replace('$', '_')
				.Replace('#', '_')
				.Replace('-', '_')
				.Replace('/', '_')
				;
		}

		protected string ToTypeName(Type type, bool isNullable)
		{
			if (type == null)
				type = typeof(object);

			var memberType = type.Name;

			switch (memberType)
			{
				case "Boolean" : memberType = "bool";    break;
				case "Byte"    : memberType = "byte";    break;
				case "SByte"   : memberType = "sbyte";   break;
				case "Byte[]"  : memberType = "byte[]";  break;
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

			if (!type.IsClass && isNullable)
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
					string name = null;

					if (schemaOptions.GetAssociationMemberName != null)
					{
						name = schemaOptions.GetAssociationMemberName(key);

						if (name != null)
							key.MemberName = name;
					}

					if (name == null)
					{
						name = key.MemberName;

						if (key.BackReference != null && key.ThisColumns.Count == 1 && key.ThisColumns[0].MemberName.ToLower().EndsWith("id"))
						{
							name = key.ThisColumns[0].MemberName;
							name = name.Substring(0, name.Length - "id".Length);

							if (t.ForeignKeys.Select(_ => _.MemberName). Concat(
								t.Columns.    Select(_ => _.MemberName)).Concat(
								new[] { t.TypeName }).All(_ => _ != name))
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

							name = string.Join("", name
								.Split('_')
								.Where(_ =>
									_.Length > 0 && _ != t.TableName &&
									(t.SchemaName == null || t.IsDefaultSchema || _ != t.SchemaName))
								.ToArray());
						}

						if (name.Length != 0 &&
							t.ForeignKeys.Select(_ => _.MemberName).Concat(
							t.Columns.    Select(_ => _.MemberName)).Concat(
								new[] { t.TypeName }).All(_ => _ != name))
						{
							key.MemberName = name;
						}
					}
				}
			}

			return databaseSchema;
		}
	}
}
