using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Schema;

namespace LinqToDB.DataModel
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
		/// <param name="context">Model generation context.</param>
		/// <param name="storedProcedure">Stored procedure model.</param>
		private static void BuildStoredProcedure(IDataModelGenerationContext context, StoredProcedureModel storedProcedure)
		{
			// TODO: refactor procedures generation logic. it became chaotic after async support added

			// generated code sample (without async version):
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
				if (context.Options.GenerateProceduresSchemaError)
				{
					(region = context.AddStoredProcedureRegion(storedProcedure.Method.Name))
						.Pragmas()
							.Add(context.AST.Error(storedProcedure.Error));
				}

				// even with this error procedure generation could continue, as we still
				// can invoke procedure, we just cannot get resultset from it
				if (context.Options.SkipProceduresWithSchemaErrors)
					return;
			}

			var methodsGroup                       = (region ??= context.AddStoredProcedureRegion(storedProcedure.Method.Name)).Methods(false);
			var useOrdinalMapping                  = false;
			ResultTableModel? customTable          = null;
			IType?           returnElementType     = null;
			// QueryProc type- and regular parameters
			CodeProperty[]? customRecordProperties = null;
			AsyncProcedureResult? asyncResult      = null;
			var classes                            = region.Classes();

			// generate custom result type if needed
			if (storedProcedure.Results.Count > 1)
			{
				// TODO: right now we don't have schema API that could load multiple result sets
				// still it makes sense to implement multiple resultsets generation in future even without
				// schema API, as user could define resultsets manually
				throw new NotImplementedException("Multiple result-sets stored procedure generation not imlpemented yet");
			}
			else if (storedProcedure.Results.Count == 1)
			{
				// for stored procedure call with result set we use QueryProc API
				(customTable, var mappedTable, asyncResult) = storedProcedure.Results[0];

				// if procedure result table contains unique and not empty column names, we use columns mappings
				// otherwise we should bind columns manually by ordinal (as we don't have by-ordinal mapping conventions support)
				useOrdinalMapping = customTable != null
					// number of columns remains same after empty names and duplicates removed?
					&& customTable.Columns.Select(c => c.Metadata.Name).Where(_ => !string.IsNullOrEmpty(_)).Distinct().Count() != customTable.Columns.Count;

				if (customTable != null)
					(returnElementType, customRecordProperties) = BuildCustomResultClass(context, customTable, classes, !useOrdinalMapping);
				else if (mappedTable != null)
					returnElementType = context.GetEntityBuilder(mappedTable).Type.Type;
			}

			if (context.Options.GenerateProcedureSync)
				BuildStoredProcedureMethod(context, storedProcedure, methodsGroup, useOrdinalMapping, customTable, returnElementType, customRecordProperties, classes, null, false);
			if (context.Options.GenerateProcedureAsync)
				BuildStoredProcedureMethod(context, storedProcedure, methodsGroup, useOrdinalMapping, customTable, returnElementType, customRecordProperties, classes, asyncResult, true);
		}

		/// <summary>
		/// Generates sync or async  stored procedure mapping method.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="storedProcedure">Stored procedure model.</param>
		/// <param name="methodsGroup">Method group to add new mapping method.</param>
		/// <param name="useOrdinalMapping">If <c>true</c>, by-ordinal mapping used for result mapping instead of by-name mapping.</param>
		/// <param name="customTable">Custom result record model.</param>
		/// <param name="returnElementType">Type of result record for procedure with result.</param>
		/// <param name="customRecordProperties">Column properties for custom result record type.</param>
		/// <param name="classes">Procedure classes group.</param>
		/// <param name="asyncResult">Optional result class model for async signature.</param>
		/// <param name="async">If <c>true</c>, generate async version of mapping.</param>
		private static void BuildStoredProcedureMethod(
			IDataModelGenerationContext context,
			StoredProcedureModel        storedProcedure,
			MethodGroup                 methodsGroup,
			bool                        useOrdinalMapping,
			ResultTableModel?           customTable,
			IType?                      returnElementType,
			CodeProperty[]?             customRecordProperties,
			ClassGroup                  classes,
			AsyncProcedureResult?       asyncResult,
			bool                        async)
		{
			var hasParameters = storedProcedure.Parameters.Count > 0 || storedProcedure.Return != null;

			// generate ToList materialization call or mark method async in two cases:
			// - when return type of mapping is List<T>
			// - when procedure has non-input parameters
			var hasReturnParameters = storedProcedure.Return != null || storedProcedure.Parameters.Any(p => p.Parameter.Direction != CodeParameterDirection.In);
			var isQueryProc         = storedProcedure.Results.Count == 1 && (storedProcedure.Results[0].Entity != null || storedProcedure.Results[0].CustomTable != null);
			var generateToList      = isQueryProc && (hasReturnParameters || context.Options.GenerateProcedureResultAsList);

			// declare mapping method
			var method = context.DefineMethod(
				methodsGroup,
				storedProcedure.Method,
				async: async,
				withAwait: async && (generateToList || hasReturnParameters));

			// declare data context parameter (extension `this` parameter)
			var ctxParam = context.AST.Parameter(
					// see method notes above regarding type of this parameter
					context.ProcedureContextParameterType,
					context.AST.Name(DataModelConstants.STORED_PROCEDURE_CONTEXT_PARAMETER),
					CodeParameterDirection.In);
			method.Parameter(ctxParam);
			var body = method.Body();

			// array of procedure parameters (DataParameter objects)
			CodeVariable?                            parametersVar             = null;
			CodeAssignmentStatement[]?               parameterRebinds          = null;
			Dictionary<FunctionParameterModel, int>? rebindedParametersIndexes = null;
			if (hasParameters)
			{
				var resultParametersCount = storedProcedure.Parameters.Count(p => p.Parameter.Direction != CodeParameterDirection.In) + (storedProcedure.Return != null ? 1 : 0);
				// bindings of parameter values to output parameters of method after procedure call
				parameterRebinds          = resultParametersCount > 0 ? new CodeAssignmentStatement[resultParametersCount] : [];
				var rebindIndex           = 0;
				if (resultParametersCount > 0)
					rebindedParametersIndexes = new(resultParametersCount);

				// DataParameter collection initialization
				var parameterValues = new ICodeExpression[storedProcedure.Parameters.Count + (storedProcedure.Return != null ? 1 : 0)];
				parametersVar       = context.AST.Variable(context.AST.Name(DataModelConstants.STORED_PROCEDURE_PARAMETERS_VARIABLE), WellKnownTypes.LinqToDB.Data.DataParameterArray, true);

				// build non-return parameters
				for (var i = 0; i < storedProcedure.Parameters.Count; i++)
				{
					var p     = storedProcedure.Parameters[i];
					ILValue? rebindTo = null;
					var rebindRequired = p.Parameter.Direction != CodeParameterDirection.In;

					CodeParameter param;
					if (async && p.Parameter.Direction != CodeParameterDirection.In)
						param = context.DefineParameter(method, p.Parameter.WithDirection(CodeParameterDirection.In));
					else
						param = context.DefineParameter(method, p.Parameter);

					if (rebindRequired)
						rebindTo = param.Reference;

					parameterValues[i] = BuildProcedureParameter(
						context,
						param,
						param.Type.Type,
						p.Direction,
						rebindTo,
						p.DbName,
						p.DataType,
						p.Type,
						parametersVar,
						i,
						parameterRebinds!,
						rebindIndex);

					if (p.Parameter.Direction != CodeParameterDirection.In)
					{
						rebindedParametersIndexes!.Add(p, rebindIndex);
						rebindIndex++;
					}
				}

				// build return parameter
				if (storedProcedure.Return != null)
				{
					CodeParameter? param = null;
					if (!async)
						param = context.DefineParameter(method, storedProcedure.Return.Parameter);

					parameterValues[storedProcedure.Parameters.Count] = BuildProcedureParameter(
						context,
						param,
						storedProcedure.Return.Parameter.Type,
						System.Data.ParameterDirection.ReturnValue,
						param?.Reference ?? context.AST.Variable(context.AST.Name("fake"), storedProcedure.Return.Parameter.Type, false).Reference,
						storedProcedure.Return.DbName ?? DataModelConstants.STORED_PROCEDURE_DEFAULT_RETURN_PARAMETER,
						storedProcedure.Return.DataType,
						storedProcedure.Return.Type,
						parametersVar,
						storedProcedure.Parameters.Count,
						parameterRebinds!,
						rebindIndex);
					rebindedParametersIndexes!.Add(storedProcedure.Return, rebindIndex);
				}

				var parametersArray = context.AST.Assign(
					parametersVar,
					context.AST.Array(WellKnownTypes.LinqToDB.Data.DataParameter, true, false, parameterValues));
				body.Append(parametersArray);
			}

			CodeParameter? cancellationTokenParameter = null;
			if (async)
				cancellationTokenParameter = context.DefineParameter(
					method,
					new ParameterModel(DataModelConstants.CANCELLATION_TOKEN_PARAMETER, WellKnownTypes.System.Threading.CancellationToken, CodeParameterDirection.In),
					context.AST.Default(WellKnownTypes.System.Threading.CancellationToken, true));

			ICodeExpression? returnValue = null;

			IType returnType;
			var resultVarSet = false;

			if (storedProcedure.Results.Count == 0 || (storedProcedure.Results.Count == 1 && storedProcedure.Results[0].CustomTable == null && storedProcedure.Results[0].Entity == null))
			{
				// for stored procedure call without result set we use ExecuteProc API
				// prepare call parameters
				var parametersCount       = (hasParameters ? 3 : 2) + (async ? 1 : 0);
				var executeProcParameters = new ICodeExpression[parametersCount];
				executeProcParameters[0]  = ctxParam.Reference;
				executeProcParameters[1]  = context.AST.Constant(context.MakeFullyQualifiedRoutineName(storedProcedure.Name), true);

				if (async)
					executeProcParameters[2] = cancellationTokenParameter!.Reference;
				if (hasParameters)
					executeProcParameters[async ? 3 : 2] = parametersVar!.Reference;

				returnType = WellKnownTypes.System.Int32;
				if (async)
				{
					returnValue = context.AST.ExtCall(
						WellKnownTypes.LinqToDB.Data.DataContextExtensions,
						WellKnownTypes.LinqToDB.Data.DataContextExtensions_ExecuteProcAsync,
						WellKnownTypes.System.Int32,
						executeProcParameters);

					if (asyncResult != null)
					{
						resultVarSet = true;
						var rowCountVar = context.AST.Variable(
							context.AST.Name(DataModelConstants.STORED_PROCEDURE_RESULT_VARIABLE),
							WellKnownTypes.System.Int32,
							true);
						body.Append(context.AST.Assign(rowCountVar, context.AST.AwaitExpression(returnValue)));
						returnValue = rowCountVar.Reference;
					}
				}
				else
				{
					returnValue = context.AST.ExtCall(
						WellKnownTypes.LinqToDB.Data.DataContextExtensions,
						WellKnownTypes.LinqToDB.Data.DataContextExtensions_ExecuteProc,
						WellKnownTypes.System.Int32,
						executeProcParameters);
				}
			}
			else if (storedProcedure.Results.Count == 1)
			{
				// QueryProc type- and regular parameters
				IType[]           queryProcTypeArgs;
				ICodeExpression[] queryProcParameters;

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

					queryProcParameters = new ICodeExpression[(hasParameters ? 4 : 3) + (async ? 1 : 0)];

					// generate positional mapping lambda
					// TODO: switch to ColumnReader.GetValue in future to utilize more precise mapping
					// based on column mapping attributes
					var drParam            = context.AST.LambdaParameter(
						context.AST.Name(DataModelConstants.STORED_PROCEDURE_CUSTOM_MAPPER_PARAMETER),
						// TODO: add IDataReader support here for linq2db v3
						WellKnownTypes.System.Data.Common.DbDataReader);
					var initializers       = new CodeAssignmentStatement[customTable!.Columns.Count];
					var lambda             = context.AST
						.Lambda(WellKnownTypes.System.Func(returnElementType!, WellKnownTypes.System.Data.Common.DbDataReader), true)
							.Parameter(drParam);
					queryProcParameters[1] = lambda.Method;

					// build mapping expressions for each column
					var mappingSchema = context.AST.Member(ctxParam.Reference, WellKnownTypes.LinqToDB.IDataContext_MappingSchema);
					for (var i = 0; i < customTable.Columns.Count; i++)
					{
						var prop        = customRecordProperties![i];
						initializers[i] = context.AST.Assign(
							prop.Reference,
							context.AST.Call(
								new CodeTypeReference(WellKnownTypes.LinqToDB.Common.Converter),
								WellKnownTypes.LinqToDB.Common.Converter_ChangeTypeTo,
								prop.Type.Type,
								new[] { prop.Type.Type },
								false,
								context.AST.Call(
									drParam.Reference,
									WellKnownTypes.System.Data.Common.DbDataReader_GetValue,
									WellKnownTypes.System.ObjectNullable,
									context.AST.Constant(i, true)),
								mappingSchema));
					}

					lambda
						.Body()
							.Append(
								context.AST.Return(
									context.AST.New(
										returnElementType!,
										[],
										initializers)));

					queryProcTypeArgs = [];
				}
				else
				{
					// use built-in record mapping by column names
					queryProcParameters = new ICodeExpression[(hasParameters ? 3 : 2) + (async ? 1 : 0)];
					queryProcTypeArgs = new[] { returnElementType! };
				}

				queryProcParameters[0] = ctxParam.Reference;
				queryProcParameters[^((hasParameters ? 2 : 1) + (async ? 1 : 0))] = context.AST.Constant(context.MakeFullyQualifiedRoutineName(storedProcedure.Name), true);
				if (async)
					queryProcParameters[hasParameters ? ^2 : ^1] = cancellationTokenParameter!.Reference;
				if (hasParameters)
					queryProcParameters[^1] = parametersVar!.Reference;

				returnType = context.Options.GenerateProcedureResultAsList
					? WellKnownTypes.System.Collections.Generic.List(returnElementType!)
					: WellKnownTypes.System.Collections.Generic.IEnumerable(returnElementType!);

				// generated QueryProc call
				if (async)
				{
					returnValue = context.AST.ExtCall(
						WellKnownTypes.LinqToDB.Data.DataContextExtensions,
						WellKnownTypes.LinqToDB.Data.DataContextExtensions_QueryProcAsync,
						WellKnownTypes.System.Threading.Tasks.Task(WellKnownTypes.System.Collections.Generic.IEnumerable(returnElementType!)),
						queryProcTypeArgs,
						false,
						queryProcParameters);
				}
				else
				{
					returnValue = context.AST.ExtCall(
						WellKnownTypes.LinqToDB.Data.DataContextExtensions,
						WellKnownTypes.LinqToDB.Data.DataContextExtensions_QueryProc,
						WellKnownTypes.System.Collections.Generic.IEnumerable(returnElementType!),
						queryProcTypeArgs,
						false,
						queryProcParameters);
				}

				if (generateToList)
				{
					if (async)
					{
						resultVarSet = true;
						var listVar = context.AST.Variable(
							context.AST.Name(DataModelConstants.STORED_PROCEDURE_RESULT_VARIABLE),
							WellKnownTypes.System.Collections.Generic.List(returnElementType!),
							true);
						body.Append(context.AST.Assign(listVar, context.AST.AwaitExpression(returnValue)));
						returnValue = context.AST.ExtCall(
							WellKnownTypes.System.Linq.Enumerable,
							WellKnownTypes.System.Linq.Enumerable_ToList,
							WellKnownTypes.System.Collections.Generic.List(returnElementType!),
							new[] { returnElementType! },
							true,
							listVar.Reference);
					}
					else
					{
						returnValue = context.AST.ExtCall(
							WellKnownTypes.System.Linq.Enumerable,
							WellKnownTypes.System.Linq.Enumerable_ToList,
							WellKnownTypes.System.Collections.Generic.List(returnElementType!),
							new[] { returnElementType! },
							true,
							returnValue);
					}
				}
			}
			// unreachable currently
			else
				throw new NotImplementedException();

			// if procedure contains non-input parameters, we need to read their values from DataParameter
			// and bind to mapping method parameters
			if (parameterRebinds?.Length > 0)
			{
				var result = returnValue;
				if (!resultVarSet)
				{
					// save API call to variable
					var callProcVar = context.AST.Variable(
						context.AST.Name(DataModelConstants.STORED_PROCEDURE_RETURN_VARIABLE),
						returnType,
						true);
						body.Append(context.AST.Assign(callProcVar, returnValue!));
					result = callProcVar.Reference;
				}

				if (async && asyncResult != null)
				{
					// as async methods cannot have ref/out parameters, we generate result class to contain out/ref/return parameters and result set
					var resultClassBuilder = context.DefineClass(classes, asyncResult.Class);
					var properties         = resultClassBuilder.Properties(true);
					var initializers       = new CodeAssignmentStatement[parameterRebinds.Length + 1];

					asyncResult.MainResult.Type = returnType;
					var prop                    = context.DefineProperty(properties, asyncResult.MainResult);
					initializers[0]             = context.AST.Assign(prop.Property.Reference, result);

					// order parameters to always generate properties in same order
					var idx = 0;
					foreach (var parameter in asyncResult.ParameterProperties.OrderBy(k => k.Value.Name))
					{
						prop                  = context.DefineProperty(properties, parameter.Value);
						initializers[idx + 1] = context.AST.Assign(prop.Property.Reference, parameterRebinds[rebindedParametersIndexes![parameter.Key]].RValue);
						idx++;
					}

					// TODO: return type update
					body.Append(context.AST.Return(context.AST.New(resultClassBuilder.Type.Type, [], initializers)));

					returnType = resultClassBuilder.Type.Type;
				}
				else
				{
					// emit rebind statements
					foreach (var rebind in parameterRebinds)
						body.Append(rebind);

					// return result value
					body.Append(context.AST.Return(result));
				}
			}
			else
				body.Append(context.AST.Return(returnValue));

			if (async)
				returnType = WellKnownTypes.System.Threading.Tasks.Task(returnType);

			method.Returns(returnType);
		}

		/// <summary>
		/// Generates code for stored procedure parameter.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="parameter">Parameter definition node.</param>
		/// <param name="valueType">Parameter value type.</param>
		/// <param name="direction">Parameter direction enum value.</param>
		/// <param name="rebindTo">lvalue to store out/ref parameter value.</param>
		/// <param name="parameterName">Database name of parameter.</param>
		/// <param name="dataType"><see cref="DataType"/>, associated with parameter.</param>
		/// <param name="dbType">Database paramer type.</param>
		/// <param name="parametersVar">Array variable with parameters.</param>
		/// <param name="parameterIndex">Index of parametwer in variable array.</param>
		/// <param name="parameterRebinds">Array of parameter value rebind statements.</param>
		/// <param name="rebindIndex">Index if parameter in rebind array.</param>
		/// <returns></returns>
		private static ICodeExpression BuildProcedureParameter(
			IDataModelGenerationContext    context,
			CodeParameter?                 parameter,
			IType                          valueType,
			System.Data.ParameterDirection direction,
			ILValue?                       rebindTo,
			string?                        parameterName,
			DataType?                      dataType,
			DatabaseType?                  dbType,
			CodeVariable                   parametersVar,
			int                            parameterIndex,
			CodeAssignmentStatement[]      parameterRebinds,
			int                            rebindIndex)
		{
			// DataParameter constructor arguments
			var ctorParams = new ICodeExpression[dataType != null ? 3 : 2];

			ctorParams[0] = context.AST.Constant(parameterName ?? string.Format(CultureInfo.InvariantCulture, DataModelConstants.STORED_PROCEDURE_PARAMETER_TEMPLATE, parameterIndex), true);
			// pass parameter value for in and inout parameters
			// otherwise pass null
			ctorParams[1] = direction is System.Data.ParameterDirection.Input or System.Data.ParameterDirection.InputOutput
				? parameter!.Reference
				: context.AST.Null(WellKnownTypes.System.ObjectNullable, true);
			if (dataType != null)
				ctorParams[2] = context.AST.Constant(dataType.Value, true);

			// DataParameter initialization statements
			// calculate initializers count to allocate array or known size intead of list to array conversion
			var initializersCount = 0;
			if (direction != System.Data.ParameterDirection.Input)
				initializersCount++;
			if (dbType != null)
			{
				if (dbType.Name != null && context.Options.GenerateProcedureParameterDbType)
					initializersCount++;
				if (dbType.Length is not null and >= int.MinValue and <= int.MaxValue)
					initializersCount++;
				if (dbType.Precision != null)
					initializersCount++;
				if (dbType.Scale != null)
					initializersCount++;
			}

			var ctorInitializers = new CodeAssignmentStatement[initializersCount];
			var initializersIdx  = 0;

			if (direction != System.Data.ParameterDirection.Input)
			{
				ctorInitializers[initializersIdx] = context.AST.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Direction, context.AST.Constant(direction, true));
				initializersIdx++;
			}

			if (dbType != null)
			{
				if (dbType.Name != null && context.Options.GenerateProcedureParameterDbType)
				{
					ctorInitializers[initializersIdx] = context.AST.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_DbType, context.AST.Constant(dbType.Name!, true));
					initializersIdx++;
				}

				if (dbType.Length != null)
				{
					ctorInitializers[initializersIdx] = context.AST.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Size, context.AST.Constant(dbType.Length.Value, true));
					initializersIdx++;
				}

				if (dbType.Precision != null)
				{
					ctorInitializers[initializersIdx] = context.AST.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Precision, context.AST.Constant(dbType.Precision.Value, true));
					initializersIdx++;
				}

				if (dbType.Scale != null)
				{
					ctorInitializers[initializersIdx] = context.AST.Assign(WellKnownTypes.LinqToDB.Data.DataParameter_Scale, context.AST.Constant(dbType.Scale.Value, true));
					initializersIdx++;
				}
			}

			// for returning parameter generate rebind statement
			if (rebindTo != null)
			{
				parameterRebinds[rebindIndex] = context.AST.Assign(
					rebindTo,
					context.AST.Call(
						new CodeTypeReference(WellKnownTypes.LinqToDB.Common.Converter),
						WellKnownTypes.LinqToDB.Common.Converter_ChangeTypeTo,
						valueType,
						new[] { valueType },
						false,
						context.AST.Member(
							context.AST.Index(
								parametersVar.Reference,
								context.AST.Constant(parameterIndex, true),
								WellKnownTypes.LinqToDB.Data.DataParameter),
							WellKnownTypes.LinqToDB.Data.DataParameter_Value)));
			}

			return context.AST.New(WellKnownTypes.LinqToDB.Data.DataParameter, ctorParams, ctorInitializers);
		}
	}
}
