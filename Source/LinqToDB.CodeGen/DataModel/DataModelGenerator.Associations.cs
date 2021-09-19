using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildAssociations(Func<ClassBuilder> getExtensionsClass)
		{
			var entityAssociations = new Dictionary<EntityModel, PropertyGroup>();
			var extensionEntityAssociations = new Dictionary<EntityModel, MethodGroup>();
			RegionGroup? extensionsRegion = null;

			foreach (var association in _dataModel.Associations)
				BuildAssociation(
					association,
					() => extensionsRegion ??= getExtensionsClass().Regions().New("Associations").Regions(),
					entityAssociations,
					extensionEntityAssociations);
		}

		private void BuildAssociation(
			AssociationModel association,
			Func<RegionGroup> extensionAssociations,
			Dictionary<EntityModel, PropertyGroup> entityAssociations,
			Dictionary<EntityModel, MethodGroup> extensionEntityAssociations)
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
				if (!entityAssociations.TryGetValue(association.Source, out var associations))
					entityAssociations.Add(association.Source, associations = _entityBuilders[association.Source].Regions().New("Assocations").Properties(false));

				BuildAssociationProperty(
					associations,
					association.PropertyName,
					sourceType,
					association.Summary,
					association.SourceMetadata);
			}

			if (association.BackreferencePropertyName != null)
			{
				if (!entityAssociations.TryGetValue(association.Target, out var associations))
					entityAssociations.Add(association.Target, associations = _entityBuilders[association.Target].Regions().New("Assocations").Properties(false));

				var type = sourceBuilder.Type.Type;
				if (association.ManyToOne)
				{
					if (_dataModel.AssociationCollectionAsArray)
						type = _code.ArrayType(type, false);
					else if (_dataModel.AssociationCollectionType != null)
						type = _dataModel.AssociationCollectionType.WithTypeArguments(new[] { type });
					else
						type = _code.Type(typeof(IEnumerable<>), false, type);
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
				if (!extensionEntityAssociations.TryGetValue(association.Source, out var associations))
					extensionEntityAssociations.Add(association.Source, associations = extensionAssociations().New($"{sourceBuilder.Type.Name.Name} Assocations").Methods(false));

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
				if (!extensionEntityAssociations.TryGetValue(association.Target, out var associations))
					extensionEntityAssociations.Add(association.Target, associations = extensionAssociations().New($"{targetBuilder.Type.Name.Name} Assocations").Methods(false));

				var type = sourceBuilder.Type.Type;
				if (association.ManyToOne)
					type = _code.Type(typeof(IQueryable<>), false, type);
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
				metadata.ThisKeyExpression = _code.Constant(() => getKey(_columnBuilders, thisColumns), true);
				metadata.OtherKeyExpression = _code.Constant(() => getKey(_columnBuilders, otherColumns), true);

				static string getKey(IReadOnlyDictionary<ColumnModel, PropertyBuilder> columnBuilders, ColumnModel[] columns)
				{
					return string.Join(",", columns.Select(c => columnBuilders[c].Property.Name.Name));
				}
			}
			else if (association.FromColumns.Length == 1)
			{
				// generate nameof() expressions
				metadata.ThisKeyExpression = _code.NameOf(_code.Member(thisBuilder.Type.Type, _columnBuilders[thisColumns[0]].Property));
				metadata.OtherKeyExpression = _code.NameOf(_code.Member(otherBuilder.Type.Type, _columnBuilders[otherColumns[0]].Property));
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
			var propertyBuilder = DefineProperty(associationsGroup, property);

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
			var methodBuilder = associationsGroup.New(_code.Identifier(name))
				.Public()
				.Static()
				.Extension()
				.Returns(type);

			if (summary != null)
				methodBuilder.XmlComment().Summary(summary);

			_metadataBuilder.BuildAssociationMetadata(metadata, methodBuilder);

			var thisParam = _code.Parameter(thisEntity.Type.Type, _code.Identifier("obj"), ParameterDirection.In);
			var ctxParam = _code.Parameter(_code.Type(typeof(IDataContext), false), _code.Identifier("db"), ParameterDirection.In);

			methodBuilder.Parameter(thisParam);
			methodBuilder.Parameter(ctxParam);

			if (associationModel.FromColumns == null || associationModel.ToColumns == null)
			{
				methodBuilder.Body().Append(_code.Throw(_code.New(_languageProvider.TypeParser.Parse<InvalidOperationException>(), new ICodeExpression[] { _code.Constant("Association cannot be called outside of query", true) }, Array.Empty<CodeAssignmentStatement>())));
			}
			else
			{
				var lambdaParam = _code.LambdaParameter(_code.Identifier("t"), resultEntity.Type.Type);

				ICodeExpression? filter = null;

				var fromObject = backReference ? lambdaParam : thisParam;
				var toObject = !backReference ? lambdaParam : thisParam;

				for (var i = 0; i < associationModel.FromColumns!.Length; i++)
				{
					var fromColumnBuilder = _columnBuilders[associationModel.FromColumns[i]];
					var toColumnBuilder = _columnBuilders[associationModel.ToColumns[i]];

					var cond = _code.Equal(
						_code.Member( fromObject.Reference, fromColumnBuilder.Property),
						_code.Member( toObject.Reference, toColumnBuilder.Property));
					//cond = CorrectEquality(fromColumnBuilder.Property.Type.Type, cond);

					filter = filter == null ? cond : _code.And(filter, cond);
				}

				var filterLambda = _code.Lambda(WellKnownTypes.Expression(WellKnownTypes.Func(WellKnownTypes.Boolean, resultEntity.Type.Type)), true)
					.Parameter(lambdaParam);
				filterLambda.Body().Append(_code.Return(filter!));

				var body = _code.ExtCall(
					_code.Type(typeof(Queryable), false),
					_code.Identifier(nameof(Queryable.Where)),
					Array.Empty<IType>(),
					new ICodeExpression[]
					{
						_code.ExtCall(
							_code.Type(typeof(DataExtensions), false),
							_code.Identifier(nameof(DataExtensions.GetTable)),
							new[] { resultEntity.Type.Type },
							new ICodeExpression[] { ctxParam.Reference },
							WellKnownTypes.LinqToDB.ITable(resultEntity.Type.Type)),
						filterLambda.Method
					},
					WellKnownTypes.Queryable(resultEntity.Type.Type));

				if (backReference)
				{
					if (!associationModel.ManyToOne)
						body = _code.ExtCall(
							_code.Type(typeof(Queryable), false),
							_code.Identifier(associationModel.TargetMetadata.CanBeNull ? nameof(Queryable.FirstOrDefault) : nameof(Queryable.First)),
							Array.Empty<IType>(),
							new ICodeExpression[] { body },
							resultEntity.Type.Type.WithNullability(associationModel.TargetMetadata.CanBeNull));
				}
				else
				{
					body = _code.ExtCall(
						_code.Type(typeof(Queryable), false),
						_code.Identifier(associationModel.SourceMetadata.CanBeNull ? nameof(Queryable.FirstOrDefault) : nameof(Queryable.First)),
						Array.Empty<IType>(),
						new ICodeExpression[] { body },
						resultEntity.Type.Type.WithNullability(associationModel.SourceMetadata.CanBeNull));
				}

				methodBuilder.Body().Append(_code.Return(body));
			}
		}

	}
}
