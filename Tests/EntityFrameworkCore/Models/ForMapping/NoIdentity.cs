using System;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping
{
	public class NoIdentity
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = null!;
	}
}
