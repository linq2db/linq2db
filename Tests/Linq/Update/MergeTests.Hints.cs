using NUnit.Framework;
using System.Linq;

namespace Tests.xUpdate
{
	using LinqToDB;
	using Model;

	// tests for query hints
	public partial class MergeTests
	{
		[Test]
		public void MergeIntoWithTargetHintSqlServer([IncludeDataSources(false,
			TestProvName.SqlAzure, ProviderName.SqlServer2008,
			ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "HOLDLOCK")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.True(db.LastQuery.Contains("MERGE INTO [TestMerge1] WITH(HOLDLOCK) [Target]"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void UsingTargetWithTargetHintSqlServer([IncludeDataSources(false,
			TestProvName.SqlAzure, ProviderName.SqlServer2008,
			ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetTarget(db)
					.Merge("HOLDLOCK")
					.UsingTarget()
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = t.Field1 + s.Field2
					})
					.Merge();

				Assert.True(db.LastQuery.Contains("MERGE INTO [TestMerge1] WITH(HOLDLOCK) [Target]"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(4, result.Count);

				Assert.AreEqual(InitialTargetData[0].Id, result[0].Id);
				Assert.AreEqual(InitialTargetData[0].Field1 + InitialTargetData[0].Field2, result[0].Field1);
				Assert.AreEqual(InitialTargetData[0].Field2, result[0].Field2);
				Assert.IsNull(result[0].Field3);
				Assert.IsNull(result[0].Field4);
				Assert.IsNull(result[0].Field5);

				Assert.AreEqual(InitialTargetData[1].Id, result[1].Id);
				Assert.AreEqual(InitialTargetData[1].Field1 + InitialTargetData[1].Field2, result[1].Field1);
				Assert.AreEqual(InitialTargetData[1].Field2, result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.IsNull(result[1].Field5);

				Assert.AreEqual(InitialTargetData[2].Id, result[2].Id);
				Assert.AreEqual(InitialTargetData[2].Field1 + InitialTargetData[2].Field2, result[2].Field1);
				Assert.AreEqual(InitialTargetData[2].Field2, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(203, result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(InitialTargetData[3].Id, result[3].Id);
				Assert.AreEqual(InitialTargetData[3].Field1 + InitialTargetData[3].Field2, result[3].Field1);
				Assert.AreEqual(InitialTargetData[3].Field2, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void MergeWithTargetHintSqlServer([IncludeDataSources(false,
			TestProvName.SqlAzure, ProviderName.SqlServer2008,
			ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetTarget(db)
					.Merge("HOLDLOCK")
					.Using(GetSource1(db).Where(s => s.Field1 != null && s.Field2 != null))
					.On(t => new { t.Field1, t.Field2 }, s => new { s.Field1, Field2 = (int?)6 })
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field3 = 321
					})
					.Merge();

				Assert.True(db.LastQuery.Contains("MERGE INTO [TestMerge1] WITH(HOLDLOCK) [Target]"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(InitialTargetData[3].Id, result[3].Id);
				Assert.AreEqual(InitialTargetData[3].Field1, result[3].Field1);
				Assert.AreEqual(InitialTargetData[3].Field2, result[3].Field2);
				Assert.AreEqual(321, result[3].Field3);
				Assert.AreEqual(InitialTargetData[3].Field4, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void MergeIntoWithTargetHintOracle([IncludeDataSources(false,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "append")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.True(db.LastQuery.Contains("MERGE /*+ append */ INTO TestMerge1 Target"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void MergeIntoWithTargetHintInformix([IncludeDataSources(false, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "AVOID_STMT_CACHE")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.True(db.LastQuery.Contains("MERGE {+ AVOID_STMT_CACHE } INTO TestMerge1 Target"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}
	}
}
