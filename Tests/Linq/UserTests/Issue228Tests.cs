using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue228Tests : TestBase
	{
		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/37999", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void Test([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var cnt = db.DataProvider.SqlProviderFlags.MaxInListValuesCount;

				try
				{
					db.DataProvider.SqlProviderFlags.MaxInListValuesCount = 1;

					var ids = new[] {1, 2};
					AreEqual(
						GetTypes(context).Where(_ => !ids.Contains(_.ID)),
						db.Types.         Where(_ => !ids.Contains(_.ID)));

				}
				finally
				{
					db.DataProvider.SqlProviderFlags.MaxInListValuesCount = cnt;
				}
			}
		}
	}
}
