using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	// dynamic properties for target setters not supported for now, as it will require additional Merge API methods
	// could be added on request
	public partial class MergeTests
	{
		sealed class DynamicColumns1
		{
			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		sealed class DynamicColumns2
		{
			[DynamicColumnsStore]
			public IDictionary<string, object> ExtendedProperties { get; set; } = null!;
		}

		private MappingSchema ConfigureDynamicColumnsMappingSchema()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<DynamicColumns1>().HasTableName("TestMerge1")
					.HasPrimaryKey(x => Sql.Property<int>(x, "Id"))
					.Property(x => Sql.Property<int?>(x, "Field1"))
					.Property(x => Sql.Property<int?>(x, "Field2"))
					.Property(x => Sql.Property<int?>(x, "Field3")).HasSkipOnInsert(true)
					.Property(x => Sql.Property<int?>(x, "Field4")).HasSkipOnUpdate(true)
					.Property(x => Sql.Property<int?>(x, "Field5")).HasSkipOnInsert(true).HasSkipOnUpdate(true)
				.Build();

			return ms;
		}

		[Test]
		public void DynamicColumns([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();

				AssertRowCount(4, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
				AssertDynamicRow(InitialSourceData[1], result[3], null, null);
				AssertDynamicRow(InitialSourceData[2], result[4], null, null);
				AssertDynamicRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void DynamicColumns_UpdateWithConditionDelete([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.OnTargetKey()
					.UpdateWhenMatchedAnd((t, s) => Sql.Property<int>(t, "Id") == 3)
					.DeleteWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void DynamicColumns_DeleteWithConditionUpdate([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => Sql.Property<int>(s, "Id") == 4)
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void DynamicColumns_UpdateWithDeleteWithDeleteCondition(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete((t, s) => Sql.Property<int>(s, "Id") == 4)
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();

				AssertRowCount(2, rows, context);

				Assert.That(result, Has.Count.EqualTo(3));

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
			}
		}

		[Test]
		public void DynamicColumns_InsertUpdateWithConditionDeleteWithCondition([MergeDataContextSource(
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.On((t, s) => Sql.Property<int>(t, "Id") == Sql.Property<int>(s, "Id"))
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAnd((t, s) => Sql.Property<int>(t, "Id") == 3)
					.DeleteWhenMatchedAnd((t, s) => Sql.Property<int>(s, "Id") == 4)
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(4));

					Assert.That(result, Has.Count.EqualTo(5));
				}

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
				AssertDynamicRow(InitialSourceData[2], result[3], null, null);
				AssertDynamicRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void DynamicColumns_InsertUpdateWithDelete([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.UpdateWhenMatchedAndThenDelete((t, s) => Sql.Property<int>(t, "Id") == 3 || Sql.Property<int>(s, "Id") == 4, (t, s) => Sql.Property<int>(s, "Id") == 4)
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();

				AssertRowCount(4, rows, context);

				Assert.That(result, Has.Count.EqualTo(5));

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialSourceData[0], result[2], null, 203);
				AssertDynamicRow(InitialSourceData[2], result[3], null, null);
				AssertDynamicRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test]
		public void DynamicColumns_SameSourceInsertWithPredicate([MergeDataContextSource(
			TestProvName.AllInformix, TestProvName.AllSapHana, TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.On((t, s) => Sql.Property<int>(t, "Id") == Sql.Property<int>(s, "Id"))
					.InsertWhenNotMatchedAnd(s => Sql.Property<int?>(s, "Field5") != null)
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();

				AssertRowCount(0, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertDynamicRow(InitialTargetData[0], result[0], null, null);
				AssertDynamicRow(InitialTargetData[1], result[1], null, null);
				AssertDynamicRow(InitialTargetData[2], result[2], null, 203);
				AssertDynamicRow(InitialTargetData[3], result[3], null, null);
			}
		}

		[Test]
		public void DynamicColumns_DeleteBySourceFromPartialSourceProjection([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(db.GetTable<DynamicColumns1>().TableName("TestMerge2")
						.Select(_ => new TestMapping1() { Id = Sql.Property<int>(_, "Id"), Field1 = Sql.Property<int?>(_, "Field1") }))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 1)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void DynamicColumns_SameSourceUpdateWithUpdate([MergeDataContextSource(
			TestProvName.AllOracle, TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(db.GetTable<DynamicColumns1>().TableName("TestMerge2"))
					.On((t, s) => t.Id == Sql.Property<int>(s, "Id"))
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Id = t.Id + Sql.Property<int>(s, "Id"),
						Field1 = t.Field1 + Sql.Property<int?>(s, "Field1"),
						Field2 = t.Field2 + Sql.Property<int?>(s, "Field2"),
						Field3 = t.Field3 + Sql.Property<int?>(s, "Field3"),
						Field4 = t.Field4 + Sql.Property<int?>(s, "Field4"),
						Field5 = t.Field5 + Sql.Property<int?>(s, "Field5")
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[2].Id, Is.EqualTo(6));
					Assert.That(result[2].Field1, Is.Null);
					Assert.That(result[2].Field2, Is.EqualTo(6));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.Null);
					Assert.That(result[2].Field5, Is.Null);

					Assert.That(result[3].Id, Is.EqualTo(8));
					Assert.That(result[3].Field1, Is.EqualTo(10));
					Assert.That(result[3].Field2, Is.EqualTo(13));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				}
			}
		}

		[Test]
		public void DynamicColumns_SameSourceInsertWithCreate([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(db.GetTable<DynamicColumns1>().TableName("TestMerge2"))
					.On((t, s) => t.Id == Sql.Property<int>(s, "Id"))
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = 10 + Sql.Property<int>(s, "Id"),
						Field1 = 123,
						Field2 = Sql.Property<int?>(s, "Field1"),
						Field3 = Sql.Property<int?>(s, "Field2"),
						Field4 = 999,
						Field5 = 888
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[4].Id, Is.EqualTo(InitialSourceData[2].Id + 10));
					Assert.That(result[4].Field1, Is.EqualTo(123));
					Assert.That(result[4].Field2, Is.EqualTo(InitialSourceData[2].Field1));
					Assert.That(result[4].Field3, Is.EqualTo(4));
					Assert.That(result[4].Field4, Is.EqualTo(999));
					Assert.That(result[4].Field5, Is.EqualTo(888));

					Assert.That(result[5].Id, Is.EqualTo(InitialSourceData[3].Id + 10));
					Assert.That(result[5].Field1, Is.EqualTo(123));
					Assert.That(result[5].Field2, Is.EqualTo(InitialSourceData[3].Field1));
					Assert.That(result[5].Field3, Is.Null);
					Assert.That(result[5].Field4, Is.EqualTo(999));
					Assert.That(result[5].Field5, Is.EqualTo(888));
				}
			}
		}

		[Test]
		public void DynamicColumns_SameSourceDeleteBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context, ConfigureDynamicColumnsMappingSchema()))
			{
				PrepareData(db);

				var table = db.GetTable<DynamicColumns1>();

				var rows = table
					.Merge()
					.Using(table.TableName("TestMerge2"))
					.On((t, s) => Sql.Property<int>(t, "Id") == Sql.Property<int>(s, "Id"))
					.DeleteWhenNotMatchedBySourceAnd(t => Sql.Property<int>(t, "Id") == 1)
					.Merge();

				var result = table.OrderBy(_ => Sql.Property<int>(_, "Id")).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertDynamicRow(InitialTargetData[1], result[0], null, null);
				AssertDynamicRow(InitialTargetData[2], result[1], null, 203);
				AssertDynamicRow(InitialTargetData[3], result[2], null, null);
			}
		}

		private void AssertDynamicRow(TestMapping1 expected, DynamicColumns1 actual, int? exprected3, int? exprected4)
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(actual.ExtendedProperties["Id"], Is.EqualTo(expected.Id));
				Assert.That(actual.ExtendedProperties["Field1"], Is.EqualTo(expected.Field1));
				Assert.That(actual.ExtendedProperties["Field2"], Is.EqualTo(expected.Field2));
				Assert.That(actual.ExtendedProperties["Field3"], Is.EqualTo(exprected3));
				Assert.That(actual.ExtendedProperties["Field4"], Is.EqualTo(exprected4));
				Assert.That(actual.ExtendedProperties["Field5"], Is.Null);
			}
		}
	}
}
