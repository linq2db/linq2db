using System.Collections.Generic;

using LINQPad.Extensibility.DataContext;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;

namespace LinqToDB.LINQPad;

internal static class DynamicSchemaGenerator
{
	private static ScaffoldOptions GetOptions(ConnectionSettings settings, string? contextNamespace, string contextName)
	{
		var options = ScaffoldOptions.Default();

		// set schema load options
		options.Schema.IncludeSchemas = settings.Schema.IncludeSchemas;
		foreach (var schema in (settings.Schema.Schemas ?? (IEnumerable<string>)[]))
			options.Schema.Schemas.Add(schema);

		options.Schema.IncludeCatalogs = settings.Schema.IncludeCatalogs;
		foreach (var catalog in (settings.Schema.Catalogs ?? (IEnumerable<string>)[]))
			options.Schema.Catalogs.Add(catalog);

		options.Schema.LoadedObjects = SchemaObjects.Table | SchemaObjects.View;

		if (settings.Schema.LoadForeignKeys       ) options.Schema.LoadedObjects |= SchemaObjects.ForeignKey;
		if (settings.Schema.LoadProcedures        ) options.Schema.LoadedObjects |= SchemaObjects.StoredProcedure;
		if (settings.Schema.LoadTableFunctions    ) options.Schema.LoadedObjects |= SchemaObjects.TableFunction;
		if (settings.Schema.LoadScalarFunctions   ) options.Schema.LoadedObjects |= SchemaObjects.ScalarFunction;
		if (settings.Schema.LoadAggregateFunctions) options.Schema.LoadedObjects |= SchemaObjects.AggregateFunction;

		options.Schema.PreferProviderSpecificTypes = settings.Scaffold.UseProviderTypes;
		options.Schema.IgnoreDuplicateForeignKeys  = false;
		options.Schema.UseSafeSchemaLoad           = false;
		options.Schema.LoadDatabaseName            = false;
		options.Schema.LoadProceduresSchema        = true;
		// TODO: disabled due to generation bug in current scaffolder
		options.Schema.EnableSqlServerReturnValue  = false;
		//options.Schema.EnableSqlServerReturnValue  = true;

		// set data model options
		if (settings.Scaffold.AsIsNames)
		{
			// https://github.com/linq2db/linq2db.LINQPad/issues/89
			// reset naming options for some objects:
			// - entities
			// - context properties
			// - columns
			// more could be added later on request
			options.DataModel.EntityClassNameOptions.Casing                        = NameCasing.None;
			options.DataModel.EntityClassNameOptions.Pluralization                 = Pluralization.None;
			options.DataModel.EntityClassNameOptions.Transformation                = NameTransformation.None;
			options.DataModel.EntityClassNameOptions.DontCaseAllCaps               = true;
			options.DataModel.EntityClassNameOptions.PluralizeOnlyIfLastWordIsText = false;

			options.DataModel.EntityContextPropertyNameOptions.Casing                        = NameCasing.None;
			options.DataModel.EntityContextPropertyNameOptions.Pluralization                 = Pluralization.None;
			options.DataModel.EntityContextPropertyNameOptions.Transformation                = NameTransformation.None;
			options.DataModel.EntityContextPropertyNameOptions.DontCaseAllCaps               = true;
			options.DataModel.EntityContextPropertyNameOptions.PluralizeOnlyIfLastWordIsText = false;

			options.DataModel.EntityColumnPropertyNameOptions.Casing                        = NameCasing.None;
			options.DataModel.EntityColumnPropertyNameOptions.Pluralization                 = Pluralization.None;
			options.DataModel.EntityColumnPropertyNameOptions.Transformation                = NameTransformation.None;
			options.DataModel.EntityColumnPropertyNameOptions.DontCaseAllCaps               = true;
			options.DataModel.EntityColumnPropertyNameOptions.PluralizeOnlyIfLastWordIsText = false;
		}

		if (!settings.Scaffold.Capitalize)
			options.DataModel.EntityColumnPropertyNameOptions.Casing = NameCasing.None;
		else
			options.DataModel.EntityColumnPropertyNameOptions.Casing = NameCasing.Pascal;

		if (!settings.Scaffold.Pluralize)
		{
			options.DataModel.EntityContextPropertyNameOptions.Pluralization             = Pluralization.None;
			options.DataModel.TargetMultipleAssociationPropertyNameOptions.Pluralization = Pluralization.None;
		}
		else
		{
			options.DataModel.EntityContextPropertyNameOptions.Pluralization             = Pluralization.PluralIfLongerThanOne;
			options.DataModel.TargetMultipleAssociationPropertyNameOptions.Pluralization = Pluralization.PluralIfLongerThanOne;
		}

		options.DataModel.GenerateDefaultSchema              = true;
		options.DataModel.GenerateDataType                   = true;
		options.DataModel.GenerateDbType                     = true;
		options.DataModel.GenerateLength                     = true;
		options.DataModel.GeneratePrecision                  = true;
		options.DataModel.GenerateScale                      = true;
		options.DataModel.HasDefaultConstructor              = false;
		options.DataModel.HasConfigurationConstructor        = false;
		options.DataModel.HasUntypedOptionsConstructor       = false;
		options.DataModel.HasTypedOptionsConstructor         = false;
		options.DataModel.ContextClassName                   = contextName;
		options.DataModel.BaseContextClass                   = "LinqToDB.LINQPad.LINQPadDataConnection";
		options.DataModel.GenerateAssociations               = true;
		options.DataModel.GenerateAssociationExtensions      = false;
		options.DataModel.AssociationCollectionType          = "System.Collections.Generic.List<>";
		options.DataModel.MapProcedureResultToEntity         = true;
		options.DataModel.TableFunctionReturnsTable          = true;
		options.DataModel.GenerateProceduresSchemaError      = false;
		options.DataModel.SkipProceduresWithSchemaErrors     = true;
		options.DataModel.GenerateProcedureResultAsList      = false;
		options.DataModel.GenerateProcedureParameterDbType   = true;
		options.DataModel.GenerateProcedureSync              = true;
		options.DataModel.GenerateProcedureAsync             = false;
		options.DataModel.GenerateSchemaAsType               = false;
		options.DataModel.GenerateIEquatable                 = false;
		options.DataModel.GenerateFindExtensions             = FindTypes.None;
		options.DataModel.OrderFindParametersByColumnOrdinal = true;

		// set code generation options
		options.CodeGeneration.EnableNullableReferenceTypes  = false;
		options.CodeGeneration.SuppressMissingXmlDocWarnings = true;
		options.CodeGeneration.MarkAsAutoGenerated           = false;
		options.CodeGeneration.ClassPerFile                  = false;
		options.CodeGeneration.Namespace                     = contextNamespace;

		return options;
	}

