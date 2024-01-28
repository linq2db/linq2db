using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Collections.Generic;
using System;
using LinqToDB.Data;

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
				var expected = "193AE7F4-5309-4EEE-A746-27B28C7E30F3";

				var a = new InfeedAdvicePositionDTO() { Id = Guid.Parse(expected) };
				db.Insert(a);

				var id = (from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   select Sql.GuidToNormalizedString(infeed.Id)).First();

				Assert.AreEqual(expected, id);

				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where Sql.GuidToNormalizedString(infeed.Id)!.Contains("7F4-53")
						   select infeed;

				var lA = qryA.ToList();
				Assert.AreEqual(1, lA.Count);

				var qryB = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where Sql.GuidToNormalizedString(infeed.Id)!.StartsWith("193AE")
						   select infeed;

				var lB = qryB.ToList();
				Assert.AreEqual(1, lB.Count);


				var qryC = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where Sql.GuidToNormalizedString(infeed.Id)!.Contains("8F4-53")
						   select infeed;

				var lC = qryC.ToList();
				Assert.AreEqual(0, lC.Count);

				var qryD = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where Sql.GuidToNormalizedString(infeed.Id)!.ToLower().StartsWith("293AE")
						   select infeed;

				var lD = qryD.ToList();
				Assert.AreEqual(0, lD.Count);
			}
		}
	}
}
