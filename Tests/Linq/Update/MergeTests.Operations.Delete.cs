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
		public void SameSourceDelete([MergeDataContextSource(
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana,
			ProviderName.Firebird, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(2, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
			}
		}

		[Test]
		public void SameSourceDeleteWithPredicate([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void DeletePartialSourceProjection_KnownFieldInCondition([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(s => new TestMapping1() {  Id = s.Id }))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void DeleteWithPredicatePartialSourceProjection_UnknownFieldInCondition([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => s.Field2 == 4)
					.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("'s.Field2' cannot be converted to SQL.", exception.Message);
				//Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void SameSourceDeleteWithPredicateDelete([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SqlServer2008,
			ProviderName.SqlServer2012, ProviderName.SqlServer2014, ProviderName.SqlServer2017, TestProvName.SqlAzure,
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(2, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
			}
		}

		[Test]
		public void OtherSourceDelete([MergeDataContextSource(
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id && t.Id == 3)
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void OtherSourceDeletePartialSourceProjection_UnknownFieldInMatch([MergeDataContextSource(
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
					.On((t, s) => s.Field2 == 3)
					.DeleteWhenMatched()
					.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("'s.Field2' cannot be converted to SQL.", exception.Message);
				//Assert.AreEqual("Column Field2 doesn't exist in source", exception.Message);
			}
		}

		[Test]
		public void OtherSourceDeleteWithPredicate([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id)
					.DeleteWhenMatchedAnd((t, s) => t.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void AnonymousSourceDeleteWithPredicate([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
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
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenMatchedAnd((t, s) => s.Key == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		// Oracle: implicit Delete to UpdateWithDelete conversion failed here
		[Test]
		public void AnonymousListSourceDeleteWithPredicate([MergeDataContextSource(
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
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
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenMatchedAnd((t, s) => s.Key == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void DeleteReservedAndCaseNames([MergeDataContextSource(
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						select = _.OtherId,
						Field = _.OtherField1,
						field = _.OtherField2,
						insert = _.OtherField3,
						order = _.OtherField4,
						by = _.OtherField5
					}))
					.On((t, s) => s.select == t.Id)
					.DeleteWhenMatchedAnd((t, s) => s.select == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void DeleteReservedAndCaseNamesFromList([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						update = _.OtherId,
						Update = _.OtherField1,
						UPDATE = _.OtherField2,
						uPDATE = _.OtherField3,
						UpDaTe = _.OtherField4,
						upDATE = _.OtherField5
					}))
					.On((t, s) => s.update == t.Id)
					.DeleteWhenMatchedAnd((t, s) => s.update == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[Test]
		public void DeleteFromPartialSourceProjection_MissingKeyField([MergeDataContextSource(
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.SapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(table.Select(_ => new TestMapping1() { Field1 = _.Field1 }))
						.OnTargetKey()
						.DeleteWhenMatched()
						.Merge());

				Assert.IsInstanceOf<LinqToDBException>(exception);
				Assert.AreEqual("'s.Id' cannot be converted to SQL.", exception.Message);
			}
		}
	}
}
