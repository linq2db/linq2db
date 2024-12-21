using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
//	[Order(10101)]
	public partial class MergeTests : TestBase
	{
		[AttributeUsage(AttributeTargets.Parameter)]
		public class MergeNotMatchedBySourceDataContextSourceAttribute : IncludeDataSourcesAttribute
		{
			static string[] Supported = new[]
			{
				TestProvName.AllFirebird5Plus,
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllPostgreSQL17Plus,
			}.SelectMany(_ => _.Split(',')).ToArray();

			public MergeNotMatchedBySourceDataContextSourceAttribute(params string[] except)
				: base(true, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}

			public MergeNotMatchedBySourceDataContextSourceAttribute(bool excludeLinqService, params string[] except)
				: base(!excludeLinqService, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[AttributeUsage(AttributeTargets.Parameter)]
		public class MergeDataContextSourceAttribute : DataSourcesAttribute
		{
			public static List<string> Unsupported = new[]
			{
				TestProvName.AllAccess,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2005,
				TestProvName.AllClickHouse,
				TestProvName.AllPostgreSQL14Minus,
				TestProvName.AllMySql,
			}.SelectMany(_ => _.Split(',')).ToList();

			public MergeDataContextSourceAttribute(params string[] except)
				: base(true, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}

			public MergeDataContextSourceAttribute(bool includeLinqService, params string[] except)
				: base(includeLinqService, Unsupported.Concat(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[AttributeUsage(AttributeTargets.Parameter)]
		public class IdentityInsertMergeDataContextSourceAttribute : IncludeDataSourcesAttribute
		{
			static string[] Supported = new[]
			{
				TestProvName.AllSybase,
				TestProvName.AllSqlServer2008Plus,
				TestProvName.AllPostgreSQL15Plus,
			}.SelectMany(_ => _.Split(',')).ToArray();

			public IdentityInsertMergeDataContextSourceAttribute(params string[] except)
				: base(true, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}

			public IdentityInsertMergeDataContextSourceAttribute(bool includeLinqService, params string[] except)
				: base(includeLinqService, Supported.Except(except.SelectMany(_ => _.Split(','))).ToArray())
			{
			}
		}

		[Table("merge1")]
		internal sealed class TestMapping1
		{
			[Column("Id")]
			[PrimaryKey]
			public int Id;

			[Column("Field1")]
			public int? Field1;

			[Column("Field2")]
			public int? Field2;

			[Column("Field3", SkipOnInsert = true)]
			public int? Field3;

			[Column("Field4", SkipOnUpdate = true)]
			public int? Field4;

			[Column("Field5", SkipOnInsert = true, SkipOnUpdate = true)]
			public int? Field5;

			[Column("fake", Configuration = "Other")]
			public int Fake;
		}

		[Table("TestMergeIdentity", Configuration = ProviderName.Sybase)]
		[Table("TestMergeIdentity", Configuration = ProviderName.SqlServer)]
		[Table("TestMergeIdentity", Configuration = ProviderName.PostgreSQL)]
		sealed class TestMappingWithIdentity
		{
			[Column("Id", SkipOnInsert = true, IsIdentity = true)]
			public int Id;

			[Column("Field")]
			public int? Field;
		}

		[Table("merge2")]
		internal sealed class TestMapping2
		{
			[Column("Id")]
			[PrimaryKey]
			public int OtherId;

			[Column("Field1", SkipOnInsert = true)]
			public int? OtherField1;

			[Column("Field2", SkipOnInsert = true, SkipOnUpdate = true)]
			public int? OtherField2;

			[Column("Field3", SkipOnUpdate = true)]
			public int? OtherField3;

			[Column("Field4")]
			public int? OtherField4;

			[Column("Field5")]
			public int? OtherField5;

			[Column("fake", Configuration = "Other")]
			public int OtherFake;
		}

#pragma warning disable NUnit1028 // The non-test method is public
		internal static ITable<TestMapping1> GetTarget(IDataContext db)
#pragma warning restore NUnit1028 // The non-test method is public
		{
			return db.GetTable<TestMapping1>().TableName("TestMerge1");
		}

#pragma warning disable NUnit1028 // The non-test method is public
		internal static ITable<TestMapping1> GetSource1(IDataContext db)
#pragma warning restore NUnit1028 // The non-test method is public
		{
			return db.GetTable<TestMapping1>().TableName("TestMerge2");
		}

		private static ITable<TestMapping2> GetSource2(IDataContext db)
		{
			return db.GetTable<TestMapping2>().TableName("TestMerge2");
		}

		private void AssertRow(TestMapping1 expected, TestMapping1 actual, int? exprected3, int? exprected4)
		{
			Assert.Multiple(() =>
			{
				Assert.That(actual.Id, Is.EqualTo(expected.Id));
				Assert.That(actual.Field1, Is.EqualTo(expected.Field1));
				Assert.That(actual.Field2, Is.EqualTo(expected.Field2));
				Assert.That(actual.Field3, Is.EqualTo(exprected3));
				Assert.That(actual.Field4, Is.EqualTo(exprected4));
				Assert.That(actual.Field5, Is.Null);
			});
		}

#pragma warning disable NUnit1028 // The non-test method is public
		internal static void PrepareData(IDataContext db)
#pragma warning restore NUnit1028 // The non-test method is public
		{
			using (new DisableLogging())
			{
				GetTarget(db).Delete();
				foreach (var record in InitialTargetData)
				{
					db.Insert(record, "TestMerge1");
				}

				GetSource1(db).Delete();
				foreach (var record in InitialSourceData)
				{
					db.Insert(record, "TestMerge2");
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

		[Test]
		public void TestDataGenerationTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var result1 = GetTarget(db). OrderBy(_ => _.Id).ToList();
				var result2 = GetSource1(db).OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(result1, Has.Count.EqualTo(4));
					Assert.That(result2, Has.Count.EqualTo(4));
				});

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
			if (context.IsAnyOf(TestProvName.AllSybase))
				Assert.That(expected, Is.LessThanOrEqualTo(actual));
			else if (context.IsAnyOf(TestProvName.AllOracleNative) && actual == -1)
			{ }
			else
				Assert.That(actual, Is.EqualTo(expected));
		}
	}
}
