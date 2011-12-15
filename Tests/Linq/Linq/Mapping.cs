using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Convert = System.Convert;

#pragma warning disable 0649

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Mapping : TestBase
	{
		[Test]
		public void Enum1()
		{
			var expected = from p in Person where new[] { Gender.Male }.Contains(p.Gender) select p;
			ForEachProvider(db => AreEqual(expected, from p in db.Person where new[] { Gender.Male }.Contains(p.Gender) select p));
		}

		[Test]
		public void Enum2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Person where p.Gender == Gender.Male select p,
				from p in db.Person where p.Gender == Gender.Male select p));
		}

		[Test]
		public void Enum21()
		{
			var gender = Gender.Male;

			ForEachProvider(db => AreEqual(
				from p in    Person where p.Gender == gender select p,
				from p in db.Person where p.Gender == gender select p));
		}

		[Test]
		public void Enum3()
		{
			var fm = Gender.Female;

			var expected = from p in Person where p.Gender != fm select p;
			ForEachProvider(db => AreEqual(expected, from p in db.Person where p.Gender != fm select p));
		}

		[Test]
		public void Enum4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent4 where p.Value1 == TypeValue.Value1 select p,
				from p in db.Parent4 where p.Value1 == TypeValue.Value1 select p));
		}

		[Test]
		public void Enum5()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent4 where p.Value1 == TypeValue.Value3 select p,
				from p in db.Parent4 where p.Value1 == TypeValue.Value3 select p));
		}

		[Test]
		public void Enum6()
		{
			ForEachProvider(db => AreEqual(
				from p in Parent4
				join c in Child on p.ParentID equals c.ParentID
				where p.Value1 == TypeValue.Value1 select p,
				from p in db.Parent4
				join c in db.Child on p.ParentID equals c.ParentID
				where p.Value1 == TypeValue.Value1 select p));
		}

		[Test]
		public void Enum7()
		{
			var v1 = TypeValue.Value1;

			ForEachProvider(db => db.Parent4.Update(p => p.Value1 == v1, p => new Parent4 { Value1 = v1 }));
		}

		enum TestValue
		{
			Value1 = 1,
		}

		[TableName("Parent")]
		class TestParent
		{
			public int       ParentID;
			public TestValue Value1;
		}

		[Test]
		public void Enum81()
		{
			ForEachProvider(db => db.GetTable<TestParent>().Where(p => p.Value1 == TestValue.Value1).ToList());
		}

		[Test]
		public void Enum82()
		{
			var testValue = TestValue.Value1;
			ForEachProvider(db => db.GetTable<TestParent>().Where(p => p.Value1 == testValue).ToList());
		}

		public enum Gender9
		{
			[MapValue('M')] Male,
			[MapValue('F')] Female,
			[MapValue('U')] Unknown,
			[MapValue('O')] Other,
		}

		[TableName("Person")]
		public class Person9
		{
			public int     PersonID;
			public string  FirstName;
			public string  LastName;
			public string  MiddleName;
			public Gender9 Gender;
		}

		[Test]
		public void Enum9()
		{
			ForEachProvider(db =>
				db.GetTable<Person9>().Where(p => p.PersonID == 1 && p.Gender == Gender9.Male).ToList());
		}

		[TableName("Parent")]
		[MapField("Value1", "Value.Value1")]
		public class ParentObject
		{
			public int   ParentID;
			public Inner Value = new Inner();

			public class Inner
			{
				public int? Value1;
			}
		}

		[Test]
		public void Inner1()
		{
			ForEachProvider(db =>
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			});
		}

		[Test]
		public void Inner2()
		{
			ForEachProvider(db =>
			{
				var e = db.GetTable<ParentObject>().First(p => p.ParentID == 1 && p.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
				Assert.AreEqual(1, e.Value.Value1);
			});
		}

		[TableName("Child")]
		public class ChildObject
		{
			public int ParentID;
			public int ChildID;

			[Association(ThisKey="ParentID", OtherKey="ParentID")]
			public ParentObject Parent;
		}

		[Test]
		public void Inner3()
		{
			ForEachProvider(db =>
			{
				var e = db.GetTable<ChildObject>().First(c => c.Parent.Value.Value1 == 1);
				Assert.AreEqual(1, e.ParentID);
			});
		}

		[TableName("Parent")]
		public class ParentObject2
		{
			class IntToDateMemberMapper : MemberMapper
			{
				public override void SetValue(object o, object value)
				{
					((ParentObject2)o).Value1 = new DateTime(2010, 1, Convert.ToInt32(value));
				}
			}

			public int      ParentID;
			[MemberMapper(typeof(IntToDateMemberMapper))]
			public DateTime Value1;
		}

		[Test]
		public void MemberMapperTest1()
		{
			ForEachProvider(db =>
			{
				var q =
					from p in db.GetTable<ParentObject2>()
					where p.ParentID == 1
					select p;

				Assert.AreEqual(new DateTime(2010, 1, 1), q.First().Value1);
			});
		}

		//[Test]
		public void MemberMapperTest2()
		{
			ForEachProvider(db =>
			{
				var q =
					from p in db.GetTable<ParentObject2>()
					where p.ParentID == 1
					select p.Value1;

				Assert.AreEqual(new DateTime(2010, 1, 1), q.First());
			});
		}

		struct MyInt
		{
			public int MyValue;
		}

		[TableName("Parent")]
		class MyParent
		{
			public MyInt ParentID;
			public int?  Value1;
		}

		class MyMappingSchema : MappingSchema
		{
			public override object ConvertChangeType(object value, Type conversionType, bool isNullable)
			{
				if (conversionType == typeof(MyInt))
					return new MyInt { MyValue = Convert.ToInt32(value) };

				if (value is MyInt)
					value = ((MyInt)value).MyValue;

				return base.ConvertChangeType(value, conversionType, isNullable);
			}

			public override object ConvertParameterValue(object value, Type systemType)
			{
				return value is MyInt ? ((MyInt)value).MyValue : value;
			}
		}

		static readonly MyMappingSchema _myMappingSchema = new MyMappingSchema();

		[Test]
		public void MyType1()
		{
			using (var db = new TestDbManager { MappingSchema = _myMappingSchema })
			{
				var list = db.GetTable<MyParent>().ToList();
			}
		}

		[Test]
		public void MyType2()
		{
			using (var db = new TestDbManager { MappingSchema = _myMappingSchema })
			{
				var list = db.GetTable<MyParent>()
					.Select(t => new MyParent { ParentID = t.ParentID, Value1 = t.Value1 })
					.ToList();
			}
		}

		[Test]
		public void MyType3()
		{
			using (var db = new TestDbManager { MappingSchema = _myMappingSchema })
			{
				db.BeginTransaction();
				db.Insert(new MyParent { ParentID = new MyInt { MyValue = 1001 }, Value1 = 1001 });
			}
		}
	}
}
