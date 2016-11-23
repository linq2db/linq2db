using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class WhereTests : TestBase
	{
		[Test, DataContextSource]
		public void MakeSubQuery(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + 1, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test, DataContextSource(ProviderName.Firebird)]
		public void MakeSubQueryWithParam(string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + n, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test, DataContextSource]
		public void DoNotMakeSubQuery(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p1 in db.Person
					select new { p1.ID, Name = p1.FirstName + "\r\r\r" } into p2
					where p2.ID == 1
					select new Person(p2.ID) { FirstName = p2.Name.TrimEnd('\r') });
		}

		[Test, DataContextSource]
		public void EqualsConst(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 select p);
		}

		[Test, DataContextSource]
		public void EqualsConsts(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName == "John" select p);
		}

		[Test, DataContextSource]
		public void EqualsConsts2(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where (p.FirstName == "John" || p.FirstName == "John's") && p.ID > 0 && p.ID < 2 && p.LastName != "123"
					select p);
		}

		[Test, DataContextSource]
		public void EqualsParam(string context)
		{
			var id = 1;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id select p);
		}

		[Test, DataContextSource]
		public void EqualsParams(string context)
		{
			var id   = 1;
			var name = "John";
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.FirstName == name select p);
		}

		[Test, DataContextSource]
		public void NullParam1(string context)
		{
			var    id   = 1;
			string name = null;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.MiddleName == name select p);
		}

		[Test, DataContextSource]
		public void NullParam2(string context)
		{
			var    id   = 1;
			string name = null;

			using (var db = GetDataContext(context))
			{
				       (from p in db.Person where p.ID == id && p.MiddleName == name select p).ToList();
				var q = from p in db.Person where p.ID == id && p.MiddleName == name select p;

				TestOneJohn(q);
			}
		}

		int TestMethod()
		{
			return 1;
		}

		[Test, DataContextSource]
		public void MethodParam(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == TestMethod() select p);
		}

		static int StaticTestMethod()
		{
			return 1;
		}

		[Test, DataContextSource]
		public void StaticMethodParam(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == StaticTestMethod() select p);
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

		public void MethodParam(int n, string context)
		{
			var t = new TestMethodClass(n);

			using (var db = GetDataContext(context))
			{
				var id = (from p in db.Person where p.ID == t.TestMethod() select new { p.ID }).ToList().First();
				Assert.AreEqual(n, id.ID);
			}
		}

		[Test, DataContextSource]
		public void MethodParam2(string context)
		{
			MethodParam(1, context);
			MethodParam(2, context);
		}

		static IQueryable<Person> TestDirectParam(ITestDataContext db, int id)
		{
			var name = "John";
			return from p in db.Person where p.ID == id && p.FirstName == name select p;
		}

		[Test, DataContextSource]
		public void DirectParams(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(TestDirectParam(db, 1));
		}

		[Test, DataContextSource]
		public void BinaryAdd(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID + 1 == 2 select p);
		}

		[Test, DataContextSource]
		public void BinaryDivide(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID + 9) / 10 == 1 && p.ID == 1 select p);
		}

		[Test, DataContextSource]
		public void BinaryModulo(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID % 2 == 1 && p.ID == 1 select p);
		}

		[Test, DataContextSource]
		public void BinaryMultiply(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID * 10 - 9 == 1 select p);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void BinaryXor(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID ^ 2) == 3 select p);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void BinaryAnd(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID & 3) == 1 select p);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void BinaryOr(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID | 2) == 3 select p);
		}

		[Test, DataContextSource]
		public void BinarySubtract(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID - 1 == 0 select p);
		}

		[Test, DataContextSource]
		public void EqualsNull(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.MiddleName == null select p);
		}

		[Test, DataContextSource]
		public void EqualsNull2(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null == p.MiddleName select p);
		}

		[Test, DataContextSource]
		public void NotEqualNull(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName != null select p);
		}

		[Test, DataContextSource]
		public void NotEqualNull2(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null != p.FirstName select p);
		}

		[Test, DataContextSource]
		public void NotTest(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null) select p);
		}

		[Test, DataContextSource]
		public void NotTest2(string context)
		{
			var n = 2;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null && p.ID == n) select p);
		}

		[Test, DataContextSource]
		public void Coalesce1(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName ?? "None") == "None" &&
						(p.FirstName  ?? "None") == "John"
					select p);
		}

		[Test, DataContextSource]
		public void Coalesce2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID == 1 ? true : false select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void Coalesce3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID != 1 ? false : true select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void Coalesce4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID == 1 ? false: true select p,
					from p in db.Parent where p.ParentID == 1 ? false: true select p);
		}

		[Test, DataContextSource]
		public void Coalesce5(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 10 select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void Coalesce6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p);
		}

		[Test, DataContextSource]
		public void Coalesce7(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p);
		}

		[Test, DataContextSource]
		public void Conditional(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName == null ? 1 : 2) == 1 &&
						(p.FirstName  != null ? 1 : 2) == 1
					select p);
		}

		[Test, DataContextSource]
		public void Conditional2(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.MiddleName == null? 1 : 2) == 1 &&
						(p.FirstName  == null ? 3 : p.FirstName  != null? 1 : 2) == 1
					select p);
		}

		[Test, DataContextSource]
		public void Conditional3(string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.ID == 2 ? 2 : p.MiddleName != null ? 0 : 1) == 1 &&
						(p.FirstName  == null ? 3 : p.ID == 2 ? 2 : p.FirstName  == null ? 0 : 1) == 1
					select p);
		}

		[Test, DataContextSource]
		public void MultipleQuery1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				var list = q.ToList();
				Assert.AreEqual(1, list[0].ID);

				id = 2;
				list = q.ToList();
				Assert.AreEqual(2, list[0].ID);
			}
		}

		[Test, DataContextSource]
		public void MultipleQuery2(string context)
		{
			using (var db = GetDataContext(context))
			{
				string str = null;
				var    q   = from p in db.Person where p.MiddleName == str select p;

				var list = q.ToList();
				Assert.AreNotEqual(0, list.Count);

				str  = "123";
				list = q.ToList();
				Assert.AreEqual(0, list.Count);
			}
		}

		[Test, DataContextSource]
		public void HasValue1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.Value1.HasValue select p,
					from p in db.Parent where p.Value1.HasValue select p);
		}

		[Test, DataContextSource]
		public void HasValue2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where !p.Value1.HasValue select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void Value(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where p.Value1.Value == 1 select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void CompareNullable1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(2, (from p in db.Parent where p.Value1 == 1 select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void CompareNullable2(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.ParentID == p.Value1 && p.Value1 == 1 select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void CompareNullable3(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from p in db.Parent where p.Value1 == p.ParentID && p.Value1 == 1 select p).ToList().Count);
		}

		[Test, DataContextSource]
		public void SubQuery(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from ch in Child
						select ch.ParentID * 1000
					where t > 2000
					select t / 1000,
					from t in
						from ch in db.Child
						select ch.ParentID * 1000
					where t > 2000
					select t / 1000);
		}

		[Test, DataContextSource]
		public void AnonymousEqual1(string context)
		{
			var child = new { ParentID = 2, ChildID = 21 };

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == child.ParentID && ch.ChildID == child.ChildID
					select ch
					,
					from ch in db.Child
					where new { ch.ParentID, ch.ChildID } == child
					select ch);
		}

		[Test, DataContextSource]
		public void AnonymousEqual2(string context)
		{
			var child = new { ParentID = 2, ChildID = 21 };

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where !(ch.ParentID == child.ParentID && ch.ChildID == child.ChildID) && ch.ParentID > 0
					select ch
					,
					from ch in db.Child
					where child != new { ch.ParentID, ch.ChildID } && ch.ParentID > 0
					select ch);
		}

		[Test, DataContextSource]
		public void AnonymousEqual31(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == 2 && ch.ChildID == 21
					select ch
					,
					from ch in db.Child
					where new { ch.ParentID, ch.ChildID } == new { ParentID = 2, ChildID = 21 }
					select ch);
		}

		[Test, DataContextSource]
		public void AnonymousEqual32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child
					where ch.ParentID == 2 && ch.ChildID == 21
					select ch
					,
					from ch in db.Child
					where new { ParentID = 2, ChildID = 21 } == new { ch.ParentID, ch.ChildID }
					select ch);
		}

		[Test, DataContextSource]
		public void AnonymousEqual4(string context)
		{
			var parent = new { ParentID = 2, Value1 = (int?)null };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
					select p
					,
					from p in db.Parent
					where new { p.ParentID, p.Value1 } == parent
					select p);
		}

		[Test, DataContextSource]
		public void AnonymousEqual5(string context)
		{
			var parent = new { ParentID = 3, Value1 = (int?)3 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == parent.ParentID && p.Value1 == parent.Value1
					select p
					,
					from p in db.Parent
					where new { p.ParentID, p.Value1 } == parent
					select p);
		}

		[Test, DataContextSource]
		public void CheckLeftJoin1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p
					,
					from p in db.Parent
						join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p);
		}

		[Test, DataContextSource]
		public void CheckLeftJoin2(string context)
		{
			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in Child on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch != null
					select p
					,
					CompiledQuery.Compile<ITestDataContext,IQueryable<Parent>>(db =>
						from p in db.Parent
							join ch in db.Child on p.ParentID equals ch.ParentID into lj1
							from ch in lj1.DefaultIfEmpty()
						where null != ch
						select p)(data));
		}

		[Test, DataContextSource(ProviderName.Firebird, ProviderName.Sybase, ProviderName.Access)]
		public void CheckLeftJoin3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in 
							from c in GrandChild
							where c.ParentID > 0
							select new { ParentID = 1 + c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null && ch == null
					select p
					,
					from p in db.Parent
						join ch in 
							from c in db.GrandChild
							where c.ParentID > 0
							select new { ParentID = 1 + c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null && ch == null
					select p);
		}

		[Test, DataContextSource]
		public void CheckLeftJoin4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
						join ch in 
							from c in Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p
					,
					from p in db.Parent
						join ch in 
							from c in db.Child
							where c.ParentID > 0
							select new { c.ParentID, c.ChildID }
						on p.ParentID equals ch.ParentID into lj1
						from ch in lj1.DefaultIfEmpty()
					where ch == null
					select p);
		}

		[Test, DataContextSource]
		public void CheckNull1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p != null select p,
					from p in db.Parent where p != null select p);
		}

		[Test, DataContextSource]
		public void CheckNull2(string context)
		{
			int? n = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.Firebird)]
		public void CheckNull3(string context)
		{
			int? n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test, DataContextSource]
		public void CheckCondition1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
					select p
					,
					from p in db.Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && p.Value1.HasValue
					select p);
		}

		[Test, DataContextSource]
		public void CheckCondition2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
					select p
					,
					from p in db.Parent
					where p.ParentID == 1 && p.Value1 == 1 || p.ParentID == 2 && (p.ParentID != 3 || p.ParentID == 4) && p.Value1.HasValue
					select p);
		}

		[Test, DataContextSource]
		public void CompareObject1(string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch == child select ch,
					from ch in db.Child where ch == child select ch);
		}

		[Test, DataContextSource]
		public void CompareObject2(string context)
		{
			var parent = (from p in Parent where p.ParentID == 2 select p).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where parent == p select p,
					from p in db.Parent where parent == p select p);
		}

		[Test, DataContextSource]
		public void CompareObject3(string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch != child select ch,
					from ch in db.Child where ch != child select ch);
		}

		[Test, DataContextSource]
		public void OrAnd(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in Child
					where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
					select c
					,
					from c in db.Child
					where (c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 21
					select c);
		}

		[Test, DataContextSource]
		public void NotOrAnd(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in Child
					where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
					select c
					,
					from c in db.Child
					where !(c.ParentID == 2 || c.ParentID == 3) && c.ChildID != 44
					select c);
		}

		[Test, DataContextSource]
		public void AndOr(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
					select p,
					from p in db.Parent
					where p.ParentID == 1 || (p.ParentID == 2 || p.ParentID == 3) && (p.ParentID == 3 || p.ParentID == 1)
					select p);
		}

		[Test, DataContextSource]
		public void Contains1(string context)
		{
			var words = new [] { "John", "Pupkin" };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Person
					where words.Contains(p.FirstName) || words.Contains(p.LastName)
					select p
					,
					from p in db.Person
					where words.Contains(p.FirstName) || words.Contains(p.LastName)
					select p);
		}

		[Test, DataContextSource]
		public void Contains2(string context)
		{
			IEnumerable<int> ids = new [] { 2, 3 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where ids.Contains(p.ParentID) select p,
					from p in db.Parent where ids.Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds()
		{
			yield return 1;
			yield return 2;
		}

		[Test, DataContextSource]
		public void Contains3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where GetIds().Contains(p.ParentID) select p,
					from p in db.Parent where GetIds().Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds(int start, int n)
		{
			for (int i = 0; i < n; i++)
				yield return start + i;
		}

		[Test, DataContextSource]
		public void Contains4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p,
					from p in db.Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void Contains5(string context)
		{
			IEnumerable<int> ids = new int[0];

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where !ids.Contains(p.ParentID) select p,
					from p in db.Parent where !ids.Contains(p.ParentID) select p);
		}

		[Test, DataContextSource]
		public void AliasTest1(string context)
		{
			int user = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID == user select p,
					from p in db.Parent where p.ParentID == user select p);
		}

		[Test, DataContextSource]
		public void AliasTest2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_ => _.ParentID == 3),
					db.Parent.Where(_ => _.ParentID == 3));
		}

		[Test, DataContextSource]
		public void AliasTest3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_p => _p.ParentID == 3),
					db.Parent.Where(_p => _p.ParentID == 3));
		}

		[Test, DataContextSource]
		public void AliasTest4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(тбл => тбл.ParentID == 3),
					db.Parent.Where(тбл => тбл.ParentID == 3));
		}

		[Test, DataContextSource]
		public void AliasTest5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p_ => p_.ParentID == 3),
					db.Parent.Where(p_ => p_.ParentID == 3));
		}

		[Test, NorthwindDataContext]
		public void SelectNestedCalculatedTest(string context)
		{
			using (var db = new NorthwindDB(context))
				AreEqual(
					from r in from o in GetNorthwindAsList(context).Order select o.Freight * 1000 where r > 100000 select r / 1000,
					from r in from o in db.Order select o.Freight * 1000 where r > 100000 select r / 1000);
		}

		[Test, DataContextSource]
		public void CheckField1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select p.p
					,
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select p.p);
		}

		[Test, DataContextSource]
		public void CheckField2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p },
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p });
		}

		[Test, DataContextSource]
		public void CheckField3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p.p },
					from p in db.Parent
					select new { p } into p
					where p.p.ParentID == 1
					select new { p.p.Value1, p.p });
		}

		[Test, DataContextSource]
		public void CheckField4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1),
					db.Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1));
		}

		[Test, DataContextSource]
		public void CheckField5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1),
					db.Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1));
		}

		[Test, DataContextSource]
		public void CheckField6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p },
					from p in db.Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p });
		}

		[Test, DataContextSource]
		public void SubQuery1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Types
					select new { Value = Math.Round(p.MoneyValue, 2) } into pp
					where pp.Value != 0 && pp.Value != 7
					select pp.Value
					,
					from p in db.Types
					select new { Value = Math.Round(p.MoneyValue, 2) } into pp
					where pp.Value != 0 && pp.Value != 7
					select pp.Value);
		}

		[Test, DataContextSource]
		public void SearchCondition1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t,
					from t in db.Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t);
		}

		[Test, DataContextSource]
		public void GroupBySubQquery1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => qry1.Any(y => y == x.ChildID));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => qry2.Any(y => y == x.ChildID));

				AreEqual(qry12, qry22);
			}
		}

		[Test, DataContextSource]
		public void GroupBySubQquery2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => qry1.Contains(x.ChildID));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => qry2.Contains(x.ChildID));

				AreEqual(qry12, qry22);
			}
		}

		[Test, DataContextSource]
		public void HavingTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Where(c => c.Count() > 1)
						.Select(g => new { count = g.Count() }),
					db.Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Count() > 1)
						.Select (g => new { count = g.Count() }));
			}
		}

		[Test, DataContextSource]
		public void HavingTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Select (g => new { count = g.Count() })
						.Where  (c => c.count > 1),
					db.Child
						.GroupBy(c => c.ParentID)
						.Select (g => new { count = g.Count() })
						.Having (c => c.count > 1)
						.Where  (c => c.count > 1));
			}
		}

		[Test, DataContextSource]
		public void HavingTest3(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Key > 1 && c.Count() > 1)
						.Select (g => g.Count()),
					db.Child
						.GroupBy(c => c.ParentID)
						.Where  (c => c.Key > 1 && c.Count() > 1)
						.Having (c => c.Key > 1)
						.Select (g => g.Count()));
			}
		}
	}
}
