using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Model;
using LinqToDB.Data;

namespace LinqToDB.CodeGen.DataModel
{
	partial class DataModelGenerator
	{
		private void BuildEntities(
			IReadOnlyCollection<EntityModel> entities,
			Func<EntityModel, ClassBuilder> defineEntityClass,
			PropertyGroup contextProperties,
			bool contextIsDataConnection,
			ICodeExpression context,
			Func<MethodGroup> findMethodsGroup)
		{
			foreach (var entity in entities)
			{
				var entityBuilder = defineEntityClass(entity);
				_entityBuilders.Add(entity, entityBuilder);
				BuildEntity(entity, entityBuilder, contextProperties, contextIsDataConnection, context, findMethodsGroup);
			}
		}

		private void BuildEntity(
			EntityModel entity,
			ClassBuilder entityBuilder,
			PropertyGroup contextProperties,
			bool contextIsDataConnection,
			ICodeExpression context,
			Func<MethodGroup> findMethodsGroup)
		{
			var columnsGroup = entityBuilder.Properties(true);

			_metadataBuilder.BuildEntityMetadata(entity.Metadata, entityBuilder);

			foreach (var columnModel in entity.Columns)
			{
				var columnBuilder = DefineProperty(columnsGroup, columnModel.Property);
				_metadataBuilder.BuildColumnMetadata(columnModel.Metadata!, columnBuilder);
				_columnBuilders.Add(columnModel, columnBuilder);
			}

			if (entity.ContextPropertyName != null)
			{
				var contextProperty = DefineProperty(
					contextProperties,
					new PropertyModel(entity.ContextPropertyName, _code.Type(typeof(ITable<>), false, entityBuilder.Type.Type))
					{
						IsPublic = true,
						Summary = entity.Class.Summary
					});

				var getTableCall = contextIsDataConnection
					?_code.Call(
							context,
							_code.Identifier(nameof(DataConnection.GetTable)),
							new[] { entityBuilder.Type.Type },
							Array.Empty<ICodeExpression>(),
							WellKnownTypes.LinqToDB.ITable(entityBuilder.Type.Type))
					:_code.ExtCall(
							_code.Type(typeof(DataExtensions), false),
							_code.Identifier(nameof(DataExtensions.GetTable)),
							new[] { entityBuilder.Type.Type },
							new ICodeExpression[] { context },
							entityBuilder.Type.Type);

				contextProperty
					.AddGetter()
					// it could be DataExtensions.GetTable or DataConnection.GetTable
					// we will generate DataExtensions.GetTable call into model for safety as we don't know if base type
					// is DataConnection-based in general
					.Append(_code.Return(getTableCall));
			}

			BuildFindExtension(entity, entityBuilder.Type.Type, findMethodsGroup);
		}

		private void BuildFindExtension(
			EntityModel entityModel,
			IType entityType,
			Func<MethodGroup> findMethodsGroup)
		{
			if (!entityModel.HasFindExtension || !entityModel.Columns.Any(c => c.Metadata.IsPrimaryKey))
				return;

			var filterEntityParameter = _code.LambdaParameter(_code.Identifier("t"), entityType);

			var methodParameters = new List<CodeParameter>();

			methodParameters.Add(_code.Parameter(_code.Type(typeof(ITable<>), false, entityType), _code.Identifier("table"), ParameterDirection.In));

			var pks = entityModel.Columns.Where(c => c.Metadata.IsPrimaryKey);
			if (_dataModel.OrderFindParametersByOrdinal)
				pks = pks.OrderBy(c => c.Metadata.PrimaryKeyOrder);

			var comparisons = new List<(int ordinal, ICodeExpression comparison)>();
			foreach (var column in pks)
			{
				// we don't allow user to set names for method parameters as it:
				// 1. simplify data model
				// 2. custom naming doesn't add value to generated model
				// we could change it on request later
				var paramName = _namingServices.NormalizeIdentifier(_parameterNameNormalization, column.Property.Name);
				var parameter = _code.Parameter(column.Property.Type, _code.Identifier(paramName), ParameterDirection.In);
				methodParameters.Add(parameter);

				var condition = _code.Equal(_code.Member(filterEntityParameter.Reference, _columnBuilders[column].Property), parameter.Reference);

				// TODO: correction visitor
				//condition = CorrectEquality(column.Property.Type, condition);

				comparisons.Add((column.Metadata.PrimaryKeyOrder ?? 0, condition));
			}

			IEnumerable<(int pkOrdinal, ICodeExpression comparison)> orderedComparisons = comparisons;
			if (!_dataModel.OrderFindParametersByOrdinal)
			{
				// restore ordinal sorting for filter
				orderedComparisons = orderedComparisons.OrderBy(_ => _.pkOrdinal);
			}

			ICodeExpression filter = null!;
			foreach (var (_, cond) in orderedComparisons)
				filter = filter == null ? cond : _code.And(filter, cond);

			var find = findMethodsGroup().New(_code.Identifier("Find"));

			foreach (var param in methodParameters)
				find.Parameter(param);

			// Expression<Func<TSource, bool>>
			var filterLambda = _code.Lambda(WellKnownTypes.Expression(WellKnownTypes.Func(WellKnownTypes.Boolean, entityType)), true)
				.Parameter(filterEntityParameter);

			filterLambda.Body().Append(_code.Return(filter));

			find
				.Public()
				.Extension()
				.Returns(entityType.WithNullability(true))
				.Body()
				.Append(
					_code.Return(
						_code.ExtCall(
							_code.Type(typeof(Queryable), false),
							_code.Identifier(nameof(Queryable.FirstOrDefault)),
							Array.Empty<IType>(),
							new ICodeExpression[]
							{
								methodParameters[0].Reference,
								filterLambda.Method
							},
							entityType.WithNullability(true))));
		}
	}
}
