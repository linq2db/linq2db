using System;
using LinqToDB.CodeModel;
using LinqToDB.Common;

namespace LinqToDB.DataModel
{
	// contains generation logic for scalar function mappings
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates aggregate function mapping.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="function">Function model.</param>
		private static void BuildScalarFunction(IDataModelGenerationContext context, ScalarFunctionModel function)
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

			var region = context.AddScalarFunctionRegion(function.Method.Name);
			var method = context.DefineMethod(region.Methods(false), function.Method);

			// scalar functions cannot be used outside of query context, so we throw exception from method
			var body = method.Body().Append(
				context.AST.Throw(
					context.AST.New(
						WellKnownTypes.System.InvalidOperationException,
						context.AST.Constant(DataModelConstants.EXCEPTION_QUERY_ONLY_SCALAR_CALL, true))));

			IType returnType;
			if (function.ReturnTuple != null)
			{
				// generate custom record class for result tuple
				// T4 generated this class inside of context class, here we move it to function region
				var tupleClassBuilder = context.DefineClass(region.Classes(), function.ReturnTuple!.Class);
				var tuplePropsRegion  = tupleClassBuilder.Properties(true);

				// mapping expression tuple fields converters
				var initializers = new CodeAssignmentStatement[function.ReturnTuple.Fields.Count];

				// parameter of mapping expression to map tuple (returned as object[] from npgsql)
				// to custom class
				var lambdaParam = context.AST.LambdaParameter(
					context.AST.Name(DataModelConstants.SCALAR_TUPLE_MAPPING_PARAMETER),
					context.AST.ArrayType(WellKnownTypes.System.ObjectArrayNullable, false));

				// generate tuple field property and mapping converter
				for (var i = 0; i < function.ReturnTuple!.Fields.Count; i++)
				{
					var field = function.ReturnTuple!.Fields[i];

					var property = context.DefineProperty(tuplePropsRegion, field.Property);

					initializers[i] = context.AST.Assign(
						property.Property.Reference,
						context.AST.Cast(
							property.Property.Type.Type,
							context.AST.Index(
								lambdaParam.Reference,
								context.AST.Constant(i, true),
								WellKnownTypes.System.ObjectNullable)));
				}

				var conversionLambda = context.AST
						.Lambda(WellKnownTypes.System.Linq.Expressions.LambdaExpression, true)
						.Parameter(lambdaParam);

				// generate full tuple conversion expression
				conversionLambda
					.Body()
					.Append(
						context.AST.Return(
							context.AST.New(
								tupleClassBuilder.Type.Type,
								[],
								initializers)));

				// add conversion expression to mapping schema initializer
				context.StaticInitializer
					.Append(
					context.AST.Call(
						context.ContextMappingSchema,
						WellKnownTypes.LinqToDB.Mapping.MappingSchema_SetConvertExpression,
						new IType[]
						{
							context.AST.ArrayType(WellKnownTypes.System.ObjectNullable, false),
							tupleClassBuilder.Type.Type
						},
						false,
						conversionLambda.Method));

				returnType = tupleClassBuilder.Type.Type.WithNullability(function.ReturnTuple.CanBeNull);
			}
			else
				returnType = function.Return!;

			method.Returns(returnType);

			method.Method.ChangeHandler += m =>
			{
				function.Return = m.ReturnType!.Type;
			};

			foreach (var param in function.Parameters)
				context.DefineParameter(method, param.Parameter);

			// metadata last
			context.MetadataBuilder?.BuildFunctionMetadata(context, function.Metadata, method);
		}
	}
}
