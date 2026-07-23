using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1363Tests : TestBase
	{
		[Table("Issue1363")]
		public sealed class Issue1363Record
		{
			[PrimaryKey, Column("required_field")] public Guid  Required { get; set; }
			[Column("optional_field")] public Guid? Optional { get; set; }
		}

		// Firebird 6: the INSERT setter's scalar Guid subquery is read-wrapped in UUID_TO_CHAR (the charset-safe
		// Guid read) and then written back into the BINARY(16) column — "string right truncation, expected
		// length 16, actual 36". The wrap is (re)applied when the subquery is converted at SQL-build time, where
		// the modification-statement guard that fixes the MERGE/INSERT-SELECT cases cannot reach it.
		// FB6 prerelease 6.0.0.2068 / FbClient 10.3.4; re-check when a newer Firebird 6 is released.
		[ActiveIssue("Unsupported INSERT syntax", Configurations = new[]
		{
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSybase,
			ProviderName.Firebird6,
		})]
		[Test]
		public void TestInsert([DataSources(TestProvName.AllSqlServer2005, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl = db.CreateLocalTable<Issue1363Record>();
			var id1 = TestData.Guid1;
			var id2 = TestData.Guid2;

			insert(id1, null);
			insert(id2, id1);

			var record = tbl.Where(_ => _.Required == id2).Single();
			Assert.That(record.Optional, Is.EqualTo(id1));

			void insert(Guid id, Guid? testId)
			{
				tbl.Insert(() => new Issue1363Record()
				{
					Required = id,
					Optional = tbl.Where(_ => _.Required == testId).Select(_ => (Guid?)_.Required).SingleOrDefault()
				});
			}
		}
	}
}
