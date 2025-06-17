using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TypesTests : TestBase
	{
		[Test]
		public void Bool1([DataSources] string context)
		{
			var value = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID > 2 && value && true && !false select p,
					from p in db.Parent where p.ParentID > 2 && value && true && !false select p);
		}

		[Test]
		public void Bool2([DataSources] string context)
		{
			var value = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where p.ParentID > 2 && value || true && !false select p,
					from p in db.Parent where p.ParentID > 2 && value || true && !false select p);
		}

		[Test]
		public void Bool3([DataSources] string context)
		{
			var values = Array.Empty<int>();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p,
					from p in db.Parent where values.Contains(p.ParentID) && !false || p.ParentID > 2 select p);
		}

		[Test]
		public void BoolField1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue select t.MoneyValue,
					from t in db.Types where t.BoolValue select t.MoneyValue);
		}

		[Test]
		public void BoolField2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where !t.BoolValue select t.MoneyValue,
					from t in db.Types where !t.BoolValue select t.MoneyValue);
		}

		[Test]
		public void BoolField3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue == true select t.MoneyValue,
					from t in db.Types where t.BoolValue == true select t.MoneyValue);
		}

		[Test]
		public void BoolField4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.BoolValue == false select t.MoneyValue,
					from t in db.Types where t.BoolValue == false select t.MoneyValue);
		}

		[Test]
		public void BoolField5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue,
					from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b == false select p.MoneyValue);
		}

		[Test]
		public void BoolField6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue,
					from p in from t in db.Types select new { t.MoneyValue, b = !t.BoolValue } where p.b select p.MoneyValue);
		}

		[Test]
		public void BoolResult1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { p.Patient, IsPatient = p.Patient != null },
					from p in db.Person select new { p.Patient, IsPatient = p.Patient != null });
		}

		[Test]
		public void BoolResult2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select new { IsPatient = Sql.AsSql(p.Patient != null) },
					from p in db.Person select new { IsPatient = Sql.AsSql(p.Patient != null) });
		}

		[Test]
		public void BoolResult3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Person select Sql.AsSql(p.ID == 1),
					from p in db.Person select Sql.AsSql(p.ID == 1));
		}

		[Test]
		public void GuidNew([DataSources] string context)
		{
			using (new DisableBaseline("Server-side guid generation test"))
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue != Guid.NewGuid() select p.GuidValue,
					from p in db.Types where p.GuidValue != Guid.NewGuid() select p.GuidValue);
		}

		[Test]
		public void Guid1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue,
					from p in db.Types where p.GuidValue == new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") select p.GuidValue);
		}

		[Test]
		public void Guid2([DataSources] string context)
		{
			var guid3 = new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436");
			var guid4 = new Guid("40932fdb-1543-4e4a-ac2c-ca371604fb4b");

			var parm = Expression.Parameter(typeof(LinqDataTypes), "p");

			using (var db = GetDataContext(context))
				Assert.That(
					db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid4),
									false,
									typeof(Guid).GetMethodEx("op_Equality")),
								new[] { parm }))
						.Single().GuidValue, Is.Not.EqualTo(db.Types
						.Where(
							Expression.Lambda<Func<LinqDataTypes,bool>>(
								Expression.Equal(
									Expression.PropertyOrField(parm, "GuidValue"),
									Expression.Constant(guid3),
									false,
									typeof(Guid).GetMethodEx("op_Equality")),
								new[] { parm }))
						.Single().GuidValue));
		}

		[Test]
		public void ContainsGuid([DataSources] string context)
		{
			var ids = new [] { new Guid("D2F970C0-35AC-4987-9CD5-5BADB1757436") };

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types where ids.Contains(p.GuidValue) select p.GuidValue,
					from p in db.Types where ids.Contains(p.GuidValue) select p.GuidValue);
		}

		[Test]
		public void NewGuid(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllFirebird,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllAccess,
				TestProvName.AllSapHana)]
			string context)
		{
			using (new DisableBaseline("Server-side guid generation test"))
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
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

				Assert.That(db.Types.Single(_ => _.GuidValue == guid).ID, Is.EqualTo(1001));
			}
		}

		[Test]
		public void BinaryLength([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types
					.Where(t => t.ID == 1)
					.Set(t => t.BinaryValue, new Binary(new byte[] { 1, 2, 3, 4, 5 }))
					.Update();

				Assert.That(
					(from t in db.Types where t.ID == 1 select t.BinaryValue!.Length).First(),
					Is.EqualTo(5));

				db.Types
					.Where(t => t.ID == 1)
					.Set(t => t.BinaryValue, (Binary?)null)
					.Update();
			}
		}

		[Test]
		public void InsertBinary1(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllSQLite)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				Binary? data = null;

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

		[Test]
		public void UpdateBinary1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
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

		[Test]
		public void UpdateBinary2([DataSources(ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
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
					Assert.That(binary.BinaryValue!.ToArray(), Is.EqualTo(binaries[binary.ID - 1]));
			}
		}

		[Test]
		public void DateTime1([DataSources] string context)
		{
			var dt = Types2[3].DateTimeValue;

			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where t.DateTimeValue!.Value.Date > dt!.Value.Date select t),
											from t in db.Types2 where t.DateTimeValue!.Value.Date > dt!.Value.Date select t);
		}

		[Test]
		public void DateTime21([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z", DateTimeFormatInfo.InvariantInfo);

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue = dt });

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue = pdt });

				Assert.That(dt2!.Value.Ticks, Is.Not.EqualTo(dt.Ticks));
			}
		}

		[Test]
		public void DateTime22(
			[DataSources(
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				TestProvName.AllAccess,
				TestProvName.AllSqlServer2005,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z", DateTimeFormatInfo.InvariantInfo);

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = dt });

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				if (context.IsAnyOf(ProviderName.ClickHouseMySql))
					dt = dt.AddTicks(-dt.Ticks % 10);

				Assert.That(dt2, Is.EqualTo(dt));
			}
		}

		[Test]
		public void DateTime23(
			[DataSources(
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				TestProvName.AllAccess,
				TestProvName.AllSqlServer2005,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z", DateTimeFormatInfo.InvariantInfo);

				db.Types2
					.Where(t => t.ID == 1)
					.Set  (_ => _.DateTimeValue2, dt)
					.Update();

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				if (context.IsAnyOf(ProviderName.ClickHouseMySql))
					dt = dt.AddTicks(-dt.Ticks % 10);

				Assert.That(dt2, Is.EqualTo(dt));
			}
		}

		[Test]
		public void DateTime24(
			[DataSources(
				TestProvName.AllSQLite,
				ProviderName.SqlCe,
				TestProvName.AllAccess,
				TestProvName.AllSqlServer2005,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllFirebird,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var pdt = db.Types2.First(t => t.ID == 1).DateTimeValue2;
				var dt  = DateTime.Parse("2010-12-14T05:00:07.4250141Z", DateTimeFormatInfo.InvariantInfo);
				var tt  = db.Types2.First(t => t.ID == 1);

				tt.DateTimeValue2 = dt;

				db.Update(tt);

				var dt2 = db.Types2.First(t => t.ID == 1).DateTimeValue2;

				db.Types2.Update(t => t.ID == 1, t => new LinqDataTypes2 { DateTimeValue2 = pdt });

				if (context.IsAnyOf(ProviderName.ClickHouseMySql))
					dt = dt.AddTicks(-dt.Ticks % 10);

				Assert.That(dt2, Is.EqualTo(dt));
			}
		}

		[Test]
		public void DateTimeArray1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100) }.Contains(t.DateTimeValue) select t),
											from t in db.Types2 where new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100) }.Contains(t.DateTimeValue) select t);
		}

		[Test]
		public void DateTimeArray2([DataSources(TestProvName.AllAccessOleDb)] string context)
		{
			var arr = new DateTime?[] { new DateTime(2001, 1, 11, 1, 11, 21, 100), new DateTime(2012, 11, 7, 19, 19, 29, 90) };

			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where arr.Contains(t.DateTimeValue) select t),
											from t in db.Types2 where arr.Contains(t.DateTimeValue) select t);
		}

		[Test]
		public void DateTimeArray3([DataSources(TestProvName.AllAccessOleDb)] string context)
		{
			var arr = new List<DateTime?> { new DateTime(2001, 1, 11, 1, 11, 21, 100) };

			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where arr.Contains(t.DateTimeValue) select t),
											from t in db.Types2 where arr.Contains(t.DateTimeValue) select t);
		}

		[Test]
		public void DateTimeParams([DataSources] string context)
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
					Assert.That(dt, Is.EqualTo(dateTime));
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
						dateTime!.Value
					};

			return q.First().Value;
		}

		[Test]
		public void Nullable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select new { Value = p.Value1.GetValueOrDefault() },
					from p in db.Parent select new { Value = p.Value1.GetValueOrDefault() });
		}

		[Test]
		public void Unicode([DataSources(
			TestProvName.AllInformix, TestProvName.AllFirebird, TestProvName.AllSybase)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.BeginTransaction();

				db.Insert(new Person()
				{
					ID        = 100,
					FirstName = "擊敗奴隸",
					LastName  = "Юникодкин",
					Gender    = Gender.Male
				});

				var person = db.Person.Single(p => p.FirstName == "擊敗奴隸" && p.LastName == "Юникодкин");

				Assert.That(person, Is.Not.Null);
				Assert.Multiple(() =>
				{
					Assert.That(person.FirstName, Is.EqualTo("擊敗奴隸"));
					Assert.That(person.LastName, Is.EqualTo("Юникодкин"));
				});
			}
		}

		[Test]
		public void TestCultureInfo([DataSources(TestProvName.AllInformix)] string context)
		{
			var current = Thread.CurrentThread.CurrentCulture;

			Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where t.MoneyValue > 0.5m select t,
					from t in db.Types where t.MoneyValue > 0.5m select t);

			Thread.CurrentThread.CurrentCulture = current;
		}

		[Test]
		public void SmallInt([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t1 in GetTypes(context)
					join t2 in GetTypes(context) on t1.SmallIntValue equals t2.ID
					select t1
					,
					from t1 in db.Types
					join t2 in db.Types on t1.SmallIntValue equals t2.ID
					select t1);
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		public class PersonCharTest
		{
			public int     PersonID;
			public string  FirstName = null!;
			public string  LastName  = null!;
			public string? MiddleName;
			public char    Gender;
		}

		[Test]
		public void CharTest11([DataSources] string context)
		{
			List<PersonCharTest> list;

			using (var db = new DataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where p.Gender == 'M' select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where p.Gender == 'M' select p.PersonID);
		}

		[Test]
		public void CharTest12([DataSources] string context)
		{
			List<PersonCharTest> list;

			using (var db = new DataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where p.Gender == 77 select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where p.Gender == 77 select p.PersonID);
		}

		[Test]
		public void CharTest2([DataSources] string context)
		{
			List<PersonCharTest> list;

			using (var db = new DataConnection())
				list = db.GetTable<PersonCharTest>().ToList();

			using (var db = GetDataContext(context))
				AreEqual(
					from p in list                          where 'M' == p.Gender select p.PersonID,
					from p in db.GetTable<PersonCharTest>() where 'M' == p.Gender select p.PersonID);
		}

		[Test]
		public void BoolTest31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where (t.BoolValue ?? false) select t),
											from t in db.Types2 where t.BoolValue!.Value      select t);
		}

		[Test]
		public void BoolTest32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where (t.BoolValue ?? false) select t),
											from t in db.Types2 where t.BoolValue == true    select t);
		}

		[Test]
		public void BoolTest33([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					AdjustExpectedData(db,	from t in    Types2 where (t.BoolValue ?? false) select t),
											from t in db.Types2 where true == t.BoolValue    select t);
		}

		[Test]
		public void LongTest1([DataSources] string context)
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

		[Test]
		public void CompareNullableInt([DataSources] string context)
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

		[Test]
		public void CompareNullableBoolean1([DataSources] string context)
		{
			bool? param = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in GetTypes(context) where param == null || t.BoolValue == param select t,
					from t in db.Types          where param == null || t.BoolValue == param select t);

			param = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in GetTypes(context) where param == null || t.BoolValue == param select t,
					from t in db.Types          where param == null || t.BoolValue == param select t);
		}

		[Test]
		public void CompareNullableBoolean2([DataSources] string context)
		{
			short? param1 = null;
			bool?  param2 = null;

			using (var db = GetDataContext(context))
				AreEqual(
					from t1 in GetTypes(context)
					join t2 in GetTypes(context) on t1.ID equals t2.ID
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
					from t1 in GetTypes(context)
					join t2 in GetTypes(context) on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1
					,
					from t1 in db.Types
					join t2 in db.Types on t1.ID equals t2.ID
					where (param1 == null || t1.SmallIntValue == param1) && (param2 == null || t1.BoolValue == param2)
					select t1);
		}

		[Test]
		public void CompareNullableBoolean3([DataSources] string context)
		{
			short? param1 = null;
			bool?  param2 = false;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in GetTypes(context) where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t,
					from t in db.Types          where (param1 == null || t.SmallIntValue == param1) && (param2 == null || t.BoolValue == param2) select t);
		}

		// AllTypes is mess...
		[Table]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		sealed class AllTypes
		{
			[Column] public int     ID             { get; set; }

			[Column]
			[Column("REALDATATYPE", Configuration = ProviderName.DB2)]
			[Column("realDataType", Configuration = ProviderName.Informix)]
			[Column("realDataType", Configuration = ProviderName.Oracle)]
			[Column("realDataType", Configuration = ProviderName.PostgreSQL)]
			[Column("realDataType", Configuration = ProviderName.SapHana)]
			[Column("realDataType", Configuration = ProviderName.SqlCe)]
			[Column("realDataType", Configuration = ProviderName.SqlServer)]
			[Column("realDataType", Configuration = ProviderName.Sybase)]
			public float? floatDataType { get; set; }

			[Column]
			[Column("DOUBLEDATATYPE", Configuration = ProviderName.DB2)]
			[Column("realDataType"  , Configuration = ProviderName.Access)]
			[Column("realDataType"  , Configuration = ProviderName.SQLite)]
			[Column("floatDataType" , Configuration = ProviderName.Informix)]
			[Column("floatDataType" , Configuration = ProviderName.Oracle)]
			[Column("floatDataType" , Configuration = ProviderName.SapHana)]
			[Column("floatDataType" , Configuration = ProviderName.SqlCe)]
			[Column("floatDataType" , Configuration = ProviderName.SqlServer)]
			[Column("floatDataType" , Configuration = ProviderName.Sybase)]
			public double? doubleDataType { get; set; }
		}

		[Test]
		public void TestSpecialValues(
			[DataSources(
				TestProvName.AllSQLite,
				TestProvName.AllAccess,
				TestProvName.AllInformix,
				TestProvName.AllSybase,
				TestProvName.AllSqlServer,
				TestProvName.AllMySql,
				TestProvName.AllSapHana,
				ProviderName.DB2,
				// SQL CE allows special values using parameters, but no idea how to generate them as literal
				ProviderName.SqlCe
				)] string context,
			[Values] bool inline)
		{
			// Firebird25: https://github.com/FirebirdSQL/firebird/issues/6750
			var skipFloatInf = context.IsAnyOf(ProviderName.Firebird25) && inline;
			var skipId       = context.IsAnyOf(ProviderName.DB2) || context.IsAnyOf(TestProvName.AllSybase) || context.IsAnyOf(ProviderName.SqlCe);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.InlineParameters = inline;

				var maxID = db.GetTable<AllTypes>().Select(_ => _.ID).Max();
				var real  = float.NaN;
				var dbl   = double.NaN;
				if (skipId)
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						floatDataType = real,
						doubleDataType = dbl,
					});
				else
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						ID             = 1000,
						floatDataType  = real,
						doubleDataType = dbl,
					});
				real = skipFloatInf ? float.NaN : float.NegativeInfinity;
				dbl  = double.NegativeInfinity;
				if (skipId)
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						floatDataType  = real,
						doubleDataType = dbl,
					});
				else
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						ID             = 1001,
						floatDataType  = real,
						doubleDataType = dbl,
					});
				real = skipFloatInf ? float.NaN : float.PositiveInfinity;
				dbl  = double.PositiveInfinity;
				if (skipId)
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						floatDataType  = real,
						doubleDataType = dbl,
					});
				else
					db.GetTable<AllTypes>().Insert(() => new AllTypes()
					{
						ID             = 1002,
						floatDataType  = real,
						doubleDataType = dbl,
					});

				var res = db.GetTable<AllTypes>()
					.Where(_ => _.ID > maxID)
					.OrderBy(_ => _.ID)
					.Select(_ => new { _.floatDataType, _.doubleDataType})
					.ToArray();

				Assert.That(res, Has.Length.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].floatDataType, Is.NaN);
					Assert.That(res[0].doubleDataType, Is.NaN);

					Assert.That(res[1].floatDataType, Is.Not.Null);
					Assert.That(res[1].doubleDataType, Is.Not.Null);
				});
				if (skipFloatInf)
					Assert.That(res[0].floatDataType, Is.NaN);
				else
					Assert.That(float.IsNegativeInfinity(res[1].floatDataType!.Value), Is.True);
				Assert.Multiple(() =>
				{
					Assert.That(double.IsNegativeInfinity(res[1].doubleDataType!.Value), Is.True);

					Assert.That(res[2].floatDataType, Is.Not.Null);
					Assert.That(res[2].doubleDataType, Is.Not.Null);
				});
				if (skipFloatInf)
					Assert.That(res[0].floatDataType, Is.NaN);
				else
					Assert.That(float.IsPositiveInfinity(res[2].floatDataType!.Value), Is.True);
				Assert.That(double.IsPositiveInfinity(res[2].doubleDataType!.Value), Is.True);
			}
		}

		#region Issue 4469
		[ActiveIssue(Configurations = [TestProvName.AllSQLite])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4469")]
		public void Issue4469Test1([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4469Table.Data);
			db.InlineParameters = inline;

			var param = 33;

			var result = (from v in tb
						 select new
						 {
							 Integer = Sql.AsSql(v.Integer / param),
							 Decimal = Sql.AsSql(v.Decimal / param),
							 Double = Sql.AsSql(v.Double / param),
						 })
						 .Single();

			Assert.Multiple(() =>
			{
				Assert.That(result.Integer, Is.EqualTo(Issue4469Table.Data[0].Integer / param));
				Assert.That(Math.Round(result.Decimal, 5), Is.EqualTo(Math.Round(Issue4469Table.Data[0].Decimal / param, 5)));
				Assert.That(result.Double, Is.EqualTo(Issue4469Table.Data[0].Double / param));
			});
		}

		[ActiveIssue(Configurations = [TestProvName.AllSQLite])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4469")]
		public void Issue4469Test2([DataSources] string context, [Values] bool inline)
		{
			if (context.IsAnyOf(TestProvName.AllFirebirdLess4) && !inline)
				Assert.Ignore("Hard-to-workaround overflow bug");

			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4469Table.Data);
			db.InlineParameters = inline;

			var param = 33m;

			var result = (from v in tb
						  select new
						  {
							  Integer = Sql.AsSql(v.Integer / param),
							  Decimal = Sql.AsSql(v.Decimal / param),
							  Double  = Sql.AsSql(v.Double / (double)param),
						  })
						 .Single();

			Assert.Multiple(() =>
			{
				Assert.That(Math.Round(result.Integer, 5), Is.EqualTo(Math.Round(Issue4469Table.Data[0].Integer / param, 5)));
				Assert.That(Math.Round(result.Decimal, 5), Is.EqualTo(Math.Round(Issue4469Table.Data[0].Decimal / param, 5)));
				Assert.That(result.Double, Is.EqualTo(Issue4469Table.Data[0].Double / (double)param));
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4469")]
		public void Issue4469Test3([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue4469Table.Data);
			db.InlineParameters = inline;

			var param = 33D;

			var result = (from v in tb
						  select new
						  {
							  Integer = Sql.AsSql(v.Integer / param),
							  Decimal = Sql.AsSql((double)v.Decimal / param),
							  Double = Sql.AsSql(v.Double / param),
						  })
						 .Single();

			Assert.Multiple(() =>
			{
				Assert.That(Math.Round(result.Integer, 5), Is.EqualTo(Math.Round(Issue4469Table.Data[0].Integer / param, 5)));
				Assert.That(Math.Round(result.Decimal, 5), Is.EqualTo(Math.Round((double)Issue4469Table.Data[0].Decimal / param, 5)));
				Assert.That(Math.Round(result.Double, 5), Is.EqualTo(Math.Round(Issue4469Table.Data[0].Double / param, 5)));
			});
		}

		sealed class Issue4469Table
		{
			public int Integer { get; set; }
			[Column(Precision = 10, Scale = 5)] public decimal Decimal { get; set; }
			public double Double { get; set; }

			public static readonly Issue4469Table[] Data =
			[
				new Issue4469Table() { Integer = 100, Decimal = 100m, Double = 100.0 },
			];
		}
		#endregion

	}
}
