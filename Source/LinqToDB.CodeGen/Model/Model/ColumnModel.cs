using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.Model
{
	public class ColumnModel
	{
		public ColumnModel(ColumnMetadata metadata, PropertyModel property)
		{
			Metadata = metadata;
			Property = property;
		}

		public ColumnMetadata Metadata { get; set; }

		public PropertyModel Property { get; set; }
	}
}
