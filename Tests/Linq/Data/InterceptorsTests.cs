using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Interceptors;
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
			using (var db = new TestDataConnection(context))
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
			using (var db = new TestDataConnection(context))
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

		// DataContext: test that interceptors triggered and one-time interceptors removed safely after single command
		[Test]
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

		#endregion

		#region IConnectionInterceptor

		#region Open Connection
		[Test]
		public void ConnectionOpenOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = new TestDataConnection(context))
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
			using (var db = new TestDataConnection(context))
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
			using (var db = new TestDataConnection(context))
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

				Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedTriggered);
				Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningTriggered);
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

				Assert.False(interceptor.ConnectionOpenedTriggered);
				Assert.False(interceptor.ConnectionOpeningTriggered);
				Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedAsyncTriggered);
				Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningAsyncTriggered);

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

			public override DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command)
			{
				CommandInitializedTriggered = true;

				return base.CommandInitialized(eventData, command);
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
	}
}
