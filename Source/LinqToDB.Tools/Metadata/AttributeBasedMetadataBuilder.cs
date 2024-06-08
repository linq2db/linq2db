using System;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides attribute-based implementation of data model metadata generator.
	/// General rule used for generation here is that we skip property setters generation when they will set
	/// attribute property to it's default value when default value is static.
	/// Which means this rule is not applied to names that depend on mapped member name, which could be changed due to rename.
	/// E.g. table name derived from class name, or column name derived from property name.
	/// </summary>
	internal sealed class AttributeBasedMetadataBuilder : IMetadataBuilder
	{
		public static readonly IMetadataBuilder Instance = new AttributeBasedMetadataBuilder();

		private AttributeBasedMetadataBuilder()
		{
		}

		void IMetadataBuilder.BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, PropertyBuilder propertyBuilder)
		{
			BuildAssociationAttribute(context, metadata, propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		void IMetadataBuilder.BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, MethodBuilder methodBuilder)
		{
			BuildAssociationAttribute(context, metadata, methodBuilder.Attribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		void IMetadataBuilder.BuildColumnMetadata(IDataModelGenerationContext context, CodeClass entityClass, ColumnMetadata metadata, PropertyBuilder propertyBuilder)
		{
			if (!metadata.IsColumn)
			{
				// for non-column mapping ignore any other metadata properties
				propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.NotColumnAttribute);
				return;
			}

			// compared to old T4 implementation we use only ColumnAttribute
			// T4 used separate attributes for primary key, nullability and identity
			// but they just duplicate ColumnAttribute functionality
			var attr = propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute);

			// always generate column name when it is provided, even if it match property name
			// otherwise generated code will be refactoring-unfriendly (will break on property rename)
			// note that rename could happen not only as user's action in generated code, but also during code
			// generation to resolve naming conflicts with other members/types
			if (metadata.Name != null)
				attr.Parameter(context.AST.Constant(metadata.Name, true));

			// generate only "CanBeNull = false" only for non-default cases (where linq2db already infer nullability from type):
			// - for reference type is is true by default
			// - for value type it is false
			// - for nullable value type it is true
			if ((!propertyBuilder.Property.Type.Type.IsValueType && !metadata.CanBeNull)
				|| (propertyBuilder.Property.Type.Type.IsValueType && metadata.CanBeNull != propertyBuilder.Property.Type.Type.IsNullable))
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CanBeNull, context.AST.Constant(metadata.CanBeNull, true));

			if (metadata.Configuration != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration , true));
			if (metadata.DataType      != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DataType      , context.AST.Constant(metadata.DataType.Value, true));

			// generate database type attributes
			if (metadata.DbType?.Name  != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DbType, context.AST.Constant(metadata.DbType.Name, true));
			if (metadata.DbType?.Length != null)
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Length, context.AST.Constant(metadata.DbType.Length.Value, true));
			if (metadata.DbType?.Precision != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Precision, context.AST.Constant(metadata.DbType.Precision.Value, true));
			if (metadata.DbType?.Scale     != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Scale    , context.AST.Constant(metadata.DbType.Scale.Value    , true));

			if (metadata.IsPrimaryKey)
			{
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsPrimaryKey, context.AST.Constant(true, true));
				if (metadata.PrimaryKeyOrder != null)
					attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_PrimaryKeyOrder, context.AST.Constant(metadata.PrimaryKeyOrder.Value, true));
			}

			if (metadata.IsIdentity          ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsIdentity       , context.AST.Constant(true                 , true));
			if (metadata.SkipOnInsert        ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnInsert     , context.AST.Constant(true                 , true));
			if (metadata.SkipOnUpdate        ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnUpdate     , context.AST.Constant(true                 , true));
			if (metadata.SkipOnEntityFetch   ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnEntityFetch, context.AST.Constant(true                 , true));
			if (metadata.MemberName != null  ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_MemberName       , context.AST.Constant(metadata.MemberName  , true));
			if (metadata.Storage != null     ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Storage          , context.AST.Constant(metadata.Storage     , true));
			if (metadata.CreateFormat != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CreateFormat     , context.AST.Constant(metadata.CreateFormat, true));
			if (metadata.IsDiscriminator     ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsDiscriminator  , context.AST.Constant(true                 , true));
			if (metadata.Order != null       ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Order            , context.AST.Constant(metadata.Order.Value , true));
		}

		void IMetadataBuilder.BuildEntityMetadata(IDataModelGenerationContext context, EntityModel entity)
		{
			var metadata      = entity.Metadata;
			var entityBuilder = context.GetEntityBuilder(entity);
			var attr          = entityBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.TableAttribute);

			if (metadata.Name != null)
			{
				// always generate table name when it is provided, even if it match class name
				// otherwise generated code will be refactoring-unfriendly (will break on class rename)
				// note that rename could happen not only as user's action in generated code, but also during code
				// generation to resolve naming conflicts with other members/types
				attr.Parameter(context.AST.Constant(metadata.Name.Value.Name, true));
				if (metadata.Name.Value.Schema   != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Schema  , context.AST.Constant(metadata.Name.Value.Schema  , true));
				if (metadata.Name.Value.Database != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Database, context.AST.Constant(metadata.Name.Value.Database, true));
				if (metadata.Name.Value.Server   != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Server  , context.AST.Constant(metadata.Name.Value.Server  , true));
			}

			if (metadata.IsView                              ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsView                   , context.AST.Constant(true                  , true));
			if (metadata.Configuration != null               ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration          , context.AST.Constant(metadata.Configuration, true));
			if (!metadata.IsColumnAttributeRequired          ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsColumnAttributeRequired, context.AST.Constant(false                 , true));
			if (metadata.IsTemporary                         ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsTemporary              , context.AST.Constant(true                  , true));
			if (metadata.TableOptions  != TableOptions.NotSet) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_TableOptions             , context.AST.Constant(metadata.TableOptions , true));
		}

		void IMetadataBuilder.BuildFunctionMetadata(IDataModelGenerationContext context, FunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlFunctionAttribute);

			if (metadata.Name != null)
				attr.Parameter(context.AST.Constant(context.MakeFullyQualifiedRoutineName(metadata.Name.Value), true));

			if (metadata.Configuration    != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration  , context.AST.Constant(metadata.Configuration         , true));
			if (metadata.ServerSideOnly   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ServerSideOnly  , context.AST.Constant(metadata.ServerSideOnly.Value  , true));
			if (metadata.PreferServerSide != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_PreferServerSide, context.AST.Constant(metadata.PreferServerSide.Value, true));
			if (metadata.InlineParameters != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_InlineParameters, context.AST.Constant(metadata.InlineParameters.Value, true));
			if (metadata.IsPredicate      != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPredicate     , context.AST.Constant(metadata.IsPredicate.Value     , true));
			if (metadata.IsAggregate      != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsAggregate     , context.AST.Constant(metadata.IsAggregate.Value     , true));
			if (metadata.IsWindowFunction != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsWindowFunction, context.AST.Constant(metadata.IsWindowFunction.Value, true));
			if (metadata.IsPure           != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPure          , context.AST.Constant(metadata.IsPure.Value          , true));
			if (metadata.CanBeNull        != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_CanBeNull       , context.AST.Constant(metadata.CanBeNull.Value       , true));
			if (metadata.IsNullable       != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsNullable      , context.AST.Constant(metadata.IsNullable.Value      , true));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ArgIndices, context.AST.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(context, metadata.ArgIndices)));

			// Sql.FunctionAttribute.Precedence not generated, as we currenty don't allow expressions for function name in generator
		}

		void IMetadataBuilder.BuildTableFunctionMetadata(IDataModelGenerationContext context, TableFunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlTableFunctionAttribute);

			if (metadata.Name != null)
			{
				// compared to Sql.FunctionAttribute, Sql.TableFunctionAttribute provides proper FQN mapping attributes
				if (metadata.Name.Value.Name     != null) attr.Parameter(context.AST.Constant(metadata.Name.Value.Name, true));
				if (metadata.Name.Value.Package  != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Package , context.AST.Constant(metadata.Name.Value.Package , true));
				if (metadata.Name.Value.Schema   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Schema  , context.AST.Constant(metadata.Name.Value.Schema  , true));
				if (metadata.Name.Value.Database != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Database, context.AST.Constant(metadata.Name.Value.Database, true));
				if (metadata.Name.Value.Server   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Server  , context.AST.Constant(metadata.Name.Value.Server  , true));
			}

			if (metadata.Configuration != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration, true));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_ArgIndices, context.AST.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(context, metadata.ArgIndices)));
		}

		/// <summary>
		/// Generates array values for <see cref="Sql.TableFunctionAttribute.ArgIndices"/>
		/// or <see cref="Sql.ExpressionAttribute.ArgIndices"/> setter.
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="argIndices">Array values.</param>
		/// <returns>AST nodes for array values.</returns>
		private static ICodeExpression[] BuildArgIndices(IDataModelGenerationContext context, int[] argIndices)
		{
			var values = new ICodeExpression[argIndices.Length];

			for (var i = 0; i < argIndices.Length; i++)
				values[i] = context.AST.Constant(argIndices[i], true);

			return values;
		}

		/// <summary>
		/// Generates <see cref="AssociationAttribute"/> on association property or method.
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="metadata">Association metadata descriptor.</param>
		/// <param name="attr">Association attribute builder.</param>
		private static void BuildAssociationAttribute(IDataModelGenerationContext context, AssociationMetadata metadata, AttributeBuilder attr)
		{
			if (!metadata.CanBeNull)
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_CanBeNull, context.AST.Constant(false, true));

			// track association is configured to avoid generation of multiple conflicting configurations
			// as assocation could be configured in several ways
			var associationConfigured = false;
			if (metadata.ExpressionPredicate != null)
			{
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ExpressionPredicate, context.AST.Constant(metadata.ExpressionPredicate, true));
				associationConfigured = true;
			}

			if (metadata.QueryExpressionMethod != null)
			{
				if (associationConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_QueryExpressionMethod, context.AST.Constant(metadata.QueryExpressionMethod, true));
				associationConfigured = true;
			}

			// track setup status of by-column assocation mapping
			var thisConfigured  = false;
			var otherConfigured = false;

			if (metadata.ThisKeyExpression != null)
			{
				if (associationConfigured || metadata.ThisKey != null)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ThisKey, metadata.ThisKeyExpression);

				thisConfigured = true;
			}

			if (metadata.OtherKeyExpression != null)
			{
				if (associationConfigured || metadata.OtherKey != null)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_OtherKey, metadata.OtherKeyExpression);

				otherConfigured = true;
			}

			if (metadata.ThisKey != null)
			{
				if (associationConfigured || thisConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ThisKey, context.AST.Constant(metadata.ThisKey, true));
				thisConfigured = true;
			}

			if (metadata.OtherKey != null)
			{
				if (associationConfigured || otherConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_OtherKey, context.AST.Constant(metadata.OtherKey, true));
				otherConfigured = true;
			}

			if (!associationConfigured && !(thisConfigured && otherConfigured))
				throw new InvalidOperationException("Association is missing relation setup");

			if (metadata.Configuration         != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.MappingAttribute_Configuration, context.AST.Constant(metadata.Configuration, true));
			if (metadata.Alias                 != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_AliasName, context.AST.Constant(metadata.Alias        , true));
			if (metadata.Storage               != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_Storage  , context.AST.Constant(metadata.Storage      , true));
		}

		void IMetadataBuilder.Complete(IDataModelGenerationContext context) { /* no-op */ }
	}
}
