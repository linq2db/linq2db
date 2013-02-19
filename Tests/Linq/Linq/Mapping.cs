using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

#pragma warning disable 0649

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Mapping : TestBase
	{
		[Test]
		public void Enum1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where new[] { Gender.Male }.Contains(p.Gender) select p,
					from p in db.Person where new[] { Gender.Male }.Contains(p.Gender) select p);
		}

		[Test]
		public void Enum2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == Gender.Male select p,
					from p in db.Person where p.Gender == Gender.Male select p);
		}

		[Test]
		public void Enum21([DataContexts] string context)
		{
			var gender = Gender.Male;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender == gender select p,
					from p in db.Person where p.Gender == gender select p);
		}

		[Test]
		public void Enum3([DataContexts] string context)
		{
			var fm = Gender.Female;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person where p.Gender != fm select p,
					from p in db.Person where p.Gender != fm select p);
		}

		[Test]
		public void Enum4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void Enum5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4 where p.Value1 == TypeValue.Value3 select p,
					from p in db.Parent4 where p.Value1 == TypeValue.Value3 select p);
		}

		[Test]
		public void Enum6([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent4
					join c in    Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p,
					from p in db.Parent4
					join c in db.Child on p.ParentID equals c.ParentID
					where p.Value1 == TypeValue.Value1 select p);
		}

		[Test]
		public void Enum7([DataContexts] string context)
		{
			var v1 = TypeValue.Value1;

			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();
				db.Parent4.Update(p => p.Value1 == v1, p => new Parent4 { Value1 = v1 });

				if (context == ProviderName.PostgreSQL + ".LinqService")
					new Create.CreateData().PostgreSQL(ProviderName.PostgreSQL);
			}
		}

		enum TestValue
		{
			Value1 = 1,
		}

		[Table("Parent")]
		class TestParent
		{
			[Column] public int       ParentID;
			[Column] public TestValue Value1;
		}

		[Test]
		public void Enum81([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == TestValue.Value1).ToList();
		}

		[Test]
		public void Enum82([DataContexts] string context)
		{
			var testValue = TestValue.Value1;
			using (var db = GetDataContext(context))
				db.GetTable<TestParent>().Where(p => p.Value1 == testValue).ToList();
		}

		public enum Gender9
		{
			[MapValue("M")] Male,
			[MapValue("F")] Female,
			[MapValue("U")] Unknown,
			[MapValue("O")] Other,
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		public class Person9
		{
			public int     PersonID;
			public string  FirstName;
			public string  LastName;
			public string  MiddleName;
			public Gender9 Gender;
		}

		[Test]
		public void Enum9([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Person9>().Where(p => p.PersonID == 1 && p.Gender == Gender9.Male).ToList();
		}

		[Table(Name="Parent")]
		[Column("Value1", "Value.Value1")]
		[MapField("Value1", "Value.Value1")]
		public class ParentObject
		{
			[Column] public int   ParentID;
			         public Inner Value = new Inner();

			public class Inner
			{
				public int? Value1;
			}
		}

		////// TODO [Test]
		public void Inner1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			}
		}

		////// TODO [Test]
		public void Inner2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			}
		}

		[Table(Name="Child")]
		public class ChildObject
		{
			[Column] public int ParentID;
			[Column] public int ChildID;

			[Association(ThisKey="ParentID", OtherKey="ParentID")]
			public ParentObject Parent;
		}

		[Test]
		public void Inner3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var e = db.GetTable<ChildObject>().First(c => c.Parent.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
			}
		}

		struct MyInt
		{
			public int MyValue;
		}

		[Table(Name="Parent")]
		class MyParent
		{
			public MyInt ParentID;
			public int?  Value1;
		}

		class MyMappingSchemaOld : MappingSchemaOld
		{
		}

		class MyMappingSchema : MappingSchema
		{
			public MyMappingSchema()
			{
				SetConvertExpression<int,MyInt>(n => new MyInt { MyValue = n });
			}
		}

		static readonly MyMappingSchema    _myMappingSchema    = new MyMappingSchema();
		static readonly MyMappingSchemaOld _myMappingSchemaOld = new MyMappingSchemaOld();

		[Test]
		public void MyType1()
		{
			using (var db = new TestDataConnection { MappingSchemaOld = _myMappingSchemaOld }.AddMappingSchema(_myMappingSchema))
			{
				var list = db.GetTable<MyParent>().ToList();
			}
		}

		[Test]
		public void MyType2()
		{
			using (var db = new TestDataConnection { MappingSchemaOld = _myMappingSchemaOld }.AddMappingSchema(_myMappingSchema))
			{
				var list = db.GetTable<MyParent>()
					.Select(t => new MyParent { ParentID = t.ParentID, Value1 = t.Value1 })
					.ToList();
			}
		}

		//[Test] //////////////// TODO
		public void MyType3()
		{
			using (var db = new TestDataConnection { MappingSchemaOld = _myMappingSchemaOld })
			{
				db.BeginTransaction();

				try
				{
					db.Insert(new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID >= 1000);
				}
			}
		}


		[Table(Name="Parent")]
		class MyParent1
		{
			[Column] public int  ParentID;
			[Column] public int? Value1;

			[NonColumn, MapIgnore]
			public string Value2 { get { return "1"; } }

			public int GetValue() { return 2; }
		}

		[Test]
		public void MapIgnore1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,   Value2 = "1" }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2 }));
		}

		[Test]
		public void MapIgnore2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID,          Length = 1 }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, p.Value2.Length }));
		}

		[Test]
		public void MapIgnore3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					              Parent    .Select(p => new { p.ParentID, Value = 2            }),
					db.GetTable<MyParent1>().Select(p => new { p.ParentID, Value = p.GetValue() }));
		}
	}
}
