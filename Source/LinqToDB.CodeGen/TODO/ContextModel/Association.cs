using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.Model
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
		public string? PropertyName { get; set; }
		public string? ExtensionName { get; set; }
		public string? Summary { get; set; }
		public ColumnModel[]? FromColumns { get; set; }

		public AssociationMetadata TargetMetadata { get; set; }
		public EntityModel Target { get; set; }
		public string? BackreferencePropertyName { get; set; }
		public string? BackreferenceExtensionName { get; set; }
		public string? BackreferenceSummary { get; set; }
		public ColumnModel[]? ToColumns { get; set; }

		public bool ManyToOne { get; set; }
	}
}
