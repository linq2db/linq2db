using System;

using LinqToDB.CodeModel;
using LinqToDB.Metadata;

namespace LinqToDB.DataModel
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
		/// <param name="context">Model generation context.</param>
		private static void BuildAssociations(IDataModelGenerationContext context)
		{
			foreach (var association in context.Model.DataContext.Associations)
				BuildAssociation(context, association);
		}

		/// <summary>
		/// Generates code (context.AST) for single association in both directions using properties and/or extension methods.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="association">Association model.</param>
		private static void BuildAssociation(IDataModelGenerationContext context, AssociationModel association)
		{
			// build metadata keys for association
			// (add fields used to define assocation to metadata)
			BuildAssociationMetadataKey(context, association, false);
			BuildAssociationMetadataKey(context, association, true);

			// Type of assocation on source side.
			// Nullable for nullable foreign key.
			var sourceType = context.GetEntityBuilder(association.Target).Type.Type.WithNullability(association.SourceMetadata.CanBeNull);

			// Type of association on target side.
			var tagetType = context.GetEntityBuilder(association.Source).Type.Type;
			if (association.ManyToOne)
			{
				// for many-to-one assocations has collection type, defined by user preferences
				if (context.Options.AssociationCollectionAsArray)
					tagetType = context.AST.ArrayType(tagetType, false);
				else if (context.Model.AssociationCollectionType != null)
					tagetType = context.Model.AssociationCollectionType.WithTypeArguments(tagetType);
				else
					// default type is IEnumerable
					tagetType = WellKnownTypes.System.Collections.Generic.IEnumerable(tagetType);
			}
			else // if one-to-one association targets nullable columns, association is nullable
				tagetType = tagetType.WithNullability(true);

			// (if enabled) generate property-based association from source to target
			if (association.Property != null)
			{
				BuildAssociationProperty(
					context,
					association.Source,
					sourceType,
					association.Property,
					association.SourceMetadata);
			}

			// (if enabled) generate property-based back-reference association from target to source
			if (association.BackreferenceProperty != null)
			{
				BuildAssociationProperty(
					context,
					association.Target,
					tagetType,
					association.BackreferenceProperty,
					association.TargetMetadata);
			}

			// (if enabled) generate extension-based association from source to target
			if (association.Extension != null)
			{
				BuildAssociationExtension(
					context,
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
					context,
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
		/// <param name="context">Model generation context.</param>
		/// <param name="association">Association model.</param>
		/// <param name="backReference">Association side.</param>
		private static void BuildAssociationMetadataKey(IDataModelGenerationContext context, AssociationModel association, bool backReference)
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
				throw new InvalidOperationException("Invalid association configuration: association columns missing or mismatch on both sides of assocation.");

			var thisColumns  =  backReference ? association.ToColumns : association.FromColumns;
			var otherColumns = !backReference ? association.ToColumns : association.FromColumns;

			var thisBuilder  = context.GetEntityBuilder(!backReference ? association.Source : association.Target);
			var otherBuilder = context.GetEntityBuilder( backReference ? association.Source : association.Target);

			// we generate keys using nameof operator to get property name to have refactoring-friendly mappings
			// (T4 always used strings here)
			var separator = association.FromColumns.Length > 1 ? context.AST.Constant(",", true) : null;
			for (var i = 0; i < association.FromColumns.Length; i++)
			{
				if (i > 0)
				{
					// add comma separator
					metadata.ThisKeyExpression  = context.AST.Add(metadata.ThisKeyExpression! , separator!);
					metadata.OtherKeyExpression = context.AST.Add(metadata.OtherKeyExpression!, separator!);
				}

				// generate nameof() expressions for current key column
				var thisKey  = context.AST.NameOf(context.AST.Member(thisBuilder .Type.Type, context.GetColumnProperty(thisColumns [i]).Reference));
				var otherKey = context.AST.NameOf(context.AST.Member(otherBuilder.Type.Type, context.GetColumnProperty(otherColumns[i]).Reference));

				// append column name to key
				metadata.ThisKeyExpression  = metadata.ThisKeyExpression  == null ? thisKey  : context.AST.Add(metadata.ThisKeyExpression , thisKey);
				metadata.OtherKeyExpression = metadata.OtherKeyExpression == null ? otherKey : context.AST.Add(metadata.OtherKeyExpression, otherKey);
			}
		}

		/// <summary>
		/// Generates association property in entity class for one side of association.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="owner">Assocation property owner entity.</param>
		/// <param name="type">Association property type.</param>
		/// <param name="propertyModel">Association property model.</param>
		/// <param name="metadata">Association property metadata.</param>
		private static void BuildAssociationProperty(
			IDataModelGenerationContext context,
			EntityModel                 owner,
			IType                       type,
			PropertyModel               propertyModel,
			AssociationMetadata         metadata)
		{
			// by default property type will be null here, but user could override it manually
			// and we should respect it
			propertyModel.Type ??= type;

			// declare property
			var propertyBuilder = context.DefineProperty(context.GetEntityAssociationsGroup(owner), propertyModel);

			// and it's metadata
			context.MetadataBuilder?.BuildAssociationMetadata(context, context.GetEntityBuilder(owner).Type, metadata, propertyBuilder);
		}

		/// <summary>
		/// Generates association extension method in extensions class for one side of association.
		/// </summary>
		/// <param name="context">Model generation context.</param>
		/// <param name="type">Association result type.</param>
		/// <param name="extensionModel">Extension method model.</param>
		/// <param name="metadata">Association methodo metadata.</param>
		/// <param name="associationModel">Association model.</param>
		/// <param name="backReference">Identifies current side of assocation.</param>
		private static void BuildAssociationExtension(
			IDataModelGenerationContext context,
			IType                       type,
			MethodModel                 extensionModel,
			AssociationMetadata         metadata,
			AssociationModel            associationModel,
			bool                        backReference)
		{
			var thisEntityType   = context.GetEntityBuilder( backReference ? associationModel.Target : associationModel.Source).Type.Type;
			var resultEntityType = context.GetEntityBuilder(!backReference ? associationModel.Target : associationModel.Source).Type.Type;

			// create (if missing) assocations region for specific owner (this) entity
			var associations = context.GetEntityAssociationExtensionsGroup(backReference ? associationModel.Target : associationModel.Source);

			// define extension method
			var  methodBuilder = context.DefineMethod(associations, extensionModel).Returns(type);

			// build method parameters...
			var thisParam = context.AST.Parameter(thisEntityType, context.AST.Name(DataModelConstants.EXTENSIONS_ENTITY_THIS_PARAMETER), CodeParameterDirection.In);
			var ctxParam  = context.AST.Parameter(WellKnownTypes.LinqToDB.IDataContext, context.AST.Name(DataModelConstants.EXTENSIONS_ENTITY_CONTEXT_PARAMETER), CodeParameterDirection.In);

			methodBuilder.Parameter(thisParam);
			methodBuilder.Parameter(ctxParam);

			// ... and body
			if (associationModel.FromColumns == null || associationModel.ToColumns == null)
				// association doesn't specify relation columns (e.g. defined usign expression method)
				// so we should generate exception for non-query execution
				methodBuilder
					.Body()
						.Append(
							context.AST.Throw(
								context.AST.New(
									WellKnownTypes.System.InvalidOperationException,
									context.AST.Constant(DataModelConstants.EXCEPTION_QUERY_ONLY_ASSOCATION_CALL, true))));
			else
			{
				// generate association query for non-query invocation

				// As method body here could conflict with custom return type for many-to-one assocation
				// we forcebly override return type here to IQueryable<T>
				if (associationModel.ManyToOne && backReference)
					methodBuilder.Returns(WellKnownTypes.System.Linq.IQueryable(resultEntityType));

				var lambdaParam = context.AST.LambdaParameter(context.AST.Name(DataModelConstants.EXTENSIONS_ASSOCIATION_FILTER_PARAMETER), resultEntityType);

				// generate assocation key columns filter, which compare
				// `this` entity parameter columns with return table entity columns
				var fromObject =  backReference ? lambdaParam : thisParam;
				var toObject   = !backReference ? lambdaParam : thisParam;

				ICodeExpression? filter = null;

				for (var i = 0; i < associationModel.FromColumns.Length; i++)
				{
					var fromColumn = context.GetColumnProperty(associationModel.FromColumns[i]);
					var toColumn   = context.GetColumnProperty(associationModel.ToColumns[i]);

					var cond = context.AST.Equal(
						context.AST.Member(fromObject.Reference, fromColumn.Reference),
						context.AST.Member(toObject.Reference  , toColumn.Reference  ));

					filter = filter == null ? cond : context.AST.And(filter, cond);
				}

				// generate filter lambda function
				var filterLambda = context.AST
					.Lambda(
						WellKnownTypes.System.Linq.Expressions.Expression(
							WellKnownTypes.System.Func(WellKnownTypes.System.Boolean, resultEntityType)),
						true)
					.Parameter(lambdaParam);
				filterLambda.Body().Append(context.AST.Return(filter!));

				// ctx.GetTable<ResultEntity>()
				var body = context.AST.ExtCall(
						WellKnownTypes.LinqToDB.DataExtensions,
						WellKnownTypes.LinqToDB.DataExtensions_GetTable,
						WellKnownTypes.LinqToDB.ITable(resultEntityType),
						new[] { resultEntityType },
						false,
						ctxParam.Reference);

				// append First/FirstOrDefault (for optional association)
				// for non-many relation
				if (!backReference || !associationModel.ManyToOne)
				{
					var optional = backReference ? associationModel.TargetMetadata.CanBeNull : associationModel.SourceMetadata.CanBeNull;

					// .First(t => t.PK == thisEntity.PK)
					// or
					// .FirstOrDefault(t => t.PK == thisEntity.PK)
					body = context.AST.ExtCall(
						WellKnownTypes.System.Linq.Queryable,
						optional ? WellKnownTypes.System.Linq.Queryable_FirstOrDefault : WellKnownTypes.System.Linq.Queryable_First,
						resultEntityType.WithNullability(optional),
						new[] { resultEntityType },
						true,
						body,
						filterLambda.Method);
				}
				else
				{
					// .Where(t => t.PK == thisEntity.PK)
					body = context.AST.ExtCall(
						WellKnownTypes.System.Linq.Queryable,
						WellKnownTypes.System.Linq.Queryable_Where,
						WellKnownTypes.System.Linq.IQueryable(resultEntityType),
						new [] { resultEntityType },
						true,
						body,
						filterLambda.Method);
				}

				methodBuilder.Body().Append(context.AST.Return(body));
			}

			// and it's metadata
			context.MetadataBuilder?.BuildAssociationMetadata(context, context.ExtensionsClass.Type, metadata, methodBuilder);
		}
	}
}
