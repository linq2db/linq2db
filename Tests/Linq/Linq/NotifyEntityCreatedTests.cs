using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class NotifyEntityCreatedTests : TestBase
	{
		class EntityCreatedDataContext : TestDataConnection//, INotifyEntityCreated
		{
			public EntityCreatedDataContext(string configString)
				: base(configString)
			{
				OnEntityCreated = EntityCreated;
			}

			void EntityCreated(EntityCreatedEventArgs args)
			{
				if (CheckEntityIdentity && args.Entity is Parent p)
				{
					if (_parents.TryGetValue(p.ParentID, out var pr))
					{
						args.Entity = pr;
						return;
					}

					_parents[p.ParentID] = p;
				}

				EntitiesCreated++;
			}

			public int  EntitiesCreated;
			public bool CheckEntityIdentity;

			Dictionary<int,Parent> _parents = new Dictionary<int,Parent>();

			public object EntityCreated(object entity)
			{
				if (CheckEntityIdentity && entity is Parent p)
				{
					if (_parents.TryGetValue(p.ParentID, out var pr))
						return pr;
					_parents[p.ParentID] = p;
				}

				EntitiesCreated++;

				return entity;
			}
		}

		[Test]
		public void EntityCreatedTest0([DataSources(false)] string configString)
		{
			using (var db = new TestDataConnection(configString))
			{
				var list = db.Parent.Take(5).ToList();
			}
		}

		[Test]
		public void EntityCreatedTest1([DataSources(false)] string configString)
		{
			using (var db = new EntityCreatedDataContext(configString))
			{
				var list = db.Parent.Take(5).ToList();

				Assert.That(db.EntitiesCreated, Is.EqualTo(5));
			}
		}

		[Test]
		public void EntityCreatedTest2([DataSources(false)] string configString)
		{
			using (var db = new EntityCreatedDataContext(configString))
			{
				var list = db.Child.Select(c => new { c, c.Parent, a = new { c } }).Take(1).ToList();

				Assert.That(db.EntitiesCreated, Is.EqualTo(2));
			}
		}

		[Test]
		public void EntityCreatedTest3([DataSources(false)] string configString, [Values(false,true)] bool checkEntityIdentity)
		{
			using (var db = new EntityCreatedDataContext(configString) { CheckEntityIdentity = checkEntityIdentity })
			{
				var list = db.Child.Where(c => c.Parent.ParentID == 3).Select(c => c.Parent).ToList();

				Assert.That(db.EntitiesCreated, Is.EqualTo(checkEntityIdentity ? 1 : 3));
			}
		}
	}
}
