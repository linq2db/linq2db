using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.CodeGen.Metadata
{

	public static class MetadataBuilder
	{
		public static DataModel LoadDataModel(DataConnection dataConnection, SchemaSettings settings)
		{
			var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();

			var schema = schemaProvider.GetSchema(dataConnection, CreateSchemaOptions(settings));

			// logic to convert old schema data to new schema model
			// later will be replaced with new schema API
			var model = new DataModel()
			{
				DatabaseName = string.IsNullOrWhiteSpace(schema.Database) ? null : schema.Database,
				DataSource = string.IsNullOrWhiteSpace(schema.DataSource) ? null : schema.DataSource,
				ServerVersion = string.IsNullOrWhiteSpace(schema.ServerVersion) ? null : schema.ServerVersion
			};

			var getTables = settings.Objects.HasFlag(SchemaObjects.Table);
			var getViews = settings.Objects.HasFlag(SchemaObjects.View);
			if (getTables || getViews)
			{
				var columnMappings = new Dictionary<ColumnSchema, Column>();
				var columnsTable = new Dictionary<Column, TableBase>(ReferenceComparer<Column>.Instance);
				foreach (var table in schema.Tables)
				{
					if (table.IsProcedureResult && table.TableName == null)
					{
						continue;
					}

					var schemaName = table.SchemaName;

					if (schemaName != null && model.Schemas.Add(schemaName) && table.IsDefaultSchema)
						model.DefaultSchemas.Add(schemaName);

					var name = new ObjectName(
						settings.ServerName,
						settings.IncludeDatabaseName ? settings.DatabaseName ?? table.CatalogName : null,
						schemaName,
						table.TableName ?? throw new InvalidOperationException("Table or view name missing"));

					var columns = new List<Column>();
					var primaryKey = new List<(int ordinal, Column column)>();
					Column? identity = null;
					foreach (var column in table.Columns)
					{
						var type = new DbType(column.IsNullable, column.ColumnType, column.Length != int.MaxValue ? column.Length : null, column.Precision, column.Precision == 0 ? null : column.Scale, column.DataType != DataType.Undefined ? column.DataType : null);
						var col = new Column(column.ColumnName, string.IsNullOrWhiteSpace(column.Description) ? null : column.Description, type, column.IsIdentity || !column.SkipOnInsert, column.IsIdentity || !column.SkipOnUpdate);
						columns.Add(col);
						if (column.IsPrimaryKey)
						{
							primaryKey.Add((column.PrimaryKeyOrder, col));
						}
						if (column.IsIdentity)
						{
							identity = col;
						}

						columnMappings.Add(column, col);

						model.TypeMap[type] = (column.DataType, column.SystemType, column.ProviderSpecificType);
					}

					if (table.IsView && getViews && settings.LoadView(name))
					{
						var view = new View(
							name,
							string.IsNullOrEmpty(table.Description) ? null : table.Description,
							table.IsProviderSpecific,
							columns,
							primaryKey.Count > 0 ? new PrimaryKey(primaryKey.OrderBy(_ => _.ordinal).Select(_ => _.column).ToArray()) : null,
							identity != null ? new Identity(identity) : null);

						model.Views.Add(view);

						foreach (var col in columns)
							columnsTable.Add(col, view);
					}
					else if (!table.IsView && getTables && settings.LoadTable(name))
					{
						var tbl = new Table(
							name,
							string.IsNullOrEmpty(table.Description) ? null : table.Description,
							table.IsProviderSpecific,
							columns,
							primaryKey.Count > 0 ? new PrimaryKey(primaryKey.OrderBy(_ => _.ordinal).Select(_ => _.column).ToArray()) : null,
							identity != null ? new Identity(identity) : null);
						model.Tables.Add(tbl);

						foreach (var col in columns)
							columnsTable.Add(col, tbl);
					}
				}

				foreach (var table in schema.Tables)
				{
					// exclude fake backreference FK (will exclude also valid FKs with such suffix)
					foreach (var fk in table.ForeignKeys.Where(fk => !fk.KeyName.EndsWith("_BackReference")))
					{
						var cols = new List<(Column, Column)>();
						for (var i = 0; i < fk.ThisColumns.Count; i++)
						{
							cols.Add((columnMappings[fk.ThisColumns[i]], columnMappings[fk.OtherColumns[i]]));
						}

						// check that table not filtered out
						if (columnsTable.TryGetValue(cols[0].Item1, out var sourceTable)
							&& columnsTable.TryGetValue(cols[0].Item2, out var targetTable))
							model.ForeignKeys.Add(new ForeignKey(fk.KeyName, sourceTable, targetTable, cols));
					}
				}
			}

			var getProcedures = settings.Objects.HasFlag(SchemaObjects.StoredProcedure);
			var getTableFunctions = settings.Objects.HasFlag(SchemaObjects.TableFunction);
			var getScalarFunctions = settings.Objects.HasFlag(SchemaObjects.ScalarFunction);
			var getAggregates = settings.Objects.HasFlag(SchemaObjects.Aggregate);
			if (getProcedures || getTableFunctions || getScalarFunctions || getAggregates)
			{
				foreach (var proc in schema.Procedures)
				{
					var schemaName = proc.SchemaName;

					if (schemaName != null && model.Schemas.Add(schemaName) && proc.IsDefaultSchema)
						model.DefaultSchemas.Add(schemaName);

					var name = new ObjectName(settings.ServerName, settings.IncludeDatabaseName ? settings.DatabaseName ?? proc.CatalogName : null, schemaName, proc.ProcedureName);
					var parameters = new List<Parameter>();
					ReturnValue? returnValue = null;
					foreach (var param in proc.Parameters)
					{
						var type = new DbType(param.IsNullable, param.SchemaType, param.Size != int.MaxValue ? param.Size : null, null, null, param.DataType != DataType.Undefined ? param.DataType : null);
						model.TypeMap[type] = (param.DataType, param.SystemType, param.ProviderSpecificType);
						var description = string.IsNullOrWhiteSpace(param.Description) ? null : param.Description;
						if (param.IsResult)
						{
							if (description != null)
								throw new NotImplementedException();
							returnValue = new ReturnValue(string.IsNullOrEmpty(param.SchemaName) ? null : param.SchemaName, type);
						}
						else
						{
							ParameterDirection direction;
							if (param.IsIn && param.IsOut)
								direction = ParameterDirection.InOut;
							else if (param.IsIn)
								direction = ParameterDirection.In;
							else if (param.IsOut)
							{
								// TODO: remove on schema provider level
								// pgsql declare generate TABLE parameter for each column
								if (proc.IsTableFunction)
									continue;
								direction = ParameterDirection.Out;
							}
							else
								throw new InvalidOperationException();

							parameters.Add(new Parameter(param.SchemaName ?? string.Empty, description, type, direction));
						}
					}

					List<Column>? result = null;
					var results = new List<List<Column>>();
					if (proc.ResultTable != null)
					{
						result = new List<Column>();
						results.Add(result);
						foreach (var column in proc.ResultTable.Columns)
						{
							var type = new DbType(column.IsNullable, column.ColumnType, column.Length != int.MaxValue ? column.Length : null, column.Precision, column.Precision == 0 ? null : column.Scale, column.DataType != DataType.Undefined ? column.DataType : null);
							model.TypeMap[type] = (column.DataType, column.SystemType, column.ProviderSpecificType);
							var col = new Column(column.ColumnName, string.IsNullOrWhiteSpace(column.Description) ? null : column.Description, type, column.IsIdentity || !column.SkipOnInsert, column.IsIdentity || !column.SkipOnUpdate);
							result.Add(col);
						}
					}

					var procDescription = string.IsNullOrWhiteSpace(proc.Description) ? null : proc.Description;

					if (!proc.IsFunction && getProcedures)
					{
						if (returnValue == null && settings.AddReturnParameterToProcedures.Contains(name))
						{
							// TODO: execute only for sql server
							// this is sqlserver-specific option
							var dbType = new DbType(false, "int", null, null, null, DataType.Int32);
							returnValue = new ReturnValue("@return", dbType);
							if (!model.TypeMap.ContainsKey(dbType))
							{
								model.TypeMap.Add(dbType, (DataType.Int32, typeof(int), "SqlInt32"));
							}
						}

						var sproc = new StoredProcedure(name, procDescription, parameters, proc.ResultException, results, returnValue);
						model.StoredProcedures.Add(sproc);
					}
					else if (proc.IsFunction)
					{
						if (proc.IsTableFunction)
						{
							if (getTableFunctions)
							{
								model.TableFunctions.Add(new TableFunction(name, procDescription, parameters, proc.ResultException, result!));
							}
						}
						else if (proc.IsAggregateFunction)
						{
							if (getAggregates)
							{
								model.Aggregates.Add(new Aggregate(name, procDescription, parameters, returnValue ?? throw new InvalidOperationException("Scalar function return parameter missing")));
							}
						}
						else
						{
							if (getScalarFunctions)
							{
								ReturnValue[] returnValues;

								if (parameters.Any(p => p.Direction != ParameterDirection.In))
								{
									// TODO: should be in schema provider
									// postgresql results conversion
									var resultParameters = parameters.Where(p => p.Direction != ParameterDirection.In).Select(p => new ReturnValue(p.Name, p.Type)).ToArray();
									parameters = parameters.Where(p => p.Direction == ParameterDirection.In || p.Direction == ParameterDirection.InOut).ToList();

									if (returnValue != null)
										throw new InvalidOperationException();
									if (resultParameters.Length == 1)
									{
										returnValues = new[] { resultParameters[0] };
									}
									else
									{
										returnValues = resultParameters;
									}
								}
								else if (returnValue == null)
								{
									// postgresql "RETURN void" function
									// we generate it as method with object return type to allow call in select context
									var dbType = new DbType(true, "void", null, null, null, DataType.Undefined);
									returnValues = new[] { new ReturnValue(null, dbType) };
									if (!model.TypeMap.ContainsKey(dbType))
									{
										model.TypeMap.Add(dbType, (DataType.Undefined, typeof(object), null));
									}
								}
								else
									returnValues = new[] { returnValue };


								model.ScalarFunctions.Add(new ScalarFunction(name, procDescription, parameters, returnValues, proc.IsResultDynamic));
							}
						}
					}
				}
			}

			return model;

		}

		private static GetSchemaOptions CreateSchemaOptions(SchemaSettings settings)
		{
			var options = new GetSchemaOptions();
			
			options.DefaultSchema = null;

			options.GetForeignKeys = settings.Objects.HasFlag(SchemaObjects.ForeignKey);

			// requires post-load filtering
			options.GetProcedures = settings.Objects.HasFlag(SchemaObjects.StoredProcedure)
				|| settings.Objects.HasFlag(SchemaObjects.ScalarFunction)
				|| settings.Objects.HasFlag(SchemaObjects.TableFunction)
				|| settings.Objects.HasFlag(SchemaObjects.Aggregate);

			// requires post-load filtering
			options.GetTables = settings.Objects.HasFlag(SchemaObjects.Table)
				|| settings.Objects.HasFlag(SchemaObjects.View);

			if (settings.IncludeCatalogs)
				options.IncludedCatalogs = settings.Catalogs?.ToArray();
			else
				options.ExcludedCatalogs = settings.Catalogs?.ToArray();

			if (settings.IncludeSchemas)
				options.IncludedSchemas = settings.Schemas?.ToArray();
			else
				options.ExcludedSchemas = settings.Schemas?.ToArray();

			options.LoadProcedure = p => settings.LoadProcedureSchema(new ObjectName(settings.ServerName, settings.IncludeDatabaseName ? settings.DatabaseName ?? p.CatalogName : null, p.SchemaName, p.ProcedureName));
			options.UseSchemaOnly = settings.UseSafeLoadProcedureSchema;
			options.PreferProviderSpecificTypes = settings.PreferProviderSpecificTypes;

			return options;
		}
	}
}
