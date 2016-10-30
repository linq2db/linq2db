using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Extensions;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class TypesTests : TestBase
	{
		[Test, DataContextSource]
		public void Bool1(string context)
		{
			var value = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID > 2 && value && true && !false select p,
					from p in db.Parent where p.ParentID > 2 && value && true && !false select p);
		}

		[Test, DataContextSource]
		public void Bool2(string context)
		{
			var value = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID > 2 && value || true && !false select p,
					from p in db.Parent where p.ParentID > 2 && value || true && !false select p);
		}

		[Test, DataContextSource]
		public void Bool3(string context)
		{
			var values = new int[0];

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p,
					from p in db.Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p);
		}

		[Test, DataContextSource]
		public void BoolField1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue select t.MoneyValue,
					from t in db.Types where t.BoolValue select t.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolField2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where !t.BoolValue select t.MoneyValue,
					from t in db.Types where !t.BoolValue select t.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolField3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue == true select t.MoneyValue,
					from t in db.Types where t.BoolValue == true select t.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolField4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue == false select t.MoneyValue,
					from t in db.Types where t.BoolValue == false select t.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolField5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue,
					from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolField6(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue,
					from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue);
		}

		[Test, DataContextSource]
		public void BoolResult1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { p.Patient, IsPatient = p.Patient != null },
					from p in db.Person select new { p.Patient, IsPatient = p.Patient != null });
		}

		[Test, DataContextSource]
		public void BoolResult2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { IsPatient = Sql.AsSql(p.Patient != null) },
					from p in db.Person select new { IsPatient = Sql.AsSql(p.Patient != null) });
		}

		[Test, DataContextSource]
		public void BoolResult3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select Sql.AsSql(p.ID == 1),
					from p in db.Person select Sql.AsSql(p.ID == 1));
		}

		[Test, DataContextSource]
		public void GuidNew(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue != Guid.NewGuid() select p.GuidValue,
					from p in db.Types where p.GuidValue != Guid.NewGuid() select p.GuidValue);
		}

		[Test, DataContextSource]
		public void Guid1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue,
					from p in db.Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue);
		}

		[Test, DataContextSource]
		public void Guid2(string context)
		{
			var guid3 = new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436");
			var guid4 = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b");

			var parm = Expression.Parameter(typeof(LinqDataTypes), "p");

			using (var db = GetDataContext(context))
				Assert.AreNotEqual(
					db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid3),
									false,
									typeof(Guid).GetMethodEx("op_Equality")),
								new[] { parm }))
						.Single().GuidValue,
					db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid4),
									false,
									typeof(Guid).GetMethodEx("op_Equality")),
								new[] { parm }))
						.Single().GuidValue);
		}

		[Test, DataContextSource]
		public void ContainsGuid(string context)
		{
			var ids = new [] { new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where ids.Contains(p.GuidValue) select p.GuidValue,
					from p in db.Types where ids.Contains(p.GuidValue) select p.GuidValue);
		}

		[Test, DataContextSource(
			ProviderName.DB2, ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access, ProviderName.SapHana)]
		public void NewGuid(string context)
		{
			using (var db = GetDataContext(context))
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
			}
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void BinaryLength(string context)
		{
			using (var db = GetDataContext(context))
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
			}
		}

		[Test, DataContextSource(
			ProviderName.DB2, ProviderName.Informix, ProviderName.Firebird, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access)]
		public void InsertBinary1(string context)
		{
			using (var db = GetDataContext(context))
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
			}
		}

		[Test, DataContextSource]
		public void UpdateBinary1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types
					.Where(t => t.ID == 1)
					.Set(t => t.BinaryValue, new Binary(new byte[] { 1, 2, 3, 4, 5 }))
					.Update();

				var g = from t in db.Types where t.ID == 1 select t.BinaryValue;

				foreach (var binary in g)
				{
				}
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe)]
		public void UpdateBinary2(string context)
		{
			using (var db = GetDataContext(context))
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
			}
		}

		[Test, DataContextSource]
		public void DateTime1(string context)
		{
			var dt = Types2[3].DateTimeValue;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where t.DateTimeValue.Value.Date > dt.Value.Date select t,
					from t in db.Types2 where t.DateTimeValue.Value.Date > dt.Value.Date select t);
		}

		[Test, DataContextSource(ProviderName.SQLite)]
		public void DateTime21(string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z");

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue = dt });

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue = pdt });

				Assert.AreNotEqual(dt.Ticks, dt2.Value.Ticks);
			}
		}

		[Test, DataContextSource(
				ProviderName.SqlCe, ProviderName.Access, ProviderName.SqlServer2005, ProviderName.DB2, ProviderName.Informix,
				ProviderName.Firebird, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql,
				ProviderName.Sybase, ProviderName.SqlServer2000, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void DateTime22(string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z");

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = dt });

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				Assert.AreEqual(dt, dt2);
			}
		}

		[Test, DataContextSource(
				ProviderName.SqlCe, ProviderName.Access, ProviderName.SqlServer2005, ProviderName.DB2, ProviderName.Informix,
				ProviderName.Firebird, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql,
				ProviderName.Sybase, ProviderName.SqlServer2000, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void DateTime23(string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z");

				db.Types2
					.Where(t => t.ID == 1)
					.Set  (_ => _.DateTimeValue2, dt)
					.Update();

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				Assert.AreEqual(dt, dt2);
			}
		}

		[Test, DataContextSource(
				ProviderName.SqlCe, ProviderName.Access, ProviderName.SqlServer2005, ProviderName.DB2, ProviderName.Informix,
				ProviderName.Firebird, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, TestProvName.MariaDB,
				TestProvName.MariaDB, TestProvName.MySql57, TestProvName.MySql57, ProviderName.Sybase, ProviderName.SqlServer2000, ProviderName.SapHana)]
		public void DateTime24(string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z");
				var tt  = db.Types2.First(t => t.ID == 1);

				tt.DateTimeValue2 = dt;

				db.Update(tt);

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				Assert.AreEqual(dt, dt2);
			}
		}

		[Test, DataContextSource]
		public void DateTimeArray1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100) }.Contains(t.DateTimeValue) select t,
					from t in db.Types2 where new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100) }.Contains(t.DateTimeValue) select t);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void DateTimeArray2(string context)
		{
			var arr = new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100), new DateTime(2012, 11, 7, 19, 19, 29, 90) };

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where arr.Contains(t.DateTimeValue) select t,
					from t in db.Types2 where arr.Contains(t.DateTimeValue) select t);
		}

		[Test, DataContextSource(ProviderName.Access)]
		public void DateTimeArray3(string context)
		{
			var arr = new List<DateTime?> { new DateTime(2001, 1, 11, 1, 11, 21, 100) };

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where arr.Contains(t.DateTimeValue) select t,
					from t in db.Types2 where arr.Contains(t.DateTimeValue) select t);
		}

		[Test, DataContextSource]
		public void DateTimeParams(string context)
		{
			var arr = new List<DateTime?>
			{
				new DateTime(1992, 1, 11, 1, 11, 21, 100),
				new DateTime(1993, 1, 11, 1, 11, 21, 100)
			};

			using (var db = GetDataContext(context))
			{
				foreach (var dateTime in arr)
				{
					var dt = DateTimeParams(db, dateTime);
					Assert.AreEqual(dateTime, dt);
				}
			}
		}

		static DateTime DateTimeParams(ITestDataContext db, DateTime? dateTime)
		{
			var q =
				from t in db.Types2
				where t.DateTimeValue > dateTime
				select new
					{
						t.DateTimeValue,
						dateTime.Value
					};

			return q.First().Value;
		}

		[Test, DataContextSource]
		public void Nullable(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1.GetValueOrDefault() },
					from p in db.Parent select new { Value = p.Value1.GetValueOrDefault() });
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Firebird, ProviderName.Sybase)]
		public void Unicode(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

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
			}
		}

