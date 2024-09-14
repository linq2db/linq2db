using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
}
