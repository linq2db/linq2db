using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Metadata;
using LinqToDB.Scaffold;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataModel
{
	internal sealed class DataModelGenerationContext : IDataModelGenerationContext
	{
		private readonly DataModelOptions                                               _options;
		private readonly ILanguageProvider                                              _languageProvider;
		private readonly ISqlBuilder                                                    _sqlBuilder;
		private readonly DatabaseModel                                                  _model;
		private readonly ClassBuilder                                                   _dataContextClass;
		private readonly PropertyGroup                                                  _staticProperties;
		private readonly ConstructorGroup                                               _constructors;
		private readonly MethodGroup                                                    _partialMethods;
		private readonly IMetadataBuilder?                                              _metadataBuilder;
		private readonly IType                                                          _procedureContextParameterType;
		private readonly Func<string, string>                                           _parameterNameNormalizer;

		private readonly Dictionary<ColumnModel, CodeProperty>                          _columnProperties = new ();
		private readonly Dictionary<AdditionalSchemaModel, IDataModelGenerationContext> _schemaContexts   = new ();
		private readonly Dictionary<string, FileData>                                   _files            = new ();
		private readonly Dictionary<EntityModel, ClassBuilder>                          _entityBuilders   = new ();

		private RegionGroup?                            _tableFunctions;
		private RegionGroup?                            _storedProcedures;
		private RegionGroup?                            _scalarFunctions;
		private RegionGroup?                            _aggregateFunctions;
		private ClassBuilder?                           _extensionsClass;
		private CodeReference?                          _mappingSchema;
		private RegionBuilder?                          _schemas;
		private MethodGroup?                            _findExtensions;
		private PropertyGroup?                          _contextProperties;
		private BlockBuilder?                           _cctorBody;
		private RegionGroup?                            _associationExtensions;
		private Dictionary<EntityModel, PropertyGroup>? _entityAssociationsGroup;
		private Dictionary<EntityModel, MethodGroup>?   _entityAssociationExtensions;

		public DataModelGenerationContext(
			DataModelOptions     options,
			ILanguageProvider    languageProvider,
			DatabaseModel        model,
			ISqlBuilder          sqlBuilder,
			IMetadataBuilder?    metadataBuilder,
			Func<string, string> parameterNameNormalizer)
		{
			_options                 = options;
			_languageProvider        = languageProvider;
			_model                   = model;
			_sqlBuilder              = sqlBuilder;
			_metadataBuilder         = metadataBuilder;
			_parameterNameNormalizer = parameterNameNormalizer;
			_dataContextClass        = this.DefineFileClass(_model.DataContext.Class);
			_staticProperties        = _dataContextClass.Properties(true);
			_constructors            = _dataContextClass.Constructors();
			_partialMethods          = _dataContextClass.Methods(true);

			// TODO: linq2db refactoring
			// check wether custom data context is inherited from DataConnection
			var contextIsDataConnection   = _model.DataContext.Class.BaseType != null && _languageProvider.TypeEqualityComparerWithoutNRT.Equals(_model.DataContext.Class.BaseType, WellKnownTypes.LinqToDB.Data.DataConnection);
			// currently stored procedures API requires DataConnection-based context
			// so we cannot use generated data context type for parameter if it is not inherited from DataConnection
			_procedureContextParameterType = contextIsDataConnection ? _dataContextClass.Type.Type : WellKnownTypes.LinqToDB.Data.DataConnection;
		}

		ILanguageProvider     IDataModelGenerationContext.LanguageProvider              => _languageProvider;
		DatabaseModel         IDataModelGenerationContext.Model                         => _model;
		DataModelOptions      IDataModelGenerationContext.Options                       => _options;
		CodeBuilder           IDataModelGenerationContext.AST                           => _languageProvider.ASTBuilder;
		IMetadataBuilder?     IDataModelGenerationContext.MetadataBuilder               => _metadataBuilder;

		ClassBuilder          IDataModelGenerationContext.MainDataContext               => _dataContextClass;
		ConstructorGroup      IDataModelGenerationContext.MainDataContextConstructors   => _constructors;
		MethodGroup           IDataModelGenerationContext.MainDataContextPartialMethods => _partialMethods;
		ClassBuilder          IDataModelGenerationContext.CurrentDataContext            => _dataContextClass;
		ICodeExpression       IDataModelGenerationContext.ContextReference              => _dataContextClass.Type.This;

		SchemaModelBase       IDataModelGenerationContext.Schema                        => _model.DataContext;
		ClassBuilder          IDataModelGenerationContext.ExtensionsClass               => GetExtensionsClass();
		IType                 IDataModelGenerationContext.ProcedureContextParameterType => _procedureContextParameterType;
		CodeClass             IDataModelGenerationContext.NonTableFunctionsClass        => GetExtensionsClass().Type;
		CodeClass             IDataModelGenerationContext.TableFunctionsClass           => _dataContextClass.Type;
		RegionBuilder         IDataModelGenerationContext.SchemasContextRegion          => _schemas ??= _dataContextClass.Regions().New(DataModelConstants.SCHEMAS_CONTEXT_REGION);
		IEnumerable<CodeFile> IDataModelGenerationContext.Files                         => _files.Values.Select(_ => _.File);
		MethodGroup           IDataModelGenerationContext.FindExtensionsGroup           => _findExtensions ??= GetExtensionsClass().Regions().New(DataModelConstants.FIND_METHODS_REGION).Methods(false);
		PropertyGroup         IDataModelGenerationContext.ContextProperties             => _contextProperties ??= _dataContextClass.Properties(true);
		BlockBuilder          IDataModelGenerationContext.StaticInitializer             => _cctorBody ??= _dataContextClass.TypeInitializer().Body();
		bool                  IDataModelGenerationContext.HasContextMappingSchema       => _mappingSchema != null;

		CodeReference IDataModelGenerationContext.ContextMappingSchema
		{
			get
			{
				// declare static mapping schema property:
				//
				// public static MappingSchema ContextSchema { get; } = new ();
				//
				return _mappingSchema ??= _staticProperties
					.New(_languageProvider.ASTBuilder.Name(DataModelConstants.CONTEXT_SCHEMA_PROPERTY), WellKnownTypes.LinqToDB.Mapping.MappingSchema)
						.SetModifiers(Modifiers.Public | Modifiers.Static)
						.Default(false)
						.SetInitializer(_languageProvider.ASTBuilder.New(WellKnownTypes.LinqToDB.Mapping.MappingSchema))
				.Property.Reference;
			}
		}

		CodeProperty                IDataModelGenerationContext.GetColumnProperty     (ColumnModel model                                                ) => _columnProperties[model];
		bool                        IDataModelGenerationContext.TryGetFile            (string fileName, [NotNullWhen(true)] out FileData? file          ) => _files.TryGetValue(fileName, out file);
		string                      IDataModelGenerationContext.NormalizeParameterName(string parameterName                                             ) => _parameterNameNormalizer(parameterName);
		void                        IDataModelGenerationContext.RegisterColumnProperty(ColumnModel model, CodeProperty property                         ) => _columnProperties.Add(model, property);
		IDataModelGenerationContext IDataModelGenerationContext.GetChildContext       (AdditionalSchemaModel schema                                     ) => _schemaContexts[schema];
		void                        IDataModelGenerationContext.RegisterChildContext  (AdditionalSchemaModel schema, IDataModelGenerationContext context) => _schemaContexts.Add(schema, context);
		void                        IDataModelGenerationContext.RegisterEntityBuilder (EntityModel model, ClassBuilder builder                          ) => _entityBuilders.Add(model, builder);

		FileData IDataModelGenerationContext.AddFile(string fileName)
		{
			var file = _languageProvider.ASTBuilder.File(fileName);
			var fileData = new FileData(file, new());
			_files.Add(fileName, fileData);
			return fileData;
		}

		ClassBuilder IDataModelGenerationContext.GetEntityBuilder(EntityModel model)
		{
			// data model misconfiguration. E.g. entity itself was removed from model, but there is association model for this entity
			if (!_entityBuilders.TryGetValue(model, out var builder))
				throw new InvalidOperationException($"Data model contains reference to unknown entity: {model.Class.Name} ({model.Metadata.Name}).");

			return builder;
		}

		RegionBuilder IDataModelGenerationContext.AddAggregateFunctionRegion(string regionName)
		{
			return (_aggregateFunctions ??= GetExtensionsClass().Regions().New(DataModelConstants.EXTENSIONS_AGGREGATES_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddScalarFunctionRegion(string regionName)
		{
			return (_scalarFunctions ??= GetExtensionsClass().Regions().New(DataModelConstants.EXTENSIONS_SCALAR_FUNCTIONS_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddStoredProcedureRegion(string regionName)
		{
			return (_storedProcedures ??= GetExtensionsClass().Regions().New(DataModelConstants.EXTENSIONS_STORED_PROCEDURES_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddTableFunctionRegion(string regionName)
		{
			return (_tableFunctions ??= _dataContextClass.Regions().New(DataModelConstants.CONTEXT_TABLE_FUNCTIONS_REGION).Regions()).New(regionName);
		}

		PropertyGroup IDataModelGenerationContext.GetEntityAssociationsGroup(EntityModel entity)
		{
			if (_entityAssociationsGroup == null || !_entityAssociationsGroup.TryGetValue(entity, out var group))
			{
				(_entityAssociationsGroup ??= new())
					.Add(
						entity,
						group = ((IDataModelGenerationContext)this)
							.GetEntityBuilder(entity)
							.Regions()
								.New(DataModelConstants.ENTITY_ASSOCIATIONS_REGION)
									.Properties(false));
			}

			return group;
		}

		MethodGroup IDataModelGenerationContext.GetEntityAssociationExtensionsGroup(EntityModel entity)
		{
			if (_entityAssociationExtensions == null || !_entityAssociationExtensions.TryGetValue(entity, out var group))
			{
				(_entityAssociationExtensions ??= new())
					.Add(entity, group = GetAssociationExtensionsRegion().New(string.Format(CultureInfo.InvariantCulture, DataModelConstants.EXTENSIONS_ENTITY_ASSOCIATIONS_REGION, entity.Class.Name)).Methods(false));
			}

			return group;
		}

		string IDataModelGenerationContext.MakeFullyQualifiedRoutineName(SqlObjectName routineName)
		{
			// TODO: linq2db refactoring
			// This method needed only because right now we don't have API that accepts name components
			// for some function API
			return _sqlBuilder.BuildObjectName(new (), routineName, ConvertType.NameToProcedure).ToString();
		}

		private RegionGroup GetAssociationExtensionsRegion()
		{
			return _associationExtensions ??= GetExtensionsClass()
				.Regions()
						.New(DataModelConstants.EXTENSIONS_ASSOCIATIONS_REGION)
							.Regions();
		}

		private ClassBuilder GetExtensionsClass()
		{
			return _extensionsClass ??= _dataContextClass.Group
				.New(_languageProvider.ASTBuilder.Name(DataModelConstants.EXTENSIONS_CLASS))
				.SetModifiers(Modifiers.Public | Modifiers.Static | Modifiers.Partial);
		}
	}
}
