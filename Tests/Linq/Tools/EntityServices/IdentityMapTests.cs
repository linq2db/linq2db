using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Tools.EntityServices;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Tools.EntityServices
{
	[TestFixture]
	public class IdentityMapTests : TestBase
	{
		[Test]
		public void IdentityTest()
		{
			using var db = new TestDataConnection();
			using var map = new IdentityMap(db);
			var p1 = db.Person.First(p => p.ID == 1);
			var p2 = db.Person.First(p => p.ID == 1);
			var p3 = db.Person.First(p => p.ID == 2);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(p2, Is.SameAs(p1));

				Assert.That(
					map.GetEntityEntries<Person>().Select(ee => new { ee.Entity, StoreCount = ee.DBCount, ee.CacheCount }),
					Is.EquivalentTo(new[]
					{
						new { Entity = p1, StoreCount = 2, CacheCount = 0 },
						new { Entity = p3, StoreCount = 1, CacheCount = 0 },
					}));
			}

			var c1 = db.Child.First(p => p.ParentID == 1 && p.ChildID == 11);
			var c2 = db.Child.First(p => p.ParentID == 1 && p.ChildID == 11);
			var c3 = db.Child.First(p => p.ParentID == 2 && p.ChildID == 21);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c2, Is.SameAs(c1));

				Assert.That(
					map.GetEntityMap<Child>().Entities?.Select(ee => new { ee.Value.Entity, StoreCount = ee.Value.DBCount, ee.Value.CacheCount }),
					Is.EquivalentTo(new[]
					{
						new { Entity = c1, StoreCount = 2, CacheCount = 0 },
						new { Entity = c3, StoreCount = 1, CacheCount = 0 },
					}));
			}
		}

		[Test]
		public void GetEntityTest()
		{
			using var db = new TestDataConnection();
			using var map = new IdentityMap(db);
			var p1 = db.Person.First(p => p.ID == 1);
			var p2 = map.GetEntity<Person>(1);
			var p3 = map.GetEntity<Person>(new { ID = 1 });
			using (Assert.EnterMultipleScope())
			{
				Assert.That(p2, Is.SameAs(p1));
				Assert.That(p3, Is.SameAs(p1));
			}

			var p4 = map.GetEntity<Person>(2)!;
			var p5 = map.GetEntity<Person>(new { ID = 3L })!;

			Assert.That(
				map.GetEntityEntries<Person>().Select(ee => new { ee.Entity, StoreCount = ee.DBCount, ee.CacheCount }),
				Is.EquivalentTo(new[]
				{
						new { Entity = p1, StoreCount = 1, CacheCount = 2 },
						new { Entity = p4, StoreCount = 1, CacheCount = 0 },
						new { Entity = p5, StoreCount = 1, CacheCount = 0 },
				}));

			var c1 = map.GetEntity<Child>(new { ParentID = 1, ChildID = 11 });

			Assert.That(
				map.GetEntityMap<Child>().Entities?.Values.Select(ee => new { ee.Entity, StoreCount = ee.DBCount, ee.CacheCount }),
				Is.EquivalentTo(new[]
				{
						new { Entity = c1, StoreCount = 1, CacheCount = 0 },
				}));
		}

		[Test]
		public void NegativeTest()
		{
			using var db = new DataConnection();
			using var map = new IdentityMap(db);
			Assert.Throws<LinqToDBConvertException>(() => map.GetEntity<Person>(new { ID1 = 1 }));
		}

		[Test]
		public async Task CompiledQueryTest()
		{
			await using var db  = new TestDataConnection();
			using       var map = new IdentityMap(db);

			var query = CompiledQuery.Compile<TestDataConnection,CancellationToken,Task<List<Person>>>(static (db, ct) => db.Person.Where(p => p.ID == 1).ToListAsync(ct));

			var result1 = await query(db, default);
			var result2 = await query(db, default);

			Assert.That(result1[0], Is.SameAs(result2[0]));
		}
	}
}
