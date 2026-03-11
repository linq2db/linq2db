using System;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ConstructorTests : TestBase
	{
		[Table("ConstructorTestTable")]
		public abstract class AbstractEntity
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column]
			public string? Value { get; set; }
		}

		public class WithPublicConstructor : AbstractEntity
		{
			private WithPublicConstructor()
			{
			}

			public WithPublicConstructor(int some)
			{
			}
		}

		public class WithPrivateConstructor : AbstractEntity
		{
			private WithPrivateConstructor()
			{
			}

			public WithPrivateConstructor(int some)
			{
			}
		}

		public class WithProtectedConstructor : AbstractEntity
		{
			protected WithProtectedConstructor()
			{
			}

			public WithProtectedConstructor(int some)
			{
			}
		}

		public class WithAmbiguousConstructor : AbstractEntity
		{
			public WithAmbiguousConstructor(int id)
			{
				Id = id;
			}

			public WithAmbiguousConstructor(string value)
			{
				Value = value;
			}
		}

		public class WithManyConstructors : AbstractEntity
		{
			public WithManyConstructors()
			{

			}

			public WithManyConstructors(int id)
			{
				Id = id;
			}

			public WithManyConstructors(string value)
			{
				Value = value;
			}
		}

		[Table("ConstructorTestTable")]
		public class WithOnlyPrivateConstructor
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; }

			[Column]
			public string? Value { get; }

			private WithOnlyPrivateConstructor(int id, string value)
			{
				Id    = id;
				Value = value;
			}

			public static WithOnlyPrivateConstructor Create(int id) => new(id, "Some");
		}

		[Table("ConstructorTestTable")]
		public class WithOnlyProtectedConstructor
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; }

			[Column]
			public string? Value { get; }

			protected WithOnlyProtectedConstructor(int id, string value)
			{
				Id    = id;
				Value = value;
			}

			public static WithOnlyProtectedConstructor Create(int id) => new(id, "Some");
		}

		[Table("ConstructorTestTable")]
		public class WithPublicAndProtectedConstructor
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; }

			[Column]
			public string? Value { get; }

			protected WithPublicAndProtectedConstructor(int id)
			{
				Id    = id;
			}
			
			public WithPublicAndProtectedConstructor(int id, string value)
			{
				Id    = id;
				Value = value;
			}

			public static WithPublicAndProtectedConstructor Create(int id) => new(id, "Some");
		}

		[Test]
		public void TestPublicConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new WithPublicConstructor(0) { Id = 1, Value = "Some" } });
			var obj = table.First();
			obj.Id.ShouldBe(1);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestPrivateConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new WithPrivateConstructor(0) { Id = 1, Value = "Some" } });
			var obj = table.First();
			obj.Id.ShouldBe(1);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestProtectedConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new WithProtectedConstructor(0) { Id = 1, Value = "Some" } });
			var obj = table.First();
			obj.Id.ShouldBe(1);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestAmbiguousConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new WithAmbiguousConstructor(0) { Id = 1, Value = "Some" } });
			var act = () => table.First();
			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void TestManyConstructors([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new WithManyConstructors(0) { Id = 1, Value = "Some" } });
			var obj = table.First();
			obj.Id.ShouldBe(1);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestPrivateParameterizedConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new []{ WithOnlyPrivateConstructor.Create(5)});

			var obj = table.First();
			obj.Id.ShouldBe(5);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestProtectedParameterizedConstructor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new []{ WithOnlyProtectedConstructor.Create(5)});

			var obj = table.First();
			obj.Id.ShouldBe(5);
			obj.Value.ShouldBe("Some");
		}

		[Test]
		public void TestPublicAndProtectedParameterizedConstructors([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(new []{ WithPublicAndProtectedConstructor.Create(5)});

			var obj = table.First();
			obj.Id.ShouldBe(5);
			obj.Value.ShouldBe("Some");
		}
	}
}
