using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if NET472
using System.Windows.Forms;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Reflection;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	using System.Data;
	using System.Data.Common;
	using System.Threading;
	using System.Threading.Tasks;
	using LinqToDB.Data.DbCommandProcessor;
	using Model;

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
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			}
		}

		void NewParam(IQueryable<Person> table, int i)
		{
			var expected = from p in Person select new { i, p.ID, p.FirstName };
			var result   = from p in table  select new { i, p.ID, p.FirstName };

			Assert.IsTrue(result.ToList().SequenceEqual(expected));
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
				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
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
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, TestProvName.AllSapHana)]
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

			Assert.IsTrue((DateTime.Now - dt).TotalSeconds < 30);
		}

		[Test]
		public void MutiplySelect12([DataSources(false)] string context)
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

				Assert.AreEqual(1, selectCount, "Why do we need \"select from select\"??");
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

				Assert.AreEqual(1,      q.ID);
				Assert.AreEqual("John", q.FirstName);
				Assert.AreEqual("None", q.MiddleName);
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

				Assert.AreEqual(1,        q.ID);
				Assert.AreEqual("John",   q.FirstName);
				Assert.AreEqual("Pupkin", q.LastName);
				Assert.AreEqual("None",   q.MiddleName);
			}
		}

		class MyMapSchema : MappingSchema
		{
			public MyMapSchema()
			{
				SetDefaultValue(typeof(string), null);
			}
		}

		static readonly MyMapSchema _myMapSchema = new MyMapSchema();

		[Test]
		public void Coalesce3([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				if (db is DataConnection)
				{
					((DataConnection)db).AddMappingSchema(_myMapSchema);

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

					Assert.AreEqual(1,        q.ID);
					Assert.AreEqual("John",   q.FirstName);
					Assert.AreEqual("Pupkin", q.LastName);
					Assert.AreEqual("None",   q.MiddleName);
				}
			}
		}

		[Test]
		public void Coalesce4([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from c in    Child
					select Sql.AsSql((from ch in    Child where ch.ChildID == c.ChildID select ch.Parent!.Value1).FirstOrDefault() ?? c.ChildID),
					from c in db.Child
					select Sql.AsSql((from ch in db.Child where ch.ChildID == c.ChildID select ch.Parent!.Value1).FirstOrDefault() ?? c.ChildID));
		}

		[Test]
		public void Coalesce5([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1),
					from p in db.Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1));
		}

		[Test]
		public void Concatenation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName  = "123" + p.FirstName + "456" };
				var f = q.Where(p => p.FirstName == "123John456").ToList().First();
				Assert.AreEqual(1, f.ID);
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

