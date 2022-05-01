using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Schema;

namespace LinqToDB.Scaffold
{
	partial class DataModelLoader
	{
		/// <summary>
		/// Creates entity model from table/view schema information.
		/// </summary>
		/// <param name="dataContext">Current data model's data context descriptor.</param>
		/// <param name="table">Table or view schema data.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		/// <param name="baseType">Optional base entity class type.</param>
		private void BuildEntity(
			DataContextModel dataContext,
			TableLikeObject  table,
			ISet<string>     defaultSchemas,
			IType?           baseType)
		{
			var (tableName, isNonDefaultSchema) = ProcessObjectName(table.Name, defaultSchemas);

			var metadata = new EntityMetadata()
			{
				Name   = tableName,
				IsView = table is View
			};

			// generate name for entity table property in data context class
			var contextPropertyName = _options.DataModel.EntityContextPropertyNameProvider?.Invoke(table);
			contextPropertyName = contextPropertyName != null
				? contextPropertyName
				: _namingServices.NormalizeIdentifier(
					_options.DataModel.EntityContextPropertyNameOptions,
					table.Name.Name);

			// generate entity class name
			var className          = _options.DataModel.EntityClassNameProvider?.Invoke(table);
			var hasCustomClassName = className != null;

			className = className != null
				? className
				: _namingServices.NormalizeIdentifier(
					_options.DataModel.EntityClassNameOptions,
					table.Name.Name);

			// add schema name ato entity class name as prefix for table from non-default schema without
			// class-per-schema option set
			if (!hasCustomClassName && !_options.DataModel.GenerateSchemaAsType && isNonDefaultSchema)
				className = table.Name.Schema + "_" + className;

			// entity class properties
			var classModel       = new ClassModel(_options.CodeGeneration.ClassPerFile ? className : dataContext.Class.FileName!, className);
			classModel.Summary   = table.Description;
			classModel.BaseType  = baseType;
			classModel.Namespace = _options.CodeGeneration.Namespace;
			classModel.Modifiers = Modifiers.Public;
			if (_options.DataModel.EntityClassIsPartial)
				classModel.Modifiers = classModel.Modifiers | Modifiers.Partial;

			// entity data model
			var entity = new EntityModel(
				metadata,
				classModel,
				contextPropertyName == null
					? null
					// note that property type is open-generic here
					// concrete type argument will be set later during AST generation
					: new PropertyModel(contextPropertyName, WellKnownTypes.LinqToDB.ITableT)
					{
						Modifiers = Modifiers.Public,
						Summary   = table.Description
					});
			entity.FindExtensions = _options.DataModel.GenerateFindExtensions;

			// add entity to lookup
			_entities.Add(table.Name, new TableWithEntity(table, entity));

			BuildEntityColumns(table, entity);

			// call interceptor after entity model completely configured
			_interceptors.PreprocessEntity(_languageProvider.TypeParser, entity);

			// add entity to model
			if (isNonDefaultSchema && _options.DataModel.GenerateSchemaAsType)
				GetOrAddAdditionalSchema(dataContext, table.Name.Schema!).Entities.Add(entity);
			else
				dataContext.Entities.Add(entity);
		}

