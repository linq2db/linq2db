using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using JetBrains.Annotations;

#pragma warning disable 472 // The result of the expression is always the same since a value of this type is never equal to 'null'

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class AssociationTests : TestBase
	{
		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child where ch.ParentID == 1 select new { ch, ch.Parent },
					from ch in db.Child where ch.ParentID == 1 select new { ch, ch.Parent });
		}

		[Test]
		public void Test2([DataSources] string context)
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

		[Test]
		public void Test3([DataSources] string context)
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

		[Test]
		public void Test4([DataSources] string context)
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

		[Test]
		public void Test5([DataSources] string context)
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

		[Test]
		public void SelectMany1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(ch => p)),
					db.Parent.SelectMany(p => p.Children.Select(ch => p)));
		}

		[Test]
		public void SelectMany2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.SelectMany(p =>    Child.Select(ch => p)),
					db.Parent.SelectMany(p => db.Child.Select(ch => p)));
			}
		}

		[Test]
		public void SelectMany3([DataSources(ProviderName.Access)] string context)
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

		[Test]
		public void SelectMany4([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent!.ParentID)),
					db.Child
						.GroupBy(ch => ch.Parent)
						.Where(g => g.Count() > 2)
						.SelectMany(g => g.Select(ch => ch.Parent!.ParentID)));
		}

		[Test]
		public void SelectMany5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)),
					db.Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)));
		}

		[Test]
		public void LeftJoin1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c },
					from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c });
		}

		[Test]
		public void LeftJoin2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p },
					from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p });
		}

		[Test]
		public void GroupBy1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child group ch by ch.Parent into g select g.Key,
					from ch in db.Child group ch by ch.Parent into g select g.Key);
		}

		[Test]
		public void GroupBy2([DataSources] string context)
		{
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
				AreEqual(
					(from ch in    Child group ch by ch.Parent1).ToList().Select(g => g.Key),
					(from ch in db.Child group ch by ch.Parent1).ToList().Select(g => g.Key));
		}

		[Test]
		public async Task GroupBy2Async([DataSources] string context)
		{
			using (new GuardGrouping(false))
			using (var db = GetDataContext(context))
				AreEqual(
					       (from ch in    Child group ch by ch.Parent1).ToList().      Select(g => g.Key),
					(await (from ch in db.Child group ch by ch.Parent1).ToListAsync()).Select(g => g.Key));
		}

		[Test]
		public void GroupBy3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent group p by p.Types!.DateTimeValue.Year into g select g.Key,
					from p in db.Parent group p by p.Types!.DateTimeValue.Year into g select g.Key);
		}

		[Test]
		public void GroupBy4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types group p by p.DateTimeValue.Year into g select g.Key,
					from p in db.Types group p by p.DateTimeValue.Year into g select g.Key);
		}

		[Test]
		public void EqualsNull1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select employee.EmployeeID,
					from employee in db.Employee where employee.ReportsToEmployee != null select employee.EmployeeID);
			}
		}

		[Test]
		public void EqualsNull2([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select employee,
					from employee in db.Employee where employee.ReportsToEmployee != null select employee);
			}
		}

		[Test]
		public void EqualsNull3([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				AreEqual(
					from employee in dd.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee },
					from employee in db.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee });
			}
		}

		[Test]
		public void StackOverflow1([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var dd = GetNorthwindAsList(context);
				Assert.AreEqual(
					(from employee in dd.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault(),
					(from employee in db.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault());
			}
		}

		[Test]
		public void StackOverflow2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count != 0 select p,
					from p in db.Parent5 where p.Children.Count != 0 select p);
		}

		[Test]
		public void StackOverflow3([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count() != 0 select p,
					from p in db.Parent5 where p.Children.Count() != 0 select p);
		}

		[Test]
		public void StackOverflow4([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 select new { p.Children.Count },
					from p in db.Parent5 select new { p.Children.Count });
		}

		[Test]
		public void DoubleJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from g in    GrandChild where g.Child!.Parent!.Value1 == 1 select g,
					from g in db.GrandChild where g.Child!.Parent!.Value1 == 1 select g);
		}

		[Test]
		public void Projection1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in
						from c in Child
						where c.Parent!.ParentID == 2
						select c
					join g in GrandChild on c.ParentID equals g.ParentID
					where g.ChildID == 22
					select new { c.Parent, c }
					,
					from c in
						from c in db.Child
						where c.Parent!.ParentID == 2
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
			public Middle? Middle { get; set; }

			[Association(ExpressionPredicate = nameof(MiddleGenericPredicate) , CanBeNull = true)]
			public Middle? MiddleGeneric { get; set; }

			public Middle? MiddleRuntime { get; set; }

			public IEnumerable<Middle> MiddlesRuntime { get; set; } = null!;

			[UsedImplicitly]
			static Expression<Func<Top, Middle, bool>> MiddleGenericPredicate =>
				(t, m) => t.ParentID == m.ParentID && m.ChildID > 1;
		}

		[Table(Name="Child")]
		public class Middle
		{
			[PrimaryKey] public int ParentID;
			[PrimaryKey] public int ChildID;

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = false)]
			public Bottom Bottom { get; set; } = null!;

			[Association(ThisKey = "ChildID", OtherKey = "ChildID", CanBeNull = true)]
			public Bottom? Bottom1 { get; set; }
		}

		[Table("GrandChild", IsColumnAttributeRequired=false)]
		public class Bottom
		{
			public int ParentID;
			public int ChildID;
			public int GrandChildID;
		}

		[Test]
		public void TestTernary1([DataSources(ProviderName.Access, TestProvName.AllSQLite)] string context)
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

		[Test]
		public void TestTernary2([DataSources(ProviderName.Access, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle!.Bottom;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Test]
		public void TestTernary3([DataSources] string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.Middle!.Bottom1;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Table(Name="Child", IsColumnAttributeRequired=false)]
		[InheritanceMapping(Code = 1, IsDefault = true, Type = typeof(ChildForHierarchy))]
		public class ChildBaseForHierarchy
		{
			[Column(IsDiscriminator = true)]
			public int ChildID { get; set; }
		}

		public class ChildForHierarchy : ChildBaseForHierarchy
		{
			public int ParentID { get; set; }
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public Parent? Parent { get; set; }
		}

		[Test]
		public void AssociationInHierarchy([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = db.GetTable<ChildBaseForHierarchy>()
					.OfType<ChildForHierarchy>()
					.Select(ch => new ChildForHierarchy { Parent = ch.Parent })
					.ToList();
			}
		}

		[Test]
		public void LetTest1([DataSources] string context)
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

		[Test]
		public void LetTest2([DataSources] string context)
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

		[Test]
		public void NullAssociation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent select p1.ParentTest,
					from p1 in db.Parent select p1.ParentTest);
		}

		[Test]
		public void MultipleUse([IncludeDataSources(TestProvName.AllSqlServer2005Plus, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12)] string context)
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

				var _ = q.ToList();

				var idx = db.LastQuery!.IndexOf("OUTER APPLY");

				Assert.That(db.LastQuery.IndexOf("OUTER APPLY", idx + 1), Is.EqualTo(-1));
			}
		}

		[Test]
		public void Issue148Test([DataSources] string context)
		{
			using (new AllowMultipleQuery())
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

		[Table("Parent")]
		[UsedImplicitly]
		class Parent170
		{
			[Column] public int ParentID;
			[Column] public int Value1;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public Parent170? Parent;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
			public List<Child170> Children = null!;
		}

		[Table("Child")]
		[UsedImplicitly]
		class Child170
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public Parent170? Parent;
		}

		[Test]
		public void Issue170Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<Parent170>().Where(x => x.Value1 == null).Select(x => (int?)x.Parent!.Value1).First();

				Assert.That(value, Is.Null);
			}
		}

		[Test]
		public void Issue170SelectManyTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<Parent170>()
					.SelectMany(x => x.Children)
					.Where(x => x.Parent!.Value1 == null)
					.Select(x => (int?)x.Parent!.Value1)
					.First();

				Assert.That(value, Is.Null);
			}
		}

		[Table("Child")]
		[UsedImplicitly]
		class StorageTestClass
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			Parent _parent = null!;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = false, Storage = "_parent")]
			public Parent Parent
			{
				get => _parent;
				set => throw new InvalidOperationException();
			}
		}

		[Test]
		public void StorageText([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<StorageTestClass>().LoadWith(x => x.Parent).First();

				Assert.That(value.Parent, Is.Not.Null);
			}
		}

		[Test]
		public void TestGenericAssociation1([DataSources(ProviderName.Access, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.MiddleGeneric == null ? null : t.MiddleGeneric.Bottom;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Test]
		public void TestGenericAssociationRuntime([DataSources(ProviderName.Access, TestProvName.AllSQLite)]
			string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<Top>()
				.Association( t => t.MiddleRuntime, (t, m) => t.ParentID == m!.ParentID && m.ChildID > 1 );

			using (var db = GetDataContext(context, ms))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.MiddleRuntime == null ? null : t.MiddleRuntime.Bottom;

				var list = q.ToList();

				Assert.NotNull(list[0]);
				Assert.Null   (list[1]);
			}
		}

		[Test]
		public void TestGenericAssociationRuntimeMany([DataSources(ProviderName.Access, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();

			mb.Entity<Top>()
				.Association( t => t.MiddlesRuntime, (t, m) => t.ParentID == m.ParentID && m.ChildID > 1 );

			using (var db = GetDataContext(context, ms))
			{
				var q =
					from t in db.GetTable<Top>()
					from m in t.MiddlesRuntime
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select new {t, m};

				var list = q.ToList();

				Assert.AreEqual(1, list.Count());
			}
		}

		[Test]
		public void TestGenericAssociation2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Parent
					from g in t.GrandChildren.Where(m => m.ChildID > 22)
					orderby g.ParentID
					select t
					,
					from t in db.Parent
					from g in t.GrandChildrenX
					orderby g.ParentID
					select t);
			}
		}

		[Test]
		public void TestGenericAssociation3([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Parent
					where t.GrandChildren.Count(m => m.ChildID > 22) > 1
					orderby t.ParentID
					select t
					,
					from t in db.Parent
					where t.GrandChildrenX.Count > 1
					orderby t.ParentID
					select t);
			}
		}

		[Test]
		public void TestGenericAssociation4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Parent
					from g in t.Children.Where(m => Math.Abs(m.ChildID) > 3)
					orderby g.ParentID
					select t
					,
					from t in db.Parent
					from g in t.ChildrenX
					orderby g.ParentID
					select t);
			}
		}

		[Test]
		public void ExtensionTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Parent.SelectMany(_ => _.Children),
				db.Parent.SelectMany(_ => _.Children()));

			}
		}

		[Test]
		public void ExtensionTest11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Parent.SelectMany(_ => _.Children),
				db.Parent.SelectMany(_ => AssociationExtension.Children(_)));
			}
		}

		[Test]
		public void ExtensionTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select(_ => _.Parent),
				db.Child.Select(_ => _.Parent()));
			}
		}

		[Test]
		public void ExtensionTest21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select(_ => _.Parent),
				db.Child.Select(_ => AssociationExtension.Parent(_)));

			}
		}

		[Test]
		public void ExtensionTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select(_ => new { p = _.Parent!  }).Select(_ => _.p.ParentID),
				db.Child.Select(_ => new { p = _.Parent() }).Select(_ => _.p.ParentID));

			}
		}

		[Test]
		public void ExtensionTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select(_ => new { c = _,  p = _.Parent   }).Select(_ => _.c.Parent),
				db.Child.Select(_ => new { c = _,  p = _.Parent() }).Select(_ => _.c.Parent()));

			}
		}

		[Test]
		public void QueryableExtensionTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Parent.SelectMany(_ => _.Children),
				db.Parent.SelectMany(_ => _.QueryableChildren(db)));
			}
		}

		[Test]
		public void QuerableExtensionTest11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Parent.SelectMany(_ => _.Children),
				db.Parent.SelectMany(_ => AssociationExtension.QueryableChildren(_, db)));
			}
		}

		[Test]
		public void QueryableExtensionTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select    (_ => _.Parent),
				db.Child.SelectMany(_ => _.QueryableParent(db)));
			}
		}

		[Test]
		public void QueryableExtensionTest21([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
				   Child.Select    (_ => _.Parent),
				db.Child.SelectMany(_ => AssociationExtension.QueryableParent(_, db)));
			}
		}

		[Test]
		public void AssociationExpressionMethod([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = db.Parent.Select(p => p.ChildPredicate()).ToList();
			}
		}

		[Test]
		public void ComplexQueryWithManyToMany([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				int? id = 3;
				int? id1 = 3;

				// yes, this query doesn't make sense - it just tests
				// that SelectContext.ConvertToIndexInternal handles ConvertFlags.Key in IsScalar branch
				var result = db
				.GetTable<ComplexChild>()
				.Where(с => AssociationExtension.ContainsNullable(
					db
						.GetTable<ComplexParent>()
						.Where(_ => _.ParentID == id.Value)
						.SelectMany(_ => _.Children())
						.Select(_ => _.Parent)
						// this fails without ConvertFlags.Key support
						.Where(_ => _ != null)
						.Select(_ => _!.ParentID),
					id1))
				.OrderBy(с => с.ChildID)
				.Select(с => (int?)с.ChildID)
				.FirstOrDefault();

				Assert.AreEqual(11, result);
			}
		}

		[Table("Parent")]
		public class ComplexParent
		{
			[Column]
			public int ParentID { get; set; }

			[Association(ThisKey = nameof(ParentID), OtherKey = nameof(ComplexManyToMany.ParentID), CanBeNull = false)]
			public IQueryable<ComplexManyToMany> ManyToMany { get; } = null!;
		}

		[Table("Child")]
		public class ComplexManyToMany
		{
			[Column]
			public int ParentID { get; set; }
			[Column]
			public int ChildID  { get; set; }

			[Association(ThisKey = nameof(ChildID), OtherKey = nameof(ComplexChild.ChildID), CanBeNull = false)]
			public ComplexChild  Child { get; } = null!;
		}

		[Table("GrandChild")]
		public class ComplexChild
		{
			[Column]
			public int ChildID  { get; set; }
			[Column]
			public int ParentID { get; set; }

			[Association(ThisKey = nameof(ParentID), OtherKey = nameof(ComplexParent.ParentID), CanBeNull = true)]
			public ComplexParent? Parent { get; }
		}

		public class User
		{
			public int Id { get; set; }
		}

		public class Lookup
		{
			public int     Id   { get; set; }
			public string? Type { get; set; }
		}

		public class Resource
		{
			public int  Id                 { get; set; }
			public int  AssociatedObjectId { get; set; }
			public int? AssociationTypeId  { get; set; }

			[Association(
				ThisKey      = nameof(AssociationTypeId),
				OtherKey     = nameof(Lookup.Id),
				CanBeNull    = true,
				Relationship = Relationship.ManyToOne)]
			public Lookup? AssociationTypeCode { get; set; }

			public static Expression<Func<Resource, IDataContext, IQueryable<User>>> UserExpression =>
				(r, db) => db.GetTable<User>().Where(c => r.AssociationTypeCode!.Type == "us" && c.Id == r.AssociatedObjectId);

			[Association(QueryExpressionMethod = nameof(UserExpression))]
			public User? User { get; set; }
		}

		[Test]
		public void Issue1614Test([DataSources(ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<User>())
			using (db.CreateLocalTable<Resource>())
			using (db.CreateLocalTable<Lookup>())
			{
				var result = db.GetTable<Resource>()
					.LoadWith(x => x.User)
					.ToList();

				//No assert, just need to get past here without an exception
			}
		}
	}

	public static class AssociationExtension
	{
		[Association(ExpressionPredicate = nameof(ChildPredicateImpl))]
		public static Child ChildPredicate(this Parent parent)
		{
			throw new InvalidOperationException("Used only as Association helper");
		}

		static Expression<Func<Parent,Child,bool>> ChildPredicateImpl()
		{
			return (p,c) => p.ParentID == c.ParentID && c.ChildPredicateMethod();
		}

		[ExpressionMethod(nameof(ChildPredicateMethodImpl))]
		public static bool ChildPredicateMethod(this Child child)
		{
			throw new NotImplementedException();
		}

		static Expression<Func<Child,bool>> ChildPredicateMethodImpl()
		{
			return c => c.ChildID > 1;
		}

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public static IEnumerable<Child> Children(this Parent parent)
		{
			throw new InvalidOperationException("Used only as Association helper");
		}

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public static IQueryable<Child> QueryableChildren(this Parent parent, IDataContext db)
		{
			return db.GetTable<Child>().Where(_ => _.ParentID == parent.ParentID);
		}

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public static Parent Parent(this Child child)
		{
			throw new InvalidOperationException("Used only as Association helper");
		}

		[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
		public static IQueryable<Parent> QueryableParent(this Child child, IDataContext db)
		{
			return db.GetTable<Parent>().Where(_ => _.ParentID == child.ParentID);
		}

		[ExpressionMethod(nameof(ContainsNullableExpression))]
		public static bool ContainsNullable<TItem>(IEnumerable<TItem> list, TItem? value)
			where TItem : struct
		{
			return value != null && list.Contains(value.Value);
		}

		private static Expression<Func<IEnumerable<TItem>, TItem?, bool>> ContainsNullableExpression<TItem>()
			where TItem : struct
		{
			// Contains does not work with Linq2DB - use Any
			return (list, value) => value != null && list.Any(li => li.Equals(value));
		}

		[ExpressionMethod(nameof(ChildrenExpression))]
		public static IQueryable<AssociationTests.ComplexChild> Children(this AssociationTests.ComplexParent p)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<AssociationTests.ComplexParent, IQueryable<AssociationTests.ComplexChild>>> ChildrenExpression()
		{
			return p => p.ManyToMany.Select(m2m => m2m.Child);
		}
	}
}
