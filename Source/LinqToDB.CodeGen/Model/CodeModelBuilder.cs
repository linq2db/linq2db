using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;
using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public class CodeModelBuilder
	{
		//private const string MAPPING_SCHEMA_INIT_METHOD = "InitMappingSchema";

		private readonly CodeGenerationSettings _settings;
		private readonly INameConverterProvider _pluralizationProvider;
		private readonly ILanguageProvider _languageProvider;
		private readonly CodeBuilder _builder;

		private readonly ISet<IType> _nonBooleanEqualityTypes = new HashSet<IType>(TypeComparer.IgnoreNRT);

		private readonly bool _disableXmlWarn;
		private readonly bool _nrtEnabled;
		private readonly  IMetadataBuilder _metadataBuilder;
		private readonly NamingServices _namingServices;

		private readonly Dictionary<EntityModel, ClassBuilder> _entityBuilders = new ();
		private readonly Dictionary<EntityModel, PropertyGroup> _entityAssociations = new ();
		private readonly Dictionary<EntityModel, MethodGroup> _extensionEntityAssociations = new ();
		private readonly Dictionary<ColumnModel, PropertyBuilder> _columnBuilders = new ();
		private readonly Dictionary<ExplicitSchemaModel, (ClassGroup storedProcedureClassGroup, ClassBuilder contextClass, ClassBuilder schemaClass)> _schemaProperties = new ();

		private readonly ISqlBuilder _sqlBuilder;

		public CodeModelBuilder(
			CodeGenerationSettings settings,
			INameConverterProvider pluralizationProvider,
			ILanguageProvider languageProvider,
			IMetadataBuilder metadataBuilder,
			CodeBuilder codeBuilder,
			NamingServices namingServices,
			ISqlBuilder sqlBuilder)
		{
			_settings              = settings;
			_pluralizationProvider = pluralizationProvider;
			_languageProvider      = languageProvider;
			_metadataBuilder       = metadataBuilder;
			_builder               = codeBuilder;
			_namingServices        = namingServices;
			_sqlBuilder            = sqlBuilder;

			foreach (var (typeName, isValueType) in settings.NonBooleanEqualityTypes)
				_nonBooleanEqualityTypes.Add(_languageProvider.TypeParser.Parse(typeName, isValueType));

			_disableXmlWarn = _languageProvider.MissingXmlCommentWarnCode != null && _settings.SuppressMissingXmlCommentWarnings;
			_nrtEnabled = _languageProvider.NRTSupported && _settings.NullableReferenceTypes;
		}

		public string[] GetSourceCode(CodeFile[] files)
		{
			var results = new string[files.Length];

			var namesNormalize = _languageProvider.GetIdentifiersNormalizer();
			foreach (var file in files)
				namesNormalize.Visit(file);

			var importsCollector = new ImportsCollector(_languageProvider);
			var nameScopes = new NameScopesCollector(_languageProvider);
			foreach (var file in files)
				nameScopes.Visit(file);

			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];

				importsCollector.Reset();
				importsCollector.Visit(file);

				foreach (var import in importsCollector.Imports.OrderBy(_ => _, CodeIdentifierComparer.Instance))
					file.Imports.Add(_builder.Import(import));

				foreach (var name in _settings.ConflictingNames)
				{
					// TODO: add separate method to parse names only
					var parsedName = _languageProvider.TypeParser.Parse(name, false);
					if (parsedName is RegularType type && type.Parent == null)
					{
						var scope = type.Namespace ?? Array.Empty<CodeIdentifier>();
						if (!nameScopes.ScopesWithNames.TryGetValue(scope, out var names))
							nameScopes.ScopesWithNames.Add(scope, names = new HashSet<CodeIdentifier>(_languageProvider.IdentifierComparer));
						names.Add(type.Name);
					}
					else
						throw new InvalidOperationException($"Cannot parse name: {name}");
				}

				var codeGenerator = _languageProvider.GetCodeGenerator(
					_settings.NewLine,
					_settings.Indent ?? "\t",
					_settings.NullableReferenceTypes,
					nameScopes.TypesNamespaces,
					nameScopes.ScopesWithNames);

				codeGenerator.Visit(file);

				results[i] = codeGenerator.GetResult();
			}

			return results;
		}

		public CodeFile[] ConvertToCodeModel(DataModel dataModel)
		{
			var files = new List<CodeFile>();
			var file = CreateFile(dataModel, dataModel.DataContext.Class.Name);
			files.Add(file);
			BuildDataContext(file, dataModel, files);

			var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in files)
			{
				var fileName = f.FileName;
				var cnt = 0;
				while (!fileNames.Add(fileName))
				{
					cnt++;
					fileName = f.FileName + cnt.ToString(NumberFormatInfo.InvariantInfo);
				}

				f.FileName = fileName;
			}

			return files.ToArray();
		}

		private CodeFile CreateFile(DataModel dataModel, string fileName)
		{
			var file = _builder.File(fileName);
			if (dataModel.AutoGeneratedComment != null)
			{
				// note that roslyn compiler disables NRT for files for backward compatibility and they should be re-enabled explicitly
				file.Header.Add(_builder.Commentary("---------------------------------------------------------------------------------------------------", false));
				file.Header.Add(_builder.Commentary("<auto-generated>", false));
				file.Header.Add(_builder.Commentary(dataModel.AutoGeneratedComment, false));
				file.Header.Add(_builder.Commentary("</auto-generated>", false));
				file.Header.Add(_builder.Commentary("---------------------------------------------------------------------------------------------------", false));
			}

			// configure compiler options
			if (_disableXmlWarn)
				file.Add(_builder.DisableWarnings(_languageProvider.MissingXmlCommentWarnCode!));

			if (_nrtEnabled && dataModel.AutoGeneratedComment != null)
				file.Add(_builder.EnableNullableReferenceTypes());

			if (_disableXmlWarn || (_nrtEnabled && dataModel.AutoGeneratedComment != null))
				file.Add(_builder.NewLine);

			return file;
		}

		private void BuildDataContext(CodeFile contextFile, DataModel dataModel, List<CodeFile> files)
		{
			var dataContext = dataModel.DataContext;

			ClassGroup fileClasses;
			ClassGroup proceduresGroup;
			ClassGroup functionsGroup;
			NamespaceBuilder? ns = null;
			if (dataContext.Class.Namespace != null)
			{
				ns = _builder.Namespace(dataContext.Class.Namespace);
				contextFile.Add(ns.Namespace);
				fileClasses = ns.Classes();
				proceduresGroup = ns.Classes();
				functionsGroup = ns.Classes();
			}
			else
			{
				contextFile.Add(fileClasses = new ClassGroup(null));
				contextFile.Add(proceduresGroup = new ClassGroup(null));
				contextFile.Add(functionsGroup = new ClassGroup(null));
			}

			var contextBuilder = DeclareClass(fileClasses, dataContext.Class);

			var contextIsDataConnection = contextBuilder.Type.Inherits != null && TypeComparer.IgnoreNRT.Equals(contextBuilder.Type.Inherits.Type, _languageProvider.TypeParser.Parse<DataConnection>());

			_staticContextProperties = contextBuilder.Properties(true);
			var contextTableProperties = contextBuilder.Properties(true);
			var schemasRegion = contextBuilder.Regions().New("Schemas");
			var contextConstructors = contextBuilder.Constructors();
			var contextPartialMethods = contextBuilder.Methods(true);
			var tableFunctions = contextBuilder.Regions().New("Table Functions").Regions();

			var entityGroups = new Dictionary<string, ClassGroup>();

			var entityClasses = ns?.Classes() ?? new ClassGroup(null);
			if (ns == null)
				contextFile.Add(entityClasses);

			MethodGroup? findMethodsGroup = null;
			RegionGroup? assocationExtensionsRegions = null;

			MethodGroup getFindMethodsGroup()
			{
				if (findMethodsGroup == null)
				{
					var extensionsClass = fileClasses.New(_builder.Identifier("TableExtensions"))
						.Public()
						.Static()
						.Partial();
					findMethodsGroup = extensionsClass.Methods(false);
					assocationExtensionsRegions = extensionsClass.Regions().New("Associations").Regions();
				}

				return findMethodsGroup;
			}

			RegionGroup getAssociationsRegion()
			{
				if (assocationExtensionsRegions == null)
				{
					var extensionsClass = fileClasses.New(_builder.Identifier("TableExtensions"))
						.Public()
						.Static()
						.Partial();
					findMethodsGroup = extensionsClass.Methods(false);
					assocationExtensionsRegions = extensionsClass.Regions().New("Associations").Regions();
				}

				return assocationExtensionsRegions;
			}

			BuildContextEntities(
				dataContext.Entities,
				contextFile,
				dataModel,
				files,
				dataContext,
				fileClasses,
				contextTableProperties,
				entityGroups,
				entityClasses,
				getFindMethodsGroup,
				null,
				null,
				contextIsDataConnection
					? null
					: _builder.Type(typeof(DataExtensions), false),
				null);

			CodeIdentifier? initSchemasName = null;
			if (dataContext.AdditionalSchemas.Count > 0)
			{
				var schemaProperties = schemasRegion.Properties(true);

				var initMethod = schemasRegion.Methods(false).New(_builder.Identifier("InitSchemas")).Public();
				var initMethodBody = initMethod.Body();
				initSchemasName = initMethod.Method.Name;

				foreach (var extraSchema in dataContext.AdditionalSchemas.Values)
					BuildExtraSchema(contextFile, dataModel, files, dataContext, fileClasses, ns, schemaProperties, initMethodBody, extraSchema);
			}

			// constructors
			BuildContextConstructors(dataContext, contextBuilder, contextConstructors, contextPartialMethods, initSchemasName);

			foreach (var association in dataModel.Associations)
				BuildAssociation(association, getAssociationsRegion);


			// procedures and functions should be generated after entities as they could use entity classes for return parameters
			// (apply to both main context and additional schemas)
			if (dataModel.DataContext.StoredProcedures.Count > 0)
			{
				var storedProceduresClass = proceduresGroup.New(_builder.Identifier(dataModel.DataContext.Class.Name + "StoredProcedures"))
						.Public()
						.Static()
						.Partial();

				BuildStoredProcedures(dataModel.DataContext.StoredProcedures, storedProceduresClass.Regions(), true, contextBuilder, contextIsDataConnection);
			}

			if (dataModel.DataContext.AggregateFunctions.Count > 0
				|| dataModel.DataContext.ScalarFunctions.Count > 0)
			{
				var functionsClass = functionsGroup.New(_builder.Identifier("SqlFunctions"))
						.Public()
						.Static()
						.Partial();

				var regions = functionsClass.Regions();

				if (dataModel.DataContext.AggregateFunctions.Count > 0)
					BuildAggregateFunctions(dataModel.DataContext.AggregateFunctions, regions.New("Aggregates").Regions());
				if (dataModel.DataContext.ScalarFunctions.Count > 0)
					BuildScalarFunctions(dataModel.DataContext.ScalarFunctions, regions.New("Scalar Functions").Regions(), true, contextBuilder);
			}

			if (dataModel.DataContext.TableFunctions.Count > 0)
				BuildTableFunctions(dataModel.DataContext.TableFunctions, contextBuilder.Regions().New("Table Functions").Regions(), contextBuilder);

			foreach (var extraSchema in dataContext.AdditionalSchemas.Values)
				BuildExtraSchemaFunctions(dataModel, extraSchema, contextBuilder, contextIsDataConnection);
		}

		private void BuildContextConstructors(DataContextModel dataContext, ClassBuilder contextBuilder, ConstructorGroup contextConstructors, MethodGroup contextPartialMethods, CodeIdentifier? initSchemasName)
		{
			var ctors = new List<BlockBuilder>();
			if (dataContext.HasDefaultConstructor)
				ctors.Add(contextConstructors.New().Public().Body());
			if (dataContext.HasConfigurationConstructor)
			{
				var configurationParam = _builder.Parameter(_builder.Type(typeof(string), false), _builder.Identifier("configuration"), Model.ParameterDirection.In);

				ctors.Add(contextConstructors.New()
							.Parameter(configurationParam)
							.Public()
							.Base(configurationParam.Name)
							.Body());
			}
			if (dataContext.HasUntypedOptionsConstructor)
			{
				var optionsParam = _builder.Parameter(_builder.Type(typeof(LinqToDbConnectionOptions), false), _builder.Identifier("options"), Model.ParameterDirection.In);
				ctors.Add(contextConstructors.New()
							.Parameter(optionsParam)
							.Public()
							.Base(optionsParam.Name)
							.Body());
			}
			if (dataContext.HasTypedOptionsConstructor)
			{
				var typedOptionsParam = _builder.Parameter(_builder.Type(typeof(LinqToDbConnectionOptions<>), false, contextBuilder.Type.Type), _builder.Identifier("options"), Model.ParameterDirection.In);
				ctors.Add(contextConstructors.New()
							.Parameter(typedOptionsParam)
							.Public()
							.Base(typedOptionsParam.Name)
							.Body());
			}

			var initDataContext = contextPartialMethods.New(_builder.Identifier("InitDataContext")).Partial();

			foreach (var body in ctors)
			{
				if (initSchemasName != null)
					body.Append(_builder.Call(_builder.This, initSchemasName, Array.Empty<IType>(), Array.Empty<ICodeExpression>()));
				body.Append(_builder.Call(_builder.This, initDataContext.Method.Name, Array.Empty<IType>(), Array.Empty<ICodeExpression>()));
			}
		}

		private BlockBuilder? _schemaInitializer;
		private CodeIdentifier? _schemaProperty;
		private PropertyGroup? _staticContextProperties;

		private (BlockBuilder initializer, CodeIdentifier schemaProperty) GetSchemaInitializer(ClassBuilder dataContextBuilder)
		{
			if (_schemaInitializer == null)
			{
				var schemaProp = _staticContextProperties!.New(_builder.Identifier("ContextSchema"), _builder.Type(typeof(MappingSchema), false))
					.Public()
					.Static()
					.Default(false)
					.SetInitializer(_builder.New(_builder.Type(typeof(MappingSchema),false), Array.Empty<ICodeExpression>(), Array.Empty<CodeAssignment>()));
				_schemaProperty = schemaProp.Property.Name;

				var cctor = dataContextBuilder.TypeInitializer();
				_schemaInitializer = cctor.Body();
			}

			return (_schemaInitializer, _schemaProperty!);
		}

		private void BuildAssociation(
			AssociationModel association,
			Func<RegionGroup> extensionAssociations)
		{
			if (!_entityBuilders.TryGetValue(association.Source, out var sourceBuilder)
				|| !_entityBuilders.TryGetValue(association.Target, out var targetBuilder))
				return;

			// build metadata keys
			BuildAssociationMetadataKey(association, false);
			BuildAssociationMetadataKey(association, true);


			var sourceType = targetBuilder.Type.Type;
			if (association.SourceMetadata.CanBeNull)
				sourceType = sourceType.WithNullability(true);

			if (association.PropertyName != null)
			{
				if (!_entityAssociations.TryGetValue(association.Source, out var associations))
					_entityAssociations.Add(association.Source, associations = _entityBuilders[association.Source].Regions().New("Assocations").Properties(false));

				BuildAssociationProperty(
					associations,
					association.PropertyName,
					sourceType,
					association.Summary,
					association.SourceMetadata);
			}

			if (association.BackreferencePropertyName != null)
			{
				if (!_entityAssociations.TryGetValue(association.Target, out var associations))
					_entityAssociations.Add(association.Target, associations = _entityBuilders[association.Target].Regions().New("Assocations").Properties(false));

				var type = sourceBuilder.Type.Type;
				if (association.ManyToOne)
				{
					if (_settings.AssociationCollectionAsArray)
						type = _builder.ArrayType(type, false);
					else if (_settings.AssociationCollectionType != null)
						type = _builder.Type(_settings.AssociationCollectionType, false, type);
					else
						type = _builder.Type(typeof(IEnumerable<>), false, type);
				}
				else if (association.TargetMetadata.CanBeNull)
					type = type.WithNullability(true);

				BuildAssociationProperty(
					associations,
					association.BackreferencePropertyName,
					type,
					association.BackreferenceSummary,
					association.TargetMetadata);
			}

			if (association.ExtensionName != null)
			{
				if (!_extensionEntityAssociations.TryGetValue(association.Source, out var associations))
					_extensionEntityAssociations.Add(association.Source, associations = extensionAssociations().New($"{sourceBuilder.Type.Name.Name} Assocations").Methods(false));

				BuildAssociationExtension(
					associations,
					sourceBuilder,
					targetBuilder,
					association.ExtensionName,
					sourceType,
					association.Summary,
					association.SourceMetadata,
					association,
					false);
			}

			if (association.BackreferenceExtensionName != null)
			{
				if (!_extensionEntityAssociations.TryGetValue(association.Target, out var associations))
					_extensionEntityAssociations.Add(association.Target, associations = extensionAssociations().New($"{targetBuilder.Type.Name.Name} Assocations").Methods(false));

				var type = sourceBuilder.Type.Type;
				if (association.ManyToOne)
					type = _builder.Type(typeof(IQueryable<>), false, type);
				else if (association.TargetMetadata.CanBeNull)
					type = type.WithNullability(true);

				BuildAssociationExtension(
					associations,
					targetBuilder,
					sourceBuilder,
					association.BackreferenceExtensionName,
					type,
					association.BackreferenceSummary,
					association.TargetMetadata,
					association,
					true);
			}
		}

		private void BuildAssociationMetadataKey(
			AssociationModel association,
			bool backReference)
		{
			var metadata = backReference ? association.TargetMetadata : association.SourceMetadata;

			if (metadata.ExpressionPredicate != null
				|| metadata.QueryExpressionMethod != null
				|| metadata.ThisKey != null
				|| metadata.ThisKeyExpression != null
				|| metadata.OtherKey != null
				|| metadata.OtherKeyExpression != null)
				return;

			if (association.FromColumns == null
				|| association.ToColumns == null
				|| association.FromColumns.Length != association.ToColumns.Length
				|| association.FromColumns.Length == 0)
			{
				throw new InvalidOperationException($"Invalid association configuration");
			}

			var thisColumns = backReference ? association.ToColumns : association.FromColumns;
			var otherColumns = !backReference ? association.ToColumns : association.FromColumns;

			var thisBuilder = !backReference ? _entityBuilders[association.Source] : _entityBuilders[association.Target];
			var otherBuilder = backReference ? _entityBuilders[association.Source] : _entityBuilders[association.Target];

			if (association.FromColumns.Length > 1)
			{
				// generate delayed string literal
				// (delayed, because it use property names, finalized before code generation)
				metadata.ThisKeyExpression = _builder.Constant(() => getKey(_columnBuilders, thisColumns), true);
				metadata.OtherKeyExpression = _builder.Constant(() => getKey(_columnBuilders, otherColumns), true);

				static string getKey(IReadOnlyDictionary<ColumnModel, PropertyBuilder> columnBuilders, ColumnModel[] columns)
				{
					return string.Join(",", columns.Select(c => columnBuilders[c].Property.Name.Name));
				}
			}
			else if (association.FromColumns.Length == 1)
			{
				// generate nameof() expressions
				metadata.ThisKeyExpression = _builder.NameOf(_builder.Member(thisBuilder.Type.Type, _columnBuilders[thisColumns[0]].Property.Name));
				metadata.OtherKeyExpression = _builder.NameOf(_builder.Member(otherBuilder.Type.Type, _columnBuilders[otherColumns[0]].Property.Name));
			}
		}

		private void BuildAssociationProperty(
			PropertyGroup associationsGroup,
			string name,
			IType type,
			string? summary,
			AssociationMetadata metadata)
		{
			var property = new PropertyModel(name, type)
			{
				IsPublic = true,
				IsDefault = true,
				HasSetter = true,
				Summary = summary
			};
			var propertyBuilder = DeclareProperty(associationsGroup, property);

			_metadataBuilder.BuildAssociationMetadata(metadata, propertyBuilder);
		}

		private void BuildAssociationExtension(
			MethodGroup associationsGroup,
			ClassBuilder thisEntity,
			ClassBuilder resultEntity,
			string name,
			IType type,
			string? summary,
			AssociationMetadata metadata,
			AssociationModel associationModel,
			bool backReference)
		{
			var methodBuilder = associationsGroup.New(_builder.Identifier(name))
				.Public()
				.Static()
				.Extension()
				.Returns(type);

			if (summary != null)
				methodBuilder.XmlComment().Summary(summary);

			_metadataBuilder.BuildAssociationMetadata(metadata, methodBuilder);

			var thisParam = _builder.Parameter(thisEntity.Type.Type, _builder.Identifier("obj"), Model.ParameterDirection.In);
			var ctxParam = _builder.Parameter(_builder.Type(typeof(IDataContext), false), _builder.Identifier("db"), Model.ParameterDirection.In);

			methodBuilder.Parameter(thisParam);
			methodBuilder.Parameter(ctxParam);

			if (associationModel.FromColumns == null || associationModel.ToColumns == null)
			{
				methodBuilder.Body().Append(_builder.Throw(_builder.New(_languageProvider.TypeParser.Parse<InvalidOperationException>(), new ICodeExpression[] { _builder.Constant("Association cannot be called outside of query", true) }, Array.Empty<CodeAssignment>())));
			}
			else
			{
				var lambdaParam = _builder.LambdaParameter(_builder.Identifier("c"));

				ICodeExpression? filter = null;

				var fromObject = backReference ? lambdaParam : thisParam;
				var toObject = !backReference ? lambdaParam : thisParam;

				for (var i = 0; i < associationModel.FromColumns!.Length; i++)
				{
					var fromColumnBuilder = _columnBuilders[associationModel.FromColumns[i]];
					var toColumnBuilder = _columnBuilders[associationModel.ToColumns[i]];
					var cond = CorrectEquality(
						fromColumnBuilder.Property.Type.Type,
						_builder.Equal(
							_builder.Member(fromObject.Name, fromColumnBuilder.Property.Name),
							_builder.Member(toObject.Name, toColumnBuilder.Property.Name)));
					filter = filter == null ? cond : _builder.And(filter, cond);
				}

				var filterLambda = _builder.Lambda().Parameter(lambdaParam);
				filterLambda.Body().Append(_builder.Return(filter!));

				var body = _builder.ExtCall(
					_builder.Type(typeof(Queryable), false),
					_builder.Identifier(nameof(Queryable.Where)),
					Array.Empty<IType>(),
					new ICodeExpression[]
					{
						_builder.ExtCall(
							_builder.Type(typeof(DataExtensions), false),
							_builder.Identifier(nameof(DataExtensions.GetTable)),
							new[] { resultEntity.Type.Type },
							new ICodeExpression[] { ctxParam.Name }),
						filterLambda.Method
					});

				if (backReference)
				{
					if (!associationModel.ManyToOne)
						body = _builder.ExtCall(
							_builder.Type(typeof(Queryable), false),
							_builder.Identifier(associationModel.TargetMetadata.CanBeNull ? nameof(Queryable.FirstOrDefault) : nameof(Queryable.First)),
							Array.Empty<IType>(),
							new ICodeExpression[] { body });
				}
				else
				{
					body = _builder.ExtCall(
						_builder.Type(typeof(Queryable), false),
						_builder.Identifier(associationModel.SourceMetadata.CanBeNull ? nameof(Queryable.FirstOrDefault) : nameof(Queryable.First)),
						Array.Empty<IType>(),
						new ICodeExpression[] { body });
				}

				methodBuilder.Body().Append(_builder.Return(body));
			}
		}

		private void BuildExtraSchema(
			CodeFile contextFile,
			DataModel dataModel,
			List<CodeFile> files,
			DataContextModel dataContext,
			ClassGroup fileClasses,
			NamespaceBuilder? ns,
			PropertyGroup schemaProperties,
			BlockBuilder initMethodBody,
			ExplicitSchemaModel extraSchema)
		{
			var schemaClass = DeclareClass(fileClasses, extraSchema.WrapperClass);
			var contextClass = DeclareClass(schemaClass.Classes(), extraSchema.ContextClass);

			// schema in data context
			var schemaProp = schemaProperties.New(_builder.Identifier(extraSchema.DataContextPropertyName), contextClass.Type.Type)
						.Public()
						.Default(true);
			initMethodBody.Append(
				_builder.Assign(
					_builder.Member(_builder.This, schemaProp.Property.Name),
					_builder.New(contextClass.Type.Type, new ICodeExpression[] { _builder.This }, Array.Empty<CodeAssignment>())));

			// schema entities
			var entityProperties = contextClass.Properties(true);

			var storedProcedureClassGroup = schemaClass.Classes();
			var findMethods = schemaClass.Regions().New("Table Extensions").Methods(false);

			// schema context class
			var ctxField = contextClass.Fields(false).New(_builder.Identifier("_dataContext"), _builder.Type(typeof(IDataContext), false))
						.Private()
						.ReadOnly();

			BuildContextEntities(extraSchema.Entities, contextFile, dataModel, files, dataContext, fileClasses, entityProperties, null, schemaClass.Classes(), () => findMethods, ns?.Namespace.Name, schemaClass.Type.Name, null, ctxField.Field.Name);

			var ctorParam = _builder.Parameter(_builder.Type(typeof(IDataContext), false), _builder.Identifier("dataContext"), Model.ParameterDirection.In);
			contextClass.Constructors().New()
				.Public()
				.Parameter(ctorParam)
				.Body()
				.Append(_builder.Assign(ctxField.Field.Name, ctorParam.Name));

			_schemaProperties.Add(extraSchema, (storedProcedureClassGroup, contextClass, schemaClass));
		}

		private void BuildExtraSchemaFunctions(DataModel dataModel, ExplicitSchemaModel extraSchema, ClassBuilder dataContextBuilder, bool contextIsDataConnection)
		{
			var (storedProcedureClassGroup, contextClass, schemaClass) = _schemaProperties[extraSchema];

			if (extraSchema.StoredProcedures.Count > 0)
			{
				RegionGroup proceduresGroup;
				if (_settings.PutSchemaStoredProceduresToNestedClass)
				{
					var storedProceduresClass = storedProcedureClassGroup.New(_builder.Identifier(dataModel.DataContext.Class.Name + "StoredProcedures"))
						.Public()
						.Static()
						.Partial();
					proceduresGroup = storedProceduresClass.Regions();
				}
				else
					proceduresGroup = contextClass.Regions().New("Stored Procedures").Regions();

				BuildStoredProcedures(dataModel.DataContext.StoredProcedures, proceduresGroup, !_settings.PutSchemaStoredProceduresToNestedClass, contextClass, contextIsDataConnection);
			}

			if (extraSchema.AggregateFunctions.Count > 0
				|| extraSchema.ScalarFunctions.Count > 0)
			{
				RegionGroup functionRegions;
				if (_settings.PutSchemaFunctionsToNestedClass)
				{
					var functionsClass = storedProcedureClassGroup.New(_builder.Identifier("SqlFunctions"))
						.Public()
						.Static()
						.Partial();

					functionRegions = functionsClass.Regions();
				}
				else
					functionRegions = schemaClass.Regions();

				if (extraSchema.AggregateFunctions.Count > 0)
					BuildAggregateFunctions(extraSchema.AggregateFunctions, functionRegions.New("Aggregates").Regions());
				if (extraSchema.ScalarFunctions.Count > 0)
					BuildScalarFunctions(extraSchema.ScalarFunctions, functionRegions.New("Scalar Functions").Regions(), !_settings.PutSchemaFunctionsToNestedClass, dataContextBuilder);
			}

			if (extraSchema.TableFunctions.Count > 0)
				BuildTableFunctions(extraSchema.TableFunctions, contextClass.Regions().New("Table Functions").Regions(), contextClass);
		}

		private void BuildAggregateFunctions(List<AggregateFunctionModel> aggregateFunctions, RegionGroup functionsGroup)
		{
			foreach (var func in aggregateFunctions)
			{
				var region = functionsGroup.New(func.Method.Name);
				var method = DeclareMethod(region.Methods(false), func.Method, false);
				method.Extension();

				var body = method.Body().Append(
					_builder.Throw(_builder.New(_builder.Type(typeof(InvalidOperationException), false), Array.Empty<ICodeExpression>(), Array.Empty<CodeAssignment>())));

				_metadataBuilder.BuildFunctionMetadata(func.Metadata, method);

				var source = _builder.TypeParameter(_builder.Identifier("TSource"));
				method.TypeParameter(source);

				method.Returns(func.ReturnType);

				var sourceParam = _builder.Parameter(_builder.Type(typeof(IEnumerable<>), false, new[] { source }), _builder.Identifier("src"), Model.ParameterDirection.In);
				method.Parameter(sourceParam);

				if (func.Parameters.Count > 0)
				{
					var argIndexes = new ICodeExpression[func.Parameters.Count];
					for (var i = 0; i < func.Parameters.Count; i++)
					{
						var param = func.Parameters[i];

						argIndexes[i] = _builder.Constant(i + 1, true);

						var parameterType = param.Parameter.Type;

						parameterType = _builder.Type(typeof(Func<,>), false, source, parameterType);
						parameterType = _builder.Type(typeof(Expression<>), false, parameterType);

						var p = _builder.Parameter(parameterType, _builder.Identifier(param.Parameter.Name, null/*ctxModel.Parameter*/, i + 1), Model.ParameterDirection.In);
						method.Parameter(p);
						if (param.Parameter.Description != null)
							method.XmlComment().Parameter(p.Name, param.Parameter.Description);
					}
				}
			}
		}

		private void BuildTableFunctions(List<TableFunctionModel> tableFunctions, RegionGroup functionsGroup, ClassBuilder ownerType)
		{
			foreach (var func in tableFunctions)
			{
				var region = functionsGroup.New(func.Method.Name);

				if (func.Error != null)
				{
					if (_settings.GenerateProceduresSchemaError)
						region.Pragmas().Add(_builder.Error(func.Error));

					// compared to procedures, table functions with errors cannot be generated
					continue;
				}

				var (customTable, entity) = func.Result;
				if (customTable == null && entity == null)
					continue;

				var methodInfo = region.Fields(false).New(_builder.Identifier(func.MethodInfoFieldName), _builder.Type(typeof(MethodInfo), false))
								.Private()
								.Static()
								.ReadOnly();

				var method = DeclareMethod(region.Methods(false), func.Method, false);

				_metadataBuilder.BuildTableFunctionMetadata(func.Metadata, method);


				IType returnEntity;
				if (entity != null)
					returnEntity = _entityBuilders[entity].Type.Type;
				else
					returnEntity = BuildCustomResultClass(customTable!, region, true).resultClassBuilder.Type.Type;

				// T4 used ITable<> here, but I don't see a good reason to generate it as ITable<>
				var returnType = _builder.Type(_settings.TableFunctionReturnsTable ? typeof(ITable<>) : typeof(IQueryable<>), false, returnEntity);
				method.Returns(returnType);

				var parameters = new List<ICodeExpression>();
				parameters.Add(_builder.This);
				parameters.Add(_builder.This);
				parameters.Add(methodInfo.Field.Name);

				var defaultParameters = new List<ICodeExpression>();
				foreach (var param in func.Parameters)
				{
					var parameter = DeclareParameter(method, param.Parameter);
					parameters.Add(parameter.Name);
					// TODO: target-typed default could cause errors with overloads?
					defaultParameters.Add(_builder.Default(param.Parameter.Type, true));
				}

				method.Body()
					.Append(
						_builder.Return(
							_builder.ExtCall(
								_builder.Type(typeof(DataExtensions), false),
								_builder.Identifier(nameof(DataExtensions.GetTable)),
								new[] { returnEntity },
								parameters.ToArray())));

				var lambdaParam = _builder.LambdaParameter(_builder.Identifier("ctx"));
				var lambda = _builder.Lambda().Parameter(lambdaParam);
				lambda.Body().
					Append(_builder.Return(_builder.Call(
						lambdaParam.Name,
						method.Method.Name,
						Array.Empty<IType>(),
						defaultParameters.ToArray())));

				methodInfo.AddInitializer(_builder.Call(
						new CodeTypeReference(_builder.Type(typeof(MemberHelper), false)),
						_builder.Identifier(nameof(MemberHelper.MethodOf)),
						new[] { ownerType.Type.Type },
						new ICodeExpression[] { lambda.Method }));

				// TODO: similar tables
			}
		}

		private void BuildScalarFunctions(
			List<ScalarFunctionModel> scalarFunctions,
			RegionGroup functionsGroup,
			bool generateExtensionMethods,
			ClassBuilder dataContextBuilder)
		{
			foreach (var func in scalarFunctions)
			{
				var region = functionsGroup.New(func.Method.Name);
				var method = DeclareMethod(region.Methods(false), func.Method, false);

				var body = method.Body().Append(
					_builder.Throw(_builder.New(_builder.Type(typeof(InvalidOperationException), false), Array.Empty<ICodeExpression>(), Array.Empty<CodeAssignment>())));

				_metadataBuilder.BuildFunctionMetadata(func.Metadata, method);

				IType returnType;
				if (func.Return != null)
					returnType = func.Return;
				else
				{
					// T4 generated this class inside of context class, here we move it to function region
					var tupleClassBuilder = DeclareClass(region.Classes(), func.ReturnTuple!.Class);
					var tuplePropsRegion = tupleClassBuilder.Properties(true);

					var initializers = new CodeAssignment[func.ReturnTuple.Fields.Count];

					var lambdaParam = _builder.LambdaParameter(_builder.Identifier("tuple"));

					for (var i = 0; i < func.ReturnTuple!.Fields.Count; i++)
					{
						var field = func.ReturnTuple!.Fields[i];

						var property = DeclareProperty(tuplePropsRegion, field.Property);

						initializers[i] = _builder.Assign(
							property.Property.Name,
							_builder.Cast(property.Property.Type.Type, _builder.Index(lambdaParam.Name, _builder.Constant(i, true))));
					}

					var conversionLambda = _builder
						.Lambda()
						.Parameter(lambdaParam);

					conversionLambda.Body().Append(_builder.Return(_builder.New(tupleClassBuilder.Type.Type, Array.Empty<ICodeExpression>(), initializers)));

					var (initializer, schema) = GetSchemaInitializer(dataContextBuilder);
					initializer
						.Append(
						_builder.Call(
							schema,
							_builder.Identifier(nameof(MappingSchema.SetConvertExpression)),
							new IType[]
							{
								_builder.ArrayType(_builder.Type(typeof(object), true), false),
								tupleClassBuilder.Type.Type
							},
							new ICodeExpression[] { conversionLambda.Method }));

					returnType = tupleClassBuilder.Type.Type;
					if (func.ReturnTuple.CanBeNull)
						returnType = returnType.WithNullability(true);
				}

				method.Returns(returnType);

				foreach (var param in func.Parameters)
					DeclareParameter(method, param.Parameter);
			}
		}

		private MethodBuilder DeclareMethod(MethodGroup methods, MethodModel model, bool extension)
		{
			var builder = methods.New(_builder.Identifier(model.Name));

			if (model.Public) builder.Public();
			if (model.Static) builder.Static();
			if (model.Partial) builder.Partial();
			if (extension) builder.Extension();

			if (model.Summary != null)
				builder.XmlComment().Summary(model.Summary);

			return builder;
		}

		private CodeParameter DeclareParameter(MethodBuilder method, ParameterModel model)
		{
			var parameter = _builder.Parameter(model.Type, _builder.Identifier(model.Name), model.Direction);
			method.Parameter(parameter);

			if (model.Description != null)
				method.XmlComment().Parameter(parameter.Name, model.Description);

			return parameter;
		}

		private string BuildFunctionName(ObjectName name)
		{
			// TODO: as we still miss API for stored procedures and functions that takes not prepared(escaped) full name but FQN components
			// we need to generate such name from FQN
			// also we use BuildTableName as there is no API for function-like objects
			return _sqlBuilder.BuildTableName(
				new StringBuilder(),
				name.Server == null ? null : _sqlBuilder.ConvertInline(name.Server, ConvertType.NameToServer),
				name.Database == null ? null : _sqlBuilder.ConvertInline(name.Database, ConvertType.NameToDatabase),
				name.Schema == null ? null : _sqlBuilder.ConvertInline(name.Schema, ConvertType.NameToSchema),
											  // NameToQueryTable used as we don't have separate ConvertType for procedures/functions
											  _sqlBuilder.ConvertInline(name.Name, ConvertType.NameToQueryTable),
				TableOptions.NotSet
			).ToString();
		}

		// IMPORTANT:
		// ExecuteProc/QueryProc API available only in DataConnection, so if context is not based on DataConnection, we
		// should generate DataConnection parameter for context isntead of typed generated context
		// note that we shouldn't fix it by extending current API to be available to DataContext too as instead of it we need
		// to introduce new API which works with FQN procedure name components
		private void BuildStoredProcedures(
			List<StoredProcedureModel> storedProcedures,
			RegionGroup proceduresGroup,
			bool generateExtensionMethods,
			ClassBuilder dataContext,
			bool contextIsDataConnection)
		{
			foreach (var proc in storedProcedures)
			{
				var region = proceduresGroup.New(proc.Method.Name);

				if (proc.Error != null)
				{
					if (_settings.GenerateProceduresSchemaError)
						region.Pragmas().Add(_builder.Error(proc.Error));

					if (_settings.SkipProceduresWithSchemaErrors)
						continue;
				}

				var method = DeclareMethod(region.Methods(false), proc.Method, generateExtensionMethods);

				var ctxParam = _builder.Parameter(
					// see method notes
					contextIsDataConnection ? dataContext.Type.Type : _languageProvider.TypeParser.Parse<DataConnection>(),
					_builder.Identifier("dataConnection"),
					Model.ParameterDirection.In);
				method.Parameter(ctxParam);
				var body = method.Body();

				CodeVariable? parametersVar = null;
				var parameterRebinds = new List<CodeAssignment>();

				var hasParameters = proc.Parameters.Count > 0 || proc.Return != null;
				if (hasParameters)
				{
					var parameterValues = new ICodeExpression[proc.Parameters.Count + (proc.Return != null ? 1 : 0)];
					parametersVar = _builder.Variable(_builder.Identifier("parameters"), _builder.Type(typeof(DataParameter[]), false), true);
					var parametersArray = _builder.Assign(
						parametersVar,
						_builder.Array(_builder.Type(typeof(DataParameter), false), true, parameterValues, false));
					body.Append(parametersArray);

					if (proc.Parameters.Count > 0)
					{
						for (var i = 0; i < proc.Parameters.Count; i++)
						{
							var p = proc.Parameters[i];

							var param = DeclareParameter(method, p.Parameter);

							parameterValues[i] = BuildProcedureParameter(
								param,
								false,
								p.DbName,
								p.DataType,
								p.Type,
								parametersVar,
								parameterRebinds,
								proc.Parameters.Count);
						}
					}

					if (proc.Return != null)
					{
						var param = DeclareParameter(method, proc.Return.Parameter);

						parameterValues[proc.Parameters.Count] = BuildProcedureParameter(
							param,
							true,
							proc.Return.Name ?? "return",
							proc.Return.DataType,
							proc.Return.Type,
							parametersVar,
							parameterRebinds,
							proc.Parameters.Count);
					}
				}

				ICodeExpression? returnValue = null;

				if (proc.Results.Count > 1)
				{
					// TODO:
					throw new NotImplementedException($"Multiple result-set stored procedure generation not imlpemented yet");
				}
				else if (proc.Results.Count == 0)
				{
					var executeProcParameters = new ICodeExpression[hasParameters ? 3 : 2];

					executeProcParameters[0] = ctxParam.Name;
					executeProcParameters[1] = _builder.Constant(BuildFunctionName(proc.Name), true);
					if (hasParameters)
						executeProcParameters[2] = parametersVar!.Name;

					method.Returns(_builder.Type(typeof(int), false));
					returnValue = _builder.ExtCall(
						_builder.Type(typeof(DataConnectionExtensions), false),
						_builder.Identifier(nameof(DataConnectionExtensions.ExecuteProc)),
						Array.Empty<IType>(),
						executeProcParameters);
				}
				else
				{
					// if procedure result table contains unique (and not empty) column names, we have use columns mappings
					// otherwise we should bind columns manually by ordinal (as we don't have by-ordinal mapping conventions support)
					var (customTable, mappedTable) = proc.Results[0];
					IType returnElementType;

					var queryProcTypeArgs = Array.Empty<IType>();
					ICodeExpression[] queryProcParameters;

					var ordinalMapping = customTable != null && customTable.Columns.Select(c => c.Metadata.Name).Where(_ => !string.IsNullOrEmpty(_)).Distinct().Count() == customTable.Columns.Count;

					ClassBuilder? customResultClass = null;
					CodeProperty[]? customProperties = null;
					if (customTable != null)
					{
						(customResultClass, customProperties) = BuildCustomResultClass(customTable, region, !ordinalMapping);
						returnElementType = customResultClass.Type.Type;
					}
					else
						returnElementType = _entityBuilders[mappedTable!].Type.Type;

					if (ordinalMapping)
					{
						// manual mapping
						queryProcParameters = new ICodeExpression[hasParameters ? 4 : 3];

						// generate positional mapping lambda
						// TODO: switch to ColumnReader.GetValue in future to utilize more precise mapping based on column mapping attributes
						var drParam = _builder.LambdaParameter(_builder.Identifier("dataReader"));
						var initializers = new CodeAssignment[customTable!.Columns.Count];
						var lambda = _builder.Lambda().Parameter(drParam);
						queryProcParameters[1] = lambda.Method;
						lambda.Body()
							.Append(_builder.Return(
								_builder.New(
									customResultClass!.Type.Type,
									Array.Empty<ICodeExpression>(),
									initializers)));

						var ms = _builder.Member(ctxParam.Name, _builder.Identifier(nameof(DataConnection.MappingSchema)));
						for (var i = 0; i < customTable.Columns.Count; i++)
						{
							var prop = customProperties![i];
							initializers[i] = _builder.Assign(
								prop.Name,
								_builder.Call(
									new CodeTypeReference(_builder.Type(typeof(Converter), false)),
									_builder.Identifier(nameof(Converter.ChangeTypeTo)),
									new[] { prop.Type.Type },
									new ICodeExpression[]
									{
										_builder.Call(drParam.Name, _builder.Identifier(nameof(DbDataReader.GetValue)), Array.Empty<IType>(), new ICodeExpression[] { _builder.Constant(i, true) }),
										ms
									}));
						}
					}
					else
					{
						// built-in mapping by name
						queryProcParameters = new ICodeExpression[hasParameters ? 3 : 2];
						queryProcTypeArgs = new[] { returnElementType };
					}

					queryProcParameters[0] = ctxParam.Name;
					queryProcParameters[queryProcParameters.Length - (hasParameters ? 2 : 1)] = _builder.Constant(BuildFunctionName(proc.Name), true);
					if (hasParameters)
						queryProcParameters[queryProcParameters.Length - 1] = parametersVar!.Name;

					method.Returns(
						_builder.Type(
							_settings.GenerateProcedureResultAsList ? typeof(List<>) : typeof(IEnumerable<>),
							false,
							returnElementType));

					returnValue = _builder.ExtCall(
						_builder.Type(typeof(Enumerable), false),
						_builder.Identifier(nameof(Enumerable.ToList)),
						Array.Empty<IType>(),
						new[]
						{
							_builder.ExtCall(
								_builder.Type(typeof(DataConnectionExtensions), false),
								_builder.Identifier(nameof(DataConnectionExtensions.QueryProc)),
								queryProcTypeArgs,
								queryProcParameters)
						});
				}

				if (parameterRebinds.Count > 0)
				{
					var callProcVar = _builder.Variable(_builder.Identifier("ret"), method.Method.ReturnType!.Type, true);
					body.Append(_builder.Assign(callProcVar, returnValue!));
					foreach (var rebind in parameterRebinds)
						body.Append(rebind);
					body.Append(_builder.Return(callProcVar.Name));
				}
				else
					body.Append(_builder.Return(returnValue));
			}
		}

		private ICodeExpression BuildProcedureParameter(
			CodeParameter parameter,
			bool returnParameter,
			string? databaseName,
			DataType? dataType,
			DatabaseType? dbType,
			CodeVariable parametersVar,
			List<CodeAssignment> parameterRebinds,
			int idx)
		{
			var ctorParams = new List<ICodeExpression>();
			var ctorInitializers = new List<CodeAssignment>();

			ctorParams.Add(_builder.Constant(databaseName ?? $"p{idx}", true));
			ctorParams.Add(parameter.Direction == Model.ParameterDirection.In || parameter.Direction == Model.ParameterDirection.Ref ? parameter.Name : _builder.Null(_builder.Type(typeof(object), true), true));
			if (dataType != null)
				ctorParams.Add(_builder.Constant(dataType.Value, true));

			if (parameter.Direction == Model.ParameterDirection.Out && !returnParameter)
				ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Direction)), _builder.Constant(System.Data.ParameterDirection.Output, true)));
			else if (parameter.Direction == Model.ParameterDirection.Ref)
				ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Direction)), _builder.Constant(System.Data.ParameterDirection.InputOutput, true)));
			else if (returnParameter)
				ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Direction)), _builder.Constant(System.Data.ParameterDirection.ReturnValue, true)));

			if (dbType != null)
			{
				if (dbType.Name != null && _settings.GenerateProcedureParameterDbType)
					ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.DbType)), _builder.Constant(dbType.Name!, true)));
				// TODO: min/max check added to avoid issues with type inconsistance in schema API and metadata
				if (dbType.Length != null && dbType.Length >= int.MinValue && dbType.Length <= int.MaxValue)
					ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Size)), _builder.Constant((int)dbType.Length.Value, true)));
				if (dbType.Precision != null)
					ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Precision)), _builder.Constant(dbType.Precision.Value, true)));
				if (dbType.Scale != null)
					ctorInitializers.Add(_builder.Assign(_builder.Identifier(nameof(DataParameter.Scale)), _builder.Constant(dbType.Scale.Value, true)));
			}

			if (parameter.Direction != Model.ParameterDirection.In)
			{
				parameterRebinds.Add(
					_builder.Assign(
						parameter.Name,
						_builder.Call(
							new CodeTypeReference(_builder.Type(typeof(Converter), false)),
							_builder.Identifier(nameof(Converter.ChangeTypeTo)),
							new[] { parameter.Type!.Type },
							new ICodeExpression[] { _builder.Member(_builder.Index(parametersVar.Name, _builder.Constant(idx, true)), _builder.Identifier(nameof(DataParameter.Value))) })));
			}

			return _builder.New(_builder.Type(typeof(DataParameter), false), ctorParams.ToArray(), ctorInitializers.ToArray());
		}

		private void BuildContextEntities(
			IEnumerable<EntityModel> entities,
			CodeFile contextFile,
			DataModel dataModel,
			List<CodeFile> files,
			DataContextModel dataContext,
			ClassGroup fileClasses,
			PropertyGroup contextTableProperties,
			Dictionary<string, ClassGroup>? entityGroups,
			ClassGroup entityClasses,
			Func<MethodGroup> getFindMethodsGroup,
			CodeIdentifier[]? parentNamespace,
			CodeIdentifier? parentType,
			IType? getTableType,
			ICodeExpression? getTableObject)
		{
			foreach (var entity in entities.OrderBy(_ => _.Class.Name))
			{
				ClassGroup? entityClassesGroup = null;
				if (entity.FileName == null)
				{
					if (parentType == null)
					{
						if (entity.Class.Namespace == dataContext.Class.Namespace)
						{
							entityClassesGroup = entityClasses;
						}
						else
						{
							if (entityGroups!.TryGetValue(entity.Class.Namespace ?? "", out entityClassesGroup))
							{
								if (entity.Class.Namespace != null)
								{
									var entityNs = _builder.Namespace(entity.Class.Namespace);
									contextFile.Add(entityNs.Namespace);
									entityGroups.Add(entity.Class.Namespace, entityClassesGroup = entityNs.Classes());
								}
								else
								{
									entityGroups.Add("", entityClassesGroup = fileClasses);
								}
							}
						}
					}
					else
					{
						entityClassesGroup = entityClasses;
					}
				}

				var entityFile = BuildEntityClass(dataModel, entity, entityClassesGroup, contextTableProperties, getFindMethodsGroup, parentNamespace, parentType, getTableType, getTableObject);
				if (entityFile != null)
					files.Add(entityFile);
			}
		}

		private ClassBuilder DeclareClass(ClassGroup classes, ClassModel model)
		{
			var @class = classes.New(_builder.Identifier(model.Name));

			if (model.IsPublic)
				@class.Public();
			if (model.IsStatic)
				@class.Static();
			if (model.IsPartial)
				@class.Partial();

			if (model.BaseType != null)
				@class.Inherits(model.BaseType);

			if (model.Summary != null)
				@class.XmlComment().Summary(model.Summary);

			return @class;
		}

		private (ClassBuilder resultClassBuilder, CodeProperty[] properties) BuildCustomResultClass(
			ResultTableModel model,
			RegionBuilder region,
			bool withMapping)
		{
			var properties = new CodeProperty[model.Columns.Count];

			var resultClassBuilder = DeclareClass(region.Classes(), model.Class);

			var columnsGroup = resultClassBuilder.Properties(true);

			for (var i = 0; i < model.Columns.Count; i++)
			{
				var columnModel = model.Columns[i];
				var columnBuilder = DeclareProperty(columnsGroup, columnModel.Property);
				if (withMapping)
					_metadataBuilder.BuildColumnMetadata(columnModel.Metadata!, columnBuilder);
				properties[i] = columnBuilder.Property;
			}

			return (resultClassBuilder, properties);
		}

		private void BuildEntityFindExtension(
			EntityModel entityModel,
			IType entityType,
			MethodGroup findMethodsGroup)
		{
			var filterEntityParameter = _builder.LambdaParameter(_builder.Identifier("t"));

			var methodParameters = new List<CodeParameter>();

			methodParameters.Add(_builder.Parameter(_builder.Type(typeof(ITable<>), false, entityType), _builder.Identifier("table"), Model.ParameterDirection.In));

			var pks = entityModel.Columns.Where(c => c.Metadata!.IsPrimaryKey);
			if (entityModel.OrderFindParametersByOrdinal)
				pks = pks.OrderBy(c => c.Metadata!.PrimaryKeyOrder);

			var comparisons = new List<(int ordinal, ICodeExpression comparison)>();
			foreach (var column in pks)
			{
				// we don't allow user to set names for method parameters as it:
				// 1. simplify data model
				// 2. custom naming doesn't add value to generated model
				// we could change it on request later
				var paramName = _namingServices.NormalizeIdentifier(_settings.ParameterNameNormalization, column.Property.Name);

				var parameter = _builder.Parameter(column.Property.Type, _builder.Identifier(paramName), Model.ParameterDirection.In);
				methodParameters.Add(parameter);

				var cond = CorrectEquality(
					column.Property.Type,
					_builder.Equal(_builder.Member(filterEntityParameter.Name, _columnBuilders[column].Property.Name), parameter.Name));

				comparisons.Add((column.Metadata!.PrimaryKeyOrder ?? 0, cond));
			}

			IEnumerable<(int pkOrdinal, ICodeExpression comparison)> orderedComparisons = comparisons;
			if (!entityModel.OrderFindParametersByOrdinal)
			{
				// restore ordinal sorting for filter
				orderedComparisons = orderedComparisons.OrderBy(_ => _.pkOrdinal);
			}

			ICodeExpression filter = null!;
			foreach (var (_, cond) in orderedComparisons)
				filter = filter == null ? cond : _builder.And(filter, cond);

			var find = findMethodsGroup.New(_builder.Identifier("Find"));

			foreach (var param in methodParameters)
				find.Parameter(param);

			var filterLambda = _builder.Lambda().Parameter(filterEntityParameter);
			filterLambda.Body().Append(_builder.Return(filter!));

			find
				.Public()
				.Extension()
				.Returns(entityType.WithNullability(true))
				.Body()
				.Append(
					_builder.Return(
						_builder.ExtCall(
							_builder.Type(typeof(Queryable), false),
							_builder.Identifier(nameof(Queryable.FirstOrDefault)),
							Array.Empty<IType>(),
							new ICodeExpression[]
							{
								methodParameters[0].Name,
								filterLambda.Method
							})));
		}

		private PropertyBuilder DeclareProperty(PropertyGroup propertyGroup, PropertyModel property)
		{
			var propertyBuilder = propertyGroup.New(_builder.Identifier(property.Name, null/* TODO: ctxModel.EntityColumn*/, propertyGroup.Members.Count + 1), property.Type);

			if (property.IsPublic)
				propertyBuilder.Public();

			if (property.IsDefault)
				propertyBuilder.Default(property.HasSetter);

			if (property.Summary != null)
				propertyBuilder.XmlComment().Summary(property.Summary);


			if (property.TrailingComment != null)
				propertyBuilder.TrailingComment(property.TrailingComment);

			return propertyBuilder;
		}

		private CodeFile? BuildEntityClass(
			DataModel dataModel,
			EntityModel entityModel,
			ClassGroup? classesGroup,
			PropertyGroup contextPropertiesGroup,
			Func<MethodGroup> getFindMethodsGroup,
			CodeIdentifier[]? parentNamespace,
			CodeIdentifier? parentType,
			IType? getTableType,
			ICodeExpression? getTableObject)
		{
			CodeFile? file = null;

			if (entityModel.FileName != null)
			{
				file = CreateFile(dataModel, entityModel.FileName);
				if (parentType == null)
				{
					if (entityModel.Class.Namespace != null)
					{
						var ns = _builder.Namespace(entityModel.Class.Namespace);
						file.Add(ns.Namespace);
						classesGroup = ns.Classes();
					}
					else
						file.Add(classesGroup = new ClassGroup(null));
				}
				else
				{
					if (parentNamespace != null)
					{
						var ns = _builder.Namespace(parentNamespace);
						file.Add(ns.Namespace);
						classesGroup = ns.Classes();
					}
					else
					{
						classesGroup = new ClassGroup(null);
						file.Add(classesGroup);
					}

					classesGroup = classesGroup.New(parentType).Public().Static().Partial().Classes();
				}
			}

			var entityBuilder = DeclareClass(classesGroup!, entityModel.Class);
			_entityBuilders.Add(entityModel, entityBuilder);

			var columnsGroup = entityBuilder.Properties(true);

			_metadataBuilder.BuildEntityMetadata(entityModel.Metadata, entityBuilder);

			foreach (var columnModel in entityModel.Columns)
			{
				var columnBuilder = DeclareProperty(columnsGroup!, columnModel.Property);

				_metadataBuilder.BuildColumnMetadata(columnModel.Metadata!, columnBuilder);
				_columnBuilders.Add(columnModel, columnBuilder);
			}

			if (entityModel.ContextPropertyName != null)
			{
				var contextProperty = new PropertyModel(entityModel.ContextPropertyName, _builder.Type(typeof(ITable<>), false, entityBuilder.Type.Type))
				{
					IsPublic = true,
					Summary = entityModel.Class.Summary
				};
				var contextPropertyBuilder = DeclareProperty(contextPropertiesGroup, contextProperty);

				var getTableCall = getTableType == null
					?_builder.Call(
							getTableObject!,
							_builder.Identifier(nameof(DataExtensions.GetTable)),
							new[] { entityBuilder.Type.Type },
							Array.Empty<ICodeExpression>())
					:_builder.ExtCall(
							_builder.Type(typeof(DataExtensions), false),
							_builder.Identifier(nameof(DataExtensions.GetTable)),
							new[] { entityBuilder.Type.Type },
							getTableType != null ? new ICodeExpression[] { _builder.This } : Array.Empty<ICodeExpression>());

				contextPropertyBuilder
					.AddGetter()
					// it could be DataExtensions.GetTable or DataConnection.GetTable
					// we will generate DataExtensions.GetTable call into model for safety as we don't know if base type
					// is DataConnection-based in general
					.Append(_builder.Return(
						getTableCall));
			}

			if (entityModel.HasFindExtension && entityModel.Columns.Any(c => c.Metadata!.IsPrimaryKey))
				BuildEntityFindExtension(entityModel, entityBuilder.Type.Type, getFindMethodsGroup());

			return file;
		}

		private ICodeExpression CorrectEquality(IType type, CodeBinary expr)
		{
			if (expr.Operation == BinaryOperation.Equal && _nonBooleanEqualityTypes.Contains(type))
				return _builder.Cast(_languageProvider.TypeParser.Parse<bool>(), expr);

			return expr;
		}
	}
}
