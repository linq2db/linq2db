using LinqToDB.CodeModel;
using LinqToDB.Common;

namespace LinqToDB.DataModel;

// contains generation logic for scalar function mappings
partial class DataModelGenerator
{
	/// <summary>
	/// Generates aggregate function mapping.
	/// </summary>
	/// <param name="function">Function model.</param>
	/// <param name="scalarsGroup">Functions region.</param>
	/// <param name="getSchemaConfigurator">Mapping schema initializer provider.</param>
	private void BuildScalarFunction(
		ScalarFunctionModel                                  function,
		Func<RegionGroup>                                    scalarsGroup,
		Func<(BlockBuilder cctorBody, CodeReference schema)> getSchemaConfigurator)
	{
		// generation sample
		/*
		 * [Sql.Function("TestScalarFunction", ServerSideOnly = true)]
		 * public static string? TestScalarFunction()
		 * {
		 *     throw new InvalidOperationException();
		 * }
		 */

		// additionally we can generate mapping schema mappings from object[] to result record class
		// for functions that return tuples
		// currently this is Npgsql/PostgreSQL specific functionality
		// custom mapping class generated in same region as function
		/*
		 * MappingSchema.SetConvertExpression<object?[], TestFunctionParametersResult>(
		 *    tuple => new TestFunctionParametersResult()
		 *    {
		 *        param2 = (int?)tuple[0],
		 *        param3 = (int?)tuple[1]
		 *    });
		 */

		var region = scalarsGroup().New(function.Method.Name);
		var method = DefineMethod(region.Methods(false), function.Method);

		// scalar functions cannot be used outside of query context, so we throw exception from method
		var body = method.Body().Append(
			AST.Throw(
				AST.New(
					WellKnownTypes.System.InvalidOperationException,
					AST.Constant(EXCEPTION_QUERY_ONLY_SCALAR_CALL, true))));

		// build mappings
		_metadataBuilder.BuildFunctionMetadata(function.Metadata, method);

		IType returnType;
		if (function.ReturnTuple != null)
		{
			// generate custom record class for result tuple
			// T4 generated this class inside of context class, here we move it to function region
			var tupleClassBuilder = DefineClass(region.Classes(), function.ReturnTuple!.Class);
			var tuplePropsRegion  = tupleClassBuilder.Properties(true);

			// mapping expression tuple fields converters
			var initializers = new CodeAssignmentStatement[function.ReturnTuple.Fields.Count];

			// parameter of mapping expression to map tuple (returned as object[] from npgsql)
			// to custom class
			var lambdaParam = AST.LambdaParameter(
				AST.Name(SCALAR_TUPLE_MAPPING_PARAMETER),
				AST.ArrayType(WellKnownTypes.System.ObjectArrayNullable, false));

			// generate tuple field property and mapping converter
			for (var i = 0; i < function.ReturnTuple!.Fields.Count; i++)
			{
				var field = function.ReturnTuple!.Fields[i];

				var property = DefineProperty(tuplePropsRegion, field.Property);

				initializers[i] = AST.Assign(
					property.Property.Reference,
					AST.Cast(
						property.Property.Type.Type,
						AST.Index(
							lambdaParam.Reference,
							AST.Constant(i, true),
							WellKnownTypes.System.ObjectNullable)));
			}

			var conversionLambda = AST
					.Lambda(WellKnownTypes.System.Linq.Expressions.LambdaExpression, true)
					.Parameter(lambdaParam);

			// generate full tuple conversion expression
			conversionLambda
				.Body()
				.Append(
					AST.Return(
						AST.New(
							tupleClassBuilder.Type.Type,
							Array<ICodeExpression>.Empty,
							initializers)));

			// add conversion expression to mapping schema initializer
			var (initializer, schema) = getSchemaConfigurator();
			initializer
				.Append(
				AST.Call(
					schema,
					WellKnownTypes.LinqToDB.Mapping.MappingSchema_SetConvertExpression,
					new IType[]
					{
						AST.ArrayType(WellKnownTypes.System.ObjectNullable, false),
						tupleClassBuilder.Type.Type
					},
					false,
					conversionLambda.Method));

			returnType = tupleClassBuilder.Type.Type.WithNullability(function.ReturnTuple.CanBeNull);
		}
		else
			returnType = function.Return!;

		method.Returns(returnType);

		foreach (var param in function.Parameters)
			DefineParameter(method, param.Parameter);
	}
}
