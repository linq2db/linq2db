using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void InsertUpdate([MergeDataContextSource(TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[1], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void InsertDelete([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSapHana, TestProvName.AllFirebird)]
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
					.InsertWhenNotMatched()
					.DeleteWhenMatched()
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

		// ASE: just fails
		[Test]
		public void UpdateWithConditionDelete([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void UpdateWithConditionDeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllOracle, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird, ProviderName.Sybase)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).ToList().Concat(new[] { new TestMapping1() { Id = 1, Field1 = 123 } }))
					.OnTargetKey()
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.DeleteWhenMatchedAnd((t, s) => s.Id == 1)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = 222
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(3, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);

				Assert.AreEqual(4, result[2].Id);
				Assert.AreEqual(222, result[2].Field1);
				Assert.AreEqual(6, result[2].Field2);
				Assert.IsNull(result[2].Field3);
				Assert.IsNull(result[2].Field4);
				Assert.IsNull(result[2].Field5);
			}
		}

		[Test]
		public void InsertUpdateBySourceWithConditionDeleteBySource(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == 2,
						t => new TestMapping1() { Field1 = 44 })
					.DeleteWhenNotMatchedBySource()
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

		[Test]
		public void InsertDeleteUpdateBySource([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.DeleteWhenMatched()
					.UpdateWhenNotMatchedBySource(t => new TestMapping1() { Field1 = 44 })
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

		[Test]
		public void InsertWithConditionInsertUpdateWithConditionDeleteWithConditionDelete([MergeDataContextSource(
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).ToList().Concat(new[] { new TestMapping1() { Id = 1, Field1 = 123 } }))
					.OnTargetKey()
					.InsertWhenNotMatchedAnd(s => s.Id == 5)
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.DeleteWhenMatchedAnd((t, s) => t.Id == 4)
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(5, rows, context);

				if (context != ProviderName.Sybase)
				{
					Assert.AreEqual(4, result.Count);

					AssertRow(InitialTargetData[1], result[0], null, null);
					AssertRow(InitialTargetData[2], result[1], null, 203);
					AssertRow(InitialSourceData[2], result[2], null, null);
					AssertRow(InitialSourceData[3], result[3], null, 216);
				}
			}
		}

		[Test]
		public void UpdateInsert([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[1], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void UpdateWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllInformix, TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[1], result[3], null, null);
			}
		}

		[Test]
		public void DeleteInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.InsertWhenNotMatched()
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

		// ASE: just fails
		[Test]
		public void DeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void UpdateWithDeleteWithDeleteCondition(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		// ASE: just fails
		[Test]
		public void InsertUpdateWithConditionDeleteWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void InsertUpdateWithDelete([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAndThenDelete((t, s) => t.Id == 3 || s.Id == 4, (t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// ASE: just fails
		[Test]
		public void InsertDeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.InsertWhenNotMatched()
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// ASE: just fails
		[Test]
		public void UpdateWithConditionInsertDeleteWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.InsertWhenNotMatched()
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// ASE: just fails
		[Test]
		public void UpdateWithConditionDeleteWithConditionInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.DeleteWhenMatchedAnd((t, s) => s.Id == 4)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// ASE: just fails
		[Test]
		public void DeleteWithConditionUpdateWithConditionInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// ASE: just fails
		[Test]
		public void DeleteWithConditionInsertUpdateWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			ProviderName.Sybase, ProviderName.SybaseManaged, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAnd((t, s) => t.Id == 3)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void UpdateWithDeleteInsert([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete((t, s) => t.Id == 3 || t.Id == 4, (t, s) => s.Id == 4)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Table]
		class PKOnlyTable
		{
			[PrimaryKey] public int ID { get; set; }
		}


		[Test]
		public void InsertUpdatePKOnly([MergeDataContextSource(TestProvName.AllSapHana)] string context)
		{
			var src = new []
				{
					new PKOnlyTable() { ID = 1 },
					new PKOnlyTable() { ID = 2 },
					new PKOnlyTable() { ID = 3 }
				};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(src.Skip(1).Take(1)))
			{
				var rows = table
					.Merge()
					.Using(src)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(2, rows);
				Assert.AreEqual(3, result.Count);

				Assert.AreEqual(1, result[0].ID);
				Assert.AreEqual(2, result[1].ID);
				Assert.AreEqual(3, result[2].ID);
			}
		}
	}
}