		/// <summary>
		/// Converts schema's table/view columns, primary key and identity information to entity columns.
		/// </summary>
		/// <param name="table">Table/view schema object.</param>
		/// <param name="entity">Entity.</param>
		private void BuildEntityColumns(TableLikeObject table, EntityModel entity)
		{
			Dictionary<string, ColumnModel> entityColumnsMap;
			_columns.Add(entity, entityColumnsMap = new());

			foreach (var column in table.Columns)
			{
				var typeMapping    = MapType(column.Type);
				var columnMetadata = new ColumnMetadata() { Name = column.Name };
				var propertyName   = _namingServices.NormalizeIdentifier(
					_options.DataModel.EntityColumnPropertyNameOptions,
					column.Name);
				var propertyType   = typeMapping.CLRType.WithNullability(column.Nullable);

				var columnProperty = new PropertyModel(propertyName, propertyType)
				{
					Modifiers       = Modifiers.Public,
					IsDefault       = true,
					HasSetter       = true,
					Summary         = column.Description,
					TrailingComment = column.Type.Name
				};

				var columnModel = new ColumnModel(columnMetadata, columnProperty);
				entity.Columns.Add(columnModel);
				entityColumnsMap.Add(column.Name, columnModel);

				// populate metadata for column
				columnMetadata.DbType = column.Type;
				if (!_options.DataModel.GenerateDbType)
					columnMetadata.DbType = columnMetadata.DbType with { Name = null };
				if (!_options.DataModel.GenerateLength)
					columnMetadata.DbType = columnMetadata.DbType with { Length = null };
				if (!_options.DataModel.GeneratePrecision)
					columnMetadata.DbType = columnMetadata.DbType with { Precision = null };
				if (!_options.DataModel.GenerateScale)
					columnMetadata.DbType = columnMetadata.DbType with { Scale = null };

				if (_options.DataModel.GenerateDataType)
					columnMetadata.DataType = typeMapping.DataType;

				columnMetadata.CanBeNull    = column.Nullable;
				columnMetadata.SkipOnInsert = !column.Insertable;
				columnMetadata.SkipOnUpdate = !column.Updatable;
				columnMetadata.IsIdentity   = table.Identity != null && table.Identity.Column == column.Name;

				if (table.PrimaryKey != null)
				{
					columnMetadata.IsPrimaryKey = table.PrimaryKey.Columns.Contains(column.Name);

					if (columnMetadata.IsPrimaryKey && table.PrimaryKey.Columns.Count > 1)
						columnMetadata.PrimaryKeyOrder = table.PrimaryKey.GetColumnPositionInKey(column);
				}
			}
		}

		/// <summary>
		/// Defines association from foreign key.
		/// </summary>
		/// <param name="fk">Foreign key schema object.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		/// <returns>Assocation model.
		/// Could return <c>null</c> if any of foreign key relation tables were not loaded into model.</returns>
		private AssociationModel? BuildAssociations(ForeignKey fk, ISet<string> defaultSchemas)
		{
			if (!_entities.TryGetValue(fk.Source, out var source)
				|| !_entities.TryGetValue(fk.Target, out var target))
				return null;

			var fromColumns   = new ColumnModel[fk.Relation.Count];
			var toColumns     = new ColumnModel[fk.Relation.Count];
			var sourceColumns = _columns[source.Entity];
			var targetColumns = _columns[target.Entity];

			// identify cardinality of relation (one-to-one vs one-to-many) and nullability using following information:
			// - nullability of foreign key columns (from/source side of association will be nullable in this case)
			//      same for to/target side of relation
			// - wether source/target columns used as PK or not (possible cardinality)
			// TODO: for now we don't have information about unique constraints (except PK), which also affects cardinality
			var fromOptional   = true;
			// if PK(and UNIQUE in future) and FK sizes on target table differ - this is definitely not one-to-one relation
			var manyToOne      = fk.Relation.Count != sourceColumns.Values.Count(c => c.Metadata.IsPrimaryKey);
			for (var i = 0; i < fk.Relation.Count; i++)
			{
				var mapping    = fk.Relation[i];
				fromColumns[i] = sourceColumns[mapping.SourceColumn];
				toColumns[i]   = targetColumns[mapping.TargetColumn];

				// if at least one column in foreign key is not nullable, we mark it as required
				if (fromOptional && !fromColumns[i].Metadata.CanBeNull)
					fromOptional = false;
				// if at least one column in foreign key is not a part of PK (or unique constraint in future), association has many-to-one cardinality
				// TODO: when adding unique constrains support make sure to validate that all FK columns are part of same PK/UNIQUE constrain
				// in case of composite key
				if (!manyToOne && !fromColumns[i].Metadata.IsPrimaryKey)
					manyToOne = true;
			}

			var sourceMetadata      = new AssociationMetadata() { CanBeNull = fromOptional   };
			// back-reference is always optional
			var targetMetadata      = new AssociationMetadata() { CanBeNull = true           };
			
			var association         = new AssociationModel(sourceMetadata, targetMetadata, source.Entity, target.Entity, manyToOne);
			association.FromColumns = fromColumns;
			association.ToColumns   = toColumns;

			// use foreign key name for xml-doc comment generation
			var summary              = fk.Name;
			var backreferenceSummary = $"{fk.Name} backreference";

			// use foreign key column name for association name generation
			var sourceColumnName     = fk.Relation.Count == 1 ? fk.Relation[0].SourceColumn : null;
			var fromAssociationName  = GenerateAssociationName(
				fk.Target,
				fk.Source,
				sourceColumnName,
				fk.Name,
				_options.DataModel.SourceAssociationPropertyNameOptions,
				defaultSchemas);
			var toAssocationName     = GenerateAssociationName(
				fk.Source,
				fk.Target,
				null,
				fk.Name,
				manyToOne
					? _options.DataModel.TargetMultipleAssociationPropertyNameOptions
					: _options.DataModel.TargetSingularAssociationPropertyNameOptions,
				defaultSchemas);

			// define association properties on on entities
			if (_options.DataModel.GenerateAssociations)
			{
				association.Property = new PropertyModel(fromAssociationName)
				{
					Modifiers = Modifiers.Public,
					IsDefault = true,
					HasSetter = true,
					Summary   = summary
				};
				association.BackreferenceProperty = new PropertyModel(toAssocationName)
				{
					Modifiers = Modifiers.Public,
					IsDefault = true,
					HasSetter = true,
					Summary   = backreferenceSummary
				};
			}

			// define association extension methods
			if (_options.DataModel.GenerateAssociationExtensions)
			{
				association.Extension = new MethodModel(fromAssociationName)
				{
					Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Extension,
					Summary   = summary
				};

				association.BackreferenceExtension = new MethodModel(toAssocationName)
				{
					Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Extension,
					Summary   = backreferenceSummary
				};
			}

			return association;
		}

