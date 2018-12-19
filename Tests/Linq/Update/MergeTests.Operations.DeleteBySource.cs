using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests
	{
		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceDeleteBySource([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySource()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(2, result.Count);

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceDeleteBySourceWithPredicate([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 1)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceDeleteBySource([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id && t.Id == 3)
					.DeleteWhenNotMatchedBySource()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(3, rows);

				Assert.AreEqual(1, result.Count);

				AssertRow(InitialTargetData[2], result[0], null, 203);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceDeleteBySourceWithPredicate([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void AnonymousSourceDeleteBySourceWithPredicate(
			[MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						Key = _.OtherId
					}))
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void AnonymousListSourceDeleteBySourceWithPredicate(
			[MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						Key = _.OtherId
					}))
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void DeleteBySourceReservedAndCaseNames([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						select = _.OtherId,
						Select = _.OtherField1
					}))
					.On((t, s) => s.select == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void DeleteBySourceReservedAndCaseNamesFromList(
			[MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						INSERT = _.OtherId,
						insert = _.OtherField2
					}))
					.On((t, s) => s.INSERT == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void DeleteBySourceFromPartialSourceProjection(
			[MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 1)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}
	}
}
