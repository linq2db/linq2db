using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	// tests for iqueryable targets (cte, non-cte)
	public partial class MergeTests
	{
		[Test]
		[ActiveIssueNew(2363, ErrorTypeName = "System.InvalidCastException", ErrorMessage = "Unable to cast object of type 'LinqToDB.Internal.Linq.Builder.SubQueryContext' to type 'LinqToDB.Internal.Linq.Builder.ITableContext'.",
			Details = "an IQueryable merge target reaches the builder as a SubQueryContext, which it cannot treat as a table. #2363 is the PR that introduced the capability.")]
		public void MergeIntoIQueryable([MergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = GetSource1(db)
					.MergeInto(table.Where(_ => _.Id >= 1))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(_ => _.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}

		[Test]
		public void MergeIntoCte([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = GetSource1(db)
					.MergeInto(table.Where(_ => _.Id >= 1).AsCte())
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(_ => _.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}

		[Test]
		public void MergeIntoCteIssue4107([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var updatedCount = db.Person.Where(x => x.FirstName == "unknown").AsCte()
				.Merge()
					.Using(db.Child)
					.On((dest, src) => dest.ID == src.ChildID)
						.UpdateWhenMatched((dest, temp) => new Model.Person()
						{
							MiddleName = "unpdated"
						})
					.Merge();
		}

		[Test]
		[ActiveIssueNew(2363, ErrorTypeName = "System.InvalidCastException", ErrorMessage = "Unable to cast object of type 'LinqToDB.Internal.Linq.Builder.SubQueryContext' to type 'LinqToDB.Internal.Linq.Builder.ITableContext'.",
			Details = "as MergeIntoIQueryable, from the source side.")]
		public void MergeFromIQueryable([MergeDataContextSource] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = table.Where(_ => _.Id >= 1)
					.Merge().Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(_ => _.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}

		[Test]
		public void MergeFromCte([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = table.Where(s => s.Id >= 1).AsCte()
					.Merge().Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(t => t.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[]
		{
			ProviderName.DB2,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			TestProvName.AllOracle,
			TestProvName.AllSapHana
		})]
		public void MergeUsingCteJoin([MergeDataContextSource(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = table
					.Merge().Using(GetSource1(db).Where(_ => _.Id >= 1).AsCte())
					.On(t => t.Id, s => s.Id)
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(_ => _.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}

		[Test]
		[ActiveIssue(3015, Configurations = new[]
		{
			ProviderName.DB2,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			TestProvName.AllOracle,
			TestProvName.AllSapHana
		})]
		public void MergeUsingCteWhere([MergeDataContextSource(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			PrepareData(db);

			var table = GetTarget(db);

			var rows = table
					.Merge().Using(GetSource1(db).Where(_ => _.Id >= 1).AsCte())
					.On((t, s) => t.Id == s.Id)
					.InsertWhenNotMatched()
					.Merge();

			var result = table.OrderBy(_ => _.Id).ToList();

			AssertRowCount(2, rows, context);

			Assert.That(result, Has.Count.EqualTo(6));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[2], result[4], null, null);
			AssertRow(InitialSourceData[3], result[5], null, 216);
		}
	}
}
