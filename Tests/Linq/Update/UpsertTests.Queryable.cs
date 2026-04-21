using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end Upsert scenarios for the bulk overload
	/// <c>Upsert&lt;TTarget, TSource&gt;(ITable&lt;TTarget&gt;, IQueryable&lt;TSource&gt;, configure)</c>.
	/// Source is a server-side query; the builder lowers to MERGE with the source as a sub-select.
	/// </summary>
	public partial class UpsertTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSapHana, ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void BulkQueryable_Upsert([InsertOrUpdateDataSources(
				// Non-MERGE providers + Oracle excluded — Phase 5.
				TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus, TestProvName.AllMySql,
				TestProvName.AllMariaDB, TestProvName.AllSqlCe, TestProvName.AllSybase, TestProvName.AllAccess,
				TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			using var target = db.CreateLocalTable("UpsertTest",   new[] { new UpsertRow { Id = 1, Name = "existing", Version = 1 } });
			using var source = db.CreateLocalTable("UpsertSource", new[]
			{
				new UpsertRow { Id = 1, Name = "from-source-1", Version = 10 },    // update
				new UpsertRow { Id = 2, Name = "from-source-2", Version = 1  },    // insert
			});

			// Use the source table as an IQueryable source for the Upsert.
			target.Upsert(source.AsQueryable(), u => u.Match((t, s) => t.Id == s.Id));

			var rows = target.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("from-source-1");
			rows[1].Name.ShouldBe("from-source-2");
		}
	}
}
