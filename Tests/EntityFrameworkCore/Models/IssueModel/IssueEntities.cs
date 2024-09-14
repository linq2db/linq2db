using System.Collections.Generic;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public class Parent
	{
		public int Id { get; set; }
		public int? ParentId { get; set; }

		public ICollection<Child> Children { get; set; } = null!;
	}

	public class Child
	{
		public int Id { get; set; }
		public int ParentId { get; set; }

		public Parent Parent { get; set; } = null!;
		public ICollection<GrandChild> GrandChildren { get; set; } = null!;
	}

	public class GrandChild
	{
		public int Id { get; set; }
		public int ChildId { get; set; }

		public Child Child { get; set; } = null!;
	}

	public class ShadowTable
	{
		public int Id { get; set; }
	}

	public class PostgreTable
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public NpgsqlTsVector? SearchVector { get; set; }
	}
}
