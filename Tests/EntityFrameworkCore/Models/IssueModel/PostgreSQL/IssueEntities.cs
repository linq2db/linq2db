using System.Collections.Generic;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.IssueModel
{
	public class PostgreTable
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public NpgsqlTsVector? SearchVector { get; set; }
	}
}
