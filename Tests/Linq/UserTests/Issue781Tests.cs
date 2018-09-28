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
		[Test, DataContextSource(false)]
		public void TestCount(string context)
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

		[Test, DataContextSource(false)]
		public void TestLongCount(string context)
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

		[Test, DataContextSource(false, ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged, ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.MySql57)]
		public void TestHavingCount(string context)
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

		[Test, DataContextSource(false, ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged, ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.MySql57)]
		public void TestHavingLongCount(string context)
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

		[Test, DataContextSource(false)]
		public void TestCountWithSelect(string context)
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

		[Test, DataContextSource(false)]
		public void TestLongCountWithSelect(string context)
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

		[Test, DataContextSource(false, ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged, ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.MySql57)]
		public void TestHavingCountWithSelect(string context)
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

		[Test, DataContextSource(false, ProviderName.Access, TestProvName.MariaDB, ProviderName.OracleManaged, ProviderName.OracleNative, ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.MySql57)]
		public void TestHavingLongCountWithSelect(string context)
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
