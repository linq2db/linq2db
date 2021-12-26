using System;
using System.Collections.Generic;
using LinqToDB.Metadata;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;

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
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				name.Name))
			{
				Public    = true,
				Static    = true,
				Summary   = func.Description,
				Extension = true
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

			var typeMapping = _typeMappingsProvider.GetTypeMapping(scalarResult.Type);
			var funcModel   = new AggregateFunctionModel(name, method, metadata, (typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(scalarResult.Nullable));

			BuildParameters(func.Parameters, funcModel.Parameters);

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
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				name.Name))
			{
				Public  = true,
				Static  = true,
				Summary = func.Description
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
					var typeMapping  = _typeMappingsProvider.GetTypeMapping(scalarResult.Type);
					funcModel.Return = (typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(scalarResult.Nullable);
					// TODO: DataType not used by current scalar function mapping API
					break;
				}
				case ResultKind.Tuple:
				{
					var tupleResult = (TupleResult)func.Result;

					// tuple model class
					var @class = new ClassModel(
						_namingServices.NormalizeIdentifier(_options.DataModel.FunctionTupleResultClassNameOptions,
						func.Name.Name))
					{
						IsPublic  = true,
						IsPartial = true
					};
					funcModel.ReturnTuple = new TupleModel(@class)
					{
						CanBeNull = tupleResult.Nullable
					};

					// fields order must be preserved, as tuple fields mapped by ordinal
					foreach (var field in tupleResult.Fields)
					{
						var typeMapping = _typeMappingsProvider.GetTypeMapping(field.Type);

						var prop = new PropertyModel(_namingServices.NormalizeIdentifier(_options.DataModel.FunctionTupleResultPropertyNameOptions, field.Name ?? "Field"), (typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(field.Nullable))
						{
							IsPublic  = true,
							IsDefault = true,
							HasSetter = true
						};
						funcModel.ReturnTuple.Fields.Add(new TupleFieldModel(prop, field.Type)
						{
							DataType = typeMapping?.DataType
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
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				name.Name))
			{
				Public  = true,
				Summary = func.Description
			};

			var metadata  = new TableFunctionMetadata() { Name = name };
			var funcModel = new TableFunctionModel(
				name,
				method,
				metadata,
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureMethodInfoFieldNameOptions, func.Name.Name))
			{
				Error = func.SchemaError?.Message
			};

			BuildParameters(func.Parameters, funcModel.Parameters);

			if (func.Result != null)
				funcModel.Result = PrepareResultSetModel(func.Name, func.Result);

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
			var (name, isNonDefaultSchema) = ProcessObjectName(func.Name, defaultSchemas);

			var method = new MethodModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureNameOptions,
				name.Name))
			{
				Public    = true,
				Static    = true,
				Summary   = func.Description,
				Extension = true
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
					var typeMapping  = _typeMappingsProvider.GetTypeMapping(scalarResult.Type);

					var paramName    = _namingServices.NormalizeIdentifier(_options.DataModel.ProcedureParameterNameOptions, scalarResult.Name ?? "return");
					funcModel.Return = new FunctionParameterModel(
						new ParameterModel(paramName, (typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(scalarResult.Nullable),
						CodeParameterDirection.Out))
					{
						Type       = scalarResult.Type,
						DataType   = typeMapping?.DataType,
						DbName     = scalarResult.Name,
						IsNullable = scalarResult.Nullable
					};
					break;
				}
			}

			if (func.ResultSets?.Count > 1)
			{
				// TODO: to support multi-result sets we need at least one implementation in schema provider
				throw new NotImplementedException($"Multi-set stored procedures not supported");
			}
			else if (func.ResultSets?.Count == 1)
			{
				funcModel.Results.Add(PrepareResultSetModel(func.Name, func.ResultSets[0]));
			}

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
				var typeMapping = _typeMappingsProvider.GetTypeMapping(param.Type);
				var paramName   = _namingServices.NormalizeIdentifier(_options.DataModel.ProcedureParameterNameOptions, param.Name);

				CodeParameterDirection direction;
				switch (param.Direction)
				{
					case ParameterDirection.Input      : direction = CodeParameterDirection.In ; break;
					case ParameterDirection.InputOutput: direction = CodeParameterDirection.Ref; break;
					case ParameterDirection.Output     : direction = CodeParameterDirection.Out; break;
					default                            :
						throw new InvalidOperationException($"Unsupported parameter direction: {param.Direction}");
				}

				var parameterModel = new ParameterModel(
					paramName,
					(typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(param.Nullable),
					direction)
				{
					Description = param.Description
				};

				var fp = new FunctionParameterModel(parameterModel)
				{
					DbName     = param.Name,
					Type       = param.Type,
					DataType   = typeMapping?.DataType,
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
					_options.DataModel.SchemaPropertyOptions,
					baseName);

				var wrapperClass = new ClassModel(
					schemaClassName,
					_options.CodeGeneration.ClassPerFile ? schemaClassName : dataContext.Class.FileName!)
				{
					IsPublic  = true,
					IsPartial = true,
					IsStatic  = true,
					Namespace = _options.CodeGeneration.Namespace
				};
				var contextClass = new ClassModel("DataContext")
				{
					IsPublic  = true,
					IsPartial = true
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
		private FunctionResult PrepareResultSetModel(ObjectName funcName, IReadOnlyCollection<ResultColumn> columns)
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
								return new FunctionResult(null, _entities[schemaObject.Name].Entity);
						}
					}
				}
			}

			var resultClass = new ClassModel(
				_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureResultClassNameOptions,
				funcName.Name))
			{
				IsPublic  = true,
				IsPartial = true
			};
			var model = new ResultTableModel(resultClass);

			foreach (var col in columns)
			{
				var typeMapping = _typeMappingsProvider.GetTypeMapping(col.Type);

				var metadata = new ColumnMetadata()
				{
					Name         = col.Name ?? string.Empty,
					DbType       = col.Type,
					DataType     = typeMapping?.DataType,
					CanBeNull    = col.Nullable,
					SkipOnInsert = true,
					SkipOnUpdate = true
				};

				var property  = new PropertyModel(
					_namingServices.NormalizeIdentifier(_options.DataModel.ProcedureResultColumnPropertyNameOptions, col.Name ?? "Column"),
					(typeMapping?.CLRType ?? WellKnownTypes.System.Object).WithNullability(col.Nullable))
				{
					IsPublic  = true,
					HasSetter = true,
					IsDefault = true,
				};

				var colModel = new ColumnModel(metadata, property);
				model.Columns.Add(colModel);
			}

			return new FunctionResult(model, null);
		}
	}
}
