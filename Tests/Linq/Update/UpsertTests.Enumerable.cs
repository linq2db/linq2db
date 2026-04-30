using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// End-to-end Upsert scenarios for the bulk overload
	/// <c>Upsert&lt;T&gt;(ITable&lt;T&gt;, IEnumerable&lt;T&gt;, configure)</c>.
	/// Sources are client-side lists / arrays; the builder lowers to MERGE.
	/// </summary>
	public partial class UpsertTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Enumerable_Upsert([InsertOrUpdateDataSources] string context)
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

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Enumerable_Update_Set_UsesBothTargetAndSource([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "seed", Version = 10 } });

			var payload = new[]
			{
				new UpsertRow { Id = 1, Name = "inc", Version = 3  }, // update: 10 + 3 = 13
				new UpsertRow { Id = 2, Name = "new", Version = 7  }, // insert: just 7
			};

			// UPDATE setter references BOTH target and source — exercises the (t, s) Set overload
			// through the bulk-MERGE path.
			table.Upsert(payload, u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.Set(x => x.Version, (t, s) => t.Version + s.Version)));

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length    .ShouldBe(2);
			rows[0].Version.ShouldBe(13);
			rows[1].Version.ShouldBe(7);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public async Task Enumerable_Async_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new UpsertRow { Id = 1, Name = "old", Version = 1 } });

			var payload = new[]
			{
				new UpsertRow { Id = 1, Name = "one", Version = 2 },
				new UpsertRow { Id = 2, Name = "two", Version = 1 },
			};

			await table.UpsertAsync(payload, u => u.Match((t, s) => t.Id == s.Id));

			var rows = table.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("one");
			rows[1].Name.ShouldBe("two");
		}
	}
}
