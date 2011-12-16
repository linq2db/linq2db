using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ComplexTest : TestBase
	{
		[Test]
		public void Contains1()
		{
			var q1 =
				from gc1 in GrandChild
					join max in
						from gch in GrandChild
						group gch by gch.ChildID into g
						select g.Max(c => c.GrandChildID)
					on gc1.GrandChildID equals max
				select gc1;

			var expected =
				from ch in Child
					join p   in Parent on ch.ParentID equals p.ParentID
					join gc2 in q1     on p.ParentID  equals gc2.ParentID into g
					from gc3 in g.DefaultIfEmpty()
				where gc3 == null || !new[] { 111, 222 }.Contains(gc3.GrandChildID.Value)
				select new { p.ParentID, gc3 };

			ForEachProvider(new [] { ProviderName.Firebird, ProviderName.Access }, db =>
			{
				var q2 =
					from gc1 in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var result =
					from ch in db.Child
						join p   in db.Parent on ch.ParentID equals p.ParentID
						join gc2 in q2        on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
				where gc3 == null || !new[] { 111, 222 }.Contains(gc3.GrandChildID.Value)
				select new { p.ParentID, gc3 };

				AreEqual(expected, result);
			});
		}

		[Test]
		public void Contains2()
		{
			var q1 =
				from gc in GrandChild
					join max in
						from gch in GrandChild
						group gch by gch.ChildID into g
						select g.Max(c => c.GrandChildID)
					on gc.GrandChildID equals max
				select gc;

			var expected =
				from ch in Child
					join p  in Parent on ch.ParentID equals p.ParentID
					join gc in q1     on p.ParentID  equals gc.ParentID into g
					from gc in g.DefaultIfEmpty()
				where gc == null || gc.GrandChildID != 111 && gc.GrandChildID != 222
				select new
				{
					Parent       = p,
					GrandChildID = gc,
					Value        = GetValue(gc != null ? gc.ChildID : int.MaxValue)
				};

			ForEachProvider(new [] { ProviderName.Firebird, ProviderName.Access }, db =>
			{
				var q2 =
					from gc in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join p  in db.Parent on ch.ParentID equals p.ParentID
						join gc in q2        on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
				where gc == null || gc.GrandChildID != 111 && gc.GrandChildID != 222
				select new
				{
					Parent       = p,
					GrandChildID = gc,
					Value        = GetValue(gc != null ? gc.ChildID : int.MaxValue)
				};

				AreEqual(expected, result);
			});
		}

		static int GetValue(int? value)
		{
			return value ?? 777;
		}

		[Test]
		public void Contains3()
		{
			var q1 =
				from gc in GrandChild1
					join max in
						from gch in GrandChild1
						group gch by gch.ChildID into g
						select g.Max(c => c.GrandChildID)
					on gc.GrandChildID equals max
				select gc;

			var expected =
				from ch in Child
					join p  in Parent on ch.ParentID equals p.ParentID
					join gc in q1     on p.ParentID  equals gc.ParentID into g
					from gc in g.DefaultIfEmpty()
				where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID.Value)
				select new { p.ParentID, gc };

			ForEachProvider(new [] { ProviderName.SQLite, ProviderName.Access }, db =>
			{
				var q2 =
					from gc in db.GrandChild1
						join max in
							from gch in db.GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join p  in db.Parent on ch.ParentID equals p.ParentID
						join gc in q2     on p.ParentID  equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
					where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID.Value)
					select new { p.ParentID, gc };

				AreEqual(expected, result);
			});
		}

		[Test]
		public void Contains4()
		{
			var q1 =
				from gc in GrandChild1
					join max in
						from gch in GrandChild1
						group gch by gch.ChildID into g
						select g.Max(c => c.GrandChildID)
					on gc.GrandChildID equals max
				select gc;

			var expected =
				from ch in Child
					join gc in q1 on ch.Parent.ParentID equals gc.ParentID into g
					from gc in g.DefaultIfEmpty()
				where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID.Value)
				select new { ch.Parent, gc };

			ForEachProvider(new [] { ProviderName.SQLite, ProviderName.Access }, db =>
			{
				var q2 =
					from gc in db.GrandChild1
						join max in
							from gch in db.GrandChild1
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc.GrandChildID equals max
					select gc;

				var result =
					from ch in db.Child
						join gc in q2 on ch.Parent.ParentID equals gc.ParentID into g
						from gc in g.DefaultIfEmpty()
				where gc == null || !new[] { 111, 222 }.Contains(gc.GrandChildID.Value)
				select new { ch.Parent, gc };

				AreEqual(expected, result);
			});
		}

		[Test]
		public void Join1()
		{
			var q1 =
				from p in Parent
					join c in Child      on p.ParentID equals c.ParentID
					join g in GrandChild on p.ParentID equals g.ParentID
				select new { p, c, g };

			var expected =
				from x in q1
				where
				(
					(x.c.ParentID == 2 || x.c.ParentID == 3) && x.g.ChildID != 21 && x.g.ChildID != 33
				) || (
					x.g.ParentID == 3 && x.g.ChildID == 32
				) || (
					x.g.ChildID == 11
				)
				select x;

			ForEachProvider(db =>
			{
				var q2 =
					from p in db.Parent
						join c in db.Child      on p.ParentID equals c.ParentID
						join g in db.GrandChild on p.ParentID equals g.ParentID
					select new { p, c, g };

				var result =
					from x in q2
					where
					(
						(x.c.ParentID == 2 || x.c.ParentID == 3) && x.g.ChildID != 21 && x.g.ChildID != 33
					) || (
						x.g.ParentID == 3 && x.g.ChildID == 32
					) || (
						x.g.ChildID == 11
					)
					select x;

					AreEqual(expected, result);
			});
		}

		public class MyObject
		{
			public Parent Parent;
			public Child  Child;
		}

		IQueryable<MyObject> GetData(ITestDataContext db, int id)
		{
			var q =
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ChildID
				where p.ParentID == id && c.ChildID > 0
				select new MyObject { Parent = p, Child = c };

			return q;
		}

		[Test]
		public void Join2()
		{
			ForEachProvider(db =>
			{
				var q =
					from o in GetData(db, 1)
					from g in o.Parent.GrandChildren
					select new { o, g };

				var list = q.ToList();
			});
		}

		[Test]
		public void ExpressionTest1()
		{
			Expression<Func<Northwind.Customer,bool>> pred1 = cust=>cust.Country=="UK";
			Expression<Func<Northwind.Customer,bool>> pred2 = cust=>cust.Country=="France";

			var param = Expression.Parameter(typeof(Northwind.Customer), "x");
			var final = Expression.Lambda<Func<Northwind.Customer, bool>>(
				Expression.OrElse(
					Expression.Invoke(pred1, param),
					Expression.Invoke(pred2, param)
				), param);

			using (var db = new NorthwindDB())
			{
				var count = db.Customer.Count(final);
			}
		}

		[Test]
		public void ExpressionTest2()
		{
			Expression<Func<Parent,bool>> pred1 = _=>_.ParentID == 1;
			Expression<Func<Parent,bool>> pred2 = _=>_.Value1   == 1 || _.Value1 == null;

			var param = Expression.Parameter(typeof(Parent), "x");
			var final = Expression.Lambda<Func<Parent, bool>>(
				Expression.AndAlso(
					Expression.Invoke(pred1, param),
					Expression.Invoke(pred2, param)
				), param);

			using (var db = new TestDbManager())
			{
				Assert.AreEqual(1, db.Parent.Count(final));
			}
		}

		#region IEnumerableTest

		public class Entity
		{
			public int Id { get; set; }
		}

		public enum TestEntityType : byte { Type1, Type2 }

		[TableName("GrandChild")]
		[MapField("GrandChildID", "Id")]
		[MapField("ChildID", "InnerEnity.Id")]
		[MapField("ParentID", "InnerEntityType")]
		public class LookupEntity : Entity
		{
			public Entity         InnerEnity      { get; set; }
			public TestEntityType InnerEntityType { get; set; }
		}

		[TableName("GrandChild")]
		[MapField("GrandChildID", "Id")]
		[MapField("ChildID",   "Owner.Id")]
		[MapField("ParentID", "EntityType")]
		public class TestEntityBase : Entity
		{
			public TestEntityType EntityType { get; set; }
			public SuperAccount   Owner      { get; set; }
		}

		public class TestEntity : TestEntityBase, IEnumerable<object>
		{
			#region IEnumerable<object> Members

			public IEnumerator<object> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		public class TestEntity2 : TestEntityBase
		{
		}

		public enum SuperAccountType { Client, Organization }
	    
		[TableName("GrandChild")]
		[MapField("GrandChildID", "Id")]
		[MapField("ParentID", "Type")]
		public class SuperAccount : Entity, IEnumerable<object>
		{
			public List<Entity>     InnerAccounts { get; set; }
			public SuperAccountType Type          { get; set; }


			#region IEnumerable<object> Members

			public IEnumerator<object> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		[Test]
		public void IEnumerableTest1()
		{
			using (var db = new TestDbManager())
			{
				var res =
					from rc in db.GetTable<TestEntity>()
					join li in db.GetTable<LookupEntity>() on rc.Id equals li.InnerEnity.Id
					where rc.EntityType == TestEntityType.Type1
					select rc;

				res.ToList();
			}
		}

		[Test]
		public void IEnumerableTest2()
		{
			using (var db = new TestDbManager())
			{
				var zones =
					from z in db.GetTable<TestEntity2>()
					join o in db.GetTable<SuperAccount>() on z.Owner.Id equals o.Id
					select z;

				zones.ToList();
			}
		}

		#endregion
	}
}
