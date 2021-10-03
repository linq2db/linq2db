using System.Reflection;

namespace LinqToDB.CodeGen.DataModel
{
	// while those names used later as wrapped in CodeIdentifier AST class,
	// we shouldn't do it here as CodeIdentifier is mutable and could change name
	// during name normalization and conflict resolution step of code generation procedure
	partial class DataModelGenerator
	{
		/// <summary>
		/// Data context constructor configuration parameter name.
		/// </summary>
		private const string CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER  = "configuration";
		/// <summary>
		/// Data context constructor options parameter name.
		/// </summary>
		private const string CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER        = "options";
		/// <summary>
		/// Data context init partial method name.
		/// </summary>
		private const string CONTEXT_INIT_METHOD                          = "InitDataContext";

		/// <summary>
		/// Name of region in data context with properties and other code for additional schemas.
		/// </summary>
		private const string SCHEMAS_CONTEXT_REGION                       = "Schemas";
		/// <summary>
		/// Additional schemas initialization method name.
		/// </summary>
		private const string SCHEMAS_INIT_METHOD                          = "InitSchemas";
		/// <summary>
		/// Additional schema context-like class field name with main data context.
		/// </summary>
		private const string SCHEMA_CONTEXT_FIELD                         = "_dataContext";
		/// <summary>
		/// Additional schema context-like class constructor main data context parameter name.
		/// </summary>
		private const string SCHEMA_CONTEXT_CONSTRUCTOR_PARAMETER         = "dataContext";

		/// <summary>
		/// Name of Extension methods class (e.g. Find methods, associations, procedures or functions).
		/// </summary>
		private const string EXTENSIONS_CLASS                             = "ExtensionMethods";

		/// <summary>
		/// Find method filter expression parameter.
		/// </summary>
		private const string FIND_ENTITY_PARAMETER                        = "e";
		/// <summary>
		/// Find method table parameter name.
		/// </summary>
		private const string FIND_TABLE_PARAMETER                         = "table";
		/// <summary>
		/// Find extension method name.
		/// </summary>
		private const string FIND_METHOD                                  = "Find";
		/// <summary>
		/// Find extension methods region.
		/// </summary>
		private const string FIND_METHODS_REGION                          = "Table Extensions";


		/// <summary>
		/// Assocation properties region name.
		/// </summary>
		private const string ENTITY_ASSOCIATIONS_REGION                   = "Associations";
		/// <summary>
		/// Association extensions region name.
		/// </summary>
		private const string EXTENSIONS_ASSOCIATIONS_REGION               = "Associations";
		/// <summary>
		/// Assocation extensions region name template for specific entity.
		/// Parameter: entity name.
		/// </summary>
		private const string EXTENSIONS_ENTITY_ASSOCIATIONS_REGION        = "{0} Associations";
		/// <summary>
		/// Association extension method <c>this</c> parameter name.
		/// </summary>
		private const string EXTENSIONS_ENTITY_THIS_PARAMETER             = "obj";
		/// <summary>
		/// Association extension method data context parameter name.
		/// </summary>
		private const string EXTENSIONS_ENTITY_CONTEXT_PARAMETER          = "db";
		/// <summary>
		/// Association extension method filter expression parameter name.
		/// </summary>
		private const string EXTENSIONS_ASSOCIATION_FILTER_PARAMETER      = "t";
		/// <summary>
		/// Stored procedures extensions region name.
		/// </summary>
		private const string EXTENSIONS_STORED_PROCEDURES_REGION          = "Stored Procedures";
		/// <summary>
		/// Aggregate functions extension region name.
		/// </summary>
		private const string EXTENSIONS_AGGREGATES_REGION                 = "Aggregate Functions";
		/// <summary>
		/// Scalar functions extensions region name.
		/// </summary>
		private const string EXTENSIONS_SCALAR_FUNCTIONS_REGION           = "Scalar Functions";
		/// <summary>
		/// Table functions mappings region name.
		/// </summary>
		private const string CONTEXT_TABLE_FUNCTIONS_REGION               = "Table Functions";
		/// <summary>
		/// Data context custom mapping schema property name.
		/// </summary>
		private const string CONTEXT_SCHEMA_PROPERTY                      = "ContextSchema";
		/// <summary>
		/// Exception message for client-side association extension method call.
		/// </summary>
		private const string EXCEPTION_QUERY_ONLY_ASSOCATION_CALL         = "Association cannot be called outside of query";
		/// <summary>
		/// Exception message for aggregate function client-side method call.
		/// </summary>
		private const string EXCEPTION_QUERY_ONLY_AGGREGATE_CALL          = "Aggregate cannot be called outside of query";
		/// <summary>
		/// Exception message for scalar function client-side method call.
		/// </summary>
		private const string EXCEPTION_QUERY_ONLY_SCALAR_CALL             = "Scalar function cannot be called outside of query";
		/// <summary>
		/// Aggregate function mapping method generic parameter name.
		/// </summary>
		private const string AGGREGATE_RECORD_TYPE                        = "TSource";
		/// <summary>
		/// Aggregate function mapping <c>this</c> parameter name.
		/// </summary>
		private const string AGGREGATE_SOURCE_PARAMETER                   = "src";
		/// <summary>
		/// Name of tuple mapping expression parameter for scalar function with tuple return type.
		/// </summary>
		private const string SCALAR_TUPLE_MAPPING_PARAMETER               = "tuple";
		/// <summary>
		/// Table function <see cref="MethodInfo"/> field initializer expression context parameter name.
		/// </summary>
		private const string TABLE_FUNCTION_METHOD_INFO_CONTEXT_PARAMETER = "ctx";

		/// <summary>
		/// Stored procedure <c>this</c> data context parameter name.
		/// </summary>
		private const string STORED_PROCEDURE_CONTEXT_PARAMETER           = "dataConnection";
		/// <summary>
		/// Stored procedure mapping parameters array variable name.
		/// </summary>
		private const string STORED_PROCEDURE_PARAMETERS_VARIABLE         = "parameters";
		/// <summary>
		/// Stored procedure default name for return parameter.
		/// </summary>
		private const string STORED_PROCEDURE_DEFAULT_RETURN_PARAMETER    = "return";
		/// <summary>
		/// Stored procedure ordinal mapping expression parameter name.
		/// </summary>
		private const string STORED_PROCEDURE_CUSTOM_MAPPER_PARAMETER     = "dataReader";
		/// <summary>
		/// Stored procedure mapping return value variable name.
		/// </summary>
		private const string STORED_PROCEDURE_RETURN_VARIABLE             = "ret";
		/// <summary>
		/// Stored procedure nameless non-return parameter naming template.
		/// Parameter: parameter index.
		/// </summary>
		private const string STORED_PROCEDURE_PARAMETER_TEMPLATE          = "p{0}";
	}
}
