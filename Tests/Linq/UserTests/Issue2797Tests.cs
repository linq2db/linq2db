using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2797Tests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int? Value { get; set; }
		}

		[Test]
		public void TestCaseGeneration([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				table.Select(e => Sql.AsSql(Sql.Between(e.Value, 2, 5) ? 0 : 1))
					.ToList(); 
				Assert.That(db.LastQuery, Does.Not.Contain(" = "));
			}
		}
	}
}
