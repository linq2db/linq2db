using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Configuration;
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
		public void CommandInitializedOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void CommandInitializedOnDataConnectionCloningTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void CommandInitializedOnDataContextCloningTest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
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

					db.GetTable<Child>().ToList();

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

					db.GetTable<Person>().ToList();

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

		// test interceptors registration using fluent options builder
		[Test]
		public void CommandInitializedOnDataConnectionTest_OptionsBuilder([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var triggered1 = false;
			var triggered2 = false;
			var interceptor1 = new TestCommandInterceptor();
			var interceptor2 = new TestCommandInterceptor();

			var options = new LinqToDbConnectionOptionsBuilder()
				.UseConfigurationString(context)
				.WithInterceptor(interceptor1)
				.WithInterceptor(interceptor2);

			using (var db = new DataConnection(options.Build()))
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
		public void CommandInitializedOnDataContextTest_OptionsBuilder([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
		{
			var triggered1 = false;
			var triggered2 = false;
			var interceptor1 = new TestCommandInterceptor();
			var interceptor2 = new TestCommandInterceptor();

			var options = new LinqToDbConnectionOptionsBuilder()
				.UseConfigurationString(context)
				.WithInterceptor(interceptor1)
				.WithInterceptor(interceptor2);

			using (var db = new DataContext(options.Build()))
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

		public void CommandInitializedOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
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
		public void DataConnection_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.True(interceptor.ExecuteNonQueryTriggered);
					Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
				}
			}
		}

		[Test]
		public async Task DataConnection_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			await using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await using(await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
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
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.True(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
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
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.True(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataConnection_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				db.Child.ToList();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.True(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public async Task DataConnection_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			await using (var db = GetDataConnection(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await db.Child.ToListAsync();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.True(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataContext_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				using (db.CreateTempTable<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
					Assert.True(interceptor.ExecuteNonQueryTriggered);
					Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
				}
			}
		}

		[Test]
		public async Task DataContext_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			await using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await using (await db.CreateTempTableAsync<InterceptorsTestsTable>())
				{
					Assert.False(interceptor.ExecuteScalarTriggered);
					Assert.False(interceptor.ExecuteScalarAsyncTriggered);
					Assert.False(interceptor.ExecuteReaderTriggered);
					Assert.False(interceptor.ExecuteReaderAsyncTriggered);
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
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.True(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
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
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.True(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				// also true, as for sqlite we generate two queries
				Assert.True(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public void DataContext_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				db.GetTable<Child>().ToList();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.True(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		[Test]
		public async Task DataContext_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			await using (var db = new DataContext(context))
			{
				var interceptor = new TestCommandInterceptor();
				db.AddInterceptor(interceptor);

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.False(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);

				await db.GetTable<Child>().ToListAsync();

				Assert.False(interceptor.ExecuteScalarTriggered);
				Assert.False(interceptor.ExecuteScalarAsyncTriggered);
				Assert.False(interceptor.ExecuteReaderTriggered);
				Assert.True(interceptor.ExecuteReaderAsyncTriggered);
				Assert.False(interceptor.ExecuteNonQueryTriggered);
				Assert.False(interceptor.ExecuteNonQueryAsyncTriggered);
			}
		}

		#endregion

		#endregion

		#region IConnectionInterceptor

		#region Open Connection
		[Test]
		public void ConnectionOpenOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public async Task ConnectionOpenAsyncOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void ConnectionOpenOnDataConnectionCloningTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
		public void ConnectionOpenOnDataContextCloningTest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
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
		public void ConnectionOpenOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
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

				db.ReleaseQuery();

				db.GetTable<Child>().ToList();

				Assert.True(interceptor.ConnectionOpenedTriggered);
				Assert.True(interceptor.ConnectionOpeningTriggered);
				Assert.False(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.False(interceptor.ConnectionOpeningAsyncTriggered);
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
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

				db.ReleaseQuery();

				await db.GetTable<Child>().ToListAsync();

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.True(interceptor.ConnectionOpenedAsyncTriggered);
				Assert.True(interceptor.ConnectionOpeningAsyncTriggered);
			}
		}

		#endregion

		#endregion

		private class TestCommandInterceptor : CommandInterceptor
		{
			public bool CommandInitializedTriggered { get; set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				CommandInitializedTriggered = true;

				return base.CommandInitialized(eventData, command);
			}

			public bool ExecuteNonQueryTriggered      { get; set; }
			public bool ExecuteNonQueryAsyncTriggered { get; set; }
			public bool ExecuteReaderTriggered        { get; set; }
			public bool ExecuteReaderAsyncTriggered   { get; set; }
			public bool ExecuteScalarTriggered        { get; set; }
			public bool ExecuteScalarAsyncTriggered   { get; set; }

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
		}

		private class TestConnectionInterceptor : ConnectionInterceptor
		{
			public bool ConnectionOpenedTriggered       { get; set; }
			public bool ConnectionOpenedAsyncTriggered  { get; set; }
			public bool ConnectionOpeningTriggered      { get; set; }
			public bool ConnectionOpeningAsyncTriggered { get; set; }

			public override void ConnectionOpened(ConnectionOpenedEventData eventData, DbConnection connection)
			{
				ConnectionOpenedTriggered = true;
				base.ConnectionOpened(eventData, connection);
			}

			public override Task ConnectionOpenedAsync(ConnectionOpenedEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				ConnectionOpenedAsyncTriggered = true;
				return base.ConnectionOpenedAsync(eventData, connection, cancellationToken);
			}

			public override void ConnectionOpening(ConnectionOpeningEventData eventData, DbConnection connection)
			{
				ConnectionOpeningTriggered = true;
				base.ConnectionOpening(eventData, connection);
			}

			public override Task ConnectionOpeningAsync(ConnectionOpeningEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				ConnectionOpeningAsyncTriggered = true;
				return base.ConnectionOpeningAsync(eventData, connection, cancellationToken);
			}
		}

		[Table]
		public class InterceptorsTestsTable
		{
			[Column, Identity] public int ID;
		}
	}
}
