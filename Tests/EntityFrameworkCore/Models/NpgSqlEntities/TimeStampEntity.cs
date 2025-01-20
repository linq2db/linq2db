using System;

using NodaTime;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.NpgSqlEntities
{
	public class TimeStampEntity
	{
		public int            Id           { get; set; }
		public DateTime       Timestamp1   { get; set; }
		public LocalDateTime  Timestamp2   { get; set; }
		public DateTime       TimestampTZ1 { get; set; }
		public DateTimeOffset TimestampTZ2 { get; set; }
		public Instant        TimestampTZ3 { get; set; }
	}
}
