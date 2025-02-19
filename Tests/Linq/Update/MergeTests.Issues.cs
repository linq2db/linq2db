using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	public partial class MergeTests : TestBase
	{
		[Table(Name = "AllTypes2")]
		sealed class AllTypes2
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
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var testData = new[]
				{
					new AllTypes2()
					{
						ID = 1,
						datetimeoffsetDataType = TestData.DateTimeOffset,
						datetime2DataType = TestData.DateTime
					},
					new AllTypes2()
					{
						ID = 2,
						datetimeoffsetDataType = TestData.DateTimeOffset.AddTicks(1),
						datetime2DataType = TestData.DateTime.AddTicks(1)
					}
				};

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(2));
					Assert.That(result, Has.Length.EqualTo(2));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].datetime2DataType, Is.EqualTo(testData[0].datetime2DataType));
					Assert.That(result[0].datetimeoffsetDataType, Is.EqualTo(testData[0].datetimeoffsetDataType));

					Assert.That(result[1].datetime2DataType, Is.EqualTo(testData[1].datetime2DataType));
					Assert.That(result[1].datetimeoffsetDataType, Is.EqualTo(testData[1].datetimeoffsetDataType));
				});
			}
		}

		[Test]
		public void Issue200InPredicate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var testData = new[]
				{
					new AllTypes2()
					{
						datetimeoffsetDataType = TestData.DateTimeOffset,
						datetime2DataType = TestData.DateTime
					},
					new AllTypes2()
					{
						datetimeoffsetDataType = TestData.DateTimeOffset.AddTicks(1),
						datetime2DataType = TestData.DateTime.AddTicks(1)
					}
				};

				var cnt = db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.On((t, s) => s.datetime2DataType == testData[0].datetime2DataType && s.datetimeoffsetDataType == testData[0].datetimeoffsetDataType)
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(2));
					Assert.That(result, Has.Length.EqualTo(2));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].datetime2DataType, Is.EqualTo(testData[0].datetime2DataType));
					Assert.That(result[0].datetimeoffsetDataType, Is.EqualTo(testData[0].datetimeoffsetDataType));

					Assert.That(result[1].datetime2DataType, Is.EqualTo(testData[1].datetime2DataType));
					Assert.That(result[1].datetimeoffsetDataType, Is.EqualTo(testData[1].datetimeoffsetDataType));
				});
			}
		}

		[Test]
		public void Issue200InPredicate2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dt = TestData.DateTime.AddTicks(-2);
				var dto = TestData.DateTimeOffset.AddTicks(-2);

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
					.On((t, s) => s.datetime2DataType != TestData.DateTime && s.datetimeoffsetDataType != TestData.DateTimeOffset)
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(2));
					Assert.That(result, Has.Length.EqualTo(2));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].datetime2DataType, Is.EqualTo(testData[0].datetime2DataType));
					Assert.That(result[0].datetimeoffsetDataType, Is.EqualTo(testData[0].datetimeoffsetDataType));

					Assert.That(result[1].datetime2DataType, Is.EqualTo(testData[1].datetime2DataType));
					Assert.That(result[1].datetimeoffsetDataType, Is.EqualTo(testData[1].datetimeoffsetDataType));
				});
			}
		}

		[Test]
		public void Issue200InInsert([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dto = TestData.DateTimeOffset;

				var testData = new[]
				{
					new AllTypes2()
					{
						ID = 1,
						datetimeoffsetDataType = dto,
						datetime2DataType = TestData.DateTime
					},
					new AllTypes2()
					{
						ID = 2,
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = TestData.DateTime.AddTicks(1)
					}
				};

				var dt2 = TestData.DateTime.AddTicks(3);
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

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(2));
					Assert.That(result, Has.Length.EqualTo(2));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].ID, Is.EqualTo(testData[0].ID));
					Assert.That(result[0].datetime2DataType, Is.EqualTo(dt2));
					Assert.That(result[0].datetimeoffsetDataType, Is.EqualTo(dto2));

					Assert.That(result[1].ID, Is.EqualTo(testData[1].ID));
					Assert.That(result[1].datetime2DataType, Is.EqualTo(dt2));
					Assert.That(result[1].datetimeoffsetDataType, Is.EqualTo(dto2));
				});
			}
		}

		[Test]
		public void Issue200InUpdate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				db.GetTable<AllTypes2>().Delete();

				var dto = TestData.DateTimeOffset;

				var testData = new[]
				{
					new AllTypes2()
					{
						datetimeoffsetDataType = dto,
						datetime2DataType = TestData.DateTime
					},
					new AllTypes2()
					{
						datetimeoffsetDataType = dto.AddTicks(1),
						datetime2DataType = TestData.DateTime.AddTicks(1)
					}
				};

				db.GetTable<AllTypes2>()
					.Merge()
					.Using(testData)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var dt2 = TestData.DateTime.AddTicks(3);
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

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(1));
					Assert.That(result, Has.Length.EqualTo(2));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].datetime2DataType, Is.EqualTo(dt2));
					Assert.That(result[0].datetimeoffsetDataType, Is.EqualTo(dto2));

					Assert.That(result[1].datetime2DataType, Is.EqualTo(testData[1].datetime2DataType));
					Assert.That(result[1].datetimeoffsetDataType, Is.EqualTo(testData[1].datetimeoffsetDataType));
				});
			}
		}
		#endregion

		#region https://github.com/linq2db/linq2db/issues/1007

		[Table("Person")]
		sealed class Person1007
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

				Assert.That(result, Has.Count.EqualTo(1));

				var newRecord = new TestMapping1();

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(lastId));
					Assert.That(result[0].Field, Is.EqualTo(10));
				});
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
		public void TestMergeWithInterfaces6([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird25)] string context)
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
		public void TestMergeWithInterfaces8([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird25)] string context)
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
		public void TestMergeWithInterfaces9([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird25)] string context)
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
		public void TestMergeWithInterfaces11([MergeDataContextSource(TestProvName.AllInformix, ProviderName.Firebird25, TestProvName.AllOracle)] string context)
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
		public void TestMergeWithInterfaces16([MergeDataContextSource(ProviderName.Firebird25, TestProvName.AllOracle, TestProvName.AllSybase)] string context)
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
		public void TestMergeWithInterfaces17([MergeDataContextSource(ProviderName.Firebird25, TestProvName.AllInformix, TestProvName.AllOracle, TestProvName.AllSybase)] string context)
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
		public void TestMergeWithInterfaces18([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(ReviewIndex.Data)
					.OnTargetKey()
					.UpdateWhenNotMatchedBySource(t =>  new ReviewIndex() { Id = 2, Value = "3" })
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces19([MergeNotMatchedBySourceDataContextSource] string context)
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
		public void TestMergeWithInterfaces20([MergeNotMatchedBySourceDataContextSource] string context)
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
		public void TestMergeWithInterfaces21([MergeNotMatchedBySourceDataContextSource] string context)
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
		sealed class CacheTestTable
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

				Assert.That(res, Has.Length.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].Id, Is.EqualTo(1));
					Assert.That(res[0].Value, Is.EqualTo(1));
					Assert.That(res[1].Id, Is.EqualTo(2));
					Assert.That(res[1].Value, Is.EqualTo(2));
				});

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

				Assert.That(res, Has.Length.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].Id, Is.EqualTo(1));
					Assert.That(res[0].Value, Is.EqualTo(1));
					Assert.That(res[1].Id, Is.EqualTo(2));
					Assert.That(res[1].Value, Is.EqualTo(4));
					Assert.That(res[2].Id, Is.EqualTo(3));
					Assert.That(res[2].Value, Is.EqualTo(3));
				});
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
		public void TestNullableParameterInSourceQuery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL15Plus)] string context)
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

		[Test(Description = "Test query filter not used for target, but preserved for source")]
		public void Issue3729Test([MergeDataContextSource] string context)
		{
			// prepare data before fiters applied
			using (var db1 = GetDataContext(context))
				PrepareData(db1);

			var ms = new MappingSchema();
			new FluentMappingBuilder(ms).Entity<TestMapping1>().HasQueryFilter((t, _) => t.Id > 5).Build();

			using var db = GetDataContext(context, ms);

			var table = GetTarget(db);

			var rows = table
				.Merge()
				.Using(GetSource1(db))
				.OnTargetKey()
				.InsertWhenNotMatched()
				.Merge();

			var result = table.IgnoreFilters().OrderBy(_ => _.Id).ToList();

			AssertRowCount(1, rows, context);

			Assert.That(result, Has.Count.EqualTo(5));

			AssertRow(InitialTargetData[0], result[0], null, null);
			AssertRow(InitialTargetData[1], result[1], null, null);
			AssertRow(InitialTargetData[2], result[2], null, 203);
			AssertRow(InitialTargetData[3], result[3], null, null);
			AssertRow(InitialSourceData[3], result[4], null, 216);
		}

		// HANA: Syntax error or access violation;257 sql syntax error: incorrect syntax near "WHEN MATCHED "
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3589")]
		public void Issue3589Test([MergeDataContextSource(TestProvName.AllSapHana)] string context)
		{
			// prepare data before fiters applied
			using (var db1 = GetDataContext(context))
				PrepareData(db1);

			using var db = GetDataContext(context);

			GetTarget(db)
				.Merge()
				// otherwise most databases will complain about multiple matches
				.Using(GetSource1(db).Where(r => r.Id == 1))
				.On((a, b) => true)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.Merge();
		}

		// merge into CTE supported only by SQL Server
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4338")]
		public void Issue4338Test([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			// prepare data before fiters applied
			using (var db1 = GetDataContext(context))
				PrepareData(db1);

			using var db = GetDataContext(context);

			var values = new int[] { 11, 22 };

			db
				.GetTable<Child>()
				.Where(ai => ai.Parent!.Value1 == -99)
				.AsCte()
				.Merge()
				.Using(values.Select(u => new { Value1 = u }))
				.On((dst, src) => dst.ParentID == src.Value1)
				.InsertWhenNotMatchedAnd(
					s => s.Value1 == -123,
					s => new()
					{
						ParentID = 10,
						ChildID = s.Value1
					})
				.Merge();
		}

		#region issue 2918
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2918")]
		public void Issue2918Test([MergeDataContextSource(TestProvName.AllSqlServer2016Minus, TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			// prepare data before fiters applied
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<PatentAssessment>();
			using var t2 = db.CreateLocalTable<PatentAssessmentTechnicalReviewer>();
			using var t3 = db.CreateLocalTable<User>();

			var userId = 1;

			var query = from pa in t1
						where t2.Any(patr => patr.UserId == userId && patr.PatentId == pa.PatentId)
						select new PatentAssessment
						{
							PatentId = pa.PatentId,
							TechnicalReviewersText = t2.LoadWith(patr => patr.User)
														.Where(patr => patr.PatentId == pa.PatentId)
														.StringAggregate("; ", patr => patr.User.DisplayName)
														.OrderBy(patr => patr.User.DisplayName)
														.ToValue()
						};

			t1
				.Merge()
				.Using(query)
				.OnTargetKey()
				.UpdateWhenMatched((target, source) => new PatentAssessment()
				{
					TechnicalReviewersText = source.TechnicalReviewersText
				})
				.Merge();
		}

		[Table]
		sealed class PatentAssessment
		{
			[PrimaryKey] public int PatentId { get; set; }
			[Column(Length = 1000)] public string? TechnicalReviewersText { get; set; }

			[Association(ThisKey = nameof(PatentId), OtherKey = nameof(PatentAssessmentTechnicalReviewer.PatentId))]
			public List<PatentAssessmentTechnicalReviewer> TechnicalReviewers { get; set; } = null!;
		}

		// use shorter name
		[Table("Issue2918Table2")]
		sealed class PatentAssessmentTechnicalReviewer
		{
			[Column] public int PatentId { get; set; }
			[Column] public int UserId { get; set; }

			[Association(ThisKey = nameof(PatentId), OtherKey = nameof(PatentAssessment.PatentId))]
			public PatentAssessment PatentAssessment { get; set; } = null!;

			[Association(ThisKey = nameof(UserId), OtherKey = nameof(User.Id))]
			public User User { get; set; } = null!;
		}

		[Table]
		sealed class User
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(CanBeNull = false, Length = 1000)] public string DisplayName { get; set; } = null!;
		}
		#endregion

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4584")]
		public void Issue4584Test([MergeDataContextSource(false)] string context)
		{
			using var db = GetDataConnection(context);

			var records = new Person[]
			{
				new Person() { ID = 123, FirstName = "first name", LastName = "last name" }
			};

			db
				.Person
				.Merge()
				.Using(records)
				.OnTargetKey()
				.InsertWhenNotMatchedAnd(
					s => s.ID == -123,
					s => new()
					{
						FirstName = s.FirstName,
						LastName = s.LastName,
						Gender = s.Gender,
					})
				.Merge();

			Assert.That(db.LastQuery!.Count(_ => _ == GetParameterToken(context)), Is.EqualTo(6));
		}

		[Test]
		public void MergeSubquery([MergeDataContextSource(false)] string context, [Values(1, 2)] int iteration)
		{
			using var db  = GetDataConnection(context);

			db.BeginTransaction();

			using var tmp = db.CreateTempTable(
				"MergeTemp",
				[new { ID = 1, Name = "John" }],
				mb => mb
					.Property(t => t.ID)
						.IsPrimaryKey()
					.Property(t => t.Name)
						.HasLength(20));

			var cacheMiss = tmp.GetCacheMissCount();

			tmp.InsertOrUpdate(
				() => new
				{
					ID   = (from t in tmp where t.Name == "John" select t.ID).Single(),
					Name = "John II"
				},
				s => new { s.ID, s.Name });

			if (iteration == 2)
			{
				Assert.That(tmp.GetCacheMissCount(), Is.EqualTo(cacheMiss));
			}
		}
	}
}
