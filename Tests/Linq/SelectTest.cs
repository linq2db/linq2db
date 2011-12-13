using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using NUnit.Framework;

using LinqToDB.Data;
using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;
using LinqToDB.DataAccess;
using LinqToDB.Reflection;
using LinqToDB.Mapping;

namespace Data.Linq
{
	using Model;

	[TestFixture]
	public class SelectTest : TestBase
	{
		[Test]
		public void SimpleDirect()
		{
			TestJohn(db => db.Person);
		}

		[Test]
		public void Simple()
		{
			TestJohn(db => from p in db.Person select p);
		}

		[Test]
		public void SimpleDouble()
		{
			TestJohn(db => db.Person.Select(p => p).Select(p => p));
		}

		[Test]
		public void New()
		{
			var expected = from p in Person select new { p.ID, p.FirstName };

			ForEachProvider(db =>
			{
				var result = from p in db.Person select new { p.ID, p.FirstName };
				Assert.IsTrue(result.ToList().SequenceEqual(expected));
			});
		}

		void NewParam(IQueryable<Person> table, int i)
		{
			var expected = from p in Person select new { i, p.ID, p.FirstName };
			var result   = from p in table  select new { i, p.ID, p.FirstName };

			Assert.IsTrue(result.ToList().SequenceEqual(expected));
		}

		[Test]
		public void NewParam()
		{
			ForEachProvider(db => { for (var i = 0; i < 5; i++) NewParam(db.Person, i); });
		}

		[Test]
		public void InitObject()
		{
			TestJohn(db => from p in db.Person select new Person { ID = p.ID, FirstName = p.FirstName });
		}

		[Test]
		public void NewObject()
		{
			TestJohn(db => from p in db.Person select new Person(p.ID, p.FirstName));
		}

		[Test]
		public void NewInitObject()
		{
			TestJohn(db => from p in db.Person select new Person(p.ID) { FirstName = p.FirstName });
		}

		[Test]
		public void NewWithExpr()
		{
			TestPerson(1, "John1", db => from p in db.Person select new Person(p.ID) { FirstName = (p.FirstName + "1\r\r\r").TrimEnd('\r') });
		}

		[Test]
		public void MultipleSelect1()
		{
			TestJohn(db => db.Person
				.Select(p => new { PersonID = p.ID, Name = p.FirstName })
				.Select(p => new Person(p.PersonID) { FirstName = p.Name }));
		}

		[Test]
		public void MultipleSelect2()
		{
			TestJohn(db => 
				from p in db.Person
				select new { PersonID = p.ID, Name = p.FirstName } into pp
				select new Person(pp.PersonID) { FirstName = pp.Name });
		}

		[Test]
		public void MultipleSelect3()
		{
			TestJohn(db => db.Person
				.Select(p => new        { PersonID = p.ID,       Name      = p.FirstName })
				.Select(p => new Person { ID       = p.PersonID, FirstName = p.Name      })
				.Select(p => new        { PersonID = p.ID,       Name      = p.FirstName })
				.Select(p => new Person { ID       = p.PersonID, FirstName = p.Name      }));
		}

