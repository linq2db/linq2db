#if NET5_0_OR_GREATER
using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3660Tests : TestBase
	{
		public class TestEntity
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public Half C { get; set; }
			[Column]
			public Half? F { get; set; }
#if NET7_0_OR_GREATER
			[Column]
			public Int128 A { get; set; }
			[Column]
			public UInt128 B { get; set; }
			[Column]
			public Int128? D { get; set; }
			[Column]
			public UInt128? E { get; set; }
#endif
		}

		[Test]
		public void TestNewNet5NumericTypes([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			using var testEnityTable = db.CreateLocalTable<TestEntity>(
				[
				new TestEntity() { Id = 1, C = (sbyte)1 },
				new TestEntity() { Id = 2, C = (sbyte)1, F = (sbyte)1 },
				new TestEntity() { Id = 3, C = Half.MaxValue },
				new TestEntity() { Id = 4, C = Half.MinValue },
				]);

			AssertQuery(testEnityTable);
		}

#if NET7_0_OR_GREATER
		[Test]
		public void TestNewNet7NumericTypes([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			using var testEnityTable = db.CreateLocalTable<TestEntity>(
				[
				new TestEntity() { Id = 1, A = 1,  B = 1,  C = (sbyte)1 },
				new TestEntity() { Id = 2, A = 1,  B = 1,  C = (sbyte)1, D = 1, E = 1, F = (sbyte)1 },
				new TestEntity() { Id = 3, A = Int128.MaxValue,  B = UInt128.MaxValue,  C = Half.MaxValue },
				new TestEntity() { Id = 4, A = Int128.MinValue,  B = UInt128.MinValue,  C = Half.MinValue },
				]);

			AssertQuery(testEnityTable);
		}
#endif
	}
}
#endif
