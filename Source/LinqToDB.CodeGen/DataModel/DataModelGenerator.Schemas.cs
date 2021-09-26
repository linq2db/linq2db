using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	// additional schema generation logic
	// except functionality shared with main context (entities, functions, etc)
	partial class DataModelGenerator
	{
		/// <summary>
		/// Defines addtional schema classes.
		/// </summary>
		/// <param name="schema">Schema model.</param>
		/// <returns>Type of schema context-like class.</returns>
		private IType BuildAdditionalSchema(AdditionalSchemaModel schema)
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

			var schemaWrapper = DefineFileClass(schema.WrapperClass);
			var schemaContext = DefineClass(schemaWrapper.Classes(), schema.ContextClass);
			var findMethods   = schemaWrapper.Regions().New(FIND_METHODS_REGION).Methods(false);

			// schema data context field
			var ctxField = schemaContext
				.Fields(false)
					.New(_code.Name(SCHEMA_CONTEXT_FIELD), WellKnownTypes.LinqToDB.IDataContext)
						.Private()
						.ReadOnly();

			// define schema table mappings as nested classes in wrapper class
			var schemaEntities = schemaWrapper.Classes();
			BuildEntities(
				schema.Entities,
				entity => DefineClass(schemaEntities, entity.Class),
				schemaContext.Properties(true),
				false,
				_code.Member(schemaContext.Type.This, ctxField.Field),
				() => findMethods);

			// add constructor to context-like class
			var ctorParam = _code.Parameter(WellKnownTypes.LinqToDB.IDataContext, _code.Name(SCHEMA_CONTEXT_CONSTRUCTOR_PARAMETER), ParameterDirection.In);
			schemaContext
				.Constructors()
					.New()
						.Public()
						.Parameter(ctorParam)
						.Body()
							.Append(
								_code.Assign(
									_code.Member(schemaContext.Type.This, ctxField.Field),
									ctorParam.Reference));

			// save wrapper and context classes in lookup for later use by function/parameter generators
			_schemaClasses.Add(schema, (schemaWrapper, schemaContext));

			return schemaContext.Type.Type;
		}
	}
}
