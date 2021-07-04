using System.Collections.Generic;

namespace LinqToDB.CodeGen.ContextModel
{
	public class Association
	{
		public EntityModel SourceEntity { get; set; } = null!;
		public List<ColumnModel> SourceColumns { get; set; } = new ();
		public bool SourceIsOptional { get; set; }
		public string SourceMemberName { get; set; } = null!;

		public EntityModel TargetEntity { get; set; } = null!;
		public List<ColumnModel> TargetColumns { get; set; } = new ();
		public bool TargetIsOptional { get; set; }
		public string TargetMemberName { get; set; } = null!;

		public bool ManyToOne { get; set; }

		public string KeyName { get; set; } = null!;
	}

}
