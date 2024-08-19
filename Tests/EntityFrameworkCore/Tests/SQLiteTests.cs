using System.Linq;

using LinqToDB.DataProvider.SQLite;

using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public class SQLiteTests : NorthwindContextTestBase
	{
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/343")]
		public void TestFunctionsMapping([EFIncludeDataSources(ProviderName.SQLiteMS)] string provider)
		{
			using var ctx = CreateContext(provider, o => o.UseSQLite(SQLiteProvider.Microsoft));

			ctx.Categories.ToLinqToDB().ToList();
		}
	}
}
