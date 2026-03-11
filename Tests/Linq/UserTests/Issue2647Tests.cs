using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2647Tests : TestBase
	{
		[Table("Issue2647Table")]
		sealed class IssueClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? LanguageId { get; set; }
			[Column]
			public string? Text { get; set; }
		}

		[Test]
		public void OrderBySubqueryTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<IssueClass>();

			var query = db.GetTable<IssueClass>()
				.OrderBy(x => x.LanguageId)
				.ThenByDescending(ss => 
					db.GetTable<IssueClass>().Count(ss2 => ss2.Id == ss.Id) * 10000 / db.GetTable<IssueClass>().Count(ss3 => ss3.Id == ss.Id)
				);

			query.ToList();

			var selectQuery = query.GetSelectQuery();
			Assert.That(selectQuery.OrderBy.Items, Has.Count.EqualTo(2));
			Assert.That(selectQuery.OrderBy.Items[0].Expression.ElementType, Is.EqualTo(QueryElementType.Column));
		}
	}
}