#if !NETSTANDARD
		[Test, DataContextSource(
			ProviderName.Informix
			)]
		public void TestCultureInfo(string context)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.MoneyValue > 0.5m select t,
					from t in db.Types where t.MoneyValue > 0.5m select t);

			Thread.CurrentThread.CurrentCulture = current;
		}
#endif

		[Test, DataContextSource]
		public void SmallInt(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t1 in Types
					join t2 in Types on t1.SmallIntValue equals t2.ID
					select t1
					,
					from t1 in db.Types
					join t2 in db.Types on t1.SmallIntValue equals t2.ID
					select t1);
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		public class PersonCharTest
		{
			public int    PersonID;
			public string FirstName;
			public string LastName;
			public string MiddleName;
			public char   Gender;
		}

		[Test, DataContextSource]
		public void CharTest11(string context)
		{
			List<PersonCharTest> list;

			using (var db = new TestDataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where p.Gender == 'M' select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where p.Gender == 'M' select p.PersonID);
		}

		[Test, DataContextSource]
		public void CharTest12(string context)
		{
			List<PersonCharTest> list;

			using (var db = new TestDataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where p.Gender == 77 select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where p.Gender == 77 select p.PersonID);
		}

		[Test, DataContextSource]
		public void CharTest2(string context)
		{
			List<PersonCharTest> list;

			using (var db = new TestDataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where 'M' == p.Gender select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where 'M' == p.Gender select p.PersonID);
		}

		[Test, DataContextSource]
		public void BoolTest31(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where (t.BoolValue ?? false) select t,
					from t in db.Types2 where t.BoolValue.Value      select t);
		}

		[Test, DataContextSource]
		public void BoolTest32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where (t.BoolValue ?? false) select t,
					from t in db.Types2 where t.BoolValue == true    select t);
		}

		[Test, DataContextSource]
		public void BoolTest33(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types2 where (t.BoolValue ?? false) select t,
					from t in db.Types2 where true == t.BoolValue    select t);
		}

		[Test, DataContextSource]
		public void LongTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				uint value = 0;

				var q =
					from t in db.Types2
					where t.BigIntValue == value
					select t;

				q.ToList();
			}
		}

		[Test, DataContextSource]
		public void CompareNullableInt(string context)
		{
			int? param = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Parent where param == null || t.Value1 == param select t,
					from t in db.Parent where param == null || t.Value1 == param select t);

			param = 1;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Parent where param == null || t.Value1 == param select t,
					from t in db.Parent where param == null || t.Value1 == param select t);
		}

		[Test, DataContextSource]
		public void CompareNullableBoolean1(string context)
		{
			bool? param = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where param == null || t.BoolValue == param select t,
					from t in db.Types where param == null || t.BoolValue == param select t);

			param = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where param == null || t.BoolValue == param select t,
					from t in db.Types where param == null || t.BoolValue == param select t);
		}

		[Test, DataContextSource]
		public void CompareNullableBoolean2(string context)
		{
			short? param1 = null;
			bool?  param2 = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from t1 in    Types
					join t2 in    Types on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1
					,
					from t1 in db.Types
					join t2 in db.Types on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1);

			//param1 = null;
			param2 = false;

			using (var db = GetDataContext(context))
				AreEqual(
					from t1 in    Types
					join t2 in    Types on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1
					,
					from t1 in db.Types
					join t2 in db.Types on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1);
		}

		[Test, DataContextSource]
		public void CompareNullableBoolean3(string context)
		{
			short? param1 = null;
			bool?  param2 = false;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t,
					from t in db.Types where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t);
		}
	}
}
