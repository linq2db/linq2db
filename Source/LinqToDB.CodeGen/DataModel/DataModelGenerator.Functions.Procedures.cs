using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;
using LinqToDB.Data;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		// IMPORTANT:
		// ExecuteProc/QueryProc API available only in DataConnection, so if context is not based on DataConnection, we
		// should generate DataConnection parameter for context isntead of typed generated context
		// note that we shouldn't fix it by extending current API to be available to DataContext too as instead of it we need
		// to introduce new API which works with FQN procedure name components
		private void BuildStoredProcedure(
			StoredProcedureModel storedProcedure,
			Func<RegionGroup> proceduresGroup,
			IType dataContextType)
		{
			RegionBuilder? region = null;

			if (storedProcedure.Error != null)
			{
				if (_dataModel.GenerateProceduresSchemaError)
					(region ??= proceduresGroup().New(storedProcedure.Method.Name)).Pragmas().Add(_code.Error(storedProcedure.Error));

				if (_dataModel.SkipProceduresWithSchemaErrors)
					return;
			}

			var method = DefineMethod((region ??= proceduresGroup().New(storedProcedure.Method.Name)).Methods(false), storedProcedure.Method, true);

			var ctxParam = _code.Parameter(
					// see method notes
					dataContextType,
					_code.Identifier("dataConnection"),
					Model.ParameterDirection.In);
			method.Parameter(ctxParam);
			var body = method.Body();

			CodeVariable? parametersVar = null;
			var parameterRebinds = new List<CodeAssignmentStatement>();

			var hasParameters = storedProcedure.Parameters.Count > 0 || storedProcedure.Return != null;
			if (hasParameters)
			{
				var parameterValues = new ICodeExpression[storedProcedure.Parameters.Count + (storedProcedure.Return != null ? 1 : 0)];
				parametersVar = _code.Variable(_code.Identifier("parameters"), _code.Type(typeof(DataParameter[]), false), true);
				var parametersArray = _code.Assign(
					parametersVar,
					_code.Array(_code.Type(typeof(DataParameter), false), true, parameterValues, false));
				body.Append(parametersArray);

				if (storedProcedure.Parameters.Count > 0)
				{
					for (var i = 0; i < storedProcedure.Parameters.Count; i++)
					{
						var p = storedProcedure.Parameters[i];

						var param = DefineParameter(method, p.Parameter);

						parameterValues[i] = BuildProcedureParameter(
							param,
							false,
							p.DbName,
							p.DataType,
							p.Type,
							parametersVar,
							parameterRebinds,
							storedProcedure.Parameters.Count);
					}
				}

				if (storedProcedure.Return != null)
				{
					var param = DefineParameter(method, storedProcedure.Return.Parameter);

					parameterValues[storedProcedure.Parameters.Count] = BuildProcedureParameter(
						param,
						true,
						storedProcedure.Return.Name ?? "return",
						storedProcedure.Return.DataType,
						storedProcedure.Return.Type,
						parametersVar,
						parameterRebinds,
						storedProcedure.Parameters.Count);
				}
			}

			ICodeExpression? returnValue = null;

			if (storedProcedure.Results.Count > 1)
			{
				// TODO:
				throw new NotImplementedException($"Multiple result-set stored procedure generation not imlpemented yet");
			}
			else if (storedProcedure.Results.Count == 0)
			{
				var executeProcParameters = new ICodeExpression[hasParameters ? 3 : 2];

				executeProcParameters[0] = ctxParam.Reference;
				executeProcParameters[1] = _code.Constant(BuildFunctionName(storedProcedure.Name), true);
				if (hasParameters)
					executeProcParameters[2] = parametersVar!.Reference;

				method.Returns(_code.Type(typeof(int), false));
				returnValue = _code.ExtCall(
					_code.Type(typeof(DataConnectionExtensions), false),
					_code.Identifier(nameof(DataConnectionExtensions.ExecuteProc)),
					Array.Empty<IType>(),
					executeProcParameters,
					method.Method.ReturnType!.Type);
			}
			else
			{
				// if procedure result table contains unique (and not empty) column names, we have use columns mappings
				// otherwise we should bind columns manually by ordinal (as we don't have by-ordinal mapping conventions support)
				var (customTable, mappedTable) = storedProcedure.Results[0];
				IType returnElementType;

				var queryProcTypeArgs = Array.Empty<IType>();
				ICodeExpression[] queryProcParameters;

				var ordinalMapping = customTable != null && customTable.Columns.Select(c => c.Metadata.Name).Where(_ => !string.IsNullOrEmpty(_)).Distinct().Count() == customTable.Columns.Count;

				ClassBuilder? customResultClass = null;
				CodeProperty[]? customProperties = null;
				if (customTable != null)
				{
					(customResultClass, customProperties) = BuildCustomResultClass(customTable, region, !ordinalMapping);
					returnElementType = customResultClass.Type.Type;
				}
				else
					returnElementType = _entityBuilders[mappedTable!].Type.Type;

				if (ordinalMapping)
				{
					// manual mapping
					queryProcParameters = new ICodeExpression[hasParameters ? 4 : 3];

					// generate positional mapping lambda
					// TODO: switch to ColumnReader.GetValue in future to utilize more precise mapping based on column mapping attributes
					var drParam = _code.LambdaParameter(_code.Identifier("dataReader"), WellKnownTypes.AdoNet.DbDataReader);
					var initializers = new CodeAssignmentStatement[customTable!.Columns.Count];
					var lambda = _code.Lambda(WellKnownTypes.Func(returnElementType, WellKnownTypes.AdoNet.DbDataReader), true)
						.Parameter(drParam);
					queryProcParameters[1] = lambda.Method;
					lambda.Body()
						.Append(_code.Return(
							_code.New(
								customResultClass!.Type.Type,
								Array.Empty<ICodeExpression>(),
								initializers)));

					var ms = _code.Member(ctxParam.Reference, _code.Identifier(nameof(DataConnection.MappingSchema)), ctxParam.Type!.Type);
					for (var i = 0; i < customTable.Columns.Count; i++)
					{
						var prop = customProperties![i];
						initializers[i] = _code.Assign(
							prop.Reference,
							_code.Call(
								new CodeTypeReference(_code.Type(typeof(Common.Converter), false)),
								_code.Identifier(nameof(Common.Converter.ChangeTypeTo)),
								new[] { prop.Type.Type },
								new ICodeExpression[]
								{
										_code.Call(drParam.Reference, _code.Identifier(nameof(DbDataReader.GetValue)), Array.Empty<IType>(), new ICodeExpression[] { _code.Constant(i, true) }, WellKnownTypes.Object.WithNullability(true)),
										ms
								},
								prop.Type.Type));
					}
				}
				else
				{
					// built-in mapping by name
					queryProcParameters = new ICodeExpression[hasParameters ? 3 : 2];
					queryProcTypeArgs = new[] { returnElementType };
				}

				queryProcParameters[0] = ctxParam.Reference;
				queryProcParameters[queryProcParameters.Length - (hasParameters ? 2 : 1)] = _code.Constant(BuildFunctionName(storedProcedure.Name), true);
				if (hasParameters)
					queryProcParameters[queryProcParameters.Length - 1] = parametersVar!.Reference;

				method.Returns(
					_code.Type(
						_dataModel.GenerateProcedureResultAsList ? typeof(List<>) : typeof(IEnumerable<>),
						false,
						returnElementType));

				returnValue = _code.ExtCall(
					_code.Type(typeof(Enumerable), false),
					_code.Identifier(nameof(Enumerable.ToList)),
					Array.Empty<IType>(),
					new[]
					{
						_code.ExtCall(
							_code.Type(typeof(DataConnectionExtensions), false),
							_code.Identifier(nameof(DataConnectionExtensions.QueryProc)),
							queryProcTypeArgs,
							queryProcParameters,
							WellKnownTypes.Enumerable(returnElementType))
					},
					WellKnownTypes.List(returnElementType));
			}

			if (parameterRebinds.Count > 0)
			{
				var callProcVar = _code.Variable(_code.Identifier("ret"), method.Method.ReturnType!.Type, true);
				body.Append(_code.Assign(callProcVar, returnValue!));
				foreach (var rebind in parameterRebinds)
					body.Append(rebind);
				body.Append(_code.Return(callProcVar.Reference));
			}
			else
				body.Append(_code.Return(returnValue));
		}

		private ICodeExpression BuildProcedureParameter(
			CodeParameter parameter,
			bool returnParameter,
			string? parameterName,
			DataType? dataType,
			DatabaseType? dbType,
			CodeVariable parametersVar,
			List<CodeAssignmentStatement> parameterRebinds,
			int idx)
		{
			var ctorParams = new List<ICodeExpression>();
			var ctorInitializers = new List<CodeAssignmentStatement>();

			ctorParams.Add(_code.Constant(parameterName ?? $"p{idx}", true));
			ctorParams.Add(parameter.Direction == Model.ParameterDirection.In || parameter.Direction == Model.ParameterDirection.Ref ? parameter.Reference : _code.Null(_code.Type(typeof(object), true), true));
			if (dataType != null)
				ctorParams.Add(_code.Constant(dataType.Value, true));

			if (parameter.Direction == Model.ParameterDirection.Out && !returnParameter)
				ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Direction.Reference, _code.Constant(System.Data.ParameterDirection.Output, true)));
			else if (parameter.Direction == Model.ParameterDirection.Ref)
				ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Direction.Reference, _code.Constant(System.Data.ParameterDirection.InputOutput, true)));
			else if (returnParameter)
				ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Direction.Reference, _code.Constant(System.Data.ParameterDirection.ReturnValue, true)));

			if (dbType != null)
			{
				if (dbType.Name != null && _dataModel.GenerateProcedureParameterDbType)
					ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.DbType.Reference, _code.Constant(dbType.Name!, true)));
				// TODO: min/max check added to avoid issues with type inconsistance in schema API and metadata
				if (dbType.Length != null && dbType.Length >= int.MinValue && dbType.Length <= int.MaxValue)
					ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Size.Reference, _code.Constant((int)dbType.Length.Value, true)));
				if (dbType.Precision != null)
					ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Precision.Reference, _code.Constant(dbType.Precision.Value, true)));
				if (dbType.Scale != null)
					ctorInitializers.Add(_code.Assign(WellKnownTypes.LinqToDB.DataParameter.Scale.Reference, _code.Constant(dbType.Scale.Value, true)));
			}

			if (parameter.Direction != Model.ParameterDirection.In)
			{
				parameterRebinds.Add(
					_code.Assign(
						parameter.Reference,
						_code.Call(
							new CodeTypeReference(_code.Type(typeof(Common.Converter), false)),
							_code.Identifier(nameof(Common.Converter.ChangeTypeTo)),
							new[] { parameter.Type!.Type },
							new ICodeExpression[] { _code.Member(_code.Index(parametersVar.Reference, _code.Constant(idx, true), WellKnownTypes.LinqToDB.DataParameter.Type), _code.Identifier(nameof(DataParameter.Value)), WellKnownTypes.Object.WithNullability(true)) },
							parameter.Type!.Type)));
			}

			return _code.New(_code.Type(typeof(DataParameter), false), ctorParams.ToArray(), ctorInitializers.ToArray());
		}
	}
}
