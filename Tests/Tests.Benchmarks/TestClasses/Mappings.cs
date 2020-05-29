using System;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Mappings
{
	[Table("User")]
	public class User
	{
		[Column("id", IsPrimaryKey = true, IsIdentity = true)] public long    Id          { get; set; }
		[Column("name")]                                       public string? Name        { get; set; }
		[Column("login_count")]                                public int?    Login_count { get; set; }
	}
}
