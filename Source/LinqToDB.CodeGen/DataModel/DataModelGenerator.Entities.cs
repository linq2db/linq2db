using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Model;
using LinqToDB.Data;

namespace LinqToDB.CodeGen.DataModel
{
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
		/// <param name="contextIsDataConnection">Indicates that data context derived from <see cref="DataConnection"/> class. Affects available API and generated code.</param>
		/// <param name="context">Data context instance accessor for context property generation.</param>
		/// <param name="findMethodsGroup">Action to get Find extension method group.</param>
		private void BuildEntities(
			IReadOnlyCollection<EntityModel> entities,
			Func<EntityModel, ClassBuilder>  defineEntityClass,
			PropertyGroup                    contextProperties,
			bool                             contextIsDataConnection,
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
					contextIsDataConnection,
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
		/// <param name="contextIsDataConnection">Indicates that data context derived from <see cref="DataConnection"/> class. Affects available API and generated code.</param>
		/// <param name="context">Data context instance accessor for context property generation.</param>
		/// <param name="findMethodsGroup">Action to get Find extension method group.</param>
		private void BuildEntity(
			EntityModel       entity,
			ClassBuilder      entityBuilder,
			PropertyGroup     contextProperties,
			bool              contextIsDataConnection,
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

			// add entity access property to data context
			BuildEntityContextProperty(entity, entityBuilder.Type.Type, contextProperties, contextIsDataConnection, context);

			// generate Find extension method
			BuildFindExtension(entity, entityBuilder.Type.Type, findMethodsGroup);
		}

		/// <summary>
		/// Generates entity table accessor property in data context.
		/// </summary>
		/// <param name="model">Entity data model.</param>
		/// <param name="entityType">Entity class type.</param>
		/// <param name="contextProperties">Property group in data context with table access properties.</param>
		/// <param name="contextIsDataConnection">Indicates that data context derived from <see cref="DataConnection"/> class. Affects available API and generated code.</param>
		/// <param name="contextReference">Data context instance accessor for context property generation.</param>
		private void BuildEntityContextProperty(
			EntityModel     model,
			IType           entityType,
			PropertyGroup   contextProperties,
			bool            contextIsDataConnection,
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

			// GetTable<Entity>() call calls:
			// for DataConnection: this.GetTable<Entity>()
			// for other contexts: DataExtensions.GetTable<Entity>() extension method
			var getTableCall = contextIsDataConnection
				?_code.Call(
					contextReference,
					WellKnownTypes.LinqToDB.Data.DataConnection_GetTable,
					WellKnownTypes.LinqToDB.ITable(entityType),
					new[] { entityType })
				:_code.ExtCall(
					WellKnownTypes.LinqToDB.DataExtensions,
					WellKnownTypes.LinqToDB.DataExtensions_GetTable,
					WellKnownTypes.LinqToDB.ITable(entityType),
					new[] { entityType },
					// `this` parameter
					contextReference);

			contextProperty
				.AddGetter()
					.Append(_code.Return(getTableCall));
		}

		/// <summary>
		/// Generates Find extension method for entity in extensions class.
		/// </summary>
		/// <param name="model">Entity data model.</param>
		/// <param name="entityType">Entity class type.</param>
		/// <param name="findMethodsGroup">Action to get method group for Find methods in extensions class.</param>
		private void BuildFindExtension(
			EntityModel       model,
			IType             entityType,
			Func<MethodGroup> findMethodsGroup)
		{
			// if entity doesn't have Find extension or primary key, skip extension generation
			if (!model.HasFindExtension || !model.Columns.Any(static c => c.Metadata.IsPrimaryKey))
				return;

			// example of generated code:
			// public Entity? Find(this ITable<Entity> table, int pk) => table.FirstOrDefault(e => e.PK == pk);

			// parameters:
			// table
			// N primary key parameters, where N - number of columns in primary key (usually 1)
			var methodParameters = new List<CodeParameter>(2);

			// table parameter
			methodParameters.Add(
				_code.Parameter(
					WellKnownTypes.LinqToDB.ITable(entityType),
					_code.Name(FIND_TABLE_PARAMETER),
					ParameterDirection.In));

			// for composite primary key we generate comparison based on field order (ordinal) in primary key definition
			// as it could be important for some databases to generate more optimal index lookups
			// As for parameters of methods we generate them also by ordinal order by default, but also support
			// by-name ordering for backward compatibility with T4 code, which used by-name ordering
			var pks = model.Columns.Where(static c => c.Metadata.IsPrimaryKey);

				// sort in order for parameters
				// sort for filter will be done later
			if (_dataModel.OrderFindParametersByOrdinal)
				pks = pks.OrderBy(static c => c.Metadata.PrimaryKeyOrder);
			else
				pks = pks.OrderBy(static c => c.Property.Name);

			// apply ordinal sort to primary key columns for parameters generation if by-name sort not
			if (_dataModel.OrderFindParametersByOrdinal)
				pks = pks.OrderBy(static c => c.Metadata.PrimaryKeyOrder);

			// filter parameter
			var entityParameter = _code.LambdaParameter(_code.Name(FIND_ENTITY_PARAMETER), entityType);
			// filter comparisons
			var comparisons = new List<(int ordinal, ICodeExpression comparison)>();

			foreach (var column in pks)
			{
				// generate parameter for primary key column
				var paramName = _parameterNameNormalizer(column.Property.Name);
				var parameter = _code.Parameter(column.Property.Type!, _code.Name(paramName), ParameterDirection.In);
				methodParameters.Add(parameter);

				// generate filter comparison
				// note that == operator could be overloaded for compared type and require comparison modification
				// e.g. we have this situation for Sql* types from Microsoft
				// comparison modification is done later after model generation by conversion visitor
				var condition = _code.Equal(
					_code.Member(entityParameter.Reference, _columnProperties[column]),
					parameter.Reference);

				comparisons.Add((column.Metadata.PrimaryKeyOrder ?? 0, condition));
			}

			// generate filter expression, e.g.:
			// e => e.PK1 == pk1 && e.PK2 == pk2
			ICodeExpression filter = null!;
			foreach (var (_, cond) in comparisons.OrderBy(static _ => _.ordinal))
				filter = filter == null ? cond : _code.And(filter, cond);

			// define filter lambda of type Expression<Func<TEntity, bool>>
			var filterLambda = _code
				.Lambda(
					WellKnownTypes.System.Linq.Expressions.Expression(WellKnownTypes.System.Func(WellKnownTypes.System.Boolean, entityType)),
					true)
				.Parameter(entityParameter);

			filterLambda.Body().Append(_code.Return(filter));

			// declare Find method
			var returnType = entityType.WithNullability(true);

			var find = findMethodsGroup()
				.New(_code.Name(FIND_METHOD))
					.Public()
					.Extension()
					.Returns(returnType);
;
			foreach (var param in methodParameters)
				find.Parameter(param);

			find
				.Body()
					.Append(
						_code.Return(
							_code.ExtCall(
								WellKnownTypes.System.Linq.Queryable,
								WellKnownTypes.System.Linq.Queryable_FirstOrDefault,
								returnType,
								new[] { entityType },
								// table `this` parameter
								methodParameters[0].Reference,
								// filter expression lambda
								filterLambda.Method)));
		}
	}
}
