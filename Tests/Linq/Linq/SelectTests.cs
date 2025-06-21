using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectTests : TestBase
	{
		[Test]
		public void SimpleDirect([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person);
		}

		[Test]
		public void Simple([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(from p in db.Person select p);
		}

		[Test]
		public void Complex([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(from p in db.ComplexPerson select p);
		}

		[Test]
		public void SimpleDouble([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person.Select(p => p).Select(p => p));
		}

		[Test]
		public void New([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var expected = from p in    Person select new { p.ID, p.FirstName };
				var result   = from p in db.Person select new { p.ID, p.FirstName };
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		void NewParam(IQueryable<Person> table, int i)
		{
			var expected = from p in Person select new { i, p.ID, p.FirstName };
			var result   = from p in table  select new { i, p.ID, p.FirstName };

			Assert.That(result.ToList().SequenceEqual(expected), Is.True);
		}

		[Test]
		public void NewParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				for (var i = 0; i < 5; i++) NewParam(db.Person, i);
			}
		}

		[Test]
		public void InitObject([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(from p in db.Person select new Person { ID = p.ID, FirstName = p.FirstName });
		}

		[Test]
		public void NewObject([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(from p in db.Person select new Person(p.ID, p.FirstName));
		}

		[Test]
		public void NewInitObject([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(from p in db.Person select new Person(p.ID) { FirstName = p.FirstName });
		}

		[Test]
		public void NewWithExpr([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestPerson(1, "John1", from p in db.Person select new Person(p.ID) { FirstName = (p.FirstName + "1\r\r\r").TrimEnd('\r') });
		}

		[Test]
		public void MultipleSelect1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p => new { PersonID = p.ID, Name = p.FirstName })
					.Select(p => new Person(p.PersonID) { FirstName = p.Name }));
		}

		[Test]
		public void MultipleSelect2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(
					from p in db.Person
					select new { PersonID = p.ID, Name = p.FirstName } into pp
					select new Person(pp.PersonID) { FirstName = pp.Name });
		}

		[Test]
		public void MultipleSelect3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p => new        { PersonID = p.ID,       Name      = p.FirstName })
					.Select(p => new Person { ID       = p.PersonID, FirstName = p.Name      })
					.Select(p => new        { PersonID = p.ID,       Name      = p.FirstName })
					.Select(p => new Person { ID       = p.PersonID, FirstName = p.Name      }));
		}

		[Test]
		public void MultipleSelect4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { p1 })
					.Select(p2 => new        { p2 })
					.Select(p3 => new Person { ID = p3.p2.p1.ID, FirstName = p3.p2.p1.FirstName }));
		}

		[Test]
		public void MultipleSelect5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { p1 })
					.Select(p2 => new Person { ID = p2.p1.ID, FirstName = p2.p1.FirstName })
					.Select(p3 => new        { p3 })
					.Select(p4 => new Person { ID = p4.p3.ID, FirstName = p4.p3.FirstName }));
		}

		[Test]
		public void MultipleSelect6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { p1 })
					.Select(p2 => new Person { ID = p2.p1.ID, FirstName = p2.p1.FirstName })
					.Select(p3 => p3)
					.Select(p4 => new Person { ID = p4.ID,    FirstName = p4.FirstName }));
		}

		[Test]
		public void MultipleSelect7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { ID = p1.ID + 1, p1.FirstName })
					.Select(p2 => new Person { ID = p2.ID - 1, FirstName = p2.FirstName }));
		}

		[Test]
		public void MultipleSelect8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var person = (

					db.Person
						.Select(p1 => new Person { ID = p1.ID * 2,           FirstName = p1.FirstName })
						.Select(p2 => new        { ID = p2.ID / "22".Length, p2.FirstName })

				).ToList().First(p => p.ID == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(person.ID, Is.EqualTo(1));
					Assert.That(person.FirstName, Is.EqualTo("John"));
				}
			}
		}

		[Test]
		public void MultipleSelect9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { ID = p1.ID - 1, p1.FirstName })
					.Select(p2 => new Person { ID = p2.ID + 1, FirstName = p2.FirstName })
					.Select(p3 => p3)
					.Select(p4 => new        { ID = p4.ID * "22".Length, p4.FirstName })
					.Select(p5 => new Person { ID = p5.ID / 2, FirstName = p5.FirstName }));
		}

		[Test]
		public void MultipleSelect10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				TestJohn(db.Person
					.Select(p1 => new        { p1.ID, p1 })
					.Select(p2 => new        { p2.ID, p2.p1, p2 })
					.Select(p3 => new        { p3.ID, p3.p1.FirstName, p11 = p3.p2.p1, p3 })
					.Select(p4 => new Person { ID = p4.p11.ID, FirstName = p4.p3.p1.FirstName }));
		}

		// ProviderName.SqlServer2014 disabled due to:
		// https://connect.microsoft.com/SQLServer/feedback/details/3139577/performace-regression-for-compatibility-level-2014-for-specific-query
		[Test]
		public void MultipleSelect11([IncludeDataSources(
			TestProvName.AllSqlServer2008, TestProvName.AllSqlServer2012, TestProvName.AllSqlServer2019, TestProvName.AllSapHana, TestProvName.AllClickHouse)]
			string context)
		{
			var dt = DateTime.Now;

			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent
					from  g1 in p.GrandChildren.DefaultIfEmpty()
					let   c1 = g1.Child!.ChildID
					where c1 == 1
					from  g2 in p.GrandChildren.DefaultIfEmpty()
					let   c2 = g2.Child!.ChildID
					where c2 == 2
					from  g3 in p.GrandChildren.DefaultIfEmpty()
					let   c3 = g3.Child!.ChildID
					where c3 == 3
					from  g4 in p.GrandChildren.DefaultIfEmpty()
					let   c4 = g4.Child!.ChildID
					where c4 == 4
					from  g5 in p.GrandChildren.DefaultIfEmpty()
					let   c5 = g5.Child!.ChildID
					where c5 == 5
					from  g6 in p.GrandChildren.DefaultIfEmpty()
					let   c6 = g6.Child!.ChildID
					where c6 == 6
					from  g7 in p.GrandChildren.DefaultIfEmpty()
					let   c7 = g7.Child!.ChildID
					where c7 == 7
					from  g8 in p.GrandChildren.DefaultIfEmpty()
					let   c8 = g8.Child!.ChildID
					where c8 == 8
					from  g9 in p.GrandChildren.DefaultIfEmpty()
					let   c9 = g9.Child!.ChildID
					where c9 == 9
					from  g10 in p.GrandChildren.DefaultIfEmpty()
					let   c10 = g10.Child!.ChildID
					where c10 == 10
					from  g11 in p.GrandChildren.DefaultIfEmpty()
					let   c11 = g11.Child!.ChildID
					where c11 == 11
					from  g12 in p.GrandChildren.DefaultIfEmpty()
					let   c12 = g12.Child!.ChildID
					where c12 == 12
					from  g13 in p.GrandChildren.DefaultIfEmpty()
					let   c13 = g13.Child!.ChildID
					where c13 == 13
					from  g14 in p.GrandChildren.DefaultIfEmpty()
					let   c14 = g14.Child!.ChildID
					where c14 == 14
					from  g15 in p.GrandChildren.DefaultIfEmpty()
					let   c15 = g15.Child!.ChildID
					where c15 == 15
					from  g16 in p.GrandChildren.DefaultIfEmpty()
					let   c16 = g16.Child!.ChildID
					where c16 == 16
					from  g17 in p.GrandChildren.DefaultIfEmpty()
					let   c17 = g17.Child!.ChildID
					where c17 == 17
					from  g18 in p.GrandChildren.DefaultIfEmpty()
					let   c18 = g18.Child!.ChildID
					where c18 == 18
					from  g19 in p.GrandChildren.DefaultIfEmpty()
					let   c19 = g19.Child!.ChildID
					where c19 == 19
					from  g20 in p.GrandChildren.DefaultIfEmpty()
					let   c20 = g20.Child!.ChildID
					where c20 == 20
					orderby c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16, c17, c18, c19, c20
					select new
					{
						p,
						cs = new [] { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16, c17, c18, c19, c20 }
					};

				var _=  q.ToList();
			}

			Assert.That((DateTime.Now - dt).TotalSeconds, Is.LessThan(30));
		}

		[Test]
		public void MutiplySelect12([DataSources(false, TestProvName.AllAccess, TestProvName.AllDB2)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from grandChild in db.GrandChild
					from child in db.Child
					where grandChild.ChildID.HasValue
					select grandChild;
				q.ToList();

				var selectCount = ((DataConnection)db).LastQuery!
					.Split(' ', '\t', '\n', '\r')
					.Count(s => s.Equals("select", StringComparison.OrdinalIgnoreCase));

				Assert.That(selectCount, Is.EqualTo(1), "Why do we need \"select from select\"??");
			}
		}

		[Test]
		public void Coalesce([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (

					from p in db.Person
					where p.ID == 1
					select new
					{
						p.ID,
						FirstName  = p.FirstName  ?? "None",
						MiddleName = p.MiddleName ?? "None"
					}

				).ToList().First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(q.ID, Is.EqualTo(1));
					Assert.That(q.FirstName, Is.EqualTo("John"));
					Assert.That(q.MiddleName, Is.EqualTo("None"));
				}
			}
		}

		[Test]
		public void Coalesce2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (

					from p in db.Person
					where p.ID == 1
					select new
					{
						p.ID,
						FirstName  = p.MiddleName ?? p.FirstName  ?? "None",
						LastName   = p.LastName   ?? p.FirstName  ?? "None",
						MiddleName = p.MiddleName ?? p.MiddleName ?? "None"
					}

				).ToList().First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(q.ID, Is.EqualTo(1));
					Assert.That(q.FirstName, Is.EqualTo("John"));
					Assert.That(q.LastName, Is.EqualTo("Pupkin"));
					Assert.That(q.MiddleName, Is.EqualTo("None"));
				}
			}
		}

		sealed class MyMapSchema : MappingSchema
		{
			public MyMapSchema()
			{
				SetDefaultValue(typeof(string), null);
			}
		}

		static readonly MyMapSchema _myMapSchema = new ();

		[Test]
		public void Coalesce3([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.AddMappingSchema(_myMapSchema);

				var q = (

					from p in db.Person
					where p.ID == 1
					select new
					{
						p.ID,
						FirstName  = p.MiddleName ?? p.FirstName  ?? "None",
						LastName   = p.LastName   ?? p.FirstName  ?? "None",
						MiddleName = p.MiddleName ?? p.MiddleName ?? "None"
					}

				).ToList().First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(q.ID, Is.EqualTo(1));
					Assert.That(q.FirstName, Is.EqualTo("John"));
					Assert.That(q.LastName, Is.EqualTo("Pupkin"));
					Assert.That(q.MiddleName, Is.EqualTo("None"));
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void Coalesce4([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child
					select Sql.AsSql((from ch in    Child where ch.ChildID == c.ChildID select ch.Parent!.Value1).FirstOrDefault() ?? c.ChildID),
					from c in db.Child
					select Sql.AsSql((from ch in db.Child where ch.ChildID == c.ChildID select ch.Parent!.Value1).FirstOrDefault() ?? c.ChildID));
		}

		[Test]
		public void Coalesce5([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1),
					from p in db.Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1));
		}

		class CoalesceNullableFields
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column] public int? Nullable1 { get; set; }
			[Column] public int? Nullable2 { get; set; }
			[Column] public int? Nullable3 { get; set; }

			public static CoalesceNullableFields[] Seed()
			{
				return
				[
					new CoalesceNullableFields { Id = 1, Nullable1 = 10,   Nullable2 = null, Nullable3 = null },
					new CoalesceNullableFields { Id = 2, Nullable1 = null, Nullable2 = 20,   Nullable3 = null },
					new CoalesceNullableFields { Id = 3, Nullable1 = null, Nullable2 = null, Nullable3 = 30   },
					new CoalesceNullableFields { Id = 4, Nullable1 = null, Nullable2 = null, Nullable3 = null  }
				];
			}
		}

		[Test]
		public void CoalesceMany([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(CoalesceNullableFields.Seed());

			var query = table.Select(t => new
			{
				Value1 = t.Nullable1 ?? t.Nullable2 ?? t.Nullable3 ?? t.Id,
				Value2 = t.Nullable2 ?? t.Nullable1 ?? t.Nullable3 ?? t.Id,
				Value3 = t.Nullable2 ?? t.Nullable3 ?? t.Nullable1 ?? t.Id,
				Value4 = t.Nullable3 ?? t.Nullable1 ?? t.Nullable2 ?? t.Id,
				Value5 = t.Nullable3 ?? t.Nullable2 ?? t.Nullable1 ?? t.Id,

				OptimalValue1 = Sql.AsSql((int?)t.Id  ?? t.Nullable1 ?? t.Nullable2 ?? t.Nullable3),
				OptimalValue2 = Sql.AsSql(t.Nullable1 ?? (int?)t.Id  ?? t.Nullable2 ?? t.Nullable3),
				OptimalValue3 = Sql.AsSql(t.Nullable1 ?? t.Nullable2 ?? (int?)t.Id  ?? t.Nullable3),
			});

			AssertQuery(query);
		}

		[Test]
		public void Concatenation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName  = "123" + p.FirstName + "456" };
				var f = q.Where(p => p.FirstName == "123John456").ToList().First();
				Assert.That(f.ID, Is.EqualTo(1));
			}
		}

		IEnumerable<int> GetList(int i)
		{
			yield return i;
		}

		[Test]
		public void SelectEnumerable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Max = GetList(p.ParentID).Max() },
					from p in db.Parent select new { Max = GetList(p.ParentID).Max() });
		}

		public class ListViewItem
		{
			public ListViewItem(string[] items)
			{
			}

			public bool    Checked    { get; set; }
			public int     ImageIndex { get; set; }
			public object? Tag        { get; set; }
		}
		[Test]
		public void ConstractClass([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Parent.Select(f =>
				new ListViewItem(new[] { "", f.ParentID.ToString()!, f.Value1.ToString()! })
				{
					Checked    = true,
					ImageIndex = 0,
					Tag        = f.ParentID
				}).ToList();

			var expected = Parent.Select(f =>
				new ListViewItem(new[] { "", f.ParentID.ToString()!, f.Value1.ToString()! })
				{
					Checked    = true,
					ImageIndex = 0,
					Tag        = f.ParentID
				}).ToList();

			AreEqual(expected, query, ComparerBuilder.GetEqualityComparer(expected));
		}

		static string ConvertString(string s, int? i, bool b, int n)
		{
			return s + "." + i + "." + b + "." + n;
		}

		[Test]
		public void Index([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					db.Child
						.OrderByDescending(m => m.ChildID)
						.Where(m => m.Parent != null && m.ParentID > 0);

				var lines =
					q.Select(
						(m, i) =>
							ConvertString(m.Parent!.ParentID.ToString(), m.ChildID, i % 2 == 0, i)).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(lines[0], Is.EqualTo("7.77.True.0"));
					Assert.That(lines[1], Is.EqualTo("6.66.False.1"));
					Assert.That(lines[2], Is.EqualTo("6.65.True.2"));
				}

				q =
					db.Child
						.OrderByDescending(m => m.ChildID)
						.Where(m => m.Parent != null && m.ParentID > 0);

				lines =
					q.Select(
						(m, i) =>
							ConvertString(m.Parent!.ParentID.ToString(), m.ChildID, i % 2 == 0, i)).ToArray();

				Assert.That(lines[0], Is.EqualTo("7.77.True.0"));
			}
		}

		[Test]
		public void InterfaceTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Parent2 select new { p.ParentID, p.Value1 };
				q.ToList();
			}
		}

		[Test]
		public void ProjectionTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child select new { c.ChildID, ID = 0, ID1 = c.ParentID2!.ParentID2, c.ParentID2.Value1, ID2 = c.ParentID },
					from c in db.Child select new { c.ChildID, ID = 0, ID1 = c.ParentID2!.ParentID2, c.ParentID2.Value1, ID2 = c.ParentID });
		}

		[Table(Name="Person")]
		[ObjectFactory(typeof(Factory))]
		public class TestPersonObject
		{
			public class Factory : IObjectFactory
			{
				#region IObjectFactory Members

				public object CreateInstance(TypeAccessor typeAccessor)
				{
					return typeAccessor.CreateInstance();
				}

				#endregion
			}

			public int     PersonID;
			public string? FirstName = null!;
		}

		[Test]
		public void ObjectFactoryTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestPersonObject>().ToList();
		}

		[Test]
		public void ProjectionTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select p.Patient,
					from p in db.Person select p.Patient);
		}

		[Test]
		public void EqualTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = (from p in db.Parent select new { p1 = p, p2 = p }).First();
				Assert.That(q.p2, Is.SameAs(q.p1));
			}
		}

		[Test]
		public void SelectEnumOnClient([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var arr = new List<Person> { new Person() };
				var p = db.Person.Select(person => new { person.ID, Arr = arr.Take(1) }).FirstOrDefault()!;

				p.Arr.Single();
			}
		}

		[Table(Name="Parent")]
		public class TestParent
		{
			[Column("ParentID")] public int  ParentID_;
			[Column("Value1")]   public int? Value1_;
		}

		[Test]
		public void SelectField([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q =
					from p in db.GetTable<TestParent>()
					select p.Value1_;

			q.ToArray();

			var sql = q.ToSqlQuery().Sql;

			Assert.That(sql, Does.Not.Contain("ParentID_"));
		}

		[Test]
		public void SelectComplexField([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<ComplexPerson>()
					select p.Name.LastName;

				q.ToArray();

				var sql = q.ToSqlQuery().Sql;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(sql, Does.Not.Contain("First"));
					Assert.That(sql, Does.Contain("LastName"));
				}
			}
		}

		[Test]
		public void SelectComplex1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var r = db.GetTable<ComplexPerson>().First(_ => _.ID == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.Name.FirstName, Is.EqualTo("John"));
					Assert.That(r.Name.MiddleName, Is.Null);
					Assert.That(r.Name.LastName, Is.EqualTo("Pupkin"));
				}
			}
		}

		[Test]
		public void SelectComplex2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var r = db.GetTable<ComplexPerson2>().First(_ => _.ID == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.Name.FirstName, Is.EqualTo("John"));
					Assert.That(r.Name.MiddleName, Is.Null);
					Assert.That(r.Name.LastName, Is.EqualTo("Pupkin"));
				}
			}
		}

		[Test]
		public void SelectComplex3([DataSources] string context)
		{
			var ms = new MappingSchema();
			var b  = new FluentMappingBuilder(ms);

			b
				.Entity<ComplexPerson3>()        .HasTableName ("Person")
				.Property(_ => _.ID)             .HasColumnName("PersonID")
				.Property(_ => _.Name.FirstName) .HasColumnName("FirstName")
				.Property(_ => _.Name.LastName)  .HasColumnName("LastName")
				.Property(_ => _.Name.MiddleName).HasColumnName("MiddleName")
				.Build();

			using (var db = GetDataContext(context, ms))
			{
				var r = db.GetTable<ComplexPerson3>().First(_ => _.ID == 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(r.Name.FirstName, Is.EqualTo("John"));
					Assert.That(r.Name.MiddleName, Is.Null);
					Assert.That(r.Name.LastName, Is.EqualTo("Pupkin"));
				}
			}
		}

		[Test]
		public void SelectNullableTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var e = new LinqDataTypes2() { ID = 1000, BoolValue = false };
					db.Insert(e);

					var e2 = db.Types2.First(_ => _.ID == 1000);

					Assert.That(e2, Is.EqualTo(e));
				}
				finally
				{
					db.Types2.Where(_ => _.ID == 1000).Delete();
				}
			}
		}

		[Test]
		public void SelectNullableTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var en = new LinqDataTypes2() { ID = 1000, BoolValue = false };
					db.Insert(en);

					DateTime defaultDate = default;
					if (db.MappingSchema.GetDefaultValue(typeof(DateTime)) is DateTime dateTime)
						defaultDate = dateTime;

					var e = new LinqDataTypes() { ID = 1000, BoolValue = false, DateTimeValue = defaultDate };

					var e2 = db.Types.First(_ => _.ID == 1000);

					Assert.That(e2, Is.EqualTo(e));
				}
				finally
				{
					db.Types2.Where(_ => _.ID == 1000).Delete();
				}
			}
		}

		[Test]
		public void SelectNullPropagationTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from p in db.Parent
					select new
					{
						Info = p != null ? new { p.ParentID, p.Value1 } : null
					};

				var query2 = from q in query1
					select new
					{
						q.Info.ParentID
					};

				var _ = query2.ToArray();
			}
		}

		[Test]
		public void SelectNullPropagationWhereTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from p in db.Parent
					from c in db.Child.LoadWith(c => c.Parent).Where(c => c.ParentID == p.ParentID)
					select new
					{
						Info1 = p != null ? new { p.ParentID, p.Value1 } : null,
						Info2 = c != null ? (c.Parent != null ? new { c.Parent.Value1 } : null) : null
					};

				var query2 = from q in query1
					select new
					{
						InfoAll = q == null
							? null
							: new
							{
								ParentID = q.Info1 != null ? q.Info1.ParentID : (int?)null,
								q.Info1!.Value1,
								Value2 = q.Info2.Value1
							}
					};

				var query3 = query2.Where(p => p.InfoAll.ParentID!.Value > 0 || p.InfoAll.Value1 > 0  || p.InfoAll.Value2 > 0);

				query3 = query3
					.OrderBy(q => q.InfoAll.ParentID)
					.ThenBy(q => q.InfoAll.Value1)
					.ThenBy(q => q.InfoAll.Value2);

				AssertQuery(query3);
			}
		}

		[Test]
		public void SelectNullPropagationTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					join c in Child on p.Value1 equals c.ParentID into gr
					from c in gr.DefaultIfEmpty()
					select new
					{
						Info2 = c != null ? (c.Parent != null ? new { c.Parent.Value1 } : null) : null
					}
					,
					from p in db.Parent
					join c in db.Child on p.Value1 equals c.ParentID into gr
					from c in gr.DefaultIfEmpty()
					select new
					{
						Info2 = c != null ? (c.Parent != null ? new { c.Parent.Value1 } : null) : null
					});
			}
		}

		[Test]
		public void SelectNullProjectionTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var actual = from p in db.Parent
					select new
					{
						V1 = p.Value1.HasValue ? p.Value1 : null,
					};

				var expected = from p in Parent
					select new
					{
						V1 = p.Value1.HasValue ? p.Value1 : null,
					};

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void SelectReverseNullPropagationTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from p in db.Parent
							 select new
							 {
								 Info = null != p ? new { p.ParentID, p.Value1 } : null
							 };

				var query2 = from q in query1
							 select new
							 {
								 q.Info.ParentID
							 };

				var _ = query2.ToArray();
			}
		}

		[Test]
		public void SelectReverseNullPropagationWhereTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from p in db.Parent
							 from c in db.Child.InnerJoin(c => c.ParentID == p.ParentID)
							 select new
							 {
								 Info1 = null != p ? new { p.ParentID, p.Value1 } : null,
								 Info2 = null != c ? (null != c.Parent ? new { c.Parent.Value1 } : null) : null
							 };

				var query2 = from q in query1
							 select new
							 {
								 InfoAll = null == q
									 ? null
									 : new
									 {
										 ParentID = null != q.Info1 ? (int?)q.Info1.ParentID : (int?)null,
										 q.Info1!.Value1,
										 Value2 = q.Info2.Value1
									 }
							 };

				var query3 = query2.Where(p => p.InfoAll.ParentID!.Value > 0 || p.InfoAll.Value1 > 0 || p.InfoAll.Value2 > 0);

				var _ = query3.ToArray();
			}
		}

		[Test]
		public void SelectReverseNullPropagationTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					join c in Child on p.Value1 equals c.ParentID into gr
					from c in gr.DefaultIfEmpty()
					select new
					{
						Info2 = null != c ? (null != c.Parent ? new { c.Parent.Value1 } : null) : null
					}
					,
					from p in db.Parent
					join c in db.Child on p.Value1 equals c.ParentID into gr
					from c in gr.DefaultIfEmpty()
					select new
					{
						Info2 = null != c ? (null != c.Parent ? new { c.Parent.Value1 } : null) : null
					});
			}
		}

		public class ClassWithInternal
		{
			public int? Int { get; set; }
			internal string? InternalStr { get; set; }
		}

		[Test]
		public void InternalFieldProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Types.Select(t => new ClassWithInternal
				{
					Int = t.ID,
					InternalStr = t.StringValue
				});

				var result = query.Where(x => x.InternalStr != "").OrderBy(_ => _.Int).ToArray();
				Assert.That(result[0].InternalStr, Is.EqualTo(Types.First().StringValue));
			}
		}

		sealed class LocalClass
		{
		}

		[Test]
		public void SelectLocalTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var c = new LocalClass();
				var _ = db.Parent.Select(p => new { c, p.Value1 }).Distinct().ToList();
			}
		}

		// excluded providers where db object names doesn't match with query
		[Test]
		public void ComplexQuery(
			[DataSources(
					false,
					ProviderName.DB2,
					TestProvName.AllPostgreSQL,
					TestProvName.AllFirebird,
					TestProvName.AllSapHana)]
				string context)
		{
			var sql = "select PersonID, FirstName, MiddleName, LastName, Gender from Person where PersonID = 3";
			if (context.IsAnyOf(TestProvName.AllOracle))
				sql = "select \"PersonID\", \"FirstName\", \"MiddleName\", \"LastName\", \"Gender\" from \"Person\" where \"PersonID\" = 3";

			using (var db = GetDataConnection(context))
			{
				var person = db.Query<ComplexPerson>(sql).FirstOrDefault()!;

				Assert.That(person, Is.Not.Null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(person.ID, Is.EqualTo(3));
					Assert.That(person.Gender, Is.EqualTo(Gender.Female));
					Assert.That(person.Name, Is.Not.Null);
					Assert.That(person.Name.FirstName, Is.EqualTo("Jane"));
					Assert.That(person.Name.MiddleName, Is.Null);
					Assert.That(person.Name.LastName, Is.EqualTo("Doe"));
				}
			}
		}

		sealed class MainEntityObject
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(Length = 50)]
			public string? MainValue { get; set; }
		}

		public class ChildEntityObject
		{
			public int Id { get; set; }

			[Column(Length = 50)]
			public string? Value { get; set; }
		}

		public class DtoChildEntityObject
		{
			public int Id { get; set; }

			public string? Value { get; set; }

			static Expression<Func<ChildEntityObject?, DtoChildEntityObject?>> OwnerImpl()
			{
				return a => a == null
					? null
					: new DtoChildEntityObject
					{
						Id = a.Id,
						Value = a.Value
					};
			}

			[ExpressionMethod("OwnerImpl")]
			public static implicit operator DtoChildEntityObject?(ChildEntityObject a)
			{
				if (a == null) return null;
				return OwnerImpl().CompileExpression()(a);
			}

		}

		public class DtoResult
		{
			public DtoChildEntityObject? Child { get; set; }
			public string? Value { get; set; }
		}

		[Test]
		public void TestExpressionMethodInProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []
			{
				new MainEntityObject{Id = 1, MainValue = "MainValue 1"},
				new MainEntityObject{Id = 2, MainValue = "MainValue 2"},
			}))
			using (db.CreateLocalTable(new []
			{
				new ChildEntityObject{Id = 1, Value = "Value 1"}
			}))
			{
				var query =
					from m in db.GetTable<MainEntityObject>()
					from c in db.GetTable<ChildEntityObject>().LeftJoin(c => c.Id == m.Id)
					select new DtoResult
					{
						Child = c,
						Value = c.Value
					};

				query = query.OrderByDescending(c => c.Child!.Id);
				var result = query.ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Child, Is.Not.Null);
					Assert.That(result[1].Child, Is.Null);
				}
			}
		}

		sealed class IntermediateChildResult
		{
			public int?   ParentId { get; set; }
			public Child? Child    { get; set; }
		}

		[Test]
		public void TestConditionalProjectionOptimization(
			[IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context,
			[Values] bool includeChild,
			[Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			var query =
				from c in db.Child
				select new IntermediateChildResult { ParentId = c.ParentID, Child = includeChild ? c : null };

			var cacheMissCount = query.GetCacheMissCount();

			var result = query.ToArray().First();

			void CheckResult()
			{
			if (includeChild)
			{
				result.Child.Should().NotBeNull();
			}
			else
			{
				result.Child.Should().BeNull();

				((DataConnection)db).LastQuery.Should().NotContain("ChildID");
			}
			}

			CheckResult();

			includeChild = !includeChild;

			result = query.ToArray().First();

			CheckResult();

			if (iteration > 1)
			{
				query.GetCacheMissCount().Should().Be(cacheMissCount);
			}
		}

		[Test]
		public void TestConditionalInProjection([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []
			{
				new MainEntityObject{Id = 1, MainValue = "MainValue 1"},
				new MainEntityObject{Id = 2, MainValue = "MainValue 2"},
			}))
			using (db.CreateLocalTable(new []
			{
				new ChildEntityObject{Id = 1, Value = "Value 1"}
			}))
			{
				var query =
					from m in db.GetTable<MainEntityObject>()
					from c in db.GetTable<ChildEntityObject>().LeftJoin(c => c.Id == m.Id)
					select new
					{
						m.Id,
						Child1 = c,
						Child2 = c == null ? null : new ChildEntityObject { Id = c.Id, Value = c.Value },
						Child3 = c != null ? c : new ChildEntityObject { Id = 4, Value = "Generated" },
						Child4 = c.Value != "Value 1" ? c : null,
						SubChild = c == null
							? db.GetTable<ChildEntityObject>()
								.Select(sc => new ChildEntityObject
									{ Id = sc.Id, Value = sc != null ? sc.Value : "NeverHappen" }).FirstOrDefault()
							: c
					};

				var result = query.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Child1, Is.Not.Null);
					Assert.That(result[1].Child1, Is.Null);

					Assert.That(result[0].Child2, Is.Not.Null);
					Assert.That(result[0].Child2.Id, Is.EqualTo(1));
					Assert.That(result[0].Child2.Value, Is.EqualTo("Value 1"));
					Assert.That(result[1].Child2, Is.Null);

					Assert.That(result[0].Child3, Is.Not.Null);
					Assert.That(result[1].Child3, Is.Not.Null);
					Assert.That(result[1].Child3.Id, Is.EqualTo(4));
					Assert.That(result[1].Child3.Value, Is.EqualTo("Generated"));

					Assert.That(result[0].Child4, Is.Null);
					Assert.That(result[1].Child4, Is.Null);
				}
			}
		}

		[Test]
		public void TestConditionalInProjectionSubquery([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []
			{
				new MainEntityObject{Id = 1, MainValue = "MainValue 1"},
				new MainEntityObject{Id = 2, MainValue = "MainValue 2"},
			}))
			using (db.CreateLocalTable(new []
			{
				new ChildEntityObject{Id = 1, Value = "Value 1"}
			}))
			{
				var query =
					(from m in db.GetTable<MainEntityObject>()
					from c in db.GetTable<ChildEntityObject>().LeftJoin(c => c.Id == m.Id)
					select new
					{
						c.Id,
						Value = (c != null) ? c.Value : (m.MainValue != null ? m.MainValue : "")
					}).Distinct();

				var query2 = from q in query
					where q.Id % 2 == 0
					select q;

				var result = query2.ToArray();

			}
		}

		[Test]
		public void TestConditionalRecursive([IncludeDataSources(ProviderName.SqlCe, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					from c in db.Child.Take(1).DefaultIfEmpty()
					select new
					{
						a = p.ParentID == 1 ? c != null ? "1" : "2" : "3"
					};

				_ = query.ToList();
			}
		}

		sealed class ParentResult
		{
			public ParentResult(int parentID, int? value1)
			{
				ParentID = parentID;
				Value1 = value1;
			}

			public int? Value1 { get; }
			public int ParentID { get; }
		}

		[Test]
		public void TestConstructorProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					select new ParentResult(p.ParentID, p.Value1);

				var resultQuery = from q in query
					where q.Value1 != null
					select q;

				var queryExpected =
					from p in Parent
					select new ParentResult(p.ParentID, p.Value1);

				var resultExpected = from q in queryExpected
					where q.Value1 != null
					select q;

				AreEqual(resultExpected, resultQuery, ComparerBuilder.GetEqualityComparer<ParentResult>());
			}
		}

		[Test]
		public void TestMethodFabricProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					select Tuple.Create(p.ParentID, p.Value1);

				var resultQuery = from q in query
					where q.Item2 != null
					select q;

				var queryExpected =
					from p in Parent
					select Tuple.Create(p.ParentID, p.Value1);

				var resultExpected = from q in queryExpected
					where q.Item2 != null
					select q;

				AreEqual(resultExpected, resultQuery);
			}
		}

		[Test]
		public void TestComplexMethodFabricProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					select Tuple.Create(Tuple.Create(p.ParentID, p.Value1), Tuple.Create(p.Value1, p.ParentID));

				var resultQuery = from q in query
					where q.Item2.Item1 != null
					select q;

				var queryExpected =
					from p in Parent
					select Tuple.Create(Tuple.Create(p.ParentID, p.Value1), Tuple.Create(p.Value1, p.ParentID));

				var resultExpected = from q in queryExpected
					where q.Item2.Item1 != null
					select q;

				AreEqual(resultExpected, resultQuery);
			}
		}

		[Test]
		public void TestComplexNestedProjection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					select new
					{
						A = new
						{
							A1 = new
							{
								B1 = p.ParentID,
								B2 = p.Value1
							},
							A2 = new
							{
								C1 = p.Value1,
								C2 = p.ParentID
							}

						}
					};

				var resultQuery = from q in query
					where q.A.A1.B2 != null
					select q;

				resultQuery.ToArray();

				// var queryExpected =
				// 	from p in Parent
				// 	select Tuple.Create(Tuple.Create(p.ParentID, p.Value1), Tuple.Create(p.Value1, p.ParentID));
				//
				// var resultExpected = from q in queryExpected
				// 	where q.Item2.Item1 != null
				// 	select q;
				//
				// AreEqual(resultExpected, resultQuery);
			}
		}

		[Test]
		public void Select_TernaryNullableValue([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var result = db.Select(() => Sql.AsSql(value) == null ? (int?)null : Sql.AsSql(value!.Value));

				Assert.That(result, Is.EqualTo(value));
			}
		}

		[Test]
		public void Select_TernaryNullableValueReversed([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var result = db.Select(() => Sql.AsSql(value) != null ? Sql.AsSql(value!.Value) : (int?)null);

				Assert.That(result, Is.EqualTo(value));
			}
		}

		[Test]
		public void Select_TernaryNullableValue_Nested([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var result = db.Select(() => Sql.AsSql(value) == null ? (int?)null : (Sql.AsSql(value!.Value) < 2 ? Sql.AsSql(value.Value) : 2 + Sql.AsSql(value.Value)));

				Assert.That(result, Is.EqualTo(value));
			}
		}

		[Test]
		public void Select_TernaryNullableValueReversed_Nested([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (var db = GetDataContext(context, suppressSequentialAccess: true))
			{
				var result = db.Select(() => Sql.AsSql(value) != null ? (Sql.AsSql(value!.Value) < 2 ? Sql.AsSql(value.Value) : Sql.AsSql(value.Value) + 4) : (int?)null);

				Assert.That(result, Is.EqualTo(value));
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, ProviderName.SqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_RowNumber)]
		public void SelectWithIndexer([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
				.OrderByDescending(p => p.ID)
				.Select((p, idx) => new { p.FirstName, p.LastName, Index = idx })
				.Where(x => x.Index > 0);

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, ProviderName.SqlCe, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_RowNumber)]
		public void SelectWithIndexerAfterGroupBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
				.GroupBy(p => p.ID)
				.OrderByDescending(g => g.Key)
				.Select((g, idx) => new { g.Key, Index = idx })
				.Where(x => x.Index > 0);

			AssertQuery(query);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = ErrorHelper.Error_OrderByRequiredForIndexing)]
		public void SelectWithIndexerNoOrder([DataSources(TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, ProviderName.SqlCe, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
				.Select((p, idx) => new { p.FirstName, Index = idx })
				.Where(x => x.Index > 1);

			AssertQuery(query);
		}

		public class Table1788
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int Value1 { get; set; }

			public static Table1788[] Seed()
			{
				return new Table1788[]
				{
					new () { Id = 1, Value1 = 11 },
					new () { Id = 2, Value1 = 22 },
					new () { Id = 3, Value1 = 33 }
				};
			}
		}

		[Test]
		public void Issue1788Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Table1788.Seed()))
			{
				var results =
					from p in table
					from l in table.LeftJoin(l => l.Id == p.Id + 1)
					select new
					{
						f1 = Sql.ToNullable(l.Value1).HasValue,
						f2 = Sql.ToNullable(l.Value1)
					};

				var tableEnumerable = table.ToList();

				AreEqual(
					from p in tableEnumerable
					join l in tableEnumerable on p.Id + 1 equals l.Id into gj
					from l in gj.DefaultIfEmpty()
					select new
					{
						f1 = (l?.Value1).HasValue,
						f2 = l?.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Table1788.Seed()))
			{
				var results =
					from p in table
					from l in table.LeftJoin(l => l.Id == p.Id + 1)
					select new
					{
						f1 = Sql.ToNullable(l.Value1) != null,
						f2 = Sql.ToNullable(l.Value1)
					};

				var tableEnumerable = table.ToList();

				AreEqual(
					from p in tableEnumerable
					join l in tableEnumerable on p.Id + 1 equals l.Id into gj
					from l in gj.DefaultIfEmpty()
					select new
					{
						f1 = l?.Value1 != null,
						f2 = l?.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Table1788.Seed()))
			{
				var results =
					from p in table
					from l in table.LeftJoin(l => l.Id == p.Id + 1)
					select new
					{
#pragma warning disable CS0472 // comparison of non-null int? with null
						f1 = ((int?)l.Value1) != null,
#pragma warning restore CS0472
						f2 = (int?)l.Value1
					};

				var tableEnumerable = table.ToList();

				AreEqual(
					from p in tableEnumerable
					join l in tableEnumerable on p.Id + 1 equals l.Id into gj
					from l in gj.DefaultIfEmpty()
					select new
					{
						f1 = l?.Value1 != null,
						f2 = l?.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Table1788.Seed()))
			{
				var results =
					from p in table
					from l in table.LeftJoin(l => l.Id == p.Id + 1)
					select new
					{
						f1 = ((int?)l.Value1).HasValue,
						f2 = (int?)l.Value1
					};

				var tableEnumerable = table.ToList();

				AreEqual(
					from p in tableEnumerable
					join l in tableEnumerable on p.Id + 1 equals l.Id into gj
					from l in gj.DefaultIfEmpty()
					select new
					{
						f1 = l?.Value1 != null,
						f2 = l?.Value1
					},
					results);
			}
		}

		[Test]
		public void OuterApplyTest(
			[IncludeDataSources(
				TestProvName.AllPostgreSQL95Plus,
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllOracle12Plus,
				TestProvName.AllMySqlWithApply,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.Parent
					from c1 in db.Child.Where(c => c.ParentID == p.ParentID).Take(1).DefaultIfEmpty()
					let children = db.Child.Where(c => c.ChildID > 2).Select(c => new { c.ChildID, c.ParentID })
					select new
					{
						Parent = p,
						Child = c1,
						Any = children.Any(),
						Child1 = children.Where(c => c.ParentID >= p.ParentID).FirstOrDefault(),
						Child2 = children.Where(c => c.ParentID >= 2).Select(c => new { c.ChildID, c.ParentID }).FirstOrDefault(),
						ChildArray = children.Where(c => c.ParentID >= p.ParentID).Select(c => new object[] {c.ChildID, c.ParentID}).FirstOrDefault(),
						ChildDictionary1 = children.Where(c => c.ParentID >= p.ParentID).Select(c => new Dictionary<int, int?>{{c.ChildID, c.ParentID}}).FirstOrDefault(),
						ChildDictionary2 = children.Where(c => c.ParentID >= p.ParentID).Select(c => new Dictionary<string, int?>{{"ChildID", c.ChildID}, {"ParentID", c.ParentID}}).FirstOrDefault()
					};

				query = query
				 	.Distinct()
				 	.OrderBy(_ => _.Parent.ParentID);

				var expectedQuery =
					from p in Parent
					from c1 in Child.Where(c => c.ParentID == p.ParentID).Take(1).DefaultIfEmpty()
					let children = Child.Where(c => c.ChildID > 2).Select(c => new { c.ChildID, c.ParentID })
					select new
					{
						Parent = p,
						Child = c1,
						Any = children.Any(),
						Child1 = children.Where(c => c.ParentID >= p.ParentID).FirstOrDefault(),
						Child2 = children.Where(c => c.ParentID >= 2).Select(c => new { c.ChildID, c.ParentID }).FirstOrDefault(),
						ChildArray = children.Where(c => c.ParentID >= p.ParentID).Select(c => new object[] {c.ChildID, c.ParentID}).FirstOrDefault(),
						ChildDictionary1 = children.Where(c => c.ParentID >= p.ParentID).Select(c => new Dictionary<int, int?>{{c.ChildID, c.ParentID}}).FirstOrDefault(),
						ChildDictionary2 = children.Where(c => c.ParentID >= p.ParentID).Select(c => new Dictionary<string, int?>{{"ChildID", c.ChildID}, {"ParentID", c.ParentID}}).FirstOrDefault()
					};

				var actual = query.ToArray();

				 var expected = expectedQuery
				 	.Distinct()
				 	.OrderBy(_ => _.Parent.ParentID)
				 	.ToArray();

				AreEqualWithComparer(expected, actual, m => !typeof(Dictionary<,>).IsSameOrParentOf(m.MemberInfo.GetMemberType()));

				for (int i = 0; i < actual.Length; i++)
				{
					var item = actual[i];
					if (item.Child1 != null)
					{
						using (Assert.EnterMultipleScope())
						{
							Assert.That(item.ChildDictionary1[item.Child1.ChildID], Is.EqualTo(item.Child1.ParentID));
							Assert.That(item.ChildDictionary2["ChildID"], Is.EqualTo(item.Child1.ChildID));
							Assert.That(item.ChildDictionary2["ParentID"], Is.EqualTo(item.Child1.ParentID));
						}
					}
				}
			}
		}

		[Test]
		public void ToStringTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var query = from p in db.GetTable<Parent>()
					where p.ParentID == id
					select p;

				var sql1 = query.ToSqlQuery(new SqlGenerationOptions() { InlineParameters = true }).Sql;

				id = 2;

				var sql2 = query.ToSqlQuery(new SqlGenerationOptions() { InlineParameters = true }).Sql;

				BaselinesManager.LogQuery(sql1);
				BaselinesManager.LogQuery(sql2);

				Assert.That(sql1, Is.Not.EqualTo(sql2));
			}
		}

		[Table]
		sealed class SelectExpressionTable
		{
			[PrimaryKey] public int ID { get; set; }

			public static readonly SelectExpressionTable[] Data = new[]
			{
				new SelectExpressionTable() { ID = 1 }
			};
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static T Wrap1<T>(T value) => throw new InvalidOperationException();

		[Sql.Expression("{0}", ServerSideOnly = true)]
		private static T Wrap2<T>(T value) => value;

		[Sql.Expression("{0}", ServerSideOnly = false)]
		private static T Wrap3<T>(T value) => throw new InvalidOperationException();

		[Sql.Expression("{0}", ServerSideOnly = false)]
		private static T Wrap4<T>(T value) => value;

		[Test]
		public void SelectExpression1([DataSources(ProviderName.DB2, TestProvName.AllFirebird)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => Wrap1(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap1(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault();

				Assert.That(res, Is.True);
			}
		}

		[Test]
		public void SelectExpression2([DataSources(ProviderName.DB2, TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => Wrap2(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap2(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault();

				Assert.That(res, Is.True);
			}
		}

		[Test]
		public void SelectExpression3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => new Guid("b3d9b51c89f9442a893bcd8a6f667d37") != new Guid("61efdcd4659d41e8910c506a9c2f31c5")).SingleOrDefault();

				Assert.That(res, Is.True);
			}
		}

		[Test]
		public void SelectExpression4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				Assert.Throws<InvalidOperationException>(() => table.Take(1).Select(_ => Wrap3(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap3(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault());
			}
		}

		[Test]
		public void SelectExpression5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => Wrap4(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap4(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault();

				Assert.That(res, Is.True);
			}
		}

		[Table("test_mapping_column_2_prop")]
		public partial class TestMappingColumn1PropInfo
		{
			[Column("id"),          PrimaryKey] public long Id         { get; set; } // bigint
			[Column("test_number"), NotNull   ] public long TestNumber { get; set; } // bigint
		}

		[Table("test_mapping_column_2_prop")]
		public partial class TestMappingColumn2PropInfo
		{
			[Column("test_number"), NotNull   ] public long TestNumber { get; set; } // bigint

			[Column("test_number"), NotNull] public long TestNumber2 { get; set; } // bigint
			[Column("test_number"), NotNull] public long TestNumber3 { get; set; } // bigint
			[Column("id"),          PrimaryKey] public long Id         { get; set; } // bigint
		}

		[Test]
		public void MaterializeTwoMapped([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var data = new[] { new TestMappingColumn1PropInfo  { Id = 1, TestNumber = 3 } };
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var value = db.GetTable<TestMappingColumn2PropInfo>().First();
				Assert.That(value.TestNumber, Is.EqualTo(value.TestNumber2));
				Assert.That(value.TestNumber, Is.EqualTo(value.TestNumber3));
			}
		}

		[Table]
		sealed class Table860_1
		{
			[Column] public int Id  { get; set; }
			[Column] public int bId { get; set; }

			[Association(ThisKey = nameof(bId), OtherKey = nameof(Table860_2.Id))]
			public IList<Table860_2> Table2 { get; set; } = null!;
		}

		[Table]
		sealed class Table860_2
		{
			[Column] public int Id  { get; set; }
			[Column] public int cId { get; set; }

			[Association(ThisKey = nameof(cId), OtherKey = nameof(Table860_3.Id))]
			public Table860_3? Table3 { get; set; }
		}

		[Table]
		sealed class Table860_3
		{
			[Column] public int     Id   { get; set; }
			[Column] public string? Prop { get; set; }
		}

		[Test]
		public void Issue860Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Table860_1>())
			using (db.CreateLocalTable<Table860_2>())
			using (db.CreateLocalTable<Table860_3>())
			{
				var q = db.GetTable<Table860_1>()
					.Where(it => (
						(it.Table2 == null)
							? null
							: ((bool?)it.Table2.Any(d =>
								 (
									 ((d == null ? null : d.Table3) == null)
										 ? null
										 : d!.Table3!.Prop
								 ) == "aaa")
							)
					) == true
				);

				q.ToArray();
			}
		}

		#region Caching Tests

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2116")]
		public void CachedObjectRefence([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var reference = new Parent() { ParentID = 1001 };

			var query = db.Person
				.Select(p => new
				{
					p,
					Reference = reference
				});

			var p1 = query.ToList();

			p1.All(p => ReferenceEquals(p.Reference, reference)).Should().BeTrue();

			reference = new Parent() { ParentID = 1002 };
			var cacheMissCount = db.Person.GetCacheMissCount();

			var p2 = query.ToList();

			p2.All(p => ReferenceEquals(p.Reference, reference)).Should().BeTrue();

			db.Person.GetCacheMissCount().Should().Be(cacheMissCount);

		}

		#endregion

		#region SequentialAccess (#2116)
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2116")]
		public void SequentialAccessTest([DataSources] string context)
		{
			// providers that support SequentialAccess:
			// Access (OleDb)
			// MySql.Data
			// npgsql
			// System.Data.SqlClient
			// Microsoft.Data.SqlClient
			// SqlCe
			using (new OptimizeForSequentialAccess(true))
			using (var db = GetDataContext(context, interceptor: SequentialAccessCommandInterceptor.Instance, suppressSequentialAccess: true))
			{
				var q = db.Person
					.Select(p => new
					{
						FirstName  = p.FirstName,
						ID         = p.ID,
						IDNullable = Sql.ToNullable(p.ID),
						LastName   = p.LastName,
						FullName   = $"{p.FirstName} {p.LastName}"
					});

				foreach (var p in q.ToArray())
					Assert.That(p.FullName, Is.EqualTo($"{p.FirstName} {p.LastName}"));
			}
		}

		[Test]
		public void SequentialAccessTest_Complex([DataSources] string context)
		{
			// fields read out-of-order, multiple times and with different types
			using (new OptimizeForSequentialAccess(true))
			// suppressSequentialAccess: true to avoid interceptor added twice
			using (var db = GetDataContext(context, interceptor: SequentialAccessCommandInterceptor.Instance, suppressSequentialAccess: true))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(InheritanceParent[0].GetType(), Is.EqualTo(typeof(InheritanceParentBase)));
					Assert.That(InheritanceParent[1].GetType(), Is.EqualTo(typeof(InheritanceParent1)));
					Assert.That(InheritanceParent[2].GetType(), Is.EqualTo(typeof(InheritanceParent2)));
				}

				AreEqual(InheritanceParent, db.InheritanceParent);
				AreEqual(InheritanceChild, db.InheritanceChild);
			}
		}
		#endregion

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4520")]
		public void Issue4520Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.Types2
				.Where(i => i.ID == 1)
				.Select(i =>
				new
				{
					IsCurrent = !i.BoolValue.GetValueOrDefault() && i.IntValue == db.Types2.Where(p => p.ID == 2).Select(p => p.IntValue).FirstOrDefault()
				})
				.ToList();
		}

		#region Issue 3372

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3372")]
		public void Issue3372Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
					.Select(e => new
					{
						FirstName = e.FirstName,
						MiddleName = e.Patient!.Person != null && e.Patient.Person.LastName != null
							? new { Id = e.Patient.Person.LastName }
							: null
					});

			query.ToList();

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3372")]
		public void Issue3372Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
					.Select(e => new
					{
						FirstName = e.FirstName,
						MiddleName = e.Patient!.Person != null && e.Patient.Person.MiddleName != null
							? new { Id = e.Patient.Person.MiddleName }
							: null
					});

			query.ToList();

			Assert.That(query.GetSelectQuery().Select.Columns, Has.Count.EqualTo(3));
		}
		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4198")]
		public void Issue4198Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q = db.GetTable<Person>().Select(a => new
			{
				Account = (Person)a
			}).Where(cwa => cwa.Account.ID == 1);

			var r = q.Count();
		}

		#region 4199
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4199")]
		public void Issue4199Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q = db.GetTable<IUserAccount>().Select(a => new
			{
				Account = (UserAccountImplicit)a
			}).Where(cwa => cwa.Account.ID == 1);

			var r = q.Count();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4199")]
		public void Issue4199Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q = db.GetTable<IUserAccount>().Select(a => new
			{
				Account = (UserAccountExplicit)a
			}).Where(cwa => cwa.Account.ID == 1);

			var r = q.Count();
		}

		[Table("Person")]
		sealed class UserAccountImplicit : IUserAccount
		{
			[Column("PersonID")]
			public int ID { get; set; }
		}

		[Table("Person")]
		sealed class UserAccountExplicit : IUserAccount
		{
			[Column("PersonID")]
			public int ID { get; set; }

			[NotColumn]
			int IUserAccount.ID { get; }
		}

		interface IUserAccount
		{
			public int ID { get; }
		}
		#endregion

		sealed record NullableIssue4200Record(int? Id);

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4200")]
		public void Issue4200Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			int[] parentIds = { 1, 2, 3 };

			var result = db.Person.Select(a => new NullableIssue4200Record(a.ID))
				.Where(i => i.Id != null && parentIds.Contains(i.Id.Value))
				.ToList();
		}

		#region issue 4192
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4192")]
		public void Issue4192Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4192TableNotNullable>();

			var resultQueryable = GetByParentId(tb, 12);
			Assert.That(() => resultQueryable.ToList(), Throws.Exception);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4192")]
		public void Issue4192Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4192TableNullable>();

			var resultQueryable = GetByParentId(tb, 12);
			var result = resultQueryable.ToList();
		}

		static IQueryable<T> GetByParentId<T>(IQueryable<T> items, int? parentId) where T : IWithParent
		{
			return items.Where(i => i.ParentId == parentId);
		}

		interface IWithParent { int? ParentId { get; } }

		[Table]
		sealed class Issue4192TableNotNullable : IWithParent
		{
			[Column] public string? Name { get; set; }
			[Column] public int ParentId { get; set; }

			int? IWithParent.ParentId => ParentId;
		}

		[Table]
		sealed class Issue4192TableNullable : IWithParent
		{
			[Column] public string? Name { get; set; }
			[Column] public int? ParentId { get; set; }
		}
		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3181")]
		public void Issue3181Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q = from t1 in db.Person
					let t2 = new Person()
					{
						FirstName = t1.FirstName,
					}
					select new Person()
					{
						FirstName = t2.FirstName,
						LastName = t2.LastName,
						Gender = t2.Gender
					};

			var res = q.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.All(r => r.LastName == null), Is.True);
				Assert.That(res.All(r => r.Gender == default), Is.True);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3181")]
		public void Issue3181Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var q = from t2 in
						from t1 in db.Person
						select new Person()
						{
							FirstName = t1.FirstName,
						}
					select new Person()
					{
						FirstName = t2.FirstName,
						LastName = t2.LastName,
						Gender = t2.Gender
					};

			var res = q.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.All(r => r.LastName == null), Is.True);
				Assert.That(res.All(r => r.Gender == default), Is.True);
			}
		}
	}
}
