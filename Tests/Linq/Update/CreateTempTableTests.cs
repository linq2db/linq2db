using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class CreateTempTableTests : TestBase
	{
		sealed class IDTable
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

				await using (var tmp = await db.CreateTempTableAsync(
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

				await using (var tmp = await db.CreateTempTableAsync(
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
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				try
				{
					await using (var tmp = await db.CreateTempTableAsync(
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
					if (!context.IsAnyOf(TestProvName.AllMySqlData))
					{
						Assert.Fail("Task should have been canceled but was not");
					}
				}
				catch (OperationCanceledException)
				{
					if (context.IsAnyOf(TestProvName.AllMySqlData
#if !NETFRAMEWORK
						, TestProvName.AllOracleManaged
#endif
						))
					{
						Assert.Fail("Update test. Oracle developers evolved");
					}
				}
				catch (Exception ex) when (ex.Message.Contains("ORA-01013") && context.IsAnyOf(TestProvName.AllOracleManaged))
				{
					// ~Aliens~ Oracle
				}

				var tableExists = true;
				try
				{
					db.DropTable<int>("TempTable", throwExceptionIfNotExists: true);
				}
				catch
				{
					tableExists = false;
				}
				Assert.That(tableExists, Is.EqualTo(false));
			}
		}

		[Test]
		public async Task CreateTableAsyncCanceled2([DataSources(false)] string context)
		{
			using var cts = new CancellationTokenSource();
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

				try
				{
					await using (var tmp = await db.CreateTempTableAsync(
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
					if (!context.IsAnyOf(TestProvName.AllMySqlData))
					{
						Assert.Fail("Task should have been canceled but was not");
					}
				}
				catch (OperationCanceledException)
				{
					if (context.IsAnyOf(TestProvName.AllMySqlData
#if !NETFRAMEWORK
						, TestProvName.AllOracleManaged
#endif
						))
					{
						Assert.Fail("Update test. Oracle developers evolved");
					}
				}
				catch (Exception ex) when (ex.Message.Contains("ORA-01013") && context.IsAnyOf(TestProvName.AllOracleManaged))
				{
					// ~Aliens~ Oracle
				}

				var tableExists = true;
				try
				{
					db.DropTable<int>("TempTable", throwExceptionIfNotExists: true);
				}
				catch
				{
					tableExists = false;
				}
				Assert.That(tableExists, Is.EqualTo(false));
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
			using var db = GetDataConnection(context);
			db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

			var tempTable = db.CreateTempTable<IDTable>("TempTable");
			var table2 = db.GetTable<IDTable>().TableOptions(TableOptions.IsTemporary).TableName("TempTable");
			table2.Drop();
			tempTable.Dispose();
		}

		[Test]
		public async Task CreateTable_NoDisposeErrorAsync([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
			await db.DropTableAsync<int>("TempTable", throwExceptionIfNotExists: false);

			var tempTable = await db.CreateTempTableAsync<IDTable>("TempTable");
			var table2 = db.GetTable<IDTable>().TableOptions(TableOptions.IsTemporary).TableName("TempTable");
			await table2.DropAsync();
			await tempTable.DisposeAsync();
		}

		[Table]
		public class TableWithPrimaryKey
		{
			[PrimaryKey] public int Key { get; set; }
		}

		[Test]
		public void CreateTempTableWithPrimaryKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateTempTable<TableWithPrimaryKey>(tableOptions: TableOptions.IsTemporary);
		}

		[Test]
		public void InsertIntoTempTableWithPrimaryKey([DataSources(false)] string context)
		{
			// Note: this test is not reenterable as it uses oracle's GLOBAL TEMPORARY TABLE incorrectly:
			// such tables not designed for DROP and DROP could fail leading to error on second run of same test
			using var db = GetDataContext(context);

			// table name set explicitly to avoid table name conflict with
			// CreateTempTableWithPrimaryKey for Sybase (reproduced with full test-run only)
			// looks like some test blocks session cleanup in Sybase
			using var t = new[] { new TableWithPrimaryKey() { Key = 1 } }
				.IntoTempTable(db, tableName: "TableWithPrimaryKey2", tableOptions: TableOptions.IsTemporary);
		}

		[Table]
		sealed class TestTempTable
		{
			[Column] public int Id        { get; set; }
			[Column] public string? Value { get; set; }
		}

		[Test]
		[ActiveIssue("It is only possible to implement limited set of mapping changes. API should be removed at some point")]
		public void CreateTempTable_TestSchemaConflicts([DataSources] string context)
		{
			using var db    = GetDataContext(context, new MappingSchema());

			using var table = db.CreateLocalTable<TestTempTable>();
			table.Insert(() => new TestTempTable() { Id = 1, Value = "value" });

			using var tmp   = db.CreateTempTable(
				"TempTable",
				table,
				setTable: em => em.Property(e => e.Value).HasColumnName("Renamed"),
				tableOptions: TableOptions.CheckExistence);

			table.Insert(() => new TestTempTable() { Id = 2, Value = "value 2" });
			tmp  .Insert(() => new TestTempTable() { Id = 2, Value = "renamed 2" });

			var records1 = table.OrderBy(r => r.Id).ToArray();
			var records2 = tmp.OrderBy(r => r.Id).ToArray();

			Assert.That(records1, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(records1[0].Id, Is.EqualTo(1));
				Assert.That(records1[0].Value, Is.EqualTo("value"));
				Assert.That(records1[1].Id, Is.EqualTo(2));
				Assert.That(records1[1].Value, Is.EqualTo("value 2"));

				Assert.That(records2, Has.Length.EqualTo(2));
			});
			Assert.Multiple(() =>
			{
				Assert.That(records2[0].Id, Is.EqualTo(1));
				Assert.That(records2[0].Value, Is.EqualTo("value"));
				Assert.That(records2[1].Id, Is.EqualTo(2));
				Assert.That(records2[1].Value, Is.EqualTo("renamed 2"));
			});
		}

		[Test]
		public void CreateTableEnumerableWithDescriptionTest([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

			using var tmp = db.CreateTempTable(
				new[] { new { Name = "John" } },
				m => m
					.HasTableName("TempTable")
					.Property(p => p.Name)
						.HasLength(20)
						.IsNotNull(),
				tableOptions:TableOptions.CheckExistence);

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring("(20) NOT NULL").Or.Not.Contains("NULL"));

			var list =
			(
				from p in db.Person
				join t in tmp on p.FirstName equals t.Name
				select t
			).ToList();

			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public void CreateTableEnumerableWithNameAndDescriptionTest([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			db.DropTable<int>("TempTable", throwExceptionIfNotExists: false);

			using var tmp = db.CreateTempTable(
				"TempTable",
				new[] { new { Name = "John" } },
				m => m
					.Property(p => p.Name)
						.HasLength(20)
						.IsNotNull(),
				tableOptions:TableOptions.CheckExistence);

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring("(20) NOT NULL").Or.Not.Contains("NULL"));

			var list =
			(
				from p in db.Person
				join t in tmp on p.FirstName equals t.Name
				select t
			).ToList();

			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task CreateTableEnumerableWithDescriptionAsyncTest([DataSources(false)] string context)
		{
			await using var db = GetDataContext(context);

			await db.DropTableAsync<int>("TempTable", throwExceptionIfNotExists: false);

			await using var tmp = await db.CreateTempTableAsync(
				new[] { new { Name = "John" } },
				m => m
					.HasTableName("TempTable")
					.Property(p => p.Name)
						.HasLength(20)
						.IsNotNull(),
				tableOptions:TableOptions.CheckExistence);

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring("(20) NOT NULL").Or.Not.Contains("NULL"));

			var list = await
			(
				from p in db.Person
				join t in tmp on p.FirstName equals t.Name
				select t
			).ToListAsync();

			Assert.That(list, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task CreateTableEnumerableWithNameAndDescriptionAsyncTest([DataSources(false, TestProvName.AllSqlServerCS)] string context)
		{
			await using var db = GetDataContext(context);

			await db.DropTableAsync<int>("TempTable", tableOptions:TableOptions.CheckExistence | TableOptions.IsTemporary);

			await using var tmp = await db.CreateTempTableAsync(
				"TempTable",
				new[] { new { Name = "John" } },
				m => m
					.Property(p => p.Name)
						.HasLength(20)
						.IsNotNull(),
				tableOptions:TableOptions.CheckExistence | TableOptions.IsTemporary);

			if (db is DataConnection dc)
				Assert.That(dc.LastQuery, Contains.Substring("(20) NOT NULL").Or.Not.Contains("NULL"));

			var list = await
			(
				from p in db.Person
				join t in tmp on p.FirstName equals t.Name
				select t
			).ToListAsync();

			Assert.That(list, Has.Count.EqualTo(1));
		}
	}
}
