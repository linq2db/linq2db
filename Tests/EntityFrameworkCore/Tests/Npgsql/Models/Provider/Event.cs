using System;

using NpgsqlTypes;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models
{
	public class Event
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public NpgsqlRange<DateTime> Duration { get; set; }
	}
}
