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
		public void SameSourceUpdateBySource([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySource(t => new TestMapping1()
					{
						Id = t.Id * 12,
						Field1 = t.Field1 * 2,
						Field2 = t.Id * 13,
						Field3 = t.Field2 * 2,
						Field4 = t.Field4 * 2,
						Field5 = t.Field5 * 2
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);

				Assert.AreEqual(12, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(13, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(24, result[3].Id);
				Assert.AreEqual(4, result[3].Field1);
				Assert.AreEqual(26, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceUpdateBySourceWithPredicate([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == 1,
						t => new TestMapping1()
						{
							Id = 123,
							Field1 = t.Id * 11,
							Field2 = t.Field2 + t.Field4,
							Field3 = t.Field3 + t.Field3,
							Field4 = t.Field4 + t.Field2,
							Field5 = t.Field5 + t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);

				Assert.AreEqual(123, result[3].Id);
				Assert.AreEqual(11, result[3].Field1);
				Assert.IsNull(result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OnConditionPartialSourceProjection_KnownFieldInCondition(
			[MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(s => new TestMapping2() { OtherId = s.OtherId }))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenNotMatchedBySource(t => new TestMapping1()
					{
						Id = t.Id + 10,
						Field1 = t.Field1 + t.Field2 + t.Field3,
						Field2 = t.Id * 10,
						Field3 = t.Field2 + t.Field1,
						Field4 = t.Field2,
						Field5 = t.Field1
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);

				Assert.AreEqual(11, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(10, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(12, result[3].Id);
				Assert.IsNull(result[3].Field1);
				Assert.AreEqual(20, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.AreEqual(2, result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceUpdateBySourceWithPredicate([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.AreEqual(2, result[1].Field5);

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void AnonymousSourceUpdateBySourceWithPredicate(
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
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.Key)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.AreEqual(2, result[1].Field5);

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void AnonymousListSourceUpdateBySourceWithPredicate(
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
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.Key)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.AreEqual(2, result[1].Field5);

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void UpdateBySourceReservedAndCaseNames([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}))
					.On((t, s) => t.Id == s.From)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.AreEqual(2, result[1].Field5);

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void UpdateBySourceReservedAndCaseNamesFromList(
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
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}))
					.On((t, s) => t.Id == s.From)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.AreEqual(2, result[1].Field5);

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}
	}
}
