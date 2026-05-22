using System;
using System.Linq;
using System.Linq.Dynamic.Core;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3506Tests : TestBase
	{
		[Test]
		public void Test1([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var cats = new[]
			{
				new { OwnerId = (int?)1 }
			};

			var owners = new[]
			{
				new { Id = 1 }
			};

			using var db = GetDataContext(context);
			using var catsTable = db.CreateLocalTable("catz", cats);
			using var ownerTable = db.CreateLocalTable("owners", owners);
			var result = catsTable
					.InnerJoin(
					ownerTable,
					(cat, owner) =>
						cat.OwnerId.HasValue
						// .Value
						&& cat.OwnerId.Value == owner.Id,
					(cat, owner) => cat)
				.Count();

			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void Test2([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var cats = new[]
			{
				new { OwnerId = (int?)1 }
			};

			var owners = new[]
			{
				new { Id = 1 }
			};

			using var db = GetDataContext(context);
			using var catsTable = db.CreateLocalTable("catz", cats);
			using var ownerTable = db.CreateLocalTable("owners", owners);
			var result = catsTable
					.InnerJoin(
					ownerTable,
					(cat, owner) =>
						cat.OwnerId.HasValue
						// no .Value
						&& cat.OwnerId == owner.Id,
					(cat, owner) => cat)
				.Count();

			Assert.That(result, Is.EqualTo(1));
		}
	}
}
