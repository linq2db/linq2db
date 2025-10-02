using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
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
		public void SelectMany3([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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
		public void SelectMany4([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
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
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));

			AreEqual(
				(from ch in Child group ch by ch.Parent1).ToList().Select(g => g.Key),
				(from ch in db.Child group ch by ch.Parent1).ToList().Select(g => g.Key));
		}

		[Test]
		public async Task GroupBy2Async([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseGuardGrouping(false));
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
				Assert.That(
					(from employee in db.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault(), Is.EqualTo((from employee in dd.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault()));
			}
		}

		[RequiresCorrelatedSubquery]
		[Test]
		public void StackOverflow2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count != 0 select p,
					from p in db.Parent5 where p.Children.Count != 0 select p);
		}

		[RequiresCorrelatedSubquery]
		[Test]
		public void StackOverflow3([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent5 where p.Children.Count() != 0 select p,
					from p in db.Parent5 where p.Children.Count() != 0 select p);
		}

		[Test]
		[RequiresCorrelatedSubquery]
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
			using var db = GetDataContext(context);

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
		public void TestTernary1([DataSources(TestProvName.AllAccess, TestProvName.AllSQLite)] string context)
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0], Is.Not.Null);
					Assert.That(list[1], Is.Null);
				}
			}
		}

		[Test]
		public void TestTernary2([DataSources(TestProvName.AllAccess, TestProvName.AllSQLite)] string context)
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0], Is.Not.Null);
					Assert.That(list[1], Is.Null);
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0], Is.Not.Null);
					Assert.That(list[1], Is.Null);
				}
			}
		}

		[Test(Description = "CanBeNull=true association doesn't enforce nullability on referenced non-nullable columns")]
		public void TestNullabilityPropagation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// left join makes t.Middle!.ParentID which means with default NULL comparison semantics we
			// should generate following SQL:
			// parent_id <> 4 or parent_id is null
			var result = db.GetTable<Top>().Where(t => t.Middle!.ParentID != 4).ToArray();
			Assert.That(result, Has.Length.EqualTo(14));
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

		[RequiresCorrelatedSubquery]
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

		[RequiresCorrelatedSubquery]
		[Test]
		public void LetTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exptected = from p in Parent
					select new { p } into pp
					let chs = pp.p.Children
					select new { pp.p.ParentID, Count = chs.Count() };

				var actual = db.Parent.Select(p => new { Peojection = p })
					.Select(pp => new { pp, chs = pp.Peojection.Children })
					.Select(@t => new { @t.pp.Peojection.ParentID, Count = @t.chs.Count() });

				var actualResult = actual.ToArray();

				AreEqual(
					exptected,
					actual);
			}
		}

		[Test]
		public void NullAssociation([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p1 in    Parent select p1.ParentTest,
					from p1 in db.Parent select p1.ParentTest);
		}

		[Test]
		public void MultipleUse([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12Plus, TestProvName.AllMySql8Plus, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
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
						s.a!.c,
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
			using (var db = GetDataContext(context))
			{
				var q =
					from n in db.Parent
					select new
					{
						n.ParentID,
						Children1 = n.Children.ToList(),
						Children2 = n.Children.Select(t => t).ToList(),
						Children3 = n.Children.Where(t => 1 == 1).ToList().ToList(),
					};

				var list = q.ToList();

				Assert.That(list,       Is.Not.Empty);
				Assert.That(list[0].Children1, Is.Not.Null);
			}
		}

		[Table("Parent")]
		[UsedImplicitly]
		sealed class Parent170
		{
			[Column] public int ParentID;
			[Column(CanBeNull = true)] public int Value1;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public Parent170? Parent;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
			public List<Child170> Children = null!;
		}

		[Table("Child")]
		[UsedImplicitly]
		sealed class Child170
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
				var value = db.GetTable<Parent170>()
#pragma warning disable CS0472 // comparison of int with null
					.Where(x => x.Value1 == null)
#pragma warning restore CS0472
					.Select(x => (int?)x.Parent!.Value1)
					.First();

				Assert.That(value, Is.Null);
			}
		}

		[Test]
		public void Issue170SelectManyTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual = db.GetTable<Parent170>()
					.SelectMany(x => x.Children)
#pragma warning disable CS0472 // comparison of int with null
					.Where(x => x.Parent!.Value1 == null)
