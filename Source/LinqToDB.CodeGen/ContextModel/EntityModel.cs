using System.Collections.Generic;
using LinqToDB.CodeGen.Metadata;

namespace LinqToDB.CodeGen.ContextModel
{
	public class EntityModel
	{
		public EntityModel(string? baseClass)
		{
			BaseClass = baseClass;
		}

		public string ClassName { get; set; } = null!;
		public string NameInContext { get; set; } = null!;
		public string? BaseClass { get; set; }

		public string? Description { get; set; }
		public string? Schema { get; set; }
		public ObjectName TableName { get; set; } = null!;
		public bool IsView { get; set; }
		public bool IsSystem { get; set; }

		public List<ColumnModel> Columns { get; } = new List<ColumnModel>();
	}
}
