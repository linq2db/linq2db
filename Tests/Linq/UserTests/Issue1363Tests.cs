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
			[Column("required_field")] public Guid  Required { get; set; }
			[Column("optional_field")] public Guid? Optional { get; set; }
		}

		[ActiveIssue("Unsupported INSERT syntax", Configurations = new[]
		{
			TestProvName.AllAccess,
			ProviderName.SqlCe,
			TestProvName.AllSybase,
		})]
		[Test]
		public void TestInsert([DataSources(TestProvName.AllSqlServer2005, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
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
}
