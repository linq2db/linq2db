using System.Collections.Generic;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.Model
{
	public class EntityModel
	{
		public EntityModel(EntityMetadata metadata, ClassModel @class, string? contextPropertyName)
		{
			Metadata = metadata;
			Class = @class;
			ContextPropertyName = contextPropertyName;
		}

		public EntityMetadata Metadata { get; set; }

		public ClassModel Class { get; set; }

		public string? ContextPropertyName { get; set; }

		public string? FileName { get; set; }

		public bool HasFindExtension { get; set; }

		public bool OrderFindParametersByOrdinal { get; set; }

		public List<ColumnModel> Columns { get; } = new ();
	}
}
