using System.Linq;
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
			[Column] public int Id    { get; set; }
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
				.ForUpdateHint(OracleHints.NoWait)
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE NOWAIT");
			source
				.ForUpdateHint(OracleHints.SkipLocked)
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE SKIP LOCKED");
			source
				.ForUpdateHint(OracleHints.Wait(5))
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE WAIT 5");
			source
				.ForUpdateHint(new[] { "\"Id\"" })
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE OF \"Id\"");
			source
				.ForUpdateHint(new[] { "\"Id\"", "\"Value\"" })
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE OF \"Id\", \"Value\"");
			source
				.ForUpdateHint(OracleHints.NoWait, "\"Id\"", "\"Value\"")
				.ToSqlQuery()
				.Sql
				.TrimEnd()
				.ShouldEndWith("FOR UPDATE OF \"Id\", \"Value\" NOWAIT");
		}

		[Test]
		public void ExpressionOverloads([Values(OracleVersion.v11, OracleVersion.v12)] OracleVersion version)
		{
			using var db = CreateConnection(version);
			var source  = db
				.GetTable<LockRow>()
				.AsQueryable()
				.AsOracle();
			var clause  = OracleHints.NoWait;
			var columns = new[] { "\"Id\"" };

			Expression<System.Func<IOracleSpecificQueryable<LockRow>>>[] expressions =
			{
				() => source.ForUpdateHint(),
				() => source.ForUpdateHint(clause),
				() => source.ForUpdateHint(columns),
				() => source.ForUpdateHint(clause, columns),
			};
			var suffixes = new[] { "FOR UPDATE", "FOR UPDATE NOWAIT", "FOR UPDATE OF \"Id\"", "FOR UPDATE OF \"Id\" NOWAIT" };

			for (var i = 0; i < expressions.Length; i++)
			{
				var query = source.Provider.CreateQuery<LockRow>(expressions[i].Body);
				query
					.ToSqlQuery()
					.Sql
					.TrimEnd()
					.ShouldEndWith(suffixes[i]);
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

			foreach (var clause in new[] { OracleHints.NoWait, OracleHints.SkipLocked, OracleHints.Wait(5), OracleHints.Wait(10) })
			{
				foreach (var columns in new[] { new[] { "\"Id\"" }, new[] { "\"Value\"" }, new[] { "\"Id\"", "\"Value\"" } })
				{
					Expression<System.Func<IOracleSpecificQueryable<LockRow>>> expression = () => source.ForUpdateHint(clause, columns);
					var query = source.Provider.CreateQuery<LockRow>(expression.Body);
					query
						.ToSqlQuery()
						.Sql
						.TrimEnd()
						.ShouldEndWith($"FOR UPDATE OF {string.Join(", ", columns)} {clause}");
				}
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
	}
}
