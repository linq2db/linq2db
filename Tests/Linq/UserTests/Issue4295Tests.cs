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
		public void TestGuidToString([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				var a = new InfeedAdvicePositionDTO() { Id = Guid.Parse("193AE7F4-5309-4EEE-A746-27B28C7E30F3") };
				db.Insert(a);
				
				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().ToLower().Contains("7f4-53")
						   select infeed;

				var lA = qryA.ToList();
				Assert.AreEqual(1, lA.Count);

				var qryB = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().ToLower().StartsWith("193ae")
						   select infeed;

				var lB = qryB.ToList();
				Assert.AreEqual(1, lB.Count);

				var qryC = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().ToLower().Contains("8f4-53")
						   select infeed;

				var lC = qryC.ToList();
				Assert.AreEqual(0, lC.Count);

				var qryD = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where infeed.Id.ToString().ToLower().StartsWith("293ae")
						   select infeed;

				var lD = qryD.ToList();
				Assert.AreEqual(0, lD.Count);
			}
		}
	}
}
