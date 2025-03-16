using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class InterceptorsTests : TestBase
	{
		#region ICommandInterceptor

		#region ICommandInterceptor.CommandInitialized
		// DataConnection: test that interceptors triggered and one-time interceptors removed safely after single command
		[Test]
		public void CommandInitializedOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var triggered1 = false;
				var triggered2 = false;
				var interceptor1 = new TestCommandInterceptor();
				var interceptor2 = new TestCommandInterceptor();
				db.AddInterceptor(interceptor1);
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered1 = true;
					return command;
				});
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered2 = true;
					return command;
				});
				db.AddInterceptor(interceptor2);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});

				db.Child.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.True);
					Assert.That(triggered2, Is.True);
				});

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.Person.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});
			}
		}

		// test interceptors registration using fluent options builder
		[Test]
		public void CommandInitializedOnDataConnectionTest_OptionsBuilder([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var triggered1 = false;
			var triggered2 = false;
			var interceptor1 = new TestCommandInterceptor();
			var interceptor2 = new TestCommandInterceptor();

			var builder = new DataOptions()
				.UseConfiguration(context)
				.UseInterceptor(interceptor1)
				.UseInterceptor(interceptor2);

			using (var db = new DataConnection(builder))
			{
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered1 = true;
					return command;
				});
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered2 = true;
					return command;
				});

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.True);
					Assert.That(triggered2, Is.True);
				});

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});
			}
		}

		[Test]
		public void CommandInitializedOnDataContextTest_OptionsBuilder([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			var triggered1 = false;
			var triggered2 = false;
			var interceptor1 = new TestCommandInterceptor();
			var interceptor2 = new TestCommandInterceptor();

			var builder = new DataOptions()
				.UseConfiguration(context)
				.UseInterceptor(interceptor1)
				.UseInterceptor(interceptor2);

			using (var db = new DataContext(builder))
			{
				db.CloseAfterUse = closeAfterUse;

				db.OnNextCommandInitialized((args, command) =>
				{
					triggered1 = true;
					return command;
				});
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered2 = true;
					return command;
				});

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.True);
					Assert.That(triggered2, Is.True);
				});

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});
			}
		}

		private void CommandInitializedOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using (var db = new DataContext(context))
			{
				// test that interceptors not lost after underlying data connection recreation
				db.CloseAfterUse = closeAfterUse;

				var triggered1 = false;
				var triggered2 = false;
				var interceptor1 = new TestCommandInterceptor();
				var interceptor2 = new TestCommandInterceptor();
				db.AddInterceptor(interceptor1);
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered1 = true;
					return command;
				});
				db.OnNextCommandInitialized((args, command) =>
				{
					triggered2 = true;
					return command;
				});
				db.AddInterceptor(interceptor2);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.True);
					Assert.That(triggered2, Is.True);
				});

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
					Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
					Assert.That(triggered1, Is.False);
					Assert.That(triggered2, Is.False);
				});
			}
		}

		#endregion

		#region ICommandInterceptor.Execute*
		[Test]
		public void DataConnection_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.Multiple(() =>
					{
						Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
						Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
						Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
					});
				}
			}
		}

		[Test]
		public async Task DataConnection_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await using(await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.Multiple(() =>
					{
						Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
						Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
					});
				}
			}
		}

		[Test]
		public void DataConnection_ExecuteScalar([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			using (var table = db.CreateTempTable<InterceptorsTestsTable>())
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.True);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					// also true, as for sqlite we generate two queries
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task DataConnection_ExecuteScalarAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			await using (var db = GetDataConnection(context))
			await using (var table = await db.CreateTempTableAsync<InterceptorsTestsTable>())
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.True);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					// also true, as for sqlite we generate two queries
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
				});
			}
		}

		[Test]
		public void DataConnection_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				db.Child.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task DataConnection_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await db.Child.ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.True);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public void DataContext_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.Multiple(() =>
					{
						Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
						Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
						Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
					});
				}
			}
		}

		[Test]
		public async Task DataContext_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await using (await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.Multiple(() =>
					{
						Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
						Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
						Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
						Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
					});
				}
			}
		}

		[Test]
		public void DataContext_ExecuteScalar([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// use non-temp table as sqlite temp tables are session-bound and context recreates session
			using (var db = new DataContext(context))
			using (var table = db.CreateTempTable<InterceptorsTestsTable>(tableOptions: TableOptions.None))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.True);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					// also true, as for sqlite we generate two queries
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task DataContext_ExecuteScalarAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			// use non-temp table as sqlite temp tables are session-bound and context recreates session
			await using (var db = new DataContext(context))
			await using (var table = await db.CreateTempTableAsync<InterceptorsTestsTable>(tableOptions: TableOptions.None))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.True);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					// also true, as for sqlite we generate two queries
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
				});
			}
		}

		[Test]
		public void DataContext_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task DataContext_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});

				await db.GetTable<Child>().ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.True);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				});
			}
		}

		#endregion

		#region ICommandInterceptor.CommandInitialized

		[Test]
		public async ValueTask BeforeReaderDisposeTestOnDataConnection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = GetDataContext(context);

			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);

			db.Person.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
			});

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.Person.ToListAsync();

			Assert.Multiple(() =>
			{
#if NETFRAMEWORK
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
#else
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.False);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.True);
#endif
			});
		}

			[Test]
		public async ValueTask BeforeReaderDisposeTestOnDataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);

			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);

			db.GetTable<Person>().ToList();

			Assert.Multiple(() =>
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
			});

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.GetTable<Person>().ToListAsync();

			Assert.Multiple(() =>
			{
#if NETFRAMEWORK
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
#else
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.False);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.True);
#endif
			});
		}

		#endregion

		#endregion

		#region IConnectionInterceptor

		#region Open Connection
		[Test]
		public void ConnectionOpenOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				db.Child.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				db.Child.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				db.Close();

				db.Child.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				await db.Child.ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
				});

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await db.Child.ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				db.Close();

				await db.Child.ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
				});
			}
		}

		[Test]
		public void ConnectionOpenOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using (var db = new DataContext(context))
			{
				// test that interceptors not lost after underlying data connection recreation
				db.CloseAfterUse = closeAfterUse;

				var interceptor = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
					//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedTriggered);
					//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningTriggered);
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				((IDataContext)db).Close();

				db.GetTable<Child>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using (var db = new DataContext(context))
			{
				// test that interceptors not lost after underlying data connection recreation
				db.CloseAfterUse = closeAfterUse;

				var interceptor = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor);

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
				});

				await db.GetTable<Child>().ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
				});

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await db.GetTable<Child>().ToListAsync();

				Assert.Multiple(() =>
				{
					// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedAsyncTriggered);
					//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningAsyncTriggered);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
				});

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await ((IDataContext)db).CloseAsync();

				await db.GetTable<Child>().ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
					Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
					Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
				});
			}
		}

		#endregion

		#endregion

		#region IDataContextInterceptor

		#region EntityCreated

		[Test]
		public void EntityCreated_DataConnection_Or_RemoteContext([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var count = db.Person.Count();
				var interceptor1 = new TestEntityServiceInterceptor();
				var interceptor2 = new TestEntityServiceInterceptor();
				db.AddInterceptor(interceptor1);

				_ = db.Person.ToList();

				Assert.That(interceptor1.EntityCreatedContexts, Has.Count.EqualTo(count));
				Assert.That(interceptor1.EntityCreatedContexts.All(ctx => ctx == db), Is.True);
			}
		}

		[Test]
		public void EntityCreated_DataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			{
				var count = db.GetTable<Person>().Count();
				var interceptor1 = new TestEntityServiceInterceptor();
				var interceptor2 = new TestEntityServiceInterceptor();
				db.AddInterceptor(interceptor1);

				db.GetTable<Person>().ToList();

				Assert.That(interceptor1.EntityCreatedContexts, Has.Count.EqualTo(count));
				Assert.That(interceptor1.EntityCreatedContexts.All(ctx => ctx == db), Is.True);
			}
		}

		#endregion

		#region OnClosing/OnClosed

		[Test]
		public void CloseEvents_DataConnection_Or_RemoteContext([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;

			using (var db = main = GetDataContext(context))
			{
				db.AddInterceptor(interceptor);

				db.GetTable<Person>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
				Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
			});
		}

		[Test]
		public void CloseEvents_DataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;

			using (var db = main = new DataContext(context))
			{
				db.AddInterceptor(interceptor);

				_ = db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Single() is DataConnection, Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.Keys.Single() is DataConnection, Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(2));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
				Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
			});
		}

		[Test]
		public async Task CloseEvents_DataConnection_Or_RemoteContext_Async([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;

			await using (var db = main = GetDataContext(context))
			{
				db.AddInterceptor(interceptor);

				await db.GetTable<Person>().ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});
			}

			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts, Is.Empty);
				Assert.That(interceptor.OnClosingContexts, Is.Empty);

				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
			});
		}

		[Test]
		public async Task CloseEvents_DataContext_Async([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;

			await using (var db = main = new DataContext(context))
			{
				db.AddInterceptor(interceptor);

				await db.GetTable<Person>().ToListAsync();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedAsyncContexts.Keys.Single() is DataConnection, Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingAsyncContexts.Keys.Single() is DataConnection, Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
				});
			}

			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts, Is.Empty);
				Assert.That(interceptor.OnClosingContexts, Is.Empty);
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(2));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(2));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
			});
		}

		[Test]
		public async Task CloseEvents_DataConnection_Or_RemoteContext_ExplicitCall([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;
			using (var db = main = GetDataContext(context))
			{
				db.AddInterceptor(interceptor);

				db.GetTable<Person>().ToList();

				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				db.Close();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				await db.CloseAsync();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
				});
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
			});
		}

		[Test]
		public async Task CloseEvents_DataContext_ExplicitCall([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor = new TestDataContextInterceptor();

			IDataContext main;
			using (var db = main = new DataContext(context))
			{
				db.AddInterceptor(interceptor);

				db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.Keys.All(_ => _ is DataConnection), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.Keys.All(_ => _ is DataConnection), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				db.Close();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(2));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				});

				await db.CloseAsync();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
				});
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosedContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1), Is.True);
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
			});
			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4457")]
		public async Task Test_Connection_Release_EagerLoad_AutoConnection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
		{
			var closeInterceptor = new TestDataContextInterceptor();
			var openInterceptor = new TestConnectionInterceptor();

			IDataContext main;
			using (var db = main = new DataContext(context))
			{
				((DataContext)db).KeepConnectionAlive = false;
				db.CloseAfterUse = closeAfterUse;
				db.AddInterceptor(closeInterceptor);
				db.AddInterceptor(openInterceptor);

				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.Zero);
					Assert.That(GetClosedCount(), Is.Zero);
				});

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.Not.Zero);
					Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
				});

				db.GetTable<Parent>().LoadWith(p => p.Children).ToList();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				await db.GetTable<Person>().LoadWith(p => p.Patient).ToListAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				await db.GetTable<Parent>().LoadWith(p => p.Children).ToListAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefault();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefault();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				await db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefaultAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				await db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefaultAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

				await db.CloseAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
			}

			Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

			int GetOpenedCount() => openInterceptor.ConnectionOpenedCount + openInterceptor.ConnectionOpenedAsyncCount;
			int GetClosedCount() => closeInterceptor.OnClosedContexts.Concat(closeInterceptor.OnClosedAsyncContexts).Where(c => c.Key is DataConnection).Sum(c => c.Value);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4457")]
		public async Task Test_Connection_Release_EagerLoad_PersistentConnection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
		{
			var closeInterceptor = new TestDataContextInterceptor();
			var openInterceptor = new TestConnectionInterceptor();

			IDataContext main;
			using (var db = main = new DataContext(context))
			{
				((DataContext)db).KeepConnectionAlive = true;
				db.CloseAfterUse = closeAfterUse;
				db.AddInterceptor(closeInterceptor);
				db.AddInterceptor(openInterceptor);

				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.Zero);
					Assert.That(GetClosedCount(), Is.Zero);
				});

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				db.GetTable<Parent>().LoadWith(p => p.Children).ToList();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				await db.GetTable<Person>().LoadWith(p => p.Patient).ToListAsync();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				await db.GetTable<Parent>().LoadWith(p => p.Children).ToListAsync();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefault();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefault();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				await db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefaultAsync();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				await db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefaultAsync();
				Assert.Multiple(() =>
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				});

				await db.CloseAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
			}

			Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

			int GetOpenedCount() => openInterceptor.ConnectionOpenedCount + openInterceptor.ConnectionOpenedAsyncCount;
			int GetClosedCount() => closeInterceptor.OnClosedContexts.Concat(closeInterceptor.OnClosedAsyncContexts).Where(c => c.Key is DataConnection).Sum(c => c.Value);
		}

		#endregion

		#endregion

		#region IExceptionInterceptor
		[Test]
		public void NonQueryExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.AddInterceptor(new TestExceptionInterceptor());

				Assert.Throws<TestException>(() =>
					db.GetTable<InterceptorsTestsTable>()
						.Insert(() => new InterceptorsTestsTable()));
			}
		}

		[Test]
		public void ScalarExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.AddInterceptor(new TestExceptionInterceptor());

				Assert.Throws<TestException>(() =>
					db.GetTable<InterceptorsTestsTable>().Count());
			}
		}

		[Test]
		public void ReaderExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.AddInterceptor(new TestExceptionInterceptor());

				Assert.Throws<TestException>(() =>
					db.GetTable<InterceptorsTestsTable>().ToList());
			}
		}
		#endregion

		private sealed class TestCommandInterceptor : CommandInterceptor
		{
			public bool CommandInitializedTriggered { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				CommandInitializedTriggered = true;

				return base.CommandInitialized(eventData, command);
			}

			public bool ExecuteNonQueryTriggered           { get; set; }
			public bool ExecuteNonQueryAsyncTriggered      { get; set; }
			public bool ExecuteReaderTriggered             { get; set; }
			public bool ExecuteReaderAsyncTriggered        { get; set; }
			public bool ExecuteScalarTriggered             { get; set; }
			public bool ExecuteScalarAsyncTriggered        { get; set; }
			public bool ExecuteAfterExecuteReaderTriggered { get; set; }
			public bool BeforeReaderDisposeTriggered       { get; set; }
			public bool BeforeReaderDisposeAsyncTriggered  { get; set; }

			public override Option<int> ExecuteNonQuery(CommandEventData eventData, DbCommand command, Option<int> result)
			{
				ExecuteNonQueryTriggered = true;
				return base.ExecuteNonQuery(eventData, command, result);
			}

			public override Task<Option<int>> ExecuteNonQueryAsync(CommandEventData eventData, DbCommand command, Option<int> result, CancellationToken cancellationToken)
			{
				ExecuteNonQueryAsyncTriggered = true;
				return base.ExecuteNonQueryAsync(eventData, command, result, cancellationToken);
			}

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				ExecuteReaderTriggered = true;
				return base.ExecuteReader(eventData, command, commandBehavior, result);
			}

			public override Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
			{
				ExecuteReaderAsyncTriggered = true;
				return base.ExecuteReaderAsync(eventData, command, commandBehavior, result, cancellationToken);
			}

			public override void AfterExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, DbDataReader dataReader)
			{
				ExecuteAfterExecuteReaderTriggered = true;
				base.AfterExecuteReader(eventData, command, commandBehavior, dataReader);
			}

			public override Option<object?> ExecuteScalar(CommandEventData eventData, DbCommand command, Option<object?> result)
			{
				ExecuteScalarTriggered = true;
				return base.ExecuteScalar(eventData, command, result);
			}

			public override Task<Option<object?>> ExecuteScalarAsync(CommandEventData eventData, DbCommand command, Option<object?> result, CancellationToken cancellationToken)
			{
				ExecuteScalarAsyncTriggered = true;
				return base.ExecuteScalarAsync(eventData, command, result, cancellationToken);
			}

			public override void BeforeReaderDispose(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
			{
				BeforeReaderDisposeTriggered = true;
				base.BeforeReaderDispose(eventData, command, dataReader);
			}

			public override Task BeforeReaderDisposeAsync(CommandEventData eventData, DbCommand? command, DbDataReader dataReader)
			{
				BeforeReaderDisposeAsyncTriggered = true;
				return base.BeforeReaderDisposeAsync(eventData, command, dataReader);
			}
		}

		private sealed class TestConnectionInterceptor : ConnectionInterceptor
		{
			public bool ConnectionOpenedTriggered       { get; set; }
			public bool ConnectionOpenedAsyncTriggered  { get; set; }
			public bool ConnectionOpeningTriggered      { get; set; }
			public bool ConnectionOpeningAsyncTriggered { get; set; }

			public int ConnectionOpenedCount       { get; set; }
			public int ConnectionOpenedAsyncCount  { get; set; }
			public int ConnectionOpeningCount      { get; set; }
			public int ConnectionOpeningAsyncCount { get; set; }

			public override void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
			{
				ConnectionOpenedTriggered = true;
				ConnectionOpenedCount++;
				base.ConnectionOpened(eventData, connection);
			}

			public override Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				ConnectionOpenedAsyncTriggered = true;
				ConnectionOpenedAsyncCount++;
				return base.ConnectionOpenedAsync(eventData, connection, cancellationToken);
			}

			public override void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
			{
				ConnectionOpeningTriggered = true;
				ConnectionOpeningCount++;
				base.ConnectionOpening(eventData, connection);
			}

			public override Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				ConnectionOpeningAsyncTriggered = true;
				ConnectionOpeningAsyncCount++;
				return base.ConnectionOpeningAsync(eventData, connection, cancellationToken);
			}
		}

		sealed class TestEntityServiceInterceptor : EntityServiceInterceptor
		{
			public List<IDataContext> EntityCreatedContexts { get; } = new ();

			public override object EntityCreated(EntityCreatedEventData eventData, object entity)
			{
				EntityCreatedContexts.Add(eventData.Context);
				return base.EntityCreated(eventData, entity);
			}
		}

		sealed class TestDataContextInterceptor : DataContextInterceptor
		{
			public Dictionary<IDataContext, int> OnClosedContexts       { get; } = new();
			public Dictionary<IDataContext, int> OnClosingContexts      { get; } = new();
			public Dictionary<IDataContext, int> OnClosedAsyncContexts  { get; } = new();
			public Dictionary<IDataContext, int> OnClosingAsyncContexts { get; } = new();

			public override void OnClosed(DataContextEventData eventData)
			{
				if (OnClosedContexts.TryGetValue(eventData.Context, out var cnt))
					OnClosedContexts[eventData.Context] = cnt + 1;
				else
					OnClosedContexts[eventData.Context] = 1;

				base.OnClosed(eventData);
			}

			public override void OnClosing(DataContextEventData eventData)
			{
				if (OnClosingContexts.TryGetValue(eventData.Context, out var cnt))
					OnClosingContexts[eventData.Context] = cnt + 1;
				else
					OnClosingContexts[eventData.Context] = 1;

				base.OnClosing(eventData);
			}

			public override Task OnClosedAsync(DataContextEventData eventData)
			{
				if (OnClosedAsyncContexts.TryGetValue(eventData.Context, out var cnt))
					OnClosedAsyncContexts[eventData.Context] = cnt + 1;
				else
					OnClosedAsyncContexts[eventData.Context] = 1;

				return base.OnClosedAsync(eventData);
			}

			public override Task OnClosingAsync(DataContextEventData eventData)
			{
				if (OnClosingAsyncContexts.TryGetValue(eventData.Context, out var cnt))
					OnClosingAsyncContexts[eventData.Context] = cnt + 1;
				else
					OnClosingAsyncContexts[eventData.Context] = 1;

				return base.OnClosingAsync(eventData);
			}
		}

		[Table]
		public class InterceptorsTestsTable
		{
			[Column, Identity] public int ID;
		}

		private sealed class TestExceptionInterceptor : ExceptionInterceptor
		{
			public override void ProcessException(ExceptionEventData eventData, Exception exception)
			{
				throw new TestException();
			}
		}

		public sealed class TestException() : Exception("Test Exception");
	}
}
