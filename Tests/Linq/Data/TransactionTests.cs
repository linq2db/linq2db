using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.Firebird;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.MySql;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.DataProvider.SqlCe;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Sybase;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class TransactionTests : TestBase
	{
		[Test]
		public async Task DataContextBeginTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = new DataContext(context);
			using var ts = new RestoreBaseTables(db);

			// ensure connection opened and test results not affected by OpenAsync
			using var ks = new KeepConnectionAliveScope(db);

			await db.GetTable<Parent>().ToListAsync();

			var tid = Environment.CurrentManagedThreadId;

			using (await db.BeginTransactionAsync())
			{
				// perform synchonously to not mess with BeginTransactionAsync testing
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataContextOpenOrBeginTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			var tid = Environment.CurrentManagedThreadId;

			using (var db = new DataContext(context))
			using (new RestoreBaseTables(db))
			using (await db.BeginTransactionAsync())
			{
				// perform synchonously to not mess with BeginTransactionAsync testing
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataContextCommitTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			using (new RestoreBaseTables(db))
			using (var tr = await db.BeginTransactionAsync())
			{
				int tid;
				try
				{
					await db.InsertAsync(new Parent { ParentID = 1010, Value1 = 1010 });

					tid = Environment.CurrentManagedThreadId;

					await tr.CommitTransactionAsync();
				}
				finally
				{
					// perform synchronously to not mess with CommitTransactionAsync testing
					db.GetTable<Parent>().Where(_ => _.ParentID == 1010).Delete();
				}

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataContextRollbackTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			using (new RestoreBaseTables(db))
			using (var tr = await db.BeginTransactionAsync())
			{
				await db.InsertAsync(new Parent { ParentID = 1010, Value1 = 1010 });

				var tid = Environment.CurrentManagedThreadId;

				await tr.RollbackTransactionAsync();

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionBeginTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			var tid = Environment.CurrentManagedThreadId;

			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			using (await db.BeginTransactionAsync())
			{
				// perform synchonously to not mess with BeginTransactionAsync testing
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionDisposeAsyncTransaction([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			var tid = Environment.CurrentManagedThreadId;

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			await using (db.BeginTransaction())
			{
				// perform synchonously to not mess with DisposeAsync testing
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionCommitTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			using (await db.BeginTransactionAsync())
			{
				int tid;
				try
				{
					await db.InsertAsync(new Parent { ParentID = 1010, Value1 = 1010 });

					tid = Environment.CurrentManagedThreadId;

					await db.CommitTransactionAsync();
				}
				finally
				{
					// perform synchonously to not mess with CommitTransactionAsync testing
					db.GetTable<Parent>().Where(_ => _.ParentID == 1010).Delete();
				}

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionRollbackTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			using (await db.BeginTransactionAsync())
			{
				// perform synchonously to not mess with BeginTransactionAsync testing
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				var tid = Environment.CurrentManagedThreadId;

				await db.RollbackTransactionAsync();

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public void AutoRollbackTransaction([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				using (db.BeginTransaction())
				{
					db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1012 });
				}

				var p = db.Parent.First(t => t.ParentID == 1010);

				Assert.That(p.Value1, Is.Not.EqualTo(1012));
			}
		}

		[Test]
		public async Task AutoRollbackTransactionAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				await db.InsertAsync(new Parent { ParentID = 1010, Value1 = 1010 });

				await using (db.BeginTransaction())
				{
					db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1012 });
				}

				var p = await db.Parent.FirstAsync(t => t.ParentID == 1010);

				Assert.That(p.Value1, Is.Not.EqualTo(1012));
			}
		}

		[Test]
		public void CommitTransaction([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				using (var tr = db.BeginTransaction())
				{
					db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1011 });
					tr.Commit();
				}

				var p = db.Parent.First(t => t.ParentID == 1010);

				Assert.That(p.Value1, Is.EqualTo(1011));
			}
		}

		[Test]
		public void RollbackTransaction([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (new RestoreBaseTables(db))
			{
				db.Insert(new Parent { ParentID = 1010, Value1 = 1010 });

				using (var tr = db.BeginTransaction())
				{
					db.Parent.Update(t => t.ParentID == 1010, t => new Parent { Value1 = 1012 });
					tr.Rollback();
				}

				var p = db.Parent.First(t => t.ParentID == 1010);

				Assert.That(p.Value1, Is.Not.EqualTo(1012));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3863")]
		public void DisposeCommitedTransaction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.BeginTransaction();
			db.Execute("commit;");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3863")]
		public async ValueTask DisposeCommitedTransactionAsync([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataConnection(context);
			await using var _ = await db.BeginTransactionAsync();
			await db.ExecuteAsync("commit;");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3863")]
		public void DisposeCommitedTransactionDataContext([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = new DataContext(context);
			using var _ = db.BeginTransaction();
			db.NextQueryHints.Add("**commit;");
			db.GetTable<Person>().Count();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3863")]
		public async ValueTask DisposeCommitedTransactionAsyncDataContext([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = new DataContext(context);
			await using var _ = await db.BeginTransactionAsync();
			db.NextQueryHints.Add("**commit;");
			await db.GetTable<Person>().CountAsync();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5225")]
		public async Task AttachToExistingTransaction([DataSources(false, ProviderName.SqlCe)] string context)
		{
			var connectionString = GetConnectionString(context);

			using var db = GetDataConnection(context);
			using var tr = db.BeginTransaction();

			_ = context switch
			{
				var ctx when ctx.IsAnyOf(TestProvName.AllAccess)     => AccessTools    .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllClickHouse) => ClickHouseTools.GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllDB2)        => DB2Tools       .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllFirebird)   => FirebirdTools  .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllInformix)   => InformixTools  .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllMySql)      => MySqlTools     .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllOracle)     => OracleTools    .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllPostgreSQL) => PostgreSQLTools.GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllSapHana)    => SapHanaTools   .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllSQLite)     => SQLiteTools    .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllSqlServer)  => SqlServerTools .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				var ctx when ctx.IsAnyOf(TestProvName.AllSybase)     => SybaseTools    .GetDataProvider(connectionString: connectionString, connection: db.OpenDbConnection(), transaction: db.Transaction),
				_                                                    => throw new NotImplementedException($"Missing case for provider {context}")
			};
		}
	}
}
