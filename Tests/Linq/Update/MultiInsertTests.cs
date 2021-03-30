﻿using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;

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

			count.Should().Be(3);
			dest1.Count().Should().Be(2);
			dest2.Count(x => x.ID == 1003).Should().Be(1);
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

			count.Should().Be(3);
			(await dest1.CountAsync()).Should().Be(2);
			(await dest1.CountAsync()).Should().Be(2);
			(await dest2.CountAsync(x => x.ID == 1003)).Should().Be(1);
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

			count.Should().Be(2);
			dest1.Count().Should().Be(1);

			dest1.Count(x => x.ID == 1001).Should().Be(1);
			dest2.Count(x => x.ID == 1003).Should().Be(1);
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

			count.Should().Be(4);
			dest1.Count().Should().Be(2);
			dest2.Count().Should().Be(2);

			dest1.Count(x => x.ID == 1001).Should().Be(1);
			dest2.Count(x => x.ID == 1003).Should().Be(1);
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

			count.Should().Be(2);
			(await dest1.CountAsync()).Should().Be(1);
			(await dest1.CountAsync(x => x.ID == 1001)).Should().Be(1);
			(await dest2.CountAsync(x => x.ID == 1003)).Should().Be(1);
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

			count.Should().Be(1);
			dest.Count().Should().Be(1);
			dest.Count(x => x.ID == 1003).Should().Be(1);
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

			count.Should().Be(1);
			(await dest.CountAsync()).Should().Be(1);
			(await dest.CountAsync(x => x.ID == 1003)).Should().Be(1);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			record.ID.Should().Be(value == null ? id1 : id2);
			record.StringValue.Should().Be(value);
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

			count.Should().Be(1);
			dest.Count().Should().Be(1);
			dest.Count(x => x.ID == 3002).Should().Be(1);

			// Perform the same INSERT ALL with different expressions
			count = InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			count.Should().Be(2);
			dest.Count(x => x.ID == 4001 || x.ID == 4002).Should().Be(2);

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

			count.Should().Be(1);
			(await dest.CountAsync()).Should().Be(1);
			(await dest.CountAsync(x => x.ID == 3002)).Should().Be(1);

			// Perform the same INSERT ALL with different expressions
			count = await InsertAll(
				() => new TestSource { ID = 4000, N = (short)42 },
				x  => true, 
				x  => new Dest1 { ID = 4002, Value = x.N });

			count.Should().Be(2);
			(await dest.CountAsync()).Should().Be(3);
			(await dest.CountAsync(x => x.ID == 4001 || x.ID == 4002)).Should().Be(2);

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
			public short?  Value       { get; set; }
			public string? StringValue { get; set; }
		}

		class Dest2
		{
			public int ID    { get; set; }
			public int Int   { get; set; }
		}
	}
}