#pragma warning restore CS0472
					.Select(x => (int?)x.Parent!.Value1)
					.First();

				Assert.That(actual, Is.Null);
			}
		}

		[Table("Child")]
		[UsedImplicitly]
		sealed class StorageTestClass
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

		sealed class ParentContainer
		{
			public Parent? Value;

			public void SetValue(Parent? value)
			{
				Value = value;
			}
		}

		[Table("Child")]
		[UsedImplicitly]
		sealed class AssociationSetterExpressionTestClass
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			ParentContainer _parent = new ParentContainer();

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = false, Storage = "_parent", AssociationSetterExpressionMethod = nameof(SetParentValue))]
			public Parent? Parent
			{
				get => _parent.Value;
				set => throw new InvalidOperationException();
			}

			[Association(ThisKey = "Parent2ID", OtherKey = "ParentID", CanBeNull = false)]
			public Parent? Parent2 { get; set; }

			public static Expression<Action<ParentContainer, Parent>> SetParentValue()
			{
				return static (container, value) => container.SetValue(value);
			}
		}

		[Test]
		public void AssociationSetterExpressionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var value = db.GetTable<AssociationSetterExpressionTestClass>().LoadWith(x => x.Parent).First();

				Assert.That(value.Parent, Is.Not.Null);
			}
		}

		// at the moment it must be generic because linq2db will infer entity type from generic arguments
		sealed class ChildrenContainer<T> : IEnumerable<T> where T : Child
		{
			public List<T>? Value;

			public IEnumerator<T> GetEnumerator()
			{
				return ((IEnumerable<T>)Value!).GetEnumerator();
			}

			public void SetValue(IEnumerable<T> value)
			{
				Value = value.ToList();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable)Value!).GetEnumerator();
			}
		}

		[Table("Parent")]
		[UsedImplicitly]
		sealed class Issue3975TestClass
		{
			[Column] public int ParentID;

			ChildrenContainer<Child> _children = new();

			[Association(ThisKey = "ParentID", OtherKey = "ParentID", Storage = "_children", AssociationSetterExpressionMethod = nameof(SetChildrenValue))]
			public ChildrenContainer<Child> Children
			{
				get => _children;
				set => throw new InvalidOperationException();
			}

			public static Expression<Action<ChildrenContainer<Child>, IEnumerable<Child>>> SetChildrenValue()
			{
				return static (container, value) => container.SetValue(value);
			}
		}

		[Test]
		public void Issue3975Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// we want to make sure the conversion is not possible because we want to bypass
				// that conversion if the setter value parameter type (IEnumerable<Child> in this case)
				// does not match the member type (ChildrenContainer<Child> in this case)
				Assert.Throws<LinqToDB.Common.LinqToDBConvertException>(() =>
					db.MappingSchema.ChangeType(new List<Child>(0), typeof(ChildrenContainer<Child>)),
					"List<Child> should not be convertible to ChildrenContainer<Child>");

				var value = db.GetTable<Issue3975TestClass>().LoadWith(x => x.Children).First();

				Assert.That(value.Children.Value, Is.Not.Null);
			}
		}

		[Test]
		public void TestGenericAssociation1([DataSources(TestProvName.AllAccess, TestProvName.AllSQLite)] string context)
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0], Is.Not.Null);
					Assert.That(list[1], Is.Null);
				}
			}
		}

		[Test]
		public void TestGenericAssociationRuntime([DataSources(TestProvName.AllAccess, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<Top>()
				.Association( t => t.MiddleRuntime, (t, m) => t.ParentID == m!.ParentID && m.ChildID > 1 )
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				var q =
					from t in db.GetTable<Top>()
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select t.MiddleRuntime == null ? null : t.MiddleRuntime.Bottom;

				var list = q.ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0], Is.Not.Null);
					Assert.That(list[1], Is.Null);
				}
			}
		}

		[Test]
		public void TestGenericAssociationRuntimeMany([DataSources(TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<Top>()
				.Association( t => t.MiddlesRuntime, (t, m) => t.ParentID == m.ParentID && m.ChildID > 1 )
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				var q =
					from t in db.GetTable<Top>()
					from m in t.MiddlesRuntime
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select new {t, m};

				var list = q.ToList();

				Assert.That(list, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void TestGenericAssociation2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exptected = (from t in Parent
					from g in t.GrandChildren.Where(m => m.ChildID > 22)
					orderby g.ParentID
					select t).ToArray();

				var actual = (from t in db.Parent
					from g in t.GrandChildrenX
					orderby g.ParentID
					select t).ToArray();

				AreEqual(exptected, actual);
			}
		}

		[RequiresCorrelatedSubquery]
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
		public void DistinctSelect([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					GrandChild.Where(gc => gc.Child!.Parent!.ParentID > 0).Select(gc => gc.Child).Distinct()
						.Select(c => c!.ChildID),
					db.GrandChild.Where(gc => gc.Child!.Parent!.ParentID > 0).Select(gc => gc.Child).Distinct()
						.Select(c => c!.ChildID));
		}

		[Test]
		public void AssociationExpressionMethod([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = db.Parent.Select(p => p.ChildPredicate()).ToList();
		}

		[Table]
		sealed class NotNullParent
		{
			[Column] public int ID { get; set; }

			[Association(ExpressionPredicate = nameof(ChildPredicate), CanBeNull = false)]
			public NotNullChild  ChildInner { get; set; } = null!;

			[Association(ExpressionPredicate = nameof(ChildPredicate), CanBeNull = true)]
			public NotNullChild? ChildOuter { get; set; }

			static Expression<Func<NotNullParent, NotNullChild, bool>> ChildPredicate => (p, c) => p.ID == c.ParentID;

			public static readonly NotNullParent[] Data = new[]
			{
				new NotNullParent { ID = 1 },
				new NotNullParent { ID = 2 },
			};
		}

		[Table]
		sealed class NotNullChild
		{
			[Column] public int ParentID { get; set; }

			public static readonly NotNullChild[] Data = new[]
			{
				new NotNullChild { ParentID = 1 },
			};
		}

		[Test]
		public void AssociationExpressionNotNull([DataSources] string context)
		{
			using var db     = GetDataContext(context);
			using var parent = db.CreateLocalTable(NotNullParent.Data);
			using var child  = db.CreateLocalTable(NotNullChild.Data);

			var query = parent.Select(p => new { ParentID = (int?)p.ChildInner.ParentID });

			var result = query.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].ParentID, Is.EqualTo(1));
		}

		[Test]
		public void AssociationExpressionNull([DataSources] string context)
		{
			using var db     = GetDataContext(context);
			using var parent = db.CreateLocalTable(NotNullParent.Data);
			using var child  = db.CreateLocalTable(NotNullChild.Data);

			var query = parent.OrderBy(_ => _.ID).Select(p => new { ParentID = (int?)p.ChildOuter!.ParentID });

			var result = query.ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].ParentID, Is.EqualTo(1));
				Assert.That(result[1].ParentID, Is.Null);
			}
		}

		[Test]
		public void AssociationExpressionNotNullCount([DataSources] string context)
		{
			var parentData = new[]
			{
				new NotNullParent { ID = 1 },
				new NotNullParent { ID = 2 },
			};

			var childData = new[]
			{
				new NotNullChild { ParentID = 1 },
			};

			using var db     = GetDataContext(context);
			using var parent = db.CreateLocalTable(parentData);
			using var child  = db.CreateLocalTable(childData);

			var query = parent.Select(p => p.ChildInner.ParentID);

			Assert.That(query.Count(), Is.EqualTo(1));
		}

		[Test]
		public void AssociationExpressionNullCount([DataSources] string context)
		{
			var parentData = new[]
			{
				new NotNullParent { ID = 1 },
				new NotNullParent { ID = 2 },
			};

			var childData = new[]
			{
				new NotNullChild { ParentID = 1 },
			};

			using var db     = GetDataContext(context);
			using var parent = db.CreateLocalTable(parentData);
			using var child  = db.CreateLocalTable(childData);

			var query = parent.Select(p => p.ChildOuter!.ParentID);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(query.Count(), Is.EqualTo(2));
				Assert.That(query.GetTableSource().Joins, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void ComplexQueryWithManyToMany([DataSources(TestProvName.AllClickHouse)] string context)
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
						.Where(p => p.ParentID == id.Value)
						.SelectMany(p => p.Children())
						.Select(c => c.Parent)
						// this fails without ConvertFlags.Key support
						.Where(c => c != null)
						.Select(c => c!.ParentID),
					id1))
				.OrderBy(с => с.ChildID)
				.Select(с => (int?)с.ChildID)
				.FirstOrDefault();

				Assert.That(result, Is.EqualTo(11));
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

			[Association(ThisKey   = nameof(AssociationTypeId), OtherKey  = nameof(Lookup.Id), CanBeNull = true)]
			public Lookup? AssociationTypeCode { get; set; }

			public static Expression<Func<Resource, IDataContext, IQueryable<User>>> UserExpression =>
				(r, db) => db.GetTable<User>().Where(c => r.AssociationTypeCode!.Type == "us" && c.Id == r.AssociatedObjectId);

			[Association(QueryExpressionMethod = nameof(UserExpression))]
			public User? User { get; set; }
		}

		[Test]
		public void Issue1614Test([DataSources(TestProvName.AllAccess)] string context)
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

		[Table]
		sealed class Employee
		{
			[Column] public int  Id           { get; set; }
			[Column] public int? DepartmentId { get; set; }

			[Association(ExpressionPredicate = nameof(DepartmentPredicate), CanBeNull = true)]
			public Department? Department { get; set; }

			public static Expression<Func<Employee, Department, bool>> DepartmentPredicate => (e, d) => e.DepartmentId == d.DepartmentId && !d.Deleted;
		}

		[Table]
		sealed class Department
		{
			[Column] public int     DepartmentId { get; set; }
			[Column] public string? Name         { get; set; }
			[Column] public bool    Deleted      { get; set; }
		}

		[Test]
		public void Issue845Test([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Employee>();
			using var t2 = db.CreateLocalTable<Department>();

			var result = db.GetTable<Employee>()
				.Select(e => new { e.Id, e.Department!.Name })
				.ToList();

			if (context.IsAnyOf(TestProvName.AllSqlServer))
			{
				Assert.That(db.LastQuery!, Does.Not.Contain(" NOT"));
				Assert.That(db.LastQuery!, Does.Contain("AND [a_Department].[Deleted] = 0"));
			}
			else
			{
				Assert.That(db.LastQuery!, Does.Contain(" NOT"));
				Assert.That(db.LastQuery!, Does.Not.Contain(" = 0"));
			}
		}

		sealed class Entity1711
		{
			public long Id { get; set; }
		}

		sealed class Relationship1711
		{
			public long EntityId { get; set; }

			public bool Deleted { get; set; }
		}

		[Test]
		public void Issue1711Test1([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Entity1711>()
				.HasTableName("Entity1711")
				.HasPrimaryKey(x => Sql.Property<long>(x, "Id"))
				.Association(x => Sql.Property<IQueryable<Relationship1711>>(x, "relationship"), e => e.Id, r => r.EntityId)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var entity = db.CreateLocalTable<Entity1711>())
			using (db.CreateLocalTable<Relationship1711>())
			{
				var result1 = entity
					.Where(t => Sql.Property<IQueryable<Relationship1711>>(t, "relationship").Any())
					.ToList();
			}
		}

		[Test]
		public void Issue1711Test2([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Entity1711>()
				.HasTableName("Entity1711")
				.HasPrimaryKey(x => Sql.Property<long>(x, "Id"))
				.Association(x => Sql.Property<IQueryable<Relationship1711>>(x, "relationship"), (e, db) => db.GetTable<Relationship1711>()
						.Where(r => r.Deleted == false && r.EntityId == e.Id))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var entity = db.CreateLocalTable<Entity1711>())
			using (db.CreateLocalTable<Relationship1711>())
			{
				var result1 = entity
					.Where(t => Sql.Property<IQueryable<Relationship1711>>(t, "relationship").Any())
					.ToList();
			}
		}

		[Table]
		sealed class Issue1096Task
		{
			[Column]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public Issue1096TaskStage ActualStage { get; set; } = null!;

			private static Expression<Func<Issue1096Task, Issue1096TaskStage, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		[Table]
		sealed class Issue1096TaskStage
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			public bool Actual { get; set; }
		}

		[Test]
		public void Issue1096Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1096Task>())
			using (db.CreateLocalTable<Issue1096TaskStage>())
			{
				db.Insert(new Issue1096Task { Id = 1, TargetName = "bda.Requests" });
				db.Insert(new Issue1096Task { Id = 1, TargetName = "bda.Requests" });
				db.Insert(new Issue1096TaskStage { Id = 1, TaskId = 1, Actual = true });

				var query = db.GetTable<Issue1096Task>()
					.Distinct()
					.Select(t => new { t, t.ActualStage });
				var res = query.ToArray();

				Assert.That(res, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].t.Id, Is.EqualTo(1));
					Assert.That(res[0].t.TargetName, Is.EqualTo("bda.Requests"));
					Assert.That(res[0].ActualStage.Id, Is.EqualTo(1));
					Assert.That(res[0].ActualStage.TaskId, Is.EqualTo(1));
					Assert.That(res[0].ActualStage.Actual, Is.True);
				}
			}
		}

		#region issue 2981

		public interface IIssue2981Entity
		{
			int OwnerId { get; set; }
		}

		public abstract class Issue2981OwnedEntity<T> where T : IIssue2981Entity
		{
			/// <summary>
			/// Owner.
			/// </summary>
			[Association(ExpressionPredicate = nameof(OwnerPredicate), CanBeNull = true)]
			public Issue2981OwnerEntity? Owner { get; set; }

			public static Expression<Func<T, Issue2981OwnerEntity, bool>> OwnerPredicate { get; set; } = (entity, owner) => entity.OwnerId == owner.Id;
		}

		[Table]
		public class Issue2981Entity: Issue2981OwnedEntity<Issue2981Entity>, IIssue2981Entity
		{
			[Column] public int OwnerId { get; set; }
		}

		[Table]
		public class Issue2981OwnerEntity
		{
			[Column] public int Id { get; set; }

		}

		[Test]
		public void Issue2981Test([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(new[]
			{
				new Issue2981Entity {OwnerId = 1},
				new Issue2981Entity {OwnerId = 2}
			});
			using var t2 = db.CreateLocalTable(new[] {new Issue2981OwnerEntity {Id = 1}});

			var res = t1.Select(r => new {r.OwnerId, Id = (int?)r.Owner!.Id})
				.OrderBy(_ => _.OwnerId)
				.ToArray();

			res.Length.ShouldBe(2);
			res[0].Id.ShouldBe(1);
			res[0].OwnerId.ShouldBe(1);
			res[1].OwnerId.ShouldBe(2);
			res[1].Id.ShouldBeNull();
		}

		#endregion

		#region issue 3260

		[Table]
		public class LeaveRequest
		{
			[Column] public virtual int                       Id                      { get; set; }
			[Column] public virtual int                       EmployeeId              { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(LeaveRequestDateEntry.LeaveRequestId))]
			public virtual ICollection<LeaveRequestDateEntry> LeaveRequestDateEntries { get; set; } = null!;
		}

		public class LeaveRequestDateEntry
		{
			public virtual int      Id             { get; set; }
			public virtual decimal? EndHour        { get; set; }
			public virtual decimal? StartHour      { get; set; }
			public virtual int      LeaveRequestId { get; set; }
		}

		public class TestDto
		{
			public decimal? Result { get; set; }
		}

		[Test]
		public void Issue3260Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var t1 = db.CreateLocalTable<LeaveRequest>())
			using (var t2 = db.CreateLocalTable<LeaveRequestDateEntry>())
			{
				db.GetTable<LeaveRequest>()
					.Select(x => new TestDto()
					{
						Result = x
							.LeaveRequestDateEntries
							.Select(e => e.StartHour)
							.DefaultIfEmpty(0)
							.Sum()
					}).ToList();
			}
		}

		#endregion

		[Test(Description = "association over set query")]
		public void Issue2966([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Patient.Concat(db.Patient).Select(r => new { r.Diagnosis, r.Person.FirstName }).ToArray();
		}

		#region issue 3557
		[Table]
		public class SubData2
		{
			[Column] public int     Id     { get; set; }
			[Column] public string? Reason { get; set; }

			public static readonly SubData2[] Records = new[]
			{
				new SubData2() { Id = 3, Reason = "прст1" },
				new SubData2() { Id = 3, Reason = "прст2" },
			};
		}

		[Table]
		public class SubData1
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubData2.Id))]
			public IEnumerable<SubData2> SubDatas { get; } = null!;

			public static readonly SubData1[] Records = new[]
			{
				new SubData1() { Id = 2 },
				new SubData1() { Id = 3 },
			};
		}

		[Table]
		public class Data
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubData1.Id), CanBeNull = true)]
			public SubData1? SubData { get; }

			public static readonly Data[] Records = new[]
			{
				new Data() { Id = 1 },
				new Data() { Id = 2 },
				new Data() { Id = 3 },
			};
		}

		[Test]
		public void Issue3557Case1([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllSapHana,
			TestProvName.AllSybase,
			TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			using var data = db.CreateLocalTable(Data.Records);
			using var subData1 = db.CreateLocalTable(SubData1.Records);
			using var subData2 = db.CreateLocalTable(SubData2.Records);

			var result = data
				.Select(
				i => new
				{
					Id     = i.Id,
					Reason = i.SubData == null ? null : i.SubData.SubDatas.Select(s => s.Reason).FirstOrDefault(),
				})
				.OrderBy(r => r.Id)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[2].Id, Is.EqualTo(3));
				Assert.That(result[0].Reason, Is.Null);
				Assert.That(result[1].Reason, Is.Null);
				Assert.That(result[2].Reason == "прст1" || result[2].Reason == "прст2", Is.True);
			}
		}

		[Test]
		public void Issue3557Case2([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllSapHana,
			TestProvName.AllSybase,
			TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			using var data = db.CreateLocalTable(Data.Records);
			using var subData1 = db.CreateLocalTable(SubData1.Records);
			using var subData2 = db.CreateLocalTable(SubData2.Records);

			var result = data
				.Select(
				i => new
				{
					Id     = i.Id,
					Reason = i.SubData!.SubDatas.Select(s => s.Reason).FirstOrDefault() ?? string.Empty,
				})
				.OrderBy(r => r.Id)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[2].Id, Is.EqualTo(3));
				Assert.That(result[0].Reason, Is.EqualTo(string.Empty));
				Assert.That(result[1].Reason, Is.EqualTo(string.Empty));
				Assert.That(result[2].Reason == "прст1" || result[2].Reason == "прст2", Is.True);
			}
		}

		[Test]
		public void Issue3557Case3([DataSources(
			TestProvName.AllClickHouse,
			TestProvName.AllSapHana,
			TestProvName.AllSybase,
			TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			using var data = db.CreateLocalTable(Data.Records);
			using var subData1 = db.CreateLocalTable(SubData1.Records);
			using var subData2 = db.CreateLocalTable(SubData2.Records);

			var result = data
				.Select(
				i => new
				{
					Id     = i.Id,
					Reason = i.SubData!.SubDatas.Select(s => s.Reason).FirstOrDefault(),
				})
				.OrderBy(r => r.Id)
				.ToList();

			Assert.That(result, Has.Count.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
				Assert.That(result[1].Id, Is.EqualTo(2));
				Assert.That(result[2].Id, Is.EqualTo(3));
				Assert.That(result[0].Reason, Is.Null);
				Assert.That(result[1].Reason, Is.Null);
				Assert.That(result[2].Reason == "прст1" || result[2].Reason == "прст2", Is.True);
			}
		}
		#endregion

		[Test]
		public void Issue3809Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var actual = db.Parent.Select(a => new
			{
				a.ParentID,
				ParentTest = a.ParentTest == null ? null : new
				{
					a.ParentTest.ParentID,
					Children = a.ParentTest.Children.OrderBy(a => a.ChildID).Select(a => new
					{
						a.ParentID,
						a.ChildID
					})
				}
			}).Where(a => a.ParentTest == null || a.ParentTest.Children.Any(a => a.ChildID == 11)).ToArray();

			var expected = Parent.Select(a => new
			{
				a.ParentID,
				ParentTest = a.ParentTest == null ? null : new
				{
					a.ParentTest.ParentID,
					Children = a.ParentTest.Children.OrderBy(a => a.ChildID).Select(a => new
					{
						a.ParentID,
						a.ChildID
					})
				}
			}).Where(a => a.ParentTest == null || a.ParentTest.Children.Any(a => a.ChildID == 11)).ToArray();
			AreEqualWithComparer(expected, actual);
		}

		#region issue association correlation nullability

		[Table]
		class Table1
		{
			[PrimaryKey] public int  ID  { get; set; }
			[Column    ] public int? ID2 { get; set; }

			[Association(ThisKey = nameof(ID2), OtherKey = nameof(AssociationTests.Table2.ID))]
			public Table2? Table2 { get; set; }

			public static readonly Table1[] Data = new[]
			{
				new Table1() { ID = 1, ID2 = 1 },
				new Table1() { ID = 2, ID2 = 2 },
			};
		}

		[Table]
		class Table2
		{
			[PrimaryKey] public int  ID  { get; set; }
			[Column    ] public int? ID3 { get; set; }

			[Association(ThisKey = nameof(ID3), OtherKey = nameof(AssociationTests.Table3.ID))]
			public Table3? Table3 { get; set; }

			public static readonly Table2[] Data = new[]
			{
				new Table2() { ID = 1, ID3 = 1 },
			};
		}

		[Table]
		class Table3
		{
			[PrimaryKey] public int ID { get; set; }

			[Association(ThisKey = nameof(ID), OtherKey = nameof(AssociationTests.Table4.ID3))]
			public IEnumerable<Table4> Table4 { get; set; } = null!;

			public static readonly Table3[] Data = new[]
			{
				new Table3() { ID = 1 },
			};
		}

		[Table]
		class Table4
		{
			[PrimaryKey] public int  ID  { get; set; }
			[Column    ] public int? ID3 { get; set; }

			public static readonly Table4[] Data = new[]
			{
				new Table4() { ID = 1, ID3 = 1 },
				new Table4() { ID = 2 },
			};
		}

		[Test]
		public void OptionalAssociationNonNullCorrelation([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Table1.Data);
			using var t2 = db.CreateLocalTable(Table2.Data);
			using var t3 = db.CreateLocalTable(Table3.Data);
			using var t4 = db.CreateLocalTable(Table4.Data);

			var query = t1
				.LoadWith(r => r.Table2!.Table3!.Table4)
				.AsQueryable();

			query = query.Where(r => r.Table2!.Table3!.Table4.Select(u => u.ID).Any(id => id == r.ID));

			AssertQuery(query);
		}

		[Test]
		public void OptionalAssociationNonNullCorrelationWithProjection([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Table1.Data);
			using var t2 = db.CreateLocalTable(Table2.Data);
			using var t3 = db.CreateLocalTable(Table3.Data);
			using var t4 = db.CreateLocalTable(Table4.Data);

			var results = t1
				.Where(r => r.Table2!.Table3!.Table4.Select(u => u.ID).Any(id => id == r.ID))
				.Select(r => new
					{
						r.Table2,
						r.Table2!.Table3,
					} 
				)
				.ToList();

			Assert.That(results, Has.Count.EqualTo(1));
			Assert.That(results[0].Table2!.ID, Is.EqualTo(1));
		}
		#endregion

		#region ViaInterface

		public class MainEntity : IHasSubentities
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubEntity.MainEntityId))]
			public ICollection<SubEntity> SubEntities { get; set; } = null!;
		}

		public interface IHasSubentities
		{
			ICollection<SubEntity> SubEntities { get; }
		}

		public class SubEntity
		{
			public int Id { get; set; }

			public int MainEntityId { get; set; }

			public MainEntity MainEntity { get; set; } = null!;
		}

		static IQueryable<T> OnlyWithSubEntities<T>(IQueryable<T> query)
			where T : IHasSubentities
		{
			return query.Where(x => x.SubEntities.Any());
		}

		[RequiresCorrelatedSubquery]
		[Test]
		public void ViaInterfaceAndExtension([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var main = db.CreateLocalTable<MainEntity>();
			using var sub = db.CreateLocalTable<SubEntity>();
			
			var query = OnlyWithSubEntities(db.GetTable<MainEntity>())
				.Select(x => new
				{
					x.Id,
					x.SubEntities.Count
				});

			var result = query.ToArray();
		}

		[RequiresCorrelatedSubquery]
		[Test]
		public void ViaInterfaceOfType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var main = db.CreateLocalTable<MainEntity>();
			using var sub = db.CreateLocalTable<SubEntity>();
			
			var query = db.GetTable<MainEntity>()
				.OfType<IHasSubentities>()
				.Select(x => new
				{
					x.SubEntities.Count
				});

			var result = query.ToArray();
		}

		[Test]
		public void ViaInterfaceSelect([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db   = GetDataContext(context);
			using var main = db.CreateLocalTable<MainEntity>();
			using var sub  = db.CreateLocalTable<SubEntity>();

			var query = db.GetTable<MainEntity>()
				.OfType<IHasSubentities>()
				.Select(x => new { x.SubEntities });

			var result = query.ToArray();
		}

		[Test]
		public void ViaInterfaceLoadWith([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db   = GetDataContext(context);
			using var main = db.CreateLocalTable<MainEntity>();
			using var sub  = db.CreateLocalTable<SubEntity>();

			var query = db.GetTable<MainEntity>()
				.OfType<IHasSubentities>()
				.LoadWith(x => x.SubEntities);

			var result = query.ToArray();
		}

		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2022")]
		public void TestAssociationAliasEscaping([DataSources(false)] string context)
		{
			var old = LinqToDB.Common.Configuration.Sql.AssociationAlias;
			try
			{
				LinqToDB.Common.Configuration.Sql.AssociationAlias = "test.[aLыi`\",:!@#$%^&*()_'=as].{0}";

				using var db = GetDataContext(context);

				db.Child.Select(c => new { c.ChildID, c.Parent!.Value1 }).ToArray();
			}
			finally
			{
				LinqToDB.Common.Configuration.Sql.AssociationAlias = old;
			}
		}

		#region Issue 2933

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2933")]
		public void Issue2933Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Issue2933Car.Data);
			using var t2 = db.CreateLocalTable(Issue2933Person.Data);
			using var t3 = db.CreateLocalTable(Issue2933Pet.Data);

			var data = t1
				.Select(x => new
				{
					x.Id,
					PetName = x
						.Person!
						.PetIds
						.Select(y => y.Name)
						.FirstOrDefault()
				})
				.ToArray();

			Assert.That(data, Has.Length.EqualTo(2));
		}

		sealed class Issue2933Car
		{
			[PrimaryKey] public int Id;

			public int? PersonId { get; set; }

			[Association(ThisKey = nameof(PersonId), OtherKey = nameof(Issue2933Person.Id), CanBeNull = true)]
			public Issue2933Person? Person { get; set; }

			public static readonly Issue2933Car[] Data =
			[
				new() { Id = 1, PersonId = 1 },
				new() { Id = 2 }
			];
		}

		sealed class Issue2933Person
		{
			[PrimaryKey] public int Id;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Issue2933Pet.PersonId), CanBeNull = true)]
			public IEnumerable<Issue2933Pet> PetIds { get; set; } = null!;

			public static readonly Issue2933Person[] Data =
			[
				new() { Id = 1 }
			];
		}

		sealed class Issue2933Pet
		{
			[PrimaryKey] public int Id;

			[Column, NotNull] public string Name { get; set; } = null!;

			[Column] public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = nameof(Issue2933Person.Id), CanBeNull = false)]
			public Issue2933Person Person { get; set; } = null!;

			public static readonly Issue2933Pet[] Data =
			[
				new() { Id = 1, PersonId = 1, Name = "Snuffles" },
				new() { Id = 2, PersonId = 1, Name = "Buddy" },
			];
		}
		#endregion

		#region Issue 4454

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4454")]
		public void Issue4454Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Issue4454Client>();
			using var t2 = db.CreateLocalTable<Issue4454Service>();

			t2.Select(s => s.Client1.Name).ToArray();

			Assert.That(db.LastQuery, Does.Contain("INNER JOIN"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4454")]
		public void Issue4454Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<Issue4454Client>();
			using var t2 = db.CreateLocalTable<Issue4454Service>();

			t2.Select(s => s.Client2.Name).ToArray();

			Assert.That(db.LastQuery, Does.Contain("INNER JOIN"));
		}

		sealed class Issue4454Client
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		sealed class Issue4454Service
		{
			public int Id { get; set; }
			public int? IdClient { get; set; }

			[Association(ExpressionPredicate = nameof(Client_ExprPr), CanBeNull = false)]
			public Issue4454Client Client1 { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(Client_QExpr), CanBeNull = false)]
			public Issue4454Client Client2 { get; set; } = null!;

			// works fine
			static Expression<Func<Issue4454Service, Issue4454Client, bool>> Client_ExprPr =>
			    (s, c) => s.IdClient == c.Id;

			// always generates left join or outer apply (if query is more complicated)
			static Expression<Func<Issue4454Service, IDataContext, IQueryable<Issue4454Client>>> Client_QExpr =>
				(s, db) => db.GetTable<Issue4454Client>().Where(c => c.Id == s.IdClient);
		}

		#endregion

		#region Issue 3822

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllAccess], ErrorMessage = ErrorHelper.Error_Join_Without_Condition)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3822")]
		public void Issue3822Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using (db.CreateLocalTable(new[] { new Dog { Id = 1, OwnerId = 1 } }))
			using (db.CreateLocalTable(new[] { new Human { Id = 1, HouseId = 1 } }))
			using (db.CreateLocalTable(new[] { new House { Id = 1 } }))
			using (db.CreateLocalTable(new[] { new Window { Id = 6, Position = 6 } }))
			{
				var windowId = db
					.GetTable<Dog>()
					.Select(x => x.House.WindowAtPosition(db, 6)!.Id)
					.FirstOrDefault();

				Assert.That(windowId, Is.EqualTo(6));
			}
		}

		public class Dog
		{
			public int Id { get; set; }

			public int OwnerId { get; set; }

			[Association(ThisKey = nameof(OwnerId), OtherKey = nameof(Human.Id), CanBeNull = false)]
			public Human Owner { get; set; } = null!;

			[ExpressionMethod(nameof(HouseExpression), IsColumn = false)]
			public House House { get; set; } = null!;

			private static Expression<Func<Dog, House>> HouseExpression()
			{
				return entity => entity
					.Owner
					.House;
			}
		}

		public class Human
		{
			public int Id { get; set; }

			public int HouseId { get; set; }

			[Association(ThisKey = nameof(HouseId), OtherKey = nameof(House.Id), CanBeNull = false)]
			public House House { get; set; } = null!;
		}

		public class House
		{
			public int Id { get; set; }

			[Association(QueryExpressionMethod = nameof(WindowAtPositionExpression), CanBeNull = true)]
			public Window? WindowAtPosition(IDataContext db, int position)
			{
				return (_windowAtPositionExpression ??= WindowAtPositionExpression().Compile())(this, db, position).FirstOrDefault();
			}

			private static Func<House, IDataContext, int, IQueryable<Window?>>? _windowAtPositionExpression;

			private static Expression<Func<House, IDataContext, int, IQueryable<Window?>>> WindowAtPositionExpression()
			{
				return (entity, db, position) => db
					.GetTable<Window>()
					.Where(x => x.Position == position)
					.Take(1);
			}
		}

		public class Window
		{
			public int Id { get; set; }

			public int Position { get; set; }
		}
		#endregion

		[Test]
		public void ManyAssociationEmptyCheck1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var parents = db.Parent.Where(p => p.Children != null).ToArray();

			Assert.That(parents, Has.Length.EqualTo(6));
			Assert.That(parents.Any(p => p.ParentID == 5), Is.False);
		}

		[Test]
		public void ManyAssociationEmptyCheck2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var parents = db.Parent.Where(p => p.Children == null).ToArray();

			Assert.That(parents, Has.Length.EqualTo(1));
			Assert.That(parents[0].ParentID, Is.EqualTo(5));
		}

		#region issue 4274

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4274")]
		public void Issue4274Test([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			var query1 = (
					from serv in db.Patient
					group serv by new { serv.PersonID } into gr
					select new Patient
					{
						PersonID = gr.Key.PersonID,
					}
				);

			var query2 = (
					from serv in query1
					where serv.Person.ID == 1
					select serv
				);

			var result = query2.ToList();
		}

		#endregion
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
