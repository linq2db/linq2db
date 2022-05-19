using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeModel;
using LinqToDB.Data;

namespace LinqToDB.DataModel;

// contains entity related generation logic:
// - entity classes generation
// - optional Find extension generation
partial class DataModelGenerator
{
	/// <summary>
	/// Generates mapping classes for table/view models (entities).
	/// </summary>
	/// <param name="entities">Collection of entity models.</param>
	/// <param name="defineEntityClass">Action to define new empty entity class.</param>
	/// <param name="contextProperties">Property group in data context with table access properties.</param>
	/// <param name="contextType">Data context class type.</param>
	/// <param name="context">Data context instance accessor for context property generation.</param>
	/// <param name="findMethodsGroup">Action to get Find extension method group.</param>
	private void BuildEntities(
		IReadOnlyCollection<EntityModel> entities,
		Func<EntityModel, ClassBuilder>  defineEntityClass,
		PropertyGroup                    contextProperties,
		IType                            contextType,
		ICodeExpression                  context,
		Func<MethodGroup>                findMethodsGroup)
	{
		foreach (var entity in entities)
		{
			var entityBuilder = defineEntityClass(entity);

			// register entity class builder in lookup so later we can access it
			// during generation of associations and procedures/functions
			_entityBuilders.Add(entity, entityBuilder);

			BuildEntity(
				entity,
				entityBuilder,
				contextProperties,
				contextType,
				context,
				findMethodsGroup);
		}
	}

	/// <summary>
	/// Generates mapping class for specific <paramref name="entity"/>. Also generates Find extension method if its
	/// generation enabled for entity.
	/// </summary>
	/// <param name="entity">Entity data model.</param>
	/// <param name="entityBuilder">Entity class builder.</param>
	/// <param name="contextProperties">Property group in data context with table access properties.</param>
	/// <param name="contextType">Data context class type.</param>
	/// <param name="context">Data context instance accessor for context property generation.</param>
	/// <param name="findMethodsGroup">Action to get Find extension method group.</param>
	private void BuildEntity(
		EntityModel       entity,
		ClassBuilder      entityBuilder,
		PropertyGroup     contextProperties,
		IType             contextType,
		ICodeExpression   context,
		Func<MethodGroup> findMethodsGroup)
	{
		// generate table metadata for entity
		_metadataBuilder.BuildEntityMetadata(entity.Metadata, entityBuilder);

		// generate colum properties
		var columnsGroup = entityBuilder.Properties(true);
		foreach (var columnModel in entity.Columns)
		{
			var columnBuilder = DefineProperty(columnsGroup, columnModel.Property);

			// register property in lookup for later use by associations generator
			_columnProperties.Add(columnModel, columnBuilder.Property);

			// generate column metadata
			_metadataBuilder.BuildColumnMetadata(columnModel.Metadata, columnBuilder);
		}

		// generate IEquatable interface implementation
		BuildEntityIEquatable(entity, entityBuilder);

		// add entity access property to data context
		BuildEntityContextProperty(entity, entityBuilder.Type.Type, contextProperties, context);

		// generate Find extension method
		BuildFindExtensions(entity, contextType, entityBuilder.Type.Type, findMethodsGroup);
	}

	private void BuildEntityIEquatable(EntityModel entity, ClassBuilder entityBuilder)
	{
		if (!entity.ImplementsIEquatable)
			return;

		var pk = entity.Columns.Where(static c => c.Metadata.IsPrimaryKey).ToArray();
		if (pk.Length == 0)
			return;

		entityBuilder.Implements(WellKnownTypes.System.IEquatable(entityBuilder.Type.Type));

		var keySelectors    = new ICodeExpression[pk.Length];
		var lambdaType      = WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(WellKnownTypes.System.ObjectNullable, entityBuilder.Type.Type));
		var entityParameter = AST.LambdaParameter(AST.Name(ENTITY_IEQUATABLE_COMPARER_LAMBDA_PARAMETER), entityBuilder.Type.Type);

		for (var i = 0; i < pk.Length; i++)
		{
			var column      = pk[i];
			var lambda      = AST.Lambda(lambdaType, true).Parameter(entityParameter);

			lambda.Body().Append(AST.Return(AST.Member(entityParameter.Reference, _columnProperties[column].Reference)));

			keySelectors[i] = lambda.Method;
		}

		// generate static field with comparer instance
		var region = entityBuilder.Regions().New(ENTITY_IEQUATABLE_REGION);

		var fieldType = WellKnownTypes.System.Collections.Generic.IEqualityComparer(entityBuilder.Type.Type);

