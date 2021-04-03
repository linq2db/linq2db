using System.Data.Common;
using System.Linq;
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

				using (var clonedDb = (DataConnection)db.Clone())
				{
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

					// test that cloned connection still preserve non-fired one-time interceptors
					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					Assert.False(interceptor1.CommandInitializedTriggered);
					Assert.False(interceptor2.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);

					clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);

					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					clonedDb.GetTable<Person>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
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

				using (var clonedDb = (DataContext)((IDataContext)db).Clone(true))
				{
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

					// test that cloned connection still preserve non-fired one-time interceptors
					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					Assert.False(interceptor1.CommandInitializedTriggered);
					Assert.False(interceptor2.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);

					clonedDb.GetTable<Child>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.True(triggered1);
					Assert.True(triggered2);

					triggered1 = false;
					triggered2 = false;
					interceptor1.CommandInitializedTriggered = false;
					interceptor2.CommandInitializedTriggered = false;

					clonedDb.GetTable<Person>().ToList();

					Assert.True(interceptor1.CommandInitializedTriggered);
					Assert.True(interceptor2.CommandInitializedTriggered);
					Assert.False(triggered1);
					Assert.False(triggered2);
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

		private class TestCommandInterceptor : CommandInterceptor
		{
			public bool CommandInitializedTriggered { get; set; }

			public override DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command)
			{
				CommandInitializedTriggered = true;

				return base.CommandInitialized(eventData, command);
			}
		}
	}
}
