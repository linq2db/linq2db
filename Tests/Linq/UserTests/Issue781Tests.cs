using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue781Tests : TestBase
	{
		[Test]
		public void TestCount([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual   = db.GetTable<Person>()
					.GroupBy(_ => Sql.Concat("test", _.Patient.Diagnosis))
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : Sql.Concat("test", _.Patient.Diagnosis))
					.Count();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestLongCount([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual    = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.LongCount();

				var expected = db.GetTable<Person>()
					.GroupBy(_ => Patient == null ? null : "test" + _.Patient.Diagnosis)
					.LongCount();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestHavingCount([DataSources(false,
			ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged,
			ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged,
			TestProvName.MySql57)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Count();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestHavingLongCount([DataSources(false,
			ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged,
			ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged,
			TestProvName.MySql57)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.LongCount();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestCountWithSelect([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.Count();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestLongCountWithSelect([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.LongCount();

				var expected = db.GetTable<Person>()
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.LongCount();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestHavingCountWithSelect([DataSources(false,
			ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged,
			ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged,
			TestProvName.MySql57)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Select(_ => _.Key)
					.Count();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}

		[Test]
		public void TestHavingLongCountWithSelect([DataSources(false,
			ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged,
			ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged,
			TestProvName.MySql57)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Select(_ => _.Key)
					.LongCount();

				Assert.AreEqual(expected, actual);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase) != -1);
			}
		}
	}
}
