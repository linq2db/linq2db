using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1982Tests : TestBase
	{
		public class Issue1982Table
		{
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Int64)]
			public TimeSpan Time { get; set; }

			public DateTime DateTime { get; set; }
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Issue1982Table>())
			{
				db.GetTable<Issue1982Table>()
					.Where(_ => _.Time < _.DateTime.TimeOfDay)
					.Any();
			}
		}
	}
}
