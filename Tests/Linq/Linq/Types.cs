using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;
using LinqToDB.DataAccess;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class Types : TestBase
	{
		[Test]
		public void Bool1()
		{
			var value = true;

			ForEachProvider(db => AreEqual(
				from p in    Parent where p.ParentID > 2 && value && true && !false select p,
				from p in db.Parent where p.ParentID > 2 && value && true && !false select p));
		}

		[Test]
		public void Bool2()
		{
			var value = true;

			ForEachProvider(db => AreEqual(
				from p in    Parent where p.ParentID > 2 && value || true && !false select p,
				from p in db.Parent where p.ParentID > 2 && value || true && !false select p));
		}

		[Test]
		public void Bool3()
		{
			var values = new int[0];

			ForEachProvider(db => AreEqual(
				from p in    Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p,
				from p in db.Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p));
		}

		[Test]
		public void BoolField1()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types where t.BoolValue select t.MoneyValue,
				from t in db.Types where t.BoolValue select t.MoneyValue));
		}

		[Test]
		public void BoolField2()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types where !t.BoolValue select t.MoneyValue,
				from t in db.Types where !t.BoolValue select t.MoneyValue));
		}

		[Test]
		public void BoolField3()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types where t.BoolValue == true select t.MoneyValue,
				from t in db.Types where t.BoolValue == true select t.MoneyValue));
		}

		[Test]
		public void BoolField4()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types where t.BoolValue == false select t.MoneyValue,
				from t in db.Types where t.BoolValue == false select t.MoneyValue));
		}

		[Test]
		public void BoolField5()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue,
				from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue));
		}

		[Test]
		public void BoolField6()
		{
			ForEachProvider(db => AreEqual(
				from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue,
				from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue));
		}

		[Test]
		public void BoolResult1()
		{
			ForEachProvider(db => AreEqual(
				from p in    Person select new { p.Patient, IsPatient = p.Patient != null },
				from p in db.Person select new { p.Patient, IsPatient = p.Patient != null }));
		}

		[Test]
		public void BoolResult2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Person select new { IsPatient = Sql.AsSql(p.Patient != null) },
				from p in db.Person select new { IsPatient = Sql.AsSql(p.Patient != null) }));
		}

		[Test]
		public void BoolResult3()
		{
			ForEachProvider(db => AreEqual(
				from p in    Person select Sql.AsSql(p.ID == 1),
				from p in db.Person select Sql.AsSql(p.ID == 1)));
		}

		[Test]
		public void GuidNew()
		{
			ForEachProvider(db => AreEqual(
				from p in    Types where p.GuidValue != Guid.NewGuid() select p.GuidValue,
				from p in db.Types where p.GuidValue != Guid.NewGuid() select p.GuidValue));
		}

		[Test]
		public void Guid1()
		{
			ForEachProvider(db => AreEqual(
				from p in    Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue,
				from p in db.Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue));
		}

		[Test]
		public void Guid2()
		{
			var guid3 = new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436");
			var guid4 = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b");

			var parm = Expression.Parameter(typeof(LinqDataTypes), "p");

			ForEachProvider(db =>
				Assert.AreNotEqual(
					db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid3),
									false,
									typeof(Guid).GetMethod("op_Equality")),
								new[] { parm }))
						.Single().GuidValue,
					db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid4),
									false,
									typeof(Guid).GetMethod("op_Equality")),
								new[] { parm }))
						.Single().GuidValue)
			);
		}

		[Test]
		public void ContainsGuid()
		{
			var ids = new [] { new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") };

			ForEachProvider(db => AreEqual(
				from p in    Types where ids.Contains(p.GuidValue) select p.GuidValue,
				from p in db.Types where ids.Contains(p.GuidValue) select p.GuidValue));
		}

		[Test]
		public void NewGuid()
		{
			ForEachProvider(
				new[] { ProviderName.DB2, ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access },
				db =>
				{
					db.Types.Delete(_ => _.ID > 1000);
					db.Types.Insert(() => new LinqDataTypes
					{
						ID            = 1001,
						MoneyValue    = 1001,
						DateTimeValue = Sql.CurrentTimestamp,
						BoolValue     = true,
						GuidValue     = Sql.NewGuid(),
						BinaryValue   = new Binary(new byte[] { 1 }),
						SmallIntValue = 1001
					});

					var guid = db.Types.Single(_ => _.ID == 1001).GuidValue;

					Assert.AreEqual(1001, db.Types.Single(_ => _.GuidValue == guid).ID);

					db.Types.Delete(_ => _.ID > 1000);
				});
		}

		[Test]
		public void BinaryLength()
		{
			ForEachProvider(
				new[] { ProviderName.Access },
				db =>
				{
					db.Types
						.Where(t => t.ID == 1)
						.Set(t => t.BinaryValue, new Binary(new byte[] { 1, 2, 3, 4, 5 }))
						.Update();

					Assert.That(
						(from t in db.Types where t.ID == 1 select t.BinaryValue.Length).First(),
						Is.EqualTo(5));

					db.Types
						.Where(t => t.ID == 1)
						.Set(t => t.BinaryValue, (Binary)null)
						.Update();
				});
		}

		[Test]
		public void InsertBinary1()
		{
			ForEachProvider(
				new[] { ProviderName.DB2, ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access },
				db =>
				{
					Binary data = null;

					db.Types.Delete(_ => _.ID > 1000);
					db.Types.Insert(() => new LinqDataTypes
					{
						ID          = 1001,
						BinaryValue = data,
						BoolValue   = true,
					});
					db.Types.Delete(_ => _.ID > 1000);
				});
		}

		[Test]
		public void UpdateBinary1()
		{
			ForEachProvider(db =>
			{
				db.Types
					.Where(t => t.ID == 1)
					.Set(t => t.BinaryValue, new Binary(new byte[] { 1, 2, 3, 4, 5 }))
					.Update();

				var g = from t in db.Types where t.ID == 1 select t.BinaryValue;

				foreach (var binary in g)
				{
				}
			});
		}

		[Test]
		public void UpdateBinary2()
		{
			ForEachProvider(new[] { ProviderName.SqlCe }, db =>
			{
				var ints     = new[] { 1, 2 };
				var binaries = new[] { new byte[] { 1, 2, 3, 4, 5 }, new byte[] { 5, 4, 3, 2, 1 } };

				for (var i = 1; i <= 2; i++)
				{
					db.Types
						.Where(t => t.ID == ints[i - 1])
						.Set(t => t.BinaryValue, binaries[i - 1])
						.Update();
				}

				var g = from t in db.Types where new[] { 1, 2 }.Contains(t.ID) select t;

				foreach (var binary in g)
					Assert.AreEqual(binaries[binary.ID - 1], binary.BinaryValue.ToArray());
			});
		}

		[Test]
		public void DateTime1()
		{
			var dt = Types2[3].DateTimeValue;

			ForEachProvider(db => AreEqual(
				from t in    Types2 where t.DateTimeValue.Value.Date > dt.Value.Date select t,
				from t in db.Types2 where t.DateTimeValue.Value.Date > dt.Value.Date select t));
		}

		[Test]
		public void Nullable()
		{
			ForEachProvider(db => AreEqual(
				from p in    Parent select new { Value = p.Value1.GetValueOrDefault() },
				from p in db.Parent select new { Value = p.Value1.GetValueOrDefault() }));
		}

		[Test]
		public void Unicode()
		{
			ForEachProvider(new[] { ProviderName.Informix, ProviderName.Firebird, ProviderName.Sybase }, db =>
			{
				try
				{
					db.Person.Delete(p => p.ID > 2);

					var id =
						db.Person
							.InsertWithIdentity(() => new Person
							{
								FirstName = "擊敗奴隸",
								LastName  = "Юникодкин",
								Gender    = Gender.Male
							});

					Assert.NotNull(id);

					var person = db.Person.Single(p => p.FirstName == "擊敗奴隸" && p.LastName == "Юникодкин");

					Assert.NotNull (person);
					Assert.AreEqual(id, person.ID);
					Assert.AreEqual("擊敗奴隸", person.FirstName);
					Assert.AreEqual("Юникодкин", person.LastName);
				}
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			});
		}

		[Test]
		public void TestCultureInfo()
		{
			var current = Thread.CurrentThread.CurrentCulture;

			Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

			ForEachProvider(db => AreEqual(
				from t in    Types where t.MoneyValue > 0.5m select t,
				from t in db.Types where t.MoneyValue > 0.5m select t));

			Thread.CurrentThread.CurrentCulture = current;
		}

		[Test]
		public void SmallInt()
		{
			ForEachProvider(db => AreEqual(
				from t1 in Types
				join t2 in Types on t1.SmallIntValue equals t2.ID
				select t1,
				from t1 in db.Types
				join t2 in db.Types on t1.SmallIntValue equals t2.ID
				select t1)
			);
		}

		[TableName("Person")]
		public class PersonCharTest
		{
			public int    PersonID;
			public string FirstName;
			public string LastName;
			public string MiddleName;
			public char   Gender;
		}

		[Test]
		public void CharTest1()
		{
			List<PersonCharTest> list;

			using (var db = new TestDbManager())
				list = db.GetTable<PersonCharTest>().ToList();

			ForEachProvider(db => AreEqual(
				from p in list                          where p.Gender == 'M' select p.PersonID,
				from p in db.GetTable<PersonCharTest>() where p.Gender == 'M' select p.PersonID));
		}

		[Test]
		public void CharTest2()
		{
			List<PersonCharTest> list;

			using (var db = new TestDbManager())
				list = db.GetTable<PersonCharTest>().ToList();

			ForEachProvider(db => AreEqual(
				from p in list                          where 'M' == p.Gender select p.PersonID,
				from p in db.GetTable<PersonCharTest>() where 'M' == p.Gender select p.PersonID));
		}

		[TableName("Person")]
		public class PersonBoolTest
		{
			public int    PersonID;
			public string FirstName;
			public string LastName;
			public string MiddleName;
			[MapField("Gender"), MapValue(true, "M"), MapValue(false, "F")]
			public bool   IsMale;
		}

		//[Test]
		public void BoolTest1()
		{
			List<PersonBoolTest> list;

			using (var db = new TestDbManager())
				list = db.GetTable<PersonBoolTest>().ToList();

			ForEachProvider(db => AreEqual(
				from p in list                          where p.IsMale select p.PersonID,
				from p in db.GetTable<PersonBoolTest>() where p.IsMale select p.PersonID));
		}

		//[Test]
		public void BoolTest2()
		{
			List<PersonBoolTest> list;

			using (var db = new TestDbManager())
				list = db.GetTable<PersonBoolTest>().ToList();

			ForEachProvider(db => AreEqual(
				from p in list                          where p.IsMale == true select p.PersonID,
				from p in db.GetTable<PersonBoolTest>() where p.IsMale == true select p.PersonID));
		}

		[Test]
		public void LongTest1()
		{
			ForEachProvider(db =>
			{
				uint value = 0;

				var q =
					from t in db.Types2
					where t.BigIntValue == value
					select t;

				q.ToList();
			});
		}

		[Test]
		public void CompareNullableInt()
		{
			int? param = null;

			ForEachProvider(db => AreEqual(
				from t in    Parent where param == null || t.Value1 == param select t,
				from t in db.Parent where param == null || t.Value1 == param select t));

			param = 1;

			ForEachProvider(db => AreEqual(
				from t in    Parent where param == null || t.Value1 == param select t,
				from t in db.Parent where param == null || t.Value1 == param select t));
		}

		[Test]
		public void CompareNullableBoolean1()
		{
			bool? param = null;

			ForEachProvider(db => AreEqual(
				from t in    Types where param == null || t.BoolValue == param select t,
				from t in db.Types where param == null || t.BoolValue == param select t));

			param = true;

			ForEachProvider(db => AreEqual(
				from t in    Types where param == null || t.BoolValue == param select t,
				from t in db.Types where param == null || t.BoolValue == param select t));
		}

		[Test]
		public void CompareNullableBoolean2()
		{
			short? param1 = null;
			bool?  param2 = null;

			ForEachProvider(db => AreEqual(
				from t1 in    Types
				join t2 in    Types on t1.ID equals t2.ID
				where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
				select t1,
				from t1 in db.Types
				join t2 in db.Types on t1.ID equals t2.ID
				where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
				select t1));

			//param1 = null;
			param2 = false;

			ForEachProvider(db => AreEqual(
				from t1 in    Types
				join t2 in    Types on t1.ID equals t2.ID
				where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
				select t1,
				from t1 in db.Types
				join t2 in db.Types on t1.ID equals t2.ID
				where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
				select t1));
		}

		[Test]
		public void CompareNullableBoolean3()
		{
			short? param1 = null;
			bool?  param2 = false;

			ForEachProvider(db => AreEqual(
				from t in    Types where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t,
				from t in db.Types where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t));
		}
	}
}
