using System;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	// contains generation logic for table function mappings
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates table function mapping.
		/// </summary>
		/// <param name="tableFunction">Function model.</param>
		/// <param name="functionsGroup">Functions region.</param>
		/// <param name="context">Data context class.</param>
		private void BuildTableFunction(
			TableFunctionModel tableFunction,
			RegionGroup        functionsGroup,
			CodeClass          context)
		{
			// generated code sample:
			/*
			 * #region Function1
			 * private static readonly MethodInfo _function1 = MemberHelper.MethodOf<DataContext>(ctx => ctx.Function1(default));
			 * 
			 * [Sql.TableFunction("Function1")]
			 * public IQueryable<Parent> Function1(int? id)
			 * {
			 * return this.GetTable<Parent>(this, _function1, id);
			 * }
			 * #endregion
			 */

			// create function region
			var region = functionsGroup.New(tableFunction.Method.Name);

			// if function schema load failed, generate error pragma with exception details
			if (tableFunction.Error != null)
			{
				if (_dataModel.GenerateProceduresSchemaError)
					region.Pragmas().Add(_code.Error($"Failed to load return table schema: {tableFunction.Error}"));

				// as we cannot generate table function without knowing it's schema, we skip failed function
				return;
			}

			// if function result schema matches known entity, we use entity class for result
			// otherwise we generate custom record mapping
			var (customTable, entity) = tableFunction.Result;
			if (customTable == null && entity == null)
				throw new InvalidOperationException($"Table function {tableFunction.Name} result table not set");

			// GetTable API for table functions need MethodInfo instance of generated method as parameter
			// to not load it on each call, we cache MethodInfo instance in static field
			var methodInfo = region
				.Fields(false)
					.New(_code.Name(tableFunction.MethodInfoFieldName), WellKnownTypes.System.Reflection.MethodInfo)
						.Private()
						.Static()
						.ReadOnly();

			// generate mapping method with metadata
			var method = DefineMethod(region.Methods(false), tableFunction.Method);
			_metadataBuilder.BuildTableFunctionMetadata(tableFunction.Metadata, method);

			// generate method parameters, return type and body

			// table record type
			IType returnEntity;
			if (entity != null)
				returnEntity = _entityBuilders[entity].Type.Type;
			else
				returnEntity = BuildCustomResultClass(customTable!, region, true).resultClassType;

			// set return type
			// T4 used ITable<T> for return type, but there is no reason to use ITable<T> over IQueryable<T>
			// Even more: ITable<T> is not correct return type here
			var returnType = _dataModel.TableFunctionReturnsTable
				? WellKnownTypes.LinqToDB.ITable(returnEntity)
				: WellKnownTypes.System.Linq.IQueryable(returnEntity);
			method.Returns(returnType);

			// parameters for GetTable call in mapping body
			var parameters = new ICodeExpression[3 + tableFunction.Parameters.Count];
			parameters[0] = context.This; // `this` extension method parameter
			parameters[1] = context.This; // context parameter
			parameters[2] = methodInfo.Field.Reference; // method info field

			// add table function parameters (if any)
			var fieldInitParameters = new ICodeExpression[tableFunction.Parameters.Count];
			for (var i = 0; i < tableFunction.Parameters.Count; i++)
			{
				var param = tableFunction.Parameters[i];
				// parameters added to 3 places:
				// - to mapping method
				// - to GetTable call in mapping
				// - to mapping call in MethodInfo initializer we add parameter's default value
				var parameter = DefineParameter(method, param.Parameter);
				parameters[i + 3] = parameter.Reference;
				// TODO: potential issue: target-typed `default` could cause errors with overloads
				fieldInitParameters[i] = _code.Default(param.Parameter.Type, true);
			}

			// generate mapping body
			method.Body()
				.Append(
					_code.Return(
						_code.ExtCall(
							WellKnownTypes.LinqToDB.DataExtensions,
							WellKnownTypes.LinqToDB.DataExtensions_GetTable,
							WellKnownTypes.LinqToDB.ITable(returnEntity),
							new[] { returnEntity },
							parameters)));

			// generate MethodInfo field initializer
			var lambdaParam = _code.LambdaParameter(_code.Name(TABLE_FUNCTION_METHOD_INFO_CONTEXT_PARAMETER), context.Type);

			// Expression<Func<context, returnType>>
			var lambda = _code
				.Lambda(WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(returnType, context.Type)), true)
				.Parameter(lambdaParam);

			lambda.Body()
				.Append(
					_code.Return(
						_code.Call(
							lambdaParam.Reference,
							method.Method.Name,
							returnType,
							fieldInitParameters)));

			methodInfo.AddInitializer(
				_code.Call(
					new CodeTypeReference(WellKnownTypes.LinqToDB.Expressions.MemberHelper),
					WellKnownTypes.LinqToDB.Expressions.MemberHelper_MethodOf,
					WellKnownTypes.System.Reflection.MethodInfo,
					new[] { functionsGroup.OwnerType.Type },
					lambda.Method));

			// TODO: similar tables
		}
	}
}
