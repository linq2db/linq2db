using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4389Tests : TestBase
	{
		[Test]
		public async Task Test([IncludeDataSources(ProviderName.ClickHouseDriver)] string context)
		{
			await using var db  = GetDataContext(context);

			await using var tmp = db.CreateTempTable("UniqueIdTmp",
				[ new { ID = 1 } ],
				fm => fm.Property(p => p.ID).IsNotNull());

			await tmp.BulkCopyAsync(new[] { new { ID = 2 } });

			await tmp.CountAsync();
		}
	}
}
