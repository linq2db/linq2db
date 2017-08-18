using System;
using System.Linq;
using Tests.Model;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

#if !NOASYNC
using System.Threading.Tasks;
#endif

namespace Tests.Merge
{
	public partial class MergeTests
	{
		#region Insert<TEntity>() + different source/match combinations
		[Test, MergeDataContextSource]
		public void SameSourceInsertFromTable(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromQuery(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromQueryWithSelect(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field1,
						Field2 = _.Field2,
						Field3 = _.Id + _.Id,
						Field4 = _.Id + _.Id,
						Field5 = _.Field5
					}))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, 10);
				AssertRow(InitialSourceData[3], result[5], null, 12);
			}
		}

		// DB2, SAPHANA: match condition matches multiple target records
		[Test, MergeDataContextSource(ProviderName.DB2, ProviderName.SapHana)]
		public void SameSourceInsertFromTableWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.On((t, s) => t.Id == s.Id || s.Field1 != null)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromTableWithMatchAlternative(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.On((t, s) => t.Id == s.Id - 1)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		// DB2, SAPHANA: match condition matches multiple target records
		[Test, MergeDataContextSource(ProviderName.DB2, ProviderName.SapHana)]
		public void SameSourceInsertFromQueryWithSelectAndMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field3,
						Field2 = _.Field4,
						Field3 = _.Id + _.Id,
						Field4 = _.Id + _.Id + _.Id,
						Field5 = _.Field5
					}))
					.On((t, s) => t.Id == s.Id || s.Field2 != null)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id, result[4].Id);
				Assert.IsNull(result[4].Field1);
				Assert.IsNull(result[4].Field2);
				Assert.IsNull(result[4].Field3);
				Assert.AreEqual(15, result[4].Field4);
				Assert.IsNull(result[4].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromQueryWithSelectAndMatchAlternative(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field3,
						Field2 = _.Field4,
						Field3 = _.Id + _.Id,
						Field4 = _.Id + _.Id + _.Id,
						Field5 = _.Field5
					}))
					.On((t, s) => t.Id == s.Id - 1)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[3].Id, result[4].Id);
				Assert.IsNull(result[4].Field1);
				Assert.AreEqual(216, result[4].Field2);
				Assert.IsNull(result[4].Field3);
				Assert.AreEqual(18, result[4].Field4);
				Assert.IsNull(result[4].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromCollection(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(InitialSourceData)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromEmptyCollection(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(new TestMapping1[0])
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(0, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}

		// DB2, SAPHANA: match condition matches multiple target records
		[Test, MergeDataContextSource(ProviderName.DB2, ProviderName.SapHana)]
		public void SameSourceInsertFromCollectionWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(InitialSourceData)
					.On((t, s) => t.Id == s.Id || s.Field1 != null)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromCollectionWithMatchAlternative(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(InitialSourceData)
					.On((t, s) => t.Id == s.Id - 1)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void SameSourceInsertFromEmptyCollectionWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(new TestMapping1[0])
					.On((t, s) => t.Id == s.Id && s.Field3 != null)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(0, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}
		#endregion

		#region Insert<TEntity>(predicate)
		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceInsertWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field3,
						Field2 = _.Field4,
						Field3 = _.Id,
						Field4 = _.Id + _.Id + _.Id,
						Field5 = _.Field4
					}))
					.OnTargetKey()
					.InsertWhenNotMatchedAnd(source => source.Field5 != null)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[3].Id, result[4].Id);
				Assert.IsNull(result[4].Field1);
				Assert.AreEqual(216, result[4].Field2);
				Assert.IsNull(result[4].Field3);
				Assert.AreEqual(18, result[4].Field4);
				Assert.IsNull(result[4].Field5);
			}
		}
		#endregion

		#region Insert<TEntity>(create)
		[Test, MergeDataContextSource]
		public void SameSourceInsertWithCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched(_ => new TestMapping1()
					{
						Id = 10 + _.Id,
						Field1 = 123,
						Field2 = _.Field1,
						Field3 = _.Field2,
						Field4 = 999,
						Field5 = 888
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id + 10, result[4].Id);
				Assert.AreEqual(123, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field2);
				Assert.AreEqual(4, result[4].Field3);
				Assert.AreEqual(999, result[4].Field4);
				Assert.AreEqual(888, result[4].Field5);

				Assert.AreEqual(InitialSourceData[3].Id + 10, result[5].Id);
				Assert.AreEqual(123, result[5].Field1);
				Assert.AreEqual(InitialSourceData[3].Field1, result[5].Field2);
				Assert.IsNull(result[5].Field3);
				Assert.AreEqual(999, result[5].Field4);
				Assert.AreEqual(888, result[5].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void DataContextTest(string context)
		{
			using (var db = new DataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched(_ => new TestMapping1
					{
						Id = 10 + _.Id,
						Field1 = 123,
						Field2 = _.Field1,
						Field3 = _.Field2,
						Field4 = 999,
						Field5 = 888
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id + 10, result[4].Id);
				Assert.AreEqual(123, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field2);
				Assert.AreEqual(4, result[4].Field3);
				Assert.AreEqual(999, result[4].Field4);
				Assert.AreEqual(888, result[4].Field5);

				Assert.AreEqual(InitialSourceData[3].Id + 10, result[5].Id);
				Assert.AreEqual(123, result[5].Field1);
				Assert.AreEqual(InitialSourceData[3].Field1, result[5].Field2);
				Assert.IsNull(result[5].Field3);
				Assert.AreEqual(999, result[5].Field4);
				Assert.AreEqual(888, result[5].Field5);
			}
		}
		#endregion

		#region Insert<TEntity>(predicate, create)
		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceInsertWithPredicateAndCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatchedAnd(
						_ => _.Field2 != null,
						_ => new TestMapping1()
						{
							Id = 10 + _.Id,
							Field1 = 123,
							Field2 = _.Field1,
							Field3 = _.Field2,
							Field4 = 999,
							Field5 = 888
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id + 10, result[4].Id);
				Assert.AreEqual(123, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field2);
				// SkipInsert is ignored by explicit insert. Is it correct?
				//Assert.IsNull(result[4].Field3);
				Assert.AreEqual(4, result[4].Field3);
				Assert.AreEqual(999, result[4].Field4);
				//Assert.IsNull(result[4].Field5);
				Assert.AreEqual(888, result[4].Field5);
			}
		}
		#endregion

		#region Insert<TTarget, TSource>(create) + different source/match combinations
		[Test, MergeDataContextSource]
		public void OtherSourceInsertFromTable(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.InsertWhenNotMatched(s => new TestMapping1()
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

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public void OtherSourceInsertFromQuery(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Where(_ => _.OtherId == 5))
					.On((t, s) => t.Id == s.OtherId && s.OtherField3 != null)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = s.OtherId,
						Field1 = s.OtherField5,
						Field2 = s.OtherField4,
						Field3 = s.OtherField3,
						Field4 = s.OtherField2,
						Field5 = s.OtherField1
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id, result[4].Id);
				Assert.IsNull(result[4].Field1);
				Assert.IsNull(result[4].Field2);
				Assert.AreEqual(InitialSourceData[2].Field3, result[4].Field3);
				Assert.AreEqual(4, result[4].Field4);
				Assert.AreEqual(10, result[4].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void OtherSourceInsertFromQueryWithSelect(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new TestMapping2()
					{
						OtherId = _.OtherId,
						OtherField1 = _.OtherField1,
						OtherField2 = _.OtherField2,
						OtherField3 = _.OtherId + _.OtherId,
						OtherField4 = _.OtherId + _.OtherId,
						OtherField5 = _.OtherField5
					}))
					.On((t, s) => t.Id == s.OtherId)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = s.OtherId,
						Field1 = s.OtherField3,
						Field2 = s.OtherField2,
						Field3 = s.OtherField1,
						Field4 = s.OtherField4,
						Field5 = s.OtherField5
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id, result[4].Id);
				Assert.AreEqual(10, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field2, result[4].Field2);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field3);
				Assert.AreEqual(10, result[4].Field4);
				Assert.IsNull(result[4].Field5);

				Assert.AreEqual(InitialSourceData[3].Id, result[5].Id);
				Assert.AreEqual(12, result[5].Field1);
				Assert.AreEqual(InitialSourceData[3].Field2, result[5].Field2);
				Assert.AreEqual(InitialSourceData[3].Field1, result[5].Field3);
				Assert.AreEqual(12, result[5].Field4);
				Assert.IsNull(result[5].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void OtherSourceInsertFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetInitialSourceData2())
					.On((t, s) => t.Id == s.OtherId)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = s.OtherId,
						Field1 = s.OtherField1,
						Field2 = s.OtherField5,
						Field3 = s.OtherField2,
						Field4 = s.OtherField4,
						Field5 = s.OtherField3
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id, result[4].Id);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field5, result[4].Field2);
				Assert.AreEqual(InitialSourceData[2].Field2, result[4].Field3);
				Assert.AreEqual(InitialSourceData[2].Field4, result[4].Field4);
				Assert.IsNull(result[4].Field5);

				Assert.AreEqual(InitialSourceData[3].Id, result[5].Id);
				Assert.AreEqual(InitialSourceData[3].Field1, result[5].Field1);
				Assert.AreEqual(InitialSourceData[3].Field5, result[5].Field2);
				Assert.AreEqual(InitialSourceData[3].Field2, result[5].Field3);
				Assert.AreEqual(InitialSourceData[3].Field4, result[5].Field4);
				Assert.AreEqual(116, result[5].Field5);
			}
		}

		[Test, MergeDataContextSource]
		public void OtherSourceInsertFromEmptyList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(new TestMapping2[0])
					.On((t, s) => t.Id == s.OtherId)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Field1 = s.OtherField1,
						Field2 = s.OtherField5,
						Field3 = s.OtherField2,
						Field4 = s.OtherField4,
						Field5 = s.OtherField3
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(0, rows);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}
		#endregion

		#region Insert<TTarget, TSource>(predicate, create)
		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void OtherSourceInsertWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.InsertWhenNotMatchedAnd(
						s => s.OtherField4 == 216,
						s => new TestMapping1()
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

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void AnonymousSourceInsertWithPredicate(string context)
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
					.InsertWhenNotMatchedAnd(
						s => s.Field04 == 216,
						s => new TestMapping1()
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

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void AnonymousListSourceInsertWithPredicate(string context)
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
					.InsertWhenNotMatchedAnd(
						s => s.Field04 == 216,
						s => new TestMapping1()
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

				AssertRowCount(1, rows, context);

				AssertRowCount(5, result.Count, context);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				if (result.Count != 6)
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void InsertReservedAndCaseNames(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						field = _.OtherId,
						Field = _.OtherField1,
						and = _.OtherField2,
						or = _.OtherField3,
						between = _.OtherField4,
						@case = _.OtherField5
					}))
					.On((t, s) => t.Id == s.field)
					.InsertWhenNotMatchedAnd(
						s => s.between == 216,
						s => new TestMapping1()
						{
							Id = s.field,
							Field1 = s.Field,
							Field2 = s.and,
							Field3 = s.or,
							Field4 = s.between,
							Field5 = s.@case
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[Test, MergeDataContextSource(ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void InsertReservedAndCaseNamesFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						@as = _.OtherId,
						take = _.OtherField1,
						skip = _.OtherField2,
						Skip = _.OtherField3,
						insert = _.OtherField4,
						SELECT = _.OtherField5
					}))
					.On((t, s) => t.Id == s.@as)
					.InsertWhenNotMatchedAnd(
						s => s.insert == 216,
						s => new TestMapping1()
						{
							Id = s.@as,
							Field1 = s.take,
							Field2 = s.skip,
							Field3 = s.Skip,
							Field4 = s.insert,
							Field5 = s.SELECT
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				if (context != ProviderName.Sybase)
					Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				if (context != ProviderName.Sybase)
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}
		#endregion

#if !NOASYNC
		#region Async
		[Test, MergeDataContextSource]
		public async Task SameSourceInsertFromTableAsyn(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = await table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeAsync();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test, MergeDataContextSource]
		public async Task SameSourceInsertFromQueryAsyn(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = await table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeAsync();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
			}
		}
		#endregion
#endif
	}
}
