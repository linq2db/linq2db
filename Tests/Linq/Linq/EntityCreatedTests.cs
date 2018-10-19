using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture, Parallelizable(ParallelScope.None)]
	public class EntityCreatedTests : TestBase
	{
		int  _entitiesCreated;
		bool _checkEntityIdentity;

		readonly Dictionary<int,Parent> _parents = new Dictionary<int,Parent>();

		ITestDataContext GetEntityCreatedContext(string configString)
		{
			_entitiesCreated     = 0;
			_checkEntityIdentity = false;
			_parents.Clear();

			var ctx = GetDataContext(configString);

			((IEntityServices)ctx).OnEntityCreated += EntityCreated;

			return ctx;
		}

		void EntityCreated(EntityCreatedEventArgs args)
		{
			if (_checkEntityIdentity && args.Entity is Parent p)
			{
				if (_parents.TryGetValue(p.ParentID, out var pr))
				{
					args.Entity = pr;
					return;
				}

				_parents[p.ParentID] = p;
			}

			_entitiesCreated++;
		}

		[Test, Combinatorial]
		public void EntityCreatedTest0([DataSources] string configString)
		{
			using (var db = GetDataContext(configString))
			{
				var list = db.Parent.Take(5).ToList();
			}
		}

		[Test, Combinatorial]
		public void EntityCreatedTest1([DataSources] string configString)
		{
			using (var db = GetEntityCreatedContext(configString))
			{
				var list = db.Parent.Take(5).ToList();

				Assert.That(_entitiesCreated, Is.EqualTo(5));
			}
		}

		[Test, Combinatorial]
		public void EntityCreatedTest2([DataSources] string configString)
		{
			using (var db = GetEntityCreatedContext(configString))
			{
				var list = db.Child.Select(c => new { c, c.Parent, a = new { c } }).Take(1).ToList();

				Assert.That(_entitiesCreated, Is.EqualTo(2));
			}
		}

		[Test, Combinatorial]
		public void EntityCreatedTest3([DataSources] string configString, [Values(false,true)] bool checkEntityIdentity)
		{
			using (var db = GetEntityCreatedContext(configString))
			{
				_checkEntityIdentity = checkEntityIdentity;

				var list = db.Child.Where(c => c.Parent.ParentID == 3).Select(c => c.Parent).ToList();

				Assert.That(_entitiesCreated, Is.EqualTo(checkEntityIdentity ? 1 : 3));
			}
		}
	}
}
