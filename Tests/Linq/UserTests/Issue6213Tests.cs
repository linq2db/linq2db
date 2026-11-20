using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue6213Tests : TestBase
	{
		[Table]
		sealed class Rate
		{
			[PrimaryKey] public int     Id  { get; set; }
			[Column]     public decimal Usd { get; set; }
		}

		static readonly Rate[] Data =
		[
			new() { Id = 1, Usd = 10m },
			new() { Id = 2, Usd = 30m },
		];

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6213")]
		public void SelectInAnonymousType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Data);

			var result = t
				.Select(x => new { x.Id, Usd = Sql.Property<decimal?>(x, "Usd") })
				.OrderBy(x => x.Id)
				.ToArray();

			result.Select(x => x.Usd).ShouldBe([10m, 30m]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6213")]
		public void SelectScalar([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Data);

			var result = t
				.Where(x => x.Id == 2)
				.Select(x => Sql.Property<decimal?>(x, "Usd"))
				.Single();

			result.ShouldBe(30m);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6213")]
		public void Where([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Data);

			var result = t
				.Where(x => Sql.Property<decimal?>(x, "Usd") > 20)
				.Select(x => x.Id)
				.ToArray();

			result.ShouldBe([2]);
		}
	}
}
