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
	/// <c>Upsert&lt;T&gt;(ITable&lt;T&gt;, IQueryable&lt;T&gt;, configure)</c>.
	/// Source is a server-side query; the builder lowers to MERGE with the source as a sub-select.
	/// </summary>
	public partial class UpsertTests
	{
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Queryable_Upsert([InsertOrUpdateDataSources] string context)
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

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Queryable_Update_Set_UsesBothTargetAndSource([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);

			using var target = db.CreateLocalTable("UpsertTest",   new[] { new UpsertRow { Id = 1, Name = "seed", Version = 10 } });
			using var source = db.CreateLocalTable("UpsertSource", new[]
			{
				new UpsertRow { Id = 1, Name = "inc", Version = 3 },   // update: 10 + 3 = 13
				new UpsertRow { Id = 2, Name = "new", Version = 7 },   // insert: 7
			});

			// UPDATE setter pulls from target + source — exercises the (t, s) Set overload through
			// the bulk-MERGE path with a server-side (IQueryable) source.
			target.Upsert(source.AsQueryable(), u => u
				.Match((t, s) => t.Id == s.Id)
				.Update(v => v.Set(x => x.Version, (t, s) => t.Version + s.Version)));

			var rows = target.OrderBy(r => r.Id).ToArray();
			rows.Length    .ShouldBe(2);
			rows[0].Version.ShouldBe(13);
			rows[1].Version.ShouldBe(7);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public async Task Queryable_Async_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);

			using var target = db.CreateLocalTable("UpsertTest",   new[] { new UpsertRow { Id = 1, Name = "existing", Version = 1 } });
			using var source = db.CreateLocalTable("UpsertSource", new[]
			{
				new UpsertRow { Id = 1, Name = "from-source-1", Version = 10 },
				new UpsertRow { Id = 2, Name = "from-source-2", Version = 1  },
			});

			await target.UpsertAsync(source.AsQueryable(), u => u.Match((t, s) => t.Id == s.Id));

			var rows = target.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("from-source-1");
			rows[1].Name.ShouldBe("from-source-2");
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public void Queryable_Mirror_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);

			using var target = db.CreateLocalTable("UpsertTest",   new[] { new UpsertRow { Id = 1, Name = "existing", Version = 1 } });
			using var source = db.CreateLocalTable("UpsertSource", new[]
			{
				new UpsertRow { Id = 1, Name = "from-source-1", Version = 10 },
				new UpsertRow { Id = 2, Name = "from-source-2", Version = 1  },
			});

			// Mirror overload: source.Upsert(target, configure) — receiver/argument swapped.
			source.AsQueryable().Upsert(target, u => u.Match((t, s) => t.Id == s.Id));

			var rows = target.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("from-source-1");
			rows[1].Name.ShouldBe("from-source-2");
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException),
			TestProvName.AllSapHana, TestProvName.AllSqlServer2005, TestProvName.AllSQLite, TestProvName.AllPostgreSQL14Minus,
			TestProvName.AllMySql, TestProvName.AllSqlCe, TestProvName.AllAccess,
			ErrorMessage = ErrorHelper.Error_Upsert_MergeLowering_NotSupported)]
		public async Task Queryable_Mirror_Async_Upsert([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);

			using var target = db.CreateLocalTable("UpsertTest",   new[] { new UpsertRow { Id = 1, Name = "existing", Version = 1 } });
			using var source = db.CreateLocalTable("UpsertSource", new[]
			{
				new UpsertRow { Id = 1, Name = "from-source-1", Version = 10 },
				new UpsertRow { Id = 2, Name = "from-source-2", Version = 1  },
			});

			// Async mirror overload.
			await source.AsQueryable().UpsertAsync(target, u => u.Match((t, s) => t.Id == s.Id));

			var rows = target.OrderBy(r => r.Id).ToArray();
			rows.Length .ShouldBe(2);
			rows[0].Name.ShouldBe("from-source-1");
			rows[1].Name.ShouldBe("from-source-2");
		}
	}
}