#if NET472
		[Test]
		public void ConstractClass([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				db.Parent.Select(f =>
					new ListViewItem(new[] { "", f.ParentID.ToString(), f.Value1.ToString() })
					{
						Checked    = true,
						ImageIndex = 0,
						Tag        = f.ParentID
					}).ToList();
		}
#endif

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

				Assert.AreEqual("7.77.True.0",  lines[0]);
				Assert.AreEqual("6.66.False.1", lines[1]);
				Assert.AreEqual("6.65.True.2",  lines[2]);

				q =
					db.Child
						.OrderByDescending(m => m.ChildID)
						.Where(m => m.Parent != null && m.ParentID > 0);

				lines =
					q.Select(
						(m, i) =>
							ConvertString(m.Parent!.ParentID.ToString(), m.ChildID, i % 2 == 0, i)).ToArray();

				Assert.AreEqual("7.77.True.0", lines[0]);
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
				Assert.AreSame(q.p1, q.p2);
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
		public void SelectField()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from p in db.GetTable<TestParent>()
					select p.Value1_;

				var sql = q.ToString()!;

				Assert.That(sql.IndexOf("ParentID_"), Is.LessThan(0));
			}
		}

		[Test]
		public void SelectComplexField([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<ComplexPerson>()
					select p.Name.LastName;

				var sql = q.ToString()!;

				TestContext.WriteLine(sql);

				Assert.That(sql.IndexOf("First"),    Is.LessThan(0));
				Assert.That(sql.IndexOf("LastName"), Is.GreaterThan(0));
			}
		}

		[Test]
		public void SelectComplex1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var r = db.GetTable<ComplexPerson>().First(_ => _.ID == 1);

				Assert.AreEqual("John", r.Name.FirstName);
				Assert.IsNull(r.Name.MiddleName);
				Assert.AreEqual("Pupkin", r.Name.LastName);
			}
		}

		[Test]
		public void SelectComplex2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var r = db.GetTable<ComplexPerson2>().First(_ => _.ID == 1);

				Assert.AreEqual("John", r.Name.FirstName);
				Assert.IsNull(r.Name.MiddleName);
				Assert.AreEqual("Pupkin", r.Name.LastName);
			}
		}

		[Test]
		public void SelectComplex3([DataSources] string context)
		{
			var ms = new MappingSchema();
			var b  = ms.GetFluentMappingBuilder();

			b
				.Entity<ComplexPerson3>()        .HasTableName ("Person")
				.Property(_ => _.ID)             .HasColumnName("PersonID")
				.Property(_ => _.Name.FirstName) .HasColumnName("FirstName")
				.Property(_ => _.Name.LastName)  .HasColumnName("LastName")
				.Property(_ => _.Name.MiddleName).HasColumnName("MiddleName");

			using (var db = GetDataContext(context, ms))
			{
				var r = db.GetTable<ComplexPerson3>().First(_ => _.ID == 1);

				Assert.AreEqual("John", r.Name.FirstName);
				Assert.IsNull(r.Name.MiddleName);
				Assert.AreEqual("Pupkin", r.Name.LastName);
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

					Assert.AreEqual(e, e2);
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

					var e  = new LinqDataTypes()  { ID = 1000, BoolValue = false };

					var e2 = db.Types.First(_ => _.ID == 1000);

					Assert.AreEqual(e, e2);
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
					from c in db.Child.InnerJoin(c => c.ParentID == p.ParentID)
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
								ParentID = q.Info1 != null ? (int?)q.Info1.ParentID : (int?)null,
								q.Info1!.Value1,
								Value2 = q.Info2.Value1
							}
					};

				var query3 = query2.Where(p => p.InfoAll.ParentID!.Value > 0 || p.InfoAll.Value1 > 0  || p.InfoAll.Value2 > 0 );

				var _ = query3.ToArray();
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

		class LocalClass
		{
		}

		[Test, Explicit("Fails")]
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
			if (context.Contains("Oracle"))
				sql = "select \"PersonID\", \"FirstName\", \"MiddleName\", \"LastName\", \"Gender\" from \"Person\" where \"PersonID\" = 3";

			using (var db = new TestDataConnection(context))
			{
				var person = db.Query<ComplexPerson>(sql).FirstOrDefault()!;

				Assert.NotNull(person);
				Assert.AreEqual(3, person.ID);
				Assert.AreEqual(Gender.Female, person.Gender);
				Assert.NotNull(person.Name);
				Assert.AreEqual("Jane", person.Name.FirstName);
				Assert.IsNull(person.Name.MiddleName);
				Assert.AreEqual("Doe", person.Name.LastName);
			}
		}

		class MainEntityObject
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
				return OwnerImpl().Compile()(a);
			}

		}

		public class DtoResult
		{
			public DtoChildEntityObject? Child { get; set; }
			public string? Value { get; set; }
		}

		[Test]
		public void TestExpressionMethodInProjection([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

				Assert.NotNull(result[0].Child);
				Assert.Null(result[1].Child);
			}
		}


		[Test]
		public void TestConditionalInProjection([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

				var result = query.ToArray();

				Assert.NotNull(result[0].Child1);
				Assert.IsNull (result[1].Child1);

				Assert.NotNull(result[0].Child2);
				Assert.AreEqual(1,         result[0].Child2.Id);
				Assert.AreEqual("Value 1", result[0].Child2.Value);
				Assert.Null(result[1].Child2);

				Assert.NotNull(result[0].Child3);
				Assert.NotNull(result[1].Child3);
				Assert.AreEqual(4,           result[1].Child3.Id);
				Assert.AreEqual("Generated", result[1].Child3.Value);

				Assert.Null(result[0].Child4);
				Assert.IsNull(result[1].Child4);
			}
		}

		[Test]
		public void TestConditionalInProjectionSubquery([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

		class ParentResult
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
		public void TestConstructorProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void TestMethodFabricProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

		[ActiveIssue("Currently linq2db do not support such queries")]
		[Test]
		public void TestComplexMethodFabricProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void TestComplexNestedProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
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

		// DB2: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null
		// IFX: Informix needs type hint for NULL value
		[ActiveIssue(Configurations = new[] { TestProvName.AllInformix, ProviderName.DB2 })]
		[Test]
		public void Select_TernaryNullableValue([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (new CustomCommandProcessor(null))
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => Sql.AsSql(value) == null ? (int?)null : Sql.AsSql(value!.Value));

				Assert.AreEqual(value, result);
			}
		}

		// DB2: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null
		// IFX: Informix needs type hint for NULL value
		[ActiveIssue(Configurations = new[] { TestProvName.AllInformix, ProviderName.DB2 })]
		[Test]
		public void Select_TernaryNullableValueReversed([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (new CustomCommandProcessor(null))
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => Sql.AsSql(value) != null ? Sql.AsSql(value!.Value) : (int?)null);

				Assert.AreEqual(value, result);
			}
		}

		// INFORMIX and DB2 need type hint in select
		// CI: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null
		[Test]
		[ActiveIssue(Configurations = new[] { TestProvName.AllInformix, ProviderName.DB2 })]
		public void Select_TernaryNullableValue_Nested([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (new CustomCommandProcessor(null))
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => Sql.AsSql(value) == null ? (int?)null : (Sql.AsSql(value!.Value) < 2 ? Sql.AsSql(value.Value) : 2 + Sql.AsSql(value.Value)));

				Assert.AreEqual(value, result);
			}
		}

		// INFORMIX and DB2 need type hint in select
		// CI: SQL0418N  The statement was not processed because the statement contains an invalid use of one of the following: an untyped parameter marker, the DEFAULT keyword, or a null
		[Test]
		[ActiveIssue(Configurations = new[] { TestProvName.AllInformix, ProviderName.DB2 })]
		public void Select_TernaryNullableValueReversed_Nested([DataSources] string context, [Values(null, 0, 1)] int? value)
		{
			// mapping fails and fallbacks to slow-mapper
			using (new CustomCommandProcessor(null))
			using (var db = GetDataContext(context))
			{
				var result = db.Select(() => Sql.AsSql(value) != null ? (Sql.AsSql(value!.Value) < 2 ? Sql.AsSql(value.Value) : Sql.AsSql(value.Value) + 4) : (int?)null);

				Assert.AreEqual(value, result);
			}
		}

		[Table("Parent")]
		public class Parent1788
		{
			[Column]
			public int Value1 { get; }
		}

		[Test]
		public void Issue1788Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = from p in db.GetTable<Parent1788>()
							   select new
							   {
								   f1 = Sql.ToNullable(p.Value1).HasValue,
								   f2 = Sql.ToNullable(p.Value1)
							   };

				AreEqual(
					from p in db.Parent.AsEnumerable()
					select new
					{
						f1 = p.Value1.HasValue,
						f2 = p.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = from p in db.GetTable<Parent1788>()
							  select new
							  {
								  f1 = Sql.ToNullable(p.Value1) != null,
								  f2 = Sql.ToNullable(p.Value1)
							  };

				AreEqual(
					from p in db.Parent.AsEnumerable()
					select new
					{
						f1 = p.Value1 != null,
						f2 = p.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = from p in db.GetTable<Parent1788>()
							  select new
							  {
#pragma warning disable CS0472 // comparison of non-null int? with null
								  f1 = ((int?)p.Value1) != null,
#pragma warning restore CS0472
								  f2 = (int?)p.Value1
							  };

				AreEqual(
					from p in db.Parent.AsEnumerable()
					select new
					{
						f1 = p.Value1 != null,
						f2 = p.Value1
					},
					results);
			}
		}

		[Test]
		public void Issue1788Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var results = from p in db.GetTable<Parent1788>()
							  select new
							  {
								  f1 = ((int?)p.Value1).HasValue,
								  f2 = (int?)p.Value1
							  };

				AreEqual(
					from p in db.Parent.AsEnumerable()
					select new
					{
						f1 = p.Value1 != null,
						f2 = p.Value1
					},
					results);
			}
		}

		[Test]
		public void OuterApplyTest([IncludeDataSources(TestProvName.AllPostgreSQL95Plus, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle12)] string context)
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
						Assert.That(item.ChildDictionary1[item.Child1.ChildID], Is.EqualTo(item.Child1.ParentID));
						Assert.That(item.ChildDictionary2["ChildID"],           Is.EqualTo(item.Child1.ChildID));
						Assert.That(item.ChildDictionary2["ParentID"],          Is.EqualTo(item.Child1.ParentID));
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

				var sql1 = query.ToString();

				id = 2;

				var sql2 = query.ToString();
				
				Assert.That(sql1, Is.Not.EqualTo(sql2));
			}
		}

		[Table]
		class SelectExpressionTable
		{
			[PrimaryKey] public int ID { get; set; }

			public static readonly SelectExpressionTable[] Data = new[]
			{
				new SelectExpressionTable() { ID = 1 }
			};
		}

		[Sql.Expression("{0}", ServerSideOnly = true)]
		public static T Wrap1<T>(T value) => throw new InvalidOperationException();

		[Sql.Expression("{0}", ServerSideOnly = true)]
		public static T Wrap2<T>(T value) => value;

		[Sql.Expression("{0}", ServerSideOnly = false)]
		public static T Wrap3<T>(T value) => throw new InvalidOperationException();

		[Sql.Expression("{0}", ServerSideOnly = false)]
		public static T Wrap4<T>(T value) => value;

		[Test]
		public void SelectExpression1([DataSources(ProviderName.DB2, TestProvName.AllFirebird)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => Wrap1(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap1(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault();

				Assert.True(res);
			}
		}

		[Test]
		public void SelectExpression2([DataSources(ProviderName.DB2, TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => Wrap2(new Guid("b3d9b51c89f9442a893bcd8a6f667d37")) != Wrap2(new Guid("61efdcd4659d41e8910c506a9c2f31c5"))).SingleOrDefault();

				Assert.True(res);
			}
		}

		[Test]
		public void SelectExpression3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(SelectExpressionTable.Data))
			{
				var res = table.Take(1).Select(_ => new Guid("b3d9b51c89f9442a893bcd8a6f667d37") != new Guid("61efdcd4659d41e8910c506a9c2f31c5")).SingleOrDefault();

				Assert.True(res);
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

				Assert.True(res);
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
		public void MaterializeTwoMapped([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		class Table860_1
		{
			[Column] public int Id  { get; set; }
			[Column] public int bId { get; set; }

			[Association(ThisKey = nameof(bId), OtherKey = nameof(Table860_2.Id))]
			public IList<Table860_2> Table2 { get; set; } = null!;
		}

		[Table]
		class Table860_2
		{
			[Column] public int Id  { get; set; }
			[Column] public int cId { get; set; }

			[Association(ThisKey = nameof(cId), OtherKey = nameof(Table860_3.Id))]
			public Table860_3? Table3 { get; set; }
		}

		[Table]
		class Table860_3
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
			using (new CustomCommandProcessor(new SequentialAccessCommandProcessor()))
			using (var db = GetDataContext(context))
			{
				var q = db.Person
					.Select(p => new
					{
						p.FirstName,
						ID = p.ID,
						IDNullable = Sql.ToNullable(p.ID),
						p.LastName,
						FullName = $"{p.FirstName} {p.LastName}"
					});

				foreach (var p in q.ToArray())
					Assert.AreEqual($"{p.FirstName} {p.LastName}", p.FullName);
			}
		}

		[Test]
		public void SequentialAccessTest_Complex([DataSources] string context)
		{
			// fields read out-of-order, multiple times and with different types
			using (new CustomCommandProcessor(new SequentialAccessCommandProcessor()))
			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(typeof(InheritanceParentBase), InheritanceParent[0].GetType());
				Assert.AreEqual(typeof(InheritanceParent1), InheritanceParent[1].GetType());
				Assert.AreEqual(typeof(InheritanceParent2), InheritanceParent[2].GetType());

				AreEqual(InheritanceParent, db.InheritanceParent);
				AreEqual(InheritanceChild, db.InheritanceChild);
			}
		}
		#endregion
	}
}
