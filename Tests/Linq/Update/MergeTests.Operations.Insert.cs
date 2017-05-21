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
		#region Insert<TEntity>() + different source/match combinations
		[MergeDataContextSource]
		public void SameSourceInsertFromTable(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.FromSame(GetSource1(db)).Insert().Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromQuery(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.FromSame(GetSource1(db).Where(_ => _.Id == 5)).Insert().Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromQueryWithSelect(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field1,
						Field2 = _.Field2,
						Field3 = _.Id + _.Id,
						Field4 = _.Id + _.Id,
						Field5 = _.Field5
					}))
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, 10);
				AssertRow(InitialSourceData[3], result[5], null, 12);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromTableWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db), (t, s) => t.Id == s.Id || s.Field1 != null)
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromQueryWithSelectAndMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field3,
						Field2 = _.Field4,
						Field3 = _.Id + _.Id,
						Field4 = _.Id + _.Id + _.Id,
						Field5 = _.Field5
					}), (t, s) => t.Id == s.Id || s.Field2 != null)
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

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

		[MergeDataContextSource]
		public void SameSourceInsertFromCollection(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(InitialSourceData)
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(2, rows);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromEmptyCollection(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(new TestMapping1[0])
					.Insert()
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

		[MergeDataContextSource]
		public void SameSourceInsertFromCollectionWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(InitialSourceData, (t, s) => t.Id == s.Id || s.Field1 != null)
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void SameSourceInsertFromEmptyCollectionWithMatch(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(new TestMapping1[0], (t, s) => t.Id == s.Id && s.Field3 != null)
					.Insert()
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
		[MergeDataContextSource]
		public void SameSourceInsertWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).Select(_ => new TestMapping1()
					{
						Id = _.Id,
						Field1 = _.Field3,
						Field2 = _.Field4,
						Field3 = _.Id,
						Field4 = _.Id + _.Id + _.Id,
						Field5 = _.Field4
					}))
					.Insert(source => source.Field5 != null)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

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
		[MergeDataContextSource]
		public void SameSourceInsertWithCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert(_ => new TestMapping1()
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

				Assert.AreEqual(2, rows);

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id + 10, result[4].Id);
				Assert.AreEqual(123, result[4].Field1);
				Assert.AreEqual(InitialSourceData[2].Field1, result[4].Field2);
				// SkipInsert is ignored by explicit insert. Is it correct?
				//Assert.IsNull(result[4].Field3);
				Assert.AreEqual(11, result[4].Field3);
				Assert.AreEqual(999, result[4].Field4);
				//Assert.IsNull(result[4].Field5);
				Assert.AreEqual(888, result[4].Field5);

				Assert.AreEqual(InitialSourceData[3].Id + 10, result[5].Id);
				Assert.AreEqual(123, result[5].Field1);
				Assert.AreEqual(InitialSourceData[3].Field1, result[5].Field2);
				Assert.IsNull(result[5].Field3);
				Assert.AreEqual(999, result[5].Field4);
				//Assert.IsNull(result[5].Field5);
				Assert.AreEqual(888, result[5].Field5);
			}
		}
		#endregion

		#region Insert<TEntity>(predicate, create)
		[MergeDataContextSource]
		public void SameSourceInsertWithPredicateAndCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert(_ => _.Field2 != null, _ => new TestMapping1()
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

				Assert.AreEqual(1, rows);

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
				Assert.AreEqual(11, result[4].Field3);
				Assert.AreEqual(999, result[4].Field4);
				//Assert.IsNull(result[4].Field5);
				Assert.AreEqual(888, result[4].Field5);
			}
		}
		#endregion

		#region Insert<TTarget, TSource>(create) + different source/match combinations
		[MergeDataContextSource]
		public void OtherSourceInsertFromTable(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.Insert(s => new TestMapping1()
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

				Assert.AreEqual(6, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[MergeDataContextSource]
		public void OtherSourceInsertFromQuery(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db).Where(_ => _.OtherId == 5), (t, s) => t.Id == s.OtherId && s.OtherField3 != null)
					.Insert(s => new TestMapping1()
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				Assert.AreEqual(InitialSourceData[2].Id, result[4].Id);
				Assert.IsNull(result[4].Field1);
				Assert.IsNull(result[4].Field2);
				Assert.AreEqual(InitialSourceData[2].Field3, result[4].Field3);
				Assert.AreEqual(11, result[4].Field4);
				Assert.AreEqual(10, result[4].Field5);
			}
		}

		[MergeDataContextSource]
		public void OtherSourceInsertFromQueryWithSelect(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new TestMapping2()
					{
						OtherId = _.OtherId,
						OtherField1 = _.OtherField1,
						OtherField2 = _.OtherField2,
						OtherField3 = _.OtherId + _.OtherId,
						OtherField4 = _.OtherId + _.OtherId,
						OtherField5 = _.OtherField5
					}), (t, s) => t.Id == s.OtherId)
					.Insert(s => new TestMapping1()
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

				Assert.AreEqual(2, rows);

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

		[MergeDataContextSource]
		public void OtherSourceInsertFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetInitialSourceData2(), (t, s) => t.Id == s.OtherId)
					.Insert(s => new TestMapping1()
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

				Assert.AreEqual(2, rows);

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

		[MergeDataContextSource]
		public void OtherSourceInsertFromEmptyList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(new TestMapping2[0], (t, s) => t.Id == s.OtherId)
					.Insert(s => new TestMapping1()
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
		[MergeDataContextSource]
		public void OtherSourceInsertWithPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table.From(GetSource2(db), (t, s) => t.Id == s.OtherId)
					.Insert(s => s.OtherField4 == 216, s => new TestMapping1()
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

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void AnonymousSourceInsertWithPredicate(string context)
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
					.Insert(s => s.Field04 == 216, s => new TestMapping1()
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

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void AnonymousListSourceInsertWithPredicate(string context)
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
					.Insert(s => s.Field04 == 216, s => new TestMapping1()
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

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void InsertReservedAndCaseNames(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).Select(_ => new
					{
						field = _.OtherId,
						Field1 = _.OtherField1,
						and = _.OtherField2,
						or = _.OtherField3,
						between = _.OtherField4,
						@case = _.OtherField5,
					}), (t, s) => t.Id == s.field)
					.Insert(s => s.between == 216, s => new TestMapping1()
					{
						Id = s.field,
						Field1 = s.Field1,
						Field2 = s.and,
						Field3 = s.or,
						Field4 = s.between,
						Field5 = s.@case
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}

		[MergeDataContextSource]
		public void InsertReservedAndCaseNamesFromList(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.From(GetSource2(db).ToList().Select(_ => new
					{
						@as = _.OtherId,
						take = _.OtherField1,
						skip = _.OtherField2,
						Skip1 = _.OtherField3,
						insert = _.OtherField4,
						SELECT = _.OtherField5,
					}), (t, s) => t.Id == s.@as)
					.Insert(s => s.insert == 216, s => new TestMapping1()
					{
						Id = s.@as,
						Field1 = s.take,
						Field2 = s.skip,
						Field3 = s.Skip1,
						Field4 = s.insert,
						Field5 = s.SELECT
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[3], result[4], null, 216);
			}
		}
		#endregion
	}
}
