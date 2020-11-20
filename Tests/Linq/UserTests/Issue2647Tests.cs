using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2647Tests : TestBase
	{
		class IssueClass
		{
			public int Id { get; set; }
			public string? LanguageId { get; set; }
			public string? Text { get; set; }
		}

		[Test]
		public void Test2647([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<IssueClass>()
				.HasTableName("Issue2647Table")
				.Property(e => e.Id).IsPrimaryKey();


			using (var db = GetDataContext(context, ms))
			{
				db.DropTable<IssueClass>(throwExceptionIfNotExists: false);
				db.CreateTable<IssueClass>();
				{
					var qryUnsorted = from tt in db.GetTable<IssueClass>() select tt;
					// sort language id 
					var qry2 = qryUnsorted.OrderBy(x => x.LanguageId);
					//after that sort with a complex sub-select 
					qry2 = qry2.ThenByDescending(ss => db.GetTable<IssueClass>().Count(ss2 => ss2.Id == ss.Id) * 10000 /
														db.GetTable<IssueClass>().Count(ss3 => ss3.Id == ss.Id));
					var ll = qry2.Select(x=>x.Text).ToList();
					var sql = ((DataConnection)db).LastQuery;
					Assert.IsNotNull(sql);
					Assert.Greater(sql.IndexOf("TextId", StringComparison.OrdinalIgnoreCase), sql.IndexOf("LanguageId", StringComparison.OrdinalIgnoreCase), "error in order by order");
				}
			}
		}
	}
}
