using System;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Extensions
{
	[TestFixture]
	public class OracleForUpdateExecutionTests : TestBase
	{
		[Table("OracleRowLocks")]
		sealed class LockRow
		{
			[PrimaryKey, Column("LockId")] public int Id    { get; set; }
			[Column]                     public int Value { get; set; }
		}

		static LockRow[] Rows() => Enumerable.Range(1, 5)
			.Select(i => new LockRow { Id = i, Value = i * 10 })
			.ToArray();

		// Oracle-specific locking syntax and driver behavior require an Oracle server.
		[Test]
		public void LockingModes([IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values("Update", "NoWait", "SkipLocked", "Wait")] string mode, [Values] bool columns)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable(Rows());
			using var transaction = db.BeginTransaction();
			var source = table
				.AsQueryable()
				.AsOracle();
			var targets = columns
				? new System.Linq.Expressions.Expression<Func<LockRow, object?>>[] { r => r.Id, r => r.Value }
				: Array.Empty<System.Linq.Expressions.Expression<Func<LockRow, object?>>>();
			var query = mode switch
			{
				"Update"     => source.ForUpdateHint(targets),
				"NoWait"     => source.ForUpdateNoWaitHint(targets),
				"SkipLocked" => source.ForUpdateSkipLockedHint(targets),
				"Wait"       => source.ForUpdateWaitHint(1, targets),
				_            => throw new ArgumentOutOfRangeException(nameof(mode)),
			};

			var rows = query.ToArray();
			rows.Length.ShouldBe(5);
			LastQuery
				.ShouldNotBeNull()
				.ShouldContain("FOR UPDATE");
			if (columns)
				LastQuery
					.ShouldNotBeNull()
					.ShouldContain("\"LockId\"");
		}

		// Both Oracle dialects must support the implicit limits used by these terminal operators.
		[Test]
		public void LimitedLockingQuery([IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values("First", "Single", "FirstOrDefault", "SingleOrDefault", "Take")] string operation)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable(Rows());
			using var transaction = db.BeginTransaction();
			var source = table
				.AsQueryable()
				.AsOracle()
				.ForUpdateHint()
				.Where(r => r.Id == 1);
			var row = operation switch
			{
				"First"           => source.First(),
				"Single"          => source.Single(),
				"FirstOrDefault"  => source.FirstOrDefault(),
				"SingleOrDefault" => source.SingleOrDefault(),
				"Take"            => source
					.Take(1)
					.ToArray()[0],
				_                 => throw new ArgumentOutOfRangeException(nameof(operation)),
			};

			row
				.ShouldNotBeNull()
				.Value
				.ShouldBe(10);
			LastQuery
				.ShouldNotBeNull()
				.ShouldContain("FOR UPDATE");
			LastQuery
				.ShouldNotBeNull()
				.ShouldContain("ROWNUM");
			LastQuery
				.ShouldNotBeNull()
				.ShouldNotContain("FETCH NEXT");
		}

		// OF must select a qualified, mapped column when both joined tables have LockId.
		[Test]
		public void QualifiedJoinColumns([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable(Rows());
			using var other = db.CreateLocalTable("OracleRowLocksOther", Rows());
			using var transaction = db.BeginTransaction();
			var query = (from left in table
			             join right in other on left.Id equals right.Id
			             select new { left, right })
				.AsOracle()
				.ForUpdateHint(r => r.left.Id);

			var rows = query.ToArray();
			rows.Length.ShouldBe(5);
			LastQuery
				.ShouldNotBeNull()
				.ShouldContain("FOR UPDATE OF ");
			LastQuery
				.ShouldNotBeNull()
				.ShouldContain("JOIN");
			var sql = LastQuery.ShouldNotBeNull();
			var leftAlias = Regex.Match(sql, "\"OracleRowLocks\"\\s+([A-Za-z0-9_]+)").Groups[1].Value;
			leftAlias.Length.ShouldBeGreaterThan(0);
			sql.ShouldContain($"FOR UPDATE OF {leftAlias}.\"LockId\"");
		}

		// Root DML obtains its own row locks and cannot carry a SELECT locking suffix.
		[Test]
		public void DmlOverLockedSource([IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values] bool generic)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable(Rows());
			var source = generic ? table
				.SubQueryHint("FOR UPDATE") : table
				.AsQueryable()
				.AsOracle()
				.ForUpdateHint();

			var updated = source
				.Where(r => r.Id == 1)
				.Set(r => r.Value, 42)
				.Update();
			updated.ShouldBe(1);
			LastQuery
				.ShouldNotBeNull()
				.ShouldNotContain("FOR UPDATE");
			var inserted = source
				.Where(r => r.Id == 1)
				.Insert(table, r => new LockRow { Id = r.Id + 10, Value = r.Value });
			inserted.ShouldBe(1);
			LastQuery
				.ShouldNotBeNull()
				.ShouldNotContain("FOR UPDATE");
			var deleted = source
				.Where(r => r.Id == 2)
				.Delete();
			deleted.ShouldBe(1);
			LastQuery
				.ShouldNotBeNull()
				.ShouldNotContain("FOR UPDATE");
		}

		// A missing locking clause makes the second query succeed and this assertion fail.
		[Test]
		public void NoWaitHonorsOtherTransaction([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var first = GetDataConnection(context);
			using var second = GetDataConnection(context);
			using var table = first.CreateLocalTable(Rows());
			using var firstTransaction = first.BeginTransaction();
			using var secondTransaction = second.BeginTransaction();

			var locked = table
				.Where(r => r.Id == 1)
				.AsOracle()
				.ForUpdateHint()
				.ToArray();
			locked.Length.ShouldBe(1);
			var contended = second
				.GetTable<LockRow>()
				.TableName(table.TableName)
				.Where(r => r.Id == 1).AsOracle().ForUpdateNoWaitHint();
			Assert.That(() => contended.ToArray(), Throws.Exception.With.Message.Contains("ORA-00054"));
		}

		// Keep row 1 locked while the second session reads all remaining unlocked rows.
		[Test]
		public void SkipLockedHonorsOtherTransaction([IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values] bool generic)
		{
			using var first = GetDataConnection(context);
			using var second = GetDataConnection(context);
			using var table = first.CreateLocalTable(Rows());
			using var firstTransaction = first.BeginTransaction();
			using var secondTransaction = second.BeginTransaction();

			var locked = table
				.Where(r => r.Id == 1)
				.AsOracle()
				.ForUpdateHint()
				.ToArray();
			locked.Length.ShouldBe(1);
			var other = second
				.GetTable<LockRow>()
				.TableName(table.TableName);
			var source = generic ? other
				.SubQueryHint("FOR UPDATE SKIP LOCKED") : other
				.AsQueryable()
				.AsOracle()
				.ForUpdateSkipLockedHint();
			Assert.That(() => source.Take(2).ToArray(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("SKIP LOCKED"));
			Assert.That(() => source.First(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("SKIP LOCKED"));
			Assert.That(() => source.FirstOrDefault(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("SKIP LOCKED"));
			Assert.That(() => source.Single(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("SKIP LOCKED"));
			Assert.That(() => source.SingleOrDefault(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("SKIP LOCKED"));
			var rows = source.ToArray();
			rows
				.Select(r => r.Id)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe(new[] { 2, 3, 4, 5 });
		}

		// Unsupported nesting must fail before invalid Oracle SQL is sent to the server.
		[Test]
		public void UnsupportedComposition([IncludeDataSources(TestProvName.AllOracle)] string context,
			[Values("Join", "Exists", "Cte", "Concat", "OrderedTake", "Skip")] string shape, [Values] bool generic)
		{
			using var db = GetDataConnection(context);
			using var table = db.CreateLocalTable(Rows());
			var locked = generic ? table
				.SubQueryHint("FOR UPDATE") : table
				.AsQueryable()
				.AsOracle()
				.ForUpdateHint();
			IQueryable<LockRow> query = shape switch
			{
				"Join"        => from x in locked.AsSubQuery() join y in table on x.Id equals y.Id select x,
				"Exists"      => table.Where(x => locked.Any(y => y.Id == x.Id)),
				"Cte"         => locked.AsCte(),
				"Concat"      => locked.Concat(table),
				"OrderedTake" => locked
					.OrderBy(r => r.Id)
					.Take(1),
				"Skip"        => locked
					.Skip(1)
					.Take(1),
				_             => throw new ArgumentOutOfRangeException(nameof(shape)),
			};
			Assert.That(() => query.ToArray(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("FOR UPDATE"));
		}
	}
}
