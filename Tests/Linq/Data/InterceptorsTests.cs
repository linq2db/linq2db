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

namespace Tests.Data
{
	using Model;

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

				Assert.False(interceptor1.CommandInitializedTriggered);
				Assert.False(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);

				db.Child.ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.True(triggered1);
				Assert.True(triggered2);

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.Person.ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);
			}
		}

		[Test]
		public void CommandInitializedOnDataConnectionCloningTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var triggered1 = false;
				var triggered2 = false;
				var triggered3 = false;
				var interceptor1 = new TestCommandInterceptor();
				var interceptor2 = new TestCommandInterceptor();
				var interceptor3 = new TestCommandInterceptor();
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

				using (var clonedDb = (DataConnection)db.Clone())
				{
					// add after cloning
					db.OnNextCommandInitialized((args, command) =>
					{
						triggered3 = true;
						return command;
					});
					db.AddInterceptor(interceptor3);

					Assert.False(interceptor1.CommandInitializedTriggered);
					Assert.False(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);

					db.Child.ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(interceptor3.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);
					Assert.True(triggered3);

					triggered1 = false;
					triggered2 = false;
					triggered3 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;
					interceptor3.CommandInitializedTriggered = false;

					db.Person.ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);

					// test that cloned connection still preserve non-fired one-time interceptors
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;
					interceptor3.CommandInitializedTriggered = false;

					clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);
					Assert.False(triggered3);

					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					clonedDb.GetTable<Person>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);
				}
			}
		}

		[Test]
		public void CommandInitializedOnDataContextCloningTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using (var db = new DataContext(context))
			{
				db.CloseAfterUse = closeAfterUse;

				var triggered1 = false;
				var triggered2 = false;
				var triggered3 = false;
				var interceptor1 = new TestCommandInterceptor();
				var interceptor2 = new TestCommandInterceptor();
				var interceptor3 = new TestCommandInterceptor();
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

				using (var clonedDb = (DataContext)((IDataContext)db).Clone(true))
				{
					// add after cloning
					db.OnNextCommandInitialized((args, command) =>
					{
						triggered3 = true;
						return command;
					});
					db.AddInterceptor(interceptor3);

					Assert.False(interceptor1.CommandInitializedTriggered);
					Assert.False(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);

					_ = db.GetTable<Child>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(interceptor3.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);
					Assert.True(triggered3);

					triggered1 = false;
					triggered2 = false;
					triggered3 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;
					interceptor3.CommandInitializedTriggered = false;

					_ = db.GetTable<Person>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);

					// test that cloned connection still preserve non-fired one-time interceptors
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;
					interceptor3.CommandInitializedTriggered = false;

					_ = clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);
					Assert.False(triggered3);

					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					_ = clonedDb.GetTable<Person>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(interceptor3.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
					Assert.False(triggered3);
				}
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
				.UseConfigurationString(context)
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

				Assert.False(interceptor1.CommandInitializedTriggered);
				Assert.False(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);

				db.GetTable<Child>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.True(triggered1);
				Assert.True(triggered2);

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);
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
				.UseConfigurationString(context)
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

				Assert.False(interceptor1.CommandInitializedTriggered);
				Assert.False(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);

				db.GetTable<Child>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.True(triggered1);
				Assert.True(triggered2);

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);
			}
		}

		public void CommandInitializedOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
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

				Assert.False(interceptor1.CommandInitializedTriggered);
				Assert.False(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);

				db.GetTable<Child>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.True(triggered1);
				Assert.True(triggered2);

				triggered1 = false;
				triggered2 = false;
				interceptor1.CommandInitializedTriggered = false;
				interceptor2.CommandInitializedTriggered = false;

				db.GetTable<Person>().ToList();

				Assert.True(interceptor1.CommandInitializedTriggered);
				Assert.True(interceptor2.CommandInitializedTriggered);
				Assert.False(triggered1);
				Assert.False(triggered2);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
					Assert.True(interceptor.ExecuteNonQueryTriggered);
					Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await using(await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteNonQueryTriggered);
					Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.True(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.True(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataConnection_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				db.Child.ToList();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.True(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.True(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public async Task DataConnection_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await db.Child.ToListAsync();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.True(interceptor.ExecuteReaderAsyncTriggered);
				Assert.True(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataContext_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
					Assert.True(interceptor.ExecuteNonQueryTriggered);
					Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await using (await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteNonQueryTriggered);
					Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.True(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
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

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.True(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataContext_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				db.GetTable<Child>().ToList();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.True(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.True(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public async Task DataContext_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await db.GetTable<Child>().ToListAsync();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.True(interceptor.ExecuteReaderAsyncTriggered);
				Assert.True(interceptor.ExecuteAfterExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
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

			Assert.True(interceptor.BeforeReaderDisposeTriggered);
			Assert.False(interceptor.BeforeReaderDisposeAsyncTriggered);

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.Person.ToListAsync();

#if NETFRAMEWORK
			Assert.True(interceptor.BeforeReaderDisposeTriggered);
			Assert.False(interceptor.BeforeReaderDisposeAsyncTriggered);
#else
			Assert.False(interceptor.BeforeReaderDisposeTriggered);
			Assert.True(interceptor.BeforeReaderDisposeAsyncTriggered);
#endif
		}

		[Test]
		public async ValueTask BeforeReaderDisposeTestOnDataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);

			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);

			db.GetTable<Person>().ToList();

			Assert.True(interceptor.BeforeReaderDisposeTriggered);
			Assert.False(interceptor.BeforeReaderDisposeAsyncTriggered);

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.GetTable<Person>().ToListAsync();

#if NETFRAMEWORK
			Assert.True(interceptor.BeforeReaderDisposeTriggered);
			Assert.False(interceptor.BeforeReaderDisposeAsyncTriggered);
#else
			Assert.False(interceptor.BeforeReaderDisposeTriggered);
			Assert.True(interceptor.BeforeReaderDisposeAsyncTriggered);
#endif
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

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				db.Child.ToList();

				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				db.Child.ToList();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				db.Close();

				db.Child.ToList();

				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				await db.Child.ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await db.Child.ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				db.Close();

				await db.Child.ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);
			}
		}

		[Test]
		public void ConnectionOpenOnDataConnectionCloningTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor1 = new TestConnectionInterceptor();
				var interceptor2 = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor1);

				using (var clonedDb = (DataConnection)db.Clone())
				{
					// test interceptor not propagaded to cloned connection after clone
					db.AddInterceptor(interceptor2);

					Assert.False(interceptor1.ConnectionOpenedTriggered);
					Assert.False(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpenedTriggered);
					Assert.False(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);

					db.Child.ToList();

					Assert.True(interceptor1.ConnectionOpenedTriggered);
					Assert.True(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.True(interceptor2.ConnectionOpenedTriggered);
					Assert.True(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);

					interceptor1.ConnectionOpenedTriggered = false;
					interceptor1.ConnectionOpeningTriggered = false;
					interceptor2.ConnectionOpenedTriggered = false;
					interceptor2.ConnectionOpeningTriggered = false;

					clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.ConnectionOpenedTriggered);
					Assert.True(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpenedTriggered);
					Assert.False(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);
				}
			}
		}

		[Test]
		public void ConnectionOpenOnDataContextCloningTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using (var db = new DataContext(context))
			{
				db.CloseAfterUse = closeAfterUse;

				var interceptor1 = new TestConnectionInterceptor();
				var interceptor2 = new TestConnectionInterceptor();
				db.AddInterceptor(interceptor1);

				using (var clonedDb = (DataContext)((IDataContext)db).Clone(true))
				{
					// test interceptor not propagaded to cloned connection after clone
					db.AddInterceptor(interceptor2);

					Assert.False(interceptor1.ConnectionOpenedTriggered);
					Assert.False(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpenedTriggered);
					Assert.False(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);

					db.GetTable<Child>().ToList();

					Assert.True(interceptor1.ConnectionOpenedTriggered);
					Assert.True(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.True(interceptor2.ConnectionOpenedTriggered);
					Assert.True(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);

					interceptor1.ConnectionOpenedTriggered = false;
					interceptor1.ConnectionOpeningTriggered = false;
					interceptor2.ConnectionOpenedTriggered = false;
					interceptor2.ConnectionOpeningTriggered = false;

					clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.ConnectionOpenedTriggered);
					Assert.True(interceptor1.ConnectionOpeningTriggered);
					Assert.False(interceptor1.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor1.ConnectionOpeningAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpenedTriggered);
					Assert.False(interceptor2.ConnectionOpeningTriggered);
					Assert.False(interceptor2.ConnectionOpenedAsyncTriggered);
					Assert.False(interceptor2.ConnectionOpeningAsyncTriggered);
				}
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

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				db.GetTable<Child>().ToList();

				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				db.GetTable<Child>().ToList();

				// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedTriggered);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedTriggered = false;
				interceptor.ConnectionOpeningTriggered = false;

				((IDataContext)db).Close();

				db.GetTable<Child>().ToList();

				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);
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

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);

				await db.GetTable<Child>().ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await db.GetTable<Child>().ToListAsync();

				// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedAsyncTriggered);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningAsyncTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);

				interceptor.ConnectionOpenedAsyncTriggered = false;
				interceptor.ConnectionOpeningAsyncTriggered = false;

				await ((IDataContext)db).CloseAsync();

				await db.GetTable<Child>().ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);
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

				Assert.AreEqual(count, interceptor1.EntityCreatedContexts.Count);
				Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == db));
				interceptor1.EntityCreatedContexts.Clear();

				using (var clonedDb = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					_ = clonedDb.GetTable<Person>().ToList();

					Assert.AreEqual(count, interceptor1.EntityCreatedContexts.Count);
					Assert.AreEqual(count, interceptor2.EntityCreatedContexts.Count);
					Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == clonedDb));
					Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == clonedDb));
				}
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

				Assert.AreEqual(count, interceptor1.EntityCreatedContexts.Count);
				Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == db));

				interceptor1.EntityCreatedContexts.Clear();

				using (var clonedDb = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					clonedDb.GetTable<Person>().ToList();

					Assert.AreEqual(count, interceptor1.EntityCreatedContexts.Count);
					Assert.AreEqual(count, interceptor2.EntityCreatedContexts.Count);
					Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == clonedDb));
					Assert.True(interceptor1.EntityCreatedContexts.All(ctx => ctx == clonedDb));
				}
			}
		}

