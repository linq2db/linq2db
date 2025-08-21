using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue269Tests : TestBase
	{
		[AttributeUsage(AttributeTargets.Parameter)]
		sealed class TestDataContextSourceAttribute : DataSourcesAttribute
		{
			public static string[] Unsupported = new[]
			{
				TestProvName.AllAccess,
				TestProvName.AllSQLite,
				TestProvName.AllOracle,
				TestProvName.AllMySql,
				TestProvName.AllSybase,
				TestProvName.AllSqlServer,
				ProviderName.DB2,
				ProviderName.SqlCe,
				TestProvName.AllSapHana
			}.SelectMany(_ => _.Split(',')).ToArray();

			public TestDataContextSourceAttribute(params string[] except) : base(
				Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[Test]
		public void TestTake([TestDataContextSource(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Take(1)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Take(1)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test]
		public void TestDistinct([TestDataContextSource(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Distinct()
						.Any(_ => _.Contains("with")));

				// DISTINCT should be optimized out
				Assert.That(q.EnumQueries().All(x => !x.Select.IsDistinct), Is.True);

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Distinct()
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test]
		public void TestSkipDistinct([TestDataContextSource(TestProvName.AllClickHouse, ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Skip(0)
						.Distinct()
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Skip(0)
						.Distinct()
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test]
		public void TestDistinctSkip([TestDataContextSource(TestProvName.AllClickHouse, ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Distinct()
						.Skip(0)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Distinct()
						.Skip(0)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test]
		public void TestSkip([TestDataContextSource(TestProvName.AllClickHouse, ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Skip(0)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient!.Diagnosis)
						.Skip(0)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}
	}
}
