using System;
using System.Linq;

using FluentAssertions;

using LinqToDB.Mapping;

using NUnit.Framework;

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

			query.GetStatement().CollectParameters().Should().HaveCount(0);

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

			query.GetStatement().CollectParameters().Should().HaveCount(context.IsUseParameters() ? 1 : 0);

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

			query.GetStatement().CollectParameters().Should().HaveCount(0);

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

			query.GetStatement().CollectParameters().Should().HaveCount(context.IsUseParameters() ? 1 : 0);

			AssertQuery(query);
		}

		[Test]
		public void static_readonly_access_readonly_members ([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerClassReadonly.Id && e.Id == ConstantsContainer.InnerClassReadonly.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Should().HaveCount(0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerClassReadonly.Id && e.Id == ConstantsContainer.InnerClassReadonly.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}

#if NET6_0_OR_GREATER
		[Test]
		public void static_readonly_field_readonly_struct([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerReadonlyStructure.Id && e.Id == ConstantsContainer.InnerReadonlyStructure.GetId(1)
				select e;

			query.GetStatement().CollectParameters().Should().HaveCount(0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerReadonlyStructure.Id && e.Id == ConstantsContainer.InnerReadonlyStructure.GetId(1)
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}

		[Test]
		public void static_field_readonly_struct([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerNonReadonlyStructure.Id && e.Id == ConstantsContainer.InnerNonReadonlyStructure.GetId(1)
				select e;

			query.GetStatement().CollectParameters().Should().HaveCount(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerNonReadonlyStructure.Id && e.Id == ConstantsContainer.InnerNonReadonlyStructure.GetId(1)
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}
#endif

		[Test]
		public void static_field_readonly_members([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());
			var query =
				from e in table
				where e.Id == ConstantsContainer.InnerClassNonReadonly.Id && e.Id == ConstantsContainer.InnerClassNonReadonly.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Should().HaveCount(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == ConstantsContainer.InnerClassNonReadonly.Id && e.Id == ConstantsContainer.InnerClassNonReadonly.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}

		[Test]
		public void instance_readonly_members([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestConstantsData.Seed());

			var innerClass = new ConstantsContainer.InnerClass(1) {InitOnlyId = 1};

			var query =
				from e in table
				where e.Id == innerClass.Id && e.Id == innerClass.InitOnlyId
				select e;

			query.GetStatement().CollectParameters().Should().HaveCount(context.IsUseParameters() ? 2 : 0);
			AssertQuery(query);

			var query2 =
				from e in table
				where e.Id == innerClass.Id && e.Id == innerClass.InitOnlyId
				select e;

			var cacheMissCount = query2.GetCacheMissCount();

			AssertQuery(query);

			query2.GetCacheMissCount().Should().Be(cacheMissCount);
		}

	}
}
