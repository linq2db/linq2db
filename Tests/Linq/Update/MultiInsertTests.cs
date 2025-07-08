using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Shouldly;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class MultiInsertTests : TestBase
	{
		[Test]
		public void Insert([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var dest1 = db.CreateLocalTable<Dest1>();
			using var dest2 = db.CreateLocalTable<Dest2>();

			int count = db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.Into(
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.Into(
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.Into(
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				)
				.Insert();

			count.ShouldBe(3);
			dest1.Count().ShouldBe(2);
			dest2.Count(x => x.ID == 1003).ShouldBe(1);
		}

		[Test]
		public async Task InsertAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			
			using var dest1 = db.CreateLocalTable<Dest1>();
			using var dest2 = db.CreateLocalTable<Dest2>();
			
			int count = await db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.Into(
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.Into(
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.Into(
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				)
				.InsertAsync();

			count.ShouldBe(3);
			(await dest1.CountAsync()).ShouldBe(2);
			(await dest1.CountAsync()).ShouldBe(2);
			(await dest2.CountAsync(x => x.ID == 1003)).ShouldBe(1);
		}

		[Test]
		public void InsertAll([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var dest1 = db.CreateLocalTable<Dest1>();
			using var dest2 = db.CreateLocalTable<Dest2>();

			int count = db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.When(
					x => x.N > 40,
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => x.N < 40,
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.When(
					x => true,
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				)
				.InsertAll();

			count.ShouldBe(2);
			dest1.Count().ShouldBe(1);

			dest1.Count(x => x.ID == 1001).ShouldBe(1);
			dest2.Count(x => x.ID == 1003).ShouldBe(1);
		}

		[Test]
		public void InsertAllFromQuery([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			var sourceData = new TestSource[] {new TestSource {ID = 1000, N = 42}, new TestSource {ID = 1001, N = 20},};

			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(sourceData);
			using var dest1  = db.CreateLocalTable<Dest1>();
			using var dest2  = db.CreateLocalTable<Dest2>();

			var sourceQuery =
				from s in source
				join s2 in source on s.ID equals s2.ID
				select s;

			int count = 
				sourceQuery
				.MultiInsert()
				.When(
					x => x.N > 40,
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => x.N < 40,
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.When(
					x => true,
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				)
				.InsertAll();

			count.ShouldBe(4);
			dest1.Count().ShouldBe(2);
			dest2.Count().ShouldBe(2);

			dest1.Count(x => x.ID == 1001).ShouldBe(1);
			dest2.Count(x => x.ID == 1003).ShouldBe(1);
		}

		[Test]
		public async Task InsertAllAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			
			using var dest1 = db.CreateLocalTable<Dest1>();
			using var dest2 = db.CreateLocalTable<Dest2>();
			
			int count = await db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.When(
					x => x.N > 40,
					dest1,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => x.N < 40,
					dest1,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.When(
					x => true,
					dest2,
					x => new Dest2 { ID = x.ID + 3, Int = x.ID + 1 }
				)
				.InsertAllAsync();

			count.ShouldBe(2);
			(await dest1.CountAsync()).ShouldBe(1);
			(await dest1.CountAsync(x => x.ID == 1001)).ShouldBe(1);
			(await dest2.CountAsync(x => x.ID == 1003)).ShouldBe(1);
		}

		[Test]
		public void InsertFirst([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db   = GetDataContext(context);
			using var dest = db.CreateLocalTable<Dest1>();
			
			int count = db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.When(
					x => x.N < 40,
					dest,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => false,
					dest,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.Else(
					dest,
					x => new Dest1 { ID = x.ID + 3, Value = x.N }
				)
				.InsertFirst();

			count.ShouldBe(1);
			dest.Count().ShouldBe(1);
			dest.Count(x => x.ID == 1003).ShouldBe(1);
		}

		[Test]
		public async Task InsertFirstAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);

			using var dest = db.CreateLocalTable<Dest1>();

			int count = await db
				.SelectQuery(() => new { ID = 1000, N = (short)42 })
				.MultiInsert()
				.When(
					x => x.N < 40,
					dest,
					x => new Dest1 { ID = x.ID + 1, Value = x.N }
				)
				.When(
					x => false,
					dest,
					x => new Dest1 { ID = x.ID + 2, Value = x.N }
				)
				.Else(
					dest,
					x => new Dest1 { ID = x.ID + 3, Value = x.N }
				)
				.InsertFirstAsync();

			count.ShouldBe(1);
			(await dest.CountAsync()).ShouldBe(1);
			(await dest.CountAsync(x => x.ID == 1003)).ShouldBe(1);
		}

		[Test]
		public void ParametersInSource(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db   = GetDataContext(context);			
			using var dest = db.CreateLocalTable<Dest1>();
			
			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => x.Value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = x.Value }
				)
				.When(
					x => x.Value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = x.Value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = command.InsertAll();
			var record = dest.Where(_ => _.ID > 1000).Single();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public async Task ParametersInSourceAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);

			using var dest = db.CreateLocalTable<Dest1>();
		
			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => x.Value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = x.Value }
				)
				.When(
					x => x.Value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = x.Value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = await command.InsertAllAsync();
			var record = await dest.Where(_ => _.ID > 1000).SingleAsync();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public void ParametersInCondition(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db   = GetDataContext(context);
			using var dest = db.CreateLocalTable<Dest1>();
		
			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = x.Value }
				)
				.When(
					x => value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = x.Value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = command.InsertAll();
			var record = dest.Where(_ => _.ID > 1000).Single();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public async Task ParametersInConditionAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);

			using var dest = db.CreateLocalTable<Dest1>();
		
			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = x.Value }
				)
				.When(
					x => value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = x.Value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = await command.InsertAllAsync();
			var record = await dest.Where(_ => _.ID > 1000).SingleAsync();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public void ParametersInInsert(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db   = GetDataContext(context);
			using var dest = db.CreateLocalTable<Dest1>();

			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => x.Value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = value }
				)
				.When(
					x => x.Value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = command.InsertAll();
			var record = dest.Where(_ => _.ID > 1000).Single();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public async Task ParametersInInsertAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);

			using var dest = db.CreateLocalTable<Dest1>();

			int id1 = 3000, id2 = 4000;

			var command = db
				.SelectQuery(() => new { Value = value })
				.MultiInsert()
				.When(
					x => x.Value == null,
					dest,
					x => new Dest1 { ID = id1, StringValue = value }
				)
				.When(
					x => x.Value != null,
					dest,
					x => new Dest1 { ID = id2, StringValue = value }
				);

			// Perform a simple INSERT ALL with parameters
			int count  = await command.InsertAllAsync();
			var record = await dest.Where(_ => _.ID > 1000).SingleAsync();

			count.ShouldBe(1);
			record.ID.ShouldBe(value == null ? id1 : id2);
			record.StringValue.ShouldBe(value);
		}

		[Test]
		public void Expressions([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db   = GetDataContext(context);
			using var dest = db.CreateLocalTable<Dest1>();
			
			// Perform a simple INSERT ALL with some expressions
			int count = InsertAll(
				() => new TestSource { ID = 3000, N = (short)42 },
				x  => x.N < 0,
				x  => new Dest1 { ID = 3002, Value = x.N });

			count.ShouldBe(1);
			dest.Count().ShouldBe(1);
			dest.Count(x => x.ID == 3002).ShouldBe(1);

			// Perform the same INSERT ALL with different expressions
			count = InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			count.ShouldBe(2);
			dest.Count(x => x.ID == 4001 || x.ID == 4002).ShouldBe(2);

			int InsertAll(
				Expression<Func<TestSource>>                source,
				Expression<Func<TestSource, bool>>          condition1,
				Expression<Func<TestSource, Dest1>> setter2)
			{
				return db
					.SelectQuery(source)
					.MultiInsert()
					.When(
						condition1,
						dest,
						x => new Dest1 { ID = x.ID + 1, Value = x.N }
					)
					.When(
						x => x.N > 40,
						dest,
						setter2
					)
					.InsertAll();
			}
		}

		[Test]
		public async Task ExpressionsAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			
			using var dest = db.CreateLocalTable<Dest1>();

			// Perform a simple INSERT ALL with some expressions
			int count = await InsertAll(
				() => new TestSource { ID = 3000, N = (short)42 },
				x  => x.N < 0,
				x  => new Dest1 { ID = 3002, Value = x.N });

			count.ShouldBe(1);
			(await dest.CountAsync()).ShouldBe(1);
			(await dest.CountAsync(x => x.ID == 3002)).ShouldBe(1);

			// Perform the same INSERT ALL with different expressions
			count = await InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			count.ShouldBe(2);
			(await dest.CountAsync()).ShouldBe(3);
			(await dest.CountAsync(x => x.ID == 4001 || x.ID == 4002)).ShouldBe(2);

			Task<int> InsertAll(
				Expression<Func<TestSource>>        source,
				Expression<Func<TestSource, bool>>  condition1,
				Expression<Func<TestSource, Dest1>> setter2)
			{
				return db
					.SelectQuery(source)
					.MultiInsert()
					.When(
						condition1,
						dest,
						x => new Dest1 { ID = x.ID + 1, Value = x.N }
					)
					.When(
						x => x.N > 40,
						dest,
						setter2
					)
					.InsertAllAsync();
			}
		}

		[Test]
		public void Issue2990([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var src   = db.CreateLocalTable<TestSource>();
			using var dest2 = db.CreateLocalTable<Dest2>();

			var q =
				from s in src
				let d = dest2.Where(x => x.ID > 5).First()
				select new
				{
					A = s.ID,
					B = d.ID,
				};

			var count = q.MultiInsert()
				.When(x => true, dest2, x => new Dest2 { ID = x.A, Int = x.B })
				.InsertFirst();

			// INSERT ALL should be generated with OUTER APPLY, as if SELECT was executed alone, and not crash.
			// #2990 crashes with 'value(TestDataContext).GetTable().Where(x => (x.ID > 5)).First().ID' cannot be converted to SQL.
			count.ShouldBe(0);
		}

		sealed class TestSource
		{
			public int   ID { get; set; }
			public short N  { get; set; }
		}

		public sealed class Dest1
		{
			public int     ID          { get; set; }
			public short?  Value       { get; set; }
			public string? StringValue { get; set; }
		}

		public sealed class Dest2
		{
			public int ID    { get; set; }
			public int Int   { get; set; }
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2988")]
		public void InheritanceMapping([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db   = GetDataContext(context);
			using var dest = db.CreateLocalTable<Base>();

			db
				.SelectQuery(() => new TestSource { ID = 1 })
				.MultiInsert()
				.Into(
					db.GetTable<Base>(),
					src => new Derived { ID = src.ID }
					)
				.Insert();

			var entity = db.GetTable<Base>().First();

			entity.ShouldBeOfType<Derived>();
		}

		[Table("MULTI_INSERT_INHERIT", IsColumnAttributeRequired = false)]
		[InheritanceMapping(Code = 42, Type = typeof(Derived))]
		abstract class Base
		{
			public int ID { get; set; }

			[Column(IsDiscriminator = true)]
			public abstract int Type { get; }
		}

		class Derived : Base
		{
			public override int Type => 42;
		}
	}
}
