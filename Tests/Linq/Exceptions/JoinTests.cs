using System;
using System.Linq;

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
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		public class Childs
		{
			[PrimaryKey]
			public int ChildID;
			public int ParentID;
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
		public void MultiJoin0(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from c in Child
					join p1 in Parent on c.ParentID equals p1.ParentID
					join p2 in Parent on c.ParentID equals p2.ParentID
					select c;

				var result = from c in db.GetTable<Childs>()
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
		public void MultiJoin1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in Parent
					join c1 in Child on p.ParentID equals c1.ParentID
					join c2 in Child on p.ParentID equals c2.ParentID
					select new Child()
					{
						ChildID = c1.ChildID,
						ParentID = c2.ParentID
					};

				var result = from p in db.GetTable<Parents>()
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
							   join p2 in Parent on c.ParentID equals p2.ParentID
							   select c;

				var result = from c in db.Child
							 join p1 in db.Parent on c.ParentID equals p1.ParentID
							 join p2 in db.Parent on c.ParentID equals p2.ParentID
							 select
							 new Child()
							 {
								 ChildID = c.ChildID,
								 ParentID = c.ParentID
							 };

				AreEqual(expected, result);
			}
		}

	}
}
