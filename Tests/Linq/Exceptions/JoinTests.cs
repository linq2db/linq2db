using System;
using System.Linq;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class JoinTests : TestBase
	{
		[Table("Parent", IsColumnAttributeRequired = false)]
		public class Parents
		{
			[PrimaryKey]
			public int ParentID;
			public int Value;

			[Association(ThisKey ="ParentID", OtherKey = "ParentID")]
			public IEnumerable<Childs> Childs;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		public class Childs
		{
			[PrimaryKey]
			public int ChildID;
			public int ParentID;

			[Association(ThisKey ="ParentID", OtherKey = "ParentID")]
			public Parents Parent;
		}

		[Table("GrandChild", IsColumnAttributeRequired = false)]
		public class GrandChilds
		{
			public int? ChildID;
			public int? ParentID;
			public int? GrandChildID;

			[Association(ThisKey = "ChildID", OtherKey = "ChildID")]
			public Child Child;
		}
		[Test, DataContextSource]
		public void InnerJoin(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p1 in db.Person
						join p2 in db.Person on new Person { FirstName = "", ID = p1.ID } equals new Person { ID = p2.ID }
					where p1.ID == 1
					select new Person { ID = p1.ID, FirstName = p2.FirstName };

				Assert.Throws(typeof(LinqException), () => q.ToList());
			}
		}

		[Test, DataContextSource]
		public void MultiJoin1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p  in Parent
							   join c1 in Child on p.ParentID equals c1.ParentID
							   join c2 in Child on p.ParentID equals c2.ParentID
							   select new Child()
							   {
							   	ChildID = c1.ChildID,
							   	ParentID = c2.ParentID
							   };

				var result = from p  in db.GetTable<Parents>()
							 join c1 in db.GetTable<Childs>() on p.ParentID equals c1.ParentID
							 join c2 in db.GetTable<Childs>() on p.ParentID equals c2.ParentID
							 select new Child()
							 {
							 	ChildID  = c1.ChildID,
							 	ParentID = c2.ParentID
							 };

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void MultiJoin2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from c in Child
							   join p1 in Parent on c.ParentID equals p1.ParentID
							   select c;

				var result = from c  in db.GetTable<Childs>()
							 join p1 in db.GetTable<Parents>() on c.ParentID equals p1.ParentID
							 join p2 in db.GetTable<Parents>() on c.ParentID equals p2.ParentID
							 select
							 new Child()
							 {
								 ChildID  = c.ChildID,
								 ParentID = c.ParentID
							 };

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Issue498Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected1 = from x in Parent
							    from y in x.Children
							    select x.ParentID;

				var result1 = from x in db.GetTable<Parents>()
							  from y in x.Childs
							  select x.ParentID;

				AreEqual(expected1, result1);

				var expected2 = from  x in expected1
							    group x by x into g
							    select g.Key;

				var result2 = from  x in result1
							  group x by x into g
							  select g.Key;

				AreEqual(expected2, result2);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlCe, ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue589(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result =
					from grandChild in db.GetTable<GrandChilds>()
					join child      in db.GetTable<Childs>() on grandChild.ChildID equals child.ChildID
					from pf in
						(
							from child1      in db.GetTable<Childs>()
							join parent1     in db.GetTable<Parents>()     on child1.ParentID  equals parent1.ParentID
							join grandChild1 in db.GetTable<GrandChilds>() on parent1.ParentID equals grandChild1.Child.ParentID
							where grandChild1.ParentID == child.Parent.ParentID
							select grandChild1
					).Take(1).DefaultIfEmpty()
					select new
					{
						GrandChildID   = grandChild.GrandChildID,
						ChildID        = child.ChildID,
						ParentParentId = child.Parent.ParentID,
						Tmp            = pf.GrandChildID
					};

				var expected =
					from grandChild in GrandChild
					join child      in Child on grandChild.ChildID equals child.ChildID
					from pf in
						(
							from child1      in Child
							join parent1     in Parent     on child1.ParentID  equals parent1.ParentID
							join grandChild1 in GrandChild on parent1.ParentID equals grandChild1.Child.ParentID
							where grandChild1.ParentID == child.Parent.ParentID
							select grandChild1
					).Take(1).DefaultIfEmpty()
					select new
					{
						GrandChildID   = grandChild.GrandChildID,
						ChildID        = child.ChildID,
						ParentParentId = child.Parent.ParentID,
						Tmp            = pf.GrandChildID
					};

				AreEqual(expected, result);
			}
		}
	}
}