		var comparerField = region
			.Fields(false)
				.New(AST.Name(ENTITY_IEQUATABLE_COMPARER_FIELD), fieldType)
					.Private()
					.Static()
					.ReadOnly()
					.AddInitializer(AST.Call(
						new CodeTypeReference(WellKnownTypes.LinqToDB.Tools.Comparers.ComparerBuilder),
						WellKnownTypes.LinqToDB.Tools.Comparers.ComparerBuilder_GetEqualityComparer,
						fieldType,
						new[] { entityBuilder.Type.Type },
						false,
						keySelectors));

		var methods = region.Methods(false);

		// generate IEquatable.Equals
		var parameter = AST.Parameter(entityBuilder.Type.Type.WithNullability(true), WellKnownTypes.System.IEquatable_Equals_Parameter, CodeParameterDirection.In);
		methods.New(WellKnownTypes.System.IEquatable_Equals)
			.SetModifiers(Modifiers.Public)
			.Parameter(parameter)
			.Returns(WellKnownTypes.System.Boolean)
			.Body().Append(AST.Return(
				AST.Call(
					comparerField.Field.Reference,
					WellKnownTypes.System.Collections.Generic.IEqualityComparer_Equals,
					WellKnownTypes.System.Boolean,
					entityBuilder.Type.This,
					AST.SuppressNull(parameter.Reference))
				));

		// override object.GetHashCode
		methods.New(WellKnownTypes.System.Object_GetHashCode)
			.SetModifiers(Modifiers.Public | Modifiers.Override)
			.Returns(WellKnownTypes.System.Int32)
			.Body().Append(AST.Return(
				AST.Call(
					comparerField.Field.Reference,
					WellKnownTypes.System.Collections.Generic.IEqualityComparer_GetHashCode,
					WellKnownTypes.System.Int32,
					entityBuilder.Type.This)
				));