		// a bit of inhuman logic to reduce migration PITA
		/// <summary>
		/// Generates association property/method name.
		/// </summary>
		/// <param name="thisTable">This table database name. Source table for direct relation and target for backreference.</param>
		/// <param name="otherTable">Other table database name. Target table for direct relation and source for backreference.</param>
		/// <param name="firstFromColumnName">Foreign key column name. Specified only for non-composite FK for from/source association.</param>
		/// <param name="fkName">Foreign key constrain name.</param>
		/// <param name="settings">Name generation/normalization rules.</param>
		/// <param name="defaultSchemas">List of default database schema names.</param>
		/// <returns>Property/method name for association.</returns>
		private string GenerateAssociationName(
			ObjectName           thisTable,
			ObjectName           otherTable,
			string?              firstFromColumnName,
			string               fkName,
			NormalizationOptions settings,
			ISet<string>         defaultSchemas)
		{
			var name = otherTable.Name;

			// T4 compatibility mode use logic, similar to one, used by old T4 templates
			if (settings.Transformation == NameTransformation.Association)
			{
				// approximate port of SetForeignKeyMemberName T4 method.
				// Approximate, because not all logic could be converted due to difference in generation pipeline

				string? newName = null;

				// TODO: customization/interceptors not implemented yet
				//if (schemaOptions.GetAssociationMemberName != null)
				//{
				//	newName = schemaOptions.GetAssociationMemberName(key);

				//	if (newName != null)
				//		name = ToValidName(newName);
				//}

				newName = fkName;

				// if column name provided - generate association name based on column name
				if (firstFromColumnName != null && firstFromColumnName.ToLower().EndsWith("id"))
				{
					// if column name provided and ends with ID suffix
					// we trim ID part and possible _ connectors before it
					newName = firstFromColumnName;
					newName = newName.Substring(0, newName.Length - "id".Length).TrimEnd('_');
					// here name could become empty if column name was ID
				}
				else
				{
					// if column name not provided - use FK name for association name

					// remove FK_ prefix
					if (newName.StartsWith("FK_"))
						newName = newName.Substring(3);

					// - split name into words using _ as separator
					// - remove words that match target table name, schema or any of default schema
					// - concat remaining words back into single name
					newName = string.Concat(newName
						.Split('_')
						.Where(_ =>
							_.Length > 0 && _ != otherTable.Name &&
							(otherTable.Schema == null || defaultSchemas.Contains(otherTable.Schema) || _ != otherTable.Schema)));

					// remove trailing digits
					// note that new implementation match all digits, not just 0-9 as it was in T4
					var skip = true;
					newName  = string.Concat(newName.EnumerateCharacters().Reverse().Select(_ =>
					{
						if (skip)
						{
							if (_.category == UnicodeCategory.DecimalDigitNumber)
								return string.Empty;
							else
								skip = false;
						}

						return _.codePoint;
					}).Reverse());
				}

				// if resulting name is empty - just use:
				// - for self-reference relation (to same table): table name
				// - otherwise: foreign key name without changes
				if (string.IsNullOrEmpty(newName))
					newName = thisTable == otherTable ? thisTable.Name : fkName;

				name = newName;
			}

			return _namingServices.NormalizeIdentifier(settings, name);
		}
	}
}
