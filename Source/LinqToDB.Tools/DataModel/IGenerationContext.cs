using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.CodeModel;
using LinqToDB.Metadata;
using LinqToDB.Scaffold;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataModel
{
	public sealed record class FileData(CodeFile File, Dictionary<string, ClassGroup> ClassesPerNamespace);

	public interface IDataModelGenerationContext
	{
		DatabaseModel     Model                  { get; }
		ScaffoldOptions   Options                { get; }
		CodeBuilder       AST                    { get; }
		CodeClass         NonTableFunctionsClass { get; }
		CodeClass         TableFunctionsClass    { get; }
		CodeClass         MainContextClass       { get; }
		CodeClass         CurrentContextClass    { get; }
		IMetadataBuilder? MetadataBuilder        { get; }
		BlockBuilder      StaticInitializer      { get; }
		CodeReference     ContextMappingSchema   { get; }
		SchemaModelBase   Schema                 { get; }
		IType ProcedureContextParameterType { get; }
		ClassBuilder ExtensionsClass { get; }
		ClassBuilder MainContextBuilder { get; }

		ICodeExpression ContextReference { get; }

		ILanguageProvider LanguageProvider { get; }

		/// <summary>
		/// Temporary (hopefully) helper to generate fully-qualified procedure or function name.
		/// </summary>
		/// <param name="routineName">Procedure/function object name.</param>
		/// <returns>Fully-qualified name of procedure or function.</returns>
		string MakeFullyQualifiedRoutineName(SqlObjectName routineName);
		CodeProperty GetColumnProperty(ColumnModel model);
		void RegisterColumnProperty(ColumnModel model, CodeProperty property);
		ClassBuilder  GetEntityBuilder             (EntityModel model);
		void RegisterEntityBuilder(EntityModel model, ClassBuilder builder);
		RegionBuilder AddTableFunctionRegion       (string regionName);
		RegionBuilder AddScalarFunctionRegion      (string regionName);
		RegionBuilder AddAggregateFunctionRegion   (string regionName);
		RegionBuilder AddStoredProcedureRegion     (string regionName);

		MethodGroup  GetEntityAssociationExtensionsGroup(EntityModel entity);
		PropertyGroup GetEntityAssociationsGroup(EntityModel entity);
		IDataModelGenerationContext GetChildContext(AdditionalSchemaModel schema);
		void RegisterChildContext(AdditionalSchemaModel schema, IDataModelGenerationContext context);

		PropertyGroup ContextProperties { get; }

		MethodGroup FindExtensionsGroup { get; }

		string NormalizeParameterName(string parameterName);

		FileData AddFile(string fileName);

		IEnumerable<CodeFile> Files { get; }

		bool TryGetFile(string fileName, [NotNullWhen(true)] out FileData? file);
	}

	internal sealed class DataModelGenerationContext : IDataModelGenerationContext
	{
		private readonly ScaffoldOptions _options;
		private readonly ILanguageProvider _languageProvider;
		private readonly ISqlBuilder _sqlBuilder;
		private readonly DatabaseModel _model;
		private readonly ClassBuilder _dataContextClass;
		private readonly IMetadataBuilder? _metadataBuilder;

		private RegionGroup? _tableFunctions;
		private RegionGroup? _storedProcedures;
		private RegionGroup? _scalarFunctions;
		private RegionGroup? _aggregateFunctions;

		private ClassBuilder? _extensionsClass;

		private readonly Dictionary<AdditionalSchemaModel, IDataModelGenerationContext> _schemaContexts = new Dictionary<AdditionalSchemaModel, IDataModelGenerationContext>();

		private readonly IType _procedureContextParameterType;
		private CodeReference? _mappingSchema;

		// stores entity column AST builder for each entity column
		// used to map entity column descriptor to generated class e.g. for association generation logic or Find method generator
		private readonly Dictionary<ColumnModel, CodeProperty>    _columnProperties = new ();

		public DataModelGenerationContext(
			ScaffoldOptions options,
			ILanguageProvider languageProvider,
			DatabaseModel model,
			ISqlBuilder sqlBuilder,
			ClassBuilder dataContextClass,
			IMetadataBuilder? metadataBuilder,
			IType procedureContextParameterType)
		{
			_options = options;
			_languageProvider = languageProvider;
			_model = model;
			_sqlBuilder = sqlBuilder;
			_dataContextClass = dataContextClass;
			_metadataBuilder = metadataBuilder;
			_procedureContextParameterType = procedureContextParameterType;
		}

		// generated AST files
		private readonly Dictionary<string, FileData> _files = new ();

		FileData IDataModelGenerationContext.AddFile(string fileName)
		{
			var file = _languageProvider.ASTBuilder.File(fileName);
			var fileData = new FileData(file, new());
			_files.Add(fileName, fileData);
			return fileData;
		}

		IEnumerable<CodeFile> IDataModelGenerationContext.Files => _files.Values.Select(_ => _.file);

		bool IDataModelGenerationContext.TryGetFile(string fileName, [NotNullWhen(true)] out FileData? file) => _files.TryGetValue(fileName, out file);

		CodeProperty IDataModelGenerationContext.GetColumnProperty(ColumnModel model) => _columnProperties[model];
		void IDataModelGenerationContext.RegisterColumnProperty(ColumnModel model, CodeProperty property)
		{
			_columnProperties.Add(model, property);
		}

		ICodeExpression IDataModelGenerationContext.ContextReference => _dataContextClass.Type.This;

		ScaffoldOptions IDataModelGenerationContext.Options => _options;
		CodeBuilder IDataModelGenerationContext.AST => _languageProvider.ASTBuilder;
		DatabaseModel IDataModelGenerationContext.Model => _model;
		CodeClass IDataModelGenerationContext.MainContextClass => _dataContextClass.Type;
		CodeClass IDataModelGenerationContext.CurrentContextClass => _dataContextClass.Type;
		IMetadataBuilder? IDataModelGenerationContext.MetadataBuilder => _metadataBuilder;

		SchemaModelBase IDataModelGenerationContext.Schema => _model.DataContext;
		ClassBuilder IDataModelGenerationContext.ExtensionsClass => GetExtensionsClass();

		IType IDataModelGenerationContext.ProcedureContextParameterType => _procedureContextParameterType;

		CodeClass IDataModelGenerationContext.NonTableFunctionsClass => throw new NotImplementedException();
		CodeClass IDataModelGenerationContext.TableFunctionsClass => throw new NotImplementedException();

		string IDataModelGenerationContext.NormalizeParameterName(string parameterName) => throw new NotImplementedException();

		//
		// stores entity class AST builder for each entity
		// used to map entity descriptor to generated class e.g. for association generation logic
		private readonly Dictionary<EntityModel, ClassBuilder>    _entityBuilders   = new ();

		ClassBuilder IDataModelGenerationContext.GetEntityBuilder(EntityModel model)
		{
			// data model misconfiguration. E.g. entity itself was removed from model, but there is association model for this entity
			if (!_entityBuilders.TryGetValue(model, out var builder))
				throw new InvalidOperationException($"Data model contains reference to unknown entity: {model.Class.Name} ({model.Metadata.Name}).");

			return builder;
		}

		private MethodGroup? _findExtensions;
		MethodGroup IDataModelGenerationContext.FindExtensionsGroup => _findExtensions ??= GetExtensionsClass().Regions().New(DataModelGenerator.FIND_METHODS_REGION).Methods(false);

		private PropertyGroup? _contextProperties;
		PropertyGroup IDataModelGenerationContext.ContextProperties => _contextProperties ??= _dataContextClass.Properties(true);

		void RegisterEntityBuilder(EntityModel model, ClassBuilder builder)
		{
			_entityBuilders.Add(model, builder);
		}
		RegionBuilder IDataModelGenerationContext.AddAggregateFunctionRegion(string regionName)
		{
			return (_aggregateFunctions ??= GetExtensionsClass().Regions().New(DataModelGenerator.EXTENSIONS_AGGREGATES_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddScalarFunctionRegion(string regionName)
		{
			return (_scalarFunctions ??= GetExtensionsClass().Regions().New(DataModelGenerator.EXTENSIONS_SCALAR_FUNCTIONS_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddStoredProcedureRegion(string regionName)
		{
			return (_storedProcedures ??= GetExtensionsClass().Regions().New(DataModelGenerator.EXTENSIONS_STORED_PROCEDURES_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddTableFunctionRegion(string regionName)
		{
			return (_tableFunctions ??= _dataContextClass.Regions().New(DataModelGenerator.CONTEXT_TABLE_FUNCTIONS_REGION).Regions()).New(regionName);
		}

		private Dictionary<EntityModel, PropertyGroup>? _entityAssociationsGroup;
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
								.New(DataModelGenerator.ENTITY_ASSOCIATIONS_REGION)
									.Properties(false));
			}

			return group;
		}

		private Dictionary<EntityModel, MethodGroup>? _entityAssociationExtensions;
		MethodGroup IDataModelGenerationContext.GetEntityAssociationExtensionsGroup(EntityModel entity)
		{
			if (_entityAssociationExtensions == null || !_entityAssociationExtensions.TryGetValue(entity, out var group))
			{
				(_entityAssociationExtensions ??= new())
					.Add(entity, group = GetAssociationExtensionsRegion().New(string.Format(DataModelGenerator.EXTENSIONS_ENTITY_ASSOCIATIONS_REGION, entity.Class.Name)).Methods(false));
			}

			return group;
		}

		private RegionGroup? _associationExtensions;
		private RegionGroup GetAssociationExtensionsRegion()
		{
			return _associationExtensions ??= GetExtensionsClass()
				.Regions()
						.New(DataModelGenerator.EXTENSIONS_ASSOCIATIONS_REGION)
							.Regions();
		}

		string IDataModelGenerationContext.MakeFullyQualifiedRoutineName(SqlObjectName routineName)
		{
			// TODO: linq2db refactoring
			// This method needed only because right now we don't have API that accepts name components
			// for some function API
			return _sqlBuilder.BuildObjectName(new (), routineName, ConvertType.NameToProcedure).ToString();
		}

		private ClassBuilder GetExtensionsClass()
		{
			return _extensionsClass ??= _dataContextClass.Group
				.New(_languageProvider.ASTBuilder.Name(DataModelGenerator.EXTENSIONS_CLASS))
				.SetModifiers(Modifiers.Public | Modifiers.Static | Modifiers.Partial);
		}

		IDataModelGenerationContext IDataModelGenerationContext.GetChildContext(AdditionalSchemaModel schema) => _schemaContexts[schema];

		void IDataModelGenerationContext.RegisterChildContext(AdditionalSchemaModel schema, IDataModelGenerationContext context)
		{
			_schemaContexts.Add(schema, context);
		}

		private BlockBuilder? _cctorBody;
		BlockBuilder IDataModelGenerationContext.StaticInitializer => _cctorBody ??= _dataContextClass.TypeInitializer().Body();

		CodeReference IDataModelGenerationContext.ContextMappingSchema
		{
			get
			{
				if (_mappingSchema == null)
				{
					// declare static mapping schema property:
					//
					// public static MappingSchema ContextSchema { get; } = new ();
					//
					_mappingSchema = _dataContextClass
						.Properties(true)
							.New(_languageProvider.ASTBuilder.Name(DataModelGenerator.CONTEXT_SCHEMA_PROPERTY), WellKnownTypes.LinqToDB.Mapping.MappingSchema)
								.SetModifiers(Modifiers.Public | Modifiers.Static)
								.Default(false)
								.SetInitializer(_languageProvider.ASTBuilder.New(WellKnownTypes.LinqToDB.Mapping.MappingSchema))
						.Property.Reference;
				}

				return _mappingSchema;
			}
		}

	}

	internal sealed class NestedSchemaGenerationContext : IDataModelGenerationContext
	{
		private readonly IDataModelGenerationContext _parentContext;
		private readonly ClassBuilder _schemaWrapper;
		private readonly ClassBuilder _schemaContext;
		private readonly  AdditionalSchemaModel _schema;

		private RegionGroup? _tableFunctions;
		private RegionGroup? _storedProcedures;
		private RegionGroup? _scalarFunctions;
		private RegionGroup? _aggregateFunctions;

		public NestedSchemaGenerationContext(IDataModelGenerationContext parentContext, AdditionalSchemaModel schema, ClassBuilder schemaWrapper, ClassBuilder schemaContext)
		{
			_parentContext = parentContext;
			_schema = schema;
			_schemaWrapper = schemaWrapper;
			_schemaContext = schemaContext;
			_contextReference = parentContext.AST.Member(schemaContext.Type.This, ctxField.Field.Reference);
		}

		private readonly ICodeExpression _contextReference;
		ICodeExpression IDataModelGenerationContext.ContextReference => _contextReference;

		ScaffoldOptions IDataModelGenerationContext.Options => _parentContext.Options;
		CodeBuilder IDataModelGenerationContext.AST => _parentContext.AST;
		CodeClass IDataModelGenerationContext.MainContextClass => _parentContext.MainContextClass;
		DatabaseModel IDataModelGenerationContext.Model => _parentContext.Model;
		CodeClass IDataModelGenerationContext.CurrentContextClass => _schemaContext.Type;
		BlockBuilder IDataModelGenerationContext.StaticInitializer => _parentContext.StaticInitializer;
		CodeReference IDataModelGenerationContext.ContextMappingSchema => _parentContext.ContextMappingSchema;
		IMetadataBuilder? IDataModelGenerationContext.MetadataBuilder => _parentContext.MetadataBuilder;
		SchemaModelBase IDataModelGenerationContext.Schema => _schema;
		IType IDataModelGenerationContext.ProcedureContextParameterType => _parentContext.ProcedureContextParameterType;
		ClassBuilder IDataModelGenerationContext.ExtensionsClass => _parentContext.ExtensionsClass;

		CodeClass IDataModelGenerationContext.NonTableFunctionsClass => throw new NotImplementedException();
		CodeClass IDataModelGenerationContext.TableFunctionsClass => throw new NotImplementedException();

		IDataModelGenerationContext GetChildContext(AdditionalSchemaModel schema) => throw new InvalidOperationException();

		RegionBuilder IDataModelGenerationContext.AddAggregateFunctionRegion(string regionName)
		{
			return (_aggregateFunctions ??= _schemaWrapper.Regions().New(DataModelGenerator.EXTENSIONS_AGGREGATES_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddScalarFunctionRegion(string regionName)
		{
			return (_scalarFunctions ??= _schemaWrapper.Regions().New(DataModelGenerator.EXTENSIONS_SCALAR_FUNCTIONS_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddStoredProcedureRegion(string regionName)
		{
			return (_storedProcedures ??= _schemaWrapper.Regions().New(DataModelGenerator.EXTENSIONS_STORED_PROCEDURES_REGION).Regions()).New(regionName);
		}
		RegionBuilder IDataModelGenerationContext.AddTableFunctionRegion(string regionName)
		{
			return (_tableFunctions ??= _schemaContext.Regions().New(DataModelGenerator.CONTEXT_TABLE_FUNCTIONS_REGION).Regions()).New(regionName);
		}

		ClassBuilder IDataModelGenerationContext.GetEntityBuilder(EntityModel model) => _parentContext.GetEntityBuilder(model);

		string IDataModelGenerationContext.MakeFullyQualifiedRoutineName(SqlObjectName routineName) => _parentContext.MakeFullyQualifiedRoutineName(routineName);

		private MethodGroup? _findExtensions;
		MethodGroup IDataModelGenerationContext.FindExtensionsGroup => _findExtensions ??= _schemaWrapper.Regions().New(DataModelGenerator.FIND_METHODS_REGION).Methods(false);

		private PropertyGroup? _contextProperties;
		PropertyGroup IDataModelGenerationContext.ContextProperties => _contextProperties ??= _schemaContext.Properties(true);
	}
}
