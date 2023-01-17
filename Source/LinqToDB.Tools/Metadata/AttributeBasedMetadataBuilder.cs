using System;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

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
		private readonly CodeBuilder                 _builder;
		private readonly Func<SqlObjectName, string> _fqnGenerator;

		public AttributeBasedMetadataBuilder(CodeBuilder builder, Func<SqlObjectName, string> fqnGenerator)
		{
			_builder      = builder;
			_fqnGenerator = fqnGenerator;
		}

		void IMetadataBuilder.BuildAssociationMetadata(AssociationMetadata metadata, PropertyBuilder propertyBuilder)
		{
			BuildAssociationAttribute(metadata, propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		void IMetadataBuilder.BuildAssociationMetadata(AssociationMetadata metadata, MethodBuilder methodBuilder)
		{
			BuildAssociationAttribute(metadata, methodBuilder.Attribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		void IMetadataBuilder.BuildColumnMetadata(ColumnMetadata metadata, PropertyBuilder propertyBuilder)
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
				attr.Parameter(_builder.Constant(metadata.Name, true));

			// generate only "CanBeNull = false" only for non-default cases (where linq2db already infer nullability from type):
			// - for reference type is is true by default
			// - for value type it is false
			// - for nullable value type it is true
			if ((!propertyBuilder.Property.Type.Type.IsValueType && !metadata.CanBeNull)
				|| (propertyBuilder.Property.Type.Type.IsValueType && metadata.CanBeNull != propertyBuilder.Property.Type.Type.IsNullable))
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CanBeNull, _builder.Constant(metadata.CanBeNull, true));

			if (metadata.Configuration != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Configuration, _builder.Constant(metadata.Configuration , true));
			if (metadata.DataType      != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DataType     , _builder.Constant(metadata.DataType.Value, true));

			// generate database type attributes
			if (metadata.DbType?.Name  != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_DbType, _builder.Constant(metadata.DbType.Name, true));
			if (metadata.DbType?.Length != null)
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Length, _builder.Constant(metadata.DbType.Length.Value, true));
			if (metadata.DbType?.Precision != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Precision, _builder.Constant(metadata.DbType.Precision.Value, true));
			if (metadata.DbType?.Scale     != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Scale    , _builder.Constant(metadata.DbType.Scale.Value    , true));

			if (metadata.IsPrimaryKey)
			{
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsPrimaryKey, _builder.Constant(true, true));
				if (metadata.PrimaryKeyOrder != null)
					attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_PrimaryKeyOrder, _builder.Constant(metadata.PrimaryKeyOrder.Value, true));
			}

			if (metadata.IsIdentity          ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsIdentity       , _builder.Constant(true                 , true));
			if (metadata.SkipOnInsert        ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnInsert     , _builder.Constant(true                 , true));
			if (metadata.SkipOnUpdate        ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnUpdate     , _builder.Constant(true                 , true));
			if (metadata.SkipOnEntityFetch   ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_SkipOnEntityFetch, _builder.Constant(true                 , true));
			if (metadata.MemberName != null  ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_MemberName       , _builder.Constant(metadata.MemberName  , true));
			if (metadata.Storage != null     ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Storage          , _builder.Constant(metadata.Storage     , true));
			if (metadata.CreateFormat != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_CreateFormat     , _builder.Constant(metadata.CreateFormat, true));
			if (metadata.IsDiscriminator     ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_IsDiscriminator  , _builder.Constant(true                 , true));
			if (metadata.Order != null       ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute_Order            , _builder.Constant(metadata.Order.Value , true));
		}

		void IMetadataBuilder.BuildEntityMetadata(EntityMetadata metadata, ClassBuilder entityBuilder)
		{
			var attr = entityBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.TableAttribute);

			if (metadata.Name != null)
			{
				// always generate table name when it is provided, even if it match class name
				// otherwise generated code will be refactoring-unfriendly (will break on class rename)
				// note that rename could happen not only as user's action in generated code, but also during code
				// generation to resolve naming conflicts with other members/types
				attr.Parameter(_builder.Constant(metadata.Name.Value.Name, true));
				if (metadata.Name.Value.Schema   != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Schema  , _builder.Constant(metadata.Name.Value.Schema  , true));
				if (metadata.Name.Value.Database != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Database, _builder.Constant(metadata.Name.Value.Database, true));
				if (metadata.Name.Value.Server   != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Server  , _builder.Constant(metadata.Name.Value.Server  , true));
			}

			if (metadata.IsView                              ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsView                   , _builder.Constant(true                  , true));
			if (metadata.Configuration != null               ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_Configuration            , _builder.Constant(metadata.Configuration, true));
			if (!metadata.IsColumnAttributeRequired          ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsColumnAttributeRequired, _builder.Constant(false                 , true));
			if (metadata.IsTemporary                         ) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_IsTemporary              , _builder.Constant(true                  , true));
			if (metadata.TableOptions  != TableOptions.NotSet) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.TableAttribute_TableOptions             , _builder.Constant(metadata.TableOptions , true));
		}

		void IMetadataBuilder.BuildFunctionMetadata(FunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlFunctionAttribute);

			if (metadata.Name != null)
			{
				// TODO: linq2db fix
				// currently we don't have mapping API for functions that use FQN components, so we need to generate raw SQL name
				attr.Parameter(_builder.Constant(_fqnGenerator(metadata.Name.Value), true));
			}

			if (metadata.Configuration    != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_Configuration   , _builder.Constant(metadata.Configuration         , true));
			if (metadata.ServerSideOnly   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ServerSideOnly  , _builder.Constant(metadata.ServerSideOnly.Value  , true));
			if (metadata.PreferServerSide != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_PreferServerSide, _builder.Constant(metadata.PreferServerSide.Value, true));
			if (metadata.InlineParameters != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_InlineParameters, _builder.Constant(metadata.InlineParameters.Value, true));
			if (metadata.IsPredicate      != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPredicate     , _builder.Constant(metadata.IsPredicate.Value     , true));
			if (metadata.IsAggregate      != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsAggregate     , _builder.Constant(metadata.IsAggregate.Value     , true));
			if (metadata.IsWindowFunction != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsWindowFunction, _builder.Constant(metadata.IsWindowFunction.Value, true));
			if (metadata.IsPure           != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsPure          , _builder.Constant(metadata.IsPure.Value          , true));
			if (metadata.CanBeNull        != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_CanBeNull       , _builder.Constant(metadata.CanBeNull.Value       , true));
			if (metadata.IsNullable       != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_IsNullable      , _builder.Constant(metadata.IsNullable.Value      , true));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				attr.Parameter(WellKnownTypes.LinqToDB.Sql_ExpressionAttribute_ArgIndices, _builder.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(metadata.ArgIndices)));

			// Sql.FunctionAttribute.Precedence not generated, as we currenty don't allow expressions for function name in generator
		}

		void IMetadataBuilder.BuildTableFunctionMetadata(TableFunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlTableFunctionAttribute);

			if (metadata.Name != null)
			{
				// compared to Sql.FunctionAttribute, Sql.TableFunctionAttribute provides proper FQN mapping attributes
				if (metadata.Name.Value.Name     != null) attr.Parameter(_builder.Constant(metadata.Name.Value.Name, true));
				if (metadata.Name.Value.Package  != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Package , _builder.Constant(metadata.Name.Value.Package , true));
				if (metadata.Name.Value.Schema   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Schema  , _builder.Constant(metadata.Name.Value.Schema  , true));
				if (metadata.Name.Value.Database != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Database, _builder.Constant(metadata.Name.Value.Database, true));
				if (metadata.Name.Value.Server   != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Server  , _builder.Constant(metadata.Name.Value.Server  , true));
			}

			if (metadata.Configuration != null) attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_Configuration, _builder.Constant(metadata.Configuration, true));

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
				attr.Parameter(WellKnownTypes.LinqToDB.Sql_TableFunctionAttribute_ArgIndices, _builder.Array(WellKnownTypes.System.Int32, true, true, BuildArgIndices(metadata.ArgIndices)));
		}

		/// <summary>
		/// Generates array values for <see cref="Sql.TableFunctionAttribute.ArgIndices"/>
		/// or <see cref="Sql.ExpressionAttribute.ArgIndices"/> setter.
		/// </summary>
		/// <param name="argIndices">Array values.</param>
		/// <returns>AST nodes for array values.</returns>
		private ICodeExpression[] BuildArgIndices(int[] argIndices)
		{
			var values = new ICodeExpression[argIndices.Length];

			for (var i = 0; i < argIndices.Length; i++)
				values[i] = _builder.Constant(argIndices[i], true);

			return values;
		}

		/// <summary>
		/// Generates <see cref="AssociationAttribute"/> on association property or method.
		/// </summary>
		/// <param name="metadata">Association metadata descriptor.</param>
		/// <param name="attr">Association attribute builder.</param>
		private void BuildAssociationAttribute(AssociationMetadata metadata, AttributeBuilder attr)
		{
			if (!metadata.CanBeNull)
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_CanBeNull, _builder.Constant(false, true));

			// track association is configured to avoid generation of multiple conflicting configurations
			// as assocation could be configured in several ways
			var associationConfigured = false;
			if (metadata.ExpressionPredicate != null)
			{
				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ExpressionPredicate, _builder.Constant(metadata.ExpressionPredicate, true));
				associationConfigured = true;
			}

			if (metadata.QueryExpressionMethod != null)
			{
				if (associationConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_QueryExpressionMethod, _builder.Constant(metadata.QueryExpressionMethod, true));
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

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_ThisKey, _builder.Constant(metadata.ThisKey, true));
				thisConfigured = true;
			}

			if (metadata.OtherKey != null)
			{
				if (associationConfigured || otherConfigured)
					throw new InvalidOperationException("Association contains multiple relation setups");

				attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_OtherKey, _builder.Constant(metadata.OtherKey, true));
				otherConfigured = true;
			}

			if (!associationConfigured && !(thisConfigured && otherConfigured))
				throw new InvalidOperationException("Association is missing relation setup");

			if (metadata.Configuration         != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_Configuration    , _builder.Constant(metadata.Configuration        , true));
			if (metadata.Alias                 != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_AliasName        , _builder.Constant(metadata.Alias                , true));
			if (metadata.Storage               != null) attr.Parameter(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute_Storage          , _builder.Constant(metadata.Storage              , true));
		}
	}
}
