using LinqToDB.CodeModel;

namespace LinqToDB.DataModel
{
	// additional schema generation logic
	// except functionality shared with main context (entities, functions, etc)
	partial class DataModelGenerator
	{
		/// <summary>
		/// Defines addtional schema classes.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="schema">Schema model.</param>
		/// <returns>Type of schema context-like class.</returns>
		private static IType BuildAdditionalSchema(IDataModelGenerationContext context, AdditionalSchemaModel schema)
		{
			// generated code for additional schema looks like:
			/*
			 * // this class defines schema-specific extension methods and table mappings
			 * static class SchemaWrapperClass
			 * {
			 *     class SchemaContextLikeClass
			 *     {
			 *         // constructor acepts main data context instance to use with GetTable calls for schema tables
			 *         .ctor(context_instance) {}
			 *
			 *         // entity table getter properties
			 *         public ITable<SchemaTable> SchemaTables => _context_instance.GetTable<SchemaTable>();
			 *     }
			 *     
			 *     // schema mapping classes
			 *     
			 *     // extensions, e.g. Find methods
			 * }
			 */

			var schemaWrapper = context.DefineFileClass(schema.WrapperClass);
			var schemaContext = context.DefineClass(schemaWrapper.Classes(), schema.ContextClass);

			// schema data context field
			var ctxField = schemaContext
				.Fields(false)
					.New(context.AST.Name(DataModelConstants.SCHEMA_CONTEXT_FIELD), WellKnownTypes.LinqToDB.IDataContext)
						.Private()
						.ReadOnly();

			var childContext  = new NestedSchemaGenerationContext(context, schema, schemaWrapper, schemaContext, ctxField.Field.Reference);
			context.RegisterChildContext(schema, childContext);

			// define schema table mappings as nested classes in wrapper class
			var schemaEntities = schemaWrapper.Classes();
			BuildEntities(childContext, schema.Entities, entity => context.DefineClass(schemaEntities, entity.Class));

			// add constructor to context-like class
			var ctorParam = context.AST.Parameter(WellKnownTypes.LinqToDB.IDataContext, context.AST.Name(DataModelConstants.SCHEMA_CONTEXT_CONSTRUCTOR_PARAMETER), CodeParameterDirection.In);
			schemaContext
				.Constructors()
					.New()
						.SetModifiers(Modifiers.Public)
						.Parameter(ctorParam)
						.Body()
							.Append(
								context.AST.Assign(
									context.AST.Member(schemaContext.Type.This, ctxField.Field.Reference),
									ctorParam.Reference));

			return schemaContext.Type.Type;
		}
	}
}
