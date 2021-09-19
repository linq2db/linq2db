using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.CodeGen.Model;
using LinqToDB.Expressions;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildTableFunction(
			TableFunctionModel tableFunction,
			RegionGroup functionsGroup,
			CodeClass context)
		{
			var region = functionsGroup.New(tableFunction.Method.Name);

			if (tableFunction.Error != null)
			{
				if (_dataModel.GenerateProceduresSchemaError)
					region.Pragmas().Add(_code.Error(tableFunction.Error));

				// compared to procedures, table functions with errors cannot be generated
				return;
			}

			var (customTable, entity) = tableFunction.Result;
			if (customTable == null && entity == null)
				return;

			var methodInfo = region.Fields(false).New(_code.Identifier(tableFunction.MethodInfoFieldName), _code.Type(typeof(MethodInfo), false))
				.Private()
				.Static()
				.ReadOnly();

			var method = DefineMethod(region.Methods(false), tableFunction.Method, false);

			_metadataBuilder.BuildTableFunctionMetadata(tableFunction.Metadata, method);


			IType returnEntity;
			if (entity != null)
				returnEntity = _entityBuilders[entity].Type.Type;
			else
				returnEntity = BuildCustomResultClass(customTable!, region, true).resultClassBuilder.Type.Type;

			// T4 used ITable<> here, but I don't see a good reason to generate it as ITable<>
			var returnType = _code.Type(_dataModel.TableFunctionReturnsTable ? typeof(ITable<>) : typeof(IQueryable<>), false, returnEntity);
			method.Returns(returnType);

			var parameters = new List<ICodeExpression>();
			parameters.Add(context.This);
			parameters.Add(context.This);
			parameters.Add(methodInfo.Field.Reference);

			var defaultParameters = new List<ICodeExpression>();
			foreach (var param in tableFunction.Parameters)
			{
				var parameter = DefineParameter(method, param.Parameter);
				parameters.Add(parameter.Reference);
				// TODO: target-typed default could cause errors with overloads?
				defaultParameters.Add(_code.Default(param.Parameter.Type, true));
			}

			method.Body()
				.Append(
					_code.Return(
						_code.ExtCall(
							_code.Type(typeof(DataExtensions), false),
							_code.Identifier(nameof(DataExtensions.GetTable)),
							new[] { returnEntity },
							parameters.ToArray(),
							WellKnownTypes.LinqToDB.ITable(returnEntity))));

			var lambdaParam = _code.LambdaParameter(_code.Identifier("ctx"), context.Type);
			// Expression<Func<context, returnType>>
			var lambda = _code.Lambda(WellKnownTypes.Expression(WellKnownTypes.Func(returnType, context.Type)), true)
				.Parameter(lambdaParam);
			lambda.Body().
				Append(_code.Return(_code.Call(
					lambdaParam.Reference,
					method.Method.Name,
					Array.Empty<IType>(),
					defaultParameters.ToArray(),
					returnType)));

			methodInfo.AddInitializer(_code.Call(
					new CodeTypeReference(_code.Type(typeof(MemberHelper), false)),
					_code.Identifier(nameof(MemberHelper.MethodOf)),
					new[] { functionsGroup.OwnerType.Type },
					new ICodeExpression[] { lambda.Method },
					WellKnownTypes.MethodInfo));

			// TODO: similar tables
		}
	}
}
