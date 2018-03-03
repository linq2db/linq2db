namespace LinqToDB.Mapping
{
	// TODO: V2 - remove?
	/// <summary>
	/// Defines relationship types for associations.
	/// See <see cref="AssociationAttribute.Relationship"/> for more details.
	/// </summary>
	public enum Relationship
	{
		/// <summary>
		/// One-to-one relationship.
		/// </summary>
		OneToOne,

		/// <summary>
		/// One-to-many relationship.
		/// </summary>
		OneToMany,

		/// <summary>
		/// Many-to-one relationship.
		/// </summary>
		ManyToOne,
	}
}
