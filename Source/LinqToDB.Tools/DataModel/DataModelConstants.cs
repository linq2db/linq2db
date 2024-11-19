using System;
using System.Reflection;
#if SUPPORTS_COMPOSITE_FORMAT
using System.Text;
#endif
using System.Threading;

namespace LinqToDB.DataModel
{
	// while those names used later as wrapped in CodeIdentifier AST class,
	// we shouldn't do it here as CodeIdentifier is mutable and could change name
	// during name normalization and conflict resolution step of code generation procedure
	internal static class DataModelConstants
	{
		/// <summary>
		/// Data context constructor configuration parameter name.
		/// </summary>
		public const string CONTEXT_CONSTRUCTOR_CONFIGURATION_PARAMETER  = "configuration";
		/// <summary>
		/// Data context constructor options parameter name.
		/// </summary>
		public const string CONTEXT_CONSTRUCTOR_OPTIONS_PARAMETER        = "options";
		/// <summary>
		/// Data context init partial method name.
		/// </summary>
		public const string CONTEXT_INIT_METHOD                          = "InitDataContext";

		/// <summary>
		/// Name of region in data context with properties and other code for additional schemas.
		/// </summary>
		public const string SCHEMAS_CONTEXT_REGION                       = "Schemas";
		/// <summary>
		/// Additional schemas initialization method name.
		/// </summary>
		public const string SCHEMAS_INIT_METHOD                          = "InitSchemas";
		/// <summary>
		/// Additional schema context-like class field name with main data context.
		/// </summary>
		public const string SCHEMA_CONTEXT_FIELD                         = "_dataContext";
		/// <summary>
		/// Additional schema context-like class constructor main data context parameter name.
		/// </summary>
		public const string SCHEMA_CONTEXT_CONSTRUCTOR_PARAMETER         = "dataContext";

		/// <summary>
		/// Name of Extension methods class (e.g. Find methods, associations, procedures or functions).
		/// </summary>
		public const string EXTENSIONS_CLASS                             = "ExtensionMethods";

		/// <summary>
		/// Async method suffix.
		/// </summary>
		public const string ASYNC_SUFFIX                                 = "Async";

		/// <summary>
		/// <see cref="CancellationToken"/> parameter name.
		/// </summary>
		public const string CANCELLATION_TOKEN_PARAMETER                 = "cancellationToken";

		/// <summary>
		/// Find method filter expression parameter.
		/// </summary>
		public const string FIND_ENTITY_FILTER_PARAMETER                 = "e";
		/// <summary>
		/// Find method table parameter name.
		/// </summary>
		public const string FIND_TABLE_PARAMETER                         = "table";
		/// <summary>
		/// Find method entity object parameter name.
		/// </summary>
		public const string FIND_CONTEXT_PARAMETER                       = "db";
		/// <summary>
		/// Find method entity parameter name.
		/// </summary>
		public const string FIND_ENTITY_PARAMETER                        = "record";
		/// <summary>
		/// Find extension method name.
		/// </summary>
		public const string FIND_METHOD                                  = "Find";
		/// <summary>
		/// FindQuery extension method name.
		/// </summary>
		public const string FIND_QUERY_SUFFIX                            = "Query";
		/// <summary>
		/// Find extension methods region.
		/// </summary>
		public const string FIND_METHODS_REGION                          = "Table Extensions";

		/// <summary>
		/// Entity <see cref="IEquatable{T}"/> interface implementation.
		/// </summary>
		public const string ENTITY_IEQUATABLE_REGION                     = "IEquatable<T> support";
		/// <summary>
		/// Entity field to store comparer instance implementation for <see cref="IEquatable{T}"/> interface implementation.
		/// </summary>
		public const string ENTITY_IEQUATABLE_COMPARER_FIELD             = "_equalityComparer";
		/// <summary>
		/// Name of parameter of primary key column selector lambda.
		/// </summary>
		public const string ENTITY_IEQUATABLE_COMPARER_LAMBDA_PARAMETER  = "c";

