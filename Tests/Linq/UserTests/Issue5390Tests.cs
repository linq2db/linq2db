using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5390Tests : TestBase
	{
		[Table("Order5390")]
		public class Order
		{
			[Column] public int      Id            { get; set; }
			[Column] public DateTime CreatedOnUtc  { get; set; }
		}

		[Test]
		public void GroupByDatePropertyTest([DataSources] string context)
		{
			var data = new[]
			{
				new Order { Id = 1, CreatedOnUtc = new DateTime(2026, 2, 24, 10, 30, 0) },
				new Order { Id = 2, CreatedOnUtc = new DateTime(2026, 2, 24, 14, 0, 0) },
				new Order { Id = 3, CreatedOnUtc = new DateTime(2026, 2, 25, 9, 0, 0) },
				new Order { Id = 4, CreatedOnUtc = new DateTime(2026, 3, 1, 12, 0, 0) },
				new Order { Id = 5, CreatedOnUtc = new DateTime(2026, 3, 1, 18, 0, 0) },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from o in table
				group o by new { o.CreatedOnUtc.Date } into g
				select new
				{
					Date  = g.Key.Date,
					Count = g.Count()
				};

			AssertQuery(query);
		}

		[Test]
		public void GroupByDatePropertyWithWhereTest([DataSources] string context)
		{
			var data = new[]
			{
				new Order { Id = 1, CreatedOnUtc = new DateTime(2026, 2, 24, 10, 30, 0) },
				new Order { Id = 2, CreatedOnUtc = new DateTime(2026, 2, 24, 14, 0, 0) },
				new Order { Id = 3, CreatedOnUtc = new DateTime(2026, 2, 25, 9, 0, 0) },
				new Order { Id = 4, CreatedOnUtc = new DateTime(2026, 3, 1, 12, 0, 0) },
				new Order { Id = 5, CreatedOnUtc = new DateTime(2026, 3, 1, 18, 0, 0) },
			};

			var createdFromUtc = new DateTime(2026, 2, 24);
			var createdToUtc   = new DateTime(2026, 3, 3);

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from o in table
				where createdFromUtc <= o.CreatedOnUtc && createdToUtc >= o.CreatedOnUtc
				group o by new { o.CreatedOnUtc.Date } into g
				select new
				{
					Date  = g.Key.Date,
					Count = g.Count()
				};

			AssertQuery(query);
		}

		[Test]
		public void GroupByDatePropertyDirectTest([DataSources] string context)
		{
			var data = new[]
			{
				new Order { Id = 1, CreatedOnUtc = new DateTime(2026, 2, 24, 10, 30, 0) },
				new Order { Id = 2, CreatedOnUtc = new DateTime(2026, 2, 24, 14, 0, 0) },
				new Order { Id = 3, CreatedOnUtc = new DateTime(2026, 2, 25, 9, 0, 0) },
				new Order { Id = 4, CreatedOnUtc = new DateTime(2026, 3, 1, 12, 0, 0) },
				new Order { Id = 5, CreatedOnUtc = new DateTime(2026, 3, 1, 18, 0, 0) },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from o in table
				group o by o.CreatedOnUtc.Date into g
				select new
				{
					Date  = g.Key,
					Count = g.Count()
				};

			AssertQuery(query);
		}
	}
}
