﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public partial class MergeTests : TestBase
	{
		[Table(Name = "AllTypes2")]
		class AllTypes2
		{
			[Column(DbType = "int"), PrimaryKey, Identity]
			public int ID { get; set; }

			[Column(DbType = "datetimeoffset(7)"), Nullable]
			public DateTimeOffset? datetimeoffsetDataType { get; set; }

			[Column(DbType = "datetime2(7)", DataType = DataType.DateTime2), Nullable]
			public DateTime? datetime2DataType { get; set; }
		}

		#region https://github.com/linq2db/linq2db/issues/200
		[Test]
		public void Issue200InSource([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = DateTime.Now;
				var dto = DateTimeOffset.Now;

				var testData = new[]
				{
					new AllTypes2()
					{
						ID = 1,
						datetimeoffsetDataType = dto,
						datetime2DataType = dt
					},
					new AllTypes2()
					{
						ID = 2,
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = dt.AddTicks(1)
					}
				};

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test]
		public void Issue200InPredicate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = DateTime.Now;
				var dto = DateTimeOffset.Now;

				var testData = new[]
				{
					new AllTypes2()
					{
						datetimeoffsetDataType = dto,
						datetime2DataType = dt
					},
					new AllTypes2()
					{
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = dt.AddTicks(1)
					}
				};

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.On((t, s) => s.datetime2DataType == testData[0].datetime2DataType && s.datetimeoffsetDataType == testData[0].datetimeoffsetDataType)
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test]
		public void Issue200InPredicate2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = DateTime.Now.AddTicks(-2);
				var dto = DateTimeOffset.Now.AddTicks(-2);

				var testData = new[]
				{
					new AllTypes2()
					{
						ID = 1,
						datetimeoffsetDataType = dto,
						datetime2DataType = dt
					},
					new AllTypes2()
					{
						ID = 2,
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = dt.AddTicks(1)
					}
				};

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.On((t, s) => s.datetime2DataType != DateTime.Now && s.datetimeoffsetDataType != DateTimeOffset.Now)
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test]
		public void Issue200InInsert([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = DateTime.Now;
				var dto = DateTimeOffset.Now;

				var testData = new[]
				{
					new AllTypes2()
					{
						ID = 1,
						datetimeoffsetDataType = dto,
						datetime2DataType = dt
					},
					new AllTypes2()
					{
						ID = 2,
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = dt.AddTicks(1)
					}
				};

				var dt2 = dt.AddTicks(3);
				var dto2 = dto.AddTicks(3);

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.On((t, s) => s.datetime2DataType == testData[0].datetime2DataType && s.datetimeoffsetDataType == testData[0].datetimeoffsetDataType)
					.InsertWhenNotMatched(s => new AllTypes2()
					{
						ID = s.ID,
						datetimeoffsetDataType = dto2,
						datetime2DataType = dt2
					})
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(testData[0].ID, result[0].ID);
				Assert.AreEqual(dt2, result[0].datetime2DataType);
				Assert.AreEqual(dto2, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].ID, result[1].ID);
				Assert.AreEqual(dt2, result[1].datetime2DataType);
				Assert.AreEqual(dto2, result[1].datetimeoffsetDataType);
			}
		}

		[Test]
		public void Issue200InUpdate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = DateTime.Now;
				var dto = DateTimeOffset.Now;

				var testData = new[]
				{
					new AllTypes2()
					{
						datetimeoffsetDataType = dto,
						datetime2DataType = dt
					},
					new AllTypes2()
					{
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = dt.AddTicks(1)
					}
				};

				db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var dt2 = dt.AddTicks(3);
				var dto2 = dto.AddTicks(3);
				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.On((t, s) => t.datetime2DataType == s.datetime2DataType
						&& t.datetimeoffsetDataType == s.datetimeoffsetDataType
						&& t.datetime2DataType == testData[0].datetime2DataType
						&& t.datetimeoffsetDataType == testData[0].datetimeoffsetDataType)
					.UpdateWhenMatched((t, s) => new AllTypes2()
					{
						datetimeoffsetDataType = dto2,
						datetime2DataType = dt2
					})
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(1, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(dt2, result[0].datetime2DataType);
				Assert.AreEqual(dto2, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}
		#endregion

		#region https://github.com/linq2db/linq2db/issues/1007

		[Table("Person")]
		class Person1007
		{
			[Column("PersonID"), Identity]
			public int ID { get; set; }

			[PrimaryKey]
			public string FirstName { get; set; } = null!;

			[Column]
			public string LastName { get; set; } = null!;

			[Column]
			public string? MiddleName { get; set; }

			[Column]
			public Gender Gender { get; set; }
		}

		[Test]
		public void Issue1007OnNewAPI([IdentityInsertMergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = db.GetTable<TestMappingWithIdentity>();
				table.Delete();

				db.Insert(new TestMappingWithIdentity());

				var lastId = table.Select(_ => _.Id).Max();

				var source = new[]
				{
					new TestMappingWithIdentity()
					{
						Field = 10
					}
				};

				var rows = table
					.Merge()
					.Using(source)
					.On((s, t) => s.Field == null)
					.InsertWhenNotMatched()
					.UpdateWhenMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, result.Count);

				var newRecord = new TestMapping1();

				Assert.AreEqual(lastId, result[0].Id);
				Assert.AreEqual(10, result[0].Field);
			}
		}
		#endregion

		[Test]
		public void TestDB2NullsInSource([IncludeDataSources(true, ProviderName.DB2)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.GetTable<MergeTypes>()
					.TableName("TestMerge1")
					.Merge()
					.Using(new[] { new MergeTypes() { Id = 1 }, new MergeTypes() { Id = 2 } })
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		#region issue 2388
		public interface IReviewIndex
		{
			int       Id   { get; set; }
			string?   Value   { get; set; }
		}

		[Table("ReviewIndexes")]
		public class ReviewIndex : IReviewIndex
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column    ] public string? Value { get; set; }

			public static readonly IReviewIndex[] Data = new IReviewIndex[]
			{
				new ReviewIndex()
				{
					Id = 1,
					Value = "2"
				}
			};
		}

		[Test]
		public void TestMergeWithInterfaces1([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.On(x => new { x.Id }, x => new { x.Id })
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces2([MergeDataContextSource(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.On(x => new { x.Id }, x => new { x.Id })
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces3([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.On((t, s) => t.Id == s.Id)
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces4([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces5([MergeDataContextSource(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces6([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.On(x => new { x.Id }, x => new { x.Id })
					.InsertWhenNotMatchedAnd(s => s.Id > 1)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces7([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.InsertWhenNotMatched(s => new ReviewIndex()
					{
						Id    = 2,
						Value = "3"
					})
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces8([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.On(x => new { x.Id }, x => new { x.Id })
					.InsertWhenNotMatchedAnd(
						s => s.Id > 1,
						s => new ReviewIndex()
						{
							Id = 2,
							Value = "3"
						})
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces9([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.On(x => new { x.Id }, x => new { x.Id })
					.UpdateWhenMatchedAnd((t, s) => t.Id != s.Id)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces10([MergeDataContextSource(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new ReviewIndex()
					{
						Id    = 2,
						Value = "3"
					})
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces11([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatchedAnd(
						(t, s) => t.Id != s.Id,
						(t, s) => new ReviewIndex()
						{
							Id = 2,
							Value = "3"
						})
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces12([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete((s, t) => s.Id != t.Id)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces13([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete((s, t) => s.Id != t.Id, (s, t) => s.Id != t.Id)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces14([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete((s, t) => new ReviewIndex() { Value = "3" }, (s, t) => s.Value != t.Value)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces15([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenMatchedAndThenDelete((s, t) => s.Value != t.Value, (s, t) => new ReviewIndex() { Value = "3" }, (s, t) => s.Value != t.Value)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces16([MergeDataContextSource(ProviderName.Firebird, TestProvName.AllOracle, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.DeleteWhenMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces17([MergeDataContextSource(ProviderName.Firebird, TestProvName.AllInformix, TestProvName.AllOracle, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => t.Id == s.Id)
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces18([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenNotMatchedBySource(t =>  new ReviewIndex() { Id = 2, Value = "3"})
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces19([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenNotMatchedBySourceAnd(t => t.Id == 3, t => new ReviewIndex() { Id = 2, Value = "3" })
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces20([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.DeleteWhenNotMatchedBySource()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces21([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 3)
					.Merge();
			}
		}
		#endregion

		#region https://github.com/linq2db/linq2db/issues/2377
		class CacheTestTable
		{
			[PrimaryKey] public int Id;
			[Column    ] public int Value;
		}

		[Test(Description = "")]
		public void TestEnumerableSourceCaching([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<CacheTestTable>())
			{
				var source = new List<CacheTestTable>()
				{
					new CacheTestTable() { Id = 1, Value = 1 },
					new CacheTestTable() { Id = 2, Value = 2 },
				};

				table
					.Merge()
					.Using(source)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				var res = table.OrderBy(_ => _.Id).ToArray();

				Assert.AreEqual(2, res.Length);
				Assert.AreEqual(1, res[0].Id);
				Assert.AreEqual(1, res[0].Value);
				Assert.AreEqual(2, res[1].Id);
				Assert.AreEqual(2, res[1].Value);

				source[1].Value = 4;
				source.Add(new CacheTestTable() { Id = 3, Value = 3 });

				table
					.Merge()
					.Using(source)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();

				res = table.OrderBy(_ => _.Id).ToArray();

				Assert.AreEqual(3, res.Length);
				Assert.AreEqual(1, res[0].Id);
				Assert.AreEqual(1, res[0].Value);
				Assert.AreEqual(2, res[1].Id);
				Assert.AreEqual(4, res[1].Value);
				Assert.AreEqual(3, res[2].Id);
				Assert.AreEqual(3, res[2].Value);
			}
		}

		#endregion

		#region TestNullableParameterInSourceQuery
		[Table]
		public class TestNullableParameterTarget
		{
			[PrimaryKey] public int Id1 { get; set; }
			[PrimaryKey] public int Id2 { get; set; }
		}

		[Table]
		public class TestNullableParameterSource
		{
			[PrimaryKey] public int Id { get; set; }
		}

		[Test]
		public void TestNullableParameterInSourceQuery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var target = db.CreateLocalTable<TestNullableParameterTarget>())
			using (var source = db.CreateLocalTable<TestNullableParameterSource>())
			{
				run(null);
				run(1);

				void run(int? id)
				{
					target
						.Merge()
						.Using(source
							.Where(_ => _.Id == id)
							.Select(_ => new TestNullableParameterTarget()
							{
								Id1 = 2,
								Id2 = _.Id
							}))
						.OnTargetKey()
						.InsertWhenNotMatched()
						.Merge();
				}
			}
		}
		#endregion
	}
}
