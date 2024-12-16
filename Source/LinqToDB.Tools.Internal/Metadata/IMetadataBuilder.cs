using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Provides database model metadata generator abstraction.
	/// </summary>
	public interface IMetadataBuilder
	{
		/// <summary>
		/// Generates entity metadata (e.g. <see cref="TableAttribute"/>).
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="entity">Entity model.</param>
		void BuildEntityMetadata(IDataModelGenerationContext context, EntityModel entity);

		/// <summary>
		/// Generated entity column metadata (e.g. <see cref="ColumnAttribute"/>).
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="entityClass">Column entity class.</param>
		/// <param name="metadata">Column metadata descriptor.</param>
		/// <param name="propertyBuilder">Column property generator.</param>
		void BuildColumnMetadata(IDataModelGenerationContext context, CodeClass entityClass, ColumnMetadata metadata, PropertyBuilder propertyBuilder);

		/// <summary>
		/// Generates association metadata (e.g. <see cref="AssociationAttribute"/>) for association mapped to entity property.
		/// Generate only one side of assocation (called twice per association if both sides are mapped).
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="entityClass">Association entity class.</param>
		/// <param name="metadata">Association metadata descriptor for current side of assocation.</param>
		/// <param name="propertyBuilder">Association property generator.</param>
		void BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, PropertyBuilder propertyBuilder);

		/// <summary>
		/// Generates association metadata (e.g. <see cref="AssociationAttribute"/>) for association mapped to method.
		/// Generate only one side of assocation (called twice per association if both sides are mapped).
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="entityClass">Association entity class.</param>
		/// <param name="metadata">Association metadata descriptor for current side of assocation.</param>
		/// <param name="methodBuilder">Association method generator.</param>
		void BuildAssociationMetadata(IDataModelGenerationContext context, CodeClass entityClass, AssociationMetadata metadata, MethodBuilder methodBuilder);

		/// <summary>
		/// Generates function metadata (e.g. <see cref="Sql.FunctionAttribute"/>) for scalar, aggregate or window (analytic) function.
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="metadata">Function metadata descriptor.</param>
		/// <param name="methodBuilder">Function method generator.</param>
		void BuildFunctionMetadata(IDataModelGenerationContext context, FunctionMetadata metadata, MethodBuilder methodBuilder);

		/// <summary>
		/// Generates function metadata (e.g. <see cref="Sql.TableFunctionAttribute"/>) for table function.
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		/// <param name="metadata">Function metadata descriptor.</param>
		/// <param name="methodBuilder">Function method generator.</param>
		void BuildTableFunctionMetadata(IDataModelGenerationContext context, TableFunctionMetadata metadata, MethodBuilder methodBuilder);

		/// <summary>
		/// Finalizes metadata generation.
		/// </summary>
		/// <param name="context">Data model generation context.</param>
		void Complete(IDataModelGenerationContext context);
	}
}
