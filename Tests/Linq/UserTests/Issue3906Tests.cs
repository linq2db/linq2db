using System.Linq;
using System.Linq.Dynamic.Core;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3906Tests : TestBase
	{
		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column] public int Id { get; set; }
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column] public int InfeedAdviceID { get; set; }
			[Column] public int Quantity { get; set; }
		}

		[Table]
		public class MlogInfeedAddonsDTO
		{
			[Column] public int Id { get; set; }

			[Column] public int Nr { get; set; }
		}

		[Test]
		public void QueryNotPossible([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			using (db.CreateLocalTable<InventoryResourceDTO>())
			using (db.CreateLocalTable<MlogInfeedAddonsDTO>())
			{
				db.Insert(new InfeedAdvicePositionDTO() { Id = 1 });
				db.Insert(new InventoryResourceDTO() { InfeedAdviceID = 1, Quantity = 9 });
				db.Insert(new MlogInfeedAddonsDTO() { Id = 1, Nr = 77 });
				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   select new
						   {
							   InfeedAdvicePosition = infeed,
							   CurrentQuantity = db.GetTable<InventoryResourceDTO>().Where(x => x.InfeedAdviceID == infeed.Id).Sum(x => x.Quantity),
						   };

				var qryB = from oto in qryA
						   select new
						   {
							   oto,
							   InfeedAddons =
								   (from ir in db.GetTable<MlogInfeedAddonsDTO>()
									where ir.Id == oto.InfeedAdvicePosition.Id
									select ir).DefaultIfEmpty().FirstOrDefault(),
						   };

				var l = qryB.First();
			}
		}
	}
}
