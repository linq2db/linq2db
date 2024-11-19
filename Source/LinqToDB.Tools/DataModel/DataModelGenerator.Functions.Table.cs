using System;
using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	// contains generation logic for table function mappings
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates table function mapping.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="tableFunction">Function model.</param>
		private static void BuildTableFunction(IDataModelGenerationContext context, TableFunctionModel tableFunction)
		{
			// generated code sample:
			/*
			 * #region Function1
			 * [Sql.TableFunction("Function1")]
			 * public IQueryable<Parent> Function1(int? id)
			 * {
			 *     return this.TableFromExpression(() => this.Function1(id));
			 * }
			 * #endregion
			 */

			// create function region
			var region = context.AddTableFunctionRegion(tableFunction.Method.Name);

			// if function schema load failed, generate error pragma with exception details
			if (tableFunction.Error != null)
			{
				if (context.Options.GenerateProceduresSchemaError)
					region.Pragmas().Add(context.AST.Error($"Failed to load return table schema: {tableFunction.Error}"));

				// as we cannot generate table function without knowing it's schema, we skip failed function
				return;
			}

			// if function result schema matches known entity, we use entity class for result
			// otherwise we generate custom record mapping
			if (tableFunction.Result == null || tableFunction.Result.CustomTable == null && tableFunction.Result.Entity == null)
				throw new InvalidOperationException($"Table function {tableFunction.Name} result record type not set");

			// generate mapping method with metadata
			var method = context.DefineMethod(region.Methods(false), tableFunction.Method);

			// generate method parameters, return type and body

			// table record type
			IType returnEntity;
			if (tableFunction.Result.Entity != null)
				returnEntity = context.GetEntityBuilder(tableFunction.Result.Entity).Type.Type;
			else
				returnEntity = BuildCustomResultClass(context, tableFunction.Result.CustomTable!, region.Classes(), true).resultClassType;

			// set return type
			// T4 used ITable<T> for return type, but there is no reason to use ITable<T> over IQueryable<T>
			// Even more: ITable<T> is not correct return type here
			var returnType = context.Options.TableFunctionReturnsTable
				? WellKnownTypes.LinqToDB.ITable(returnEntity)
				: WellKnownTypes.System.Linq.IQueryable(returnEntity);
			method.Returns(returnType);

			// add table function parameters (if any)
			var methodParameters = new ICodeExpression[tableFunction.Parameters.Count];
			for (var i = 0; i < tableFunction.Parameters.Count; i++)
			{
				var param           = tableFunction.Parameters[i];
				var parameter       = context.DefineParameter(method, param.Parameter);
				methodParameters[i] = parameter.Reference;
			}

			var lambda = context.AST
				.Lambda(WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(returnType)), true);

			lambda.Body()
				.Append(
					context.AST.Return(
						context.AST.Call(
							context.CurrentDataContext.Type.This,
							method.Method.Name,
							returnType,
							methodParameters)));

			// parameters for TableFromExpression call in mapping body
			var parameters = new ICodeExpression[2];
			parameters[0] = context.ContextReference; // `this` extension method parameter
			parameters[1] = lambda.Method; // self-call expression

			// generate mapping body
			method.Body()
				.Append(
					context.AST.Return(
						context.AST.ExtCall(
							WellKnownTypes.LinqToDB.DataExtensions,
							context.Options.TableFunctionReturnsTable
								? WellKnownTypes.LinqToDB.DataExtensions_TableFromExpression
								: WellKnownTypes.LinqToDB.DataExtensions_QueryFromExpression,
							returnType,
							new[] { returnEntity },
							false,
							parameters)));

			// TODO: similar tables deduplication

			// metadata last
			context.MetadataBuilder?.BuildTableFunctionMetadata(context, tableFunction.Metadata, method);
		}
	}
}
