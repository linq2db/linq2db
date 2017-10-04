using System;
using System.Linq;

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

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue200InSource(string context)
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

				Assert.AreEqual(testData[0].ID, result[0].ID);
				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].ID, result[1].ID);
				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue200InPredicate(string context)
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
					.On((t, s) => s.datetime2DataType == testData[0].datetime2DataType && s.datetimeoffsetDataType == testData[0].datetimeoffsetDataType)
					.InsertWhenNotMatched()
					.Merge();

				var result = db.GetTable<AllTypes2>().OrderBy(_ => _.ID).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, result.Length);

				Assert.AreEqual(testData[0].ID, result[0].ID);
				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].ID, result[1].ID);
				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue200InPredicate2(string context)
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

				Assert.AreEqual(testData[0].ID, result[0].ID);
				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].ID, result[1].ID);
				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue200InInsert(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue200InUpdate(string context)
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

				Assert.AreEqual(testData[0].ID, result[0].ID);
				Assert.AreEqual(dt2, result[0].datetime2DataType);
				Assert.AreEqual(dto2, result[0].datetimeoffsetDataType);

				Assert.AreEqual(testData[1].ID, result[1].ID);
				Assert.AreEqual(testData[1].datetime2DataType, result[1].datetime2DataType);
				Assert.AreEqual(testData[1].datetimeoffsetDataType, result[1].datetimeoffsetDataType);
			}
		}
	}
}
