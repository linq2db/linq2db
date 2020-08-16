using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using System.Threading.Tasks;
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
			DateTime  Date    { get; set; }
			int       Index   { get; set; }
			decimal   Value   { get; set; }
			bool      Ctime   { get; set; }
			DateTime? DateMsk { get; set; }
			double?   Change  { get; set; }
			short?    Decp    { get; set; }
		}

		[Table("ReviewIndexes")]
		public class ReviewIndex : IReviewIndex
		{
			[PrimaryKey]
			public DateTime Date { get; set; }

			[PrimaryKey]
			public int Index { get; set; }

			[Column]
			public decimal Value { get; set; }

			[Column]
			public bool Ctime { get; set; }

			[Column]
			public DateTime? DateMsk { get; set; }

			[Column]
			public double? Change { get; set; }

			[Column]
			public short? Decp { get; set; }
		}

		[Test]
		public void TestMergeWithInterfaces1([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				var items = new IReviewIndex[]
				{
					new ReviewIndex()
					{
						Change  = 1.1,
						Ctime   = true,
						Date    = DateTime.Now,
						DateMsk = DateTime.Now,
						Decp    = 2,
						Index   = 1,
						Value   = 2.2m
					}
				};

				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(items)
					.On(x => new { x.Index, x.Date }, x => new { x.Index, x.Date })
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces2([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(((ITable<IReviewIndex>)db.GetTable<ReviewIndex>()))
					.On(x => new { x.Index, x.Date }, x => new { x.Index, x.Date })
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
				var items = new IReviewIndex[]
				{
					new ReviewIndex()
					{
						Change  = 1.1,
						Ctime   = true,
						Date    = DateTime.Now,
						DateMsk = DateTime.Now,
						Decp    = 2,
						Index   = 1,
						Value   = 2.2m
					}
				};

				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(items)
					.On((t, s) => t.Index == s.Index && t.Date == s.Date)
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
				var items = new IReviewIndex[]
				{
					new ReviewIndex()
					{
						Change  = 1.1,
						Ctime   = true,
						Date    = DateTime.Now,
						DateMsk = DateTime.Now,
						Decp    = 2,
						Index   = 1,
						Value   = 2.2m
					}
				};

				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(items)
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}

		[Test]
		public void TestMergeWithInterfaces5([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ReviewIndex>())
			{
				((ITable<IReviewIndex>)db.GetTable<ReviewIndex>())
					.Merge()
					.Using(((ITable<IReviewIndex>)db.GetTable<ReviewIndex>()))
					.OnTargetKey()
					.UpdateWhenMatched()
					.InsertWhenNotMatched()
					.Merge();
			}
		}
		#endregion
	}
}
