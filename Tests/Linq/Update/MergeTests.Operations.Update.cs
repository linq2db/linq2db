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
		[MergeDataContextSource]
		public void SameSourceUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db), (t, s) => s.Id == 3)
					.Update()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(4, result.Count);

				Assert.AreEqual(1, result[0].Id);
				Assert.IsNull(result[0].Field1);
				Assert.AreEqual(3, result[0].Field2);
				Assert.IsNull(result[0].Field3);
				Assert.IsNull(result[0].Field4);
				Assert.IsNull(result[0].Field5);

				Assert.AreEqual(2, result[1].Id);
				Assert.IsNull(result[1].Field1);
				Assert.AreEqual(3, result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.IsNull(result[1].Field5);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(3, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(203, result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(4, result[3].Id);
				Assert.IsNull(result[3].Field1);
				Assert.AreEqual(3, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void SameSourceUpdateWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void SameSourceUpdateWithUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => new TestMapping1()
					{
						Id = t.Id + s.Id,
						Field1 = t.Field1 + s.Field1,
						Field2 = t.Field2 + s.Field2,
						Field3 = t.Field3 + s.Field3,
						Field4 = t.Field4 + s.Field4,
						Field5 = t.Field5 + s.Field5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(6, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(6, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(8, result[3].Id);
				Assert.AreEqual(10, result[3].Field1);
				Assert.AreEqual(13, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void SameSourceUpdateWithPredicateAndUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => s.Id == 3, (t, s) => new TestMapping1()
					{
						Id = 123,
						Field1 = t.Field1 + s.Field5,
						Field2 = t.Field2 + s.Field4,
						Field3 = t.Field3 + s.Field3,
						Field4 = t.Field4 + s.Field2,
						Field5 = t.Field5 + s.Field1
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[3], result[2], null, null);

				Assert.AreEqual(123, result[3].Id);
				Assert.IsNull(result[3].Field1);
				Assert.IsNull(result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(206, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void OtherSourceUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.Update((t, s) => new TestMapping1()
					{
						Id = s.OtherId,
						Field1 = s.OtherField1,
						Field2 = s.OtherField2,
						Field3 = s.OtherField3,
						Field4 = s.OtherField4,
						Field5 = s.OtherField5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(3, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void OtherSourceUpdateWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.Update((t, s) => s.OtherField4 == 214, (t, s) => new TestMapping1()
					{
						Id = s.OtherId,
						Field1 = s.OtherField1,
						Field2 = s.OtherField2,
						Field3 = s.OtherField3,
						Field4 = s.OtherField4,
						Field5 = s.OtherField5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void AnonymousSourceUpdateWithPredicate(string context)
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
					.Update((t, s) => s.Field04 == 214, (t, s) => new TestMapping1()
					{
						Id = s.Key,
						Field1 = s.Field01,
						Field2 = s.Field02,
						Field3 = s.Field03,
						Field4 = s.Field04,
						Field5 = s.Field05
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void AnonymousListSourceUpdateWithPredicate(string context)
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
					.Update((t, s) => s.Field04 == 214, (t, s) => new TestMapping1()
					{
						Id = s.Key,
						Field1 = s.Field01,
						Field2 = s.Field02,
						Field3 = s.Field03,
						Field4 = s.Field04,
						Field5 = s.Field05
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void UpdateReservedAndCaseNames(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new
					{
						order = _.OtherId,
						delete = _.OtherField1,
						Delete1 = _.OtherField2,
						Field = _.OtherField3,
						field1 = _.OtherField4,
						As = _.OtherField5,
					}), (t, s) => t.Id == s.order)
					.Update((t, s) => s.field1 == 214, (t, s) => new TestMapping1()
					{
						Id = s.order,
						Field1 = s.delete,
						Field2 = s.Delete1,
						Field3 = s.Field,
						Field4 = s.field1,
						Field5 = s.As
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[MergeDataContextSource]
		public void UpdateReservedAndCaseNamesFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).ToList().Select(_ => new
					{
						@in = _.OtherId,
						join = _.OtherField1,
						outer = _.OtherField2,
						inner = _.OtherField3,
						with = _.OtherField4,
						left = _.OtherField5,
					}), (t, s) => t.Id == s.@in)
					.Update((t, s) => s.with == 214, (t, s) => new TestMapping1()
					{
						Id = s.@in,
						Field1 = s.join,
						Field2 = s.outer,
						Field3 = s.inner,
						Field4 = s.with,
						Field5 = s.left
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}
	}
}
