using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Association : TestBase
	{
		[Test]
		public void Test1()
		{
			ForEachProvider(db => AreEqual(
				from ch in    Child where ch.ParentID == 1 select new { ch, ch.Parent },
				from ch in db.Child where ch.ParentID == 1 select new { ch, ch.Parent }));
		}

		[Test]
		public void Test2()
		{
			var expected =
				from p  in Parent
				from ch in p.Children
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID };

			ForEachProvider(db => AreEqual(expected,
				from p  in db.Parent
				from ch in p.Children
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID }));
		}

		[Test]
		public void Test3()
		{
			var expected =
				from p  in Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID };

			ForEachProvider(db => AreEqual(expected,
				from p  in db.Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID }));
		}

		[Test]
		public void Test4()
		{
			var expected =
				from p  in Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID, ch.ChildID };

			ForEachProvider(db => AreEqual(expected,
				from p  in db.Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID, ch.ChildID }));
		}

		[Test]
		public void Test5()
		{
			var expected =
				from p  in Parent
				from ch in p.Children2
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID };

			ForEachProvider(db => AreEqual(expected,
				from p  in db.Parent
				from ch in p.Children2
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID }));
		}

		[Test]
		public void SelectMany1()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.Select(ch => p)),
				db.Parent.SelectMany(p => p.Children.Select(ch => p))));
		}

		[Test]
		public void SelectMany2()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p =>    Child.Select(ch => p)),
				db.Parent.SelectMany(p => db.Child.Select(ch => p))));
		}

		[Test]
		public void SelectMany3()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				Child
					.GroupBy(ch => ch.Parent)
					.Where(g => g.Count() > 2)
					.SelectMany(g => g.Select(ch => ch.Parent)),
				db.Child
					.GroupBy(ch => ch.Parent)
					.Where(g => g.Count() > 2)
					.SelectMany(g => g.Select(ch => ch.Parent))));
		}

		[Test]
		public void SelectMany4()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				Child
					.GroupBy(ch => ch.Parent)
					.Where(g => g.Count() > 2)
					.SelectMany(g => g.Select(ch => ch.Parent.ParentID)),
				db.Child
					.GroupBy(ch => ch.Parent)
					.Where(g => g.Count() > 2)
					.SelectMany(g => g.Select(ch => ch.Parent.ParentID))));
		}

		[Test]
		public void SelectMany5()
		{
			ForEachProvider(db => AreEqual(
				   Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)),
				db.Parent.SelectMany(p => p.Children.Select(ch => p.ParentID))));
		}

		[Test]
		public void LeftJoin1()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c },
				from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c }));
		}

		[Test]
		public void LeftJoin2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p },
				from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p }));
		}

		[Test]
		public void GroupBy1()
		{
			ForEachProvider(db => AreEqual(
				from ch in    Child group ch by ch.Parent into g select g.Key,
				from ch in db.Child group ch by ch.Parent into g select g.Key));
		}

		[Test]
		public void GroupBy2()
		{
			ForEachProvider(db => AreEqual(
				(from ch in    Child group ch by ch.Parent1).ToList().Select(g => g.Key),
				(from ch in db.Child group ch by ch.Parent1).ToList().Select(g => g.Key)));
		}

		[Test]
		public void GroupBy3()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent group p by p.Types.DateTimeValue.Year into g select g.Key,
				from p in db.Parent group p by p.Types.DateTimeValue.Year into g select g.Key));
		}

		[Test]
		public void GroupBy4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Types group p by p.DateTimeValue.Year into g select g.Key,
				from p in db.Types group p by p.DateTimeValue.Year into g select g.Key));
		}

		[Test]
		public void EqualsNull1()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from employee in    Employee where employee.ReportsToEmployee != null select employee.EmployeeID,
					from employee in db.Employee where employee.ReportsToEmployee != null select employee.EmployeeID);
		}

		[Test]
		public void EqualsNull2()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from employee in    Employee where employee.ReportsToEmployee != null select employee, 
					from employee in db.Employee where employee.ReportsToEmployee != null select employee);
		}

		[Test]
		public void EqualsNull3()
		{
			using (var db = new NorthwindDB())
				AreEqual(
					from employee in    Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee },
					from employee in db.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee });
		}

		[Test]
		public void StackOverflow1()
		{
			using (var db = new NorthwindDB())
				Assert.AreEqual(
					(from employee in    Employee where employee.Employees.Count > 0 select employee).FirstOrDefault(),
					(from employee in db.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault());
		}

		[Test]
		public void StackOverflow2()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from p in    Parent5 where p.Children.Count != 0 select p,
				from p in db.Parent5 where p.Children.Count != 0 select p));
		}

		[Test]
		public void StackOverflow3()
		{
			ForEachProvider(new[] { ProviderName.SqlCe },
				db => AreEqual(
					from p in    Parent5 where p.Children.Count() != 0 select p,
					from p in db.Parent5 where p.Children.Count() != 0 select p));
		}

		[Test]
		public void StackOverflow4()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from p in    Parent5 select new { p.Children.Count },
				from p in db.Parent5 select new { p.Children.Count }));
		}

		[Test]
		public void DoulbeJoin()
		{
			ForEachProvider(db => AreEqual(
				from g in    GrandChild where g.Child.Parent.Value1 == 1 select g,
				from g in db.GrandChild where g.Child.Parent.Value1 == 1 select g));
		}

		[Test]
		public void Projection1()
		{
			ForEachProvider(db => AreEqual(
				from c in
					from c in Child
					where c.Parent.ParentID == 2
					select c
				join g in GrandChild on c.ParentID equals g.ParentID
				where g.ChildID == 22
				select new { c.Parent, c },
				from c in
					from c in db.Child
					where c.Parent.ParentID == 2
					select c
				join g in db.GrandChild on c.ParentID equals g.ParentID
				where g.ChildID == 22
				select new { c.Parent, c }));
		}

		[TableName("Parent")]
		public class Top
		{
			public int  ParentID;
			public int? Value1;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public Middle Middle { get; set; }
		}

		[TableName("Child")]
		public class Middle
		{
			[PrimaryKey] public int ParentID;
			[PrimaryKey] public int ChildID;

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
			public Bottom Bottom { get; set; }

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = true)]
			public Bottom Bottom1 { get; set; }
		}

		[TableName("GrandChild")]
		public class Bottom
		{
			public int ParentID;
			public int ChildID;
			public int GrandChildID;
		}

		[Test]
		public void TestTernary1()
		{
			var ids = new[] { 1, 5 };

			ForEachProvider(
				new[] { ProviderName.SQLite, ProviderName.Access },
				db =>
				{
					var q =
						from t in db.GetTable<Top>()
						where ids.Contains(t.ParentID)
						orderby t.ParentID
						select t.Middle == null ? null : t.Middle.Bottom;

					var list = q.ToList();

					Assert.NotNull(list[0]);
					Assert.Null   (list[1]);
				});
		}

		[Test]
		public void TestTernary2()
		{
			var ids = new[] { 1, 5 };

			ForEachProvider(
				new[] { ProviderName.SQLite, ProviderName.Access },
				db =>
				{
					var q =
						from t in db.GetTable<Top>()
						where ids.Contains(t.ParentID)
						orderby t.ParentID
						select t.Middle.Bottom;

					var list = q.ToList();

					Assert.NotNull(list[0]);
					Assert.Null   (list[1]);
				});
		}

		[Test]
		public void TestTernary3()
		{
			var ids = new[] { 1, 5 };

			ForEachProvider(db =>
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle.Bottom1;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			});
		}

		[TableName("Child")]
		[InheritanceMapping(Code = 1, IsDefault = true, Type = typeof(ChildForHeirarhy))]
		public class ChildBaseForHeirarhy
		{
			[MapField(IsInheritanceDiscriminator = true)]
			public int ChildID { get; set; }
		}

		public class ChildForHeirarhy : ChildBaseForHeirarhy
		{
			public int ParentID { get; set; }
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public Parent Parent { get; set; }
		}

		[Test]
		public void AssociationInHeirarhy()
		{
			ForEachProvider(db =>
			{
				db.GetTable<ChildBaseForHeirarhy>()
					.OfType<ChildForHeirarhy>()
					.Select(ch => new ChildForHeirarhy { Parent = ch.Parent })
					.ToList();
			});
		}
	}
}
