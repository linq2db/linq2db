namespace LinqToDB.CodeGen.DataModel
{
	// while we usually wrap names here in CodeIdentifier AST class, we shouldn't do it
	// here as CodeIdentifier is mutable and could change name during name normalization and conflict
	// resolution step of code generation procedure
	partial class DataModelGenerator
	{
		/// <summary>
		/// Name of region in data context with properties and other code for additional schemas.
		/// </summary>
		private const string SCHEMAS_CONTEXT_REGION = "Schemas";
		private const string SCHEMAS_INIT_METHOD = "InitSchemas";

		// TODO: use better name
		private const string EXTENSIONS_CLASS = "SqlFunctions";

		private const string FIND_ENTITY_PARAMETER = "e";
		private const string FIND_TABLE_PARAMETER = "table";
		private const string FIND_METHOD = "Find";
		private const string FIND_METHODS_REGION = "Table Extensions";

		private const string SCHEMA_CONTEXT_FIELD = "_dataContext";
		private const string SCHEMA_CONTEXT_CONSTRUCTOR_PARAMETER = "dataContext";

		private const string CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER = "configuration";
		private const string CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER = "options";
		private const string CONTEXT_INIT_METHOD = "InitDataContext";

		private const string EXTENSIONS_ASSOCIATIONS_REGION = "Associations";
		private const string ENTITY_ASSOCIATIONS_REGION = "Associations";
		private const string EXTENSIONS_ENTITY_ASSOCIATIONS_REGION = "{0} Associations";
		private const string EXTENSIONS_ENTITY_THIS_PARAMETER = "obj";
		private const string EXTENSIONS_ENTITY_CONTEXT_PARAMETER = "db";
		private const string EXTENSIONS_ASSOCIATION_FILTER_PARAMETER = "t";

		private const string EXTENSIONS_STORED_PROCEDURES_REGION = "Stored Procedures";
		private const string EXTENSIONS_AGGREGATES_REGION = "Aggregate Functions";
		private const string EXTENSIONS_SCALAR_FUNCTIONS_REGION = "Scalar Functions";
		private const string CONTEXT_TABLE_FUNCTIONS_REGION = "Table Functions";
		private const string CONTEXT_SCHEMA_PROPERTY = "ContextSchema";

		private const string EXCEPTION_QUERY_ONLY_ASSOCATION_CALL = "Association cannot be called outside of query";
		private const string EXCEPTION_QUERY_ONLY_AGGREGATE_CALL = "Aggregate cannot be called outside of query";
		private const string EXCEPTION_QUERY_ONLY_SCALAR_CALL = "Scalar function cannot be called outside of query";
		private const string AGGREGATE_RECORD_TYPE = "TSource";
		private const string AGGREGATE_SOURCE_PARAMETER = "src";

		private const string SCALAR_TUPLE_MAPPING_PARAMETER = "tuple";
	}
}
