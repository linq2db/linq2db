using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2816Tests : TestBase
	{
		[Table("Issue2816Table")]
		class TestClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? Text { get; set; }
		}

		[Test]
		public void Issue2816Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (var table = db.CreateLocalTable<TestClass>())
				{
					db.Insert(new TestClass() { Id = 0, Text = "a" });
					db.Insert(new TestClass() { Id = 1, Text = null });
					db.Insert(new TestClass() { Id = 2, Text = " " });
					db.Insert(new TestClass() { Id = 3, Text = "  " });
					db.Insert(new TestClass() { Id = 4, Text = " m" });

					var query = from p in table
								where string.IsNullOrWhiteSpace(p.Text)
								select p;

					var res = query.ToList();

					var sql = ((DataConnection) db).LastQuery;

					Assert.AreEqual(res.Count, 3);
				}
			}
		}
	}
}
