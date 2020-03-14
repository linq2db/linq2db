using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Stores inheritance mapping information for single discriminator value.
	/// </summary>
	public class InheritanceMapping
	{
		/// <summary>
		/// Inheritance discriminator value.
		/// </summary>
		public object?          Code;
		/// <summary>
		/// Is it default mapping.
		/// </summary>
		public bool             IsDefault;
		/// <summary>
		/// Mapping class type for current discriminator value.
		/// </summary>
		public Type             Type = null!;
		/// <summary>
		/// Discriminator column descriptor.
		/// </summary>
		public ColumnDescriptor Discriminator = null!;

		/// <summary>
		/// Gets discriminator field or property name.
		/// </summary>
		public string DiscriminatorName => Discriminator.MemberName;
	}
}
