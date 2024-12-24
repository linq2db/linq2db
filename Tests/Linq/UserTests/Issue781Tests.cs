using System;
using System.Linq;

using LinqToDB;

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
			using (var db = GetDataConnection(context))
			{
				var actual   = db.GetTable<Person>()
					.GroupBy(_ => Sql.Concat("test", _.Patient!.Diagnosis))
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : Sql.Concat("test", _.Patient.Diagnosis))
					.Count();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestLongCount([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual    = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient!.Diagnosis)
					.LongCount();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestHavingCount([DataSources(false, TestProvName.AllAccess, TestProvName.AllOracle, TestProvName.AllSybase, TestProvName.AllMySql, ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Having(_ => _.Key != null)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Count();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestHavingLongCount([DataSources(false, TestProvName.AllAccess, TestProvName.AllOracle, TestProvName.AllSybase, TestProvName.AllMySql, ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Having(_ => _.Key != null)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.LongCount();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestCountWithSelect([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Select(_ => _.Key)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.Count();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestLongCountWithSelect([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Select(_ => _.Key)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Select(_ => _.Key)
					.LongCount();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestHavingCountWithSelect([DataSources(false, TestProvName.AllAccess, TestProvName.AllOracle, TestProvName.AllSybase, TestProvName.AllMySql, ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.Count();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Select(_ => _.Key)
					.Count();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}

		[Test]
		public void TestHavingLongCountWithSelect([DataSources(false,
				TestProvName.AllAccess, TestProvName.AllOracle, TestProvName.AllSybase,
				TestProvName.AllMySql, ProviderName.SqlCe)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient!.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.LongCount();

				var expected = Person
					.GroupBy(_ => _.Patient == null ? null : "test" + _.Patient.Diagnosis)
					.Where(_ => _.Key != null)
					.Select(_ => _.Key)
					.LongCount();

				Assert.Multiple(() =>
				{
					Assert.That(actual, Is.EqualTo(expected));
					Assert.That(db.LastQuery!.IndexOf("COUNT", StringComparison.OrdinalIgnoreCase), Is.Not.EqualTo(-1));
				});
			}
		}
	}
}
