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
		public void Test([IncludeDataSources(TestProvName.AllOracle)] string context)
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

				Assert.AreEqual(2         , data.Length);
				Assert.AreEqual(1         , data[0].Id);
				Assert.AreEqual(123.456m  , data[0].Decimal1);
				Assert.AreEqual(0m        , data[0].Decimal2);
				Assert.AreEqual(0.1m      , data[0].Decimal3);
				Assert.AreEqual(0         , data[0].Int1);
				Assert.AreEqual(22        , data[0].Int2);
				Assert.AreEqual(2         , data[1].Id);
				Assert.AreEqual(-123.456m , data[1].Decimal1);
				Assert.AreEqual(678.903m  , data[1].Decimal2);
				Assert.AreEqual(3523.2352m, data[1].Decimal3);
				Assert.AreEqual(-123      , data[1].Int1);
				Assert.AreEqual(345       , data[1].Int2);
			}
		}
	}
}
