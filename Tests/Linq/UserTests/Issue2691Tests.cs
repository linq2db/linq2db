using System.Data.Linq;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2691Tests : TestBase
	{
		[Table("Issue2691Table")]
		sealed class IssueClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public byte[]? Data { get; set; }
			[Column]
			public Binary? DataB { get; set; }
		}

		[Test]
		public void TestBinaryLengthTranslation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
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
				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Contain("length("));

				var qry2 = table.Select(x => x.Data!.Length).ToList();
				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Contain("length("));

				var qry3 = db.GetTable<IssueClass>().Select(x => Sql.Length(x.DataB)).ToList();
				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Contain("length("));

				var qry4 = db.GetTable<IssueClass>().Select(x => x.DataB!.Length).ToList();
				Assert.Multiple(() =>
				{
					Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Contain("length("));

					Assert.That(qry1[0], Is.EqualTo(5));
					Assert.That(qry2[0], Is.EqualTo(5));
					Assert.That(qry3[0], Is.EqualTo(6));
					Assert.That(qry4[0], Is.EqualTo(6));
				});
			}
		}
	}
}
