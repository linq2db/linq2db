using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private IType BuildAdditionalSchema(AdditionalSchemaModel schema)
		{
			var (schemaWrapper, _) = DefineFileClass(schema.WrapperClass);
			var schemaContext = DefineClass(schema.ContextClass, schemaWrapper.Classes());

			var findMethods = schemaWrapper.Regions().New("Table Extensions").Methods(false);

			// schema data context field
			var ctxField = schemaContext.Fields(false).New(_code.Identifier("_dataContext"), _code.Type(typeof(IDataContext), false))
						.Private()
						.ReadOnly();

			var schemaEntities = schemaWrapper.Classes();
			BuildEntities(
				schema.Entities,
				entity => DefineClass(entity.Class, schemaEntities),
				schemaContext.Properties(true),
				false,
				_code.Member(schemaContext.Type.This, ctxField.Field),
				() => findMethods);

			var ctorParam = _code.Parameter(_code.Type(typeof(IDataContext), false), _code.Identifier("dataContext"), ParameterDirection.In);
			schemaContext.Constructors().New()
				.Public()
				.Parameter(ctorParam)
				.Body()
				.Append(_code.Assign(_code.Member(schemaContext.Type.This, ctxField.Field), ctorParam.Reference));

			_schemaProperties.Add(schema, (schemaWrapper, schemaContext));

			return schemaContext.Type.Type;
		}
	}
}
