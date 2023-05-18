using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LinqToDB.CodeModel;
using LinqToDB.Metadata;
using LinqToDB.Scaffold;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	internal sealed class NestedSchemaGenerationContext : IDataModelGenerationContext
	{
		private readonly IDataModelGenerationContext _parentContext;
		private readonly ClassBuilder                _schemaWrapper;
		private readonly ClassBuilder                _schemaContext;
		private readonly  AdditionalSchemaModel      _schema;
		private readonly ICodeExpression             _contextReference;

		private RegionGroup?   _tableFunctions;
		private RegionGroup?   _storedProcedures;
		private RegionGroup?   _scalarFunctions;
		private RegionGroup?   _aggregateFunctions;
		private MethodGroup?   _findExtensions;
		private PropertyGroup? _contextProperties;

		public NestedSchemaGenerationContext(IDataModelGenerationContext parentContext, AdditionalSchemaModel schema, ClassBuilder schemaWrapper, ClassBuilder schemaContext, CodeReference contextSchemaField)
		{
			_parentContext    = parentContext;
			_schema           = schema;
			_schemaWrapper    = schemaWrapper;
			_schemaContext    = schemaContext;
			_contextReference = parentContext.AST.Member(schemaContext.Type.This, contextSchemaField);
		}

		ILanguageProvider     IDataModelGenerationContext.LanguageProvider              => _parentContext.LanguageProvider;
		DatabaseModel         IDataModelGenerationContext.Model                         => _parentContext.Model;
		DataModelOptions      IDataModelGenerationContext.Options                       => _parentContext.Options;
		CodeBuilder           IDataModelGenerationContext.AST                           => _parentContext.AST;
		IMetadataBuilder?     IDataModelGenerationContext.MetadataBuilder               => _parentContext.MetadataBuilder;

		ClassBuilder          IDataModelGenerationContext.MainDataContext               => _parentContext.MainDataContext;
		ConstructorGroup      IDataModelGenerationContext.MainDataContextConstructors   => _parentContext.MainDataContextConstructors;
		MethodGroup           IDataModelGenerationContext.MainDataContextPartialMethods => _parentContext.MainDataContextPartialMethods;
		ClassBuilder          IDataModelGenerationContext.CurrentDataContext            => _schemaContext;
		ICodeExpression       IDataModelGenerationContext.ContextReference              => _contextReference;

		RegionBuilder         IDataModelGenerationContext.SchemasContextRegion          => _parentContext.SchemasContextRegion;
		IEnumerable<CodeFile> IDataModelGenerationContext.Files                         => _parentContext.Files;
		BlockBuilder          IDataModelGenerationContext.StaticInitializer             => _parentContext.StaticInitializer;
		bool                  IDataModelGenerationContext.HasContextMappingSchema       => _parentContext.HasContextMappingSchema;
		CodeReference         IDataModelGenerationContext.ContextMappingSchema          => _parentContext.ContextMappingSchema;
		IType                 IDataModelGenerationContext.ProcedureContextParameterType => _parentContext.ProcedureContextParameterType;
		ClassBuilder          IDataModelGenerationContext.ExtensionsClass               => _parentContext.ExtensionsClass;
		CodeClass             IDataModelGenerationContext.NonTableFunctionsClass        => _schemaWrapper.Type;
		CodeClass             IDataModelGenerationContext.TableFunctionsClass           => _schemaContext.Type;

		MethodGroup           IDataModelGenerationContext.FindExtensionsGroup           => _findExtensions ??= _schemaWrapper.Regions().New(DataModelConstants.FIND_METHODS_REGION).Methods(false);
		PropertyGroup         IDataModelGenerationContext.ContextProperties             => _contextProperties ??= _schemaContext.Properties(true);
		SchemaModelBase       IDataModelGenerationContext.Schema                        => _schema;

		IDataModelGenerationContext IDataModelGenerationContext.GetChildContext     (AdditionalSchemaModel schema                                     ) => throw new InvalidOperationException();
		void                        IDataModelGenerationContext.RegisterChildContext(AdditionalSchemaModel schema, IDataModelGenerationContext context) => throw new InvalidOperationException();

		ClassBuilder  IDataModelGenerationContext.GetEntityBuilder                   (EntityModel model                                      ) => _parentContext.GetEntityBuilder(model);
		string        IDataModelGenerationContext.MakeFullyQualifiedRoutineName      (SqlObjectName routineName                              ) => _parentContext.MakeFullyQualifiedRoutineName(routineName);
		CodeProperty  IDataModelGenerationContext.GetColumnProperty                  (ColumnModel model                                      ) => _parentContext.GetColumnProperty(model);
		void          IDataModelGenerationContext.RegisterColumnProperty             (ColumnModel model, CodeProperty property               ) => _parentContext.RegisterColumnProperty(model, property);
		void          IDataModelGenerationContext.RegisterEntityBuilder              (EntityModel model, ClassBuilder builder                ) => _parentContext.RegisterEntityBuilder(model, builder);
		MethodGroup   IDataModelGenerationContext.GetEntityAssociationExtensionsGroup(EntityModel entity                                     ) => _parentContext.GetEntityAssociationExtensionsGroup(entity);
		PropertyGroup IDataModelGenerationContext.GetEntityAssociationsGroup         (EntityModel entity                                     ) => _parentContext.GetEntityAssociationsGroup(entity);
		string        IDataModelGenerationContext.NormalizeParameterName             (string parameterName                                   ) => _parentContext.NormalizeParameterName(parameterName);
		FileData      IDataModelGenerationContext.AddFile                            (string fileName                                        ) => _parentContext.AddFile(fileName);
		bool          IDataModelGenerationContext.TryGetFile                         (string fileName, [NotNullWhen(true)] out FileData? file) => _parentContext.TryGetFile(fileName, out file);

		RegionBuilder IDataModelGenerationContext.AddAggregateFunctionRegion(string regionName)
		{
			return (_aggregateFunctions ??= _schemaWrapper.Regions().New(DataModelConstants.EXTENSIONS_AGGREGATES_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddScalarFunctionRegion(string regionName)
		{
			return (_scalarFunctions ??= _schemaWrapper.Regions().New(DataModelConstants.EXTENSIONS_SCALAR_FUNCTIONS_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddStoredProcedureRegion(string regionName)
		{
			return (_storedProcedures ??= _schemaWrapper.Regions().New(DataModelConstants.EXTENSIONS_STORED_PROCEDURES_REGION).Regions()).New(regionName);
		}

		RegionBuilder IDataModelGenerationContext.AddTableFunctionRegion(string regionName)
		{
			return (_tableFunctions ??= _schemaContext.Regions().New(DataModelConstants.CONTEXT_TABLE_FUNCTIONS_REGION).Regions()).New(regionName);
		}
	}
}
