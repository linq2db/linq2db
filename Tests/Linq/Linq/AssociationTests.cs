using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
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
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child where ch.ParentID == 1 select new { ch, ch.Parent },
				from ch in db.Child where ch.ParentID == 1 select new { ch, ch.Parent });
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				from ch in p.Children
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID }
				,
				from p in db.Parent
				from ch in p.Children
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID });
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID }
				,
				from p in db.Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID });
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID, ch.ChildID }
				,
				from p in db.Parent
				from ch in p.Children
				where p.ParentID < 4 || p.ParentID >= 4
				select new { p.ParentID, ch.ChildID });
		}

		[Test]
		public void Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				from ch in p.Children2
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID }
				,
				from p in db.Parent
				from ch in p.Children2
				where ch.ParentID < 4 || ch.ParentID >= 4
				select new { p.ParentID, ch.ChildID });
		}

		[Test]
		public void SelectMany1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.SelectMany(p => p.Children.Select(ch => p)),
				db.Parent.SelectMany(p => p.Children.Select(ch => p)));
		}

		[Test]
		public void SelectMany2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.SelectMany(p => Child.Select(ch => p)),
				db.Parent.SelectMany(p => db.Child.Select(ch => p)));
		}

		[Test]
		public void SelectMany3([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)),
				db.Parent.SelectMany(p => p.Children.Select(ch => p.ParentID)));
		}

		[Test]
		public void LeftJoin1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c },
				from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { p, c });
		}

		[Test]
		public void LeftJoin2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p },
				from p in db.Parent from c in p.Children.DefaultIfEmpty() where p.ParentID >= 4 select new { c, p });
		}

		// YDB excluded rather than gated: this hangs there rather than failing, and a test that never returns has no
		// outcome to declare - on CI the hang dump kills the whole leg. The underlying gap is #5597: the YDB-only
		// [PrimaryKey] on Parent.ParentID (added for YDB's mandatory-PK DDL requirement) is seen by the optimizer as
		// a unique key, so grouping by the Parent entity reduces it to the PK and drops the projected Value1 column.
		[Test]
		public void GroupBy1([DataSources(TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child group ch by ch.Parent into g select g.Key,
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
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent group p by p.Types!.DateTimeValue.Year into g select g.Key,
				from p in db.Parent group p by p.Types!.DateTimeValue.Year into g select g.Key);
		}

		[Test]
		public void GroupBy4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Types group p by p.DateTimeValue.Year into g select g.Key,
				from p in db.Types group p by p.DateTimeValue.Year into g select g.Key);
		}

		[Test]
		public void EqualsNull1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				from employee in dd.Employee where employee.ReportsToEmployee != null select employee.EmployeeID,
				from employee in db.Employee where employee.ReportsToEmployee != null select employee.EmployeeID);
		}

		[Test]
		public void EqualsNull2([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				from employee in dd.Employee where employee.ReportsToEmployee != null select employee,
				from employee in db.Employee where employee.ReportsToEmployee != null select employee);
		}

		[Test]
		public void EqualsNull3([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			AreEqual(
				from employee in dd.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee },
				from employee in db.Employee where employee.ReportsToEmployee != null select new { employee.ReportsToEmployee, employee });
		}

		[Test]
		public void StackOverflow1([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var dd = GetNorthwindAsList(context);
			Assert.That(
				(from employee in db.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault(), Is.EqualTo((from employee in dd.Employee where employee.Employees.Count > 0 select employee).FirstOrDefault()));
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void StackOverflow2([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent5 where p.Children.Count != 0 select p,
				from p in db.Parent5 where p.Children.Count != 0 select p);
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void StackOverflow3([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent5 where p.Children.Count() != 0 select p,
				from p in db.Parent5 where p.Children.Count() != 0 select p);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void StackOverflow4([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent5 select new { p.Children.Count },
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
			using var db = GetDataContext(context);
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
		public void TestTernary1([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			using var db = GetDataContext(context);
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

		[Test]
		public void TestTernary2([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			using var db = GetDataContext(context);
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

		[Test]
		public void TestTernary3([DataSources] string context)
		{
			var ids = new[] { 1, 5 };

			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);
			var _ = db.GetTable<ChildBaseForHierarchy>()
					.OfType<ChildForHierarchy>()
					.Select(ch => new ChildForHierarchy { Parent = ch.Parent })
					.ToList();
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void LetTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				let chs = p.Children
				select new { p.ParentID, Count = chs.Count() },
				from p in db.Parent
				let chs = p.Children
				select new { p.ParentID, Count = chs.Count() });
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void LetTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void NullAssociation([DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p1 in Parent select p1.ParentTest,
				from p1 in db.Parent select p1.ParentTest);
		}

		[Test]
		public void MultipleUse([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12Plus, TestProvName.AllMySql8Plus, TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataConnection(context);
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/148")]
		public void ChildrenCollectionsInProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

			Assert.That(list, Is.Not.Empty);
			Assert.That(list[0].Children1, Is.Not.Null);
		}

		[Table("Parent")]
		[UsedImplicitly]
		sealed class SelfAssociatedParent
		{
			[Column] public int ParentID;
			[Column(CanBeNull = true)] public int Value1;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public SelfAssociatedParent? Parent;

			[Association(ThisKey = "ParentID", OtherKey = "ParentID")]
			public List<SelfAssociatedChild> Children = null!;
		}

		[Table("Child")]
		[UsedImplicitly]
		sealed class SelfAssociatedChild
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			[Association(ThisKey = "ParentID", OtherKey = "Value1", CanBeNull = true)]
			public SelfAssociatedParent? Parent;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/170")]
		public void LeftJoinAssociationNullableValueIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var value = db.GetTable<SelfAssociatedParent>()
#pragma warning disable CS0472 // comparison of int with null
					.Where(x => x.Value1 == null)
#pragma warning restore CS0472
					.Select(x => (int?)x.Parent!.Value1)
					.First();

			Assert.That(value, Is.Null);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/170")]
		public void SelectManyAssociationNullableValueIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var actual = db.GetTable<SelfAssociatedParent>()
					.SelectMany(x => x.Children)
#pragma warning disable CS0472 // comparison of int with null
					.Where(x => x.Parent!.Value1 == null)
#pragma warning restore CS0472
					.Select(x => (int?)x.Parent!.Value1)
					.First();

			Assert.That(actual, Is.Null);
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
			using var db = GetDataContext(context);
			var value = db.GetTable<StorageTestClass>().LoadWith(x => x.Parent).First();

			Assert.That(value.Parent, Is.Not.Null);
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
			using var db = GetDataContext(context);
			var value = db.GetTable<AssociationSetterExpressionTestClass>().LoadWith(x => x.Parent).First();

			Assert.That(value.Parent, Is.Not.Null);
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
		sealed class ChildrenContainerParent
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3975")]
		public void AssociationStorageWithDifferentSetterType([DataSources] string context)
		{
			using var db = GetDataContext(context);
			// we want to make sure the conversion is not possible because we want to bypass
			// that conversion if the setter value parameter type (IEnumerable<Child> in this case)
			// does not match the member type (ChildrenContainer<Child> in this case)
			Assert.That(() =>
				db.MappingSchema.ChangeType(new List<Child>(0), typeof(ChildrenContainer<Child>)),
				Throws.TypeOf<LinqToDB.Common.LinqToDBConvertException>(), "List<Child> should not be convertible to ChildrenContainer<Child>");

			var value = db.GetTable<ChildrenContainerParent>().LoadWith(x => x.Children).First();

			Assert.That(value.Children.Value, Is.Not.Null);
		}

		[Test]
		public void TestGenericAssociation1([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			using var db = GetDataContext(context);
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

		[Test]
		public void TestGenericAssociationRuntime([DataSources(TestProvName.AllNativeAccess, TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<Top>()
				.Association( t => t.MiddleRuntime, (t, m) => t.ParentID == m!.ParentID && m.ChildID > 1 )
				.Build();

			using var db = GetDataContext(context, ms);
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

		[Test]
		public void TestGenericAssociationRuntimeMany([DataSources(TestProvName.AllSQLite)] string context)
		{
			var ids = new[] { 1, 5 };

			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			mb.Entity<Top>()
				.Association( t => t.MiddlesRuntime, (t, m) => t.ParentID == m.ParentID && m.ChildID > 1 )
				.Build();

			using var db = GetDataContext(context, ms);
			var q =
					from t in db.GetTable<Top>()
					from m in t.MiddlesRuntime
					where ids.Contains(t.ParentID)
					orderby t.ParentID
					select new {t, m};

			var list = q.ToList();

			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestGenericAssociation2([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void TestGenericAssociation3([DataSources(ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void TestGenericAssociation4([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void ExtensionTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Parent.SelectMany(_ => _.Children),
			db.Parent.SelectMany(_ => _.Children()));
		}

		[Test]
		public void ExtensionTest11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Parent.SelectMany(_ => _.Children),
			db.Parent.SelectMany(_ => AssociationExtension.Children(_)));
		}

		[Test]
		public void ExtensionTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => _.Parent),
			db.Child.Select(_ => _.Parent()));
		}

		[Test]
		public void ExtensionTest21([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => _.Parent),
			db.Child.Select(_ => AssociationExtension.Parent(_)));
		}

		[Test]
		public void ExtensionTest3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => new { p = _.Parent! }).Select(_ => _.p.ParentID),
			db.Child.Select(_ => new { p = _.Parent() }).Select(_ => _.p.ParentID));
		}

		[Test]
		public void ExtensionTest4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => new { c = _, p = _.Parent }).Select(_ => _.c.Parent),
			db.Child.Select(_ => new { c = _, p = _.Parent() }).Select(_ => _.c.Parent()));
		}

		[Test]
		public void QueryableExtensionTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Parent.SelectMany(_ => _.Children),
			db.Parent.SelectMany(_ => _.QueryableChildren(db)));
		}

		[Test]
		public void QuerableExtensionTest11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Parent.SelectMany(_ => _.Children),
			db.Parent.SelectMany(_ => AssociationExtension.QueryableChildren(_, db)));
		}

		[Test]
		public void QueryableExtensionTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => _.Parent),
			db.Child.SelectMany(_ => _.QueryableParent(db)));
		}

		[Test]
		public void QueryableExtensionTest21([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
			   Child.Select(_ => _.Parent),
			db.Child.SelectMany(_ => AssociationExtension.QueryableParent(_, db)));
		}

		[Test]
		public void DistinctSelect([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
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
			[PrimaryKey] public int ID { get; set; }

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
			[PrimaryKey] public int ParentID { get; set; }

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
			using var db = GetDataContext(context);
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
			[PrimaryKey] public int Id { get; set; }
		}

		public class Lookup
		{
			[PrimaryKey] public int     Id   { get; set; }
			public string? Type { get; set; }
		}

		public class Resource
		{
			[PrimaryKey] public int  Id    { get; set; }
			public int  AssociatedObjectId { get; set; }
			public int? AssociationTypeId  { get; set; }

			[Association(ThisKey   = nameof(AssociationTypeId), OtherKey  = nameof(Lookup.Id), CanBeNull = true)]
			public Lookup? AssociationTypeCode { get; set; }

			public static Expression<Func<Resource, IDataContext, IQueryable<User>>> UserExpression =>
				(r, db) => db.GetTable<User>().Where(c => r.AssociationTypeCode!.Type == "us" && c.Id == r.AssociatedObjectId);

			[Association(QueryExpressionMethod = nameof(UserExpression))]
			public User? User { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1614")]
		public void LoadWithQueryExpressionAssociation([DataSources(TestProvName.AllNativeAccess)] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/845")]
		public void ExpressionPredicateAssociationNegation([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllSQLite)] string context)
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

		sealed class SqlPropertyAssociationEntity
		{
			[PrimaryKey] public long Id { get; set; }
		}

		sealed class SqlPropertyAssociationRelation
		{
			[PrimaryKey] public long EntityId { get; set; }

			public bool Deleted { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1711")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void FluentSqlPropertyAssociationByKeys([DataSources(TestProvName.AllNativeAccess, TestProvName.AccessLibRedMdb, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<SqlPropertyAssociationEntity>()
				.HasTableName("SqlPropertyAssociationEntity")
				.HasPrimaryKey(x => Sql.Property<long>(x, "Id"))
				.Association(x => Sql.Property<IQueryable<SqlPropertyAssociationRelation>>(x, "relationship"), e => e.Id, r => r.EntityId)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var entity = db.CreateLocalTable<SqlPropertyAssociationEntity>())
			using (db.CreateLocalTable<SqlPropertyAssociationRelation>())
			{
				var result1 = entity
					.Where(t => Sql.Property<IQueryable<SqlPropertyAssociationRelation>>(t, "relationship").Any())
					.ToList();
			}
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1711")]
		public void FluentSqlPropertyAssociationByQuery([DataSources(TestProvName.AllNativeAccess, TestProvName.AccessLibRedMdb, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<SqlPropertyAssociationEntity>()
				.HasTableName("SqlPropertyAssociationEntity")
				.HasPrimaryKey(x => Sql.Property<long>(x, "Id"))
				.Association(x => Sql.Property<IQueryable<SqlPropertyAssociationRelation>>(x, "relationship"), (e, db) => db.GetTable<SqlPropertyAssociationRelation>()
						.Where(r => r.Deleted == false && r.EntityId == e.Id))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var entity = db.CreateLocalTable<SqlPropertyAssociationEntity>())
			using (db.CreateLocalTable<SqlPropertyAssociationRelation>())
			{
				var result1 = entity
					.Where(t => Sql.Property<IQueryable<SqlPropertyAssociationRelation>>(t, "relationship").Any())
					.ToList();
			}
		}

		[Table]
		sealed class TaskWithStage
		{
			[PrimaryKey] public int Pk { get; set; }

			[Column] public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public string? TargetName { get; set; }

			[Association(ExpressionPredicate = nameof(ActualStageExp))]
			public TaskStageRecord ActualStage { get; set; } = null!;

			private static Expression<Func<TaskWithStage, TaskStageRecord, bool>> ActualStageExp()
				=> (t, ts) => t.Id == ts.TaskId && ts.Actual == true;
		}

		[Table]
		sealed class TaskStageRecord
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public int TaskId { get; set; }

			[Column]
			public bool Actual { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1096")]
		public void DistinctWithExpressionPredicateAssociation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TaskWithStage>())
			using (db.CreateLocalTable<TaskStageRecord>())
			{
				db.Insert(new TaskWithStage { Pk = 1, Id = 1, TargetName = "bda.Requests" });
				db.Insert(new TaskStageRecord { Id = 1, TaskId = 1, Actual = true });

				var query = db.GetTable<TaskWithStage>()
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

		public interface IHasOwnerId
		{
			int OwnerId { get; set; }
		}

		public abstract class GenericOwnedEntityBase<T> where T : IHasOwnerId
		{
			/// <summary>
			/// Owner.
			/// </summary>
			[Association(ExpressionPredicate = nameof(OwnerPredicate), CanBeNull = true)]
			public GenericOwnerEntity? Owner { get; set; }

			public static Expression<Func<T, GenericOwnerEntity, bool>> OwnerPredicate { get; set; } = (entity, owner) => entity.OwnerId == owner.Id;
		}

		[Table]
		public class GenericOwnedEntity: GenericOwnedEntityBase<GenericOwnedEntity>, IHasOwnerId
		{
			[Column] public int OwnerId { get; set; }
		}

		[Table]
		public class GenericOwnerEntity
		{
			[Column] public int Id { get; set; }

		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2981")]
		public void GenericExpressionPredicateAssociation([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(new[]
			{
				new GenericOwnedEntity {OwnerId = 1},
				new GenericOwnedEntity {OwnerId = 2}
			});
			using var t2 = db.CreateLocalTable(new[] {new GenericOwnerEntity {Id = 1}});

			var res = t1.Select(r => new {r.OwnerId, Id = (int?)r.Owner!.Id})
				.OrderBy(_ => _.OwnerId)
				.ToArray();

			res.Length.ShouldBe(2);
			res[0].Id.ShouldBe(1);
			res[0].OwnerId.ShouldBe(1);
			res[1].OwnerId.ShouldBe(2);
			res[1].Id.ShouldBeNull();
		}

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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3260")]
		public void SumOverAssociationDefaultIfEmpty([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<LeaveRequest>();
			using var t2 = db.CreateLocalTable<LeaveRequestDateEntry>();
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2966")]
		public void AssociationOverConcatQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Patient.Concat(db.Patient).Select(r => new { r.Diagnosis, r.Person.FirstName }).ToArray();
		}

		[Table]
		public class SubData2
		{
			[PrimaryKey, Identity] public int  PK { get; set; }
			[Column] public int     Id { get; set; }
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
			[PrimaryKey] public int Id { get; set; }

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
			[PrimaryKey] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubData1.Id), CanBeNull = true)]
			public SubData1? SubData { get; }

			public static readonly Data[] Records = new[]
			{
				new Data() { Id = 1 },
				new Data() { Id = 2 },
				new Data() { Id = 3 },
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3557")]
		public void NestedAssociationFirstOrDefaultWithNullCheck([DataSources(
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3557")]
		public void NestedAssociationFirstOrDefaultWithCoalesce([DataSources(
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3557")]
		public void NestedAssociationFirstOrDefault([DataSources(
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3809")]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void ConditionalProjectionWithChildrenAnyFilter([DataSources(TestProvName.AllClickHouse)] string context)
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
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
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

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test]
		public void OptionalAssociationNonNullCorrelationWithProjection([DataSources] string context)
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
			[PrimaryKey] public int Id { get; set; }

			public int MainEntityId { get; set; }

			public MainEntity MainEntity { get; set; } = null!;
		}

		static IQueryable<T> OnlyWithSubEntities<T>(IQueryable<T> query)
			where T : IHasSubentities
		{
			return query.Where(x => x.SubEntities.Any());
		}

		[ThrowsRequiresCorrelatedSubquery]
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

		[ThrowsRequiresCorrelatedSubquery]
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

		// mutates the global Configuration.Sql.AssociationAlias
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2022"), NonParallelizable]
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

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2933")]
		public void OneToManyAssociationKeepsLeftJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(OneToManyCar.Data);
			using var t2 = db.CreateLocalTable(OneToManyPerson.Data);
			using var t3 = db.CreateLocalTable(OneToManyPet.Data);

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

		sealed class OneToManyCar
		{
			[PrimaryKey] public int Id;

			public int? PersonId { get; set; }

			[Association(ThisKey = nameof(PersonId), OtherKey = nameof(OneToManyPerson.Id), CanBeNull = true)]
			public OneToManyPerson? Person { get; set; }

			public static readonly OneToManyCar[] Data =
			[
				new() { Id = 1, PersonId = 1 },
				new() { Id = 2 }
			];
		}

		sealed class OneToManyPerson
		{
			[PrimaryKey] public int Id;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(OneToManyPet.PersonId), CanBeNull = true)]
			public IEnumerable<OneToManyPet> PetIds { get; set; } = null!;

			public static readonly OneToManyPerson[] Data =
			[
				new() { Id = 1 }
			];
		}

		sealed class OneToManyPet
		{
			[PrimaryKey] public int Id;

			[Column, NotNull] public string Name { get; set; } = null!;

			[Column] public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = nameof(OneToManyPerson.Id), CanBeNull = false)]
			public OneToManyPerson Person { get; set; } = null!;

			public static readonly OneToManyPet[] Data =
			[
				new() { Id = 1, PersonId = 1, Name = "Snuffles" },
				new() { Id = 2, PersonId = 1, Name = "Buddy" },
			];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4454")]
		public void ExpressionPredicateNotNullableAssociationUsesInnerJoin([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<ExpressionAssociationClient>();
			using var t2 = db.CreateLocalTable<ExpressionAssociationService>();

			t2.Select(s => s.Client1.Name).ToArray();

			Assert.That(db.LastQuery, Does.Contain("INNER JOIN"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4454")]
		public void QueryExpressionNotNullableAssociationUsesInnerJoin([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			using var t1 = db.CreateLocalTable<ExpressionAssociationClient>();
			using var t2 = db.CreateLocalTable<ExpressionAssociationService>();

			t2.Select(s => s.Client2.Name).ToArray();

			Assert.That(db.LastQuery, Does.Contain("INNER JOIN"));
		}

		sealed class ExpressionAssociationClient
		{
			[PrimaryKey] public int Id { get; set; }
			public string? Name { get; set; }
		}

		sealed class ExpressionAssociationService
		{
			[PrimaryKey] public int Id { get; set; }
			public int? IdClient { get; set; }

			[Association(ExpressionPredicate = nameof(Client_ExprPr), CanBeNull = false)]
			public ExpressionAssociationClient Client1 { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(Client_QExpr), CanBeNull = false)]
			public ExpressionAssociationClient Client2 { get; set; } = null!;

			// works fine
			static Expression<Func<ExpressionAssociationService, ExpressionAssociationClient, bool>> Client_ExprPr =>
			    (s, c) => s.IdClient == c.Id;

			// always generates left join or outer apply (if query is more complicated)
			static Expression<Func<ExpressionAssociationService, IDataContext, IQueryable<ExpressionAssociationClient>>> Client_QExpr =>
				(s, db) => db.GetTable<ExpressionAssociationClient>().Where(c => c.Id == s.IdClient);
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllSybase], ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3822")]
		public void ChainedExpressionMethodAndAssociationWithTake([DataSources] string context)
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
			[PrimaryKey] public int Id { get; set; }

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
			[PrimaryKey] public int Id { get; set; }

			public int HouseId { get; set; }

			[Association(ThisKey = nameof(HouseId), OtherKey = nameof(House.Id), CanBeNull = false)]
			public House House { get; set; } = null!;
		}

		public class House
		{
			[PrimaryKey] public int Id { get; set; }

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
			[PrimaryKey] public int Id { get; set; }

			public int Position { get; set; }
		}

		[Test]
		[ActiveIssue(5590, Configurations = new[] { TestProvName.AllYdb }, ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "The LINQ expression 'p.Children != null' could not be converted to SQL.",
			Details = "YDB swallows the correlated-subquery rejection into a generic conversion error. ClickHouse was dropped from the scope: the sweep ran it with the gate open and it did not fail, so the claim that it returns wrong results is stale.")]
		public void ManyAssociationEmptyCheck1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var parents = db.Parent.Where(p => p.Children != null).ToArray();

			Assert.That(parents, Has.Length.EqualTo(6));
			Assert.That(parents.Any(p => p.ParentID == 5), Is.False);
		}

		[Test]
		[ActiveIssue(5590, Configurations = new[] { TestProvName.AllYdb }, ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "The LINQ expression 'p.Children == null' could not be converted to SQL.",
			Details = "as ManyAssociationEmptyCheck1, with the equality direction. ClickHouse dropped from the scope for the same reason.")]
		public void ManyAssociationEmptyCheck2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var parents = db.Parent.Where(p => p.Children == null).ToArray();

			Assert.That(parents, Has.Length.EqualTo(1));
			Assert.That(parents[0].ParentID, Is.EqualTo(5));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4274")]
		public void NavigationPropertyOverGroupedProjection([DataSources(false)] string context)
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

		[Test]
		public void TableNotFoundRegressionTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ = db.GrandChild.Where(r => r.Child!.Parent!.ParentID == 1).Any();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1196")]
		public void ExpressionPredicateAssociationAfterSelectMany([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using (db.CreateLocalTable<FirmRequest>())
			using (db.CreateLocalTable<FirmInfo>())
			using (db.CreateLocalTable<RequestAssignment>())
			{
				db.Insert(new FirmRequest { Id = 1002, FirmId = 1 });
				db.Insert(new FirmInfo { Id = 1 });
				db.Insert(new RequestAssignment { Id = 1, TargetId = 1, DirectionId = new Guid("c5c0a778-694e-49d1-b1a0-f8ef5569c673") });

				var query =
						db.GetTable<FirmRequest>()
						.Where(r => r.Id == 1002)
						.Select(r => r.FirmInfo!)
						.SelectMany(r => r.Requests)
						.Select(r => new
						{
							Instance = r,
							Instance2 = r.DocPrepareAssignment,
						});

				var res = query.ToArray();

				var query2 =
						db.GetTable<FirmRequest>()
						.Where(r => r.Id == 1002)
						.Select(r => r.FirmInfo!)
						.SelectMany(r => r.Requests)
						.Select(r => r.DocPrepareAssignment);

				var res2 = query.ToArray();
			}
		}

		[Table(Name ="Requests")]
		sealed class FirmRequest
		{
			[Column] public int Id { get; set; }
			[Column] public int FirmId { get; set; }

			[Association(ThisKey ="FirmId", OtherKey ="Id")]
			public FirmInfo? FirmInfo { get; set; }

			[Association(ExpressionPredicate = nameof(DocPrepareAssignmentExp))]
			public RequestAssignment? DocPrepareAssignment { get; set; }

			private static Expression<Func<FirmRequest, RequestAssignment, bool>> DocPrepareAssignmentExp()
				=> (r, a) => a.TargetId == r.Id;
		}

		[Table(Name ="FirmInfo")]
		sealed class FirmInfo
		{
			[Column]public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FirmId")]
			public IEnumerable<FirmRequest> Requests { get; set; } = null!;
		}

		[Table(Name ="Assignments")]
		sealed class RequestAssignment
		{
			[PrimaryKey, Identity] public int Id { get; set; } // Int
			[Column, NotNull] public Guid DirectionId { get; set; }
			[Column, Nullable] public int TargetId { get; set; } // varchar(50)
			[Column, Nullable] public DateTime? DateRevoke { get; set; } // varchar(50)
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromAttributeAssociationByKeys([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				db.Insert(new ForumTopic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<ForumTopic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA1.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromAttributeAssociationByExpression([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				db.Insert(new ForumTopic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<ForumTopic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA2.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromAttributeAssociationByQuery([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				db.Insert(new ForumTopic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<ForumTopic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesA3.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromFluentAssociationByExpression([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ForumTopic>()
					.Property(e => e.Id)
					.Association(e => e.MessagesF1, (t, m) => t.Id == m.TopicId)
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<ForumMessage>()
					.Property(e => e.Id)
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using var db = GetDataContext(context, ms);
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				db.Insert(new ForumTopic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<ForumTopic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesF1.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromFluentAssociationByKeys([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ForumTopic>()
					.Property(e => e.Id)
					.Association(e => e.MessagesF2, t => t.Id, m => m.TopicId)
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<ForumMessage>()
					.Property(e => e.Id)
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using var db = GetDataContext(context, ms);
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				db.Insert(new ForumTopic() { Id = 6, Title = "title", Text = "text" });

				db.GetTable<ForumTopic>()
					.Where(x => x.Id == 6)
					.Select(x =>
					new
					{
						Topic = x,
						MessagesIds = x.MessagesF2.Select(t => t.Id).ToList()
					}).FirstOrDefault();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1498")]
		public void ProjectIdsFromFluentAssociationByQueryWithKeys([DataSources] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ForumTopic>()
					.Property(e => e.Id).IsPrimaryKey()
					.Association(e => e.MessagesF3, (t, ctx) => ctx.GetTable<ForumMessage>().Where(m => m.TopicId == t.Id))
					.Property(e => e.Title)
					.Property(e => e.Text)
				.Entity<ForumMessage>()
					.Property(e => e.Id).IsPrimaryKey()
					.Property(e => e.TopicId)
					.Property(e => e.Text)
				.Build();

			using var db = GetDataContext(context, ms);
			using (db.CreateLocalTable<ForumTopic>())
			using (db.CreateLocalTable<ForumMessage>())
			{
				var topic = new ForumTopic { Id = 6, Text = "text", Title = "title" };

				db.Insert(topic);
				db.Insert(new ForumMessage { Id = 60, Text = "message", TopicId = 6 });
				db.Insert(new ForumMessage { Id = 61, Text = "message", TopicId = 7 });

				var result = db.GetTable<ForumTopic>()
						.Where(x => x.Id == 6)
						.Select(x =>
						new
						{
							Topic = x,
							MessagesIds = x.MessagesF3.Select(t => t.Id).ToList()
						}).FirstOrDefault()!;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result, Is.Not.Null);
					Assert.That(topic.Id, Is.EqualTo(result.Topic.Id));
					Assert.That(topic.Text, Is.EqualTo(result.Topic.Text));
					Assert.That(topic.Title, Is.EqualTo(result.Topic.Title));
					Assert.That(new[] { 60 }, Is.EqualTo(result.MessagesIds));
				}
			}
		}

		public class ForumTopic
		{
			[PrimaryKey] public int Id { get; set; }

			public string? Title { get; set; }

			public string? Text { get; set; }

			[Association(ThisKey = "Id", OtherKey = "TopicId")]
			public virtual ICollection<ForumMessage> MessagesA1 { get; set; } = null!;

			[Association(ExpressionPredicate = nameof(Predicate))]
			public virtual ICollection<ForumMessage> MessagesA2 { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(Query))]
			public virtual ICollection<ForumMessage> MessagesA3 { get; set; } = null!;

			public virtual ICollection<ForumMessage> MessagesF1 { get; set; } = null!;
			public virtual ICollection<ForumMessage> MessagesF2 { get; set; } = null!;
			public virtual ICollection<ForumMessage> MessagesF3 { get; set; } = null!;

			static Expression<Func<ForumTopic, ForumMessage, bool>> Predicate => (t, m) => t.Id == m.TopicId;

			static Expression<Func<ForumTopic, IDataContext, IQueryable<ForumMessage>>> Query => (t, ctx) => ctx.GetTable<ForumMessage>().Where(m => m.TopicId == t.Id);
		}

		public class ForumMessage
		{
			[PrimaryKey] public int Id { get; set; }

			public int TopicId { get; set; }

			[Association(ThisKey = "TopicId", OtherKey = "Id")]
			public virtual ForumTopic? Topic { get; set; }

			public string? Text { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1885")]
		public void FluentAssociationFromSqlClientProjection([IncludeDataSources(ProviderName.SqlCe, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var ms = new MappingSchema();
			var mb = new FluentMappingBuilder(ms);

			var values = new[] { 1, 5 };

			mb.Entity<FromSqlAssociationUser>()
				.Association(t => t.Data, (t, db) => db.FromSql<FromSqlAssociationRow>("SELECT 'Testing' AS Data")
					.Where(x => t.Id == t.Id) // Necessary so that AssociationParentJoin is not null
					.Select(x => new FromSqlAssociationData
					{
						Ids = values,
						Value = x.Data
					})
				)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (var u = db.CreateLocalTable<FromSqlAssociationUser>())
			{
				u.Insert(() => new FromSqlAssociationUser { Id = 1 });
				u.Insert(() => new FromSqlAssociationUser { Id = 2 });

				var q =
					from t in db.GetTable<FromSqlAssociationUser>()
					select t.Data;

				var list = q.ToList();

				Assert.That(list, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(list[0].Value, Is.EqualTo("Testing"));
					Assert.That(list[0].Ids, Is.EqualTo(values));

					Assert.That(list[1].Value, Is.EqualTo("Testing"));
					Assert.That(list[1].Ids, Is.EqualTo(values));
				}
			}
		}

		public class FromSqlAssociationUser
		{
			public int Id { get; set; }
			public FromSqlAssociationData? Data { get; set; }
		}

		public class FromSqlAssociationData
		{
			[NotColumn]
			public int[]? Ids { get; set; }

			[Column]
			public string? Value { get; set; }
		}

		public class FromSqlAssociationRow
		{
			public string? Data { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2506")]
		public void EagerLoadFilteredByParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values("en", "de")] string lang)
		{
			var items = new EagerFilterItem[]
			{
				new EagerFilterItem { Id = 1, Name = "Item 1" },
				new EagerFilterItem { Id = 2, Name = "Item 2" }
			};

			var itemTexts = new EagerFilterItemText[]
			{
				new EagerFilterItemText{ ItemId = 1, Lang = "de", Text = "Item 1 german text"},
				new EagerFilterItemText{ ItemId = 1, Lang = "en", Text = "Item 1 english text"},
				new EagerFilterItemText{ ItemId = 2, Lang = "de", Text = "Item 2 german text"},
				new EagerFilterItemText{ ItemId = 2, Lang = "en", Text = "Item 2 english text"},
			};

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(items))
			using (db.CreateLocalTable(itemTexts))
			{
				var query = db.GetTable<EagerFilterItem>().Select(item => new
				{
					item.Name,
					Texts = item.ItemTexts.Where(x => x.Lang == lang).Select(itemText => new
					{
						itemText.Lang,
						itemText.Text
					}).ToArray()
				}).ToArray();

				var expected = items.Select(item => new
				{
					item.Name,
					Texts = itemTexts.Where(x => x.ItemId == item.Id && x.Lang == lang).Select(itemText => new
					{
						itemText.Lang,
						itemText.Text
					}).ToArray()
				}).ToArray();

				AreEqualWithComparer(expected, query);
			}
		}

		[Table(Schema = "dbo", Name = "EagerFilterItem")]
		public partial class EagerFilterItem
		{
			[PrimaryKey, NotNull] public int Id { get; set; } // int
			[Column, NotNull] public string Name { get; set; } = null!; // nvarchar(50)

			#region Associations

			/// <summary>
			/// FK_ItemText_Item_BackReference
			/// </summary>
			[Association(ThisKey = "Id", OtherKey = "ItemId", CanBeNull = true)]
			public IEnumerable<EagerFilterItemText> ItemTexts { get; set; } = null!;

			#endregion
		}

		[Table(Schema = "dbo", Name = "EagerFilterItemText")]
		public partial class EagerFilterItemText
		{
			[PrimaryKey(1), NotNull] public int ItemId { get; set; } // int
			[PrimaryKey(2), NotNull] public string Lang { get; set; } = null!; // varchar(2)
			[Column, NotNull] public string Text { get; set; } = null!; // nvarchar(50)

			#region Associations

			/// <summary>
			/// FK_ItemText_Item
			/// </summary>
			[Association(ThisKey = "ItemId", OtherKey = "Id", CanBeNull = false)]
			public EagerFilterItem Item { get; set; } = null!;

			#endregion
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2895")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedAssociationCollectionsInProjection([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<NestedAssocInternalEmail>(new[] { new NestedAssocInternalEmail { Id = 10, UserId = 1, RequestId = 1 } }))
			using (db.CreateLocalTable<NestedAssocRequest>(new[] { new NestedAssocRequest { Id = 1, UserId = 1 } }))
			using (db.CreateLocalTable<NestedAssocUser>(new[] { new NestedAssocUser { Id = 1 } }))
			using (db.CreateLocalTable<NestedAssocAdmin>(new[] { new NestedAssocAdmin { Id = 1 } }))
			using (db.CreateLocalTable<NestedAssocEmailAdmin>(new[] { new NestedAssocEmailAdmin { AdminId = 1, EmailId = 10 } }))
			using (db.CreateLocalTable<NestedAssocEmail>(new[] { new NestedAssocEmail { Id = 10 } }))
			using (db.CreateLocalTable<NestedAssocEmailAttachment>(new[] { new NestedAssocEmailAttachment { EmailId = 10, AttachmentId = 100 } }))
			using (db.CreateLocalTable<NestedAssocAttachment>(new[] { new NestedAssocAttachment { Id = 100 } }))
			using (db.CreateLocalTable<NestedAssocDocument>(new[]
			{
				new NestedAssocDocument{AttachmentId = 100, Name = "Some Doc 1", Position = 1},
				new NestedAssocDocument{AttachmentId = 100, Name = "Some Doc 2", Position = 2},
				new NestedAssocDocument{AttachmentId = 101, Name = "Some Doc x", Position = 1},
			}))
			{
				var result = db.GetTable<NestedAssocRequest>()
					.Select(r => new
					{
						DocumentName = r.User.Admin.EmailAdminAssociations
							.Select(ea => ea.Email.InternalEmail)
							.Select(ie => new
							{
								Names = ie.Email.EmailAttachmentAssociations
									.SelectMany(eba => eba.Attachment.Documents.Select(aa => aa.Name)),
							})
							.FirstOrDefault(),
					})
					.ToArray();

				result.Count().ShouldBe(1);
				result[0].DocumentName?.Names.Count().ShouldBe(2);
			}
		}

		[Table]
		public class NestedAssocInternalEmail
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Column]
			public int? RequestId { get; set; }

			[Column]
			public int? UserId { get; set; }

			[Association(ThisKey = nameof(RequestId), OtherKey = nameof(NestedAssocRequest.Id))]
			public virtual NestedAssocRequest Request { get; set; } = null!;

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(NestedAssocUser.Id))]
			public virtual NestedAssocUser User { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocEmail.Id))]
			public virtual NestedAssocEmail Email { get; set; } = null!;
		}

		[Table]
		public class NestedAssocRequest
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Column]
			public int UserId { get; set; }

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(NestedAssocUser.Id))]
			public virtual NestedAssocUser User { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocInternalEmail.Id))]
			public virtual ICollection<NestedAssocInternalEmail> InternalEmails { get; set; } = null!;
		}

		[Table]
		public class NestedAssocUser
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocAdmin.Id))]
			public virtual NestedAssocAdmin Admin { get; set; } = null!;
		}

		[Table]
		public class NestedAssocAdmin
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocEmailAdmin.AdminId))]
			public virtual ICollection<NestedAssocEmailAdmin> EmailAdminAssociations { get; set; } = null!;
		}

		[Table]
		public class NestedAssocEmailAdmin
		{
			[PrimaryKey(1)]
			[Column]
			public int EmailId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int AdminId { get; set; }

			[Association(ThisKey = nameof(EmailId), OtherKey = nameof(NestedAssocEmail.Id))]
			public virtual NestedAssocEmail Email { get; set; } = null!;

			[Association(ThisKey = nameof(AdminId), OtherKey = nameof(NestedAssocAdmin.Id))]
			public virtual NestedAssocAdmin Admin { get; set; } = null!;
		}

		[Table]
		public class NestedAssocEmail
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocInternalEmail.Id))]
			public virtual NestedAssocInternalEmail InternalEmail { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocEmailAttachment.EmailId))]
			public virtual ICollection<NestedAssocEmailAttachment> EmailAttachmentAssociations { get; set; } = null!;
		}

		[Table]
		public class NestedAssocEmailAttachment
		{
			[PrimaryKey(1)]
			[Column]
			public int EmailId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int AttachmentId { get; set; }

			[Association(ThisKey = nameof(AttachmentId), OtherKey = nameof(NestedAssocAttachment.Id))]
			public virtual NestedAssocAttachment Attachment { get; set; } = null!;

			[Association(ThisKey = nameof(EmailId), OtherKey = nameof(NestedAssocEmail.Id))]
			public virtual NestedAssocEmail Email { get; set; } = null!;
		}

		[Table]
		public class NestedAssocAttachment
		{
			[PrimaryKey, Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NestedAssocDocument.AttachmentId))]
			public virtual ICollection<NestedAssocDocument> Documents { get; set; } = null!;
		}

		[Table]
		public class NestedAssocDocument
		{
			[PrimaryKey(1)]
			[Column]
			public int AttachmentId { get; set; }

			[PrimaryKey(2)]
			[Column]
			public int Position { get; set; }

			[Column]
			public string Name { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2900")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void AssociationCollectionFirstOrDefaultWithConditionalProjection([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var requests = db.CreateLocalTable<RequestWithMetrics>())
			using (db.CreateLocalTable<RequestMetricRow>())
			{
				var result = requests
					.Select(a => new
					{
						Metrics = a.Metrics
							.Select(aa => new
							{
								Value = aa.Value.HasValue
									? new { Value = aa.Value.Value }
									: null,
							})
							.FirstOrDefault()
					})
					.ToArray();
			}
		}

		[Table]
		public class RequestWithMetrics
		{
			[PrimaryKey, Identity, Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(RequestMetricRow.RequestId))]
			public virtual ICollection<RequestMetricRow> Metrics { get; set; } = null!;
		}

		[Table]
		public class RequestMetricRow
		{
			[PrimaryKey, Identity, Column] public int     Id        { get; set; }
			[Column                      ] public int     RequestId { get; set; }
			[Column                      ] public double? Value     { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3061")]
		public void AssociationProjectionColumnsOptimization([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<ColOptProperties>();
			using var t2 = db.CreateLocalTable<ColOptCaseLog>();
			using var t3 = db.CreateLocalTable<ColOptCaseLogProperty>();
			using var t4 = db.CreateLocalTable<ColOptIncident>();
			using var t5 = db.CreateLocalTable<ColOptIncidentProperty>();

			var query = db.GetTable<ColOptProperties>()
					.Where(x => x.Id.In(1, 2))
					.Select(x => new
					{
						CaseNumber     = x.CaseLogProperties.FirstOrDefault()!.CaseLog.Number,
						IncidentNumber = x.IncidentProperties.FirstOrDefault()!.Incident.EventNumber
					});

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(2);

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3487")]
		public void ColumnsOptimizationForTwoFirstOrDefaultCollectionProjections([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<ColOptRoot>();
			using var t2 = db.CreateLocalTable<ColOptNarrativeDraft>();
			using var t3 = db.CreateLocalTable<ColOptSynopsisDraft>();

			var query = db.GetTable<ColOptRoot>()
					.Select(x => new
					{
						NarrativeDraft = x.SomeDrafts
							.Select(y => new
							{
								Html   = y.Html,
								Plain2 = y.Plain
							})
							.FirstOrDefault(),
						SynopsisDraft = x.OtherDrafts
							.Select(y => new
							{
								Html  = y.Html,
								Plain = y.Plain
							})
							.FirstOrDefault()
					});

			query.GetSelectQuery().Select.Columns.Count.ShouldBe(6);
			query.ToArray();
		}

		[Table]
		sealed class ColOptProperties
		{
			[PrimaryKey]
			public int     Id    { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ColOptCaseLogProperty.PropertyId))]
			public List<ColOptCaseLogProperty> CaseLogProperties { get;   set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ColOptIncidentProperty.PropertyId))]
			public List<ColOptIncidentProperty> IncidentProperties { get; set; } = null!;
		}

		sealed class ColOptCaseLog
		{
			[PrimaryKey]
			public int  Id     { get; set; }

			public int? Number { get; set; }
		}

		sealed class ColOptIncident
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = true)]
			public int? EventNumber { get; set; }
		}

		sealed class ColOptCaseLogProperty
		{
			[Column(CanBeNull = true)]
			public int? PropertyId { get; set; }
			[Column(CanBeNull = true)]
			public int? CaseLogId { get; set; }

			[Association(ThisKey = nameof(CaseLogId), OtherKey = nameof(ColOptCaseLog.Id))]
			public ColOptCaseLog CaseLog { get; set; } = null!;
		}

		sealed class ColOptIncidentProperty
		{
			[Column(CanBeNull = true)]
			public int? PropertyId { get; set; }
			[Column(CanBeNull = true)]
			public int? IncidentId  { get; set; }

			[Association(ThisKey = nameof(IncidentId), OtherKey = nameof(ColOptIncident.Id))]
			public ColOptIncident Incident { get; set; } = null!;
		}

		[Table]
		sealed class ColOptRoot
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ColOptNarrativeDraft.RootId))]
			public ICollection<ColOptNarrativeDraft> SomeDrafts { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ColOptSynopsisDraft.RootId))]
			public ICollection<ColOptSynopsisDraft> OtherDrafts { get; set; } = null!;
		}

		[Table]
		sealed class ColOptNarrativeDraft
		{
			[Column] public int     RootId { get; set; }
			[Column] public string? Html   { get; set; }
			[Column] public string? Plain  { get; set; }
		}

		[Table]
		sealed class ColOptSynopsisDraft
		{
			[Column] public int     RootId { get; set; }
			[Column] public string? Html   { get; set; }
			[Column] public string? Plain  { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3109")]
		public void LoadWithAssociationsOverByteArrayKeys([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)]
			string context)
		{
			ByteKeyLeft      left      = new() { LeftId  = 1 };
			ByteKeyRight     right     = new() { RightId = new byte[] { 2 } };
			ByteKeyLeftRight leftRight = new() { LeftId  = 1, RightId = new byte[] { 2 } };

			using (var db = new ByteKeyAssociationDb(context))
			using (db.CreateLocalTable(new[] { left }))
			using (db.CreateLocalTable(new[] { right }))
			using (db.CreateLocalTable(new[] { leftRight }))
			{
				
				var leftRightItem = db.LeftRights
					.LoadWith(x => x.Left)
					.LoadWith(x => x.Right)
					.First();

				var leftItem      = db.Lefts.LoadWith(x => x.LeftRights).First();
				var rightItem     = db.Rights.LoadWith(x => x.LeftRights).First();

				if (leftRightItem.Left == null)
				{
					throw new InvalidOperationException("LeftRight type failed to load Left association.");
				}

				if (leftRightItem.Right == null)
				{
					throw new InvalidOperationException("LeftRight type failed to load Right association.");
				}

				if (leftItem.LeftRights?.Count() != 1)
				{
					throw new InvalidOperationException("Left type failed to load LeftRight association.");
				}
				
				if (rightItem.LeftRights.Count() != 1)
				{
					throw new InvalidOperationException("Right type failed to load LeftRight association.");
				}
			}
		}

		private sealed class ByteKeyAssociationDb : DataConnection
		{
			public ByteKeyAssociationDb(string configuration) : base(configuration)
			{
			}

			public ITable<ByteKeyLeft>      Lefts      => this.GetTable<ByteKeyLeft>();
			public ITable<ByteKeyLeftRight> LeftRights => this.GetTable<ByteKeyLeftRight>();
			public ITable<ByteKeyRight>     Rights     => this.GetTable<ByteKeyRight>();
		}

		private sealed class ByteKeyLeft
		{
			[Association(ThisKey = nameof(LeftId), OtherKey = nameof(ByteKeyLeftRight.LeftId))]
			public IEnumerable<ByteKeyLeftRight>? LeftRights;

			[Column(IsPrimaryKey = true, CanBeNull = false)]
			public int LeftId { get; set; }

			public string?             LeftData { get; set; }
			public IEnumerable<ByteKeyRight>? Rights   => LeftRights?.Select(x => x.Right!);

			public override string ToString()
			{
				return $"{nameof(ByteKeyLeft)} : [{LeftId} : {LeftData}]";
			}
		}

		private sealed class ByteKeyLeftRight
		{
			[Column(IsPrimaryKey = true, CanBeNull = false)]
			public int LeftId { get; set; }

			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.Blob)]
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.VarBinary, Configuration = ProviderName.ClickHouse)]
			public byte[] RightId { get; set; } = null!;

			[Association(ThisKey = nameof(LeftId), OtherKey = nameof(ByteKeyLeft.LeftId))]
			public ByteKeyLeft? Left { get; set; }

			[Association(ThisKey = nameof(RightId), OtherKey = nameof(ByteKeyRight.RightId))]
			public ByteKeyRight? Right { get; set; }

			public override string ToString()
			{
				return $"{nameof(ByteKeyLeftRight)} : [{LeftId} <=> {BitConverter.ToString(RightId)}]";
			}
		}

		private sealed class ByteKeyRight
		{
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.Blob)]
			[Column(IsPrimaryKey = true, CanBeNull = false, DataType = DataType.VarBinary, Configuration = ProviderName.ClickHouse)]
			public byte[] RightId { get; set; } = null!;

			public string? RightData { get; set; }

			[Association(ThisKey = nameof(RightId), OtherKey = nameof(ByteKeyLeftRight.RightId))]
			public IEnumerable<ByteKeyLeftRight> LeftRights { get; set; } = null!;

			public IEnumerable<ByteKeyLeft> Lefts => LeftRights.Select(x => x.Left!);

			public override string ToString()
			{
				return $"{nameof(ByteKeyRight)} : [{BitConverter.ToString(RightId)} : {RightData}]";
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3371")]
		public void ConditionalProjectionByNavigationAndByForeignKeyNullChecksMatch([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms      = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<PayRateEmployee>()
				.Association(x => x.PayRate, x => x.PayRateId, x => x!.Id);

			builder.Entity<PayRate>();

			builder.Build();

			var payRateData = new PayRate[]
			{
				new() { Id = 1, Name = "Name1" }, new() { Id = 2, Name = "Name2" }, new() { Id = 3, Name = "test" }
			};

			var employeeData = new PayRateEmployee[]
			{
				new() { Id = 1, PayRateId = 1 }, new() { Id = 2, PayRateId = null },
				new() { Id = 3, PayRateId = 3 },
			};

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));
			using var payRates = db.CreateLocalTable("PayRate", payRateData);
			using var employees = db.CreateLocalTable("PayRateEmployees", employeeData);
			var queryNavProp = employees
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRate == null // nav property
							? null
							: new { x.Id, x.PayRate.Name }
					})
					.Where(item => item.PayRate!.Name.Equals("test"));

			var good = queryNavProp.ToList();

			var queryFK = employees
					.Select(x => new
					{
						x.Id,
						PayRate = x.PayRateId == null // FK property
							? null
							: new
							{
								x.Id,
								x.PayRate!.Name,
							}
					})
					.Where(item => item.PayRate!.Name.Equals("test"));

			var bad = queryFK.ToList(); // System.NullReferenceException

			AreEqual(good, bad);
		}

		private sealed class PayRate
		{
			public PayRate()
			{
				Name = string.Empty;
			}

			public int    Id   { get; set; }
			public string Name { get; set; }
		}

		private sealed class PayRateEmployee
		{
			public int      Id        { get; set; }
			public PayRate? PayRate   { get; set; }
			public int?     PayRateId { get; set; }
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3402")]
		public void AnyOverAssociationInLetColumnOptimization([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<EmployeeScheduleSection>())
			using (db.CreateLocalTable<EmployeeScheduleSectionAdditionalPermission>())
			{
				var fullAccess = false;

				var permissions = from ess in db.GetTable<EmployeeScheduleSection>()
					let allowEdit = fullAccess || ess.AdditionalPermissions.Any(y => y.IsActive)
					where allowEdit
					select new
					{
						SectionID = ess.ID,
					};

				var data1 = permissions.ToList();

				fullAccess = true;

				permissions = from ess in db.GetTable<EmployeeScheduleSection>()
					let allowEdit = fullAccess || ess.AdditionalPermissions.Any(y => y.IsActive)
					where allowEdit
					select new
					{
						SectionID = ess.ID,
					};

				var data2 = permissions.ToList(); 
			}
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3402")]
		public void AnyOverAssociationInLetFilter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<EmployeeScheduleSection>())
			using (db.CreateLocalTable<EmployeeScheduleSectionAdditionalPermission>())
			{
				var query = from ess in db.GetTable<EmployeeScheduleSection>()
					let hasAdditionalPermissions = ess.AdditionalPermissions.Any(y => y.IsActive)
					where hasAdditionalPermissions
					select new
					{
						SectionID = ess.ID,
					};

				var result = query.ToList();
			}
		}

		[Table(Name = "VEMPLOYEE_SCH_SEC")]
		public class EmployeeScheduleSection
		{
			[Column(Name = "ACTIVE", CanBeNull = false)]
			public bool Active { get; set; }

			[Column(Name = "ID", CanBeNull = false, IsPrimaryKey = true)]
			public int ID { get; set; }

			[Column(Name = "NAME", CanBeNull = false)]
			public string Name { get; set; } = null!;

			[Association(ThisKey = "ID", OtherKey = "EmployeeScheduleSectionID")]
			public List<EmployeeScheduleSectionAdditionalPermission> AdditionalPermissions { get; set; } = null!;
		}

		[Table(Name = "VEMPLOYEE_SCHDL_PERM")]
		public class EmployeeScheduleSectionAdditionalPermission
		{
			[Column(Name = "ID", CanBeNull = false, IsPrimaryKey = true)]
			public int EmployeeScheduleSectionID { get; set; }

			[Column(Name = "IS_ACTIVE", CanBeNull = false)]
			public bool IsActive { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4383")]
		public void GenericBaseAssociationLoadWithThenLoad([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			var data1 = new GenericAssociationSewerLine     [] { new() { Id = 1 }, new() { Id = 2 } };
			var data2 = new GenericAssociationSewerLineChain[] { new() { LineId = 1, ChainId = 11 }, new() { LineId = 2, ChainId = 22 } };
			var data3 = new GenericAssociationSewerChain        [] { new() { Id = 11 }, new() { Id = 22 } };
			var data4 = new GenericAssociationSewerChainPoint   [] { new() { ElementId = 11 }, new() { ElementId = 22 } };

			using var t1 = db.CreateLocalTable(data1);
			using var t2 = db.CreateLocalTable(data2);
			using var t3 = db.CreateLocalTable(data3);
			using var t4 = db.CreateLocalTable(data4);

			var items = db.GetTable<GenericAssociationSewerLine>()
				.LoadWith(i => i.PipeLineChains)
				.ThenLoad(i => i.Chain.ChainPoints)
				.OrderBy (i => i.Id)
				.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(items.Select(r => new { r.Id }), Is.EquivalentTo(data1.Select(r => new { r.Id })));
				Assert.That(items[0].PipeLineChains.Select(r => new { r.LineId, r.ChainId }), Is.EquivalentTo(data2.Take(1).Select(r => new { r.LineId, r.ChainId })));
				Assert.That(items[1].PipeLineChains.Select(r => new { r.LineId, r.ChainId }), Is.EquivalentTo(data2.Skip(1).Select(r => new { r.LineId, r.ChainId })));
				Assert.That(items[0].PipeLineChains.Select(c => new { c.Chain.Id }), Is.EquivalentTo(data3.Take(1).Select(r => new { r.Id })));
				Assert.That(items[1].PipeLineChains.Select(c => new { c.Chain.Id }), Is.EquivalentTo(data3.Skip(1).Select(r => new { r.Id })));

				Assert.That(items[0].PipeLineChains.SelectMany(c => c.Chain.ChainPoints!).Select(c => new { c.ElementId }), Is.EquivalentTo(data4.Take(1).Select(r => new { r.ElementId })));
				Assert.That(items[1].PipeLineChains.SelectMany(c => c.Chain.ChainPoints!).Select(c => new { c.ElementId }), Is.EquivalentTo(data4.Skip(1).Select(r => new { r.ElementId })));
			}
		}

		[Table]
		public class GenericAssociationElement
		{
			[Column("ELEMENT_ID"), PrimaryKey] public int Id { get; set; }
		}

		public interface IGenericAssociationChain
		{
			[Column] public int Id { get; set; }
		}

		[Table("CHAINPOINTS")]
		public class GenericAssociationSewerChainPoint
		{
			[Column("CHAIN_ID"), PrimaryKey(0)] public int ElementId { get; set; }
		}

		[Table("CHAINS"), Column("CHAIN_ID", nameof(Id))]
		public class GenericAssociationSewerChain : GenericAssociationElement, IGenericAssociationChain
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(GenericAssociationSewerChainPoint.ElementId), CanBeNull = true)]
			public IEnumerable<GenericAssociationSewerChainPoint>? ChainPoints { get; set; }
		}

		[Table]
		public class GenericAssociationLineChain<TChain> where TChain : IGenericAssociationChain
		{
			[PrimaryKey, Column("LINE_ID")]  public int LineId  { get; set; }
			[PrimaryKey, Column("CHAIN_ID")] public int ChainId { get; set; }

			[Association(ThisKey = nameof(ChainId), OtherKey = nameof(IGenericAssociationChain.Id))]
			public TChain Chain { get; set; } = default!;
		}

		[Table, Column("LINE_ID", nameof(Id))]
		public abstract class GenericAssociationLine<TChain, TPumpLineChain> : GenericAssociationElement
			where TChain : IGenericAssociationChain
			where TPumpLineChain : GenericAssociationLineChain<TChain>
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(GenericAssociationLineChain<>.LineId), CanBeNull = false)]
			public IEnumerable<TPumpLineChain> PipeLineChains { get; set; } = null!;
		}

		[Table("PUMPLINE_CHAINS")]
		public class GenericAssociationSewerLineChain : GenericAssociationLineChain<GenericAssociationSewerChain>;

		[Table("PUMPLINES")]
		public class GenericAssociationSewerLine : GenericAssociationLine<GenericAssociationSewerChain, GenericAssociationSewerLineChain>;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/496")]
		public void SelectManyAssociationNullableForeignKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var children = db.GetTable<NullableFkParent>()
					.Where(p => p.ParentID == 1)
					.SelectMany(c => c.Children)
					.ToList();

			Assert.That(children, Is.Not.Empty);

			var expected = Child.Where(c => c.ParentID == 1);
			var result = children.Select(c => new Model.Child { ChildID = c.ChildID, ParentID = c.ParentID!.Value });

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/496")]
		public void SelectAssociationCollectionNullableForeignKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var children = db.GetTable<NullableFkParent>()
					.Select(p => new {p.Children})
					.ToList();

			Assert.That(children, Is.Not.Empty);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/496")]
		public void SelectManyAssociationLongForeignKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var children = db.GetTable<LongFkParent>()
					.Where(p => p.ParentID == 1)
					.SelectMany(p => p.Children)
					.ToList();

			Assert.That(children, Is.Not.Empty);

			var expected = Child.Where(p => p.ParentID == 1);
			var result = children.Select(c => new Model.Child() { ChildID = c.ChildID, ParentID = (int)c.ParentID });

			AreEqual(expected, result);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/496")]
		public void SelectAssociationCollectionLongForeignKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var children = db.GetTable<LongFkParent>()
					.Select(p => new {p.Children})
					.ToList();

			Assert.That(children, Is.Not.Empty);
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		sealed class NullableFkParent
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<NullableFkChild> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class NullableFkChild
		{
			public int  ChildID;
			public int? ParentID;
		}

		[Table("Parent", IsColumnAttributeRequired = false)]
		sealed class LongFkParent
		{
			public int ParentID;
			[Association(ThisKey = "ParentID", OtherKey = "ParentID", CanBeNull = true)]
			public ICollection<LongFkChild> Children = null!;
		}

		[Table("Child", IsColumnAttributeRequired = false)]
		sealed class LongFkChild
		{
			public int  ChildID;
			public long ParentID;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5049")]
		public void LoadWithQueryExpressionMethodAssociationAggregate([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var personsData = new[] { new PersonWithAssociation { Id = 1, Name = "Bob" } };
			var personGrateData = new[]
			{
				new PersonGrade { Id = 1, PersonId = 1, Subject = "English", Grade = 8 },
				new PersonGrade { Id = 2, PersonId = 1, Subject = "Math", Grade = 5 },
				new PersonGrade { Id = 3, PersonId = 1, Subject = "Geography", Grade = 9 }
			};

			using var persons     = db.CreateLocalTable(personsData);
			using var personGrate = db.CreateLocalTable(personGrateData);

			var query = db.GetTable<PersonWithAssociation>().LoadWith(p => p.GradeStats)
				.Where(p => p.GradeStats!.AverageGrade > 5)
				.ToArray();

		}

		[Table(Name = "PersonWithAssociation")]
		public sealed class PersonWithAssociation
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }

			[Association(QueryExpressionMethod = nameof(GetGradeStats))]
			public GradeStatsCls? GradeStats { get; set; }

			public class GradeStatsCls
			{
				public int    PersonId     { get; set; }
				public double AverageGrade { get; set; }
			}

			public static Expression<Func<PersonWithAssociation, IDataContext, IQueryable<GradeStatsCls?>>> GetGradeStats()
			{
				return (person, db) => db.GetTable<PersonGrade>()
					.Where(x => x.PersonId == person.Id)
					.GroupBy(x => x.PersonId)
					.Select(g => new GradeStatsCls { PersonId = g.Key, AverageGrade = g.Average(x => (double)x.Grade), })
					.DefaultIfEmpty();
			}
		}

		[Table(Name = "PersonGrades")]
		public sealed class PersonGrade
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int PersonId { get; set; }

			[Column]
			public string? Subject { get; set; }

			[Column]
			public int Grade { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5052")]
		public void ProjectQueryExpressionAssociationCollection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var personData = new List<LocAssocPersonDto>
			{
				new() { Id = 1, Name = "John Doe" },
			};

			var locationData = new List<LocAssocLocationDto>
			{
				new() { Id = 1, Description = "Location 1" },
			};

			using var db = GetDataContext(context);

			using var personTable = db.CreateLocalTable(personData);
			using var locationTable = db.CreateLocalTable(locationData);
			using var linkTable = db.CreateLocalTable<LocAssocPersonLocationLink>();

			var query = db.GetTable<LocAssocPersonDto>()
				.Where(p => p.Id == 1)
				.Select(dtoPersonDto => new LocAssocPersonEntity
					{
						Id   = dtoPersonDto.Id,
						Name = dtoPersonDto.Name,
						Locations = dtoPersonDto.Locations
							.Select(dtoLocationMapping => new LocAssocLocation
								{
									LinkId = dtoLocationMapping!.TheLink!.Id, Id = dtoLocationMapping.Location.Id, Description = dtoLocationMapping.Location.Description
								}
							)
							.ToList()
					}
				);

			query.ToList();
		}

		public sealed class LocAssocPersonEntity
		{
			public          int            Id        { get; set; }
			public required string         Name      { get; set; }
			public          List<LocAssocLocation> Locations { get; set; } = new();
		}

		[Table("test_person")]
		public sealed class LocAssocPersonDto
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public required string Name { get; set; }

			[Association(QueryExpressionMethod = "GetLocationMappings")]
			public List<LocAssocLocationMapping> Locations { get; set; } = new();

			public static Expression<Func<LocAssocPersonDto, IDataContext, IQueryable<LocAssocLocationMapping>>> GetLocationMappings =>
				(p, db) => from d in db.GetTable<LocAssocLocationDto>()
					from l in db.GetTable<LocAssocPersonLocationLink>().LeftJoin(j => j.Id == 5) // does not exist
					where d.Id == 1
					select new LocAssocLocationMapping { TheLink = l, Location = d };
		}

		[Table("test_link_person_location")]
		public sealed class LocAssocPersonLocationLink
		{
			[PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public int PersonId { get; set; }

			[Column]
			public int LocationId { get; set; }
		}

		[Table("test_location")]
		public sealed class LocAssocLocationDto
		{
			[Column("id"), PrimaryKey, Identity]
			public int Id { get; set; }
			[Column]
			public required string Description { get; set; }
		}

		public sealed class LocAssocLocation
		{
			public int Id { get; set; }
			public required string Description { get; set; }
			public int LinkId { get; set; }
		}

		public sealed class LocAssocLocationMapping
		{
			public LocAssocPersonLocationLink? TheLink  { get; set; }
			public LocAssocLocationDto         Location { get; set; } = default!;
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/825")]
		public void AnyFilterThenSelectManyThenParentAssociation([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var userId  = 32;
			var childId = 32;

			var query = db.GetTable<ParentWithPermissions>()
					.Where(p => p.ParentPermissions.Any(permission => permission.UserId == userId))
					.SelectMany(parent => parent.Childs)
					.Where(child => child.Id == childId)
					.Select(child => child.Parent);

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result[0].Id, Is.EqualTo(3));
		}

		[Table(Name = "Child")]
		public class ChildWithRequiredParent
		{
			[PrimaryKey, Identity, Column("ChildID")]
			public int Id { get; set; }

			[Column("ParentID"), NotNull]
			public int ParentId { get; set; }

			[Association(ThisKey = "ParentId", OtherKey = "Id", CanBeNull = false)]
			public ParentWithPermissions Parent { get; set; } = null!;
		}

		[Table(Name = "Parent")]
		public class ParentWithPermissions
		{
			[PrimaryKey, Identity, Column("ParentID")]
			public int Id { get; set; }

			[Association(ThisKey = "Id", OtherKey = "ParentId", CanBeNull = true)]
			public IList<ParentPermissionRow> ParentPermissions { get; set; } = null!;

			[Association(ThisKey = "Id", OtherKey = "ParentId", CanBeNull = true)]
			public IList<ChildWithRequiredParent> Childs { get; set; } = null!;
		}

		[Table(Name = "GrandChild")]
		public class ParentPermissionRow
		{
			[PrimaryKey, Identity, Column("GrandChildID")]
			public int Id { get; set; }

			[Column("ParentID"), NotNull]
			public int ParentId { get; set; }

			[Column("ChildID"), NotNull]
			public int UserId { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/975")]
		public void ExpressionPredicateAssociationAfterDistinctJoin([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using (db.CreateLocalTable<WorkTask>())
			using (db.CreateLocalTable<WorkTaskStage>())
			using (db.CreateLocalTable<WorkAssignment>())
			{
				var directionId = TestData.Guid1;
				var taskId = db.GetTable<WorkTask>().InsertWithInt32Identity(() => new WorkTask
				{
					DirectionId = directionId,
					TargetId = 1,
					TargetName = "TN",
					Text = "SomeText",
					DateBegin = Sql.CurrentTimestamp
				});

				db.GetTable<WorkAssignment>().Insert(() => new WorkAssignment
				{
					DirectionId = directionId,
					TargetId = 1,
					TargetName = "TN",
					EmployeeId = 10,
					DateAssign = Sql.CurrentTimestamp
				});

				db.GetTable<WorkTaskStage>().Insert(() => new WorkTaskStage
				{
					Actual = true,
					TaskId = taskId,
					StageId = 800,
					SwitchDate = Sql.CurrentTimestamp,
				});

				var employeeId = 10;
				var query = (from t in db.GetTable<WorkTask>()
							 join a in db.GetTable<WorkAssignment>()
								on new {t.DirectionId, t.TargetId, t.TargetName}
								equals new {a.DirectionId, a.TargetId, a.TargetName}
							 where a.EmployeeId == employeeId
								  && (a.DateRevoke == null || a.DateRevoke > SqlServerClock.GetDate())
							 select t)
						.Distinct()
#pragma warning disable CS0472 // comparison of non-null int? with null
						.Where(it => it.ActualStage.Any(d => d.StageId < 9000 || ((int?)d.StageId) == null));
#pragma warning restore CS0472

				var zz = query.ToArray();
			}
		}

		public static class SqlServerClock
		{
			[Sql.Function(ServerSideOnly = true, CanBeNull = false)]
			public static DateTime GetDate() { throw new ServerSideOnlyException(nameof(GetDate)); }
		}

		public interface IWorkTargetAware
		{
			string TargetName { get; }
			int    TargetId   { get; }
		}

		[Table(Name="WorkTaskStages")]
		public partial class WorkTaskStage
		{
			[PrimaryKey, Identity   ] public int      Id           { get; set; } // Int
			[Column,     NotNull    ] public int      TaskId       { get; set; } // Int
			[Column,     NotNull    ] public int      StageId      { get; set; } // Int
			[Column,     NotNull    ] public DateTime SwitchDate   { get; set; } // DateTime
			[Column,     NotNull    ] public bool     Actual       { get; set; } // Bit
			[Column,        Nullable] public int?     WorkReportId { get; set; } // Int
			[Column,        Nullable] public int?     UserId       { get; set; } // Int
		}

		[Table(Name="WorkTasks")]
		public partial class WorkTask : IWorkTargetAware
		{
			[PrimaryKey, Identity   ] public int       Id          { get; set; } // Int
			[Column,     NotNull    ] public DateTime  DateBegin   { get; set; } // DateTime
			[Column,        Nullable] public DateTime? DateEnd     { get; set; } // DateTime
			[Column,     NotNull    ] public Guid      DirectionId { get; set; } // UniqueIdentifier
			[Column,     NotNull    ] public string    Text        { get; set; } = null!; // varchar(1000)
			[Column,     NotNull    ] public string    TargetName  { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull    ] public int       TargetId    { get; set; } // Int
			[Column,        Nullable] public int?      ParentId    { get; set; } // Int

			[Association(ThisKey =nameof(Id), OtherKey =nameof(WorkTaskStage.TaskId), ExpressionPredicate = nameof(ActualTaskExp), CanBeNull =true)]
			public IEnumerable<WorkTaskStage> ActualStage { get; set; } = null!;

			private static Expression<Func<WorkTask, WorkTaskStage, bool>> ActualTaskExp() => (t, ts) => ts.Actual == true;
		  }

		[Table(Name="WorkAssignments")]
		public partial class WorkAssignment : IWorkTargetAware
		{
			[PrimaryKey, Identity   ] public int       Id          { get; set; } // Int
			[Column,     NotNull    ] public int       EmployeeId  { get; set; } // Int
			[Column,     NotNull    ] public DateTime  DateAssign  { get; set; } // DateTime
			[Column,        Nullable] public DateTime? DateRevoke  { get; set; } // DateTime
			[Column,     NotNull    ] public Guid      DirectionId { get; set; } // UniqueIdentifier
			[Column,     NotNull    ] public string    TargetName  { get; set; } = null!; // nvarchar(128)
			[Column,     NotNull    ] public int       TargetId    { get; set; } // Int
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/38")]
		[ThrowsRequiresCorrelatedSubquery]
		public void CountOfAssociationInProjectionHasNoInnerJoin([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from a in Child
				select new { Count = a.GrandChildren.Count() },
				from a in db.Child
				select new { Count = a.GrandChildren1.Count() });

			var sql = ((TestDataConnection)db).LastQuery;

			Assert.That(sql, Is.Not.Contains("INNER JOIN"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/498")]
		public void GroupByOverAssociationKeepsInnerJoin([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeJoins(false));
			var q = from x in db.Child
					//join y in db.GrandChild on new { x.ParentID, x.ChildID } equals new { ParentID = (int)y.ParentID, ChildID = (int)y.ChildID }
					from y in x.GrandChildren1
					select x.ParentID;

			var r = from x in q
					group x by x
					into g
					select new { g.Key, Cghildren = g.Count() };

			var qq = from x in Child
					 from y in x.GrandChildren
					 select x.ParentID;

			var rr = from x in qq
					 group x by x
					into g
					 select new { g.Key, Cghildren = g.Count() };

			AreEqual(rr, r);

			var sql = r.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("INNER"));
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
			throw new ServerSideOnlyException(nameof(ChildPredicateMethod));
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
			throw new ServerSideOnlyException(nameof(Children));
		}

		private static Expression<Func<AssociationTests.ComplexParent, IQueryable<AssociationTests.ComplexChild>>> ChildrenExpression()
		{
			return p => p.ManyToMany.Select(m2m => m2m.Child);
		}
	}
}
