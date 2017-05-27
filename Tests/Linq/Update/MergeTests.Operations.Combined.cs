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
		public void IsertUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert()
					.Update()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[1], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[MergeDataContextSource]
		public void IsertDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert()
					.Delete()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[2], result[2], null, null);
				AssertRow(InitialSourceData[3], result[3], null, 216);
			}
		}

		[MergeDataContextSource]
		public void UpdateWithConditionDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => t.Id == 3)
					.Delete()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[MergeDataContextSource(TestProvName.SqlAzure, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void UpdateWithConditionDeleteWithConditionUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).ToList().Concat(new[] { new TestMapping1() { Id = 1, Field1 = 123 } }))
					.Update((t, s) => t.Id == 3)
					.Delete((t, s) => s.Id == 1)
					.Update((t, s) => new TestMapping1()
					{
						Field1 = 222
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(3, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, null);
			}
		}

		[MergeBySourceDataContextSource]
		public void InsertUpdateBySourceWithConditionDeleteBySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert()
					.UpdateBySource(t => t.Id == 2, t => new TestMapping1() { Field1 = 44 })
					.DeleteBySource()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				Assert.AreEqual(2, result[0].Id);
				Assert.AreEqual(44, result[0].Field1);
				Assert.IsNull(result[0].Field2);
				Assert.IsNull(result[0].Field3);
				Assert.IsNull(result[0].Field4);
				Assert.IsNull(result[0].Field5);

				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeBySourceDataContextSource]
		public void InsertDeleteUpdateBySource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert()
					.Delete()
					.UpdateBySource(t => new TestMapping1() { Field1 = 44 })
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(6, rows);

				Assert.AreEqual(4, result.Count);

				Assert.AreEqual(1, result[0].Id);
				Assert.AreEqual(44, result[0].Field1);
				Assert.IsNull(result[0].Field2);
				Assert.IsNull(result[0].Field3);
				Assert.IsNull(result[0].Field4);
				Assert.IsNull(result[0].Field5);

				Assert.AreEqual(2, result[1].Id);
				Assert.AreEqual(44, result[1].Field1);
				Assert.IsNull(result[1].Field2);
				Assert.IsNull(result[1].Field3);
				Assert.IsNull(result[1].Field4);
				Assert.IsNull(result[1].Field5);

				AssertRow(InitialSourceData[2], result[2], null, null);
				AssertRow(InitialSourceData[3], result[3], null, 216);
			}
		}

		[MergeDataContextSource(TestProvName.SqlAzure, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void InsertWithConditionInsertUpdateWithConditionDeleteWithConditionDelete(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).ToList().Concat(new[] { new TestMapping1() { Id = 1, Field1 = 123 } }))
					.Insert(s => s.Id == 5)
					.Insert()
					.Update((t, s) => t.Id == 3)
					.Delete((t, s) => t.Id == 4)
					.Delete()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(5, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, null);
				AssertRow(InitialSourceData[0], result[3], null, null);
			}
		}
	}
}
