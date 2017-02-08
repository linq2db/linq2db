using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

#pragma warning disable 472 // The result of the expression is always the same since a value of this type is never equal to 'null'

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class AssociationTests : TestBase
	{
		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch.ParentID == 1 select new { ch, ch.Parent },
					from ch in db.Child where ch.ParentID == 1 select new { ch, ch.Parent });
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p  in Parent
					from ch in p.Children
					where ch.ParentID < 4 || ch.ParentID >= 4
					select new { p.ParentID, ch.ChildID }
					,
					from p  in db.Parent
					from ch in p.Children
					where ch.ParentID < 4 || ch.ParentID >= 4
					select new { p.ParentID, ch.ChildID });
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p  in Parent
					from ch in p.Children
					where p.ParentID < 4 || p.ParentID >= 4
					select new { p.ParentID }
					,
					from p  in db.Parent
					from ch in p.Children
					where p.ParentID < 4 || p.ParentID >= 4
					select new { p.ParentID });
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p  in Parent
					from ch in p.Children
					where p.ParentID < 4 || p.ParentID >= 4
					select new { p.ParentID, ch.ChildID }
					,
					from p  in db.Parent
					from ch in p.Children
					where p.ParentID < 4 || p.ParentID >= 4
					select new { p.ParentID, ch.ChildID });
		}

		[Test, DataContextSource]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p  in Parent
					from ch in p.Children2
					where ch.ParentID < 4 || ch.ParentID >= 4
					select new { p.ParentID, ch.ChildID }
					,
					from p  in db.Parent
					from ch in p.Children2
					where ch.ParentID < 4 || ch.ParentID >= 4
					select new { p.ParentID, ch.ChildID });
		}

		[Test, DataContextSource]
		public void SelectMany1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(ch => p)),
					db.Parent.SelectMany(p => p.Children.Select(ch => p)));
		}

		[Test, DataContextSource]
		public void SelectMany2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(ch => p)),
					db.Parent.SelectMany(p => db.Child.Select(ch => p)));
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void SelectMany3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent)),
					db.Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent)));
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void SelectMany4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent.ParentID)),
					db.Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent.ParentID)));
		}

		[Test, DataContextSource]
		public void SelectMany5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)),
					db.Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)));
		}

		[Test, DataContextSource]
		public void LeftJoin1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c },
					from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c });
		}

		[Test, DataContextSource]
		public void LeftJoin2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p },
					from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p });
		}

		[Test, DataContextSource]
		public void GroupBy1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child group ch by ch.Parent into g select g.Key,
					from ch in db.Child group ch by ch.Parent into g select g.Key);
		}

		[Test, DataContextSource]
		public void GroupBy2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child group ch by ch.Parent1).ToList().Select(g => g.Key),
					(from ch in db.Child group ch by ch.Parent1).ToList().Select(g => g.Key));
		}

		[Test, DataContextSource]
		public void GroupBy3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent group p by p.Types.DateTimeValue.Year into g select g.Key,
					from p in db.Parent group p by p.Types.DateTimeValue.Year into g select g.Key);
		}

		[Test, DataContextSource]
		public void GroupBy4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types group p by p.DateTimeValue.Year into g select g.Key,
					from p in db.Types group p by p.DateTimeValue.Year into g select g.Key);
		}

		[Test, NorthwindDataContext]
		public void EqualsNull1(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select employee.EmployeeID,
					from employee in db.Employee where employee.ReportsToEmployee != null select employee.EmployeeID);
			}
		}

		[Test, NorthwindDataContext]
		public void EqualsNull2(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select employee, 
					from employee in db.Employee where employee.ReportsToEmployee != null select employee);
			}
		}

		[Test, NorthwindDataContext]
		public void EqualsNull3(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee },
					from employee in db.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee });
			}
		}

		[Test, NorthwindDataContext]
		public void StackOverflow1(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				Assert.AreEqual(
					(from employee in dd.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault(),
					(from employee in db.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault());
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void StackOverflow2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count != 0 select p,
					from p in db.Parent5 where p.Children.Count != 0 select p);
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void StackOverflow3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count() != 0 select p,
					from p in db.Parent5 where p.Children.Count() != 0 select p);
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void StackOverflow4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 select new { p.Children.Count },
					from p in db.Parent5 select new { p.Children.Count });
		}

		[Test, DataContextSource]
		public void DoubleJoin(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from g in    GrandChild where g.Child.Parent.Value1 == 1 select g,
					from g in db.GrandChild where g.Child.Parent.Value1 == 1 select g);
		}

		[Test, DataContextSource]
		public void Projection1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in
						from c in Child
						where c.Parent.ParentID == 2
						select c
					join g in GrandChild on c.ParentID equals g.ParentID
					where g.ChildID == 22
					select new { c.Parent, c }
					,
					from c in
						from c in db.Child
						where c.Parent.ParentID == 2
						select c
					join g in db.GrandChild on c.ParentID equals g.ParentID
					where g.ChildID == 22
					select new { c.Parent, c });
		}

		[Table("Parent")]
		public class Top
		{
			[Column] public int  ParentID;
			[Column] public int? Value1;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public Middle Middle { get; set; }
		}

		[Table(Name="Child")]
		public class Middle
		{
			[PrimaryKey] public int ParentID;
			[PrimaryKey] public int ChildID;

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
			public Bottom Bottom { get; set; }

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = true)]
			public Bottom Bottom1 { get; set; }
		}

		[Table("GrandChild", IsColumnAttributeRequired=false)]
		public class Bottom
		{
			public int ParentID;
			public int ChildID;
			public int GrandChildID;
		}

		[Test, DataContextSource(ProviderName.SQLite, ProviderName.Access, TestProvName.SQLiteMs)]
		public void TestTernary1(string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle == null ? null : t.Middle.Bottom;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Test, DataContextSource(ProviderName.SQLite, ProviderName.Access, TestProvName.SQLiteMs)]
		public void TestTernary2(string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle.Bottom;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Test, DataContextSource]
		public void TestTernary3(string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle.Bottom1;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Table(Name="Child", IsColumnAttributeRequired=false)]
		[InheritanceMapping(Code = 1, IsDefault = true, Type = typeof(ChildForHeirarhy))]
		public class ChildBaseForHeirarhy
		{
			[Column(IsDiscriminator = true)]
			public int ChildID { get; set; }
		}

		public class ChildForHeirarhy : ChildBaseForHeirarhy
		{
			public int ParentID { get; set; }
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public Parent Parent { get; set; }
		}

		[Test, DataContextSource]
		public void AssociationInHeirarhy(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<ChildBaseForHeirarhy>()
					.OfType<ChildForHeirarhy>()
					.Select(ch => new ChildForHeirarhy { Parent = ch.Parent })
					.ToList();
			}
		}

		[Test, DataContextSource]
		public void LetTest1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let chs = p.Children
					select new { p.ParentID, Count = chs.Count() },
					from p in db.Parent
					let chs = p.Children
					select new { p.ParentID, Count = chs.Count() });
		}

		[Test, DataContextSource]
		public void LetTest2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select new { p } into p
					let chs = p.p.Children
					select new { p.p.ParentID, Count = chs.Count() },
					from p in db.Parent
					select new { p } into p
					let chs = p.p.Children
					select new { p.p.ParentID, Count = chs.Count() });
		}

		[Test, DataContextSource]
		public void NullAssociation(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent select p1.ParentTest,
					from p1 in db.Parent select p1.ParentTest);
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2012, ProviderName.PostgreSQL)]
		public void MultipleUse(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var q = db.Child
					.Select(g => new
					{
						g.ChildID,
						a =
						(
							from c in db.Child
							where c.ChildID == g.ChildID
							select new { c, c.Parent }
						).FirstOrDefault()
					})
					.Where(s => s.a != null)
					.Select(s => new
					{
						s.ChildID,
						s.a.c,
						s.a.Parent
					})
					.Select(s => new
					{
						p1 = s.c.ParentID,
						c1 = s.c.ChildID,
						p2 = s.Parent.ParentID,
						v1 = s.Parent.Value1
					});

				var list = q.ToList();

				var idx = db.LastQuery.IndexOf("OUTER APPLY");

				Assert.That(db.LastQuery.IndexOf("OUTER APPLY", idx + 1), Is.EqualTo(-1));
			}
		}

		[Test, DataContextSource]
		public void Issue148Test(string context)
		{
			try
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

				using (var db = GetDataContext(context))
				{
					var q =
						from n in db.Parent
						select new
						{
							n.ParentID,
							Children = n.Children.ToList(),
							//Children = n.Children//.Select(t => t).ToList(),
							//Children = n.Children.Where(t => 1 == 1).ToList().ToList(),
						};

					var list = q.ToList();

					Assert.That(list.Count,       Is.GreaterThan(0));
					Assert.That(list[0].Children, Is.Not.Null);
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
			}
		}

		[Table("Parent")]
		class Parent170
		{
			[Column] public int ParentID;
			[Column] public int Value1;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public Parent170 Parent;
		}

		[Test, DataContextSource]
		public void Issue170Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<Parent170>().Where(x => x.Value1 == null).Select(x => (int?)x.Parent.Value1).First();

				Assert.That(value, Is.Null);
			}
		}

		[Table("Child")]
		class StorageTestClass
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			Parent _parent;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = false, Storage = "_parent")]
			public Parent Parent
			{
				get { return _parent; }
				set { throw new InvalidOperationException(); }
			}
		}

		[Test, DataContextSource]
		public void StorageText(string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<StorageTestClass>().LoadWith(x => x.Parent).First();

				Assert.That(value.Parent, Is.Not.Null);
			}
		}
	}
}
