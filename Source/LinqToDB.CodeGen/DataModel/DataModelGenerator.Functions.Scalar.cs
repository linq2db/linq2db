using System;
using LinqToDB.CodeGen.Model;
using LinqToDB.Mapping;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildScalarFunction(
			ScalarFunctionModel function,
			Func<RegionGroup> scalarsGroup,
			Func<(BlockBuilder cctorBody, CodeReference schema)> getSchemaConfigurator)
		{
			var region = scalarsGroup().New(function.Method.Name);
			var method = DefineMethod(region.Methods(false), function.Method, false);

			var body = method.Body().Append(
				_code.Throw(
					_code.New(
						_code.Type(typeof(InvalidOperationException), false),
						Array.Empty<ICodeExpression>(),
						Array.Empty<CodeAssignmentStatement>())));

			_metadataBuilder.BuildFunctionMetadata(function.Metadata, method);

			IType returnType;
			if (function.Return != null)
				returnType = function.Return;
			else
			{
				// T4 generated this class inside of context class, here we move it to function region
				var tupleClassBuilder = DefineClass(function.ReturnTuple!.Class, region.Classes());
				var tuplePropsRegion = tupleClassBuilder.Properties(true);

				var initializers = new CodeAssignmentStatement[function.ReturnTuple.Fields.Count];

				var lambdaParam = _code.LambdaParameter(_code.Identifier("tuple"), _code.ArrayType(WellKnownTypes.Object.WithNullability(true), false));

				for (var i = 0; i < function.ReturnTuple!.Fields.Count; i++)
				{
					var field = function.ReturnTuple!.Fields[i];

					var property = DefineProperty(tuplePropsRegion, field.Property);

					initializers[i] = _code.Assign(
						property.Property.Reference,
						_code.Cast(property.Property.Type.Type, _code.Index(lambdaParam.Reference, _code.Constant(i, true), WellKnownTypes.Object.WithNullability(true))));
				}

				var conversionLambda = _code
						.Lambda(WellKnownTypes.LambdaExpression, true)
						.Parameter(lambdaParam);

				conversionLambda.Body().Append(_code.Return(_code.New(tupleClassBuilder.Type.Type, Array.Empty<ICodeExpression>(), initializers)));

				var (initializer, schema) = getSchemaConfigurator();
				initializer
					.Append(
					_code.Call(
						schema,
						_code.Identifier(nameof(MappingSchema.SetConvertExpression)),
						new IType[]
						{
								_code.ArrayType(_code.Type(typeof(object), true), false),
								tupleClassBuilder.Type.Type
						},
						new ICodeExpression[] { conversionLambda.Method }));

				returnType = tupleClassBuilder.Type.Type;
				if (function.ReturnTuple.CanBeNull)
					returnType = returnType.WithNullability(true);
			}

			method.Returns(returnType);

			foreach (var param in function.Parameters)
				DefineParameter(method, param.Parameter);
		}
	}
}