	public static (List<ExplorerItem> items, string sourceCode, string providerAssemblyLocation) GetModel(
		ConnectionSettings settings,
		ref string?        contextNamespace,
		ref string         contextName)
	{
		var scaffoldOptions  = GetOptions(settings, contextNamespace, contextName);

		var provider         = DatabaseProviders.GetDataProvider(settings);

		using var db         = new DataConnection(new DataOptions().UseConnectionString(settings.Connection.GetFullConnectionString()!).UseDataProvider(provider));
		if (settings.Connection.CommandTimeout != null)
			db.CommandTimeout = settings.Connection.CommandTimeout.Value;

		var providerAssemblyLocation = db.DataProvider.DataReaderType.Assembly.Location;

		var sqlBuilder  = db.DataProvider.CreateSqlBuilder(db.MappingSchema, db.Options);
		var language    = LanguageProviders.CSharp;
		var interceptor = new ModelProviderInterceptor(settings, sqlBuilder);
		var generator   = new Scaffolder(language, HumanizerNameConverter.Instance, scaffoldOptions, interceptor);

		var legacySchemaProvider = new LegacySchemaProvider(db, scaffoldOptions.Schema, language);
		ISchemaProvider      schemaProvider       = legacySchemaProvider;
		ITypeMappingProvider typeMappingsProvider = legacySchemaProvider;

		DatabaseModel dataModel;
		if (settings.Connection.Database == ProviderName.Access && settings.Connection.SecondaryConnectionString != null)
		{
			var secondaryConnectionString = settings.Connection.GetFullSecondaryConnectionString()!;
			var secondaryProvider         = DatabaseProviders.GetDataProvider(settings.Connection.SecondaryProvider, secondaryConnectionString, null);
			using var sdc                 = new DataConnection(new DataOptions().UseConnectionString(secondaryConnectionString).UseDataProvider(secondaryProvider));

			if (settings.Connection.CommandTimeout != null)
				sdc.CommandTimeout = settings.Connection.CommandTimeout.Value;

			var secondLegacyProvider = new LegacySchemaProvider(sdc, scaffoldOptions.Schema, language);
			schemaProvider           = settings.Connection.Provider == ProviderName.Access
				? new MergedAccessSchemaProvider(schemaProvider, secondLegacyProvider)
				: new MergedAccessSchemaProvider(secondLegacyProvider, schemaProvider);
			typeMappingsProvider     = new AggregateTypeMappingsProvider(typeMappingsProvider, secondLegacyProvider);
			dataModel                = generator.LoadDataModel(schemaProvider, typeMappingsProvider);
		}
		else
			dataModel = generator.LoadDataModel(schemaProvider, typeMappingsProvider);

		var files = generator.GenerateCodeModel(
			sqlBuilder,
			dataModel,
			MetadataBuilders.GetMetadataBuilder(generator.Language, MetadataSource.Attributes),
			new ProviderSpecificStructsEqualityFixer(generator.Language),
			new DataModelAugmentor(language, language.TypeParser.Parse<LINQPadDataConnection>(), settings.Connection.CommandTimeout));

		// IMPORTANT:
		// real identifiers from generated code set to data model only after this line (GenerateSourceCode call)
		// so we call GetTree or read identifiers from dataModel.DataContext.Class before this line
		var sourceCode   = generator.GenerateSourceCode(dataModel, files)[0].Code;

		contextNamespace = dataModel.DataContext.Class.Namespace;
		contextName      = dataModel.DataContext.Class.Name;

		return (interceptor.GetTree(), sourceCode, providerAssemblyLocation);
	}
}

