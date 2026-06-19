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

		// A nullable member that is left unbound (never assigned) in one projection and then
		// consumed through Nullable<T>.HasValue in a later projection. Building used to throw
		// "InvalidOperationException: Called when root is not initialized." — see #5575.
		sealed class StatEntity
		{
			public int  Id        { get; set; }
			public int? LeadCount { get; set; }
		}

		// Reported shape: HasValue inside a conditional projection.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5575")]
		public void ConditionalNullableHasValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new SomeTable { Id = 1, Value = 4 } });

			var query = tb
				.Select(c => new StatEntity { Id = c.Id }) // LeadCount left unbound
				.Select(s => new
				{
					s.Id,
					Rate = s.LeadCount.HasValue ? (decimal?)(s.LeadCount.Value / 2m) : null
				});

			AssertQuery(query);
		}

		// Minimal trigger: HasValue on the unbound member, without a conditional.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5575")]
		public void UnboundNullableHasValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new SomeTable { Id = 1, Value = 4 } });

			var query = tb
				.Select(c => new StatEntity { Id = c.Id }) // LeadCount left unbound
				.Select(s => new { s.Id, Has = s.LeadCount.HasValue });

			AssertQuery(query);
		}
	}
}
