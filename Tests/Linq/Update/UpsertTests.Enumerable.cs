using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end Upsert scenarios for the bulk overload
	/// <c>Upsert&lt;TTarget, TSource&gt;(ITable&lt;TTarget&gt;, IEnumerable&lt;TSource&gt;, configure)</c>.
	/// Sources are client-side lists / arrays; the builder lowers to MERGE.
	/// </summary>
	public partial class UpsertTests
	{
		[Test]
		public void E2E_BulkEnumerable_Upsert([InsertOrUpdateDataSources(
				// Non-MERGE providers + Oracle (WHERE-placement quirk) excluded — Phase 5.
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "old", Version = 1 } });

			var payload = new[]
			{
				new UpsertRow { Id = 1, Name = "one", Version = 2 },   // update existing
				new UpsertRow { Id = 2, Name = "two", Version = 1 },   // insert new
			};

			table.Upsert(payload, u => u.Match((t, s) => t.Id == s.Id));

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("one");
			rows[1].Name.ShouldBe("two");
		}
	}
}
