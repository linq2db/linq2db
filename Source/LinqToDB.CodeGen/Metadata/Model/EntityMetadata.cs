using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Metadata
{
	public class EntityMetadata
	{
		public EntityMetadata()
		{
		}

		public EntityMetadata(ObjectName name, bool isView)
		{
			Name = name;
			IsView = isView;
		}

		// set by framework
		public ObjectName? Name { get; set; }
		public bool IsView { get; set; }

		// additional metadata, that could be set by user
		public string? Configuration { get; set; }
		public bool IsColumnAttributeRequired { get; set; } = true;
		public bool IsTemporary { get; set; }
		public TableOptions TableOptions { get; set; }
	}
}
