using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.Model;
using LinqToDB.Configuration;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildDataContextConstructors(
			CodeClass contextClass,
			ConstructorGroup constructors,
			MethodGroup partialMethods,
			CodeIdentifier? initSchemasMethodName)
		{
			var ctors = new List<BlockBuilder>();

			if (_dataModel.DataContext.HasDefaultConstructor)
				ctors.Add(constructors.New().Public().Body());
			if (_dataModel.DataContext.HasConfigurationConstructor)
			{
				var configurationParam = _code.Parameter(_code.Type(typeof(string), false), _code.Identifier("configuration"), ParameterDirection.In);

				ctors.Add(constructors.New()
							.Parameter(configurationParam)
							.Public()
							.Base(configurationParam.Reference)
							.Body());
			}
			if (_dataModel.DataContext.HasUntypedOptionsConstructor)
			{
				var optionsParam = _code.Parameter(_code.Type(typeof(LinqToDbConnectionOptions), false), _code.Identifier("options"), ParameterDirection.In);
				ctors.Add(constructors.New()
							.Parameter(optionsParam)
							.Public()
							.Base(optionsParam.Reference)
							.Body());
			}
			if (_dataModel.DataContext.HasTypedOptionsConstructor)
			{
				var typedOptionsParam = _code.Parameter(_code.Type(typeof(LinqToDbConnectionOptions<>), false, contextClass.Type), _code.Identifier("options"), ParameterDirection.In);
				ctors.Add(constructors.New()
							.Parameter(typedOptionsParam)
							.Public()
							.Base(typedOptionsParam.Reference)
							.Body());
			}

			var initDataContext = partialMethods.New(_code.Identifier("InitDataContext")).Partial();

			foreach (var body in ctors)
			{
				if (initSchemasMethodName != null)
					body.Append(_code.Call(contextClass.This, initSchemasMethodName, Array.Empty<IType>(), Array.Empty<ICodeExpression>()));
				body.Append(_code.Call(contextClass.This, initDataContext.Method.Name, Array.Empty<IType>(), Array.Empty<ICodeExpression>()));
			}
		}
	}
}
