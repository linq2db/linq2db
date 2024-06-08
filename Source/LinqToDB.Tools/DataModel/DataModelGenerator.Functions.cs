using System;
using System.Text;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	// basic functionality for function/procedure mappings generation
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates mappings for all data model functions and procedures.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		private static void BuildAllFunctions(IDataModelGenerationContext context)
		{
			// build functions for main context
			BuildFunctions(context);

			// build functions for additional schemas
			foreach (var schema in context.Model.DataContext.AdditionalSchemas.Values)
				BuildFunctions(context.GetChildContext(schema));
		}

		/// <summary>
		/// Generates mappings for functions and procedures for specific schema (or main context).
		/// </summary>
		/// <param name="context">Model generation context.</param>
		private static void BuildFunctions(IDataModelGenerationContext context)
		{
			if (context.Options.GenerateProcedureSync || context.Options.GenerateProcedureAsync)
			{
				foreach (var proc in context.Schema.StoredProcedures)
					BuildStoredProcedure(context, proc);
			}

			foreach (var func in context.Schema.ScalarFunctions)
				BuildScalarFunction(context, func);

			foreach (var func in context.Schema.AggregateFunctions)
				BuildAggregateFunction(context, func);

			foreach (var func in context.Schema.TableFunctions)
				BuildTableFunction(context, func);
		}

		/// <summary>
		/// Generates custom class for table function/stored procedure result record
		/// in cases when there is no suitable entity mapping for it.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="model">Result record model.</param>
		/// <param name="classes">Procedure classes group.</param>
		/// <param name="withMapping">Indicate wether we need to generate mapping attributes for generated class columns.
		/// Column mappings needed for table function records (as we use them as query sources) and could be missing
		/// for stored procedures in cases when returned columns cannot be mapped due to duplicate/missing column names, which
		/// is possible for stored procedures.</param>
		/// <returns>Generated class and column properties.</returns>
		private static (IType resultClassType, CodeProperty[] properties) BuildCustomResultClass(
			IDataModelGenerationContext context,
			ResultTableModel            model,
			ClassGroup                  classes,
			bool                        withMapping)
		{
			var properties = new CodeProperty[model.Columns.Count];

			var resultClassBuilder = context.DefineClass(classes, model.Class);

			var columnsGroup = resultClassBuilder.Properties(true);

			for (var i = 0; i < model.Columns.Count; i++)
			{
				var columnModel   = model.Columns[i];
				var columnBuilder = context.DefineProperty(columnsGroup, columnModel.Property);

				context.RegisterColumnProperty(columnModel, columnBuilder.Property);

				if (withMapping)
					context.MetadataBuilder?.BuildColumnMetadata(context, resultClassBuilder.Type, columnModel.Metadata, columnBuilder);

				properties[i] = columnBuilder.Property;
			}

			return (resultClassBuilder.Type.Type, properties);
		}
	}
}
