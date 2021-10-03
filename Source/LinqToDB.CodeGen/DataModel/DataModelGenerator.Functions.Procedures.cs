using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.DataModel
{
	// contains generation logic for stored procedure mappings
	partial class DataModelGenerator
	{
		// IMPORTANT:
		// ExecuteProc/QueryProc APIs currently support only DataConnection context, so if
		// context is not based on DataConnection, we should generate DataConnection parameter
		// for context isntead of typed generated context
		//
		// TODO: needs linq2db refactoring
		// Note that we shouldn't fix it by extending current API to be available to DataContext
		// as current API needs refactoring to work with FQN components of procedure name
		// It will be more productive to invest time into new API implementation instead with FQN support on all
		// context types

		/// <summary>
		/// Generates stored procedure mapping.
		/// </summary>
		/// <param name="storedProcedure">Stored procedure model.</param>
		/// <param name="proceduresGroup">Stored procedures region.</param>
		/// <param name="dataContextType">Data context class type.</param>
		private void BuildStoredProcedure(
			StoredProcedureModel storedProcedure,
			Func<RegionGroup>    proceduresGroup,
			IType                dataContextType)
		{
			// generated code sample:
			/*
			 * #region Procedure1
			 * public static IEnumerable<Procedure1Result> Procedure1(this DataConnection dataConnection, int? input, ref int? output)
			 * {
			 *     var parameters = new []
			 *     {
			 *         new DataParameter("@input", input, DataType.Int32),
			 *         new DataParameter("@output", output, DataType.Int32)
			 *         {
			 *             Direction = ParameterDirection.InputOutput
			 *         }
			 *     };
			 *     var ret = dataConnection.QueryProc<Procedure1Result>("Procedure1", parameters).ToList();
			 *     output = Converter.ChangeTypeTo<int?>(parameters[2].Value);
			 *     return ret;
			 * }
			 * 
			 * public partial class Procedure1Result
			 * {
			 *     [LinqToDB.Mapping.Column("Column", CanBeNull = false, DbType = "nvarchar(8)", DataType = DataType.NVarChar, SkipOnInsert = true, SkipOnUpdate = true)] public string Column { get; set; } = null!;
			 * }
			 * #endregion
			 */
			// some notes:
			// - procedure could return no data sets or return records matching known entity, so *Result class generation is optional
			// - for result set with nameless/duplicate columns we generate ordinal reader instead of by-column-name mapping

			// stored procedure region that will contain procedure method mapping and optionally
			// result record mapping
			RegionBuilder? region = null;

			if (storedProcedure.Error != null)
			{
				// if procedure resultset schema load failed, generate error pragma
				if (_dataModel.GenerateProceduresSchemaError)
					(region ??= proceduresGroup().New(storedProcedure.Method.Name))
						.Pragmas()
							.Add(_code.Error(storedProcedure.Error));

				// even with this error procedure generation could continue, as we still
				// can invoke procedure, we just cannot get resultset from it
				if (_dataModel.SkipProceduresWithSchemaErrors)
					return;
			}

			// declare mapping method
			var method = DefineMethod(
				(region ??= proceduresGroup().New(storedProcedure.Method.Name)).Methods(false),
				storedProcedure.Method);

			// declare data context parameter (extension `this` parameter)
			var ctxParam = _code.Parameter(
					// see method notes above regarding type of this parameter
					dataContextType,
					_code.Name(STORED_PROCEDURE_CONTEXT_PARAMETER),
					Model.ParameterDirection.In);
			method.Parameter(ctxParam);
			var body = method.Body();

			// array of procedure parameters (DataParameter objects)
			CodeVariable?              parametersVar    = null;
			// bindings of parameter values to output parameters of method after procedure call
			CodeAssignmentStatement[]? parameterRebinds = null;

			var hasParameters = storedProcedure.Parameters.Count > 0 || storedProcedure.Return != null;
			if (hasParameters)
			{
				// preparations for parameters with return value (out, inout and ret parameters)
				var resultParametersCount = storedProcedure.Parameters.Count(p => p.Parameter.Direction != Model.ParameterDirection.In)
					+ (storedProcedure.Return != null ? 1 : 0);
				parameterRebinds          = resultParametersCount > 0 ? new CodeAssignmentStatement[resultParametersCount] : Array.Empty<CodeAssignmentStatement>();
				var rebindIndex            = 0;

				// DataParameter collection initialization
				var parameterValues = new ICodeExpression[storedProcedure.Parameters.Count + (storedProcedure.Return != null ? 1 : 0)];
				parametersVar       = _code.Variable(_code.Name(STORED_PROCEDURE_PARAMETERS_VARIABLE), WellKnownTypes.LinqToDB.Data.DataParameterArray, true);
				var parametersArray = _code.Assign(
					parametersVar,
					// note that parameterValues is not filled yet, so here we depend on Array method
					// behavior (no copy of parameterValues content)
					_code.Array(WellKnownTypes.LinqToDB.Data.DataParameter, true, parameterValues, false));
				body.Append(parametersArray);

				// build non-return parameters
				for (var i = 0; i < storedProcedure.Parameters.Count; i++)
				{
					var p     = storedProcedure.Parameters[i];
					var param = DefineParameter(method, p.Parameter);

					parameterValues[i] = BuildProcedureParameter(
						param,
						false,
						p.DbName,
						p.DataType,
						p.Type,
						parametersVar,
						storedProcedure.Parameters.Count,
						parameterRebinds,
						rebindIndex);

					if (p.Parameter.Direction != Model.ParameterDirection.In)
						rebindIndex++;
				}

				// build return parameter
				if (storedProcedure.Return != null)
				{
					var param = DefineParameter(method, storedProcedure.Return.Parameter);

					parameterValues[storedProcedure.Parameters.Count] = BuildProcedureParameter(
						param,
						true,
						storedProcedure.Return.Name ?? STORED_PROCEDURE_DEFAULT_RETURN_PARAMETER,
						storedProcedure.Return.DataType,
						storedProcedure.Return.Type,
						parametersVar,
						storedProcedure.Parameters.Count,
						parameterRebinds,
						rebindIndex);
				}
			}

			ICodeExpression? returnValue = null;

			if (storedProcedure.Results.Count > 1)
				// TODO: right now we don't have schema API that could load multiple result sets
				// still it makes sense to implement multiple resultsets generation in future even without
				// schema API, as user could define resultsets manually
				throw new NotImplementedException("Multiple result-sets stored procedure generation not imlpemented yet");
			else if (storedProcedure.Results.Count == 0)
			{
				// for stored procedure call without result set we use ExecuteProc API
				// prepare call parameters
				var executeProcParameters    = new ICodeExpression[hasParameters ? 3 : 2];
				executeProcParameters[0]     = ctxParam.Reference;
				executeProcParameters[1]     = _code.Constant(BuildFunctionName(storedProcedure.Name), true);
				if (hasParameters)
					executeProcParameters[2] = parametersVar!.Reference;

				method.Returns(WellKnownTypes.System.Int32);
				returnValue = _code.ExtCall(
					WellKnownTypes.LinqToDB.Data.DataConnectionExtensions,
					WellKnownTypes.LinqToDB.Data.DataConnectionExtensions_ExecuteProc,
					WellKnownTypes.System.Int32,
					executeProcParameters);
			}
			else
			{
				// for stored procedure call with result set we use QueryProc API
				var (customTable, mappedTable) = storedProcedure.Results[0];

				// QueryProc type- and regular parameters
				IType[]           queryProcTypeArgs;
				ICodeExpression[] queryProcParameters;
				IType             returnElementType;

				// if procedure result table contains unique and not empty column names, we use columns mappings
				// otherwise we should bind columns manually by ordinal (as we don't have by-ordinal mapping conventions support)
				var useOrdinalMapping = customTable != null
					// number of columns remains same after empty names and duplicates removed?
					&& customTable.Columns.Select(c => c.Metadata.Name).Where(_ => !string.IsNullOrEmpty(_)).Distinct().Count() != customTable.Columns.Count;

				CodeProperty[]? customRecordProperties = null;
				if (customTable != null)
					(returnElementType, customRecordProperties) = BuildCustomResultClass(customTable, region, !useOrdinalMapping);
				else
					returnElementType = _entityBuilders[mappedTable!].Type.Type;

				if (useOrdinalMapping)
				{
					// for ordinal mapping we call QueryProc API with mapper lambda parameter:
					// manual mapping
					// Example:
					/*
					 * dataReader => new CustomResult()
					 * {
					 *     Column1 = Converter.ChangeTypeTo<int>(dataReader.GetValue(0), dataConnection.MappingSchema),
					 *     Column2 = Converter.ChangeTypeTo<string>(dataReader.GetValue(1), dataConnection.MappingSchema),
					 * }
					 */

					queryProcParameters = new ICodeExpression[hasParameters ? 4 : 3];

					// generate positional mapping lambda
					// TODO: switch to ColumnReader.GetValue in future to utilize more precise mapping
					// based on column mapping attributes
					var drParam            = _code.LambdaParameter(
						_code.Name(STORED_PROCEDURE_CUSTOM_MAPPER_PARAMETER),
						// TODO: add IDataReader support here for linq2db v3
						WellKnownTypes.System.Data.Common.DbDataReader);
					var initializers       = new CodeAssignmentStatement[customTable!.Columns.Count];
					var lambda             = _code
						.Lambda(WellKnownTypes.System.Func(returnElementType, WellKnownTypes.System.Data.Common.DbDataReader), true)
							.Parameter(drParam);
					queryProcParameters[1] = lambda.Method;

					// build mapping expressions for each column
					var mappingSchema = _code.Member(ctxParam.Reference, WellKnownTypes.LinqToDB.IDataContext_MappingSchema);
					for (var i = 0; i < customTable.Columns.Count; i++)
					{
						var prop        = customRecordProperties![i];
						initializers[i] = _code.Assign(
							prop.Reference,
							_code.Call(
								new CodeTypeReference(WellKnownTypes.LinqToDB.Common.Converter),
								WellKnownTypes.LinqToDB.Common.Converter_ChangeTypeTo,
								prop.Type.Type,
								new[] { prop.Type.Type },
								_code.Call(
									drParam.Reference,
									WellKnownTypes.System.Data.Common.DbDataReader_GetValue,
									WellKnownTypes.System.ObjectNullable,
									_code.Constant(i, true)),
								mappingSchema));
					}

					lambda
						.Body()
							.Append(
								_code.Return(
									_code.New(
										returnElementType,
										Array.Empty<ICodeExpression>(),
										initializers)));

					queryProcTypeArgs = Array.Empty<IType>();
				}
				else
				{
					// use built-in record mapping by column names
					queryProcParameters = new ICodeExpression[hasParameters ? 3 : 2];
					queryProcTypeArgs   = new[] { returnElementType };
				}

				queryProcParameters[0] = ctxParam.Reference;
				queryProcParameters[queryProcParameters.Length - (hasParameters ? 2 : 1)] = _code.Constant(BuildFunctionName(storedProcedure.Name), true);
				if (hasParameters)
					queryProcParameters[queryProcParameters.Length - 1] = parametersVar!.Reference;

				method.Returns(
					_dataModel.GenerateProcedureResultAsList
						? WellKnownTypes.System.Collections.Generic.List(returnElementType)
						: WellKnownTypes.System.Collections.Generic.IEnumerable(returnElementType));

				// generated QueryProc call
				returnValue = _code.ExtCall(
					WellKnownTypes.LinqToDB.Data.DataConnectionExtensions,
					WellKnownTypes.LinqToDB.Data.DataConnectionExtensions_QueryProc,
					WellKnownTypes.System.Collections.Generic.IEnumerable(returnElementType),
					queryProcTypeArgs,
					queryProcParameters);

				// generate ToList materialization call in two cases:
				// - when return type of mapping is List<T>
				// - when procedure has non-input parameters, because parameter values only available after
				//   full data set read from data reader
				if (_dataModel.GenerateProcedureResultAsList
					|| parameterRebinds?.Length > 0)
				{
					returnValue = _code.ExtCall(
						WellKnownTypes.System.Linq.Enumerable,
						WellKnownTypes.System.Linq.Enumerable_ToList,
						WellKnownTypes.System.Collections.Generic.List(returnElementType),
						new[] { returnElementType },
						returnValue);
				}
			}

			// if procedure contains non-input parameters, we need to read their values from DataParameter
			// and bind to mapping method parameters
			if (parameterRebinds?.Length > 0)
			{
				// save API call to variable
				var callProcVar = _code.Variable(
					_code.Name(STORED_PROCEDURE_RETURN_VARIABLE),
					method.Method.ReturnType!.Type,
					true);
				body.Append(_code.Assign(callProcVar, returnValue));

				// emit rebind statements
				foreach (var rebind in parameterRebinds)
					body.Append(rebind);

				// return result value
				body.Append(_code.Return(callProcVar.Reference));
			}
			else
				body.Append(_code.Return(returnValue));
		}

		/// <summary>
		/// Generates code for stored procedure parameter.
		/// </summary>
		/// <param name="parameter">Parameter definition node.</param>
		/// <param name="returnParameter">Return parameter flag.</param>
		/// <param name="parameterName">Database name of parameter.</param>
		/// <param name="dataType"><see cref="DataType"/>, associated with parameter.</param>
		/// <param name="dbType">Database paramer type.</param>
		/// <param name="parametersVar">Array variable with parameters.</param>
		/// <param name="parameterIndex">Index of parametwer in variable array.</param>
		/// <param name="parameterRebinds">Array of parameter value rebind statements.</param>
		/// <param name="rebindIndex">Index if parameter in rebind array.</param>
		/// <returns></returns>
		private ICodeExpression BuildProcedureParameter(
			CodeParameter             parameter,
			bool                      returnParameter,
			string?                   parameterName,
			DataType?                 dataType,
			DatabaseType?             dbType,
			CodeVariable              parametersVar,
			int                       parameterIndex,
			CodeAssignmentStatement[] parameterRebinds,
			int                       rebindIndex)
		{
			// DataParameter constructor arguments
			var ctorParams = new ICodeExpression[dataType != null ? 3 : 2];

			ctorParams[0] = _code.Constant(parameterName ?? string.Format(STORED_PROCEDURE_PARAMETER_TEMPLATE, parameterIndex), true);
			// pass parameter value for in and inout parameters
			// otherwise pass null
			ctorParams[1] = parameter.Direction == Model.ParameterDirection.In || parameter.Direction == Model.ParameterDirection.Ref ? parameter.Reference : _code.Null(WellKnownTypes.System.ObjectNullable, true);
			if (dataType != null)
				ctorParams[2] = _code.Constant(dataType.Value, true);

			// DataParameter initialization statements
			// calculate initializers count to allocate array or known size intead of list to array conversion
			var initializersCount = 0;
			if (parameter.Direction != Model.ParameterDirection.In)
				initializersCount++;
			if (dbType != null)
			{
				if (dbType.Name != null && _dataModel.GenerateProcedureParameterDbType)
					initializersCount++;
				if (dbType.Length != null && dbType.Length >= int.MinValue && dbType.Length <= int.MaxValue)
					initializersCount++;
				if (dbType.Precision != null)
					initializersCount++;
				if (dbType.Scale != null)
					initializersCount++;
			}

			var ctorInitializers = new CodeAssignmentStatement[initializersCount];
			var initializersIdx  = 0;

			if (parameter.Direction != Model.ParameterDirection.In)
			{
				System.Data.ParameterDirection direction = default;

				if (parameter.Direction == Model.ParameterDirection.Out && !returnParameter)
					direction = System.Data.ParameterDirection.Output;
				else if (parameter.Direction == Model.ParameterDirection.Ref)
					direction = System.Data.ParameterDirection.InputOutput;
				else if (returnParameter)
					direction = System.Data.ParameterDirection.ReturnValue;

				ctorInitializers[initializersIdx] = _code.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Direction, _code.Constant(direction, true));
				initializersIdx++;
			}

			if (dbType != null)
			{
				if (dbType.Name != null && _dataModel.GenerateProcedureParameterDbType)
				{
					ctorInitializers[initializersIdx] = _code.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_DbType, _code.Constant(dbType.Name!, true));
					initializersIdx++;
				}
				// TODO: linq2db refactoring required
				// min/max check added to avoid issues with type inconsistance in schema API and metadata
				// (in one place we use long, in another int for type size)
				if (dbType.Length != null && dbType.Length >= int.MinValue && dbType.Length <= int.MaxValue)
				{
					ctorInitializers[initializersIdx] = _code.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Size, _code.Constant((int)dbType.Length.Value, true));
					initializersIdx++;
				}
				if (dbType.Precision != null)
				{
					ctorInitializers[initializersIdx] = _code.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Precision, _code.Constant(dbType.Precision.Value, true));
					initializersIdx++;
				}
				if (dbType.Scale != null)
				{
					ctorInitializers[initializersIdx] = _code.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Scale, _code.Constant(dbType.Scale.Value, true));
					initializersIdx++;
				}
			}

			// for returning parameter generate rebind statement
			if (parameter.Direction != Model.ParameterDirection.In)
			{
				parameterRebinds[rebindIndex] = _code.Assign(
					parameter.Reference,
					_code.Call(
						new CodeTypeReference(WellKnownTypes.LinqToDB.Common.Converter),
						WellKnownTypes.LinqToDB.Common.Converter_ChangeTypeTo,
						parameter.Type.Type,
						new[] { parameter.Type.Type },
						_code.Member(
							_code.Index(
								parametersVar.Reference,
								_code.Constant(parameterIndex, true),
								WellKnownTypes.LinqToDB.Data.DataParameter),
							WellKnownTypes.LinqToDB.Data.DataParameter_Value)));
			}

			return _code.New(WellKnownTypes.LinqToDB.Data.DataParameter, ctorParams, ctorInitializers);
		}
	}
}
