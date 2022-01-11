// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.DataProvider.Oracle;

using NUnit.Framework;

namespace Tests.Linq
{
	partial class QueryExtensionOracleTests
	{
		[Test]
		public void TableHintFullTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracleSpecific()
					.FullHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Full}(p) */"));
		}

		[Test]
		public void TableHintFullInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracleSpecific()
			.FullInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Full}(p) {OracleHints.Table.Full}(c_1) */"));
		}

		[Test]
		public void TableHintClusterTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracleSpecific()
					.ClusterHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cluster}(p) */"));
		}

		[Test]
		public void TableHintClusterInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracleSpecific()
			.ClusterInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Cluster}(p) {OracleHints.Table.Cluster}(c_1) */"));
		}

		[Test]
		public void TableHintHashTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsOracleSpecific()
					.HashHint()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Hash}(p) */"));
		}

		[Test]
		public void TableHintHashInScopeTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsOracleSpecific()
			.HashInScopeHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Table.Hash}(p) {OracleHints.Table.Hash}(c_1) */"));
		}

		[Test]
		public void QueryHintAllRowsTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracleSpecific()
			.AllRowsHint();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.AllRows} */"));
		}

		[Test]
		public void QueryHintFirstRowsTest([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in db.Child
				join p in db.Parent on c.ParentID equals p.ParentID
				select p
			)
			.AsOracleSpecific()
			.FirstRowsHint(10);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"SELECT /*+ {OracleHints.Query.FirstRows(10)} */"));
		}

	}
}
