using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5575Tests : TestBase
	{
		[Table]
		sealed class SomeTable
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int Value { get; set; }
		}

		sealed class Counts
		{
			public int Id    { get; set; }
			public int Count { get; set; }
		}

		// Canonical repro from https://github.com/linq2db/linq2db/issues/5575
		// (does not currently reproduce on master across SQLite / SQL Server — see PR notes).
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5575")]
		public void ConditionalNullableHasValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new SomeTable { Id = 1, Value = 4 } });

			var counts = new[] { new Counts { Id = 1, Count = 5 } };

			var query =
				from t in tb
				from c in counts.AsQueryable()
								.Where(c => c.Id == t.Id)
								.DefaultIfEmpty()
				select new
				{
					t.Id,
					Rate = ((int?)c.Count).HasValue
						? (decimal?)(((int?)c.Count).Value / (decimal)t.Value * 100)
						: null
				};

			query.ToList();
		}
	}
}
