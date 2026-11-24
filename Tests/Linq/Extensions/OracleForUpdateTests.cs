using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Extensions
{
	[TestFixture]
	public class OracleForUpdateTests
	{
		[Table("LockRows")]
		sealed class LockRow
		{
			[Column("LockId")] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		static DataConnection CreateConnection(OracleVersion version)
		{
			return new DataConnection(new DataOptions().UseConnectionString(
				OracleTools.GetDataProvider(version, OracleProvider.Managed), "Data Source=unused"));
		}

		[TestCase(OracleVersion.v11)]
		[TestCase(OracleVersion.v12)]
		public void GenericSubQueryHint(OracleVersion version)
		{
			using var db = CreateConnection(version);

			var sql = db.GetTable<LockRow>()
				.Where(r => r.Id > 0)
				.OrderBy(r => r.Id)
				.SubQueryHint("FOR UPDATE")
				.ToSqlQuery().Sql;

			sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE");
			sql.IndexOf("FOR UPDATE", System.StringComparison.Ordinal)
				.ShouldBeGreaterThan(sql.IndexOf("ORDER BY", System.StringComparison.Ordinal));
		}

		[Test]
		public void ForUpdateOverloads([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var source = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle();
			source
				.ForUpdateHint()
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith(OracleHints.ForUpdate);
			source
				.ForUpdateNoWaitHint()
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE NOWAIT");
			source
				.ForUpdateSkipLockedHint()
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE SKIP LOCKED");
			source
				.ForUpdateWaitHint(5)
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE WAIT 5");
			var sql = source
				.ForUpdateHint(r => r.Id, r => r.Value)
				.ToSqlQuery()
				.Sql;
			sql.ShouldMatch("FOR UPDATE OF [A-Za-z0-9_]+\\.\"LockId\", [A-Za-z0-9_]+\\.\"Value\"");
			var joined = (
				from left in db.GetTable<LockRow>()
				join right in db.GetTable<LockRow>() on left.Id equals right.Id
				select new { left, right })
				.AsOracle()
				.ForUpdateNoWaitHint(r => r.left.Id, r => r.right.Id);
			var joinSql = joined.ToSqlQuery().Sql;
			var match = Regex.Match(joinSql, "FOR UPDATE OF ([A-Za-z0-9_]+)\\.\"LockId\", ([A-Za-z0-9_]+)\\.\"LockId\" NOWAIT");
			match.Success.ShouldBeTrue(joinSql);
			match.Groups[1].Value.ShouldNotBe(match.Groups[2].Value);
			Assert.That(() => source.ForUpdateHint(r => r.Id + 1).ToSqlQuery(), Throws.TypeOf<LinqToDBException>());
			Assert.That(() => source.ForUpdateHint().ForUpdateNoWaitHint().ToSqlQuery(), Throws.TypeOf<LinqToDBException>());
		}

		[Test]
		public void ExpressionOverloads([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var source = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle();
			Expression<Func<LockRow, object?>>[][] targets =
			{
				new Expression<Func<LockRow, object?>>[] { r => r.Id },
				new Expression<Func<LockRow, object?>>[] { r => r.Value },
				new Expression<Func<LockRow, object?>>[] { r => r.Id, r => r.Value },
			};
			var physicalColumns = new[] { "LockId", "Value", "LockId" };
			for (var i = 0; i < targets.Length; i++)
			{
				var columns = targets[i];
				Expression<Func<IOracleSpecificQueryable<LockRow>>>[] expressions =
				{
					() => source.ForUpdateHint(columns),
					() => source.ForUpdateNoWaitHint(columns),
					() => source.ForUpdateSkipLockedHint(columns),
					() => source.ForUpdateWaitHint(5, columns),
					() => db
						.GetTable<LockRow>()
						.Where(r => r.Id > 0).AsOracle().ForUpdateHint(columns),
				};
				foreach (var expression in expressions)
				{
					var sql = source
						.Provider
						.CreateQuery<LockRow>(expression.Body)
						.ToSqlQuery()
						.Sql;
					sql.ShouldContain("FOR UPDATE OF ");
					var suffix = sql.Substring(sql.IndexOf("FOR UPDATE", StringComparison.Ordinal));
					suffix.ShouldContain($".\"{physicalColumns[i]}\"");
					if (i == 1)
						suffix.ShouldNotContain("\"LockId\"");
				}
			}
		}

		[Test]
		public void QueryDependentArguments([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var source = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle();
			foreach (var seconds in new[] { 0, 5, 10 })
			{
				Expression<Func<IOracleSpecificQueryable<LockRow>>> expression = () => source.ForUpdateWaitHint(seconds, r => r.Id);
				var sql = source
					.Provider
					.CreateQuery<LockRow>(expression.Body)
					.ToSqlQuery()
					.Sql;
				sql
					.TrimEnd()
					.ShouldEndWith($"WAIT {seconds}");
			}

			var negative = -1;
			Expression<Func<IOracleSpecificQueryable<LockRow>>> invalid = () => source.ForUpdateWaitHint(negative);
			Assert.That(() => source.Provider.CreateQuery<LockRow>(invalid.Body).ToSqlQuery(), Throws.TypeOf<ArgumentOutOfRangeException>());
			Assert.That(() => source.ForUpdateWaitHint(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void CompiledLockingQuery([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var compiled = CompiledQuery.Compile((DataConnection connection, int seconds) => connection
				.GetTable<LockRow>().AsQueryable().AsOracle().ForUpdateWaitHint(seconds, r => r.Id).Where(r => r.Value >= 0));
			foreach (var seconds in new[] { 0, 5, 10 })
			{
				var sql = compiled(db, seconds)
					.ToSqlQuery()
					.Sql;
				sql
					.TrimEnd()
					.ShouldEndWith($"WAIT {seconds}");
			}
		}
		[Test]
		public void OptimizerHints([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var source = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle()
				.QueryHint(OracleHints.Hint.AllRows);
			var plainSql = source.ToSqlQuery().Sql;
			plainSql.ShouldContain("SELECT /*+ ALL_ROWS */");
			plainSql.ShouldNotContain("FOR UPDATE");

			var lockingSql = source
				.ForUpdateHint()
				.ToSqlQuery()
				.Sql;
			lockingSql.ShouldContain("SELECT /*+ ALL_ROWS */");
			lockingSql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE");

			var nestedSql = db.GetTable<LockRow>()
				.Where(r => source.AsSubQuery().Any(s => s.Id == r.Id))
				.ToSqlQuery().Sql;
			nestedSql.ShouldContain("ALL_ROWS");
			nestedSql.ShouldNotContain("FOR UPDATE");
		}

		[Test]
		public void RejectUnsupportedShapes(
			[Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version,
			[Values("Join", "Exists", "Cte", "Concat", "Union", "Distinct", "DistinctBeforeHint", "OrderedTake", "OrderedTakeBeforeHint", "Skip", "SkipBeforeHint", "Group")] string shape)
		{
			using var db = CreateConnection(version);
			var table = db.GetTable<LockRow>();
			var locked = table.SubQueryHint("FOR UPDATE");
			IQueryable<LockRow> query = shape switch
			{
				"Join"        => from x in locked.AsSubQuery() join y in table on x.Id equals y.Id select x,
				"Exists"      => table.Where(x => locked.Any(y => y.Id == x.Id)),
				"Cte"         => locked.AsCte(),
				"Concat"      => locked.Concat(table),
				"Union"       => locked.Union(table),
				"Distinct"    => locked.Distinct(),
				"DistinctBeforeHint" => table
					.Distinct()
					.SubQueryHint("FOR UPDATE"),
				"OrderedTake" => locked
					.OrderBy(x => x.Id)
					.Take(1),
				"OrderedTakeBeforeHint" => table
					.OrderBy(x => x.Id)
					.Take(1)
					.SubQueryHint("FOR UPDATE"),
				"Skip"        => locked
					.Skip(1)
					.Take(1),
				"SkipBeforeHint" => table
					.Skip(1)
					.Take(1)
					.SubQueryHint("FOR UPDATE"),
				"Group"       => locked
					.GroupBy(r => r.Id)
					.Select(g => new LockRow { Id = g.Key, Value = g.Count() }),
				_             => throw new System.ArgumentOutOfRangeException(nameof(shape)),
			};

			Assert.That(() => query.ToSqlQuery(), Throws.TypeOf<LinqToDBException>().With.Message.Contains("FOR UPDATE"));
		}

		[Test]
		public void SimpleLockedTake([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var sql = db.GetTable<LockRow>()
				.Where(x => x.Id == 1 || x.Value == 2)
				.SubQueryHint("FOR UPDATE")
				.Take(2)
				.ToSqlQuery().Sql;

			sql.ShouldContain("ROWNUM");
			sql.ShouldNotContain("FETCH NEXT");
		}

		[Test]
		public void NegativeWait()
		{
			Assert.That(() => OracleHints.Wait(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
		}

		[Test]
		public void TerminalOperators([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version,
			[Values("First", "Single", "FirstOrDefault", "SingleOrDefault")] string operation)
		{
			using var db = CreateConnection(version);
			var source = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle()
				.ForUpdateHint()
				.Where(r => r.Id == 1);
			Expression<Func<LockRow?>> expression = operation switch
			{
				"First" => () => source.First(),
				"Single" => () => source.Single(),
				"FirstOrDefault" => () => source.FirstOrDefault(),
				"SingleOrDefault" => () => source.SingleOrDefault(),
				_ => throw new ArgumentOutOfRangeException(nameof(operation)),
			};
			var sql = source
				.Provider
				.CreateQuery<LockRow>(expression.Body)
				.ToSqlQuery()
				.Sql;
			sql.ShouldContain("ROWNUM");
			sql.ShouldNotContain("FETCH NEXT");
			sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE");
		}

		[Test]
		public void DmlSql([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version,
			[Values("Delete", "Update", "Insert")] string operation, [Values] bool generic)
		{
			using var db = CreateConnection(version);
			var table = db.GetTable<LockRow>();
			var source = generic ? table
				.SubQueryHint("FOR UPDATE") : table
				.AsQueryable()
				.AsOracle()
				.ForUpdateHint();
			Expression<Func<int>> expression = operation switch
			{
				"Delete" => () => source
					.Where(r => r.Id == 1)
					.Delete(),
				"Update" => () => source
					.Where(r => r.Id == 1)
					.Set(r => r.Value, 42)
					.Update(),
				"Insert" => () => source.Insert(table, r => new LockRow { Id = r.Id + 10, Value = r.Value }),
				_ => throw new ArgumentOutOfRangeException(nameof(operation)),
			};
			var sql = table
				.Provider
				.CreateQuery<int>(expression.Body)
				.ToSqlQuery()
				.Sql;
			sql.ShouldNotContain("FOR UPDATE");
			sql.ShouldContain(operation.ToUpperInvariant());
		}

		[Test]
		public void GenericHintRecognition([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var table = db.GetTable<LockRow>();
			var sql = table
				.SubQueryHint("  for\tupdate NOWAIT")
				.ToSqlQuery()
				.Sql;
			sql.ShouldContain("for\tupdate NOWAIT");
			Assert.That(() => table.SubQueryHint("FOR SHARE").ToSqlQuery(), Throws.TypeOf<LinqToDBException>());
			Assert.That(() => table.SubQueryHint("/* FOR UPDATE */").ToSqlQuery(), Throws.TypeOf<LinqToDBException>());
		}

		[Test]
		public void TakeArguments([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var table = db.GetTable<LockRow>();
			foreach (var count in new[] { 0, 1, 5 })
			{
				var sql = table
					.SubQueryHint("FOR UPDATE")
					.Take(count)
					.ToSqlQuery(new SqlGenerationOptions { InlineParameters = true })
					.Sql;
				sql.ShouldNotContain("FETCH NEXT");
				sql.ShouldContain("FOR UPDATE");
				if (count > 0)
					sql.ShouldContain($"ROWNUM <= {count}");
			}

			var plain = table
				.Take(2)
				.ToSqlQuery()
				.Sql;
			if (version == OracleVersion.v12)
				plain.ShouldContain("FETCH NEXT");
		}
	}
}
