using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class WhereTest : TestBase
	{
		[Test]
		public void MakeSubQuery()
		{
			TestOneJohn(db => 
				from p in db.Person
				select new { PersonID = p.ID + 1, p.FirstName } into p
				where p.PersonID == 2
				select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void MakeSubQueryWithParam()
		{
			var n = 1;

			TestOneJohn(new[] { "Fdp" }, db => 
				from p in db.Person
				select new { PersonID = p.ID + n, p.FirstName } into p
				where p.PersonID == 2
				select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void DoNotMakeSubQuery()
		{
			TestOneJohn(db => 
				from p1 in db.Person
				select new { p1.ID, Name = p1.FirstName + "\r\r\r" } into p2
				where p2.ID == 1
				select new Person(p2.ID) { FirstName = p2.Name.TrimEnd('\r') });
		}

		[Test]
		public void EqualsConst()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 select p);
		}

		[Test]
		public void EqualsConsts()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && p.FirstName == "John" select p);
		}

		[Test]
		public void EqualsConsts2()
		{
			TestOneJohn(db =>
				from p in db.Person
				where (p.FirstName == "John" || p.FirstName == "John's") && p.ID > 0 && p.ID < 2 && p.LastName != "123"
				select p);
		}

		[Test]
		public void EqualsParam()
		{
			var id = 1;
			TestOneJohn(db => from p in db.Person where p.ID == id select p);
		}

		[Test]
		public void EqualsParams()
		{
			var id   = 1;
			var name = "John";
			TestOneJohn(db => from p in db.Person where p.ID == id && p.FirstName == name select p);
		}

		[Test]
		public void NullParam1()
		{
			var    id   = 1;
			string name = null;
			TestOneJohn(db => from p in db.Person where p.ID == id && p.MiddleName == name select p);
		}

		[Test]
		public void NullParam2()
		{
			var    id   = 1;
			string name = null;
			TestOneJohn(db =>
			{
				      (from p in db.Person where p.ID == id && p.MiddleName == name select p).ToList();
				return from p in db.Person where p.ID == id && p.MiddleName == name select p;
			});
		}

		int TestMethod()
		{
			return 1;
		}

		[Test]
		public void MethodParam()
		{
			TestOneJohn(db => from p in db.Person where p.ID == TestMethod() select p);
		}

		static int StaticTestMethod()
		{
			return 1;
		}

		[Test]
		public void StaticMethodParam()
		{
			TestOneJohn(db => from p in db.Person where p.ID == StaticTestMethod() select p);
		}

		class TestMethodClass
		{
			private readonly int _n;

			public TestMethodClass(int n)
			{
				_n = n;
			}

			public int TestMethod()
			{
				return _n;
			}
		}

		public void MethodParam(int n)
		{
			var t = new TestMethodClass(n);

			ForEachProvider(db =>
			{
				var id = (from p in db.Person where p.ID == t.TestMethod() select new { p.ID }).ToList().First();
				Assert.AreEqual(n, id.ID);
			});
		}

		[Test]
		public void MethodParam2()
		{
			MethodParam(1);
			MethodParam(2);
		}

		static IQueryable<Person> TestDirectParam(ITestDataContext db, int id)
		{
			var name = "John";
			return from p in db.Person where p.ID == id && p.FirstName == name select p;
		}

		[Test]
		public void DirectParams()
		{
			TestOneJohn(db => TestDirectParam(db, 1));
		}

		[Test]
		public void BinaryAdd()
		{
			TestOneJohn(db => from p in db.Person where p.ID + 1 == 2 select p);
		}

		[Test]
		public void BinaryDivide()
		{
			TestOneJohn(db => from p in db.Person where (p.ID + 9) / 10 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryModulo()
		{
			TestOneJohn(db => from p in db.Person where p.ID % 2 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryMultiply()
		{
			TestOneJohn(db => from p in db.Person where p.ID * 10 - 9 == 1 select p);
		}

		[Test]
		public void BinaryXor()
		{
			TestOneJohn(new[] { ProviderName.Access }, db => from p in db.Person where (p.ID ^ 2) == 3 select p);
		}

		[Test]
		public void BinaryAnd()
		{
			TestOneJohn(new[] { ProviderName.Access }, db => from p in db.Person where (p.ID & 3) == 1 select p);
		}

		[Test]
		public void BinaryOr()
		{
			TestOneJohn(new[] { ProviderName.Access }, db => from p in db.Person where (p.ID | 2) == 3 select p);
		}

		[Test]
		public void BinarySubtract()
		{
			TestOneJohn(db => from p in db.Person where p.ID - 1 == 0 select p);
		}

		[Test]
		public void EqualsNull()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && p.MiddleName == null select p);
		}

		[Test]
		public void EqualsNull2()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && null == p.MiddleName select p);
		}

		[Test]
		public void NotEqualNull()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && p.FirstName != null select p);
		}

		[Test]
		public void NotEqualNull2()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && null != p.FirstName select p);
		}

		[Test]
		public void NotTest()
		{
			TestOneJohn(db => from p in db.Person where p.ID == 1 && !(p.MiddleName != null) select p);
		}

		[Test]
		public void NotTest2()
		{
			int n = 2;
			TestOneJohn(db => from p in db.Person where p.ID == 1 && !(p.MiddleName != null && p.ID == n) select p);
		}

		[Test]
		public void Coalesce1()
		{
			TestOneJohn(db =>

				from p in db.Person
				where
					p.ID == 1 &&
					(p.MiddleName ?? "None") == "None" &&
					(p.FirstName  ?? "None") == "John"
				select p

			);
		}

		[Test]
		public void Coalesce2()
		{
			ForEachProvider(db => Assert.AreEqual(1, (from p in db.Parent where p.ParentID == 1 ? true : false select p).ToList().Count));
		}

		[Test]
		public void Coalesce3()
		{
			ForEachProvider(db => Assert.AreEqual(1, (from p in db.Parent where p.ParentID != 1 ? false: true select p).ToList().Count));
		}

		[Test]
		public void Coalesce4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where p.ParentID == 1 ? false: true select p,
				from p in db.Parent where p.ParentID == 1 ? false: true select p));
		}

		[Test]
		public void Coalesce5()
		{
			ForEachProvider(db => Assert.AreEqual(2,
				(from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 10 select p).ToList().Count));
		}

		[Test]
		public void Coalesce6()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p,
				from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p));
		}

		[Test]
		public void Coalesce7()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p,
				from p in db.Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p));
		}

		[Test]
		public void Conditional()
		{
			TestOneJohn(db =>

				from p in db.Person
				where
					p.ID == 1 &&
					(p.MiddleName == null ? 1 : 2) == 1 &&
					(p.FirstName  != null ? 1 : 2) == 1
				select p

			);
		}

		[Test]
		public void Conditional2()
		{
			TestOneJohn(db =>

				from p in db.Person
				where
					p.ID == 1 &&
					(p.MiddleName != null ? 3 : p.MiddleName == null? 1 : 2) == 1 &&
					(p.FirstName  == null ? 3 : p.FirstName  != null? 1 : 2) == 1
				select p

			);
		}

		[Test]
		public void Conditional3()
		{
			TestOneJohn(db =>

				from p in db.Person
				where
					p.ID == 1 &&
					(p.MiddleName != null ? 3 : p.ID == 2 ? 2 : p.MiddleName != null ? 0 : 1) == 1 &&
					(p.FirstName  == null ? 3 : p.ID == 2 ? 2 : p.FirstName  == null ? 0 : 1) == 1
				select p

			);
		}

		[Test]
		public void MultipleQuery1()
		{
			ForEachProvider(db =>
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				var list = q.ToList();
				Assert.AreEqual(1, list[0].ID);

				id = 2;
				list = q.ToList();
				Assert.AreEqual(2, list[0].ID);
			});
		}

		[Test]
		public void MultipleQuery2()
		{
			ForEachProvider(db =>
			{
				string str = null;
				var    q   = from p in db.Person where p.MiddleName == str select p;

				var list = q.ToList();
				Assert.AreNotEqual(0, list.Count);

				str  = "123";
				list = q.ToList();
				Assert.AreEqual(0, list.Count);
			});
		}

		[Test]
		public void HasValue1()
		{
			var expected = from p in Parent where p.Value1.HasValue select p;
			ForEachProvider(db => AreEqual(expected, from p in db.Parent where p.Value1.HasValue select p));
		}

		[Test]
		public void HasValue2()
		{
			ForEachProvider(db => Assert.AreEqual(2, (from p in db.Parent where !p.Value1.HasValue select p).ToList().Count));
		}

		[Test]
		public void Value()
		{
			ForEachProvider(db => Assert.AreEqual(2, (from p in db.Parent where p.Value1.Value == 1 select p).ToList().Count));
		}

		[Test]
		public void CompareNullable1()
		{
			ForEachProvider(db => Assert.AreEqual(2, (from p in db.Parent where p.Value1 == 1 select p).ToList().Count));
		}

		[Test]
		public void CompareNullable2()
		{
			ForEachProvider(db => Assert.AreEqual(1, (from p in db.Parent where p.ParentID == p.Value1 && p.Value1 == 1 select p).ToList().Count));
		}

		[Test]
		public void CompareNullable3()
		{
			ForEachProvider(db => Assert.AreEqual(1, (from p in db.Parent where p.Value1 == p.ParentID && p.Value1 == 1 select p).ToList().Count));
		}

		[Test]
		public void SubQuery()
		{
			var expected =
				from t in
					from ch in Child
					select ch.ParentID * 1000
				where t > 2000
				select t / 1000;

			ForEachProvider(db => AreEqual(expected,
				from t in
					from ch in db.Child
					select ch.ParentID * 1000
				where t > 2000
				select t / 1000));
		}

		[Test]
		public void AnonymousEqual1()
		{
			var child    = new { ParentID = 2, ChildID = 21 };
			var expected =
				from ch in Child
				where ch.ParentID == child.ParentID && ch.ChildID == child.ChildID
				select ch;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				where new { ch.ParentID, ch.ChildID } == child
				select ch));
		}

		[Test]
		public void AnonymousEqual2()
		{
			var child    = new { ParentID = 2, ChildID = 21 };
			var expected =
				from ch in Child
				where !(ch.ParentID == child.ParentID && ch.ChildID == child.ChildID) && ch.ParentID > 0
				select ch;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				where child != new { ch.ParentID, ch.ChildID } && ch.ParentID > 0
				select ch));
		}

		[Test]
		public void AnonymousEqual3()
		{
			var expected =
				from ch in Child
				where ch.ParentID == 2 && ch.ChildID == 21
				select ch;

			ForEachProvider(db => AreEqual(expected,
				from ch in db.Child
				where new { ch.ParentID, ch.ChildID } == new { ParentID = 2, ChildID = 21 }
				select ch));
		}

		[Test]
		public void AnonymousEqual4()
		{
			var parent   = new { ParentID = 2, Value1 = (int?)null };
			var expected =
				from p in Parent
				where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
				where new { p.ParentID, p.Value1 } == parent
				select p));
		}

		[Test]
		public void AnonymousEqual5()
		{
			var parent   = new { ParentID = 3, Value1 = (int?)3 };
			var expected =
				from p in Parent
				where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
				where new { p.ParentID, p.Value1 } == parent
				select p));
		}

		[Test]
		public void CheckLeftJoin1()
		{
			var expected =
				from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null
				select p));
		}

		[Test]
		public void CheckLeftJoin2()
		{
			var expected =
				from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch != null
				select p;

			ForEachProvider(data => AreEqual(expected, CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(db =>
				from p in db.Parent
					join ch in db.Child on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where null != ch
				select p)(data)));
		}

		[Test]
		public void CheckLeftJoin3()
		{
			var expected =
				from p in Parent
					join ch in 
						from c in GrandChild
						where c.ParentID > 0
						select new { ParentID = 1 + c.ParentID, c.ChildID }
					on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null && ch == null
				select p;

			ForEachProvider(new[] { ProviderName.Firebird, ProviderName.Sybase, ProviderName.Access }, db => AreEqual(expected,
				from p in db.Parent
					join ch in 
						from c in db.GrandChild
						where c.ParentID > 0
						select new { ParentID = 1 + c.ParentID, c.ChildID }
					on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null && ch == null
				select p));
		}

		[Test]
		public void CheckLeftJoin4()
		{
			var expected =
				from p in Parent
					join ch in 
						from c in Child
						where c.ParentID > 0
						select new { c.ParentID, c.ChildID }
					on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
					join ch in 
						from c in db.Child
						where c.ParentID > 0
						select new { c.ParentID, c.ChildID }
					on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
				where ch == null
				select p));
		}

		[Test]
		public void CheckNull1()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where p != null select p,
				from p in db.Parent where p != null select p));
		}

		[Test]
		public void CheckNull2()
		{
			int? n = null;

			ForEachProvider(db => AreEqual(
				from p in    Parent where n != null || p.ParentID > 1 select p,
				from p in db.Parent where n != null || p.ParentID > 1 select p));
		}

		[Test]
		public void CheckNull3()
		{
			int? n = 1;

			ForEachProvider(new[] { ProviderName.SqlCe, ProviderName.Firebird }, db => AreEqual(
				from p in    Parent where n != null || p.ParentID > 1 select p,
				from p in db.Parent where n != null || p.ParentID > 1 select p));
		}

		[Test]
		public void CheckCondition1()
		{
			var expected =
				from p in Parent
				where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
				where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
				select p));
		}

		[Test]
		public void CheckCondition2()
		{
			var expected =
				from p in Parent
				where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Parent
				where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
				select p));
		}

		[Test]
		public void CompareObject1()
		{
			var child    = (from ch in Child where ch.ParentID == 2 select ch).First();
			var expected = from ch in Child where ch == child select ch;

			ForEachProvider(db => AreEqual(expected, from ch in db.Child where ch == child select ch));
		}

		[Test]
		public void CompareObject2()
		{
			var parent   = (from p in Parent where p.ParentID == 2 select p).First();
			var expected = from p in Parent where parent == p select p;

			ForEachProvider(db => AreEqual(expected, from p in db.Parent where parent == p select p));
		}

		[Test]
		public void CompareObject3()
		{
			var child    = (from ch in Child where ch.ParentID == 2 select ch).First();
			var expected = from ch in Child where ch != child select ch;

			ForEachProvider(db => AreEqual(expected, from ch in db.Child where ch != child select ch));
		}

		[Test]
		public void OrAnd()
		{
			var expected =
				from c in Child
				where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
				select c;

			ForEachProvider(db => AreEqual(expected,
				from c in db.Child
				where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
				select c));
		}

		[Test]
		public void NotOrAnd()
		{
			var expected =
				from c in Child
				where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
				select c;

			ForEachProvider(db => AreEqual(expected,
				from c in db.Child
				where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
				select c));
		}

		[Test]
		public void AndOr()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
				select p,
				from p in db.Parent
				where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
				select p));
		}

		[Test]
		public void Contains1()
		{
			var words = new [] { "John", "Pupkin" };

			var expected =
				from p in Person
				where words.Contains(p.FirstName) || words.Contains(p.LastName)
				select p;

			ForEachProvider(db => AreEqual(expected,
				from p in db.Person
				where words.Contains(p.FirstName) || words.Contains(p.LastName)
				select p));
		}

		[Test]
		public void Contains2()
		{
			IEnumerable<int> ids = new [] { 2, 3 };

			ForEachProvider(db => AreEqual(
				from p in    Parent where ids.Contains(p.ParentID) select p,
				from p in db.Parent where ids.Contains(p.ParentID) select p));
		}

		static IEnumerable<int> GetIds()
		{
			yield return 1;
			yield return 2;
		}

		[Test]
		public void Contains3()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where GetIds().Contains(p.ParentID) select p,
				from p in db.Parent where GetIds().Contains(p.ParentID) select p));
		}

		static IEnumerable<int> GetIds(int start, int n)
		{
			for (int i = 0; i < n; i++)
				yield return start + i;
		}

		[Test]
		public void Contains4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p,
				from p in db.Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p));
		}

		[Test]
		public void Contains5()
		{
			IEnumerable<int> ids = new int[0];

			ForEachProvider(db => AreEqual(
				from p in    Parent where !ids.Contains(p.ParentID) select p,
				from p in db.Parent where !ids.Contains(p.ParentID) select p));
		}

		[Test]
		public void AliasTest1()
		{
			int user = 3;

			ForEachProvider(db => AreEqual(
				from p in    Parent where p.ParentID == user select p,
				from p in db.Parent where p.ParentID == user select p));
		}

		[Test]
		public void AliasTest2()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Where(_ => _.ParentID == 3),
				db.Parent.Where(_ => _.ParentID == 3)));
		}

		[Test]
		public void AliasTest3()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Where(_p => _p.ParentID == 3),
				db.Parent.Where(_p => _p.ParentID == 3)));
		}

		[Test]
		public void AliasTest4()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Where(тбл => тбл.ParentID == 3),
				db.Parent.Where(тбл => тбл.ParentID == 3)));
		}

		[Test]
		public void AliasTest5()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Where(p_ => p_.ParentID == 3),
				db.Parent.Where(p_ => p_.ParentID == 3)));
		}

		[Test]
		public void SelectNestedCalculatedTest()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from r in from o in    Order select o.Freight * 1000 where r > 100000 select r / 1000,
					from r in from o in db.Order select o.Freight * 1000 where r > 100000 select r / 1000);
		}

		[Test]
		public void CheckField1()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				select new { p } into p
				where p.p.ParentID == 1
				select p.p,
				from p in db.Parent
				select new { p } into p
				where p.p.ParentID == 1
				select p.p));
		}

		[Test]
		public void CheckField2()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				select new { p } into p
				where p.p.ParentID == 1
				select new { p.p.Value1, p },
				from p in db.Parent
				select new { p } into p
				where p.p.ParentID == 1
				select new { p.p.Value1, p }));
		}

		[Test]
		public void CheckField3()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent
				select new { p } into p
				where p.p.ParentID == 1
				select new { p.p.Value1, p.p },
				from p in db.Parent
				select new { p } into p
				where p.p.ParentID == 1
				select new { p.p.Value1, p.p }));
		}

		[Test]
		public void CheckField4()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1),
				db.Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1)));
		}

		[Test]
		public void CheckField5()
		{
			ForEachProvider(db => AreEqual(
				   Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1),
				db.Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1)));
		}

		[Test]
		public void CheckField6()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent
				select new { p, Value = p.Value1 * 100 } into p
				where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p },
				from p in db.Parent
				select new { p, Value = p.Value1 * 100 } into p
				where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p }));
		}

		[Test]
		public void SubQuery1()
		{
			ForEachProvider(db => AreEqual(
				from p in Types
				select new { Value = Math.Round(p.MoneyValue, 2) } into pp
				where pp.Value != 0
				select pp.Value,
				from p in db.Types
				select new { Value = Math.Round(p.MoneyValue, 2) } into pp
				where pp.Value != 0
				select pp.Value));
		}

		[Test]
		public void SearchCondition1()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types
				where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
				select t,
				from t in db.Types
				where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
				select t));
		}
	}
}
