using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

			Assert.AreEqual(3, count);
			Assert.AreEqual(2, dest1.Count());
			Assert.AreEqual(1, dest2.Count(x => x.ID == 1003));
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

			Assert.AreEqual(3, count);
			Assert.AreEqual(2, await dest1.CountAsync());
			Assert.AreEqual(1, await dest2.CountAsync(x => x.ID == 1003));
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

			Assert.AreEqual(2, count);
			Assert.AreEqual(1, dest1.Count());
			Assert.AreEqual(1, dest1.Count(x => x.ID == 1001));
			Assert.AreEqual(1, dest2.Count(x => x.ID == 1003));
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

			Assert.AreEqual(2, count);
			Assert.AreEqual(1, await dest1.CountAsync());
			Assert.AreEqual(1, await dest1.CountAsync(x => x.ID == 1001));
			Assert.AreEqual(1, await dest2.CountAsync(x => x.ID == 1003));
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(1, dest.Count());
			Assert.AreEqual(1, dest.Count(x => x.ID == 1003));
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(1, await dest.CountAsync());
			Assert.AreEqual(1, await dest.CountAsync(x => x.ID == 1003));
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(value == null ? id1 : id2, record.ID);
			Assert.AreEqual(value, record.StringValue);
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

			Assert.AreEqual(1, count);
			Assert.AreEqual(1, dest.Count());
			Assert.AreEqual(1, dest.Count(x => x.ID == 3002));

			// Perform the same INSERT ALL with different expressions
			count = InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			Assert.AreEqual(2, count);
			Assert.AreEqual(2, dest.Count(x => x.ID == 4001 || x.ID == 4002));

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

			Assert.AreEqual(1, count);
			Assert.AreEqual(1, await dest.CountAsync());
			Assert.AreEqual(1, await dest.CountAsync(x => x.ID == 3002));

			// Perform the same INSERT ALL with different expressions
			count = await InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			Assert.AreEqual(2, count);
			Assert.AreEqual(3, await dest.CountAsync());
			Assert.AreEqual(2, await dest.CountAsync(x => x.ID == 4001 || x.ID == 4002));

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

		class TestSource
		{
			public int   ID { get; set; }
			public short N  { get; set; }
		}

		class Dest1
		{
			public int     ID          { get; set; }
			public short   Value       { get; set; }
			public string? StringValue { get; set; }
		}

		class Dest2
		{
			public int ID    { get; set; }
			public int Int   { get; set; }
		}
	}
}
