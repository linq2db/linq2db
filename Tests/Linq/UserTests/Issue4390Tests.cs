using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

using System;

using FluentAssertions;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4390Tests : TestBase
	{
		[Table]
		public class InfeedAdviceDTO
		{
			[Column] public int Id { get; set; }
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column] public int InfeedAdviceID { get; set; }
		}
		public class MlogCombined1
		{
			public InfeedAdviceDTO? InfeedAdvice { get; set; }

			public int? Count { get; set; }
		}

		public class MlogCombined2
		{
			public MlogCombined1? MlogCombined1 { get; set; }
		}

		[Test]
		public void Issue4390Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdviceDTO>())
			using (db.CreateLocalTable<InventoryResourceDTO>())
			{
				db.Insert(new InventoryResourceDTO() { InfeedAdviceID = 1 });
				db.Insert(new InfeedAdviceDTO() { Id = 1 });

				var irs = from ir in db.GetTable<InventoryResourceDTO>() select ir;

				var qry = from infeed in db.GetTable<InfeedAdviceDTO>()
								join inventory in db.GetTable<InventoryResourceDTO>() on infeed.Id equals inventory.InfeedAdviceID
								select new MlogCombined2
								{
									MlogCombined1 = new MlogCombined1
									{
										InfeedAdvice = infeed,
										Count = irs.Count(x => x.InfeedAdviceID == infeed.Id),
									}
								};

				var count = qry.Where(x => x.MlogCombined1 != null).Count();

				count.Should().Be(1);
			}
		}
	}
}
