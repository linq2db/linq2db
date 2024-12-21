using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace LinqToDB.Scaffold
{
	partial class DataModelLoader
	{
		/// <summary>
		/// Generates aggregate function model from schema data.
		/// </summary>
		/// <param name="dataContext">Data context model.</param>
		/// <param name="func">Function schema.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		private void BuildAggregateFunction(DataContextModel dataContext, AggregateFunction func, ISet<string> defaultSchemas)
		{
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas, false);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				(func.Name.Package != null ? $"{func.Name.Package}_" : null) + name.Name))
			{
				Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Extension,
				Summary   = func.Description,
			};

			var metadata = new FunctionMetadata()
			{
				Name           = name,
				ServerSideOnly = true,
				IsAggregate    = true
			};

			if (func.Parameters.Count > 0)
			{
				metadata.ArgIndices = new int[func.Parameters.Count];
				for (var i = 0; i < metadata.ArgIndices.Length; i++)
					metadata.ArgIndices[i] = i + 1;
			}

			// just a guard
			if (func.Result is not ScalarResult scalarResult)
				throw new InvalidOperationException($"Aggregate function {func.Name} returns non-scalar value.");

			var typeMapping = MapType(scalarResult.Type);
			var funcModel   = new AggregateFunctionModel(name, method, metadata, typeMapping.CLRType.WithNullability(scalarResult.Nullable));

			BuildParameters(func.Parameters, funcModel.Parameters);

			_interceptors.PreprocessAggregateFunction(_languageProvider.TypeParser, funcModel);

			if (isNonDefaultSchema && _options.DataModel.GenerateSchemaAsType)
				GetOrAddAdditionalSchema(dataContext, func.Name.Schema!).AggregateFunctions.Add(funcModel);
			else
				dataContext.AggregateFunctions.Add(funcModel);
		}

		/// <summary>
		/// Generates scalar function model from schema data.
		/// </summary>
		/// <param name="dataContext">Data context model.</param>
		/// <param name="func">Function schema.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		private void BuildScalarFunction(DataContextModel dataContext, ScalarFunction func, ISet<string> defaultSchemas)
		{
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas, _schemaProvider.DatabaseOptions.ScalarFunctionSchemaRequired);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				(func.Name.Package != null ? $"{func.Name.Package}_" : null) + name.Name))
			{
				Modifiers = Modifiers.Public | Modifiers.Static,
				Summary   = func.Description
			};

			var metadata = new FunctionMetadata()
			{
				Name           = name,
				ServerSideOnly = true
			};

			var funcModel = new ScalarFunctionModel(name, method, metadata);

			BuildParameters(func.Parameters, funcModel.Parameters);

			// thanks to pgsql, scalar function could return not only scalar, but also tuple or just nothing
			switch (func.Result.Kind)
			{
				case ResultKind.Scalar:
				{
					var scalarResult = (ScalarResult)func.Result;
					var typeMapping  = MapType(scalarResult.Type);
					funcModel.Return = typeMapping.CLRType.WithNullability(scalarResult.Nullable);
					// TODO: DataType not used by current scalar function mapping API
					break;
				}
				case ResultKind.Tuple:
				{
					var tupleResult = (TupleResult)func.Result;

					// tuple model class
					var @class = new ClassModel(
						_namingServices.NormalizeIdentifier(
							_options.DataModel.FunctionTupleResultClassNameOptions,
							func.Name.Name))
					{
						Modifiers = Modifiers.Public | Modifiers.Partial
					};
					funcModel.ReturnTuple = new TupleModel(@class)
					{
						CanBeNull = tupleResult.Nullable
					};

					// fields order must be preserved, as tuple fields mapped by ordinal
					foreach (var field in tupleResult.Fields)
					{
						var typeMapping = MapType(field.Type);

						var prop = new PropertyModel(_namingServices.NormalizeIdentifier(_options.DataModel.FunctionTupleResultPropertyNameOptions, field.Name ?? "Field"), typeMapping.CLRType.WithNullability(field.Nullable))
						{
							Modifiers = Modifiers.Public,
							IsDefault = true,
							HasSetter = true
						};
						funcModel.ReturnTuple.Fields.Add(new TupleFieldModel(prop, field.Type)
						{
							DataType = typeMapping.DataType
						});
					}

					break;
				}
				case ResultKind.Void:
					// just regular postgresql void function, nothing to see here...
					// because function must have return type to be callable in query, we set return type to object?
					funcModel.Return = WellKnownTypes.System.ObjectNullable;
					break;
			}

			_interceptors.PreprocessScalarFunction(_languageProvider.TypeParser, funcModel);

			if (isNonDefaultSchema && _options.DataModel.GenerateSchemaAsType)
				GetOrAddAdditionalSchema(dataContext, func.Name.Schema!).ScalarFunctions.Add(funcModel);
			else
				dataContext.ScalarFunctions.Add(funcModel);
		}

		/// <summary>
		/// Generates table function model from schema data.
		/// </summary>
		/// <param name="dataContext">Data context model.</param>
		/// <param name="func">Function schema.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		private void BuildTableFunction(DataContextModel dataContext, TableFunction func, ISet<string> defaultSchemas)
		{
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas, false);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				(func.Name.Package != null ? $"{func.Name.Package}_" : null) + name.Name))
			{
				Modifiers = Modifiers.Public,
				Summary   = func.Description
			};

			var metadata  = new TableFunctionMetadata() { Name = name };
			var funcModel = new TableFunctionModel(
				name,
				method,
				metadata)
			{
				Error = func.SchemaError?.Message
			};

			BuildParameters(func.Parameters, funcModel.Parameters);

			if (func.Result != null)
				funcModel.Result = PrepareResultSetModel(func.Name, func.Result);

			_interceptors.PreprocessTableFunction(_languageProvider.TypeParser, funcModel);

			if (isNonDefaultSchema && _options.DataModel.GenerateSchemaAsType)
				GetOrAddAdditionalSchema(dataContext, func.Name.Schema!).TableFunctions.Add(funcModel);
			else
				dataContext.TableFunctions.Add(funcModel);
		}

		/// <summary>
		/// Generates stored procedure model from schema data.
		/// </summary>
		/// <param name="dataContext">Data context model.</param>
		/// <param name="func">Function schema.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		private void BuildStoredProcedure(DataContextModel dataContext, StoredProcedure func, ISet<string> defaultSchemas)
		{
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas, false);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				(func.Name.Package != null ? $"{func.Name.Package}_" : null) + name.Name))
			{
				Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Extension,
				Summary   = func.Description,
			};

			var funcModel = new StoredProcedureModel(name, method)
			{
				Error = func.SchemaError?.Message
			};

			BuildParameters(func.Parameters, funcModel.Parameters);

			switch (func.Result.Kind)
			{
				case ResultKind.Void:
					break;
				case ResultKind.Tuple:
					// no support from db (maybe pgsql could do it?) and schema API now
					throw new NotImplementedException($"Tuple return type support not implemented for stored procedures");
				case ResultKind.Scalar:
				{
					var scalarResult = (ScalarResult)func.Result;
					var typeMapping  = MapType(scalarResult.Type);

					var paramName    = _namingServices.NormalizeIdentifier(_options.DataModel.ProcedureParameterNameOptions, scalarResult.Name ?? "return");
					funcModel.Return = new FunctionParameterModel(
						new ParameterModel(paramName, typeMapping.CLRType.WithNullability(scalarResult.Nullable),
						CodeParameterDirection.Out),
						System.Data.ParameterDirection.ReturnValue)
					{
						Type       = scalarResult.Type,
						DataType   = typeMapping.DataType,
						DbName     = scalarResult.Name,
						IsNullable = scalarResult.Nullable
					};
					break;
				}
			}

			FunctionResult? resultModel = null;
			if (func.ResultSets?.Count > 1)
			{
				// TODO: to support multi-result sets we need at least one implementation in schema provider
				throw new NotImplementedException($"Multi-set stored procedures not supported");
			}
			else if (func.ResultSets?.Count == 1)
			{
				funcModel.Results.Add(resultModel = PrepareResultSetModel(func.Name, func.ResultSets[0]));
			}

			// prepare async result class descriptor if needed
			var returningParameters = funcModel.Parameters.Where(p => p.Direction != System.Data.ParameterDirection.Input).ToList();
			if (funcModel.Return != null)
				returningParameters.Add(funcModel.Return);
			if (returningParameters.Count > 0)
			{
				var asyncResult = new AsyncProcedureResult(
					new ClassModel(
						_namingServices.NormalizeIdentifier(
							_options.DataModel.AsyncProcedureResultClassNameOptions,
							func.Name.Name))
					{
						Modifiers = Modifiers.Public
					}, new PropertyModel("Result")
					{
						Modifiers = Modifiers.Public,
						IsDefault = true,
						HasSetter = true
					});

				foreach (var parameter in returningParameters)
				{
					asyncResult.ParameterProperties.Add(
						parameter,
						new PropertyModel(_namingServices.NormalizeIdentifier(_options.DataModel.AsyncProcedureResultClassPropertiesNameOptions, parameter.Parameter.Name), parameter.Parameter.Type)
						{
							Modifiers = Modifiers.Public,
							IsDefault = true,
							HasSetter = true
						});
				}

				// TODO: next line will need refactoring if we add multi-set support
				funcModel.Results.Clear();
				funcModel.Results.Add(new FunctionResult(resultModel?.CustomTable, resultModel?.Entity, asyncResult));
			}

			_interceptors.PreprocessStoredProcedure(_languageProvider.TypeParser, funcModel);

			if (isNonDefaultSchema && _options.DataModel.GenerateSchemaAsType)
				GetOrAddAdditionalSchema(dataContext, func.Name.Schema!).StoredProcedures.Add(funcModel);
			else
				dataContext.StoredProcedures.Add(funcModel);
		}

		/// <summary>
		/// Create function/procedure parameter models from schema data.
		/// </summary>
		/// <param name="parameters">Parameters schema data.</param>
		/// <param name="models">Collection to store parameter model in.</param>
		private void BuildParameters(IReadOnlyCollection<Parameter> parameters, List<FunctionParameterModel> models)
		{
			foreach (var param in parameters)
			{
				var typeMapping = MapType(param.Type);
				var paramName   = _namingServices.NormalizeIdentifier(_options.DataModel.ProcedureParameterNameOptions, param.Name);

				CodeParameterDirection         direction;
				System.Data.ParameterDirection metadataDirection;
				switch (param.Direction)
				{
					case ParameterDirection.Input      : direction = CodeParameterDirection.In ; metadataDirection = System.Data.ParameterDirection.Input      ; break;
					case ParameterDirection.InputOutput: direction = CodeParameterDirection.Ref; metadataDirection = System.Data.ParameterDirection.InputOutput; break;
					case ParameterDirection.Output     : direction = CodeParameterDirection.Out; metadataDirection = System.Data.ParameterDirection.Output     ; break;
					default                            :
						throw new InvalidOperationException($"Unsupported parameter direction: {param.Direction}");
				}

				var parameterModel = new ParameterModel(
					paramName,
					typeMapping.CLRType.WithNullability(param.Nullable),
					direction)
				{
					Description = param.Description
				};

				var fp = new FunctionParameterModel(parameterModel, metadataDirection)
				{
					DbName     = param.Name,
					Type       = param.Type,
					DataType   = typeMapping.DataType,
					IsNullable = param.Nullable
				};

				models.Add(fp);
			}
		}

		/// <summary>
		/// Return existing or define new model for additional schema.
		/// </summary>
		/// <param name="dataContext">Data context model.</param>
		/// <param name="schemaName">Schema name.</param>
		/// <returns>Additional schema model.</returns>
		private AdditionalSchemaModel GetOrAddAdditionalSchema(DataContextModel dataContext, string schemaName)
		{
			if (!dataContext.AdditionalSchemas.TryGetValue(schemaName, out var schemaModel))
			{
				if (!_options.DataModel.SchemaMap.TryGetValue(schemaName, out var baseName))
					baseName = schemaName;

				var schemaClassName = _namingServices.NormalizeIdentifier(
					_options.DataModel.SchemaClassNameOptions,
					baseName);

				var contextPropertyName = _namingServices.NormalizeIdentifier(
					_options.DataModel.SchemaPropertyNameOptions,
					baseName);

				var wrapperClass = new ClassModel(_options.CodeGeneration.ClassPerFile ? schemaClassName : dataContext.Class.FileName!, schemaClassName)
				{
					Modifiers = Modifiers.Public | Modifiers.Partial | Modifiers.Static,
					Namespace = _options.CodeGeneration.Namespace
				};
				var contextClass = new ClassModel("DataContext")
				{
					Modifiers = Modifiers.Public | Modifiers.Partial
				};
				schemaModel = new AdditionalSchemaModel(contextPropertyName, wrapperClass, contextClass);
				dataContext.AdditionalSchemas.Add(schemaName, schemaModel);
			}

			return schemaModel;
		}

		/// <summary>
		/// Create table function/stored procedure result set model using existing entity or custom model.
		/// </summary>
		/// <param name="funcName">Database name for function/procedure.</param>
		/// <param name="columns">Result set columns schema. Must be ordered by ordinal.</param>
		/// <returns>Model for result set.</returns>
		private FunctionResult PrepareResultSetModel(SqlObjectName funcName, IReadOnlyCollection<ResultColumn> columns)
		{
			// try to find entity model with same set of columns
			// column equality check includes: name, database type and nullability
			if (_options.DataModel.MapProcedureResultToEntity)
			{
				var names = new Dictionary<string, (DatabaseType type, bool isNullable)>();
				foreach (var column in columns)
				{
					// columns without name or duplicate names indicate that it is not entity and requires custom
					// mapper with by-ordinal mapping
					if (string.IsNullOrEmpty(column.Name) || names.ContainsKey(column.Name!))
						break;

					names.Add(column.Name!, (column.Type, column.Nullable));
				}

				if (names.Count == columns.Count)
				{
					foreach (var (schemaObject, entity) in _entities.Values)
					{
						if (schemaObject.Columns.Count == names.Count)
						{
							var match = true;
							foreach (var column in schemaObject.Columns)
							{
								if (!names.TryGetValue(column.Name, out var columnType)
									|| !column.Type.Equals(columnType.type)
									|| column.Nullable != columnType.isNullable)
								{
									match = false;
									break;
								}
							}

							if (match)
								return new FunctionResult(null, _entities[schemaObject.Name].Entity, null);
						}
					}
				}
			}

			var resultClass = new ClassModel(
				_namingServices.NormalizeIdentifier(
					_options.DataModel.ProcedureResultClassNameOptions,
					(funcName.Package != null ? $"{funcName.Package}_" : null) + funcName.Name))
			{
				Modifiers = Modifiers.Partial | Modifiers.Public
			};
			var model = new ResultTableModel(resultClass);

			foreach (var col in columns)
			{
				var typeMapping = MapType(col.Type);

				var metadata = new ColumnMetadata()
				{
					Name      = col.Name ?? string.Empty,
					DbType    = _options.DataModel.GenerateDbType   ? col.Type             : null,
					DataType  = _options.DataModel.GenerateDataType ? typeMapping.DataType : null,
					CanBeNull = col.Nullable
				};

				var property  = new PropertyModel(
					_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureResultColumnPropertyNameOptions, col.Name ?? "Column"),
					typeMapping.CLRType.WithNullability(col.Nullable))
				{
					Modifiers = Modifiers.Public,
					HasSetter = true,
					IsDefault = true,
				};

				var colModel = new ColumnModel(metadata, property);
				model.Columns.Add(colModel);
			}

			return new FunctionResult(model, null, null);
		}
	}
}
