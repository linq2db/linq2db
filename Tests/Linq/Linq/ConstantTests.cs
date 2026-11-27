using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public static class ConstantsContainer
	{
		public class InnerClass
		{
			public InnerClass(int id)
			{
				Id = id;
			}

			public readonly int Id;

			public int InitOnlyId { get; init; }
		}

		public readonly struct InnerReadonlyStruct
		{
			public InnerReadonlyStruct(int id)
			{
				Id = id;
			}

			public int Id { get; }

			public int GetId(int increment) => Id + increment;
		}

		public static readonly Guid GuidReadonly = TestData.Guid1;
		public static Guid GuidNonReadonly = TestData.Guid1;

		public static readonly string StringReadOnly = "StrValue";
		public static string StringNonReadOnly = "StrValue";

		public static string ReadOnlyStringProp => "StrValue";

		public static readonly InnerClass InnerClassReadonly = new InnerClass(1) {InitOnlyId = 1};
		public static InnerClass InnerClassNonReadonly = new InnerClass(1) {InitOnlyId = 1};

		public static readonly InnerReadonlyStruct InnerReadonlyStructure = new InnerReadonlyStruct(1);
		public static InnerReadonlyStruct InnerNonReadonlyStructure = new InnerReadonlyStruct(1);
	}

	[TestFixture]
	public class ConstantTests : TestBase
	{
		class TestConstantsData
		{
			[PrimaryKey]
			public int Id { get; set; }

			public Guid  GuidValue         { get; set; }
			public Guid? GuidNullableValue { get; set; }

			public string StringValue { get; set; } = default!;

			public static TestConstantsData[] Seed()
			{
				return [new TestConstantsData
				{
					Id = 1, 
					GuidValue = ConstantsContainer.GuidReadonly, 
					GuidNullableValue = ConstantsContainer.GuidReadonly, 
					StringValue = ConstantsContainer.StringReadOnly
				}];
			}
		}

		[Test]
		public void static_readonly_field ([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var query =
				from e in table
				where e.GuidValue == ConstantsContainer.GuidReadonly && e.GuidNullableValue == ConstantsContainer.GuidReadonly
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(0);

			AssertQuery(query);
		}
		
		[Test]
		public void static_field([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var query =
				from e in table
				where e.GuidValue == ConstantsContainer.GuidNonReadonly && e.GuidNullableValue == ConstantsContainer.GuidNonReadonly
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(context.IsUseParameters() ? 1 : 0);

			AssertQuery(query);
		}

		[Test]
		public void static_readonly_field_concatenation ([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var query =
				from e in table
				where e.StringValue + "1" == ConstantsContainer.StringReadOnly + "1"
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(0);

			AssertQuery(query);
		}

		[Test]
		public void static_readonly_concatenation ([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var query =
				from e in table
				where e.StringValue + "1" == ConstantsContainer.StringNonReadOnly + "1"
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(context.IsUseParameters() ? 1 : 0);

			AssertQuery(query);
		}

		[Test, QueryCacheTest]
		public void static_readonly_access_readonly_members ([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerClassReadonly.Id && e.Id == ConstantsContainer.InnerClassReadonly.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerClassReadonly.Id && e.Id == ConstantsContainer.InnerClassReadonly.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

#if SUPPORTS_READONLY
		[Test, QueryCacheTest]
		public void static_readonly_field_readonly_struct([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerReadonlyStructure.Id && e.Id == ConstantsContainer.InnerReadonlyStructure.GetId(1)
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerReadonlyStructure.Id && e.Id == ConstantsContainer.InnerReadonlyStructure.GetId(1)
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		[Test, QueryCacheTest]
		public void static_field_readonly_struct([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerNonReadonlyStructure.Id && e.Id == ConstantsContainer.InnerNonReadonlyStructure.GetId(1)
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerNonReadonlyStructure.Id && e.Id == ConstantsContainer.InnerNonReadonlyStructure.GetId(1)
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}
#endif

		[Test, QueryCacheTest]
		public void static_field_readonly_members([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerClassNonReadonly.Id && e.Id == ConstantsContainer.InnerClassNonReadonly.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerClassNonReadonly.Id && e.Id == ConstantsContainer.InnerClassNonReadonly.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		[Test, QueryCacheTest]
		public void instance_readonly_members([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var innerClass = new ConstantsContainer.InnerClass(1) {InitOnlyId = 1};

			var query =
				from e in table
				where e.Id == innerClass.Id && e.Id == innerClass.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Length.ShouldBe(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == innerClass.Id && e.Id == innerClass.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		[Test]
		public void NullableTypeConstantDefininition([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var one = db.SelectQuery(() => 1 as int?).First();

			Assert.That(one, Is.EqualTo(1 as int?));
		}

		[Test]
		public void NullableTypeConstantDefininition2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var one = db.SelectQuery(() => (int?)1).First();

			Assert.That(one, Is.EqualTo(1 as int?));
		}

		[Test]
		public void NullableTypeConstantDefininitionAsSqlThrows([DataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(() =>
				{
					db.SelectQuery(() => Sql.AsSql(1 as int?)).First();
				}, Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("The LINQ expression '(1 as int?)' could not be converted to SQL."))
			;
		}

		[Test]
		public void NullableTypeConstantDefininitionAsSql2([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var one = db.SelectQuery(() => Sql.AsSql((int?)1)).First();

			Assert.That(one, Is.EqualTo(1 as int?));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void SByteMinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = sbyte.MinValue;
			var max = sbyte.MaxValue;

			db.Select(() => Sql.AsSql(sbyte.MinValue)).ShouldBe(sbyte.MinValue);
			db.Select(() => Sql.AsSql(sbyte.MaxValue)).ShouldBe(sbyte.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2,   ErrorMessage = "Unable to cast object of type 'System.SByte' to type 'System.Byte[]'")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSapHana,    ErrorMessage = "No mapping exists from DbType SByte to a known")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void SByteMinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = sbyte.MinValue;
			var max = sbyte.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void ByteMinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = byte.MinValue;
			var max = byte.MaxValue;

			db.Select(() => Sql.AsSql(byte.MinValue)).ShouldBe(byte.MinValue);
			db.Select(() => Sql.AsSql(byte.MaxValue)).ShouldBe(byte.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void ByteMinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = byte.MinValue;
			var max = byte.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Int16MinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = short.MinValue;
			var max = short.MaxValue;

			db.Select(() => Sql.AsSql(short.MinValue)).ShouldBe(short.MinValue);
			db.Select(() => Sql.AsSql(short.MaxValue)).ShouldBe(short.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Int16MinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = short.MinValue;
			var max = short.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void UInt16MinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = ushort.MinValue;
			var max = ushort.MaxValue;

			db.Select(() => Sql.AsSql(ushort.MinValue)).ShouldBe(ushort.MinValue);
			db.Select(() => Sql.AsSql(ushort.MaxValue)).ShouldBe(ushort.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.SapHanaNative, ErrorMessage = "Index was outside the bounds of the array")]
		[ActiveIssue(5973, Configuration = ProviderName.SapHanaOdbc,   ErrorMessage = "No mapping exists from DbType UInt16 to a known OdbcType")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void UInt16MinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = ushort.MinValue;
			var max = ushort.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Int32MinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = int.MinValue;
			var max = int.MaxValue;

			db.Select(() => Sql.AsSql(int.MinValue)).ShouldBe(int.MinValue);
			db.Select(() => Sql.AsSql(int.MaxValue)).ShouldBe(int.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Int32MinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = int.MinValue;
			var max = int.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void UInt32MinMaxInlined([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = uint.MinValue;
			var max = uint.MaxValue;

			db.Select(() => Sql.AsSql(uint.MinValue)).ShouldBe(uint.MinValue);
			db.Select(() => Sql.AsSql(uint.MaxValue)).ShouldBe(uint.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void UInt32MinMaxAsParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = uint.MinValue;
			var max = uint.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Int64MinMaxInlined([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = long.MinValue;
			var max = long.MaxValue;

			db.Select(() => Sql.AsSql(long.MinValue)).ShouldBe(long.MinValue);
			db.Select(() => Sql.AsSql(long.MaxValue)).ShouldBe(long.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Int64MinMaxAsParameter([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			var min = long.MinValue;
			var max = long.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSQLite,        ErrorMessage = "Arithmetic operation resulted in an overflow")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSqlServer,     ErrorMessage = "Arithmetic overflow error converting numeric to data type numeric")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSybase,        ErrorMessage = "Arithmetic overflow during explicit conversion")]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebird4Plus, ErrorMessage = "numeric overflow")]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebirdLess4, ErrorMessage = "Token unknown")]
		[ActiveIssue(5973, Configuration = TestProvName.AllPostgreSQL,    BaselinesMismatch = true)]
		public void UInt64MinMaxInlined([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = ulong.MinValue;
			var max = ulong.MaxValue;

			db.Select(() => Sql.AsSql(ulong.MinValue)).ShouldBe(ulong.MinValue);
			db.Select(() => Sql.AsSql(ulong.MaxValue)).ShouldBe(ulong.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebird, ErrorMessage = "Value was either too large or too small for an Int64")]
		[ActiveIssue(5973, Configuration = ProviderName.SQLiteMS,    ErrorMessage = "Arithmetic operation resulted in an overflow")]
		public void UInt64MinMaxAsParameter([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			var min = ulong.MinValue;
			var max = ulong.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}
	}
}
