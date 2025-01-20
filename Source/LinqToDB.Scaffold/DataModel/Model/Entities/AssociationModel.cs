using LinqToDB.Metadata;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Association (e.g. foreign key relation) model. Defines whole relation (both sides).
	/// </summary>
	public sealed class AssociationModel
	{
		public AssociationModel(
			AssociationMetadata sourceMetadata,
			AssociationMetadata tagetMetadata,
			EntityModel         source,
			EntityModel         target,
			bool                manyToOne)
		{
			SourceMetadata = sourceMetadata;
			TargetMetadata = tagetMetadata;
			Source         = source;
			Target         = target;
			ManyToOne      = manyToOne;
		}

		/// <summary>
		/// Gets or sets association metadata for source (FROM) side or relation.
		/// </summary>
		public AssociationMetadata SourceMetadata         { get; set; }
		/// <summary>
		/// Gets or sets association source (FROM) entity model.
		/// </summary>
		public EntityModel         Source                 { get; set; }
		/// <summary>
		/// Gets or sets association source (FROM) property descriptor on entity class.
		/// When not specified - association property is not generated on entity class.
		/// </summary>
		public PropertyModel?      Property               { get; set; }
		/// <summary>
		/// Gets or sets association source (FROM) extension method descriptor.
		/// When not specified - association extension method is not generated.
		/// </summary>
		public MethodModel?        Extension              { get; set; }
		/// <summary>
		/// Gets or sets association source (FROM) columns, that used as association keys (usually foreign key columns).
		/// When not specified, assocation is defined using other means (<see cref="SourceMetadata"/> for details).
		/// </summary>
		public ColumnModel[]?      FromColumns            { get; set; }

		/// <summary>
		/// Gets or sets association metadata for target (TO) side or relation.
		/// </summary>
		public AssociationMetadata TargetMetadata         { get; set; }
		/// <summary>
		/// Gets or sets association target (TO) entity model.
		/// </summary>
		public EntityModel         Target                 { get; set; }
		/// <summary>
		/// Gets or sets association target (TO) property descriptor on entity class.
		/// When not specified - association property is not generated on entity class.
		/// </summary>
		public PropertyModel?      BackreferenceProperty  { get; set; }
		/// <summary>
		/// Gets or sets association target (TO) extension method descriptor.
		/// When not specified - association extension method is not generated.
		/// </summary>
		public MethodModel?        BackreferenceExtension { get; set; }
		/// <summary>
		/// Gets or sets association target (TO) columns, that used as association keys (usually foreign key columns).
		/// When not specified, assocation is defined using other means (<see cref="TargetMetadata"/> for details).
		/// </summary>
		public ColumnModel[]?      ToColumns              { get; set; }

		/// <summary>
		/// Gets or sets flag indicating that association is many-to-one assocation, where
		/// it has collection-based type on target entity.
		/// </summary>
		public bool                ManyToOne              { get; set; }

		/// <summary>
		/// Get or set name of foreign key constrain for association based on foreign key.
		/// </summary>
		public string?             ForeignKeyName         { get; set; }
	}
}
