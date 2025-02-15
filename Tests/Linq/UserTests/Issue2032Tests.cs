using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2032Tests : TestBase
	{
		[Table]
		public class Issue2032Table
		{
			[Column]                            public int      Id       { get; set; }
			[Column(Precision = 10, Scale = 4)] public decimal  Decimal1 { get; set; }
			[Column(Precision = 10, Scale = 4)] public decimal? Decimal2 { get; set; }
			[Column(Precision = 10, Scale = 4)] public decimal? Decimal3 { get; set; }
			[Column]                            public int?     Int1     { get; set; }
			[Column]                            public int?     Int2     { get; set; }

			public static readonly Issue2032Table[] Data = new[]
			{
				new Issue2032Table() { Id = 1, Decimal1 = 123.456m },
				new Issue2032Table() { Id = 2, Decimal1 = -123.456m, Decimal2 = 678.903m, Decimal3 = 3523.2352m, Int1 = -123, Int2 = 345 },
			};
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllOracle, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Issue2032Table.Data))
			{
				var data = table
					.OrderBy(r => r.Id)
					.Select(r => new 
					{
						r.Id,
						r.Decimal1,
						Decimal2 = r.Decimal2.GetValueOrDefault(),
						Decimal3 = r.Decimal3 ?? 0.1m,
						Int1     = r.Int1.GetValueOrDefault(),
						Int2     = r.Int2 ?? 22
					})
					.ToArray();

				Assert.That(data, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Decimal1, Is.EqualTo(123.456m));
					Assert.That(data[0].Decimal2, Is.EqualTo(0m));
					Assert.That(data[0].Decimal3, Is.EqualTo(0.1m));
					Assert.That(data[0].Int1, Is.EqualTo(0));
					Assert.That(data[0].Int2, Is.EqualTo(22));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Decimal1, Is.EqualTo(-123.456m));
					Assert.That(data[1].Decimal2, Is.EqualTo(678.903m));
					Assert.That(data[1].Decimal3, Is.EqualTo(3523.2352m));
					Assert.That(data[1].Int1, Is.EqualTo(-123));
					Assert.That(data[1].Int2, Is.EqualTo(345));
				});
			}
		}
	}
}
