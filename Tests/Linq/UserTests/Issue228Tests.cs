using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SQLite;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue228Tests : TestBase
	{
		// A dedicated SQLite provider whose IN list limit is preset to one value. Using a separate provider
		// instance keeps the limit off the shared per-provider SqlProviderFlags singleton, which a parallel
		// test run would otherwise observe (each provider instance owns its own SqlProviderFlags).
		sealed class LimitedInListSQLiteProvider : SQLiteDataProvider
		{
			public LimitedInListSQLiteProvider(string name, SQLiteProvider provider, int maxInListValuesCount)
				: base(name, provider)
			{
				SqlProviderFlags.MaxInListValuesCount = maxInListValuesCount;
			}
		}

		// IN list splitting lives in BasicSqlBuilder and is provider-agnostic, so one provider covers it.
		[Test]
		public void Test([IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			var sqliteProvider = context == ProviderName.SQLiteClassic ? SQLiteProvider.System : SQLiteProvider.Microsoft;
			var dataProvider   = new LimitedInListSQLiteProvider(context + ".MaxInList1", sqliteProvider, maxInListValuesCount: 1);
			var options        = new DataOptions().UseConnectionString(dataProvider, GetConnectionString(context));

			using var db = GetDataConnection(options);

			var ids = new[] { 1, 2 };
			AreEqual(
				GetTypes(context).Where(_ => !ids.Contains(_.ID)),
				db.Types.Where(_ => !ids.Contains(_.ID)));

			// the split list is semantically identical to a single one, so the results alone cannot tell whether
			// the limit was applied at all
			db.LastQuery!.ShouldContain("NOT IN (1) AND");
			db.LastQuery!.ShouldContain("NOT IN (2)");
		}
	}
}
