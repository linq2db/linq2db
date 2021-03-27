using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	public class MultiInsertTests : TestBase
	{
		private void Cleanup(ITestDataContext db)
		{
			db.Types.Delete(x => x.ID > 1000);
			db.Child.Delete(x => x.ChildID > 1000);
		}

		[Test]
		public void Insert([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.Into(
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.Insert();

				Assert.AreEqual(3, count);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task InsertAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = await db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.Into(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.Into(
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.InsertAsync();

				Assert.AreEqual(3, count);
				Assert.AreEqual(2, await db.Types.CountAsync(x => x.ID > 1000));
				Assert.AreEqual(1, await db.Child.CountAsync(x => x.ChildID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertAll([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.When(
						x => x.N > 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.When(
						x => true,
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.InsertAll();

				Assert.AreEqual(2, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 1000));
				Assert.AreEqual(1, db.Child.Count(x => x.ChildID == 1003));
				Assert.AreEqual(1, db.Types.Count(x => x.ID == 1001));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task InsertAllAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = await db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.When(
						x => x.N > 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.When(
						x => true,
						db.Child,
						x => new Child { ParentID = x.ID + 1, ChildID = x.ID + 3 }
					)
					.InsertAllAsync();

				Assert.AreEqual(2, count);
				Assert.AreEqual(1, await db.Types.CountAsync(x => x.ID > 1000));
				Assert.AreEqual(1, await db.Child.CountAsync(x => x.ChildID == 1003));
				Assert.AreEqual(1, await db.Types.CountAsync(x => x.ID == 1001));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void InsertFirst([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => false,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.Else(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 3, SmallIntValue = x.N }
					)
					.InsertFirst();

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task InsertFirstAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int count = await db
					.SelectQuery(() => new { ID = 1000, N = (short)42 })
					.MultiInsert()
					.When(
						x => x.N < 40,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => false,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 2, SmallIntValue = x.N }
					)
					.Else(
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 3, SmallIntValue = x.N }
					)
					.InsertFirstAsync();

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, await db.Types.CountAsync(x => x.ID == 1003));
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void ParametersInSource(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => x.Value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = x.Value }
					)
					.When(
						x => x.Value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = x.Value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = command.InsertAll();
				var record = db.Types.Where(_ => _.ID > 1000).Single();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task ParametersInSourceAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => x.Value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = x.Value }
					)
					.When(
						x => x.Value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = x.Value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = await command.InsertAllAsync();
				var record = await db.Types.Where(_ => _.ID > 1000).SingleAsync();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void ParametersInCondition(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = x.Value }
					)
					.When(
						x => value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = x.Value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = command.InsertAll();
				var record = db.Types.Where(_ => _.ID > 1000).Single();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task ParametersInConditionAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = x.Value }
					)
					.When(
						x => value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = x.Value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = await command.InsertAllAsync();
				var record = await db.Types.Where(_ => _.ID > 1000).SingleAsync();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void ParametersInInsert(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => x.Value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = value }
					)
					.When(
						x => x.Value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = command.InsertAll();
				var record = db.Types.Where(_ => _.ID > 1000).Single();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public async Task ParametersInInsertAsync(
			[IncludeDataSources(true, TestProvName.AllOracle)] string context,
			[Values("one", null, "two")] string? value)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				int id1 = 3000, id2 = 4000;

				var command = db
					.SelectQuery(() => new { Value = value })
					.MultiInsert()
					.When(
						x => x.Value == null,
						db.Types,
						x => new LinqDataTypes { ID = id1, StringValue = value }
					)
					.When(
						x => x.Value != null,
						db.Types,
						x => new LinqDataTypes { ID = id2, StringValue = value }
					);

				// Perform a simple INSERT ALL with parameters
				int count  = await command.InsertAllAsync();
				var record = await db.Types.Where(_ => _.ID > 1000).SingleAsync();

				Assert.AreEqual(1, count);
				Assert.AreEqual(value == null ? id1 : id2, record.ID);
				Assert.AreEqual(value, record.StringValue);
			}
			finally
			{
				Cleanup(db);
			}
		}

		[Test]
		public void Expressions([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				// Perform a simple INSERT ALL with some expressions
				int count = InsertAll(
					() => new TestSource { ID = 3000, N = (short)42 },
					x  => x.N < 0,
					x  => new LinqDataTypes { ID = 3002, SmallIntValue = x.N });

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, db.Types.Count(x => x.ID > 3000));
				Assert.AreEqual(0, db.Types.Count(x => x.ID > 4000));

				Cleanup(db);

				// Perform the same INSERT ALL with different expressions
				count = InsertAll(
					() => new TestSource { ID = 4000, N = (short)42 },
					x  => true, 
					x  => new LinqDataTypes { ID = 4002, SmallIntValue = x.N });

				Assert.AreEqual(2, count);
				Assert.AreEqual(2, db.Types.Count(x => x.ID > 4000));
			}
			finally
			{
				Cleanup(db);
			}

			int InsertAll(
				Expression<Func<TestSource>>                source,
				Expression<Func<TestSource, bool>>          condition1,
				Expression<Func<TestSource, LinqDataTypes>> setter2)
			{
				return db
					.SelectQuery(source)
					.MultiInsert()
					.When(
						condition1,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => x.N > 40,
						db.Types,
						setter2
					)
					.InsertAll();
			}
		}

		[Test]
		public async Task ExpressionsAsync([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			await using var db = GetDataContext(context);
			Cleanup(db);
			try
			{
				// Perform a simple INSERT ALL with some expressions
				int count = await InsertAll(
					() => new TestSource { ID = 3000, N = (short)42 },
					x  => x.N < 0,
					x  => new LinqDataTypes { ID = 3002, SmallIntValue = x.N });

				Assert.AreEqual(1, count);
				Assert.AreEqual(1, await db.Types.CountAsync(x => x.ID > 3000));
				Assert.AreEqual(0, await db.Types.CountAsync(x => x.ID > 4000));

				Cleanup(db);

				// Perform the same INSERT ALL with different expressions
				count = await InsertAll(
					() => new TestSource { ID = 4000, N = (short)42 },
					x  => true, 
					x  => new LinqDataTypes { ID = 4002, SmallIntValue = x.N });

				Assert.AreEqual(2, count);
				Assert.AreEqual(2, await db.Types.CountAsync(x => x.ID > 4000));
			}
			finally
			{
				Cleanup(db);
			}

			Task<int> InsertAll(
				Expression<Func<TestSource>>                source,
				Expression<Func<TestSource, bool>>          condition1,
				Expression<Func<TestSource, LinqDataTypes>> setter2)
			{
				return db
					.SelectQuery(source)
					.MultiInsert()
					.When(
						condition1,
						db.Types,
						x => new LinqDataTypes { ID = x.ID + 1, SmallIntValue = x.N }
					)
					.When(
						x => x.N > 40,
						db.Types,
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
	}
}
