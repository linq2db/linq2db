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
		public void MergeIntoWithTargetHintSqlServer([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "HOLDLOCK")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.That(db.LastQuery!, Does.Contain("MERGE INTO [TestMerge1] WITH(HOLDLOCK) [Target]"));

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

		[Test]
		public void UsingTargetWithTargetHintSqlServer([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
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

				Assert.That(db.LastQuery!, Does.Contain("MERGE INTO [TestMerge1] WITH(HOLDLOCK)"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(InitialTargetData[0].Id));
					Assert.That(result[0].Field1, Is.EqualTo(InitialTargetData[0].Field1 + InitialTargetData[0].Field2));
					Assert.That(result[0].Field2, Is.EqualTo(InitialTargetData[0].Field2));
					Assert.That(result[0].Field3, Is.Null);
					Assert.That(result[0].Field4, Is.Null);
					Assert.That(result[0].Field5, Is.Null);

					Assert.That(result[1].Id, Is.EqualTo(InitialTargetData[1].Id));
					Assert.That(result[1].Field1, Is.EqualTo(InitialTargetData[1].Field1 + InitialTargetData[1].Field2));
					Assert.That(result[1].Field2, Is.EqualTo(InitialTargetData[1].Field2));
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.Null);

					Assert.That(result[2].Id, Is.EqualTo(InitialTargetData[2].Id));
					Assert.That(result[2].Field1, Is.EqualTo(InitialTargetData[2].Field1 + InitialTargetData[2].Field2));
					Assert.That(result[2].Field2, Is.EqualTo(InitialTargetData[2].Field2));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.EqualTo(203));
					Assert.That(result[2].Field5, Is.Null);

					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1 + InitialTargetData[3].Field2));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void MergeWithTargetHintSqlServer([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
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

				Assert.That(db.LastQuery!, Does.Contain("MERGE INTO [TestMerge1] WITH(HOLDLOCK) [Target]"));

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.EqualTo(321));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void MergeIntoWithTargetHintOracle([IncludeDataSources(false, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "append")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.That(db.LastQuery!, Does.Contain("MERGE /*+ append */ INTO \"TestMerge1\" Target"));

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

		[Test]
		public void MergeIntoWithTargetHintInformix([IncludeDataSources(false, TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(GetTarget(db), "AVOID_STMT_CACHE")
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				Assert.That(db.LastQuery!, Does.Contain("MERGE {+ AVOID_STMT_CACHE } INTO TestMerge1 Target"));

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
}
