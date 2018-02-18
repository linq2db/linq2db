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

		#region https://github.com/linq2db/linq2db/issues/200
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

				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

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

				Assert.AreEqual(testData[0].datetime2DataType, result[0].datetime2DataType);
				Assert.AreEqual(testData[0].datetimeoffsetDataType, result[0].datetimeoffsetDataType);

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
			public string FirstName { get; set; }

			[Column]
			public string LastName { get; set; }

			[Column]
			public string MiddleName { get; set; }

			[Column]
			public Gender Gender { get; set; }
		}

		[Test, IdentityInsertMergeDataContextSource]
		public void Issue1007OnNewAPI(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
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

		[Ignore("Incorrect SQL generated by old API")]
		[Test, IdentityInsertMergeDataContextSource]
		public void Issue1007OnOldAPIv1(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var table = db.GetTable<TestMappingWithIdentity>();
				table.Delete();

				db.Insert(new TestMappingWithIdentity());

				var lastId = table.Select(_ => _.Id).Max();

				var source = new[]
				{
					new TestMappingWithIdentity()
					{
						Id = lastId,
						Field = 10
					}
				};

				var rows = db.Merge(source);

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, result.Count);

				var newRecord = new TestMapping1();

				Assert.AreEqual(lastId, result[0].Id);
				Assert.AreEqual(10, result[0].Field);
			}
		}

		// ASE: not supported by old Merge API
		[Test, IdentityInsertMergeDataContextSource(ProviderName.Sybase)]
		public void Issue1007OnOldAPIv2(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				db.Doctor.Delete();
				db.Patient.Delete();

				var table = db.GetTable<Person1007>();
				table.Delete();

				db.Insert(new Person1007()
				{
					FirstName = "first name",
					LastName = "last name",
					Gender = Gender.Female
				});

				var lastId = table.Select(_ => _.ID).Max();

				var source = new[]
				{
					new Person1007()
					{
						FirstName = "first name",
						LastName = "updated",
						Gender = Gender.Male,
						ID = 10
					}
				};

				var rows = db.Merge(source);

				var result = table.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, result.Count);

				var newRecord = new TestMapping1();

				Assert.AreEqual(lastId, result[0].ID);
				Assert.AreEqual("first name", result[0].FirstName);
				Assert.AreEqual("updated", result[0].LastName);
				Assert.AreEqual(Gender.Male, result[0].Gender);
			}
		}
		#endregion
	}
}
