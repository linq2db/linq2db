using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public class Parent
	{
		public int Id { get; set; }
		public int? ParentId { get; set; }

		public Parent ParentsParent { get; set; } = null!;
		public ICollection<Parent> ParentChildren { get; set; } = null!;
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

	public class IdentityTable
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; private set; }
		[MaxLength(50)] public required string Name { get; init; }
	}
}
