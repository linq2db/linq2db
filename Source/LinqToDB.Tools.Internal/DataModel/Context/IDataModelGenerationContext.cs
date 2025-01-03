using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LinqToDB.CodeModel;
using LinqToDB.Metadata;
using LinqToDB.Scaffold;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	public interface IDataModelGenerationContext
	{
		/// <summary>
		/// Gets language services provider.
		/// </summary>
		ILanguageProvider     LanguageProvider              { get; }
		/// <summary>
		/// Gets data model.
		/// </summary>
		DatabaseModel         Model                         { get; }
		/// <summary>
		/// Gets data model scaffold options.
		/// </summary>
		DataModelOptions      Options                       { get; }
		/// <summary>
		/// Gets AST builder.
		/// </summary>
		CodeBuilder           AST                           { get; }
		/// <summary>
		/// Gets data model metadata builder.
		/// </summary>
		IMetadataBuilder?     MetadataBuilder               { get; }
		/// <summary>
		/// Main data context class builder (implements <see cref="IDataContext"/>).
		/// </summary>
		ClassBuilder          MainDataContext               { get; }
		/// <summary>
		/// Main data context countructors group.
		/// </summary>
		ConstructorGroup      MainDataContextConstructors    { get; }
		/// <summary>
		/// Main data context partial methods group.
		/// </summary>
		MethodGroup          MainDataContextPartialMethods   { get; }
		/// <summary>
		/// Current data context class builder.
		/// For default schema references <see cref="MainDataContext"/>.
		/// For schema context references schema context class builder.
		/// </summary>
		ClassBuilder          CurrentDataContext            { get; }
		/// <summary>
		/// Code reference to instance of <see cref="MainDataContext"/>.
		/// </summary>
		ICodeExpression       ContextReference              { get; }
		/// <summary>
		/// Gets current schema model.
		/// </summary>
		SchemaModelBase       Schema                        { get; }
		/// <summary>
		/// Returns <c>true</c> if current scaffold generated context mapping schema.
		/// </summary>
		bool                  HasContextMappingSchema       { get; }
		/// <summary>
		/// Adds static mapping schema property (if not added yet) to main data context class and returns reference to it.
		/// </summary>
		CodeReference         ContextMappingSchema          { get; }
		/// <summary>
		/// Adds static constructor (if not added yet) to main data context class and returns it's body builder.
		/// </summary>
		BlockBuilder          StaticInitializer             { get; }
		/// <summary>
		/// Gets class with mapping methods for scalar, aggregate functions and stored procedures.
		/// </summary>
		CodeClass             NonTableFunctionsClass        { get; }
		/// <summary>
		/// Gets class with mapping methods for table functions.
		/// </summary>
		CodeClass             TableFunctionsClass           { get; }
		/// <summary>
		/// Gets class with extension methods.
		/// </summary>
		ClassBuilder          ExtensionsClass               { get; }
		/// <summary>
		/// Gets property group for table access properties in <see cref="CurrentDataContext"/> class.
		/// </summary>
		PropertyGroup         ContextProperties             { get; }
		/// <summary>
		/// Gets method group for Find extension methods.
		/// </summary>
		MethodGroup           FindExtensionsGroup           { get; }
		/// <summary>
		/// Gets region with addtional schemas code.
		/// </summary>
		RegionBuilder         SchemasContextRegion          { get; }
		/// <summary>
		/// Get type of <c>this</c> context parameter for stored procedures.
		/// </summary>
		IType                 ProcedureContextParameterType { get; }
		/// <summary>
		/// Gets generated files.
		/// </summary>
		IEnumerable<CodeFile> Files                         { get; }

		/// <summary>
		/// Gets registered entity class builder by entity model.
		/// </summary>
		/// <param name="model">Entity model.</param>
		/// <returns>Entity mapping class builder.</returns>
		ClassBuilder                GetEntityBuilder                   (EntityModel model                                                );
		/// <summary>
		/// Register entity class builder for entity model.
		/// </summary>
		/// <param name="model">Entity model.</param>
		/// <param name="builder">Entity class builder.</param>
		void                        RegisterEntityBuilder              (EntityModel model, ClassBuilder builder                          );
		/// <summary>
		/// Gets entity property by column model.
		/// </summary>
		/// <param name="model">Column model.</param>
		/// <returns>Column mapping property.</returns>
		CodeProperty                GetColumnProperty                  (ColumnModel model                                                );
		/// <summary>
		/// Register entity property for column.
		/// </summary>
		/// <param name="model">Column model.</param>
		/// <param name="property">Enity column property.</param>
		void                        RegisterColumnProperty             (ColumnModel model, CodeProperty property                         );
		/// <summary>
		/// Gets method group for asociation extension methods for specific entity.
		/// </summary>
		/// <param name="entity">Entity model.</param>
		/// <returns>Association extension methods group.</returns>
		MethodGroup                 GetEntityAssociationExtensionsGroup(EntityModel entity                                               );
		/// <summary>
		/// Gets property group for associations in specific entity.
		/// </summary>
		/// <param name="entity">Entity model.</param>
		/// <returns>Association properties group in entity class.</returns>
		PropertyGroup               GetEntityAssociationsGroup         (EntityModel entity                                               );
		/// <summary>
		/// Adds named region for table function mappings.
		/// </summary>
		/// <param name="regionName">Region name.</param>
		/// <returns>Region builder.</returns>
		RegionBuilder               AddTableFunctionRegion             (string regionName                                                );
		/// <summary>
		/// Adds named region for scalar function mappings.
		/// </summary>
		/// <param name="regionName">Region name.</param>
		/// <returns>Region builder.</returns>
		RegionBuilder               AddScalarFunctionRegion            (string regionName                                                );
		/// <summary>
		/// Adds named region for aggregate function mappings.
		/// </summary>
		/// <param name="regionName">Region name.</param>
		/// <returns>Region builder.</returns>
		RegionBuilder               AddAggregateFunctionRegion         (string regionName                                                );
		/// <summary>
		/// Adds named region for stored procedure mappings.
		/// </summary>
		/// <param name="regionName">Region name.</param>
		/// <returns>Region builder.</returns>
		RegionBuilder               AddStoredProcedureRegion           (string regionName                                                );
		/// <summary>
		/// Helper to generate fully-qualified procedure or function name.
		/// </summary>
		/// <param name="routineName">Procedure/function object name.</param>
		/// <returns>Fully-qualified name of procedure or function.</returns>
		string                      MakeFullyQualifiedRoutineName      (SqlObjectName routineName                                        );
		/// <summary>
		/// Apply identifier normalization to method parameter.
		/// </summary>
		/// <param name="parameterName">Original parameter name.</param>
		/// <returns>Normalized parameter name.</returns>
		string                      NormalizeParameterName             (string parameterName                                             );
		/// <summary>
		/// Gets child schema model generation context.
		/// Available only on main context.
		/// </summary>
		/// <param name="schema">Addtional schema model.</param>
		/// <returns>Schema context.</returns>
		IDataModelGenerationContext GetChildContext                    (AdditionalSchemaModel schema                                     );
		/// <summary>
		/// Register child schema model generation context.
		/// Available only on main context.
		/// </summary>
		/// <param name="schema">Additional schema model.</param>
		/// <param name="context">Schema context.</param>
		void                        RegisterChildContext               (AdditionalSchemaModel schema, IDataModelGenerationContext context);
		/// <summary>
		/// Register new file in code model.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <returns>Registered file.</returns>
		FileData                    AddFile                            (string fileName                                                  );
		/// <summary>
		/// Tries to find file in code model by name.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="file">File model.</param>
		/// <returns><c>true</c> if file with such name already registered; <c>false</c> otherwise.</returns>
		bool                        TryGetFile                         (string fileName, [NotNullWhen(true)] out FileData? file          );
	}
}
