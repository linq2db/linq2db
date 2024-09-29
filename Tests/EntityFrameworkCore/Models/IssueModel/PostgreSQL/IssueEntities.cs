using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel
{
	public class PostgreTable
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public NpgsqlTsVector? SearchVector { get; set; }
	}

	public class Issue155Table
	{
		public int Id { get; set; }
		public int[] Linked { get; set; } = null!;
		[NotMapped]
		public int[] LinkedFrom { get; set; } = null!;
	}

	public class Issue4641Table
	{
		public int Id { get; set; }
		public string? Value { get; set; }
	}
}
