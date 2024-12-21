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

				Assert.That(result, Has.Count.EqualTo(6));

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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[2], result[2], null, null);
				AssertRow(InitialSourceData[3], result[3], null, 216);
			}
		}

		[Test]
		public void UpdateWithConditionDelete([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(3));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void UpdateWithConditionDeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllPostgreSQL15Plus,
			TestProvName.AllOracle,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			ProviderName.Firebird25,
			ProviderName.Sybase)]
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

				Assert.That(result, Has.Count.EqualTo(3));

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[2].Id, Is.EqualTo(4));
					Assert.That(result[2].Field1, Is.EqualTo(222));
					Assert.That(result[2].Field2, Is.EqualTo(6));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.Null);
					Assert.That(result[2].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void InsertUpdateBySourceWithConditionDeleteBySource([MergeNotMatchedBySourceDataContextSource] string context)
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(2));
					Assert.That(result[0].Field1, Is.EqualTo(44));
					Assert.That(result[0].Field2, Is.Null);
					Assert.That(result[0].Field3, Is.Null);
					Assert.That(result[0].Field4, Is.Null);
					Assert.That(result[0].Field5, Is.Null);
				});

				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void InsertDeleteUpdateBySource([MergeNotMatchedBySourceDataContextSource] string context)
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(6));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(1));
					Assert.That(result[0].Field1, Is.EqualTo(44));
					Assert.That(result[0].Field2, Is.Null);
					Assert.That(result[0].Field3, Is.Null);
					Assert.That(result[0].Field4, Is.Null);
					Assert.That(result[0].Field5, Is.Null);

					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.EqualTo(44));
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.Null);
				});

				AssertRow(InitialSourceData[2], result[2], null, null);
				AssertRow(InitialSourceData[3], result[3], null, 216);
			}
		}

		[Test]
		public void InsertWithConditionInsertUpdateWithConditionDeleteWithConditionDelete([MergeDataContextSource(
			TestProvName.AllSqlServer2008Plus,
			TestProvName.AllOracle,
			TestProvName.AllPostgreSQL15Plus,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			ProviderName.Firebird25)]
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

				if (!context.IsAnyOf(ProviderName.Sybase))
				{
					Assert.That(result, Has.Count.EqualTo(4));

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

				Assert.That(result, Has.Count.EqualTo(6));

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
			TestProvName.AllPostgreSQL15Plus,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			ProviderName.Firebird25)]
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

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[1], result[3], null, null);
			}
		}

		[Test]
		public void DeleteInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[2], result[2], null, null);
				AssertRow(InitialSourceData[3], result[3], null, 216);
			}
		}

		[Test]
		public void DeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(3));
				});

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

				Assert.That(result, Has.Count.EqualTo(3));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void InsertUpdateWithConditionDeleteWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

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

				Assert.That(result, Has.Count.EqualTo(5));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void InsertDeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void UpdateWithConditionInsertDeleteWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void UpdateWithConditionDeleteWithConditionInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void DeleteWithConditionUpdateWithConditionInsert([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void DeleteWithConditionInsertUpdateWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
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

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				});

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

				Assert.That(result, Has.Count.EqualTo(5));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialSourceData[0], result[2], null, 203);
				AssertRow(InitialSourceData[2], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Table]
		sealed class PKOnlyTable
		{
			[PrimaryKey] public int ID { get; set; }
		}

		[Test]
		public void InsertUpdatePKOnly([MergeDataContextSource] string context)
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

				if (context.IsAnyOf(TestProvName.AllOracleNative))
					Assert.That(rows, Is.EqualTo(-1));
				else
					Assert.That(rows, Is.EqualTo(2));

				Assert.That(result, Has.Count.EqualTo(3));

				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(1));
					Assert.That(result[1].ID, Is.EqualTo(2));
					Assert.That(result[2].ID, Is.EqualTo(3));
				});
			}
		}
	}
}
