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
		[MergeDataContextSource(ProviderName.Firebird, ProviderName.SapHana)]
		public void SameSourceDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Delete()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(2, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void SameSourceDeleteWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Delete((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.SapHana)]
		public void OtherSourceDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db), (t, s) => s.OtherId == t.Id && t.Id == 3)
					.Delete()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void OtherSourceDeleteWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db), (t, s) => s.OtherId == t.Id)
					.Delete((t, s) => t.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void AnonymousSourceDeleteWithPredicate(string context)
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
					}), (t, s) => s.Key == t.Id)
					.Delete((t, s) => s.Key == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void AnonymousListSourceDeleteWithPredicate(string context)
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
					}), (t, s) => s.Key == t.Id)
					.Delete((t, s) => s.Key == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void DeleteReservedAndCaseNames(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new
					{
						select = _.OtherId,
						Field = _.OtherField1,
						field = _.OtherField2,
						insert = _.OtherField3,
						order = _.OtherField4,
						by = _.OtherField5
					}), (t, s) => s.select == t.Id)
					.Delete((t, s) => s.select == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void DeleteReservedAndCaseNamesFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).ToList().Select(_ => new
					{
						update = _.OtherId,
						Update = _.OtherField1,
						UPDATE = _.OtherField2,
						uPDATE = _.OtherField3,
						UpDaTe = _.OtherField4,
						upDATE = _.OtherField5
					}), (t, s) => s.update == t.Id)
					.Delete((t, s) => s.update == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
			}
		}
	}
}
