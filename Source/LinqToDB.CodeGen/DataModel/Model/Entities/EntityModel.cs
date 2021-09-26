using System.Collections.Generic;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.DataModel
{
	public class EntityModel
	{
		public EntityModel(EntityMetadata metadata, ClassModel @class, PropertyModel? contextProperty)
		{
			Metadata = metadata;
			Class = @class;
			ContextProperty = contextProperty;
		}

		public EntityMetadata Metadata { get; set; }

		public ClassModel Class { get; set; }

		public PropertyModel? ContextProperty { get; set; }

		public bool HasFindExtension { get; set; }

		public List<ColumnModel> Columns { get; } = new ();
	}
}
