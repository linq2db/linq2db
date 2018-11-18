using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests
	{
		[Test]
		public void SameSourceUpdate([MergeDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.On((t, s) => s.Id == 3)
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

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

		[Test]
		public void UpdatePartialSourceProjection_KnownFieldsInDefaultSetter([MergeDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(s => new TestMapping1()
					{
						Id = s.Id,
						Field1 = s.Field1,
						Field2 = s.Field2,
						Field3 = s.Field3,
						Field4 = s.Field4
					}))
					.On((t, s) => s.Id == 3)
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

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

		[Test]
		public void SameSourceUpdateWithPredicate([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAnd((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

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

		// Oracle: updates field, used in match
		// Firebird: update of match key leads to incorrect update
		[Test]
		public void SameSourceUpdateWithUpdate([MergeDataContextSource(
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new TestMapping1()
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

				AssertRowCount(2, rows, context);

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

		[Test]
		public void SameSourceUpdateWithUpdateOracle([MergeDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = t.Field1 + s.Field1,
						Field2 = t.Field2 + s.Field2,
						Field3 = t.Field3 + s.Field3,
						Field4 = t.Field4 + s.Field4,
						Field5 = t.Field5 + s.Field5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(6, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(10, result[3].Field1);
				Assert.AreEqual(13, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void UpdatePartialSourceProjection_KnownFieldInSetter([MergeDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(s => new TestMapping1() { Id = s.Id, Field1 = s.Field1 }))
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = t.Field1 + s.Field1
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.AreEqual(3, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(203, result[2].Field4);
				Assert.IsNull(result[2].Field5);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(10, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void SameSourceUpdateWithPredicateAndUpdate([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAnd(
						(t, s) => s.Id == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.Field5,
							Field2 = t.Field2 + s.Field4,
							Field3 = t.Field3 + s.Field3,
							Field4 = t.Field4 + s.Field2,
							Field5 = t.Field5 + s.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);

				Assert.AreEqual(3, result[2].Id);
				Assert.IsNull(result[2].Field1);
				Assert.IsNull(result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.AreEqual(206, result[2].Field4);
				Assert.IsNull(result[2].Field5);

				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}

		[Test]
		public void UpdateWithPredicatePartialSourceProjection_UnknownFieldInCondition([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
					.OnTargetKey()
					.UpdateWhenMatchedAnd(
						(t, s) => s.Field2 == 3,
						(t, s) => new TestMapping1()
						{
							Field5 = t.Field5 + s.Field1
						})
					.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void OtherSourceUpdate([MergeDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = s.OtherField1,
						Field2 = s.OtherField2,
						Field3 = s.OtherField3,
						Field4 = s.OtherField4,
						Field5 = s.OtherField5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

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

		[Test]
		public void OtherSourceUpdateWithPredicate([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedAnd(
						(t, s) => s.OtherField4 == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.OtherField1,
							Field2 = s.OtherField2,
							Field3 = s.OtherField3,
							Field4 = s.OtherField4,
							Field5 = s.OtherField5
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

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

		[Test]
		public void UpdatePartialSourceProjection_KnownFieldInCondition([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
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
							OtherField4 = s.OtherField4
						}))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenMatchedAnd(
						(t, s) => s.OtherField4 == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.OtherField1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(6, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.IsNull(result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void AnonymousSourceUpdateWithPredicate([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
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
					.UpdateWhenMatchedAnd(
						(t, s) => s.Field04 == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.Field01,
							Field2 = s.Field02,
							Field3 = s.Field03,
							Field4 = s.Field04,
							Field5 = s.Field05
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

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

		[Test]
		public void AnonymousListSourceUpdateWithPredicate([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
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
					.UpdateWhenMatchedAnd(
						(t, s) => s.Field04 == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.Field01,
							Field2 = s.Field02,
							Field3 = s.Field03,
							Field4 = s.Field04,
							Field5 = s.Field05
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				if (context != ProviderName.Sybase)
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.AreEqual(4, result[3].Id);
				Assert.AreEqual(5, result[3].Field1);
				Assert.AreEqual(7, result[3].Field2);
				Assert.IsNull(result[3].Field3);
				Assert.AreEqual(214, result[3].Field4);
				Assert.IsNull(result[3].Field5);
			}
		}

		[Test]
		public void UpdateReservedAndCaseNames([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
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
						Delete = _.OtherField2,
						Field = _.OtherField3,
						field = _.OtherField4,
						As = _.OtherField5
					}))
					.On((t, s) => t.Id == s.order)
					.UpdateWhenMatchedAnd(
						(t, s) => s.field == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.delete,
							Field2 = s.Delete,
							Field3 = s.Field,
							Field4 = s.field,
							Field5 = s.As
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

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

		[Test]
		public void UpdateReservedAndCaseNamesFromList([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird, ProviderName.Sybase)]
			string context)
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
						Left = _.OtherField2
					}))
					.On((t, s) => t.Id == s.@in)
					.UpdateWhenMatchedAnd(
						(t, s) => s.with == 214,
						(t, s) => new TestMapping1()
						{
							Field1 = s.join,
							Field2 = s.outer,
							Field3 = s.inner,
							Field4 = s.with,
							Field5 = s.left
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

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

		[Test]
		public void UpdateFromPartialSourceProjection_UnknownFieldInDefaultSetter([MergeDataContextSource] string context)
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
						.UpdateWhenMatched()
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void UpdateFromPartialSourceProjection_UnknownFieldInSetter([MergeDataContextSource] string context)
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
						.UpdateWhenMatched((t, s) => new TestMapping1()
						{
							Id = s.Id,
							Field1 = s.Field2
						})
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}
	}
}
