using System.Collections.Generic;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.IssueModel
{
	public class Issue129Table
	{
		public int Id { get; private set; }
		public int Key { get; private set; }
	}

	public class Issue4816Table
	{
		public int Id { get; private set; }
		public string? ValueVarChar { get; set; }
		public string? ValueNVarChar { get; set; }
	}
}
