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

		// Not attributed to #1363: that is what the fixture tests, not why this gate exists. #1363 is "Use of null
		// value for parameter of subquery makes it ignore non-null values on next calls", which is unrelated to
		// the INSERT syntax these providers reject.
		[ActiveIssue(Configuration = TestProvName.AllSybase, ErrorTypeName = "AdoNetCore.AseClient.AseException",
			ErrorMessage = "The name 'required_field' is illegal in this context",
			Details = "no-issue: Sybase rejects a column reference where it wants a constant. SqlCe was dropped from this gate - it passes, direct and remote.")]
		[ActiveIssue(Configuration = TestProvName.AllAccess, ErrorMessage = "Query input must contain at least one table or query",
			Details = "no-issue: both ACE drivers reject it with the same sentence, so one fragment covers OleDb and ODBC alike.")]
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