		/// <summary>
		/// Assocation properties region name.
		/// </summary>
		public const string ENTITY_ASSOCIATIONS_REGION                   = "Associations";
		/// <summary>
		/// Association extensions region name.
		/// </summary>
		public const string EXTENSIONS_ASSOCIATIONS_REGION               = "Associations";
		/// <summary>
		/// Assocation extensions region name template for specific entity.
		/// Parameter: entity name.
		/// </summary>
#if SUPPORTS_COMPOSITE_FORMAT
		public static readonly CompositeFormat EXTENSIONS_ENTITY_ASSOCIATIONS_REGION = CompositeFormat.Parse("{0} Associations");
#else
		public const string EXTENSIONS_ENTITY_ASSOCIATIONS_REGION        = "{0} Associations";
#endif
		/// <summary>
		/// Association extension method <c>this</c> parameter name.
		/// </summary>
		public const string EXTENSIONS_ENTITY_THIS_PARAMETER             = "obj";
		/// <summary>
		/// Association extension method data context parameter name.
		/// </summary>
		public const string EXTENSIONS_ENTITY_CONTEXT_PARAMETER          = "db";
		/// <summary>
		/// Association extension method filter expression parameter name.
		/// </summary>
		public const string EXTENSIONS_ASSOCIATION_FILTER_PARAMETER      = "t";
		/// <summary>
		/// Stored procedures extensions region name.
		/// </summary>
		public const string EXTENSIONS_STORED_PROCEDURES_REGION          = "Stored Procedures";
		/// <summary>
		/// Aggregate functions extension region name.
		/// </summary>
		public const string EXTENSIONS_AGGREGATES_REGION                 = "Aggregate Functions";
		/// <summary>
		/// Scalar functions extensions region name.
		/// </summary>
		public const string EXTENSIONS_SCALAR_FUNCTIONS_REGION           = "Scalar Functions";
		/// <summary>
		/// Table functions mappings region name.
		/// </summary>
		public const string CONTEXT_TABLE_FUNCTIONS_REGION              = "Table Functions";
		/// <summary>
		/// Data context custom mapping schema property name.
		/// </summary>
		public const string CONTEXT_SCHEMA_PROPERTY                      = "ContextSchema";
		/// <summary>
		/// Exception message for client-side association extension method call.
		/// </summary>
		public const string EXCEPTION_QUERY_ONLY_ASSOCATION_CALL         = "Association cannot be called outside of query";
		/// <summary>
		/// Exception message for aggregate function client-side method call.
		/// </summary>
		public const string EXCEPTION_QUERY_ONLY_AGGREGATE_CALL          = "Aggregate cannot be called outside of query";
		/// <summary>
		/// Exception message for scalar function client-side method call.
		/// </summary>
		public const string EXCEPTION_QUERY_ONLY_SCALAR_CALL             = "Scalar function cannot be called outside of query";
		/// <summary>
		/// Aggregate function mapping method generic parameter name.
		/// </summary>
		public const string AGGREGATE_RECORD_TYPE                        = "TSource";
		/// <summary>
		/// Aggregate function mapping <c>this</c> parameter name.
		/// </summary>
		public const string AGGREGATE_SOURCE_PARAMETER                   = "src";
		/// <summary>
		/// Name of tuple mapping expression parameter for scalar function with tuple return type.
		/// </summary>
		public const string SCALAR_TUPLE_MAPPING_PARAMETER               = "tuple";
		/// <summary>
		/// Table function <see cref="MethodInfo"/> field initializer expression context parameter name.
		/// </summary>
		public const string TABLE_FUNCTION_METHOD_INFO_CONTEXT_PARAMETER = "ctx";

		/// <summary>
		/// Stored procedure <c>this</c> data context parameter name.
		/// </summary>
		public const string STORED_PROCEDURE_CONTEXT_PARAMETER           = "dataConnection";
		/// <summary>
		/// Stored procedure mapping parameters array variable name.
		/// </summary>
		public const string STORED_PROCEDURE_PARAMETERS_VARIABLE         = "parameters";
		/// <summary>
		/// Stored procedure default name for return parameter.
		/// </summary>
		public const string STORED_PROCEDURE_DEFAULT_RETURN_PARAMETER    = "return";
		/// <summary>
		/// Stored procedure ordinal mapping expression parameter name.
		/// </summary>
		public const string STORED_PROCEDURE_CUSTOM_MAPPER_PARAMETER     = "dataReader";
		/// <summary>
		/// Stored procedure mapping return value variable name.
		/// </summary>
		public const string STORED_PROCEDURE_RETURN_VARIABLE             = "ret";
		/// <summary>
		/// Stored procedure mapping results list variable name.
		/// </summary>
		public const string STORED_PROCEDURE_RESULT_VARIABLE             = "result";
		/// <summary>
		/// Stored procedure nameless non-return parameter naming template.
		/// Parameter: parameter index.
		/// </summary>
#if SUPPORTS_COMPOSITE_FORMAT
		public static readonly CompositeFormat STORED_PROCEDURE_PARAMETER_TEMPLATE = CompositeFormat.Parse("p{0}");
#else
		public const string STORED_PROCEDURE_PARAMETER_TEMPLATE          = "p{0}";
#endif
	}
}
