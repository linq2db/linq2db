using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		[MergeBySourceDataContextSource]
		public void SameSourceUpdateBySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.UpdateBySource(t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void SameSourceUpdateBySourceWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.UpdateBySource(t => t.Id == 1, t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void OtherSourceUpdateBySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.UpdateBySource(t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void OtherSourceUpdateBySourceWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.UpdateBySource(t => t.Field1 == 2, t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void AnonymousSourceUpdateBySourceWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new
					{
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}), (t, s) => t.Id == s.Key)
					.UpdateBySource(t => t.Field1 == 2, t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void AnonymousListSourceUpdateBySourceWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).ToList().Select(_ => new
					{
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}), (t, s) => t.Id == s.Key)
					.UpdateBySource(t => t.Field1 == 2, t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void UpdateBySourceReservedAndCaseNames(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new
					{
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field3 = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}), (t, s) => t.Id == s.From)
					.UpdateBySource(t => t.Field1 == 2, t => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void UpdateBySourceReservedAndCaseNamesFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).ToList().Select(_ => new
					{
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field2 = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}), (t, s) => t.Id == s.From)
					.UpdateBySource(t => t.Field1 == 2, t => new TestMapping1()
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