		[Test]
		public void MultipleSelect4()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { p1 })
				.Select(p2 => new        { p2 })
				.Select(p3 => new Person { ID = p3.p2.p1.ID, FirstName = p3.p2.p1.FirstName }));
		}

		[Test]
		public void MultipleSelect5()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { p1 })
				.Select(p2 => new Person { ID = p2.p1.ID, FirstName = p2.p1.FirstName })
				.Select(p3 => new        { p3 })
				.Select(p4 => new Person { ID = p4.p3.ID, FirstName = p4.p3.FirstName }));
		}

		[Test]
		public void MultipleSelect6()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { p1 })
				.Select(p2 => new Person { ID = p2.p1.ID, FirstName = p2.p1.FirstName })
				.Select(p3 => p3)
				.Select(p4 => new Person { ID = p4.ID,    FirstName = p4.FirstName }));
		}

		[Test]
		public void MultipleSelect7()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { ID = p1.ID + 1, p1.FirstName })
				.Select(p2 => new Person { ID = p2.ID - 1, FirstName = p2.FirstName }));
		}

		[Test]
		public void MultipleSelect8()
		{
			ForEachProvider(db =>
			{
				var person = (

					db.Person
						.Select(p1 => new Person { ID = p1.ID * 2,           FirstName = p1.FirstName })
						.Select(p2 => new        { ID = p2.ID / "22".Length, p2.FirstName })

				).ToList().Where(p => p.ID == 1).First();
				Assert.AreEqual(1,      person.ID);
				Assert.AreEqual("John", person.FirstName);
			});
		}

		[Test]
		public void MultipleSelect9()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { ID = p1.ID - 1, p1.FirstName })
				.Select(p2 => new Person { ID = p2.ID + 1, FirstName = p2.FirstName })
				.Select(p3 => p3)
				.Select(p4 => new        { ID = p4.ID * "22".Length, p4.FirstName })
				.Select(p5 => new Person { ID = p5.ID / 2, FirstName = p5.FirstName }));
		}

		[Test]
		public void MultipleSelect10()
		{
			TestJohn(db => db.Person
				.Select(p1 => new        { p1.ID, p1 })
				.Select(p2 => new        { p2.ID, p2.p1, p2 })
				.Select(p3 => new        { p3.ID, p3.p1.FirstName, p11 = p3.p2.p1, p3 })
				.Select(p4 => new Person { ID = p4.p11.ID, FirstName = p4.p3.p1.FirstName }));
		}

		[Test]
		public void Coalesce()
		{
			ForEachProvider(db =>
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
			});
		}

		[Test]
		public void Coalesce2()
		{
			ForEachProvider(db =>
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
			});
		}

		class MyMapSchema : MappingSchema
		{
			public override void InitNullValues()
			{
				base.InitNullValues();
				DefaultStringNullValue = null;
			}
		}

		static readonly MyMapSchema _myMapSchema = new MyMapSchema();

		[Test]
		public void Coalesce3()
		{
			ForEachProvider(db =>
			{
				if (db is DbManager)
				{
					((DbManager)db).MappingSchema = _myMapSchema;

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
			});
		}

		[Test]
		public void Coalesce4()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from c in    Child
				select Sql.AsSql((from ch in    Child where ch.ChildID == c.ChildID select ch.Parent.Value1).FirstOrDefault() ?? c.ChildID),
				from c in db.Child
				select Sql.AsSql((from ch in db.Child where ch.ChildID == c.ChildID select ch.Parent.Value1).FirstOrDefault() ?? c.ChildID)));
		}

		[Test]
		public void Coalesce5()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db => AreEqual(
				from p in    Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1),
				from p in db.Parent select Sql.AsSql(p.Children.Max(c => (int?)c.ChildID) ?? p.Value1)));
		}

		[Test]
		public void Concatenation()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.ID == 1 select new { p.ID, FirstName  = "123" + p.FirstName + "456" };
				var f = q.Where(p => p.FirstName == "123John456").ToList().First();
				Assert.AreEqual(1, f.ID);
			});
		}

		IEnumerable<int> GetList(int i)
		{
			yield return i;
		}

		[Test]
		public void SelectEnumerable()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select new { Max = GetList(p.ParentID).Max() },
				from p in db.Parent select new { Max = GetList(p.ParentID).Max() }));
		}

		[Test]
		public void ConstractClass()
		{
			ForEachProvider(db =>
				db.Parent.Select(f =>
					new ListViewItem(new[] { "", f.ParentID.ToString(), f.Value1.ToString() })
					{
						Checked    = true,
						ImageIndex = 0,
						Tag        = f.ParentID
					}).ToList());
		}

		static string ConvertString(string s, int? i, bool b, int n)
		{
			return s + "." + i + "." + b + "." + n;
		}

		[Test]
		public void Index()
		{
			ForEachProvider(db =>
			{
				var q =
					db.Child
						.OrderByDescending(m => m.ChildID)
						.Where(m => m.Parent != null && m.ParentID > 0);

				var lines =
					q.Select(
						(m, i) =>
							ConvertString(m.Parent.ParentID.ToString(), m.ChildID, i % 2 == 0, i)).ToArray();

				Assert.AreEqual("7.77.True.0", lines[0]);
			});
		}

		[Test]
		public void InterfaceTest()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Parent2 select new { p.ParentID, p.Value1 };
				q.ToList();
			});
		}

		[Test]
		public void ProjectionTest1()
		{
			ForEachProvider(db => AreEqual(
				from c in    Child select new { c.ChildID, ID = 0, ID1 = c.ParentID2.ParentID2, c.ParentID2.Value1, ID2 = c.ParentID },
				from c in db.Child select new { c.ChildID, ID = 0, ID1 = c.ParentID2.ParentID2, c.ParentID2.Value1, ID2 = c.ParentID }));
		}

		[TableName("Person")]
		[ObjectFactory(typeof(Factory))]
		public class TestPersonObject
		{
			public class Factory : IObjectFactory
			{
				#region IObjectFactory Members

				public object CreateInstance(TypeAccessor typeAccessor, InitContext context)
				{
					if (context == null)
						throw new Exception("InitContext is null while mapping from DataReader!");

					return typeAccessor.CreateInstance();
				}

				#endregion
			}

			public int    PersonID;
			public string FirstName;
		}

		[Test]
		public void ObjectFactoryTest()
		{
			ForEachProvider(db => db.GetTable<TestPersonObject>().ToList());
		}

		[Test]
		public void ProjectionTest2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Person select p.Patient,
				from p in db.Person select p.Patient));
		}

		[Test]
		public void EqualTest1()
		{
			ForEachProvider(db =>
			{
				var q = (from p in db.Parent select new { p1 = p, p2 = p }).First();
				Assert.AreSame(q.p1, q.p2);
			});
		}

		[Test]
		public void SelectEnumOnClient()
		{
			ForEachProvider(context =>
			{
				var arr = new List<Person> { new Person() };
				var p = context.Person.Select(person => new { person.ID, Arr = arr.Take(1) }).FirstOrDefault();

				p.Arr.Single();
			});
		}
	}
}
