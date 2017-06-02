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
	[TestFixture]
	public partial class MergeTests : TestBase
	{
		public class MergeUpdateWithDeleteDataContextSourceAttribute : IncludeDataContextSourceAttribute
		{
			public MergeUpdateWithDeleteDataContextSourceAttribute()
				: base(false, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative)
			{
			}
		}

		public class MergeBySourceDataContextSourceAttribute : IncludeDataContextSourceAttribute
		{
			public MergeBySourceDataContextSourceAttribute()
				: base(false, TestProvName.SqlAzure, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)
			{
			}
		}

		public class MergeDataContextSourceAttribute : DataContextSourceAttribute
		{
			private static string[] Unsupported = new []
			{
				ProviderName.Access,
				ProviderName.SqlCe,
				ProviderName.SQLite,
				TestProvName.SQLiteMs,
				ProviderName.SqlServer,
				ProviderName.SqlServer2000,
				ProviderName.SqlServer2005,
				ProviderName.PostgreSQL,
				ProviderName.PostgreSQL92,
				ProviderName.PostgreSQL93,
				ProviderName.MySql,
				TestProvName.MySql57,
				TestProvName.MariaDB
			};

			public MergeDataContextSourceAttribute(params string[] except)
				: base(false, Unsupported.Concat(except).ToArray())
			{
			}
		}

		[Table("merge1")]
		class TestMapping1
		{
			[Column("id")]
			[PrimaryKey]
			public int Id;

			[Column("field1")]
			public int? Field1;

			[Column("field2")]
			public int? Field2;

			[Column("field3", SkipOnInsert = true)]
			public int? Field3;

			[Column("field4", SkipOnUpdate = true)]
			public int? Field4;

			[Column("field5", SkipOnInsert = true, SkipOnUpdate = true)]
			public int? Field5;

			[Column("fake", Configuration = "Other")]
			public int Fake;
		}

		[Table("merge2")]
		class TestMapping2
		{
			[Column("id")]
			[PrimaryKey]
			public int OtherId;

			[Column("field1", SkipOnInsert = true)]
			public int? OtherField1;

			[Column("field2", SkipOnInsert = true, SkipOnUpdate = true)]
			public int? OtherField2;

			[Column("field3", SkipOnUpdate = true)]
			public int? OtherField3;

			[Column("field4")]
			public int? OtherField4;

			[Column("field5")]
			public int? OtherField5;

			[Column("fake", Configuration = "Other")]
			public int OtherFake;
		}

		private static ITable<TestMapping1> GetTarget(TestDataConnection db)
		{
			return db.GetTable<TestMapping1>().TableName("testmerge1");
		}

		private static ITable<TestMapping1> GetSource1(TestDataConnection db)
		{
			return db.GetTable<TestMapping1>().TableName("testmerge2");
		}

		private static ITable<TestMapping2> GetSource2(TestDataConnection db)
		{
			return db.GetTable<TestMapping2>().TableName("testmerge2");
		}

		private void AssertRow(TestMapping1 expected, TestMapping1 actual, int? exprected3, int? exprected4)
		{
			Assert.AreEqual(expected.Id, actual.Id);
			Assert.AreEqual(expected.Field1, actual.Field1);
			Assert.AreEqual(expected.Field2, actual.Field2);
			Assert.AreEqual(exprected3, actual.Field3);
			Assert.AreEqual(exprected4, actual.Field4);
			Assert.IsNull(actual.Field5);
		}

		private void PrepareData(TestDataConnection db)
		{
			using (new DisableLogging())
			{
				GetTarget(db).Delete();
				foreach (var record in InitialTargetData)
				{
					db.Insert(record, "testmerge1");
				}

				GetSource1(db).Delete();
				foreach (var record in InitialSourceData)
				{
					db.Insert(record, "testmerge2");
				}
			}
		}

		private static readonly TestMapping1[] InitialTargetData = new[]
				{
			new TestMapping1() { Id = 1                                                                   },
			new TestMapping1() { Id = 2, Field1 = 2,             Field3 = 101                             },
			new TestMapping1() { Id = 3,             Field2 = 3,               Field4 = 203               },
			new TestMapping1() { Id = 4, Field1 = 5, Field2 = 6,                             Field5 = 304 },
		};

		private static readonly TestMapping1[] InitialSourceData = new[]
		{
			new TestMapping1() { Id = 3,              Field2 = 3,  Field3 = 113                             },
			new TestMapping1() { Id = 4, Field1 = 5,  Field2 = 7,                Field4 = 214               },
			new TestMapping1() { Id = 5, Field1 = 10, Field2 = 4,                             Field5 = 315 },
			new TestMapping1() { Id = 6,                           Field3 = 116, Field4 = 216, Field5 = 316 },
		};

		private static IEnumerable<TestMapping2> GetInitialSourceData2()
		{
			foreach (var record in InitialSourceData)
			{
				yield return new TestMapping2()
				{
					OtherId = record.Id,
					OtherField1 = record.Field1,
					OtherField2 = record.Field2,
					OtherField3 = record.Field3,
					OtherField4 = record.Field4,
					OtherField5 = record.Field5,
					OtherFake = record.Fake
				};
			}
		}

		[DataContextSource(false)]
		public void TestDataGenerationTest(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var result1 = GetTarget(db).OrderBy(_ => _.Id).ToList();
				var result2 = GetSource1(db).OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(4, result1.Count);
				Assert.AreEqual(4, result2.Count);

				AssertRow(InitialTargetData[0], result1[0], null, null);
				AssertRow(InitialTargetData[1], result1[1], null, null);
				AssertRow(InitialTargetData[2], result1[2], null, 203);
				AssertRow(InitialTargetData[3], result1[3], null, null);

				AssertRow(InitialSourceData[0], result2[0], null, null);
				AssertRow(InitialSourceData[1], result2[1], null, 214);
				AssertRow(InitialSourceData[2], result2[2], null, null);
				AssertRow(InitialSourceData[3], result2[3], null, 216);
			}
		}

		private void AssertRowCount(int expected, int actual, string context)
		{
			// another sybase quirk, nothing surprising
			if (context == ProviderName.Sybase)
				Assert.LessOrEqual(expected, actual);
			else
				Assert.AreEqual(expected, actual);
		}
	}
}
