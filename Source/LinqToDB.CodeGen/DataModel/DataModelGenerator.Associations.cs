using System;
using System.Collections.Generic;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	// Constains assiction generation code
	// Associations could generated in two equivalent forms:
	// - as association properties in entities
	// - as association extension methods
	partial class DataModelGenerator
	{
		/// <summary>
		/// Generates associations for all data model relations.
		/// </summary>
		/// <param name="getExtensionsClass">Class builder provider to get extensions class that
		/// will contain assocation extension methods.</param>
		private void BuildAssociations(Func<ClassBuilder> getExtensionsClass)
		{
			// stores assocation property group for each entity
			var          entityAssociations          = new Dictionary<EntityModel, PropertyGroup>();
			// stores association methods groupsfor each entity
			var          extensionEntityAssociations = new Dictionary<EntityModel, MethodGroup>();
			// region in extensions class with association extension methods
			RegionGroup? extensionsRegion            = null;

			foreach (var association in _dataModel.Associations)
			{
				BuildAssociation(
					association,
					getExtensionAssociations,
					entityAssociations,
					extensionEntityAssociations);
			}

			// method to provide access to associations extensions region in extensions class
			// creates new region if it is not created yet
			RegionGroup getExtensionAssociations()
			{
				return extensionsRegion ??= getExtensionsClass()
					.Regions()
						.New(EXTENSIONS_ASSOCIATIONS_REGION)
							.Regions();
			}
		}

		/// <summary>
		/// Generates code (AST) for single association in both directions using properties and/or extension methods.
		/// </summary>
		/// <param name="association">Association model.</param>
		/// <param name="extensionAssociations">Association extensions region provider.</param>
		/// <param name="entityAssociations">Assocation property groups for each entity.</param>
		/// <param name="extensionEntityAssociations">Association methods groupsfor each entity.</param>
		private void BuildAssociation(
			AssociationModel                       association,
			Func<RegionGroup>                      extensionAssociations,
			Dictionary<EntityModel, PropertyGroup> entityAssociations,
			Dictionary<EntityModel, MethodGroup>   extensionEntityAssociations)
		{
			// get source and target entities for association
			// source is foreign key source table
			// target is foreign key target table
			if (!_entityBuilders.TryGetValue(association.Source, out var sourceBuilder)
				|| !_entityBuilders.TryGetValue(association.Target, out var targetBuilder))
			{
				// data model misconfiguration (e.g. entity was removed from model, but not its associations)
				throw new InvalidOperationException($"Association {association.SourceMetadata.KeyName} connects tables, missing from current model.");
			}

			// build metadata keys for association
			// (add fields used to define assocation to metadata)
			BuildAssociationMetadataKey(association, false);
			BuildAssociationMetadataKey(association, true);

			// Type of assocation on source side.
			// Nullable for nullable foreign key.
			var sourceType = targetBuilder.Type.Type.WithNullability(association.SourceMetadata.CanBeNull);

			// Type of association on target side.
			var tagetType = sourceBuilder.Type.Type;
			if (association.ManyToOne)
			{
				// for many-to-one assocations has collection type, defined by user preferences
				if (_dataModel.AssociationCollectionAsArray)
					tagetType = _code.ArrayType(tagetType, false);
				else if (_dataModel.AssociationCollectionType != null)
					tagetType = _dataModel.AssociationCollectionType.WithTypeArguments(tagetType);
				else
					// default type is IQueryable to emphasize that associations are
					// query-time objects by default if not used with eager load
					tagetType = WellKnownTypes.System.Linq.IQueryable(tagetType);
			}
			else // if one-to-one association targets nullable columns, association is nullable
				tagetType = tagetType.WithNullability(true);

			// (if enabled) generate property-based association from source to target
			if (association.Property != null)
			{
				BuildAssociationProperty(
					entityAssociations,
					association.Source,
					sourceType,
					association.Property,
					association.SourceMetadata);
			}

			// (if enabled) generate property-based back-reference association from target to source
			if (association.BackreferenceProperty != null)
			{
				BuildAssociationProperty(
					entityAssociations,
					association.Target,
					tagetType,
					association.BackreferenceProperty,
					association.TargetMetadata);
			}

			// (if enabled) generate extension-based association from source to target
			if (association.Extension != null)
			{
				BuildAssociationExtension(
					extensionEntityAssociations,
					extensionAssociations,
					sourceBuilder,
					targetBuilder,
					sourceType,
					association.Extension,
					association.SourceMetadata,
					association,
					false);
			}

			// (if enabled) generate extension-based back-reference association from target to source
			if (association.BackreferenceExtension != null)
			{
				BuildAssociationExtension(
					extensionEntityAssociations,
					extensionAssociations,
					targetBuilder,
					sourceBuilder,
					tagetType,
					association.BackreferenceExtension,
					association.TargetMetadata,
					association,
					true);
			}
		}

		/// <summary>
		/// Generates assocation keys in metadata.
		/// </summary>
		/// <param name="association">Association model.</param>
		/// <param name="backReference">Association side.</param>
		private void BuildAssociationMetadataKey(AssociationModel association, bool backReference)
		{
			// metadata model to update with keys
			var metadata = backReference ? association.TargetMetadata : association.SourceMetadata;

			// if metadata keys already specified in model, skip generation
			// (we don't generate them before this stage, so it means user generated them manually)
			if (   metadata.ExpressionPredicate   != null
				|| metadata.QueryExpressionMethod != null
				|| metadata.ThisKey               != null
				|| metadata.ThisKeyExpression     != null
				|| metadata.OtherKey              != null
				|| metadata.OtherKeyExpression    != null)
				return;

			if (association.FromColumns == null
				|| association.ToColumns == null
				|| association.FromColumns.Length != association.ToColumns.Length
				|| association.FromColumns.Length == 0)
				throw new InvalidOperationException($"Invalid association configuration: association columns missing or mismatch on both sides of assocation.");

			var thisColumns  =  backReference ? association.ToColumns : association.FromColumns;
			var otherColumns = !backReference ? association.ToColumns : association.FromColumns;

			var thisBuilder = !backReference ? _entityBuilders[association.Source] : _entityBuilders[association.Target];
			var otherBuilder = backReference ? _entityBuilders[association.Source] : _entityBuilders[association.Target];

			// we generate keys using nameof operator to get property name to have refactoring-friendly mappings
			// (T4 always used strings here)
			var separator = association.FromColumns.Length > 1 ? _code.Constant(",", true) : null;
			for (var i = 0; i < association.FromColumns.Length; i++)
			{
				if (i > 0)
				{
					// add comma separator
					metadata.ThisKeyExpression  = _code.Add(metadata.ThisKeyExpression! , separator!);
					metadata.OtherKeyExpression = _code.Add(metadata.OtherKeyExpression!, separator!);
				}

				// generate nameof() expressions for current key column
				var thisKey  = _code.NameOf(_code.Member(thisBuilder .Type.Type, _columnProperties[thisColumns [0]]));
				var otherKey = _code.NameOf(_code.Member(otherBuilder.Type.Type, _columnProperties[otherColumns[0]]));

				// append column name to key
				metadata.ThisKeyExpression  = metadata.ThisKeyExpression  == null ? thisKey  : _code.Add(metadata.ThisKeyExpression , thisKey);
				metadata.OtherKeyExpression = metadata.OtherKeyExpression == null ? otherKey : _code.Add(metadata.OtherKeyExpression, thisKey);
			}
		}

		/// <summary>
		/// Generates association property in entity class for one side of association.
		/// </summary>
		/// <param name="entityAssociations">Lookup for association properties group in each entity.</param>
		/// <param name="owner">Assocation property owner entity.</param>
		/// <param name="type">Association property type.</param>
		/// <param name="propertyModel">Association property model.</param>
		/// <param name="metadata">Association property metadata.</param>
		private void BuildAssociationProperty(
			Dictionary<EntityModel, PropertyGroup> entityAssociations,
			EntityModel                            owner,
			IType                                  type,
			PropertyModel                          propertyModel,
			AssociationMetadata                    metadata)
		{
			// if entity class doesn't have assocation properties group yet
			// (not created yet by previous associations) - create it
			if (!entityAssociations.TryGetValue(owner, out var associations))
				entityAssociations.Add(
					owner,
					associations = _entityBuilders[owner]
						.Regions()
							.New(ENTITY_ASSOCIATIONS_REGION)
								.Properties(false));

			// by default property type will be null here, but user could override it manually
			// and we should respect it
			if (propertyModel.Type == null)
				propertyModel.Type = type;

			// declare property
			var propertyBuilder = DefineProperty(associations, propertyModel);
			// and it's metadata
			_metadataBuilder.BuildAssociationMetadata(metadata, propertyBuilder);
		}

		/// <summary>
		/// Generates association extension method in extensions class for one side of association.
		/// </summary>
		/// <param name="extensionEntityAssociations">Association methods groupsfor each entity.</param>
		/// <param name="extensionAssociations">Association extensions region provider.</param>
		/// <param name="thisEntity">Entity class for this side of assocation (used for extension <c>this</c> parameter).</param>
		/// <param name="resultEntity">Entity class for other side of assocation (used for extension result type).</param>
		/// <param name="type">Association result type.</param>
		/// <param name="extensionModel">Extension method model.</param>
		/// <param name="metadata">Association methodo metadata.</param>
		/// <param name="associationModel">Association model.</param>
		/// <param name="backReference">Identifies current side of assocation.</param>
		private void BuildAssociationExtension(
			Dictionary<EntityModel, MethodGroup> extensionEntityAssociations,
			Func<RegionGroup>                    extensionAssociations,
			ClassBuilder                         thisEntity,
			ClassBuilder                         resultEntity,
			IType                                type,
			MethodModel                          extensionModel,
			AssociationMetadata                  metadata,
			AssociationModel                     associationModel,
			bool                                 backReference)
		{
			// create (if missing) assocations region for specific owner (this) entity
			var key = backReference ? associationModel.Target : associationModel.Source;

			if (!extensionEntityAssociations.TryGetValue(key, out var associations))
				extensionEntityAssociations.Add(
					key,
					associations = extensionAssociations()
						.New(string.Format(EXTENSIONS_ENTITY_ASSOCIATIONS_REGION, thisEntity.Type.Name.Name))
							.Methods(false));

			// define extension method
			var  methodBuilder = DefineMethod(associations, extensionModel).Returns(type);
			// and it's metadata
			_metadataBuilder.BuildAssociationMetadata(metadata, methodBuilder);

			// build method parameters...
			var thisParam = _code.Parameter(thisEntity.Type.Type, _code.Name(EXTENSIONS_ENTITY_THIS_PARAMETER), ParameterDirection.In);
			var ctxParam  = _code.Parameter(WellKnownTypes.LinqToDB.IDataContext, _code.Name(EXTENSIONS_ENTITY_CONTEXT_PARAMETER), ParameterDirection.In);

			methodBuilder.Parameter(thisParam);
			methodBuilder.Parameter(ctxParam);

			// ... and body
			if (associationModel.FromColumns == null || associationModel.ToColumns == null)
				// association doesn't specify relation columns (e.g. defined usign expression method)
				// so we should generate exception for non-query execution
				methodBuilder
					.Body()
						.Append(
							_code.Throw(
								_code.New(
									WellKnownTypes.System.InvalidOperationException,
									_code.Constant(EXCEPTION_QUERY_ONLY_ASSOCATION_CALL, true))));
			else
			{
				// generate association query for non-query invocation

				// As method body here could conflict with custom return type for many-to-one assocation
				// we forcebly override return type here to IQueryable<T>
				if (associationModel.ManyToOne && backReference)
					methodBuilder.Returns(WellKnownTypes.System.Linq.IQueryable(resultEntity.Type.Type));

				var lambdaParam = _code.LambdaParameter(_code.Name(EXTENSIONS_ASSOCIATION_FILTER_PARAMETER), resultEntity.Type.Type);

				// generate assocation key columns filter, which compare
				// `this` entity parameter columns with return table entity columns
				var fromObject =  backReference ? lambdaParam : thisParam;
				var toObject   = !backReference ? lambdaParam : thisParam;

				ICodeExpression? filter = null;

				for (var i = 0; i < associationModel.FromColumns.Length; i++)
				{
					var fromColumn = _columnProperties[associationModel.FromColumns[i]];
					var toColumn   = _columnProperties[associationModel.ToColumns[i]];

					var cond = _code.Equal(
						_code.Member(fromObject.Reference, fromColumn),
						_code.Member(toObject.Reference  , toColumn  ));

					filter = filter == null ? cond : _code.And(filter, cond);
				}

				// generate filter lambda function
				var filterLambda = _code
					.Lambda(
						WellKnownTypes.System.Linq.Expressions.Expression(
							WellKnownTypes.System.Func(WellKnownTypes.System.Boolean, resultEntity.Type.Type)),
						true)
					.Parameter(lambdaParam);
				filterLambda.Body().Append(_code.Return(filter!));

				// ctx.GetTable<ResultEntity>()
				var body = _code.ExtCall(
						WellKnownTypes.LinqToDB.DataExtensions,
						WellKnownTypes.LinqToDB.DataExtensions_GetTable,
						WellKnownTypes.LinqToDB.ITable(resultEntity.Type.Type),
						new[] { resultEntity.Type.Type },
						ctxParam.Reference);

				// append First/FirstOrDefault (for optional association)
				// for non-many relation
				if (!backReference || !associationModel.ManyToOne)
				{
					var optional = backReference ? associationModel.SourceMetadata.CanBeNull : associationModel.TargetMetadata.CanBeNull;

					// .First(t => t.PK == thisEntity.PK)
					// or
					// .FirstOrDefault(t => t.PK == thisEntity.PK)
					body = _code.ExtCall(
						WellKnownTypes.System.Linq.Queryable,
						optional ? WellKnownTypes.System.Linq.Queryable_FirstOrDefault : WellKnownTypes.System.Linq.Queryable_First,
						resultEntity.Type.Type.WithNullability(optional),
						new[] { resultEntity.Type.Type },
						body,
						filterLambda.Method);
				}
				else
				{
					// .Where(t => t.PK == thisEntity.PK)
					body = _code.ExtCall(
						WellKnownTypes.System.Linq.Queryable,
						WellKnownTypes.System.Linq.Queryable_Where,
						WellKnownTypes.System.Linq.IQueryable(resultEntity.Type.Type),
						new [] { resultEntity.Type.Type },
						body,
						filterLambda.Method);
				}

				methodBuilder.Body().Append(_code.Return(body));
			}
		}
	}
}