		// override object.Equals
		parameter = AST.Parameter(WellKnownTypes.System.ObjectNullable, WellKnownTypes.System.Object_Equals_Parameter, CodeParameterDirection.In);
		methods.New(WellKnownTypes.System.Object_Equals)
			.SetModifiers(Modifiers.Public | Modifiers.Override)
			.Parameter(parameter)
			.Returns(WellKnownTypes.System.Boolean)
			.Body()
				.Append(
					AST.Return(
						AST.Call(
							entityBuilder.Type.This,
							WellKnownTypes.System.IEquatable_Equals,
							WellKnownTypes.System.Boolean,
							AST.As(entityBuilder.Type.Type, parameter.Reference))));
	}

	/// <summary>
	/// Generates entity table accessor property in data context.
	/// </summary>
	/// <param name="model">Entity data model.</param>
	/// <param name="entityType">Entity class type.</param>
	/// <param name="contextProperties">Property group in data context with table access properties.</param>
	/// <param name="contextReference">Data context instance accessor for context property generation.</param>
	private void BuildEntityContextProperty(
		EntityModel     model,
		IType           entityType,
		PropertyGroup   contextProperties,
		ICodeExpression contextReference)
	{
		// context property disabled for entity? skip generation
		if (model.ContextProperty == null)
			return;

		// example of generated code:
		// public ITable<Entity> Entities => GetTable<Entity>();

		if (model.ContextProperty.Type!.Kind == TypeKind.OpenGeneric)
			model.ContextProperty.Type = model.ContextProperty.Type.WithTypeArguments(entityType);
		else
			throw new InvalidOperationException($"Entity {model.Class.Name} context property type is not valid");

		var contextProperty = DefineProperty(contextProperties, model.ContextProperty);

		// this.GetTable<Entity>() call
		var getTableCall = AST.ExtCall(
				WellKnownTypes.LinqToDB.DataExtensions,
				WellKnownTypes.LinqToDB.DataExtensions_GetTable,
				WellKnownTypes.LinqToDB.ITable(entityType),
				new[] { entityType },
				false,
				// `this` parameter
				contextReference);

		contextProperty
			.AddGetter()
				.Append(AST.Return(getTableCall));
	}

	/// <summary>
	/// Generates Find extension method for entity in extensions class.
	/// </summary>
	/// <param name="model">Entity data model.</param>
	/// <param name="contextType">Generated data context class type.</param>
	/// <param name="entityType">Entity class type.</param>
	/// <param name="findMethodsGroup">Action to get method group for Find methods in extensions class.</param>
	private void BuildFindExtensions(
		EntityModel       model,
		IType             contextType,
		IType             entityType,
		Func<MethodGroup> findMethodsGroup)
	{
		// if entity doesn't have primary key, skip extension generation
		if (!model.Columns.Any(static c => c.Metadata.IsPrimaryKey))
			return;

		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindByPkOnTable           );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindAsyncByPkOnTable      );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindQueryByPkOnTable      );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindByRecordOnTable       );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindAsyncByRecordOnTable  );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindQueryByRecordOnTable  );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindByPkOnContext         );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindAsyncByPkOnContext    );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindQueryByPkOnContext    );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindByRecordOnContext     );
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindAsyncByRecordOnContext);
		BuildFindExtension(model, contextType, entityType, findMethodsGroup, FindTypes.FindQueryByRecordOnContext);
	}

	/// <summary>
	/// Generates Find extension method for entity in extensions class.
	/// </summary>
	/// <param name="model">Entity data model.</param>
	/// <param name="contextType">Generated data context class type.</param>
	/// <param name="entityType">Entity class type.</param>
	/// <param name="findMethodsGroup">Action to get method group for Find methods in extensions class.</param>
	/// <param name="methodType">Type of find method to generate.</param>
	private void BuildFindExtension(
		EntityModel       model,
		IType             contextType,
		IType             entityType,
		Func<MethodGroup> findMethodsGroup,
		FindTypes         methodType)
	{
		if ((model.FindExtensions & methodType) != methodType)
			return;

		var async     = (methodType & FindTypes.Async       ) == FindTypes.Async;
		var query     = (methodType & FindTypes.Query       ) == FindTypes.Query;
		var byPK      = (methodType & FindTypes.ByPrimaryKey) == FindTypes.ByPrimaryKey;
		var byEntity  = (methodType & FindTypes.ByEntity    ) == FindTypes.ByEntity;
		var onTable   = (methodType & FindTypes.OnTable     ) == FindTypes.OnTable;
		var onContext = (methodType & FindTypes.OnContext   ) == FindTypes.OnContext;

		// examples of generated code:
		// public Entity? Find(this ITable<Entity> table, int pk) => table.FirstOrDefault(e => e.PK == pk);
		// public Task<Entity?> FindAsync(this ITable<Entity> table, int pk, CancellationToken cancellationToken = default) => table.FirstOrDefaultAsync(e => e.PK == pk, cancellationToken);
		// public IQueryable<Entity> FindQuery(this ITable<Entity> table, int pk) => table.Where(e => e.PK == pk);

		// public Entity? Find(this MyContext db, int pk) => db.GetTable<Entity>().FirstOrDefault(e => e.PK == pk);
		// public Task<Entity?> FindAsync(this MyContext db, int pk, CancellationToken cancellationToken = default) => db.GetTable<Entity>().FirstOrDefaultAsync(e => e.PK == pk, cancellationToken);
		// public IQueryable<Entity> FindQuery(this MyContext db, int pk) => db.GetTable<Entity>().Where(e => e.PK == pk);

		// public Entity? Find(this ITable<Entity> table, Entity record) => table.FirstOrDefault(e => e.PK == record.PK);
		// public Task<Entity?> FindAsync(this ITable<Entity> table, Entity record, CancellationToken cancellationToken = default) => table.FirstOrDefaultAsync(e => e.PK == record.PK, cancellationToken);
		// public IQueryable<Entity> FindQuery(this ITable<Entity> table, Entity record) => table.Where(e => e.PK == record.PK);

		// public Entity? Find(this MyContext db, Entity record) => db.GetTable<Entity>().FirstOrDefault(e => e.PK == record.PK);
		// public Task<Entity?> FindAsync(this MyContext db, Entity record, CancellationToken cancellationToken = default) => db.GetTable<Entity>().FirstOrDefaultAsync(e => e.PK == record.PK, cancellationToken);
		// public IQueryable<Entity> FindQuery(this MyContext db, Entity record) => db.GetTable<Entity>().Where(e => e.PK == record.PK);

		// parameters:
		// table or context
		// N primary key parameters, where N - number of columns in primary key (usually 1) or entity object
		// cancellation token for async method
		var methodParameters = new List<CodeParameter>(async ? 3 : 2);

		// extended parameter (table or context)
		methodParameters.Add(
			AST.Parameter(
				onTable ? WellKnownTypes.LinqToDB.ITable(entityType) : contextType,
				AST.Name(onTable ? FIND_TABLE_PARAMETER : FIND_CONTEXT_PARAMETER),
				CodeParameterDirection.In));

		// for composite primary key we generate comparison based on field order (ordinal) in primary key definition
		// as it could be important for some databases to generate more optimal index lookups
		// As for parameters of methods we generate them also by ordinal order by default, but also support
		// by-name ordering for backward compatibility with T4 code, which used by-name ordering
		var pks = model.Columns.Where(static c => c.Metadata.IsPrimaryKey);

		// sort in order for parameters
		// sort for filter will be done later
		if (_options.DataModel.OrderFindParametersByColumnOrdinal)
			pks = pks.OrderBy(static c => c.Metadata.PrimaryKeyOrder);
		else
			pks = pks.OrderBy(static c => c.Property.Name);

		// apply ordinal sort to primary key columns for parameters generation if by-name sort not
		if (_options.DataModel.OrderFindParametersByColumnOrdinal)
			pks = pks.OrderBy(static c => c.Metadata.PrimaryKeyOrder);

		// filter parameter
		var entityParameter = AST.LambdaParameter(AST.Name(FIND_ENTITY_FILTER_PARAMETER), entityType);
		// filter comparisons
		var comparisons = new List<(int ordinal, ICodeExpression comparison)>();

		ICodeExpression? recordReference = null;
		if (byEntity)
		{
			var param       = AST.Parameter(entityType, AST.Name(FIND_ENTITY_PARAMETER), CodeParameterDirection.In);
			recordReference = param.Reference;
			methodParameters.Add(param);
		}

		foreach (var column in pks)
		{
			ICodeExpression fieldFromParameters;
			if (byPK)
			{
				// generate parameter for primary key column
				var paramName       = _findMethodParameterNameNormalizer(column.Property.Name);
				var parameter       = AST.Parameter(column.Property.Type!, AST.Name(paramName), CodeParameterDirection.In);
				fieldFromParameters = parameter.Reference;
				methodParameters.Add(parameter);
			}
			else
				fieldFromParameters = AST.Member(recordReference!, _columnProperties[column].Reference);

			// generate filter comparison
			// note that == operator could be overloaded for compared type and require comparison modification
			// e.g. we have this situation for Sql* types from Microsoft
			// comparison modification is done later after model generation by conversion visitor
			var condition = AST.Equal(
				AST.Member(entityParameter.Reference, _columnProperties[column].Reference),
				fieldFromParameters);

			comparisons.Add((column.Metadata.PrimaryKeyOrder ?? 0, condition));
		}

		ICodeExpression? cancellationTokenParameter = null;
		if (async)
		{
			var param                  = AST.Parameter(
				WellKnownTypes.System.Threading.CancellationToken,
				AST.Name(CANCELLATION_TOKEN_PARAMETER),
				CodeParameterDirection.In,
				AST.Default(WellKnownTypes.System.Threading.CancellationToken, true));
			cancellationTokenParameter = param.Reference;
			methodParameters.Add(param);
		}

		// generate filter expression, e.g.:
		// e => e.PK1 == pk1 && e.PK2 == pk2
		ICodeExpression filter = null!;
		foreach (var (_, cond) in comparisons.OrderBy(static _ => _.ordinal))
			filter = filter == null ? cond : AST.And(filter, cond);

		// define filter lambda of type Expression<Func<TEntity, bool>>
		var filterLambda = AST
			.Lambda(
				WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(WellKnownTypes.System.Boolean, entityType)),
				true)
			.Parameter(entityParameter);

		filterLambda.Body().Append(AST.Return(filter));

		// declare Find method
		IType returnType;
		if (query)
			returnType = WellKnownTypes.System.Linq.IQueryable(entityType);
		else
		{
			returnType = entityType.WithNullability(true);
			if (async)
				returnType = WellKnownTypes.System.Threading.Tasks.Task(returnType);
		}

		var methodName = onContext && byPK ? FIND_METHOD + entityType.Name!.Name : FIND_METHOD;
		if (query)
			methodName += FIND_QUERY_SUFFIX;
		if (async)
			methodName += ASYNC_SUFFIX;

		var find       = findMethodsGroup()
			.New(AST.Name(methodName))
				.SetModifiers(Modifiers.Public | Modifiers.Static | Modifiers.Extension)
				.Returns(returnType);
		;
		foreach (var param in methodParameters)
			find.Parameter(param);

		var table = onContext
				? AST.ExtCall(
					WellKnownTypes.LinqToDB.DataExtensions,
					WellKnownTypes.LinqToDB.DataExtensions_GetTable,
					WellKnownTypes.LinqToDB.ITable(entityType),
					new[] { entityType },
					false,
					// `this` parameter
					methodParameters[0].Reference)
				: (ICodeExpression)methodParameters[0].Reference;

		if (query)
		{
			find
				.Body()
					.Append(
						AST.Return(
							AST.ExtCall(
								WellKnownTypes.System.Linq.Queryable,
								WellKnownTypes.System.Linq.Queryable_Where,
								returnType,
								new[] { entityType },
								true,
								table,
								filterLambda.Method)));
		}
		else
		{
			find
				.Body()
					.Append(
						AST.Return(
							AST.ExtCall(
								async ? WellKnownTypes.LinqToDB.AsyncExtensions                     : WellKnownTypes.System.Linq.Queryable,
								async ? WellKnownTypes.LinqToDB.AsyncExtensions_FirstOrDefaultAsync : WellKnownTypes.System.Linq.Queryable_FirstOrDefault,
								returnType,
								new[] { entityType },
								true,
								async
									? new ICodeExpression[] { table, filterLambda.Method, cancellationTokenParameter! }
									: new ICodeExpression[] { table, filterLambda.Method })));
		}
	}
}
