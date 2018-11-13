using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB;
	using Model;

	public partial class MergeTests
	{
		[Test]
		public void SameSourceUpdateWithDelete([MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.On((t, s) => s.Id == 3)
					.UpdateWhenMatchedThenDelete((t, s) => t.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(3, result.Count);

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
			}
		}

		[Test]
		public void UpdateWithDeletePartialSourceProjection_KnownFieldsInDefaultSetter(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db)
						.Select(s => new TestMapping1()
						{
							Id = s.Id,
							Field1 = s.Field1,
							Field2 = s.Field2,
							Field3 = s.Field3
						}))
					.On((t, s) => s.Id == 3)
					.UpdateWhenMatchedThenDelete((t, s) => t.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(3, result.Count);

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
			}
		}

		[Test]
		public void SameSourceUpdateWithDeleteWithPredicate(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete((t, s) => s.Id == 4 || s.Id == 3, (t, s) => t.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void SameSourceUpdateWithDeleteWithUpdate(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete(
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.Field1,
							Field2 = t.Field2 + s.Field2,
							Field3 = t.Field3 + s.Field3,
							Field4 = t.Field4 + s.Field4,
							Field5 = t.Field5 + s.Field5
						},
						(t, s) => t.Field1 == 10)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(6, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void SameSourceUpdateWithDeleteWithPredicateAndUpdate(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.Id == 3 || t.Id == 4,
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.Field5,
							Field2 = t.Field2 + s.Field4,
							Field3 = t.Field3 + s.Field3,
							Field4 = t.Field4 + s.Field2,
							Field5 = t.Field5 + s.Field1
						},
						(t, s) => s.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(220, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void UpdateWithDeletePartialSourceProjection_KnownFieldInUpdateCondition(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db)
						.Select(s => new TestMapping1()
						{
							Id = s.Id,
							Field2 = s.Field2,
							Field5 = s.Field5
						}))
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.Field2 == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.Field5
						},
						(t, s) => s.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(6, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void OtherSourceUpdateWithDelete([MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedThenDelete(
						(t, s) => new TestMapping1()
						{
							Field1 = s.OtherField1,
							Field2 = s.OtherField2,
							Field3 = s.OtherField3,
							Field4 = s.OtherField4,
							Field5 = s.OtherField5
						},
						(t, s) => s.OtherId == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(3, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void UpdateWithDeletePartialSourceProjection_KnownFieldInDeleteCondition(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db)
						.Select(s => new TestMapping2()
						{
							OtherId = s.OtherId,
							OtherField1 = s.OtherField1,
							OtherField2 = s.OtherField2
						}))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedThenDelete(
						(t, s) => new TestMapping1()
						{
							Field1 = s.OtherField1,
							Field2 = s.OtherField2
						},
						(t, s) => s.OtherId == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(3, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(203, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void OtherSourceUpdateWithDeleteWithPredicate(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.OtherField4 == 214 || t.Id == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = s.OtherField1,
							Field2 = s.OtherField2,
							Field3 = s.OtherField3,
							Field4 = s.OtherField4,
							Field5 = s.OtherField5
						},
						(t, s) => t.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(214, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void AnonymousSourceUpdateWithDeleteWithPredicate(
			[MergeUpdateWithDeleteDataContextSource] string context)
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
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.Field04 == 214 || t.Id == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = s.Field01,
							Field2 = s.Field02,
							Field3 = s.Field03,
							Field4 = s.Field04,
							Field5 = s.Field05
						},
						(t, s) => s.Key == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(214, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void AnonymousListSourceUpdateWithDeleteWithPredicate(
			[MergeUpdateWithDeleteDataContextSource] string context)
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
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.Field04 == 214 || s.Key == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = s.Field01,
							Field2 = s.Field02,
							Field3 = s.Field03,
							Field4 = s.Field04,
							Field5 = s.Field05
						},
						(t, s) => s.Key == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(214, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void UpdateWithDeleteReservedAndCaseNames(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						order = _.OtherId,
						delete = _.OtherField1,
						Delete1 = _.OtherField2,
						Field = _.OtherField3,
						field1 = _.OtherField4,
						As = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.order)
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.field1 == 214 || s.order == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = s.delete,
							Field2 = s.Delete1,
							Field3 = s.Field,
							Field4 = s.field1,
							Field5 = s.As
						},
						(t, s) => t.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(214, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void UpdateWithDeleteReservedAndCaseNamesFromList(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						@in = _.OtherId,
						join = _.OtherField1,
						outer = _.OtherField2,
						inner = _.OtherField3,
						with = _.OtherField4,
						left = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.@in)
					.UpdateWhenMatchedAndThenDelete(
						(t, s) => s.with == 214 || t.Id == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = s.join,
							Field2 = s.outer,
							Field3 = s.inner,
							Field4 = s.with,
							Field5 = s.left
						},
						(t, s) => t.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(5, result[2].Field1);
				Assert.AreEqual(7, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(214, result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void UpdateWithDeleteDeleteByConditionOnUpdatedField(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedThenDelete(
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.OtherField1 + 345
						},
						(t, s) => t.Field1 == 355)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void UpdateThenDeleteFromPartialSourceProjection_UnknownFieldInDeleteCondition(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(table.Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
						.OnTargetKey()
						.UpdateWhenMatchedThenDelete(
							(t, s) => new TestMapping1()
							{
								Id = s.Id,
								Field1 = s.Field1
							},
							(t, s) => t.Field2 == s.Field2)
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void UpdateThenDeleteFromPartialSourceProjection_UnknownFieldInSetter(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(table.Select(_ => new TestMapping1() { Id = _.Id }))
						.OnTargetKey()
						.UpdateWhenMatchedThenDelete(
							(t, s) => new TestMapping1()
							{
								Id = s.Id,
								Field1 = s.Field2
							},
							(t, s) => t.Field2 == s.Field1)
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void UpdateThenDeleteFromPartialSourceProjection_UnknownFieldInDefaultSetter(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(table.Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
						.OnTargetKey()
						.UpdateWhenMatchedThenDelete((t, s) => t.Field2 == s.Field1)
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void UpdateThenDeleteFromPartialSourceProjection_UnknownFieldInSearchCondition(
			[MergeUpdateWithDeleteDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(table.Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
						.OnTargetKey()
						.UpdateWhenMatchedAndThenDelete(
						    (t, s) => t.Field2 == s.Field2,
							(t, s) => new TestMapping1()
							{
								Id = s.Id,
								Field1 = s.Field1
							},
							(t, s) => t.Field2 == s.Field1)
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}
	}
}
