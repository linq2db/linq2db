using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2647Tests : TestBase
	{
		[Table("Issue2647Table")]
		class IssueClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? LanguageId { get; set; }
			[Column]
			public string? Text { get; set; }
		}

		[Test]
		public void OrderBySybqueryTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qryUnsorted = db.GetTable<IssueClass>();
				var query = qryUnsorted.OrderBy(x => x.LanguageId);
				query = query.ThenByDescending(ss => db.GetTable<IssueClass>().Count(ss2 => ss2.Id == ss.Id) * 10000 /
				                                     db.GetTable<IssueClass>().Count(ss3 => ss3.Id == ss.Id));

				var sql = query.ToString();
				TestContext.WriteLine(sql);

				var selectQuery = query.GetSelectQuery();
				Assert.That(selectQuery.OrderBy.Items.Count, Is.EqualTo(2));
				Assert.That(selectQuery.OrderBy.Items[0].Expression.ElementType, Is.EqualTo(QueryElementType.SqlField));
			}
		}
	}
}
