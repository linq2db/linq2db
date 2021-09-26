using System;
using System.Text;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;
using LinqToDB.CodeGen.Schema;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public class AttributeBasedMetadataBuilder : IMetadataBuilder
	{
		private readonly CodeBuilder _builder;
		private readonly ISqlBuilder _sqlBuilder;

		public AttributeBasedMetadataBuilder(CodeBuilder builder, ISqlBuilder sqlBuilder)
		{
			_builder = builder;
			_sqlBuilder = sqlBuilder;
		}

		void IMetadataBuilder.BuildAssociationMetadata(AssociationMetadata metadata, PropertyBuilder propertyBuilder)
		{
			BuildAssociationAttribute(metadata, propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		private void BuildAssociationAttribute(AssociationMetadata metadata, AttributeBuilder attr)
		{
			if (!metadata.CanBeNull)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.CanBeNull)), _builder.Constant(false, true));

			var associationConfigured = false;
			if (metadata.ExpressionPredicate != null)
			{
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.ExpressionPredicate)), _builder.Constant(metadata.ExpressionPredicate, true));
				associationConfigured = true;
			}

			if (metadata.QueryExpressionMethod != null)
			{
				if (associationConfigured)
					throw new InvalidOperationException($"Association contains multiple relation setups");

				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.QueryExpressionMethod)), _builder.Constant(metadata.QueryExpressionMethod, true));
				associationConfigured = true;
			}

			var thisConfigured = false;
			var otherConfigured = false;
			if (metadata.ThisKeyExpression != null)
			{
				if (associationConfigured || metadata.ThisKey != null)
					throw new InvalidOperationException($"Association contains multiple relation setups");

				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.ThisKey)), metadata.ThisKeyExpression);

				thisConfigured = true;
			}

			if (metadata.OtherKeyExpression != null)
			{
				if (associationConfigured || metadata.OtherKey != null)
					throw new InvalidOperationException($"Association contains multiple relation setups");

				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.OtherKey)), metadata.OtherKeyExpression);

				otherConfigured = true;
			}

			if (metadata.ThisKey != null)
			{
				if (associationConfigured || thisConfigured)
					throw new InvalidOperationException($"Association contains multiple relation setups");

				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.ThisKey)), _builder.Constant(metadata.ThisKey, true));
				thisConfigured = true;
			}

			if (metadata.OtherKey != null)
			{
				if (associationConfigured || otherConfigured)
					throw new InvalidOperationException($"Association contains multiple relation setups");

				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.OtherKey)), _builder.Constant(metadata.OtherKey, true));
				otherConfigured = true;
			}

			if (!associationConfigured && !(thisConfigured && otherConfigured))
				throw new InvalidOperationException($"Association is missing relation setup");

			if (metadata.Configuration != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.Configuration)), _builder.Constant(metadata.Configuration, true));
			if (metadata.Alias != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.AliasName)), _builder.Constant(metadata.Alias, true));
			if (metadata.Storage != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.Storage)), _builder.Constant(metadata.Storage, true));
			if (metadata.KeyName != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.KeyName)), _builder.Constant(metadata.KeyName, true));
			if (metadata.BackReferenceName != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.BackReferenceName)), _builder.Constant(metadata.BackReferenceName, true));
			if (metadata.HasIsBackReference != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.IsBackReference)), _builder.Constant(metadata.IsBackReference, true));
			if (metadata.Relationship != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.Relationship)), _builder.Constant(metadata.Relationship.Value, true));
			if (metadata.QueryExpressionMethod != null)
				attr.Parameter(new CodeIdentifier(nameof(AssociationAttribute.QueryExpressionMethod)), _builder.Constant(metadata.QueryExpressionMethod, true));
		}

		void IMetadataBuilder.BuildAssociationMetadata(AssociationMetadata metadata, MethodBuilder methodBuilder)
		{
			BuildAssociationAttribute(metadata, methodBuilder.Attribute(WellKnownTypes.LinqToDB.Mapping.AssociationAttribute));
		}

		void IMetadataBuilder.BuildColumnMetadata(ColumnMetadata metadata, PropertyBuilder propertyBuilder)
		{
			if (!metadata.IsColumn)
			{
				propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.NotColumnAttribute);
				return;
			}

			// compared to old T4 implementation we use only ColumnAttribute
			var attr = propertyBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.ColumnAttribute);

			// always add name as we can rename property (e.g. being invalid/duplicate in context)
			if (metadata.Name != null)
				attr.Parameter(_builder.Constant(metadata.Name, true));

			attr.Parameter(_builder.Name(nameof(ColumnAttribute.CanBeNull)), _builder.Constant(metadata.CanBeNull, true));

			if (metadata.DbType?.Name != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.DbType)), _builder.Constant(metadata.DbType.Name, true));
			if (metadata.DataType != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.DataType)), _builder.Constant(metadata.DataType.Value, true));
			// TODO: min/max check added to avoid issues with type inconsistance in schema API and metadata
			if (metadata.DbType?.Length != null && metadata.DbType.Length >= int.MinValue && metadata.DbType.Length <= int.MaxValue)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Length)), _builder.Constant((int)metadata.DbType.Length.Value, true));
			if (metadata.DbType?.Precision != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Precision)), _builder.Constant(metadata.DbType.Precision.Value, true));
			if (metadata.DbType?.Scale != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Scale)), _builder.Constant(metadata.DbType.Scale.Value, true));

			if (metadata.IsPrimaryKey)
			{
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.IsPrimaryKey)), _builder.Constant(true, true));
				if (metadata.PrimaryKeyOrder != null)
					attr.Parameter(_builder.Name(nameof(ColumnAttribute.PrimaryKeyOrder)), _builder.Constant(metadata.PrimaryKeyOrder.Value, true));
			}

			if (metadata.IsIdentity)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.IsIdentity)), _builder.Constant(true, true));

			if (metadata.SkipOnInsert)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.SkipOnInsert)), _builder.Constant(true, true));
			if (metadata.SkipOnUpdate)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.SkipOnUpdate)), _builder.Constant(true, true));

			if (metadata.Configuration != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Configuration)), _builder.Constant(metadata.Configuration, true));
			if (metadata.MemberName != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.MemberName)), _builder.Constant(metadata.MemberName, true));
			if (metadata.Storage != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Storage)), _builder.Constant(metadata.Storage, true));
			if (metadata.CreateFormat != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.CreateFormat)), _builder.Constant(metadata.CreateFormat, true));

			if (metadata.IsDiscriminator)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.IsDiscriminator)), _builder.Constant(true, true));
			if (metadata.SkipOnEntityFetch)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.SkipOnEntityFetch)), _builder.Constant(true, true));

			if (metadata.Order != null)
				attr.Parameter(_builder.Name(nameof(ColumnAttribute.Order)), _builder.Constant(metadata.Order.Value, true));
		}

		void IMetadataBuilder.BuildEntityMetadata(EntityMetadata metadata, ClassBuilder entityBuilder)
		{
			var attr = entityBuilder.AddAttribute(WellKnownTypes.LinqToDB.Mapping.TableAttribute);
			if (metadata.Name != null)
			{
				attr.Parameter(_builder.Constant(metadata.Name.Name, true));
				if (metadata.Name.Schema != null)
					attr.Parameter(new CodeIdentifier(nameof(TableAttribute.Schema)), _builder.Constant(metadata.Name.Schema, true));
				if (metadata.Name.Database != null)
					attr.Parameter(new CodeIdentifier(nameof(TableAttribute.Database)), _builder.Constant(metadata.Name.Database, true));
				if (metadata.Name.Server != null)
					attr.Parameter(new CodeIdentifier(nameof(TableAttribute.Server)), _builder.Constant(metadata.Name.Server, true));
			}

			if (metadata.IsView)
				attr.Parameter(new CodeIdentifier(nameof(TableAttribute.IsView)), _builder.Constant(true, true));

			if (metadata.Configuration != null)
				attr.Parameter(new CodeIdentifier(nameof(TableAttribute.Configuration)), _builder.Constant(metadata.Configuration, true));
			if (!metadata.IsColumnAttributeRequired)
				attr.Parameter(new CodeIdentifier(nameof(TableAttribute.IsColumnAttributeRequired)), _builder.Constant(false, true));
			if (metadata.IsTemporary)
				attr.Parameter(new CodeIdentifier(nameof(TableAttribute.IsTemporary)), _builder.Constant(true, true));
			if (metadata.TableOptions != TableOptions.NotSet)
				attr.Parameter(new CodeIdentifier(nameof(TableAttribute.TableOptions)), _builder.Constant(metadata.TableOptions, true));
		}

		void IMetadataBuilder.BuildFunctionMetadata(FunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlFunctionAttribute);

			if (metadata.Name != null)
			{
				attr.Parameter(_builder.Constant(BuildFunctionName(metadata.Name), true));
			}

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
			{
				var values = new ICodeExpression[metadata.ArgIndices.Length];
				for (var i = 0; i < metadata.ArgIndices.Length; i++)
					values[i] = _builder.Constant(metadata.ArgIndices[i], true);
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.ArgIndices)), _builder.Array(WellKnownTypes.System.Int32, true, values, true));
			}

			if (metadata.ServerSideOnly != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.ServerSideOnly)), _builder.Constant(metadata.ServerSideOnly.Value, true));
			if (metadata.PreferServerSide != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.PreferServerSide)), _builder.Constant(metadata.PreferServerSide.Value, true));
			if (metadata.InlineParameters != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.InlineParameters)), _builder.Constant(metadata.InlineParameters.Value, true));
			if (metadata.IsPredicate != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.IsPredicate)), _builder.Constant(metadata.IsPredicate.Value, true));

			if (metadata.IsAggregate != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.IsAggregate)), _builder.Constant(metadata.IsAggregate.Value, true));
			if (metadata.IsWindowFunction != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.IsWindowFunction)), _builder.Constant(metadata.IsWindowFunction.Value, true));
			if (metadata.IsPure != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.IsPure)), _builder.Constant(metadata.IsPure.Value, true));
			if (metadata.CanBeNull != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.CanBeNull)), _builder.Constant(metadata.CanBeNull.Value, true));
			if (metadata.IsNullable != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.IsNullable)), _builder.Constant(metadata.IsNullable.Value, true));

			if (metadata.Precedence != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.Precedence)), _builder.Constant(metadata.Precedence.Value, true));
			if (metadata.Configuration != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.FunctionAttribute.Configuration)), _builder.Constant(metadata.Configuration, true));
		}

		void IMetadataBuilder.BuildTableFunctionMetadata(TableFunctionMetadata metadata, MethodBuilder methodBuilder)
		{
			var attr = methodBuilder.Attribute(WellKnownTypes.LinqToDB.SqlTableFunctionAttribute);

			if (metadata.Name != null)
			{
				if (metadata.Name.Name != null)
					attr.Parameter(_builder.Constant(metadata.Name.Name, true));

				if (metadata.Name.Schema != null)
					attr.Parameter(new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Schema)), _builder.Constant(metadata.Name.Schema, true));
				if (metadata.Name.Database != null)
					attr.Parameter(new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Database)), _builder.Constant(metadata.Name.Database, true));
				if (metadata.Name.Server != null)
					attr.Parameter(new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Server)), _builder.Constant(metadata.Name.Server, true));
			}

			if (metadata.ArgIndices != null && metadata.ArgIndices.Length > 0)
			{
				var values = new ICodeExpression[metadata.ArgIndices.Length];
				for (var i = 0; i < metadata.ArgIndices.Length; i++)
					values[i] = _builder.Constant(metadata.ArgIndices[i], true);
				attr.Parameter(new CodeIdentifier(nameof(Sql.TableFunctionAttribute.ArgIndices)), _builder.Array(WellKnownTypes.System.Int32, true, values, true));
			}

			if (metadata.Configuration != null)
				attr.Parameter(new CodeIdentifier(nameof(Sql.TableFunctionAttribute.Configuration)), _builder.Constant(metadata.Configuration, true));
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
	}
}
