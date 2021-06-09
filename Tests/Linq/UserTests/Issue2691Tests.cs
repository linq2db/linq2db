using System.Data.Linq;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2691Tests : TestBase
	{
		[Table("Issue2691Table")]
		class IssueClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public byte[]? Data { get; set; }
			[Column]
			public Binary? DataB { get; set; }
		}

		[Test]
		public void TestBinaryLengthTranslation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			using (var table = db.CreateLocalTable<IssueClass>())
			{
				db.Insert(new IssueClass()
				{
					Id = 99,
					Data = new byte[] { 1, 2, 3, 4, 5 },
					DataB = new Binary(new byte[] { 1, 2, 3, 4, 5, 6 })
				});

				var qry1 = table.Select(x => Sql.Length(x.Data)).ToList();
				Assert.That(db.LastQuery!.ToLower().Contains("length("));

				var qry2 = table.Select(x => x.Data!.Length).ToList();
				Assert.That(db.LastQuery!.ToLower().Contains("length("));

				var qry3 = db.GetTable<IssueClass>().Select(x => Sql.Length(x.DataB)).ToList();
				Assert.That(db.LastQuery!.ToLower().Contains("length("));

				var qry4 = db.GetTable<IssueClass>().Select(x => x.DataB!.Length).ToList();
				Assert.That(db.LastQuery!.ToLower().Contains("length("));

				Assert.AreEqual(5, qry1[0]);
				Assert.AreEqual(5, qry2[0]);
				Assert.AreEqual(6, qry3[0]);
				Assert.AreEqual(6, qry4[0]);
			}
		}
	}
}
