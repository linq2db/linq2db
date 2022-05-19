using LinqToDB.CodeModel;
using LinqToDB.Mapping;

namespace LinqToDB.Metadata;

/// <summary>
/// Provides database model metadata generator abstraction.
/// </summary>
public interface IMetadataBuilder
{
	/// <summary>
	/// Generates entity metadata (e.g. <see cref="TableAttribute"/>).
	/// </summary>
	/// <param name="metadata">Entity metadata descriptor.</param>
	/// <param name="entityBuilder">Entity class generator.</param>
	void BuildEntityMetadata       (EntityMetadata metadata       , ClassBuilder entityBuilder     );

	/// <summary>
	/// Generated entity column metadata (e.g. <see cref="ColumnAttribute"/>).
	/// </summary>
	/// <param name="metadata">Column metadata descriptor.</param>
	/// <param name="propertyBuilder">Column property generator.</param>
	void BuildColumnMetadata       (ColumnMetadata metadata       , PropertyBuilder propertyBuilder);

	/// <summary>
	/// Generates association metadata (e.g. <see cref="AssociationAttribute"/>) for association mapped to entity property.
	/// Generate only one side of assocation (called twice per association if both sides are mapped).
	/// </summary>
	/// <param name="metadata">Association metadata descriptor for current side of assocation.</param>
	/// <param name="propertyBuilder">Association property generator.</param>
	void BuildAssociationMetadata  (AssociationMetadata metadata  , PropertyBuilder propertyBuilder);

	/// <summary>
	/// Generates association metadata (e.g. <see cref="AssociationAttribute"/>) for association mapped to method.
	/// Generate only one side of assocation (called twice per association if both sides are mapped).
	/// </summary>
	/// <param name="metadata">Association metadata descriptor for current side of assocation.</param>
	/// <param name="methodBuilder">Association method generator.</param>
	void BuildAssociationMetadata  (AssociationMetadata metadata  , MethodBuilder methodBuilder    );

	/// <summary>
	/// Generates function metadata (e.g. <see cref="Sql.FunctionAttribute"/>) for scalar, aggregate or window (analytic) function.
	/// </summary>
	/// <param name="metadata">Function metadata descriptor.</param>
	/// <param name="methodBuilder">Function method generator.</param>
	void BuildFunctionMetadata     (FunctionMetadata metadata     , MethodBuilder methodBuilder    );

	/// <summary>
	/// Generates function metadata (e.g. <see cref="Sql.TableFunctionAttribute"/>) for table function.
	/// </summary>
	/// <param name="metadata">Function metadata descriptor.</param>
	/// <param name="methodBuilder">Function method generator.</param>
	void BuildTableFunctionMetadata(TableFunctionMetadata metadata, MethodBuilder methodBuilder    );
}
