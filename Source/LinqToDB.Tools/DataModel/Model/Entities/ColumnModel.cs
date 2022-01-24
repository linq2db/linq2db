using LinqToDB.Metadata;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Entity column model.
	/// </summary>
	public sealed class ColumnModel
	{
		public ColumnModel(ColumnMetadata metadata, PropertyModel property)
		{
			Metadata = metadata;
			Property = property;
		}

		/// <summary>
		/// Gets or sets column mapping metadata.
		/// </summary>
		public ColumnMetadata Metadata { get; set; }
		/// <summary>
		/// Gets or sets code property attributes for column.
		/// </summary>
		public PropertyModel  Property { get; set; }
	}
}
