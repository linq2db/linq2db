using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class WhereTests : TestBase
	{
		[Test]
		public void MakeSubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + 1, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void MakeSubQueryWithParam([DataSources] string context)
		{
			var n = 1;

			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					select new { PersonID = p.ID + n, p.FirstName } into p
					where p.PersonID == 2
					select new Person(p.PersonID - 1) { FirstName = p.FirstName });
		}

		[Test]
		public void DoNotMakeSubQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p1 in db.Person
					select new { p1.ID, Name = p1.FirstName + "\r\r\r" } into p2
					where p2.ID == 1
					select new Person(p2.ID) { FirstName = p2.Name.TrimEnd('\r') });
		}

		[Test]
		public void EqualsConst([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 select p);
		}

		[Test]
		public void EqualsConsts([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName == "John" select p);
		}

		[Test]
		public void EqualsConsts2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where (p.FirstName == "John" || p.FirstName == "John's") && p.ID > 0 && p.ID < 2 && p.LastName != "123"
					select p);
		}

		[Test]
		public void EqualsParam([DataSources] string context)
		{
			var id = 1;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id select p);
		}

		[Test]
		public void EqualsParams([DataSources] string context)
		{
			var id   = 1;
			var name = "John";
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.FirstName == name select p);
		}

		[Test]
		public void NullParam1([DataSources] string context)
		{
			var     id   = 1;
			string? name = null;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == id && p.MiddleName == name select p);
		}

		[Test]
		public void NullParam2([DataSources] string context)
		{
			var     id   = 1;
			string? name = null;

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

		[Test]
		public void MethodParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == TestMethod() select p);
		}

		static int StaticTestMethod()
		{
			return 1;
		}

		[Test]
		public void StaticMethodParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == StaticTestMethod() select p);
		}

		sealed class TestMethodClass
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

		private void MethodParam(int n, string context)
		{
			var t = new TestMethodClass(n);

			using (var db = GetDataContext(context))
			{
				var id = (from p in db.Person where p.ID == t.TestMethod() select new { p.ID }).ToList().First();
				Assert.That(id.ID, Is.EqualTo(n));
			}
		}

		[Test]
		public void MethodParam2([DataSources] string context)
		{
			MethodParam(1, context);
			MethodParam(2, context);
		}

		static IQueryable<Person> TestDirectParam(ITestDataContext db, int id)
		{
			var name = "John";
			return from p in db.Person where p.ID == id && p.FirstName == name select p;
		}

		[Test]
		public void DirectParams([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(TestDirectParam(db, 1));
		}

		[Test]
		public void BinaryAdd([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID + 1 == 2 select p);
		}

		[Test]
		public void BinaryDivide([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID + 9) / 10 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryModulo([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID % 2 == 1 && p.ID == 1 select p);
		}

		[Test]
		public void BinaryMultiply([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID * 10 - 9 == 1 select p);
		}

		[Test]
		public void BinaryXor([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID ^ 2) == 3 select p);
		}

		[Test]
		public void BinaryAnd([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where (p.ID & 3) == 1 select p);
		}

		[Test]
		public void BinaryOr([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Person.Where(p => (p.ID | 2) == 3),
					db.Person.Where(p => (p.ID | 2) == 3));
			}
		}

		[Test]
		public void BinarySubtract([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID - 1 == 0 select p);
		}

		[Test]
		public void EqualsNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.MiddleName == null select p);
		}

		[Test]
		public void EqualsNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null == p.MiddleName select p);
		}

		[Test]
		public void NotEqualNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && p.FirstName != null select p);
		}

		[Test]
		public void NotEqualNull2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && null != p.FirstName select p);
		}

		[Test]
		public void ComparisionNullCheckOn1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => p.Value1 != 1),
					db.Parent.Where(p => p.Value1 != 1));
		}

		[Test]
		public void ComparisionNullCheckOn2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p => 1 != p.Value1),
					db.Parent.Where(p => 1 != p.Value1));
		}

		[Test]
		public void ComparisionNullCheckOff([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseCompareNulls(CompareNulls.LikeSql));
			AreEqual(
				   Parent.Where(p => p.Value1 != 1 && p.Value1 != null),
				db.Parent.Where(p => p.Value1 != 1 && p.Value1 != null));
		}

		[Test]
		public void NotTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null) select p);
		}

		[Test]
		public void NotTest2([DataSources] string context)
		{
			var n = 2;
			using (var db = GetDataContext(context))
				TestOneJohn(from p in db.Person where p.ID == 1 && !(p.MiddleName != null && p.ID == n) select p);
		}

		[Test]
		public void Coalesce([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName ?? "None") == "None" &&
						(p.FirstName ?? "None") == "John"
					select p);
		}

		[Test]
		public void Conditional1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.ParentID == 1 ? true : false select p).ToList(), Has.Count.EqualTo(1));
		}

		[Test]
		public void Conditional2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.ParentID != 1 ? false : true select p).ToList(), Has.Count.EqualTo(1));
		}

		[Test]
		public void Conditional3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p.ParentID == 1 ? false : true select p,
					from p in db.Parent where p.ParentID == 1 ? false : true select p);
		}

		[Test]
		public void Conditional4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 10 select p).ToList(), Has.Count.EqualTo(2));
		}

		[Test]
		public void Conditional5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.Value1 == 1 ? 10 : 20) == 20 select p);
		}

		[Test]
		public void Conditional6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p,
					from p in db.Parent where (p.ParentID == 1 ? 10 : 20) == 20 select p);
		}

		[Test]
		public void Conditional7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName == null ? 1 : 2) == 1 &&
						(p.FirstName != null ? 1 : 2) == 1
					select p);
		}

		[Test]
		public void Conditional8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.MiddleName == null ? 1 : 2) == 1 &&
						(p.FirstName == null ? 3 : p.FirstName != null ? 1 : 2) == 1
					select p);
		}

		[Test]
		public void Conditional9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestOneJohn(
					from p in db.Person
					where
						p.ID == 1 &&
						(p.MiddleName != null ? 3 : p.ID == 2 ? 2 : p.MiddleName != null ? 0 : 1) == 1 &&
						(p.FirstName == null ? 3 : p.ID == 2 ? 2 : p.FirstName == null ? 0 : 1) == 1
					select p);
		}

		[Test]
		public void MultipleQuery1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var q  = from p in db.Person where p.ID == id select p;

				var list = q.ToList();
				Assert.That(list[0].ID, Is.EqualTo(1));

				id = 2;
				list = q.ToList();
				Assert.That(list[0].ID, Is.EqualTo(2));
			}
		}

		[Test]
		public void MultipleQuery2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				string? str = null;
				var     q   = from p in db.Person where p.MiddleName == str select p;

				var list = q.ToList();
				Assert.That(list, Is.Not.Empty);

				str = "123";
				list = q.ToList();
				Assert.That(list, Is.Empty);
			}
		}

		[Test]
		public void HasValue1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p.Value1.HasValue select p,
					from p in db.Parent where p.Value1.HasValue select p);
		}

		[Test]
		public void HasValue2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where !p.Value1.HasValue select p).ToList(), Has.Count.EqualTo(2));
		}

		[Test]
		public void Value([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.Value1!.Value == 1 select p).ToList(), Has.Count.EqualTo(2));
		}

		[Test]
		public void CompareNullable1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.Value1 == 1 select p).ToList(), Has.Count.EqualTo(2));
		}

		[Test]
		public void CompareNullable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.ParentID == p.Value1 && p.Value1 == 1 select p).ToList(), Has.Count.EqualTo(1));
		}

		[Test]
		public void CompareNullable3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That((from p in db.Parent where p.Value1 == p.ParentID && p.Value1 == 1 select p).ToList(), Has.Count.EqualTo(1));
		}

		sealed class WhereCompareData
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = false)]
			public int NotNullable { get; set; }

			[Column(CanBeNull = true)]
			public int? Nullable { get; set; }

			[Column(CanBeNull = true)]
			public int? OtherNullable { get; set; }

			public static WhereCompareData[] Seed()
			{
				return new WhereCompareData[]
				{
					new WhereCompareData{Id = 1, NotNullable = 1, Nullable = null, OtherNullable = 10},
					new WhereCompareData{Id = 2, NotNullable = 1, Nullable = 10,   OtherNullable = 10},
					new WhereCompareData{Id = 3, NotNullable = 1, Nullable = 10,   OtherNullable = null},
					new WhereCompareData{Id = 4, NotNullable = 1, Nullable = null, OtherNullable = null},

					new WhereCompareData{Id = 5, NotNullable = 1, Nullable = null, OtherNullable = 20},
					new WhereCompareData{Id = 6, NotNullable = 1, Nullable = 10,   OtherNullable = 20},
					new WhereCompareData{Id = 7, NotNullable = 1, Nullable = 10,   OtherNullable = null},
					new WhereCompareData{Id = 8, NotNullable = 1, Nullable = null, OtherNullable = null},

					new WhereCompareData{Id = 9,  NotNullable = 1, Nullable = null, OtherNullable = 20},
					new WhereCompareData{Id = 10, NotNullable = 1, Nullable = 30,   OtherNullable = 20},
					new WhereCompareData{Id = 11, NotNullable = 1, Nullable = 30,   OtherNullable = null},
					new WhereCompareData{Id = 12, NotNullable = 1, Nullable = null, OtherNullable = null},

				};
			}
		}

		[Test]
		public void CompareEqual([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(WhereCompareData.Seed()))
			{
				AssertQuery(table.Where(p => p.Nullable == p.OtherNullable));
				AssertQuery(table.Where(p => !(p.Nullable == p.OtherNullable)));
				AssertQuery(table.Where(p => p.OtherNullable == p.Nullable));
				AssertQuery(table.Where(p => !(p.OtherNullable == p.Nullable)));
			}
		}

		[Test]
		public void CompareGreat([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(WhereCompareData.Seed()))
			{
				AssertQuery(table.Where(p => p.Nullable > p.OtherNullable));
				AssertQuery(table.Where(p => !(p.Nullable > p.OtherNullable)));
				AssertQuery(table.Where(p => p.OtherNullable < p.Nullable));
				AssertQuery(table.Where(p => !(p.OtherNullable < p.Nullable)));
			}
		}

		[Test]
		public void CompareLess([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(WhereCompareData.Seed()))
			{
				AssertQuery(table.Where(p => p.Nullable < p.OtherNullable));
				AssertQuery(table.Where(p => !(p.Nullable < p.OtherNullable)));
				AssertQuery(table.Where(p => p.OtherNullable > p.Nullable));
				AssertQuery(table.Where(p => !(p.OtherNullable > p.Nullable)));
			}
		}

		[Test]
		public void CompareNullableEqual([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 == 1));
				AssertQuery(db.Parent.Where(p => !(p.Value1 == 1)));
				AssertQuery(db.Parent.Where(p => 1 == p.Value1));
				AssertQuery(db.Parent.Where(p => !(1 == p.Value1)));
			}
		}

		[Test]
		public void CompareNullableNotEqual([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 != 1));
				AssertQuery(db.Parent.Where(p => !(p.Value1 != 1)));
				AssertQuery(db.Parent.Where(p => 1 != p.Value1));
				AssertQuery(db.Parent.Where(p => !(1 != p.Value1)));
			}
		}

		[Test]
		public void CompareNullableGreatOrEqual([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 >= 2));
				AssertQuery(db.Parent.Where(p => !(p.Value1 >= 2)));
				AssertQuery(db.Parent.Where(p => 2 <= p.Value1));
				AssertQuery(db.Parent.Where(p => !(2 <= p.Value1)));
			}
		}

		[Test]
		public void CompareNullableGreat([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 > 2));
				AssertQuery(db.Parent.Where(p => !(p.Value1 > 2)));
				AssertQuery(db.Parent.Where(p => 2 < p.Value1));
				AssertQuery(db.Parent.Where(p => !(2 < p.Value1)));
			}
		}

		[Test]
		public void CompareNullableLessOrEqual([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 <= 2));
				AssertQuery(db.Parent.Where(p => !(p.Value1 <= 2)));
				AssertQuery(db.Parent.Where(p => 2 >= p.Value1));
				AssertQuery(db.Parent.Where(p => !(2 >= p.Value1)));
			}
		}

		[Test]
		public void CompareNullableLess([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AssertQuery(db.Parent.Where(p => p.Value1 < 2));
				AssertQuery(db.Parent.Where(p => !(p.Value1 < 2)));
				AssertQuery(db.Parent.Where(p => 2 > p.Value1));
				AssertQuery(db.Parent.Where(p => !(2 > p.Value1)));
			}
		}

		[Test]
		public void SubQuery([DataSources] string context)
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

		[Test]
		public void AnonymousEqual1([DataSources] string context)
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

		[Test]
		public void AnonymousEqual2([DataSources] string context)
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

		[Test]
		public void AnonymousEqual31([DataSources] string context)
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

		[Test]
		public void AnonymousEqual32([DataSources] string context)
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

		[Test]
		public void AnonymousEqual4([DataSources] string context)
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

		[Test]
		public void AnonymousEqual5([DataSources] string context)
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

		[Test]
		public void CheckLeftJoin1([DataSources] string context)
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

		[Test]
		public void CheckLeftJoin2([DataSources] string context)
		{
			using (var data = GetDataContext(context))
				AreEqual(
					from p in Parent
					join ch in Child on p.ParentID equals ch.ParentID into lj1
					from ch in lj1.DefaultIfEmpty()
					where ch != null
					select p
					,
					CompiledQuery.Compile<ITestDataContext, IQueryable<Parent>>(db =>
						 from p in db.Parent
						 join ch in db.Child on p.ParentID equals ch.ParentID into lj1
						 from ch in lj1.DefaultIfEmpty()
						 where null != ch
						 select p)(data));
		}

		[Test]
		public void CheckLeftJoin3([DataSources] string context)
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

		[Test]
		public void CheckLeftJoin4([DataSources] string context)
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

		[Test]
		public void CheckNull1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p != null select p,
					from p in db.Parent where p != null select p);
		}

		[Test]
		public void CheckNull2([DataSources] string context)
		{
			int? n = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test]
		public void CheckNull3([DataSources(ProviderName.SqlCe)] string context)
		{
			int? n = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where n != null || p.ParentID > 1 select p,
					from p in db.Parent where n != null || p.ParentID > 1 select p);
		}

		[Test]
		public void CheckCondition1([DataSources] string context)
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

		[Test]
		public void CheckCondition2([DataSources] string context)
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

		[Test]
		public void CompareObject1([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child where ch == child select ch,
					from ch in db.Child where ch == child select ch);
		}

		[Test]
		public void CompareObject2([DataSources] string context)
		{
			var parent = (from p in Parent where p.ParentID == 2 select p).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where parent == p select p,
					from p in db.Parent where parent == p select p);
		}

		[Test]
		public void CompareObject3([DataSources] string context)
		{
			var child = (from ch in Child where ch.ParentID == 2 select ch).First();

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in Child where ch != child select ch,
					from ch in db.Child where ch != child select ch);
		}

		[Test]
		public void OrAnd([DataSources] string context)
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

		[Test]
		public void NotOrAnd([DataSources] string context)
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

		[Test]
		public void AndOr([DataSources] string context)
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

		[Test]
		public void Contains1([DataSources] string context)
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

		[Test]
		public void Contains2([DataSources] string context)
		{
			IEnumerable<int> ids = new [] { 2, 3 };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where ids.Contains(p.ParentID) select p,
					from p in db.Parent where ids.Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds()
		{
			yield return 1;
			yield return 2;
		}

		[Test]
		public void Contains3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where GetIds().Contains(p.ParentID) select p,
					from p in db.Parent where GetIds().Contains(p.ParentID) select p);
		}

		static IEnumerable<int> GetIds(int start, int n)
		{
			for (int i = 0; i < n; i++)
				yield return start + i;
		}

		[Test]
		public void Contains4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p,
					from p in db.Parent where GetIds(1, 2).Contains(p.ParentID) || GetIds(3, 0).Contains(p.ParentID) select p);
		}

		[Test]
		public void Contains5([DataSources] string context)
		{
			IEnumerable<int> ids = [];

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where !ids.Contains(p.ParentID) select p,
					from p in db.Parent where !ids.Contains(p.ParentID) select p);
		}

		[Test]
		public void AliasTest1([DataSources] string context)
		{
			int user = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent where p.ParentID == user select p,
					from p in db.Parent where p.ParentID == user select p);
		}

		[Test]
		public void AliasTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_ => _.ParentID == 3),
					db.Parent.Where(_ => _.ParentID == 3));
		}

		[Test]
		public void AliasTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(_p => _p.ParentID == 3),
					db.Parent.Where(_p => _p.ParentID == 3));
		}

		[Test]
		public void AliasTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(тбл => тбл.ParentID == 3),
					db.Parent.Where(тбл => тбл.ParentID == 3));
		}

		[Test]
		public void AliasTest5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Where(p_ => p_.ParentID == 3),
					db.Parent.Where(p_ => p_.ParentID == 3));
		}

		[Test]
		public void SelectNestedCalculatedTest([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from r in from o in dd.Order select o.Freight * 1000 where r > 100000 select r / 1000,
					from r in from o in db.Order select o.Freight * 1000 where r > 100000 select r / 1000);
			}
		}

		[Test]
		public void CheckField1([DataSources] string context)
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

		[Test]
		public void CheckField2([DataSources] string context)
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

		[Test]
		public void CheckField3([DataSources] string context)
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

		[Test]
		public void CheckField4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1),
					db.Parent.Select(p => new { p }).Where(p => p.p.ParentID == 1));
		}

		[Test]
		public void CheckField5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1),
					db.Parent.Select(p => new { Value = p.Value1 + 1, p }).Where(p => p.Value == 2 && p.p.ParentID == 1));
		}

		[Test]
		public void CheckField6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p },
					from p in db.Parent
					select new { p, Value = p.Value1 * 100 } into p
					where p.p.ParentID == 1 && p.Value > 0 select new { p.p.Value1, p.Value, p.p, p1 = p });
		}

		[YdbTableNotFound]
		[Test]
		public void SubQuery1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Types
						select new { Value = Math.Round(p.MoneyValue, 2) } into pp
						where pp.Value != 0 && pp.Value != 7
						select pp.Value;

				if (context.IsAnyOf(ProviderName.DB2))
					q = q.AsQueryable().Select(t => Math.Round(t, 2));

				AreEqual(
					from p in Types
					select new { Value = Math.Round(p.MoneyValue, 2) } into pp
					where pp.Value != 0 && pp.Value != 7
					select pp.Value,
					q);
			}
		}

		[Test]
		public void SearchCondition1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t,
					from t in db.Types
					where !t.BoolValue && t.MoneyValue > 1 && (t.SmallIntValue == 5 || t.SmallIntValue == 7 || t.SmallIntValue == 8)
					select t);
		}

		[Test]
		public void GroupBySubQquery1([DataSources(TestProvName.AllClickHouse)] string context)
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

		[YdbCteAsSource]
		[Test]
		public void GroupBySubQquery2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => qry2.Contains(x.ChildID));

				var xx = qry22.ToArray();

				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => qry1.Contains(x.ChildID));

				AreEqual(qry12, qry22);
			}
		}

		[YdbCteAsSource]
		[Test]
		public void GroupBySubQquery2In([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p1    = Child;
				var qry1  = p1.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry12 = p1.Where(x => x.ChildID.In(qry1));

				var p2    = db.Child;
				var qry2  = p2.GroupBy(x => x.ParentID).Select(x => x.Max(y => y.ChildID));
				var qry22 = p2.Where(x => x.ChildID.In(qry2));

				AreEqual(qry12, qry22);
			}
		}

		[Test]
		public void HavingTest1([DataSources] string context)
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
						.Where(c => c.Count() > 1)
						.Select(g => new { count = g.Count() }));
			}
		}

		[Test]
		public void HavingTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Select(g => new { count = g.Count() })
						.Where(c => c.count > 1),
					db.Child
						.GroupBy(c => c.ParentID)
						.Select(g => new { count = g.Count() })
						.Having(c => c.count > 1)
						.Where(c => c.count > 1));
			}
		}

		[Test]
		public void HavingTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					Child
						.GroupBy(c => c.ParentID)
						.Where(c => c.Key > 1 && c.Count() > 1)
						.Select(g => g.Count()),
					db.Child
						.GroupBy(c => c.ParentID)
						.Where(c => c.Key > 1 && c.Count() > 1)
						.Having(c => c.Key > 1)
						.Select(g => g.Count()));
			}
		}

		[Test]
		public void WhereDateTimeTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue > new DateTime(2009, 1, 1))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					GetTypes(context)
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Types2
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _),
					db.Types2
						.Where(_ => _.DateTimeValue == new DateTime(2009, 9, 27))
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					GetTypes(context)
						.Where(_ => _.DateTimeValue.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _),
					db.Types
						.Where(_ => _.DateTimeValue.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _));
			}
		}

		[Test]
		public void WhereDateTimeTest6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   AdjustExpectedData(db, Types2
						.Where(_ => _.DateTimeValue!.Value.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _)),
					db.Types2
						.Where(_ => _.DateTimeValue!.Value.Date == new DateTime(2009, 9, 20).Date)
						.Select(_ => _));
			}
		}

		sealed class WhereCases
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "smallint")]
			public bool BoolValue { get; set; }
			[Column]
			[Column(Configuration = ProviderName.DB2, DbType = "smallint")]
			public bool? NullableBoolValue { get; set; }

			public static readonly IEqualityComparer<WhereCases> Comparer = ComparerBuilder.GetEqualityComparer<WhereCases>();
		}

		[Test]
		public void WhereBooleanTest2([DataSources(TestProvName.AllSybase, TestProvName.AllFirebird)] string context)
		{
			void AreEqualLocal(IEnumerable<WhereCases> expected, IQueryable<WhereCases> actual, Expression<Func<WhereCases, bool>> predicate)
			{
				var exp = expected.Where(predicate.CompileExpression());
				var act = actual.  Where(predicate);
				AreEqual(exp, act, WhereCases.Comparer);
				Assert.That(act.ToSqlQuery().Sql, Does.Not.Contain("<>"));

				var notPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(predicate.Body), predicate.Parameters);

				var expNot      = expected.Where(notPredicate.CompileExpression()).ToArray();
				var actNotQuery = actual.Where(notPredicate);
				var actNot      = actNotQuery.ToArray();
				AreEqual(expNot, actNot, WhereCases.Comparer);

				Assert.That(actNotQuery.ToSqlQuery().Sql, Does.Not.Contain("<>"));
			}

			void AreEqualLocalPredicate(IEnumerable<WhereCases> expected, IQueryable<WhereCases> actual, Expression<Func<WhereCases, bool>> predicate, Expression<Func<WhereCases, bool>> localPredicate)
			{
				var actualQuery = actual.Where(predicate);
				AreEqual(expected.Where(localPredicate.CompileExpression()), actualQuery, WhereCases.Comparer);
				Assert.That(actualQuery.ToSqlQuery().Sql, Does.Not.Contain("<>"));

				var notLocalPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(localPredicate.Body), localPredicate.Parameters);

				var notPredicate = Expression.Lambda<Func<WhereCases, bool>>(
					Expression.Not(predicate.Body), predicate.Parameters);

				var expNot = expected.Where(notLocalPredicate.CompileExpression()).ToArray();
				var actualNotQuery = actual.Where(notPredicate);

				var actNot = actualNotQuery.ToArray();
				AreEqual(expNot, actNot, WhereCases.Comparer);

				Assert.That(actualNotQuery.ToSqlQuery().Sql, Does.Not.Contain("<>"));
			}

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(new[]
			{
				new WhereCases { Id = 1,  BoolValue = true,  NullableBoolValue = null  },
				new WhereCases { Id = 2,  BoolValue = true,  NullableBoolValue = true  },
				new WhereCases { Id = 3,  BoolValue = true,  NullableBoolValue = null  },
				new WhereCases { Id = 4,  BoolValue = true,  NullableBoolValue = true  },
				new WhereCases { Id = 5,  BoolValue = true,  NullableBoolValue = true  },

				new WhereCases { Id = 11, BoolValue = false, NullableBoolValue = null  },
				new WhereCases { Id = 12, BoolValue = false, NullableBoolValue = false },
				new WhereCases { Id = 13, BoolValue = false, NullableBoolValue = null  },
				new WhereCases { Id = 14, BoolValue = false, NullableBoolValue = false },
				new WhereCases { Id = 15, BoolValue = false, NullableBoolValue = false },
			}))
			{
				var local = table.ToArray();

				AreEqualLocal(local, table, t => !t.BoolValue && t.Id > 0);
				AreEqualLocal(local, table, t => !(t.BoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue == true && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue != true && t.Id > 0);
				AreEqualLocal(local, table, t => t.BoolValue == false && t.Id > 0);

				AreEqualLocalPredicate(local, table,
					t => !t.NullableBoolValue!.Value && t.Id > 0,
					t => (!t.NullableBoolValue.HasValue || !t.NullableBoolValue.Value) && t.Id > 0);

				AreEqualLocal(local, table, t => !(t.NullableBoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => t.NullableBoolValue == true && t.Id > 0);

				if (!context.IsAnyOf(TestProvName.AllAccess))
				{
					AreEqualLocal(local, table, t => t.NullableBoolValue == null && t.Id > 0);
					AreEqualLocal(local, table, t => t.NullableBoolValue != null && t.Id > 0);

					AreEqualLocal(local, table, t => !(t.NullableBoolValue == null) && t.Id > 0);
					AreEqualLocal(local, table, t => !(t.NullableBoolValue != null) && t.Id > 0);
				}

				AreEqualLocal(local, table, t => (!t.BoolValue && t.NullableBoolValue != true) && t.Id > 0);
				AreEqualLocal(local, table, t => !(!t.BoolValue && t.NullableBoolValue != true) && t.Id > 0);

				AreEqualLocal(local, table, t => (!t.BoolValue && t.NullableBoolValue == false) && t.Id > 0);

				AreEqualLocal(local, table, t => !(!t.BoolValue && t.NullableBoolValue == false) && t.Id > 0);
			}
		}

		[Test]
		public void IsNullTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(nm == null)
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(nm == null)
					select new { nm });
			}
		}

		[Test]
		public void IsNullOrEmptyTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm });
			}
		}

		[Test]
		public void IsNullOrEmptyTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.FirstName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm }
					,
					from p in db.Person
					select p.FirstName into nm
					where !(string.IsNullOrEmpty(nm))
					select new { nm });
			}
		}

		[Test]
		public void LengthTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.MiddleName into nm
					where !(nm?.Length == 0)
					select new { nm }
					,
					from p in db.Person
					select p.MiddleName into nm
					where !(nm.Length == 0)
					select new { nm });
			}
		}

		[Test]
		public void LengthTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in db.Person.AsEnumerable()
					select p.FirstName into nm
					where !(nm.Length == 0)
					select new { nm }
					,
					from p in db.Person
					select p.FirstName into nm
					where !(nm.Length == 0)
					select new { nm });
			}
		}

		[Test]
		public void Issue1755Test1([DataSources] string context, [Values(1, 2)] int id, [Values] bool? flag)
		{
			using (var db = GetDataContext(context))
			{
				var results = (from c in db.Parent
							   where c.ParentID == id
								   && (!flag.HasValue || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
							   select c);

				var sql = results.ToSqlQuery().Sql;

				AreEqual(
					from c in db.Parent.AsEnumerable()
					where c.ParentID == id
						&& (!flag.HasValue || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
					select c,
					results,
					true);

				Assert.That(Regex.Matches(sql, " AND "), Has.Count.EqualTo(flag == null ? 0 : 1));
			}
		}

		[Test]
		public void Issue1755Test2([DataSources] string context, [Values(1, 2)] int id, [Values] bool? flag)
		{
			using (var db = GetDataContext(context))
			{
				var results = (from c in db.Parent
							   where c.ParentID == id
								   && (flag == null || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
							   select c);

				var sql = results.ToSqlQuery().Sql;

				AreEqual(
					from c in db.Parent.AsEnumerable()
					where c.ParentID == id
						&& (flag == null || flag.Value && c.Value1 == null || !flag.Value && c.Value1 != null)
					select c,
					results,
					true);

				Assert.That(Regex.Matches(sql, " AND "), Has.Count.EqualTo(flag == null ? 0 : 1));
			}
		}

		[YdbCteAsSource]
		[Test]
		public void ExistsSqlTest1([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Parent.Where(p => db.Child.Select(c => c.ParentID).Contains(p.ParentID + 100)).Delete();

				Assert.That(db.LastQuery!.ToLowerInvariant().Contains("iif(exists(") || db.LastQuery!.ToLowerInvariant().Contains("when exists("), Is.False);
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void ExistsSqlTest2([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.Parent.Where(p => p.Children.Any() && p.ParentID > 100).Delete();

				Assert.That(db.LastQuery!.ToLowerInvariant().Contains("iif(exists(") || db.LastQuery!.ToLowerInvariant().Contains("when exists("), Is.False);
			}
		}

		sealed class Parameter
		{
			public int Id;
		}

		[Test]
		public void OptionalObjectInCondition([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p  = new Parameter() { Id = 1};
				db.Person.Where(r => r.FirstName == (p != null ? p.Id.ToString() : null)).ToList();
				p = null;
				db.Person.Where(r => r.FirstName == (p != null ? p.Id.ToString() : null)).ToList();
				p = new Parameter() { Id = 1 };
				db.Person.Where(r => r.FirstName == (p != null ? p.Id.ToString() : null)).ToList();
			}
		}

		[Test]
		public void StringInterpolationTests([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cnt = db.Person
					.Count(p => p.LastName + ", " + p.FirstName == $"{p.LastName}, {p.FirstName}"
					            && "<" + p.LastName + ", " + p.FirstName + ">" == $"<{p.LastName}, {p.FirstName}>"
					            && "<" + p.LastName + p.FirstName + ">" == $"<{p.LastName}{p.FirstName}>"
					            && "<{p.LastName}, " + p.FirstName + " {" + p.LastName + "}" + ">" == $"<{{p.LastName}}, {p.FirstName} {{{p.LastName}}}>"
					            && "{}" + p.LastName == $"{{}}{p.LastName}"
					);

				Assert.That(cnt, Is.EqualTo(db.Person.Count()));
			}
		}

		[Test]
		[ActiveIssue("Sybase converts empty string to space and we don't plan to do anything about it for now", Configuration = TestProvName.AllSybase)]
		public void StringInterpolationTestsNullable([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Person
					select new
					{
						FirstName = $"{p.FirstName}",
						LastName  = $"{p.LastName }, {p.FirstName}",
						FullName  = $"{p.LastName  ?? ""}, {p.FirstName ?? ""} ({p.MiddleName ?? ""} + {p.MiddleName ?? ""})", // it should be more tan three expressions to avoid optimization
					} into s
					where s.FirstName != "" || s.LastName != "" || s.FullName != ""
					orderby s.FirstName, s.LastName
					select s;

				AssertQuery(query);
			}
		}

		[Test]
		[ActiveIssue("Sybase converts empty string to space and we don't plan to do anything about it for now", Configuration = TestProvName.AllSybase)]
		public void StringInterpolationCoalesce([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Person
					select new
					{
						FirstName = $"{p.FirstName ?? ""}",
						LastName  = $"{p.LastName  ?? ""}, {p.FirstName ?? ""}",
						FullName  = $"{p.LastName  ?? ""}, {p.FirstName ?? ""} ({p.MiddleName ?? ""} + {p.MiddleName ?? ""})", // it should be more tan three expressions to avoid optimization
					} into s
					where s.FirstName != "" || s.LastName != "" || s.FullName != ""
					orderby s.FirstName, s.LastName
					select s;

				AssertQuery(query);
			}
		}

		[Test]
		public void NullableBooleanConditionEvaluationTrueTests([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool? value1)
		{
			using (var db = GetDataContext(context))
			{
				Assert.That(db.Person.Where(_ => value1 == true).Any(), Is.EqualTo(value1 == true));
			}
		}

		[Test]
		public void NullableBooleanConditionEvaluationTrueTestsNot([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool? value1)
		{
			using (var db = GetDataContext(context))
			{
				Assert.That(db.Person.Where(_ => !(value1 == true)).Any(), Is.EqualTo(!(value1 == true)));
			}
		}

		[Test]
		public void NullableBooleanConditionEvaluationFalseTests([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse, TestProvName.AllSqlServer)] string context, [Values] bool? value1)
		{
			using (var db = GetDataContext(context))
			{
				Assert.That(db.Person.Where(_ => value1 == false).Any(), Is.EqualTo(value1 == false));
			}
		}

		[Test]
		public void NullableBooleanConditionEvaluationFalseTestsNot([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool? value1)
		{
			using (var db = GetDataContext(context))
			{
				Assert.That(db.Person.Where(_ => !(value1 == false)).Any(), Is.EqualTo(!(value1 == false)));
			}
		}

		[Test]
		public void BinaryComparisonTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => (_.FirstName == _.FirstName) == (_.MiddleName != _.LastName)).Any();
			}
		}

		[Test]
		public void BinaryComparisonTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => (_.FirstName == _.FirstName) != (_.MiddleName != _.LastName)).Any();
			}
		}

		sealed class ComplexPredicate
		{
			[PrimaryKey] public int Id { get; set; }
			public string? Value { get; set; }

			public static ComplexPredicate[] Data =
			[
				new ComplexPredicate() { Id = 1 },
				new ComplexPredicate() { Id = 2, Value = "other" },
				new ComplexPredicate() { Id = 3, Value = "123" },
				new ComplexPredicate() { Id = 4, Value = "test" },
				new ComplexPredicate() { Id = 5, Value = "1" },
			];
		}

		[Test]
		public void ComplexIsNullPredicateTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(ComplexPredicate.Data);

			var query = tb.OrderBy(r => r.Id).Where(r => (r.Value == "123") == (ComplexIsNullPredicateTestFunc(r.Value) == "test"));

			AssertQuery(query);
		}

		[ExpressionMethod(nameof(ComplexIsNullPredicateTestFuncExpr))]
		private static string? ComplexIsNullPredicateTestFunc(string? value) => throw new NotImplementedException();

		private static Expression<Func<string?, string?>> ComplexIsNullPredicateTestFuncExpr()
		{
			return value => value == "1" ? "test" : value;
		}

		sealed class WhereWithBool
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public bool BoolValue { get; set; }
		}

		[Test]
		public void BooleanSubquery([DataSources] string context)
		{
			//TODO: Store in SelectQuery IsSingleRecord information, to allow optimizer moving CrossApply to SubQuery Column

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<WhereWithBool>(new List<WhereWithBool>(){new WhereWithBool()
			{
				Id = 1,
				BoolValue = true
			}}))
			{
				var query =
					from t in table
					where table.Single(x => x.Id == 1).BoolValue
					select t;

				var result = query.ToArray();
			}
		}

		sealed class WhereWithString
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? StringValue { get; set; }
		}

		[Test]
		public void CaseOptimization([DataSources] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(new List<WhereWithString>{new()
			{
				Id        = 1,
				StringValue = "Str1"
			}}))
			{
				// ReSharper disable RedundantCast
				var query = table.Where(x =>
					(x.StringValue == null ? (bool?)null : (bool?)x.StringValue.Contains("Str")) == true);
				// ReSharper restore RedundantCast

				var result = query.ToArray();

				var str = query.ToSqlQuery().Sql;

				str.ShouldContain("IS NOT NULL");
			}
		}

		[Test]
		public void CaseOptimizationNullable([DataSources(TestProvName.AllSQLite)] string context, [Values(2, null)] int? filterValue)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(new List<WhereWithString>{new()
				{
					Id          = 1,
					StringValue = "Str1"
				}}))
			{
				var query = table.Where(x => filterValue.HasValue ? x.Id == filterValue : true);

				var result = query.ToArray();

				if (filterValue == null)
					result.Length.ShouldBe(1);
				else
					result.Length.ShouldBe(0);
			}
		}

		#region issue 2424
		// TODO: add test case with non-constant expected result
		sealed class Isue2424Table
		{
			[PrimaryKey               ] public int     Pk;
			[Column                   ] public int     Id;
			[Column(CanBeNull = false)] public string  StrValue = null!;
			[Column                   ] public string? StrValueNullable;

			public static readonly Isue2424Table[] Data = new[]
			{
				new Isue2424Table(){ Pk = 1, Id = 0, StrValue = "0", StrValueNullable = null },
				new Isue2424Table(){ Pk = 2, Id = 1, StrValue = "1", StrValueNullable = "1" },
				new Isue2424Table(){ Pk = 3, Id = 2, StrValue = "2", StrValueNullable = "2" },
				new Isue2424Table(){ Pk = 4, Id = 2, StrValue = "3", StrValueNullable = "3" },
				new Isue2424Table(){ Pk = 5, Id = 2, StrValue = "4", StrValueNullable = "4" },
				new Isue2424Table(){ Pk = 6, Id = 2, StrValue = "5", StrValueNullable = "5" },
			};
		}

		[Test]
		public void Issue2424([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Isue2424Table.Data);

			AssertQuery(tb.Where(i => 0 <= i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 <= i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 <= i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 <= i.StrValue.CompareTo("5")));

			AssertQuery(tb.Where(i => 0 >= i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 >= i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 >= i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 >= i.StrValue.CompareTo("5")));

			AssertQuery(tb.Where(i => 0 < i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 < i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 < i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 < i.StrValue.CompareTo("5")));

			AssertQuery(tb.Where(i => 0 > i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 > i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 > i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 > i.StrValue.CompareTo("5")));

			AssertQuery(tb.Where(i => 0 == i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 == i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 == i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 == i.StrValue.CompareTo("5")));

			AssertQuery(tb.Where(i => 0 != i.StrValue.CompareTo("0")));
			AssertQuery(tb.Where(i => 0 != i.StrValue.CompareTo("1")));
			AssertQuery(tb.Where(i => 0 != i.StrValue.CompareTo("3")));
			AssertQuery(tb.Where(i => 0 != i.StrValue.CompareTo("5")));
		}

		[Test]
		public void Issue2424Nullable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Isue2424Table.Data);

			AssertQuery(tb.Where(i => 0 <= string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 <= string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 <= string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 <= string.Compare(i.StrValueNullable, "5")));

			AssertQuery(tb.Where(i => 0 >= string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 >= string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 >= string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 >= string.Compare(i.StrValueNullable, "5")));

			AssertQuery(tb.Where(i => 0 < string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 < string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 < string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 < string.Compare(i.StrValueNullable, "5")));

			AssertQuery(tb.Where(i => 0 > string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 > string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 > string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 > string.Compare(i.StrValueNullable, "5")));