#endregion
#region OnClosing/OnClosed

		[Test]
		public void CloseEvents_DataConnection_Or_RemoteContext([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor1 = new TestDataContextInterceptor();
			var interceptor2 = new TestDataContextInterceptor();

			IDataContext main;
			IDataContext cloned;

			using (var db = main = GetDataContext(context))
			{
				db.AddInterceptor(interceptor1);

				db.GetTable<Person>().ToList();

				Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

				using (var clonedDb = cloned = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					clonedDb.GetTable<Person>().ToList();

					Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

					Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
				}

				Assert.AreEqual(1, interceptor1.OnClosedContexts.Count);
				Assert.True(interceptor1.OnClosedContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor1.OnClosedContexts[cloned]);
				Assert.AreEqual(1, interceptor1.OnClosingContexts.Count);
				Assert.True(interceptor1.OnClosingContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor1.OnClosingContexts[cloned]);
				Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

				Assert.AreEqual(1, interceptor2.OnClosedContexts.Count);
				Assert.True(interceptor2.OnClosedContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor2.OnClosedContexts[cloned]);
				Assert.AreEqual(1, interceptor2.OnClosingContexts.Count);
				Assert.True(interceptor2.OnClosingContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor2.OnClosingContexts[cloned]);
				Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
			}

			Assert.AreEqual(2, interceptor1.OnClosedContexts.Count);
			Assert.True(interceptor1.OnClosedContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosedContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor1.OnClosedContexts[main]);
			Assert.AreEqual(1, interceptor1.OnClosedContexts[cloned]);

			Assert.AreEqual(2, interceptor1.OnClosingContexts.Count);
			Assert.True(interceptor1.OnClosingContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosingContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor1.OnClosingContexts[main]);
			Assert.AreEqual(1, interceptor1.OnClosingContexts[cloned]);

			Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
			Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

			Assert.AreEqual(1, interceptor2.OnClosedContexts.Count);
			Assert.True(interceptor2.OnClosedContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor2.OnClosedContexts[cloned]);
			Assert.AreEqual(1, interceptor2.OnClosingContexts.Count);
			Assert.True(interceptor2.OnClosingContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor2.OnClosingContexts[cloned]);
			Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
			Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
		}

		[Test]
		public void CloseEvents_DataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor1 = new TestDataContextInterceptor();
			var interceptor2 = new TestDataContextInterceptor();

			IDataContext main;
			IDataContext cloned;

			using (var db = main = new DataContext(context))
			{
				db.AddInterceptor(interceptor1);

				_ = db.GetTable<Person>().ToList();

				Assert.AreEqual(1, interceptor1.OnClosedContexts.Count);
				Assert.True(interceptor1.OnClosedContexts.Keys.Single() is DataConnection);
				Assert.True(interceptor1.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(1, interceptor1.OnClosingContexts.Count);
				Assert.True(interceptor1.OnClosingContexts.Keys.Single() is DataConnection);
				Assert.True(interceptor1.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

				using (var clonedDb = cloned = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					_ = clonedDb.GetTable<Person>().ToList();

					Assert.AreEqual(2, interceptor1.OnClosedContexts.Count);
					Assert.True(interceptor1.OnClosedContexts.Keys.All(_ => _ is DataConnection));
					Assert.True(interceptor1.OnClosedContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(2, interceptor1.OnClosingContexts.Count);
					Assert.True(interceptor1.OnClosingContexts.Keys.All(_ => _ is DataConnection));
					Assert.True(interceptor1.OnClosingContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

					Assert.AreEqual(1, interceptor2.OnClosedContexts.Count);
					Assert.True(interceptor2.OnClosedContexts.Keys.Single() is DataConnection);
					Assert.True(interceptor2.OnClosedContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(1, interceptor2.OnClosingContexts.Count);
					Assert.True(interceptor2.OnClosingContexts.Keys.Single() is DataConnection);
					Assert.True(interceptor2.OnClosingContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
				}

				Assert.AreEqual(3, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(2, interceptor1.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor1.OnClosedContexts.ContainsKey(cloned));
				Assert.True(interceptor1.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(3, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(2, interceptor1.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor1.OnClosingContexts.ContainsKey(cloned));
				Assert.True(interceptor1.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

				Assert.AreEqual(2, interceptor2.OnClosedContexts.Count);
				Assert.AreEqual(1, interceptor2.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor2.OnClosedContexts.ContainsKey(cloned));
				Assert.True(interceptor2.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(2, interceptor2.OnClosingContexts.Count);
				Assert.AreEqual(1, interceptor2.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor2.OnClosingContexts.ContainsKey(cloned));
				Assert.True(interceptor2.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
			}

			Assert.AreEqual(4, interceptor1.OnClosedContexts.Count);
			Assert.AreEqual(2, interceptor1.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor1.OnClosedContexts.ContainsKey(cloned));
			Assert.True(interceptor1.OnClosedContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosedContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(4, interceptor1.OnClosingContexts.Count);
			Assert.AreEqual(2, interceptor1.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor1.OnClosingContexts.ContainsKey(cloned));
			Assert.True(interceptor1.OnClosingContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosingContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
			Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

			Assert.AreEqual(2, interceptor2.OnClosedContexts.Count);
			Assert.AreEqual(1, interceptor2.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor2.OnClosedContexts.ContainsKey(cloned));
			Assert.True(interceptor2.OnClosedContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(2, interceptor2.OnClosingContexts.Count);
			Assert.AreEqual(1, interceptor2.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor2.OnClosingContexts.ContainsKey(cloned));
			Assert.True(interceptor2.OnClosingContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
			Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
		}

		[Test]
		public async Task CloseEvents_DataConnection_Or_RemoteContext_Async([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor1 = new TestDataContextInterceptor();
			var interceptor2 = new TestDataContextInterceptor();

			IDataContext main;
			IDataContext cloned;

			await using (var db = main = GetDataContext(context))
			{
				db.AddInterceptor(interceptor1);

				await db.GetTable<Person>().ToListAsync();

				Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

				await using (var clonedDb = cloned = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					await clonedDb.GetTable<Person>().ToListAsync();

					Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingAsyncContexts.Count);

					Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosedAsyncContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingAsyncContexts.Count);
				}

				Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(1, interceptor1.OnClosedAsyncContexts.Count);
				Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor1.OnClosedAsyncContexts[cloned]);
				Assert.AreEqual(1, interceptor1.OnClosingAsyncContexts.Count);
				Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor1.OnClosingAsyncContexts[cloned]);

				Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
				Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts.Count);
				Assert.True(interceptor2.OnClosedAsyncContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts[cloned]);
				Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts.Count);
				Assert.True(interceptor2.OnClosingAsyncContexts.ContainsKey(cloned));
				Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts[cloned]);
			}

			Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
			Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);

			Assert.AreEqual(2, interceptor1.OnClosedAsyncContexts.Count);
			Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor1.OnClosedAsyncContexts[main]);
			Assert.AreEqual(1, interceptor1.OnClosedAsyncContexts[cloned]);

			Assert.AreEqual(2, interceptor1.OnClosingAsyncContexts.Count);
			Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor1.OnClosingAsyncContexts[main]);
			Assert.AreEqual(1, interceptor1.OnClosingAsyncContexts[cloned]);

			Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
			Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
			Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts.Count);
			Assert.True(interceptor2.OnClosedAsyncContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts[cloned]);
			Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts.Count);
			Assert.True(interceptor2.OnClosingAsyncContexts.ContainsKey(cloned));
			Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts[cloned]);
		}

		[Test]
		public async Task CloseEvents_DataContext_Async([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var interceptor1 = new TestDataContextInterceptor();
			var interceptor2 = new TestDataContextInterceptor();

			IDataContext main;
			IDataContext cloned;

			await using (var db = main = new DataContext(context))
			{
				db.AddInterceptor(interceptor1);

				await db.GetTable<Person>().ToListAsync();

				Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(1, interceptor1.OnClosedAsyncContexts.Count);
				Assert.True(interceptor1.OnClosedAsyncContexts.Keys.Single() is DataConnection);
				Assert.True(interceptor1.OnClosedAsyncContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(1, interceptor1.OnClosingAsyncContexts.Count);
				Assert.True(interceptor1.OnClosingAsyncContexts.Keys.Single() is DataConnection);
				Assert.True(interceptor1.OnClosingAsyncContexts.Values.All(_ => _ == 1));


				await using (var clonedDb = cloned = (IDataContext)((IDataContext)db).Clone(true))
				{
					clonedDb.AddInterceptor(interceptor2);
					await clonedDb.GetTable<Person>().ToListAsync();

					Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
					Assert.AreEqual(2, interceptor1.OnClosedAsyncContexts.Count);
					Assert.True(interceptor1.OnClosedAsyncContexts.Keys.All(_ => _ is DataConnection));
					Assert.True(interceptor1.OnClosedAsyncContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(2, interceptor1.OnClosingAsyncContexts.Count);
					Assert.True(interceptor1.OnClosingAsyncContexts.Keys.All(_ => _ is DataConnection));
					Assert.True(interceptor1.OnClosingAsyncContexts.Values.All(_ => _ == 1));

					Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
					Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
					Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts.Count);
					Assert.True(interceptor2.OnClosedAsyncContexts.Keys.Single() is DataConnection);
					Assert.True(interceptor2.OnClosedAsyncContexts.Values.All(_ => _ == 1));
					Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts.Count);
					Assert.True(interceptor2.OnClosingAsyncContexts.Keys.Single() is DataConnection);
					Assert.True(interceptor2.OnClosingAsyncContexts.Values.All(_ => _ == 1));
				}

				Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
				Assert.AreEqual(3, interceptor1.OnClosedAsyncContexts.Count);
				Assert.AreEqual(2, interceptor1.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(cloned));
				Assert.True(interceptor1.OnClosedAsyncContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(3, interceptor1.OnClosingAsyncContexts.Count);
				Assert.AreEqual(2, interceptor1.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(cloned));
				Assert.True(interceptor1.OnClosingAsyncContexts.Values.All(_ => _ == 1));

				Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
				Assert.AreEqual(2, interceptor2.OnClosedAsyncContexts.Count);
				Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor2.OnClosedAsyncContexts.ContainsKey(cloned));
				Assert.True(interceptor2.OnClosedAsyncContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(2, interceptor2.OnClosingAsyncContexts.Count);
				Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor2.OnClosingAsyncContexts.ContainsKey(cloned));
				Assert.True(interceptor2.OnClosingAsyncContexts.Values.All(_ => _ == 1));
			}

			Assert.AreEqual(0, interceptor1.OnClosedContexts.Count);
			Assert.AreEqual(0, interceptor1.OnClosingContexts.Count);
			Assert.AreEqual(4, interceptor1.OnClosedAsyncContexts.Count);
			Assert.AreEqual(2, interceptor1.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(cloned));
			Assert.True(interceptor1.OnClosedAsyncContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosedAsyncContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(4, interceptor1.OnClosingAsyncContexts.Count);
			Assert.AreEqual(2, interceptor1.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(cloned));
			Assert.True(interceptor1.OnClosingAsyncContexts.ContainsKey(main));
			Assert.True(interceptor1.OnClosingAsyncContexts.Values.All(_ => _ == 1));

			Assert.AreEqual(0, interceptor2.OnClosedContexts.Count);
			Assert.AreEqual(0, interceptor2.OnClosingContexts.Count);
			Assert.AreEqual(2, interceptor2.OnClosedAsyncContexts.Count);
			Assert.AreEqual(1, interceptor2.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor2.OnClosedAsyncContexts.ContainsKey(cloned));
			Assert.True(interceptor2.OnClosedAsyncContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(2, interceptor2.OnClosingAsyncContexts.Count);
			Assert.AreEqual(1, interceptor2.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor2.OnClosingAsyncContexts.ContainsKey(cloned));
			Assert.True(interceptor2.OnClosingAsyncContexts.Values.All(_ => _ == 1));
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

				Assert.AreEqual(0, interceptor.OnClosedContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				db.Close();

				Assert.AreEqual(1, interceptor.OnClosedContexts.Count);
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosedContexts[main]);
				Assert.AreEqual(1, interceptor.OnClosingContexts.Count);
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosingContexts[main]);
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				db.GetTable<Person>().ToList();

				Assert.AreEqual(1, interceptor.OnClosedContexts.Count);
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosedContexts[main]);
				Assert.AreEqual(1, interceptor.OnClosingContexts.Count);
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosingContexts[main]);
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				await db.CloseAsync();

				Assert.AreEqual(1, interceptor.OnClosedContexts.Count);
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosedContexts[main]);
				Assert.AreEqual(1, interceptor.OnClosingContexts.Count);
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosingContexts[main]);
				Assert.AreEqual(1, interceptor.OnClosedAsyncContexts.Count);
				Assert.True(interceptor.OnClosedAsyncContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosedAsyncContexts[main]);
				Assert.AreEqual(1, interceptor.OnClosingAsyncContexts.Count);
				Assert.True(interceptor.OnClosingAsyncContexts.ContainsKey(main));
				Assert.AreEqual(1, interceptor.OnClosingAsyncContexts[main]);
			}

			Assert.AreEqual(1, interceptor.OnClosedContexts.Count);
			Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
			Assert.AreEqual(2, interceptor.OnClosedContexts[main]);
			Assert.AreEqual(1, interceptor.OnClosingContexts.Count);
			Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
			Assert.AreEqual(2, interceptor.OnClosingContexts[main]);
			Assert.AreEqual(1, interceptor.OnClosedAsyncContexts.Count);
			Assert.True(interceptor.OnClosedAsyncContexts.ContainsKey(main));
			Assert.AreEqual(1, interceptor.OnClosedAsyncContexts[main]);
			Assert.AreEqual(1, interceptor.OnClosingAsyncContexts.Count);
			Assert.True(interceptor.OnClosingAsyncContexts.ContainsKey(main));
			Assert.AreEqual(1, interceptor.OnClosingAsyncContexts[main]);
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

				Assert.AreEqual(1, interceptor.OnClosedContexts.Count);
				Assert.True(interceptor.OnClosedContexts.Keys.All(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosedContexts.Values.All(_ => _  == 1));
				Assert.AreEqual(1, interceptor.OnClosingContexts.Count);
				Assert.True(interceptor.OnClosingContexts.Keys.All(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				db.Close();

				Assert.AreEqual(2, interceptor.OnClosedContexts.Count);
				Assert.AreEqual(1, interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(2, interceptor.OnClosingContexts.Count);
				Assert.AreEqual(1, interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				db.GetTable<Person>().ToList();

				Assert.AreEqual(3, interceptor.OnClosedContexts.Count);
				Assert.AreEqual(2, interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(3, interceptor.OnClosingContexts.Count);
				Assert.AreEqual(2, interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(0, interceptor.OnClosedAsyncContexts.Count);
				Assert.AreEqual(0, interceptor.OnClosingAsyncContexts.Count);

				await db.CloseAsync();

				Assert.AreEqual(3, interceptor.OnClosedContexts.Count);
				Assert.AreEqual(2, interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosedContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(3, interceptor.OnClosingContexts.Count);
				Assert.AreEqual(2, interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
				Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosingContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(1, interceptor.OnClosedAsyncContexts.Count);
				Assert.True(interceptor.OnClosedAsyncContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1));
				Assert.AreEqual(1, interceptor.OnClosingAsyncContexts.Count);
				Assert.True(interceptor.OnClosingAsyncContexts.ContainsKey(main));
				Assert.True(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1));
			}

			Assert.AreEqual(3, interceptor.OnClosedContexts.Count);
			Assert.AreEqual(2, interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor.OnClosedContexts.ContainsKey(main));
			Assert.AreEqual(2, interceptor.OnClosedContexts[main]);
			Assert.True(interceptor.OnClosedContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1));
			Assert.AreEqual(3, interceptor.OnClosingContexts.Count);
			Assert.AreEqual(2, interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection));
			Assert.True(interceptor.OnClosingContexts.ContainsKey(main));
			Assert.AreEqual(2, interceptor.OnClosingContexts[main]);
			Assert.True(interceptor.OnClosingContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1));
			Assert.AreEqual(1, interceptor.OnClosedAsyncContexts.Count);
			Assert.True(interceptor.OnClosedAsyncContexts.ContainsKey(main));
			Assert.True(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1));
			Assert.AreEqual(1, interceptor.OnClosingAsyncContexts.Count);
			Assert.True(interceptor.OnClosingAsyncContexts.ContainsKey(main));
			Assert.True(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1));
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

				Assert.That(GetOpenedCount(), Is.Zero);
				Assert.That(GetClosedCount(), Is.Zero);

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				Assert.That(GetOpenedCount(), Is.Not.Zero);
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

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

				Assert.That(GetOpenedCount(), Is.Zero);
				Assert.That(GetClosedCount(), Is.Zero);

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				db.GetTable<Parent>().LoadWith(p => p.Children).ToList();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				await db.GetTable<Person>().LoadWith(p => p.Patient).ToListAsync();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				await db.GetTable<Parent>().LoadWith(p => p.Children).ToListAsync();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefault();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefault();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				await db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefaultAsync();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				await db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefaultAsync();
				Assert.That(GetOpenedCount(), Is.EqualTo(1));
				Assert.That(GetClosedCount(), Is.Zero);

				await db.CloseAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
			}

			Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

			int GetOpenedCount() => openInterceptor.ConnectionOpenedCount + openInterceptor.ConnectionOpenedAsyncCount;
			int GetClosedCount() => closeInterceptor.OnClosedContexts.Concat(closeInterceptor.OnClosedAsyncContexts).Where(c => c.Key is DataConnection).Sum(c => c.Value);
		}

		#endregion

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
	}
}
