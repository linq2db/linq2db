using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.DataModel
{
	public class AssociationModel
	{
		public AssociationModel(
			AssociationMetadata sourceMetadata,
			AssociationMetadata tagetMetadata,
			EntityModel source,
			EntityModel target,
			bool manyToOne)
		{
			SourceMetadata = sourceMetadata;
			TargetMetadata = tagetMetadata;
			Source = source;
			Target = target;
			ManyToOne = manyToOne;
		}

		public AssociationMetadata SourceMetadata { get; set; }
		public EntityModel Source { get; set; }
		public PropertyModel? Property { get; set; }
		public MethodModel? Extension { get; set; }
		public ColumnModel[]? FromColumns { get; set; }

		public AssociationMetadata TargetMetadata { get; set; }
		public EntityModel Target { get; set; }
		public PropertyModel? BackreferenceProperty { get; set; }
		public MethodModel? BackreferenceExtension { get; set; }
		public ColumnModel[]? ToColumns { get; set; }

		public bool ManyToOne { get; set; }
	}
}