#pragma warning disable CA2251 // Use 'string.Equals'
			AssertQuery(tb.Where(i => 0 == string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 == string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 == string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 == string.Compare(i.StrValueNullable, "5")));

			AssertQuery(tb.Where(i => 0 != string.Compare(i.StrValueNullable, null)));
			AssertQuery(tb.Where(i => 0 != string.Compare(i.StrValueNullable, "1")));
			AssertQuery(tb.Where(i => 0 != string.Compare(i.StrValueNullable, "3")));
			AssertQuery(tb.Where(i => 0 != string.Compare(i.StrValueNullable, "5")));
#pragma warning restore CA2251 // Use 'string.Equals'
		}

		[Test]
		public void Issue2424Fields([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Isue2424Table.Data);
			var src = from left in tb
					  from right in tb
					  select new
					  {
						  LeftId = left.Id,
						  LeftString = left.StrValue,
						  LeftStringN = left.StrValueNullable,
						  RightId = right.Id,
						  RightString = right.StrValue,
						  RightStringN = right.StrValueNullable,
					  };

#pragma warning disable CA2251 // Use 'string.Equals'
			// NonNullable vs NonNullable
			AssertQuery(src.Where(i => 0 <= string.Compare(i.LeftString, i.RightString)));
			AssertQuery(src.Where(i => 0 >= string.Compare(i.LeftString, i.RightString)));
			AssertQuery(src.Where(i => 0 < string.Compare(i.LeftString, i.RightString)));
			AssertQuery(src.Where(i => 0 > string.Compare(i.LeftString, i.RightString)));
			AssertQuery(src.Where(i => 0 == string.Compare(i.LeftString, i.RightString)));
			AssertQuery(src.Where(i => 0 != string.Compare(i.LeftString, i.RightString)));

			// NonNullable vs Nullable
			AssertQuery(src.Where(i => 0 <= string.Compare(i.LeftString, i.RightStringN)));
			AssertQuery(src.Where(i => 0 >= string.Compare(i.LeftString, i.RightStringN)));
			AssertQuery(src.Where(i => 0 < string.Compare(i.LeftString, i.RightStringN)));
			AssertQuery(src.Where(i => 0 > string.Compare(i.LeftString, i.RightStringN)));
			AssertQuery(src.Where(i => 0 == string.Compare(i.LeftString, i.RightStringN)));
			AssertQuery(src.Where(i => 0 != string.Compare(i.LeftString, i.RightStringN)));

			// Nullable vs Nullable
			AssertQuery(src.Where(i => 0 <= string.Compare(i.LeftStringN, i.RightStringN)));
			AssertQuery(src.Where(i => 0 >= string.Compare(i.LeftStringN, i.RightStringN)));
			AssertQuery(src.Where(i => 0 < string.Compare(i.LeftStringN, i.RightStringN)));
			AssertQuery(src.Where(i => 0 > string.Compare(i.LeftStringN, i.RightStringN)));
			AssertQuery(src.Where(i => 0 == string.Compare(i.LeftStringN, i.RightStringN)));
			AssertQuery(src.Where(i => 0 != string.Compare(i.LeftStringN, i.RightStringN)));
#pragma warning restore CA2251 // Use 'string.Equals'
		}
		#endregion

		[Test]
		public void Issue1767Test1([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Parent.Where(p => p.Value1 != null && p.Value1 != 1);

				AreEqual(
					db.Parent.AsEnumerable().Where(p => p.Value1 != null && p.Value1 != 1),
					query);

				var sql = query.ToSqlQuery().Sql;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(sql, Does.Not.Contain("IS NULL"), sql);
					Assert.That(Regex.Matches(sql, "IS NOT NULL"), Has.Count.EqualTo(1), sql);
				}
			}
		}

		[Test]
		public void Issue1767Test2([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Parent.Where(p => p.Value1 == null || p.Value1 != 1);

				AreEqual(
					db.Parent.AsEnumerable().Where(p => p.Value1 == null || p.Value1 != 1),
					query);

				var sql = query.ToSqlQuery().Sql;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(Regex.Matches(sql, "IS NULL"), Has.Count.EqualTo(1), sql);
					Assert.That(sql, Does.Not.Contain("IS NOT NULL"), sql);
				}
			}
		}

		[Test]
		public void Issue_SubQueryFilter1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var filter1 = "John";
			var filter2 = "Tester";

			var query = db
				.Patient
				.Where(_ =>
					db.Person
						.Where(e => e.FirstName.Contains(filter1))
						.Any(e => e.ID == db.Patient.Select(d => d.PersonID)
						.First()) ||
					db.Person
						.Where(e => e.FirstName.Contains(filter2))
						.Any(e => e.ID == db.Patient.Select(d => d.PersonID)
							.First()))
				.OrderBy(p => p.PersonID);

			AssertQuery(query);
		}

		[YdbMemberNotFound]
		[Test]
		public void Issue_SubQueryFilter2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var filter1 = "John";
			var filter2 = "Tester";

			var query = db
				.Patient
				.Where(p =>
					db.Person
						.Where(e => e.ID == p.PersonID && e.FirstName.Contains(filter1))
						.Any(e => e.ID == db.Patient.Select(d => d.PersonID)
						.First()) ||
					db.Person
						.Where(e => e.ID == p.PersonID && e.FirstName.Contains(filter2))
						.Any(e => e.ID == db.Patient.Select(d => d.PersonID)
							.First()))
				.OrderBy(p => p.PersonID);

			AssertQuery(query);
		}

		[Test]
		public void Issue_SubQueryFilter3([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllAccess,
			TestProvName.AllSapHana,
			TestProvName.AllFirebirdLess4,
			TestProvName.AllOracle,
			TestProvName.AllMySql57,
			TestProvName.AllSybase
			)] string context)
		{
			using var db = GetDataContext(context);

			var filter1 = "John";
			var filter2 = "Tester";

			IQueryable<Model.Patient> query = db.Patient;

			var whereParameter = Expression.Parameter(typeof(Model.Patient), "patient");

			query = query.Where(
				Expression.Lambda<Func<Model.Patient, bool>>(
					Expression.AndAlso(
						BuildFilterSubQuery(db, filter1, whereParameter),
						BuildFilterSubQuery(db, filter2, whereParameter)),
					whereParameter))
				.OrderBy(r => r.PersonID);

			AssertQuery(query);

			static Expression BuildFilterSubQuery(
				ITestDataContext db,
				string filter,
				ParameterExpression whereParameter)
			{
				// db.Person
				//    .Where(p => p.FirstName.Contains(filter))
				//    .Any(e => e.ID == subquery2.First())
				var subquery2 = BuildFilterSubQuery2(db, whereParameter);

				var subquery = db.Person.Where(p => p.FirstName.Contains(filter)).Expression;

				subquery2 = Expression.Call(
					typeof(Queryable),
					nameof(Queryable.First),
					[typeof(int)],
					subquery2);

				var anyParameter = Expression.Parameter(typeof(Model.Person), "e");

				var predicate = Expression.Lambda<Func<Model.Person, bool>>(
					Expression.Equal(
						Expression.PropertyOrField(anyParameter, nameof(Model.Person.ID)),
						subquery2),
					anyParameter);

				subquery = Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Any),
					[typeof(Model.Person)],
					subquery,
					predicate);

				return subquery;
			}

			static Expression BuildFilterSubQuery2(ITestDataContext db, ParameterExpression whereParameter)
			{
				// db.Person.Where(p => p.ID == patient.PersonID).Select(p => p.ID)
				var subquery = db.Person.Expression;

				var personParameter = Expression.Parameter(typeof(Model.Person), "d");

				var predicate = Expression.Lambda<Func<Model.Person, bool>>(
					Expression.Equal(
						Expression.PropertyOrField(personParameter, nameof(Model.Person.ID)),
						Expression.PropertyOrField(whereParameter, nameof(Model.Patient.PersonID))),
					personParameter);

				subquery = Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Model.Person)],
					subquery,
					predicate);

				var selector = Expression.Lambda<Func<Model.Person, int>>(
					Expression.PropertyOrField(personParameter, nameof(Model.Person.ID)),
					personParameter);

				subquery = Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Select),
					[typeof(Model.Person), typeof(int)],
					subquery,
					selector);

				return subquery;
			}
		}

		[YdbTableNotFound]
		[Test]
		public void Issue_Filter_Checked([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			ProviderName.SqlCe)]
			string context)
		{
			checked
			{
				using var db = GetDataContext(context);

				var query = LinqExtensions.FullJoin(
						db.Person.Select(_ => (int?)123).Take(1).GroupBy(_ => _!).Select(_ => new { _.Key, Count = _.Count() }),
						db.Person.Select(_ => ((int?)null)!).Where(_ => false).GroupBy(_ => _!).Select(_ => new { _.Key, Count = _.Count() }),
						(a1, a2) => a1.Count == a2.Count,
						(a1, a2) => new { LeftCount = (int?)a1.Count, RightCount = (int?)a2.Count })
					.Where(_ => _.LeftCount == null || _.RightCount == null);

				// crashes
				//AssertQuery(query);
				query.ToList();
			}
		}

		[YdbCteAsSource]
		[Test]
		public void Issue_CompareQueries1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Where(p => new int[] { 1, 2 }.Contains(p.ID)).Select(p => p.ID);
			var query2 = db.Person.Where(p => new int[0].Contains(p.ID)).Select(p => p.ID);

			var result1 = query1.Where(rec => !query2.Contains(rec)).Select(p => Sql.Ext.Count(p, Sql.AggregateModifier.None).ToValue()).Single() == 0;
			var result2 = query2.Where(rec => !query1.Contains(rec)).Select(p => Sql.Ext.Count(p, Sql.AggregateModifier.None).ToValue()).Single() == 0;

			Assert.That(result1 && result2, Is.False);
		}

		[YdbCteAsSource]
		[Test]
		public void Issue_CompareQueries2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = db.Person.Where(p => new int[] { 1, 2 }.Contains(p.ID)).Select(p => p.ID);
			var query2 = db.Person.Where(p => new int[] { 3 }.Contains(p.ID)).Select(p => p.ID);

			var result1 = query1.Where(rec => !query2.Contains(rec)).Select(p => Sql.Ext.Count(p, Sql.AggregateModifier.None).ToValue()).Single() == 0;
			var result2 = query2.Where(rec => !query1.Contains(rec)).Select(p => Sql.Ext.Count(p, Sql.AggregateModifier.None).ToValue()).Single() == 0;

			Assert.That(result1 && result2, Is.False);
		}

		#region Issue 2667
		[Table("LinkedContracts", IsColumnAttributeRequired = false)]
		public class LinkedContractsRaw
		{
			public int Id { get; set; }
			public int FK { get; set; }

			public static readonly LinkedContractsRaw[] Data = new []
			{
				new LinkedContractsRaw() { Id = 11, FK = 1 },
				new LinkedContractsRaw() { Id = 22, FK = 2 }
			};
		}

		[Table("Contract", IsColumnAttributeRequired = false)]
		public class ContractRaw
		{
			public int Id { get; set; }
			public bool? Bit01 { get; set; }

			public static readonly ContractRaw[] Data = new []
			{
				new ContractRaw() { Id = 1 },
				new ContractRaw() { Id = 2 }
			};
		}

		[Table("LinkedContracts", IsColumnAttributeRequired = false)]
		public class LinkedContracts
		{
			public int Id { get; set; }

			public int FK { get; set; }

			[Association(ThisKey = nameof(FK), OtherKey = nameof(Contract.Id))]
			public Contract? Ref { get; set; }
		}

		[Table("Contract", IsColumnAttributeRequired = false)]
		public class Contract
		{
			[Nullable] public bool Bit01 { get; set; }

			public int Id { get; set; }
		}

		[Test]
		public void Issue2667Test1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(ContractRaw.Data))
			using (var linkedContracts = db.CreateLocalTable(LinkedContractsRaw.Data))
			{
				var linkedContract = db.GetTable<LinkedContracts>()
					.LoadWith(linked => linked.Ref)
					.Where(linked => linked.FK == 1)
					.ToList();

				Assert.That(linkedContract[0].Ref, Is.Not.Null);
			}
		}

		[Test]
		public void Issue2667Test2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (var linkedContracts = db.CreateLocalTable(LinkedContractsRaw.Data))
			using (db.CreateLocalTable(ContractRaw.Data))
			{
				var result = db.GetTable<LinkedContracts>()
					.Where(linked => linked.FK == 1)
					.Select(verträge => verträge.Ref)
					.ToList();

				Assert.That(result[0], Is.Not.Null);
			}
		}
		#endregion

		[Test]
		public void Issue2897_ParensGeneration_Or([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Parent.Where(p => p.ParentID > 1 || p.ParentID > 2 || p.ParentID > 3).ToList();

				var sql = db.LastQuery!;
				Assert.That(sql, Does.Not.Contain("("), sql);
				Assert.That(sql, Does.Not.Contain(")"), sql);
			}
		}

		[Test]
		public void Issue2897_ParensGeneration_And([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Parent.Where(p => p.ParentID > 1 && p.ParentID > 2 && p.ParentID > 3).ToList();

				var sql = db.LastQuery!;
				Assert.That(sql, Does.Not.Contain("("), sql);
				Assert.That(sql, Does.Not.Contain(")"), sql);
			}
		}

		[Test]
		public void Issue2897_ParensGeneration_MixedFromAnd([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Parent
					.Where(p => p.ParentID > 1 && p.ParentID > 2 && (p.ParentID > 3 || p.ParentID > 4) && (p.ParentID > 5 || p.ParentID > 6 || p.ParentID > 7) && p.ParentID > 8 && p.ParentID > 9 && p.ParentID > 10 && (p.ParentID > 11 || p.ParentID > 12))
					.ToList();

				CompareSql(@"SELECT
		p.ParentID,
		p.Value1
	FROM
		Parent p
	WHERE
		p.ParentID > 1 AND
		p.ParentID > 2 AND
		(p.ParentID > 3 OR p.ParentID > 4) AND
		(p.ParentID > 5 OR p.ParentID > 6 OR p.ParentID > 7) AND
		p.ParentID > 8 AND
		p.ParentID > 9 AND
		p.ParentID > 10 AND
		(p.ParentID > 11 OR p.ParentID > 12)", db.LastQuery!.Replace("\"", "").Replace("[", "").Replace("]", "").Replace("`", ""));
			}
		}

		[Test]
		public void Issue2897_ParensGeneration_MixedFromOr([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				db.Parent
					.Where(p => (p.ParentID > 1 || p.ParentID > 2) && (p.ParentID > 3 || p.ParentID > 4) && (p.ParentID > 5 || p.ParentID > 6 || p.ParentID > 7) && p.ParentID > 8 && p.ParentID > 9 && p.ParentID > 10 && (p.ParentID > 11 || p.ParentID > 12) && p.ParentID > 13)
					.ToList();

				CompareSql(@"SELECT
		p.ParentID,
		p.Value1
	FROM
		Parent p
	WHERE
		(p.ParentID > 1 OR p.ParentID > 2) AND
		(p.ParentID > 3 OR p.ParentID > 4) AND
		(p.ParentID > 5 OR p.ParentID > 6 OR p.ParentID > 7) AND
		p.ParentID > 8 AND
		p.ParentID > 9 AND
		p.ParentID > 10 AND
		(p.ParentID > 11 OR p.ParentID > 12) AND
		p.ParentID > 13", db.LastQuery!.Replace("\"", "").Replace("[", "").Replace("]", "").Replace("`", ""));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1662")]
		public void Boolean_NotFalse_AsTrue1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			db.Types.Where(r => r.BoolValue != false).ToList();

			Assert.That(db.LastQuery, Does.Contain(" = "));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1662")]
		public void Boolean_NotFalse_AsTrue2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			db.Types.Where(r => !r.BoolValue).ToList();

			if (context.IsAnyOf(TestProvName.AllPostgreSQL, TestProvName.AllFirebird3Plus, TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllDB2, TestProvName.AllClickHouse, TestProvName.AllAccess, TestProvName.AllInformix, ProviderName.Ydb))
			{
				Assert.That(db.LastQuery, Does.Not.Contain(" = "));
				Assert.That(db.LastQuery, Does.Contain("NOT "));
			}
			else
			{
				Assert.That(db.LastQuery, Does.Not.Contain("NOT "));
				Assert.That(db.LastQuery, Does.Contain(" = "));
			}
		}

		[Table]
		sealed class NullableBool
		{
			[PrimaryKey] public int   ID   { get; set; }
			[Column] public bool? Bool { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1662")]
		public void NullableBoolean_NotFalse_AsNotTrue([DataSources(false, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<NullableBool>();

			tb.Where(r => r.Bool != false).ToList();

			Assert.That(db.LastQuery, Does.Contain(" = "));
			Assert.That(db.LastQuery, Does.Contain("IS NULL"));
		}

		[Test]
		public void PredicateOptimization_Exception([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			int? p1 = 1;
			int? p2 = 2;
			int? p3 = 3;
			int? p4 = null;

			_ = db.Person.Where(
				p =>
						(p1.HasValue && p1.Value == 6 && p2.HasValue && p2.Value == p.ID)

						|| (p1.HasValue && p1.Value == 5 && p3.HasValue && p3.Value == p.ID)

						|| (p1.HasValue
							&& p1.Value == 7
							&& ((p.FirstName == p.LastName && p.FirstName == p.MiddleName) || (p.FirstName == p.MiddleName && p.FirstName == p.FirstName)))

						|| (p1.HasValue && p1.Value == 8
							&& ((p.FirstName == p.LastName && p.FirstName == p.MiddleName) || (p.FirstName == p.MiddleName && p.FirstName == p.FirstName)))

						|| (p1.HasValue && p1.Value == 2
							&& ((p.FirstName == p.LastName && p.FirstName == p.MiddleName) || (p.FirstName == p.MiddleName && p.FirstName == p.FirstName)))

						|| (p1.HasValue && p1.Value == 3)

						|| (p1.HasValue && p1.Value == 4
							&& ((p.FirstName == p.LastName && p.FirstName == p.MiddleName) || (p.FirstName == p.MiddleName && p.FirstName == p.FirstName)))

						|| (p1.HasValue && p1.Value == 1
							&& ((p4 <= p.ID && p.ID <= p4) || (p4 <= p.ID && p.ID <= p4))))
				.ToArray();
		}

		[Test]
		public void PredicateOptimization_SimilarInSearch1([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var noPersons = db.Person.Where(x => x.ID > 3 && (x.FirstName == "John" || x.FirstName == "Jane"));
			var specificNoPersons = noPersons.Where(x => x.FirstName == "Jane");

			AssertQuery(specificNoPersons);
			AssertQuery(noPersons);
		}

		[Test]
		public void PredicateOptimization_SimilarInSearch2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var noPersons         = db.Person.Where(x => (x.FirstName == "John" || x.FirstName == "Jane") && x.ID > 3);
			var specificNoPersons = noPersons.Where(x => x.FirstName == "Jane");

			AssertQuery(specificNoPersons);
			AssertQuery(noPersons);
		}

		class WithMultipleDates
		{
			[PrimaryKey] public int PK { get; set; }

			public int? Id { get; set; }

			public DateTime? Date1 { get; set; }
			public DateTime? Date2 { get; set; }
			public DateTime? Date3 { get; set; }
			public DateTime? Date4 { get; set; }
		}

		[Test]
		public void PredicateOptimization_Subquery([DataSources(
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllAccess,
			TestProvName.AllMariaDB,
			TestProvName.AllMySql57,
			TestProvName.AllDB2,
			// yep, it works in older versions...
			TestProvName.AllFirebird5Plus,
			TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			using var tb = db.CreateLocalTable(new[]
			{
				new WithMultipleDates
				{
					PK    = 1,
					Id    = 1,
					Date1 = new DateTime(2023, 1, 1),
					Date2 = new DateTime(2023, 1, 2),
					Date3 = new DateTime(2023, 1, 3),
					Date4 = new DateTime(2023, 1, 4)
				},
				new WithMultipleDates
				{
					PK    = 2,
					Id    = 2,
					Date1 = new DateTime(2023, 2, 1),
					Date2 = new DateTime(2023, 2, 2),
					Date3 = new DateTime(2023, 2, 3),
					Date4 = new DateTime(2023, 2, 4)
				},
				new WithMultipleDates
				{
					PK    = 3,
					Id    = null,
					Date1 = null,
					Date2 = null,
					Date3 = null,
					Date4 = null
				}
			});

			var query1 =
				from p in tb
				where new[] { p.Date1, p.Date2, p.Date3, p.Date4 }.Max() > new DateTime(2023, 1, 1)
				select p;

			var query2 =
				from p in tb
				where !(new[] { p.Date1, p.Date2, p.Date3, p.Date4 }.Max() > p.Date1)
				select p;

			var result1 = query1.ToArray();
			var result2 = query2.ToArray();
		}

	}
}
