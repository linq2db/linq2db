using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;
using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class CreateTempTableTests : TestBase
	{
		class IDTable
		{
			public int ID;
		}

		[Test]
		public void CreateTable1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }),
					tableOptions:TableOptions.CheckExistence))
				{
					var l = tmp.ToList();

					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new { ID = p.ParentID }),
					tableOptions:TableOptions.CheckExistence))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTable3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new { ID = p.ParentID }),
					em => em
						.Property(e => e.ID)
							.IsPrimaryKey(),
					tableOptions:TableOptions.CheckExistence))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public void CreateTableEnumerable([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }).ToList(),
					tableOptions:TableOptions.CheckExistence))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public async Task CreateTableAsync([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				await db.DropTableAsync<int>("TempTable", throwExceptionIfNotExists: false);

#if !NET472
				await
#endif
				using (var tmp = await db.CreateTempTableAsync(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }),
					tableOptions:TableOptions.CheckExistence))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public async Task CreateTableAsyncEnumerable([DataSources(false)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

#if !NET472
				await
#endif
				using (var tmp = await db.CreateTempTableAsync(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID }).ToList(),
					tableOptions:TableOptions.CheckExistence))
				{
					var list =
					(
						from p in db.Parent
						join t in tmp on p.ParentID equals t.ID
						select t
					).ToList();
				}
			}
		}

		[Test]
		public async Task CreateTableAsyncCanceled([DataSources(false)] string context)
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				try
				{
#if !NET472
					await
#endif
					using (var tmp = await db.CreateTempTableAsync(
						"TempTable",
						db.Parent.Select(p => new IDTable { ID = p.ParentID }).ToList(),
						cancellationToken: cts.Token))
					{
						var list =
						(
							from p in db.Parent
							join t in tmp on p.ParentID equals t.ID
							select t
						).ToList();
					}
					Assert.Fail("Task should have been canceled but was not");
				}
				catch (OperationCanceledException) { }


				var tableExists = true;
				try
				{
					db.DropTable<int>("TempTable", throwExceptionIfNotExists: true);
				}
				catch
				{
					tableExists = false;
				}
				Assert.AreEqual(false, tableExists);
			}
		}

		[Test]
		public async Task CreateTableAsyncCanceled2([DataSources(false)] string context)
		{
			var cts = new CancellationTokenSource();
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				try
				{
#if !NET472
					await
#endif
					using (var tmp = await db.CreateTempTableAsync(
						"TempTable",
						db.Parent.Select(p => new IDTable { ID = p.ParentID }),
						action: (table) =>
						{
							cts.Cancel();
							return Task.CompletedTask;
						},
						cancellationToken: cts.Token))
					{
						var list =
						(
							from p in db.Parent
							join t in tmp on p.ParentID equals t.ID
							select t
						).ToList();
					}
					Assert.Fail("Task should have been canceled but was not");
				}
				catch (OperationCanceledException) { }

				var tableExists = true;
				try
				{
					db.DropTable<int>("TempTable", throwExceptionIfNotExists: true);
				}
				catch
				{
					tableExists = false;
				}
				Assert.AreEqual(false, tableExists);
			}
		}

		[Test]
		public void CreateTableSQLite([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var data = new[] { new { ID = 1, Field = 2 } };

			using (var tmp = db.CreateTempTable(data, tableName : "#TempTable"))
			{
				var list = tmp.ToList();

				Assert.That(list, Is.EquivalentTo(data));
			}
		}

		[Test]
		public void CreateTable_NoDisposeError([DataSources(false)] string context)
		{
			using var db = new TestDataConnection(context);
			db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

			var tempTable = db.CreateTempTable<IDTable>("TempTable");
			tempTable.Drop();
			tempTable.Dispose();
		}

#if !NETFRAMEWORK
		[Test]
		public async Task CreateTable_NoDisposeErrorAsync([DataSources(false)] string context)
		{
			using var db = new TestDataConnection(context);
			await db.DropTableAsync<int>("TempTable", throwExceptionIfNotExists: false);

			var tempTable = await db.CreateTempTableAsync<IDTable>("TempTable");
			await tempTable.DropAsync();
			await tempTable.DisposeAsync();
		}
#endif
	}
}
