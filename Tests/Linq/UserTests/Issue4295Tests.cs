using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4295Tests : TestBase
	{
		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column] public Guid Id { get; set; }
		}

		[Test]
		public void TestGuidToString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				var expected = "193AE7F4-5309-4EEE-A746-27B28C7E30F3".ToLowerInvariant();

				var a = new InfeedAdvicePositionDTO() { Id = Guid.Parse(expected) };
				db.Insert(a);

				var id = (from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   select infeed.Id.ToString()).First();

				Assert.That(id, Is.EqualTo(expected));

				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().Contains("7f4-53")
						   select infeed;

				var lA = qryA.ToList();
				Assert.That(lA, Has.Count.EqualTo(1));

				var qryB = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().StartsWith("193ae")
						   select infeed;

				var lB = qryB.ToList();
				Assert.That(lB, Has.Count.EqualTo(1));


				var qryC = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().Contains("8f4-53")
						   select infeed;

				var lC = qryC.ToList();
				Assert.That(lC, Has.Count.EqualTo(0));

				var qryD = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().ToLower().StartsWith("293ae")
						   select infeed;

				var lD = qryD.ToList();
				Assert.That(lD, Has.Count.EqualTo(0));
			}
		}
	}
}
