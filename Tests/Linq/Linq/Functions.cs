using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Functions : TestBase
	{
		[Test]
		public void Contains1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where new[] { 1, 2 }.Contains(p.ParentID) select p,
					from p in db.Parent where new[] { 1, 2 }.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains2([DataContexts] string context)
		{
			var arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains3([DataContexts] string context)
		{
			var n = 2;

			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
					where new[] { 1, n }.Contains(p.ParentID)
					select p,
					CompiledQuery.Compile<ITestDataContext, IQueryable<Parent>>(db =>
						from p in db.Parent
						where new[] { 1, n }.Contains(p.ParentID)
						select p)(data));
		}

		[Test]
		public void Contains4([DataContexts] string context)
		{
			var arr = new[] { 1, 2 };

			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
					where arr.Contains(p.ParentID)
					select p,
					CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(db =>
						from p in db.Parent
						where arr.Contains(p.ParentID)
						select p)(data));
		}

		[Test]
		public void Contains5([DataContexts] string context)
		{
			var arr1 = new[] { 1, 2 };
			var arr2 = new[] { 1, 2, 4 };

			var expected1 = from p in Parent where arr1.Contains(p.ParentID) select p;
			var expected2 = from p in Parent where arr2.Contains(p.ParentID) select p;

			using (var data = GetDataContext(context))
			{
				var cq = CompiledQuery.Compile<ITestDataContext,int[],IQueryable<Parent>>((db,a) =>
					from p in db.Parent
					where a.Contains(p.ParentID)
					select p);

				AreEqual(expected1, cq(data, arr1));
				AreEqual(expected2, cq(data, arr2));
			}
		}

		[Test]
		public void Contains6([DataContexts] string context)
		{
			var arr = new List<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains7([DataContexts] string context)
		{
			IEnumerable<int> arr = new[] { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void ContainsKey1([DataContexts] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Keys.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Keys.Contains(p.ParentID) select p);
		}

		[Test]
		public void ContainsKey2([DataContexts] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsKey(p.ParentID) select p,
					from p in db.Parent where arr.ContainsKey(p.ParentID) select p);
		}

		[Test]
		public void ContainsValue1([DataContexts] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Values.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Values.Contains(p.ParentID) select p);
		}

		[Test]
		public void ContainsValue2([DataContexts] string context)
		{
			var arr = new Dictionary<int,int>
			{
				{ 1, 1 },
				{ 2, 2 },
			};

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.ContainsValue(p.ParentID) select p,
					from p in db.Parent where arr.ContainsValue(p.ParentID) select p);
		}

		[Test]
		public void ContainsHashSet1([DataContexts] string context)
		{
			var arr = new HashSet<int> { 1, 2 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where arr.Contains(p.ParentID) select p,
					from p in db.Parent where arr.Contains(p.ParentID) select p);
		}

		[Test]
		public void EmptyContains1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where new int[0].Contains(p.ParentID) || p.ParentID == 2
					select p,
					from p in db.Parent
					where new int[0].Contains(p.ParentID) || p.ParentID == 2
					select p);
		}

		[Test]
		public void ContainsString11([DataContexts] string context)
		{
			var arr = new List<string> { "John" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString12([DataContexts] string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString13([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new List<string> { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new List<string> { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString21([DataContexts] string context)
		{
			var arr = new[] { "John" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where arr.Contains(p.FirstName) select p,
					from p in db.Person where arr.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString22([DataContexts] string context)
		{
			var nm = "John";

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { nm }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { nm }.Contains(p.FirstName) select p);
		}

		[Test]
		public void ContainsString23([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { "John" }.Contains(p.FirstName) select p,
					from p in db.Person where new[] { "John" }.Contains(p.FirstName) select p);
		}

		[Test]
		public void Equals1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID.Equals(2) select p,
					from p in db.Parent where p.ParentID.Equals(2) select p);
		}

		[Test]
		public void Equals2([DataContexts] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where !ch.Equals(child) select ch,
					from ch in db.Child where !ch.Equals(child) select ch);
		}

		[Test]
		public void Equals3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.Equals(null) select p,
					from p in db.Parent where p.Value1.Equals(null) select p);
		}

		[Test]
		public void Equals4([DataContexts] string context)
		{
			using (var db = new NorthwindDB())
				AreEqual(
					   Customer.Where(c => !c.Address.Equals(null)),
					db.Customer.Where(c => !c.Address.Equals(null)));
		}

		[Test]
		public void NewGuid1([DataContexts(
			ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue != Sql.NewGuid() select p.GuidValue,
					from p in db.Types where p.GuidValue != Sql.NewGuid() select p.GuidValue);
		}

		[Test]
		public void NewGuid2([DataContexts(ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreNotEqual(Guid.Empty, (from p in db.Types select Sql.NewGuid()).First());
		}

		[Test]
		public void CustomFunc([DataContexts] string context)
		{
			Expressions.MapMember<Person>(p => p.FullName(), (Expression<Func<Person,string>>)(p => p.LastName + ", " + p.FirstName));

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.FullName() == "Pupkin, John" select p.FullName(),
					from p in db.Person where p.FullName() == "Pupkin, John" select p.FullName());
		}

		[Test]
		public void Count1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Count(c => c.ParentID == 1),
					db.Child.Count(c => c.ParentID == 1));
		}

		[Test]
		public void Sum1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(
					   Child.Sum(c => c.ParentID),
					db.Child.Sum(c => c.ParentID));
		}

		[MethodExpression("ChildCountExpression")]
		public static int ChildCount(Parent parent)
		{
			throw new NotSupportedException();
		}

		static Expression ChildCountExpression()
		{
			return
				(Expression<Func<Parent, int>>)
				(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID));
		}

		[Test]
		public void Sum2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => p.Children.Where(c => c.ParentID > 2).Sum(c => c.ParentID * c.ChildID)),
					db.Parent.Select(p => ChildCount(p)));
		}
	}

	public static class PersonExtension
	{
		static public string FullName(this Person person)
		{
			return person.LastName + ", " + person.FirstName;
		}
	}
}
