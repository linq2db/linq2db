using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

using ExpressionKind = LinqToDB.Interceptors.QueryExpressionArgs.ExpressionKind;

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
			using var db = GetDataConnection(context);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}

			db.Child.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.True);
				Assert.That(triggered2, Is.True);
			}

			triggered1 = false;
			triggered2 = false;
			interceptor1.CommandInitializedTriggered = false;
			interceptor2.CommandInitializedTriggered = false;

			db.Person.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
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

			using var db = new DataConnection(builder);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.True);
				Assert.That(triggered2, Is.True);
			}

			triggered1 = false;
			triggered2 = false;
			interceptor1.CommandInitializedTriggered = false;
			interceptor2.CommandInitializedTriggered = false;

			db.GetTable<Person>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
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

			using var db = new DataContext(builder);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.True);
				Assert.That(triggered2, Is.True);
			}

			triggered1 = false;
			triggered2 = false;
			interceptor1.CommandInitializedTriggered = false;
			interceptor2.CommandInitializedTriggered = false;

			db.GetTable<Person>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}
		}

		private void CommandInitializedOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.False);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.False);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.True);
				Assert.That(triggered2, Is.True);
			}

			triggered1 = false;
			triggered2 = false;
			interceptor1.CommandInitializedTriggered = false;
			interceptor2.CommandInitializedTriggered = false;

			db.GetTable<Person>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor1.CommandInitializedTriggered, Is.True);
				Assert.That(interceptor2.CommandInitializedTriggered, Is.True);
				Assert.That(triggered1, Is.False);
				Assert.That(triggered2, Is.False);
			}
		}

		#endregion

		#region ICommandInterceptor.Execute*
		[Test]
		public void DataConnection_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			using (db.CreateTempTable<InterceptorsTestsTable>())
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				}
			}
		}

		[Test]
		public async Task DataConnection_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using var db = GetDataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await using (await db.CreateTempTableAsync<InterceptorsTestsTable>())
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
				}
			}
		}

		[Test]
		public void DataConnection_ExecuteScalar([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool combinedCommands)
		{
			using var db = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			using var table = db.CreateTempTable<InterceptorsTestsTable>();
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

			// Which callbacks fire depends on whether the identity insert runs combined. Combined: INSERT + SELECT
			// last_insert_rowid() go as ONE command via ExecuteReader, with rows-affected and the identity harvested
			// from the reader — so ExecuteScalar / ExecuteNonQuery are never called. Sequential: two commands, the
			// INSERT as a non-query and the identity SELECT as a scalar.
			//
			// Read back from the context rather than reused from combinedCommands, so the expectation cannot drift from
			// the engine's own gate: it also accounts for provider capability and for contexts the combined executor
			// cannot serve at all.
			var combined = db.UsesCombinedCommands();

			interceptor.ShouldSatisfyAllConditions(
				i => i.ExecuteScalarTriggered            .ShouldBe(!combined),
				i => i.ExecuteScalarAsyncTriggered       .ShouldBeFalse(),
				i => i.ExecuteReaderTriggered            .ShouldBe(combined),
				i => i.ExecuteReaderAsyncTriggered       .ShouldBeFalse(),
				i => i.ExecuteAfterExecuteReaderTriggered.ShouldBe(combined),
				i => i.ExecuteNonQueryTriggered          .ShouldBe(!combined),
				i => i.ExecuteNonQueryAsyncTriggered     .ShouldBeFalse());
		}

		[Test]
		public async Task DataConnection_ExecuteScalarAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool combinedCommands)
		{
			await using var db = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));
			await using var table = await db.CreateTempTableAsync<InterceptorsTestsTable>();
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

			// Combined into ONE command via ExecuteReaderAsync (see the sync test): the async reader interceptors
			// fire, not ExecuteScalarAsync / ExecuteNonQueryAsync. Sequential instead runs a non-query INSERT and a
			// scalar identity SELECT.
			var combined = db.UsesCombinedCommands();

			interceptor.ShouldSatisfyAllConditions(
				i => i.ExecuteScalarTriggered            .ShouldBeFalse(),
				i => i.ExecuteScalarAsyncTriggered       .ShouldBe(!combined),
				i => i.ExecuteReaderTriggered            .ShouldBeFalse(),
				i => i.ExecuteReaderAsyncTriggered       .ShouldBe(combined),
				i => i.ExecuteAfterExecuteReaderTriggered.ShouldBe(combined),
				i => i.ExecuteNonQueryTriggered          .ShouldBeFalse(),
				i => i.ExecuteNonQueryAsyncTriggered     .ShouldBe(!combined));
		}

		[Test]
		public void DataConnection_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			db.Child.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}
		}

		[Test]
		public async Task DataConnection_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using var db = GetDataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await db.Child.ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.True);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}
		}

		[Test]
		public void DataContext_ExecuteNonQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = new DataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			using (db.CreateTempTable<InterceptorsTestsTable>())
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.True);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
				}
			}
		}

		[Test]
		public async Task DataContext_ExecuteNonQueryAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using var db = new DataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await using (await db.CreateTempTableAsync<InterceptorsTestsTable>())
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
					Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
					Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
					Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.True);
				}
			}
		}

		[Test]
		public void DataContext_ExecuteScalar([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool combinedCommands)
		{
			// use non-temp table as sqlite temp tables are session-bound and context recreates session
			using var db = new DataContext(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(combinedCommands));
			using var table = db.CreateTempTable<InterceptorsTestsTable>(tableOptions: TableOptions.None);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			table.InsertWithIdentity(() => new InterceptorsTestsTable() { ID = 1 });

			// Combined into ONE command via ExecuteReader (see DataConnection_ExecuteScalar): reader interceptors
			// fire, not ExecuteScalar / ExecuteNonQuery. Sequential runs the two commands separately instead.
			var combined = db.UsesCombinedCommands();

			interceptor.ShouldSatisfyAllConditions(
				i => i.ExecuteScalarTriggered            .ShouldBe(!combined),
				i => i.ExecuteScalarAsyncTriggered       .ShouldBeFalse(),
				i => i.ExecuteReaderTriggered            .ShouldBe(combined),
				i => i.ExecuteReaderAsyncTriggered       .ShouldBeFalse(),
				i => i.ExecuteAfterExecuteReaderTriggered.ShouldBe(combined),
				i => i.ExecuteNonQueryTriggered          .ShouldBe(!combined),
				i => i.ExecuteNonQueryAsyncTriggered     .ShouldBeFalse());
		}

		[Test]
		public async Task DataContext_ExecuteScalarAsync([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool combinedCommands)
		{
			// use non-temp table as sqlite temp tables are session-bound and context recreates session
			await using var db = new DataContext(new DataOptions()
				.UseConfiguration(context)
				.UseCombinedCommands(combinedCommands));
			await using var table = await db.CreateTempTableAsync<InterceptorsTestsTable>(tableOptions: TableOptions.None);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await table.InsertWithIdentityAsync(() => new InterceptorsTestsTable() { ID = 1 });

			// Combined into ONE command via ExecuteReaderAsync (see the sync test): the async reader interceptors
			// fire, not ExecuteScalarAsync / ExecuteNonQueryAsync. Sequential runs the two commands separately.
			var combined = db.UsesCombinedCommands();

			interceptor.ShouldSatisfyAllConditions(
				i => i.ExecuteScalarTriggered            .ShouldBeFalse(),
				i => i.ExecuteScalarAsyncTriggered       .ShouldBe(!combined),
				i => i.ExecuteReaderTriggered            .ShouldBeFalse(),
				i => i.ExecuteReaderAsyncTriggered       .ShouldBe(combined),
				i => i.ExecuteAfterExecuteReaderTriggered.ShouldBe(combined),
				i => i.ExecuteNonQueryTriggered          .ShouldBeFalse(),
				i => i.ExecuteNonQueryAsyncTriggered     .ShouldBe(!combined));
		}

		[Test]
		public void DataContext_ExecuteReader([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = new DataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}
		}

		[Test]
		public async Task DataContext_ExecuteReaderAsync([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			await using var db = new DataContext(context);
			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
			}

			await db.GetTable<Child>().ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ExecuteScalarTriggered, Is.False);
				Assert.That(interceptor.ExecuteScalarAsyncTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderTriggered, Is.False);
				Assert.That(interceptor.ExecuteReaderAsyncTriggered, Is.True);
				Assert.That(interceptor.ExecuteAfterExecuteReaderTriggered, Is.True);
				Assert.That(interceptor.ExecuteNonQueryTriggered, Is.False);
				Assert.That(interceptor.ExecuteNonQueryAsyncTriggered, Is.False);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
			}

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.Person.ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.False);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.True);
			}
		}

			[Test]
		public async ValueTask BeforeReaderDisposeTestOnDataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);

			var interceptor = new TestCommandInterceptor();
			db.AddInterceptor(interceptor);

			db.GetTable<Person>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.True);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.False);
			}

			interceptor.BeforeReaderDisposeTriggered = false;

			await db.GetTable<Person>().ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.BeforeReaderDisposeTriggered, Is.False);
				Assert.That(interceptor.BeforeReaderDisposeAsyncTriggered, Is.True);
			}
		}

		#endregion

		#endregion

		#region IConnectionInterceptor

		#region Open Connection
		[Test]
		public void ConnectionOpenOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var interceptor = new TestConnectionInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			db.Child.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			interceptor.ConnectionOpenedTriggered = false;
			interceptor.ConnectionOpeningTriggered = false;

			db.Child.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			db.Close();

			db.Child.ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataConnectionTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var interceptor = new TestConnectionInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			await db.Child.ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
			}

			interceptor.ConnectionOpenedAsyncTriggered = false;
			interceptor.ConnectionOpeningAsyncTriggered = false;

			await db.Child.ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			db.Close();

			await db.Child.ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
			}
		}

		[Test]
		public void ConnectionOpenOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);
			// test that interceptors not lost after underlying data connection recreation
			db.CloseAfterUse = closeAfterUse;

			var interceptor = new TestConnectionInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			interceptor.ConnectionOpenedTriggered = false;
			interceptor.ConnectionOpeningTriggered = false;

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedTriggered);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningTriggered);
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			interceptor.ConnectionOpenedTriggered = false;
			interceptor.ConnectionOpeningTriggered = false;

			((IDataContext)db).Close();

			db.GetTable<Child>().ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}
		}

		[Test]
		public async Task ConnectionOpenAsyncOnDataContextTest([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool closeAfterUse)
		{
			using var db = new DataContext(context);
			// test that interceptors not lost after underlying data connection recreation
			db.CloseAfterUse = closeAfterUse;

			var interceptor = new TestConnectionInterceptor();
			db.AddInterceptor(interceptor);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.False);
			}

			await db.GetTable<Child>().ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
			}

			interceptor.ConnectionOpenedAsyncTriggered = false;
			interceptor.ConnectionOpeningAsyncTriggered = false;

			await db.GetTable<Child>().ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				// TODO: right now enumerable queries behave like CloseAfterUse=true for data context
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpenedAsyncTriggered);
				//Assert.AreEqual(closeAfterUse, interceptor.ConnectionOpeningAsyncTriggered);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
			}

			interceptor.ConnectionOpenedAsyncTriggered = false;
			interceptor.ConnectionOpeningAsyncTriggered = false;

			await ((IDataContext)db).CloseAsync();

			await db.GetTable<Child>().ToListAsync();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.ConnectionOpenedTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpeningTriggered, Is.False);
				Assert.That(interceptor.ConnectionOpenedAsyncTriggered, Is.True);
				Assert.That(interceptor.ConnectionOpeningAsyncTriggered, Is.True);
			}
		}

		#endregion

		#endregion

		#region IDataContextInterceptor

		#region EntityCreated

		[Test]
		public void EntityCreated_DataConnection_Or_RemoteContext([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var count = db.Person.Count();
			var interceptor1 = new TestEntityServiceInterceptor();
			var interceptor2 = new TestEntityServiceInterceptor();
			db.AddInterceptor(interceptor1);

			_ = db.Person.ToList();

			Assert.That(interceptor1.EntityCreatedContexts, Has.Count.EqualTo(count));
			Assert.That(interceptor1.EntityCreatedContexts.All(ctx => ctx == db), Is.True);
		}

		[Test]
		public void EntityCreated_DataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = new DataContext(context);
			var count = db.GetTable<Person>().Count();
			var interceptor1 = new TestEntityServiceInterceptor();
			var interceptor2 = new TestEntityServiceInterceptor();
			db.AddInterceptor(interceptor1);

			db.GetTable<Person>().ToList();

			Assert.That(interceptor1.EntityCreatedContexts, Has.Count.EqualTo(count));
			Assert.That(interceptor1.EntityCreatedContexts.All(ctx => ctx == db), Is.True);
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
				Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Single(), Is.InstanceOf<DataConnection>());
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.Keys.Single(), Is.InstanceOf<DataConnection>());
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
				Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts, Is.Empty);
				Assert.That(interceptor.OnClosingContexts, Is.Empty);

				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));

				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts.Keys.Single(), Is.InstanceOf<DataConnection>());
					Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingAsyncContexts.Keys.Single(), Is.InstanceOf<DataConnection>());
					Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
				}
			}

			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts, Is.Empty);
				Assert.That(interceptor.OnClosingContexts, Is.Empty);
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(2));
				Assert.That(interceptor.OnClosedAsyncContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(2));
				Assert.That(interceptor.OnClosingAsyncContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts, Is.Empty);
					Assert.That(interceptor.OnClosingContexts, Is.Empty);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				db.Close();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				await db.CloseAsync();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
				}
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts[main], Is.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts[main], Is.EqualTo(1));
			}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.Keys.All(_ => _ is DataConnection), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.Keys.All(_ => _ is DataConnection), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				db.Close();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(2));
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(1));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				db.GetTable<Person>().ToList();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Is.Empty);
					Assert.That(interceptor.OnClosingAsyncContexts, Is.Empty);
				}

				await db.CloseAsync();

				Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
					Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
					Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
					Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
					Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
				}
			}

			Assert.That(interceptor.OnClosedContexts, Has.Count.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
				Assert.That(interceptor.OnClosedContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosedContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1), Is.True);
				Assert.That(interceptor.OnClosingContexts, Has.Count.EqualTo(3));
				Assert.That(interceptor.OnClosingContexts.Keys.Count(_ => _ is DataConnection), Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingContexts[main], Is.EqualTo(2));
				Assert.That(interceptor.OnClosingContexts.Where(_ => _.Key is DataConnection).All(_ => _.Value == 1), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosedAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosedAsyncContexts.Values.All(_ => _ == 1), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts, Has.Count.EqualTo(1));
				Assert.That(interceptor.OnClosingAsyncContexts.ContainsKey(main), Is.True);
				Assert.That(interceptor.OnClosingAsyncContexts.Values.All(_ => _ == 1), Is.True);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4457")]
		public async Task Test_Connection_Release_EagerLoad_AutoConnection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
		{
			var closeInterceptor = new TestDataContextInterceptor();
			var openInterceptor = new TestConnectionInterceptor();

			IDataContext main;
			using (var db = main = new DataContext(context))
			{
				((DataContext)db).SetKeepConnectionAlive(false);
				db.CloseAfterUse = closeAfterUse;
				db.AddInterceptor(closeInterceptor);
				db.AddInterceptor(openInterceptor);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.Zero);
					Assert.That(GetClosedCount(), Is.Zero);
				}

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.Not.Zero);
					Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
				}

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
				((DataContext)db).SetKeepConnectionAlive(true);
				db.CloseAfterUse = closeAfterUse;
				db.AddInterceptor(closeInterceptor);
				db.AddInterceptor(openInterceptor);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.Zero);
					Assert.That(GetClosedCount(), Is.Zero);
				}

				db.GetTable<Person>().LoadWith(p => p.Patient).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				db.GetTable<Parent>().LoadWith(p => p.Children).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				await db.GetTable<Person>().LoadWith(p => p.Patient).ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				await db.GetTable<Parent>().LoadWith(p => p.Children).ToListAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefault();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefault();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				await db.GetTable<Person>().LoadWith(p => p.Patient).FirstOrDefaultAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				await db.GetTable<Parent>().LoadWith(p => p.Children).FirstOrDefaultAsync();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetOpenedCount(), Is.EqualTo(1));
					Assert.That(GetClosedCount(), Is.Zero);
				}

				await db.CloseAsync();
				Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));
			}

			Assert.That(GetClosedCount(), Is.EqualTo(GetOpenedCount()));

			int GetOpenedCount() => openInterceptor.ConnectionOpenedCount + openInterceptor.ConnectionOpenedAsyncCount;
			int GetClosedCount() => closeInterceptor.OnClosedContexts.Concat(closeInterceptor.OnClosedAsyncContexts).Where(c => c.Key is DataConnection).Sum(c => c.Value);
		}

		#endregion

		#endregion

		static readonly MappingSchema _personFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<Person>()
				.HasQueryFilter(p => p.ID > 0)
			.Build()
			.MappingSchema;

		static readonly MappingSchema _personNamedFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<Person>()
				.HasQueryFilter("positive", p => p.ID > 0)
				.HasQueryFilter("bounded",  p => p.ID < 1000)
			.Build()
			.MappingSchema;

		static readonly MappingSchema _personLowHighFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<Person>()
				.HasQueryFilter("low",  p => p.ID <= 2)
				.HasQueryFilter("high", p => p.ID >= 3)
			.Build()
			.MappingSchema;

		static readonly MappingSchema _personNamedFuncFilterSchema = new FluentMappingBuilder(new MappingSchema())
			.Entity<Person>()
				.HasQueryFilter("positive", (q, dc) => q.Where(p => p.ID > 0))
				.HasQueryFilter("bounded",  (q, dc) => q.Where(p => p.ID < 1000))
			.Build()
			.MappingSchema;

		/// <summary>
		/// Appends <c>Where(p => p.ID &lt;= Limit)</c> to the expression of the given kind. The limit is read from the
		/// interceptor instance, as a per-tenant interceptor reads its tenant id.
		/// </summary>
		sealed class PersonLimitInterceptor(ExpressionKind kind, int limit) : IQueryExpressionInterceptor
		{
			const string Marker = "__limit";

			public int Limit => limit;

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != kind || !typeof(IQueryable<Person>).IsAssignableFrom(expression.Type))
					return expression;

				// already applied: ExposedQuery runs again on its own output
				if (expression is MethodCallExpression { Method.Name: nameof(Queryable.Where) } call
					&& call.Arguments[1] is UnaryExpression { Operand: LambdaExpression { Parameters: [{ Name: Marker }] } })
				{
					return expression;
				}

				var p    = Expression.Parameter(typeof(Person), Marker);
				var body = Expression.LessThanOrEqual(
					Expression.Field(p, nameof(Model.Person.ID)),
					Expression.Property(Expression.Constant(this), nameof(Limit)));

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Person)],
					expression,
					Expression.Quote(Expression.Lambda<Func<Person, bool>>(body, p)));
			}
		}

		/// <summary>
		/// Filters by tenant at the given stage; the admin (no tenant) leaves the query unchanged. The cache key tells the
		/// tenants' translations apart.
		/// </summary>
		sealed class TenantLimitInterceptor(ExpressionKind kind, int? tenant) : IQueryExpressionInterceptor, IInterceptorCacheKey
		{
			readonly PersonLimitInterceptor? _limit = tenant == null ? null : new PersonLimitInterceptor(kind, tenant.Value);

			public string? CacheKey => tenant?.ToString(CultureInfo.InvariantCulture);

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				return _limit?.ProcessExpression(expression, args) ?? expression;
			}
		}

		/// <summary>
		/// <see cref="TenantLimitInterceptor"/> without <see cref="IInterceptorCacheKey"/>.
		/// </summary>
		sealed class UnkeyedTenantLimitInterceptor(ExpressionKind kind, int? tenant) : IQueryExpressionInterceptor
		{
			readonly PersonLimitInterceptor? _limit = tenant == null ? null : new PersonLimitInterceptor(kind, tenant.Value);

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				return _limit?.ProcessExpression(expression, args) ?? expression;
			}
		}

		/// <summary>
		/// <see cref="UnkeyedTenantLimitInterceptor"/> at the query filter stage, applied only to a filter chain that contains
		/// the "bounded" filter of <see cref="_personNamedFilterSchema"/>.
		/// </summary>
		sealed class BoundedChainTenantInterceptor(int? tenant) : IQueryExpressionInterceptor
		{
			readonly PersonLimitInterceptor? _limit = tenant == null ? null : new PersonLimitInterceptor(ExpressionKind.QueryFilter, tenant.Value);

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (_limit == null || expression.Find(static e => e.NodeType == ExpressionType.LessThan) == null)
					return expression;

				return _limit.ProcessExpression(expression, args);
			}
		}

		/// <summary>
		/// Appends <c>Where(p => p.ID &lt;= 2)</c> at its stage once after being armed, and counts the calls of that stage.
		/// </summary>
		sealed class ArmOnceInterceptor(ExpressionKind kind) : IQueryExpressionInterceptor
		{
			public bool Armed { get; set; }
			public int  Calls { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != kind || !typeof(IQueryable<Person>).IsAssignableFrom(expression.Type))
					return expression;

				Calls++;

				if (!Armed)
					return expression;

				Armed = false;

				var p    = Expression.Parameter(typeof(Person), "p");
				var body = Expression.LessThanOrEqual(Expression.Field(p, nameof(Model.Person.ID)), Expression.Constant(2));

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Person)],
					expression,
					Expression.Quote(Expression.Lambda<Func<Person, bool>>(body, p)));
			}
		}

		/// <summary>
		/// Once armed, returns at its stage a new root call over the same arguments: a new tree equal to its input.
		/// </summary>
		sealed class CopyRootInterceptor(ExpressionKind kind) : IQueryExpressionInterceptor
		{
			public bool Armed  { get; set; }
			public int  Copies { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (!Armed || args.Kind != kind || expression is not MethodCallExpression call)
					return expression;

				Copies++;

				return Expression.Call(call.Object, call.Method, call.Arguments);
			}
		}

		sealed class Bound(int value)
		{
			public int Value => value;
		}

		/// <summary>
		/// Appends <c>Where(p => p.ID &gt;= Low &amp;&amp; p.ID &lt;= High)</c> to the query filter. Equal bounds are equal
		/// values at build time, which the translator may merge into one parameter.
		/// </summary>
		sealed class PersonRangeInterceptor(int low, int high) : IQueryExpressionInterceptor
		{
			readonly Bound _low  = new(low);
			readonly Bound _high = new(high);

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != ExpressionKind.QueryFilter || !typeof(IQueryable<Person>).IsAssignableFrom(expression.Type))
					return expression;

				var p    = Expression.Parameter(typeof(Person), "p");
				var id   = Expression.Field(p, nameof(Model.Person.ID));
				var body = Expression.AndAlso(
					Expression.GreaterThanOrEqual(id, Expression.Property(Expression.Constant(_low),  nameof(Bound.Value))),
					Expression.LessThanOrEqual   (id, Expression.Property(Expression.Constant(_high), nameof(Bound.Value))));

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Person)],
					expression,
					Expression.Quote(Expression.Lambda<Func<Person, bool>>(body, p)));
			}
		}

		static readonly Func<IDataContext, IQueryable<Person>> _compiledPersons =
			CompiledQuery.Compile((IDataContext db) => db.GetTable<Person>());

		static readonly Func<IDataContext, int> _compiledPersonCount =
			CompiledQuery.Compile((IDataContext db) => db
				.GetTable<Person>()
				.Count());

		static readonly Func<IDataContext, IQueryable<Person>> _compiledPersonsTwice =
			CompiledQuery.Compile((IDataContext db) => db
				.GetTable<Person>()
				.Concat(db.GetTable<Person>()));

		static readonly Func<IDataContext, int, IQueryable<Person>> _compiledPersonsFrom =
			CompiledQuery.Compile((IDataContext db, int minId) => db
				.GetTable<Person>()
				.Where(p => p.ID >= minId));

		static readonly Func<IDataContext, string[], IQueryable<Person>> _compiledPersonsIgnoring =
			CompiledQuery.Compile((IDataContext db, string[] ignored) => db
				.GetTable<Person>()
				.IgnoreFilters(ignored));

		/// <summary>
		/// <c>field IN (values)</c> with the values inlined when the query is translated: a
		/// <see cref="SqlQueryDependentAttribute"/> argument that is a collection but not an array.
		/// </summary>
		[Sql.Extension("{field} IN ({values, ', '})", IsPredicate = true, BuilderType = typeof(InValuesBuilder), ServerSideOnly = true)]
		static bool InValues([ExprParameter] int field, [SqlQueryDependent] List<int> values) => throw new ServerSideOnlyException(nameof(InValues));

		sealed class InValuesBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				foreach (var value in builder.GetValue<List<int>>("values"))
					builder.AddParameter("values", new SqlValue(value));
			}
		}

		static readonly Func<IDataContext, List<int>, IQueryable<Person>> _compiledPersonsIn =
			CompiledQuery.Compile((IDataContext db, List<int> ids) => db
				.GetTable<Person>()
				.Where(p => InValues(p.ID, ids)));

		/// <summary>
		/// Materializes <see cref="Person.FirstName"/> as a constant.
		/// </summary>
		sealed class FirstNameBindingInterceptor : EntityBindingInterceptor
		{
			public const string FirstName = "bound";

			public override SqlGenericConstructorExpression ConvertConstructorExpression(SqlGenericConstructorExpression expression)
			{
				if (expression.Type != typeof(Person))
					return expression;

				var assignments = expression.Assignments
					.Select(a => a.MemberInfo.Name == nameof(Model.Person.FirstName) ? a.WithExpression(Expression.Constant(FirstName, typeof(string))) : a)
					.ToList()
					.AsReadOnly();

				return expression.ReplaceAssignments(assignments);
			}
		}

		// The query selects entities, so that the Query and ExposedQuery stages see IQueryable<Person>.
		static int[] PersonIds(IQueryable<Person> query)
		{
			return query
				.AsEnumerable()
				.Select(p => p.ID)
				.OrderBy(id => id)
				.ToArray();
		}

		static int[] PersonIds(IDataContext db)
		{
			return PersonIds(db.GetTable<Person>());
		}

		static void ShouldReturnIds(IQueryable<Person> query, int[] expected, string step)
		{
			PersonIds(query)
				.ShouldBe(expected, step);
		}

		static void ShouldReturnIds(IDataContext db, int[] expected, string step)
		{
			ShouldReturnIds(db.GetTable<Person>(), expected, step);
		}

		int[] ExpectedIds(int limit = int.MaxValue)
		{
			return Person
				.Select(p => p.ID)
				.Where(id => id <= limit)
				.OrderBy(id => id)
				.ToArray();
		}

		static DataContext CreateFilteredDataContext(string context, bool disableQueryCache = false)
		{
			return new DataContext(new DataOptions()
				.UseConfiguration(context)
				.UseMappingSchema(_personFilterSchema)
				.UseDisableQueryCache(disableQueryCache));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_AddInterceptor_SeparatesCachedQueries(
			[IncludeDataSources(true, TestProvName.AllSQLite)]                                       string         context,
			[Values(ExpressionKind.Query, ExpressionKind.ExposedQuery, ExpressionKind.QueryFilter)] ExpressionKind kind,
			[Values]                                                                                 bool           interceptedFirst)
		{
			Query.ClearCaches();

			using var intercepted = GetDataContext(context, o => o.UseMappingSchema(_personFilterSchema));
			using var plain       = GetDataContext(context, o => o.UseMappingSchema(_personFilterSchema));

			ShouldReturnIds(intercepted, ExpectedIds(), "before");

			intercepted.AddInterceptor(new PersonLimitInterceptor(kind, 2));

			int[] interceptedIds, plainIds;

			if (interceptedFirst)
			{
				interceptedIds = PersonIds(intercepted);
				plainIds       = PersonIds(plain);
			}
			else
			{
				plainIds       = PersonIds(plain);
				interceptedIds = PersonIds(intercepted);
			}

			interceptedIds.ShouldBe(ExpectedIds(2));
			plainIds.ShouldBe(ExpectedIds());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_DataContextAddInterceptorAfterQuery([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			using var intercepted = CreateFilteredDataContext(context);
			using var plain       = CreateFilteredDataContext(context);

			ShouldReturnIds(intercepted, ExpectedIds(), "before");

			intercepted.AddInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, 2));

			ShouldReturnIds(intercepted, ExpectedIds(2), "after");
			ShouldReturnIds(plain,       ExpectedIds(),  "plain");
		}

		// Instances of one interceptor type with different state, as per-tenant interceptors are: they share one cached
		// query, and each context still reads its own value.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameTypeInstances_ShareCachedQuery([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			var missCount = Query<Person>.CacheMissCount;

			for (var limit = 1; limit <= 3; limit++)
			{
				using var db = GetDataContext(context, o => o
					.UseMappingSchema(_personFilterSchema)
					.UseInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, limit)));

				ShouldReturnIds(db, ExpectedIds(limit), $"limit {limit}");
			}

			var misses = Query<Person>.CacheMissCount - missCount;

			misses.ShouldBe(1);
		}

		// The same for a query that references the filtered table more than once: every reference reads the value of the
		// executing context's interceptor.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameTypeInstances_SeveralReferences_ShareCachedQuery(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   join)
		{
			Query.ClearCaches();

			var missCount = Query<Person>.CacheMissCount;

			for (var limit = 1; limit <= 3; limit++)
			{
				using var db = GetDataContext(context, o => o
					.UseMappingSchema(_personFilterSchema)
					.UseInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, limit)));

				IQueryable<Person> query;
				int[]              expected;

				if (join)
				{
					query =
						from a in db.GetTable<Person>()
						join b in db.GetTable<Person>() on a.ID equals b.ID
						select b;

					expected = ExpectedIds(limit);
				}
				else
				{
					query = db.GetTable<Person>()
						.Concat(db.GetTable<Person>());

					expected = ExpectedIds(limit)
						.Concat(ExpectedIds(limit))
						.OrderBy(id => id)
						.ToArray();
				}

				ShouldReturnIds(query, expected, $"limit {limit}");
			}

			var misses = Query<Person>.CacheMissCount - missCount;

			misses.ShouldBe(1);
		}

		// Instances of one type that translate the query differently tell the cache apart through IInterceptorCacheKey.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_CacheKey_SeparatesInstances(
			[IncludeDataSources(true, TestProvName.AllSQLite)]                   string         context,
			[Values(ExpressionKind.ExposedQuery, ExpressionKind.QueryFilter)] ExpressionKind kind,
			[Values]                                                             bool           adminFirst)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new TenantLimitInterceptor(kind, tenant)));

			using var admin   = Create(null);
			using var tenant2 = Create(2);
			using var tenant3 = Create(3);

			if (adminFirst)
				ShouldReturnIds(admin, ExpectedIds(), "admin first");

			ShouldReturnIds(tenant2, ExpectedIds(2), "tenant 2");
			ShouldReturnIds(tenant3, ExpectedIds(3), "tenant 3");
			ShouldReturnIds(admin,   ExpectedIds(),  "admin");
		}

		// A compiled query reuses its query by the configuration ID, which does not tell interceptor instances apart: a query
		// filter they rewrite is checked against the executing context first.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameTypeInstances_CompiledQuery([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			IDataContext Create(int limit) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, limit)));

			using var limit2 = Create(2);
			using var limit3 = Create(3);

			ShouldReturnIds(_compiledPersons(limit2), ExpectedIds(2), "limit 2");
			ShouldReturnIds(_compiledPersons(limit3), ExpectedIds(3), "limit 3");
			ShouldReturnIds(_compiledPersons(limit2), ExpectedIds(2), "limit 2 again");

			var counts = new[]
			{
				_compiledPersonCount(limit2),
				_compiledPersonCount(limit3),
			};

			counts.ShouldBe([ExpectedIds(2).Length, ExpectedIds(3).Length]);
		}

		// Values equal when the query was built may share one parameter; another context whose values differ must not reuse
		// that query, compiled or not.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_MergedParameters_NotReusedForOtherValues(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   compiled)
		{
			Query.ClearCaches();

			IDataContext Create(int low, int high) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new PersonRangeInterceptor(low, high)));

			IQueryable<Person> Persons(IDataContext db) => compiled ? _compiledPersons(db) : db.GetTable<Person>();

			int[] Expected(int low, int high) => ExpectedIds(high)
				.Where(id => id >= low)
				.ToArray();

			using var single = Create(2, 2);
			using var range  = Create(1, 3);

			ShouldReturnIds(Persons(single), Expected(2, 2), "2..2");
			ShouldReturnIds(Persons(range),  Expected(1, 3), "1..3");
		}

		// Two references to one table with different active filter sets in one query: the interceptor rewrites one of the
		// two filter chains, and only for a tenant.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_DifferentFilterSetsOnOneTable(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   funcFilters,
			[Values]                                           bool   adminFirst)
		{
			Query.ClearCaches();

			var ms = funcFilters ? _personNamedFuncFilterSchema : _personNamedFilterSchema;

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(ms)
				.UseInterceptor(new BoundedChainTenantInterceptor(tenant)));

			static IQueryable<Person> BothChains(IDataContext db)
			{
				var positive = db
					.GetTable<Person>()
					.IgnoreFilters(["positive"]);

				return db
					.GetTable<Person>()
					.IgnoreFilters(["bounded"])
					.Concat(positive);
			}

			int[] Expected(int limit) => ExpectedIds()
				.Concat(ExpectedIds(limit))
				.OrderBy(id => id)
				.ToArray();

			using var admin   = Create(null);
			using var tenant2 = Create(2);

			if (adminFirst)
				ShouldReturnIds(BothChains(admin), Expected(int.MaxValue), "admin first");

			ShouldReturnIds(BothChains(tenant2), Expected(2),            "tenant 2");
			ShouldReturnIds(BothChains(admin),   Expected(int.MaxValue), "admin");
		}

		// Two references to one table with the same filter set in one query: each takes the executing context's value.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameFilterSetReferencedTwice(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   compiled)
		{
			Query.ClearCaches();

			IDataContext Create(int limit) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, limit)));

			IQueryable<Person> Twice(IDataContext db) => compiled
				? _compiledPersonsTwice(db)
				: db
					.GetTable<Person>()
					.Concat(db.GetTable<Person>());

			int[] Expected(int limit) => ExpectedIds(limit)
				.Concat(ExpectedIds(limit))
				.OrderBy(id => id)
				.ToArray();

			using var limit2 = Create(2);
			using var limit3 = Create(3);

			ShouldReturnIds(Twice(limit2), Expected(2), "limit 2");
			ShouldReturnIds(Twice(limit3), Expected(3), "limit 3");
			ShouldReturnIds(Twice(limit2), Expected(2), "limit 2 again");
		}

		// The query filter stage of an interceptor without IInterceptorCacheKey runs once when the query is built, and the
		// rewrite it returns is used.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_QueryFilterStageRunsOnceOnBuild([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			using var db = GetDataConnection(context, o => o.UseMappingSchema(_personFilterSchema));

			var interceptor = new ArmOnceInterceptor(ExpressionKind.QueryFilter) { Armed = true };

			db.AddInterceptor(interceptor);

			ShouldReturnIds(db, ExpectedIds(2), "armed");

			interceptor.Calls.ShouldBe(1, "stage calls");
		}

		// Without IInterceptorCacheKey, instances of one type that differ in whether they rewrite the query are told apart by
		// checking their rewrites again when a query is reused.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_ConditionalRewriteNotShared(
			[IncludeDataSources(true, TestProvName.AllSQLite)]                   string         context,
			[Values(ExpressionKind.ExposedQuery, ExpressionKind.QueryFilter)] ExpressionKind kind,
			[Values]                                                             bool           adminFirst)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(kind, tenant)));

			using var admin   = Create(null);
			using var tenant2 = Create(2);
			using var tenant3 = Create(3);

			if (adminFirst)
				ShouldReturnIds(admin, ExpectedIds(), "admin first");

			ShouldReturnIds(tenant2, ExpectedIds(2), "tenant 2");
			ShouldReturnIds(admin,   ExpectedIds(),  "admin");
			ShouldReturnIds(tenant3, ExpectedIds(3), "tenant 3 after admin");
		}

		// The same for a compiled query, which applies the query filter stage only.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_ConditionalRewriteNotShared_CompiledQuery(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   adminFirst)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, tenant)));

			using var admin   = Create(null);
			using var tenant2 = Create(2);
			using var tenant3 = Create(3);

			if (adminFirst)
				ShouldReturnIds(_compiledPersons(admin), ExpectedIds(), "admin first");

			ShouldReturnIds(_compiledPersons(tenant2), ExpectedIds(2), "tenant 2");
			ShouldReturnIds(_compiledPersons(admin),   ExpectedIds(),  "admin");
			ShouldReturnIds(_compiledPersons(tenant3), ExpectedIds(3), "tenant 3 after admin");
		}

		// Contexts alternating on one compiled query that need different queries for it: each keeps its own built query
		// instead of rebuilding on every switch.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_CompiledQueryAlternatingContexts([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, tenant)));

			using var admin   = Create(null);
			using var tenant2 = Create(2);

			var adminQuery  = _compiledPersons(admin);
			var tenantQuery = _compiledPersons(tenant2);

			ShouldReturnIds(adminQuery,  ExpectedIds(),  "admin");
			ShouldReturnIds(tenantQuery, ExpectedIds(2), "tenant 2");

			// the query an IQueryable a compiled query returned runs with, read once it was enumerated
			var adminInfo  = adminQuery.GetCompiledQueryInfo();
			var tenantInfo = tenantQuery.GetCompiledQueryInfo();

			adminInfo.ShouldNotBeSameAs(tenantInfo);

			for (var i = 0; i < 3; i++)
			{
				adminQuery  = _compiledPersons(admin);
				tenantQuery = _compiledPersons(tenant2);

				ShouldReturnIds(adminQuery,  ExpectedIds(),  $"admin, round {i}");
				ShouldReturnIds(tenantQuery, ExpectedIds(2), $"tenant 2, round {i}");

				adminQuery
					.GetCompiledQueryInfo()
					.ShouldBeSameAs(adminInfo, $"admin, round {i}");

				tenantQuery
					.GetCompiledQueryInfo()
					.ShouldBeSameAs(tenantInfo, $"tenant 2, round {i}");
			}

			// with an argument: the query the tenant finds among the others its context needs reads it as well
			foreach (var minId in new[] { 2, 1, 3 })
			{
				var adminExpected = ExpectedIds()
					.Where(id => id >= minId)
					.ToArray();

				var tenantExpected = ExpectedIds(2)
					.Where(id => id >= minId)
					.ToArray();

				ShouldReturnIds(_compiledPersonsFrom(admin,   minId), adminExpected,  $"admin from {minId}");
				ShouldReturnIds(_compiledPersonsFrom(tenant2, minId), tenantExpected, $"tenant 2 from {minId}");
			}
		}

		// Many compiled queries of one shape: the queries one of them keeps for its contexts do not push out another's.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_ManyCompiledQueriesOfOneShape([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, tenant)));

			using var admin   = Create(null);
			using var tenant2 = Create(2);

			var compiled = Enumerable.Range(0, 20)
				.Select(_ => CompiledQuery.Compile((IDataContext db) => db.GetTable<Person>()))
				.ToArray();

			var tenantInfos = new object[compiled.Length];

			for (var i = 0; i < compiled.Length; i++)
			{
				ShouldReturnIds(compiled[i](admin), ExpectedIds(), $"admin, query {i}");

				var query = compiled[i](tenant2);

				ShouldReturnIds(query, ExpectedIds(2), $"tenant 2, query {i}, first");

				tenantInfos[i] = query.GetCompiledQueryInfo();
			}

			for (var i = 0; i < compiled.Length; i++)
			{
				var query = compiled[i](tenant2);

				ShouldReturnIds(query, ExpectedIds(2), $"tenant 2, query {i}");

				query
					.GetCompiledQueryInfo()
					.ShouldBeSameAs(tenantInfos[i], $"query {i}");
			}
		}

		// The same with a [SqlQueryDependent] argument, which selects the query by value: a query the tenant needs for one
		// value must not serve it for another.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_CompiledQueryAlternatingContexts_DependentArgument([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personLowHighFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, tenant)));

			using var admin   = Create(null);
			using var tenant3 = Create(3);

			IQueryable<Person> Ignoring(IDataContext db, string key) => _compiledPersonsIgnoring(db, [key]);

			var low  = ExpectedIds(2);
			var high = ExpectedIds()
				.Where(id => id >= 3)
				.ToArray();

			var tenantHigh = high
				.Where(id => id <= 3)
				.ToArray();

			for (var i = 0; i < 2; i++)
			{
				ShouldReturnIds(Ignoring(admin,   "high"), low,        $"admin, low, round {i}");
				ShouldReturnIds(Ignoring(admin,   "low"),  high,       $"admin, high, round {i}");
				ShouldReturnIds(Ignoring(tenant3, "low"),  tenantHigh, $"tenant 3, high, round {i}");
				ShouldReturnIds(Ignoring(tenant3, "high"), low,        $"tenant 3, low, round {i}");
			}
		}

		// The same with a [SqlQueryDependent] argument that is a collection but not an array: the query a tenant needs for
		// one list must not serve it for another.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_CompiledQueryAlternatingContexts_CollectionArgument([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			IDataContext Create(int? tenant) => GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, tenant)));

			using var admin   = Create(null);
			using var tenant3 = Create(3);

			int[] Expected(List<int> ids, int limit = int.MaxValue)
			{
				return ExpectedIds(limit)
					.Where(ids.Contains)
					.ToArray();
			}

			for (var i = 0; i < 2; i++)
			{
				foreach (var ids in new[] { new List<int> { 1, 2 }, new List<int> { 3, 4 } })
				{
					ShouldReturnIds(_compiledPersonsIn(admin,   ids), Expected(ids),    $"admin, [{string.Join(", ", ids)}], round {i}");
					ShouldReturnIds(_compiledPersonsIn(tenant3, ids), Expected(ids, 3), $"tenant 3, [{string.Join(", ", ids)}], round {i}");
				}
			}
		}

		// The same IQueryable executed after its interceptor was replaced by another instance of the same type, without
		// IInterceptorCacheKey, which rewrites where the first one did not.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameQueryable_InstanceReplacedWithoutCacheKey(
			[IncludeDataSources(false, TestProvName.AllSQLite)]                                      string         context,
			[Values(ExpressionKind.Query, ExpressionKind.ExposedQuery, ExpressionKind.QueryFilter)] ExpressionKind kind,
			[Values]                                                                                 bool           viaDataContext)
		{
			Query.ClearCaches();

			using IDataContext db = viaDataContext
				? CreateFilteredDataContext(context)
				: GetDataConnection(context, o => o.UseMappingSchema(_personFilterSchema));

			var unchanged = new UnkeyedTenantLimitInterceptor(kind, null);
			var limiting  = new UnkeyedTenantLimitInterceptor(kind, 2);
			var query     = db.GetTable<Person>();

			db.AddInterceptor(unchanged);

			ShouldReturnIds(query, ExpectedIds(), "unchanged");

			db.RemoveInterceptor(unchanged);
			db.AddInterceptor(limiting);

			ShouldReturnIds(query, ExpectedIds(2), "replaced");
		}

		// Each stage of an interceptor without IInterceptorCacheKey runs once per execution, and a rewrite it returns is used,
		// also when the same IQueryable is executed again.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_StagesRunOncePerExecution(
			[IncludeDataSources(false, TestProvName.AllSQLite)]          string         context,
			[Values(ExpressionKind.Query, ExpressionKind.ExposedQuery)] ExpressionKind kind,
			[Values]                                                     bool           sameQueryable)
		{
			Query.ClearCaches();

			using var db = GetDataConnection(context);

			var interceptor = new ArmOnceInterceptor(kind);
			var query       = db.GetTable<Person>();

			db.AddInterceptor(interceptor);

			ShouldReturnIds(query, ExpectedIds(), "unarmed");

			interceptor.Armed = true;

			var calls = interceptor.Calls;

			ShouldReturnIds(sameQueryable ? query : db.GetTable<Person>(), ExpectedIds(2), "armed");

			(interceptor.Calls - calls)
				.ShouldBe(1, "stage calls");
		}

		// An interceptor without IInterceptorCacheKey that returns a new tree equal to its input does not rewrite the query:
		// the query cached for its input is used.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_WithoutCacheKey_EqualTreeUsesCachedQuery(
			[IncludeDataSources(false, TestProvName.AllSQLite)]          string         context,
			[Values(ExpressionKind.Query, ExpressionKind.ExposedQuery)] ExpressionKind kind)
		{
			Query.ClearCaches();

			using var db = GetDataConnection(context);

			var interceptor = new CopyRootInterceptor(kind);

			db.AddInterceptor(interceptor);

			ShouldReturnIds(db, ExpectedIds(), "input returned");

			interceptor.Armed = true;

			var missCount = Query<Person>.CacheMissCount;

			ShouldReturnIds(db, ExpectedIds(), "copy returned");

			(Query<Person>.CacheMissCount - missCount)
				.ShouldBe(0, "cache misses");

			interceptor.Copies
				.ShouldBeGreaterThan(0, "copies");
		}

		// The IQueryable a compiled query returns, executed after the context's query interceptor changed - after its first
		// enumeration, or between the call and the first enumeration: one of another type added, or the instance replaced by
		// another of the same type, without IInterceptorCacheKey, that rewrites where the first one did not.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameCompiledQueryable_InterceptorChanged(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values]                                            bool   sameType,
			[Values]                                            bool   viaDataContext,
			[Values]                                            bool   enumeratedFirst)
		{
			Query.ClearCaches();

			using IDataContext db = viaDataContext
				? CreateFilteredDataContext(context)
				: GetDataConnection(context, o => o.UseMappingSchema(_personFilterSchema));

			var unchanged = new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, null);

			if (sameType)
				db.AddInterceptor(unchanged);

			var query = _compiledPersonsFrom(db, 2);

			int[] Expected(int limit) => ExpectedIds(limit)
				.Where(id => id >= 2)
				.ToArray();

			if (enumeratedFirst)
				ShouldReturnIds(query, Expected(int.MaxValue), "before");

			if (sameType)
				db.RemoveInterceptor(unchanged);

			db.AddInterceptor(new UnkeyedTenantLimitInterceptor(ExpressionKind.QueryFilter, 3));

			ShouldReturnIds(query, Expected(3), "after");
		}

		/// <summary>
		/// Delegates to <see cref="Inner"/>, which can be replaced at any time; without one, leaves expressions unchanged.
		/// </summary>
		sealed class ReplaceableInterceptor : IQueryExpressionInterceptor
		{
			public IQueryExpressionInterceptor? Inner { get; set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				return Inner?.ProcessExpression(expression, args) ?? expression;
			}
		}

		// The IQueryable a compiled query returns, enumerated after the context's query interceptor, without
		// IInterceptorCacheKey, changed what it rewrites: each enumeration runs the query the interceptor produces then.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_CompiledQueryable_InterceptorStateChanged([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			var interceptor = new ReplaceableInterceptor();

			using var db = GetDataConnection(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(interceptor));

			var query = _compiledPersons(db);

			interceptor.Inner = new InlinedLimitFilterInterceptor(3);

			ShouldReturnIds(query, ExpectedIds(3), "changed before the first enumeration");

			interceptor.Inner = new InlinedLimitFilterInterceptor(2);

			ShouldReturnIds(query, ExpectedIds(2), "changed before the second enumeration");
		}

		/// <summary>
		/// Leaves every expression unchanged and counts the query filters it is asked to rewrite. Declares
		/// <see cref="IInterceptorCacheKey"/>, so its rewrites are trusted to depend on its type and key only.
		/// </summary>
		sealed class FilterCallCounter : IQueryExpressionInterceptor, IInterceptorCacheKey
		{
			public string? CacheKey => null;

			public int QueryFilterCalls { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind == ExpressionKind.QueryFilter)
					QueryFilterCalls++;

				return expression;
			}
		}

		// An interceptor with IInterceptorCacheKey that leaves the query filters unchanged sees them when the query is built,
		// not on every cache hit.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_UnchangedQueryFilter_NotReevaluatedOnCacheHit([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			var counter = new FilterCallCounter();

			using var db = GetDataContext(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(counter));

			for (var i = 0; i < 3; i++)
			{
				ShouldReturnIds(db, ExpectedIds(), $"execution {i}");
			}

			counter.QueryFilterCalls.ShouldBe(1);
		}

		// A compiled query is reused as it is when the context's interceptor declares IInterceptorCacheKey: a query filter
		// given as a function is not built again on every call.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_UnchangedFuncFilter_CompiledQueryReusedAsIs([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			var counter = new FilterCallCounter();
			var count   = CompiledQuery.Compile((IDataContext db) => db
				.GetTable<Person>()
				.Count());

			using var db = GetDataConnection(context, o => o
				.UseMappingSchema(_personNamedFuncFilterSchema)
				.UseInterceptor(counter));

			for (var i = 0; i < 3; i++)
			{
				count(db)
					.ShouldBe(ExpectedIds().Length, $"call {i}");
			}

			counter.QueryFilterCalls.ShouldBe(1);
		}

		/// <summary>
		/// Appends <c>Where(p => p.ID &lt;= limit)</c> to the query filter with the limit inlined, so that each limit gets a
		/// query of its own, and counts the query filters it is asked to rewrite.
		/// </summary>
		sealed class InlinedLimitFilterInterceptor(int limit) : IQueryExpressionInterceptor
		{
			public int QueryFilterCalls { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != ExpressionKind.QueryFilter || !typeof(IQueryable<Person>).IsAssignableFrom(expression.Type))
					return expression;

				QueryFilterCalls++;

				var p    = Expression.Parameter(typeof(Person), "p");
				var body = Expression.LessThanOrEqual(Expression.Field(p, nameof(Model.Person.ID)), Expression.Constant(limit));

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Person)],
					expression,
					Expression.Quote(Expression.Lambda<Func<Person, bool>>(body, p)));
			}
		}

		// The cache keeps a query per tenant when a query interceptor inlines the tenant into the query filter. A lookup
		// rewrites the filter once for the executing context, not once per cached query it compares.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_QueryFilterRewrittenOncePerLookup(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values]                                            bool   compiled)
		{
			Query.ClearCaches();

			var interceptors = Enumerable.Range(1, 4)
				.Select(limit => new InlinedLimitFilterInterceptor(limit))
				.ToArray();

			var contexts = interceptors
				.Select(interceptor => GetDataConnection(context, o => o
					.UseMappingSchema(_personFilterSchema)
					.UseInterceptor(interceptor)))
				.ToArray();

			// a scalar, so that each execution makes one lookup
			int PersonCount(IDataContext db) => compiled
				? _compiledPersonCount(db)
				: db
					.GetTable<Person>()
					.Count();

			try
			{
				for (var round = 0; round < 2; round++)
				{
					for (var i = 0; i < contexts.Length; i++)
					{
						var calls = interceptors[i].QueryFilterCalls;

						PersonCount(contexts[i])
							.ShouldBe(ExpectedIds(i + 1).Length, $"round {round}, limit {i + 1}");

						if (round > 0)
							(interceptors[i].QueryFilterCalls - calls)
								.ShouldBe(1, $"query filter calls, limit {i + 1}");
					}
				}
			}
			finally
			{
				foreach (var db in contexts)
				{
					db.Dispose();
				}
			}
		}

		/// <summary>
		/// <see cref="InlinedLimitFilterInterceptor"/> with <see cref="IInterceptorCacheKey"/>.
		/// </summary>
		sealed class KeyedInlinedLimitFilterInterceptor(int limit) : IQueryExpressionInterceptor, IInterceptorCacheKey
		{
			readonly InlinedLimitFilterInterceptor _inner = new(limit);

			public string? CacheKey => limit.ToString(CultureInfo.InvariantCulture);

			public int QueryFilterCalls => _inner.QueryFilterCalls;

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				return _inner.ProcessExpression(expression, args);
			}
		}

		// One context executing a query: a query interceptor that rewrites the query filter is asked to rewrite it once when
		// the query is built, and, without IInterceptorCacheKey, once more on every cache hit.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_RewrittenQueryFilterCalls(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values]                                            bool   keyed)
		{
			Query.ClearCaches();

			var unkeyedInterceptor = new InlinedLimitFilterInterceptor(3);
			var keyedInterceptor   = new KeyedInlinedLimitFilterInterceptor(3);

			IQueryExpressionInterceptor interceptor = keyed ? keyedInterceptor : unkeyedInterceptor;

			int QueryFilterCalls() => keyed ? keyedInterceptor.QueryFilterCalls : unkeyedInterceptor.QueryFilterCalls;

			using var db = GetDataConnection(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(interceptor));

			for (var i = 0; i < 3; i++)
			{
				var calls = QueryFilterCalls();

				db
					.GetTable<Person>()
					.Count()
					.ShouldBe(ExpectedIds(3).Length, $"execution {i}");

				(QueryFilterCalls() - calls)
					.ShouldBe(keyed && i > 0 ? 0 : 1, $"query filter calls, execution {i}");
			}
		}

		// A compiled query that returns an IQueryable, without IInterceptorCacheKey: each enumeration rewrites the query
		// filter once, when it checks the query for the context as it is then. A call that builds the query rewrites it too.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_CompiledQueryable_QueryFilterRewrittenOncePerExecution([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			Query.ClearCaches();

			var interceptor = new InlinedLimitFilterInterceptor(3);

			using var db = GetDataConnection(context, o => o
				.UseMappingSchema(_personFilterSchema)
				.UseInterceptor(interceptor));

			for (var i = 0; i < 2; i++)
			{
				var built = i == 0 ? 1 : 0;
				var calls = interceptor.QueryFilterCalls;
				var query = _compiledPersons(db);

				ShouldReturnIds(query, ExpectedIds(3), $"call {i}");

				(interceptor.QueryFilterCalls - calls)
					.ShouldBe(built + 1, $"query filter calls, call {i}");

				ShouldReturnIds(query, ExpectedIds(3), $"call {i}, enumerated again");

				(interceptor.QueryFilterCalls - calls)
					.ShouldBe(built + 2, $"query filter calls, call {i}, enumerated again");
			}
		}

		// The same IQueryable executed while an interceptor is added and removed, with the query cache on or off: the query it
		// keeps and its own tree must not outlive the change.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameQueryableAcrossAddAndRemove(
			[IncludeDataSources(false, TestProvName.AllSQLite)]                                      string         context,
			[Values(ExpressionKind.Query, ExpressionKind.ExposedQuery, ExpressionKind.QueryFilter)] ExpressionKind kind,
			[Values]                                                                                 bool           viaDataContext,
			[Values]                                                                                 bool           disableQueryCache)
		{
			Query.ClearCaches();

			IDataContext Create() => viaDataContext
				? CreateFilteredDataContext(context, disableQueryCache)
				: GetDataConnection(context, o => o
					.UseMappingSchema(_personFilterSchema)
					.UseDisableQueryCache(disableQueryCache));

			using var db    = Create();
			using var other = Create();

			var interceptor = new PersonLimitInterceptor(kind, 2);
			var query       = db.GetTable<Person>();

			ShouldReturnIds(query, ExpectedIds(), "before");

			db.AddInterceptor(interceptor);

			ShouldReturnIds(query, ExpectedIds(2), "added");

			db.RemoveInterceptor(interceptor);

			ShouldReturnIds(query, ExpectedIds(), "removed");

			// what the context without the interceptor cached must not reach a context that has it
			other.AddInterceptor(new PersonLimitInterceptor(kind, 2));

			ShouldReturnIds(other, ExpectedIds(2), "other context with the interceptor");
		}

		/// <summary>
		/// Appends <c>Where(p => p.ID &lt;= Limit)</c> at the query stage, with the limit read from the interceptor instance
		/// and declared in the cache key, and counts the calls of that stage.
		/// </summary>
		sealed class KeyedQueryStageLimitInterceptor(int limit) : IQueryExpressionInterceptor, IInterceptorCacheKey
		{
			public string? CacheKey => limit.ToString(CultureInfo.InvariantCulture);

			public int Limit      => limit;
			public int QueryCalls { get; private set; }

			public Expression ProcessExpression(Expression expression, QueryExpressionArgs args)
			{
				if (args.Kind != ExpressionKind.Query || !typeof(IQueryable<Person>).IsAssignableFrom(expression.Type))
					return expression;

				QueryCalls++;

				var p    = Expression.Parameter(typeof(Person), "p");
				var body = Expression.LessThanOrEqual(
					Expression.Field(p, nameof(Model.Person.ID)),
					Expression.Property(Expression.Constant(this), nameof(Limit)));

				return Expression.Call(
					typeof(Queryable),
					nameof(Queryable.Where),
					[typeof(Person)],
					expression,
					Expression.Quote(Expression.Lambda<Func<Person, bool>>(body, p)));
			}
		}

		// The same IQueryable executed again under an interceptor with IInterceptorCacheKey that rewrites at the query stage:
		// it keeps the query built for the rewrite, not the rewritten tree, which must not outlive the interceptor.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_SameQueryable_CacheKey_QueryStageRunsOnce(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values]                                            bool   viaDataContext,
			[Values]                                            bool   disableQueryCache)
		{
			Query.ClearCaches();

			using IDataContext db = viaDataContext
				? CreateFilteredDataContext(context, disableQueryCache)
				: GetDataConnection(context, o => o
					.UseMappingSchema(_personFilterSchema)
					.UseDisableQueryCache(disableQueryCache));

			var limit2 = new KeyedQueryStageLimitInterceptor(2);
			var limit3 = new KeyedQueryStageLimitInterceptor(3);

			// FromSql: a query that an IQueryable keeps between executions, which the filtered table is not
			var query  = db.FromSql<Person>("SELECT * FROM Person");

			db.AddInterceptor(limit2);

			for (var i = 0; i < 3; i++)
			{
				ShouldReturnIds(query, ExpectedIds(2), $"limit 2, enumeration {i}");
			}

			limit2.QueryCalls.ShouldBe(1, "query stage calls");

			db.RemoveInterceptor(limit2);
			db.AddInterceptor(limit3);

			ShouldReturnIds(query, ExpectedIds(3), "another key");

			db.RemoveInterceptor(limit3);

			ShouldReturnIds(query, ExpectedIds(), "removed");
		}

		// An interceptor added inside a UseOptions / UseMappingSchema scope stays registered after it, so the
		// configuration ID saved at the scope start must not come back: it would find the query built before the
		// interceptor, whose query filter has nothing to check again.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void QueryExpression_AddInterceptorInsideScope(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   mappingSchemaScope,
			[Values]                                           bool   viaDataContext)
		{
			if (viaDataContext && context.IsRemote())
				Assert.Inconclusive("A remote context has no DataContext arm");

			Query.ClearCaches();

			IDataContext Create() => viaDataContext
				? CreateFilteredDataContext(context)
				: GetDataContext(context, o => o.UseMappingSchema(_personFilterSchema));

			// the same type and options: the configuration ID differs only by the interceptor
			using var db    = Create();
			using var plain = Create();

			ShouldReturnIds(db, ExpectedIds(), "before");

			// no query inside the scope: the ID it saved is then the last one computed
			using (mappingSchemaScope ? db.UseMappingSchema(new MappingSchema()) : db.UseOptions(o => o.UseCommandTimeout(123)))
			{
				db.AddInterceptor(new PersonLimitInterceptor(ExpressionKind.QueryFilter, 2));
			}

			ShouldReturnIds(db,    ExpectedIds(2), "after");
			ShouldReturnIds(plain, ExpectedIds(),  "plain");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void EntityBinding_AddInterceptor_SeparatesCachedQueries([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool interceptedFirst)
		{
			Query.ClearCaches();

			using var intercepted = GetDataContext(context);
			using var plain       = GetDataContext(context);

			intercepted.AddInterceptor(new FirstNameBindingInterceptor());

			string[] interceptedNames, plainNames;

			if (interceptedFirst)
			{
				interceptedNames = PersonFirstNames(intercepted);
				plainNames       = PersonFirstNames(plain);
			}
			else
			{
				plainNames       = PersonFirstNames(plain);
				interceptedNames = PersonFirstNames(intercepted);
			}

			interceptedNames.ShouldAllBe(name => name == FirstNameBindingInterceptor.FirstName);
			plainNames.ShouldBe(ExpectedFirstNames());
		}

		// An entity-binding interceptor added after the configuration ID is computed, and removed again: its rewrites are
		// not checked when a query is reused, so the configuration ID alone keeps the queries apart.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6303"), QueryCacheTest]
		public void EntityBinding_AddAndRemoveInterceptor(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context,
			[Values]                                           bool   viaDataContext)
		{
			if (viaDataContext && context.IsRemote())
				Assert.Inconclusive("A remote context has no DataContext arm");

			Query.ClearCaches();

			using IDataContext db = viaDataContext
				? new DataContext(context)
				: GetDataContext(context);

			var interceptor = new FirstNameBindingInterceptor();

			PersonFirstNames(db)
				.ShouldBe(ExpectedFirstNames(), "before");

			db.AddInterceptor(interceptor);

			PersonFirstNames(db)
				.ShouldAllBe(name => name == FirstNameBindingInterceptor.FirstName, "added");

			db.RemoveInterceptor(interceptor);

			PersonFirstNames(db)
				.ShouldBe(ExpectedFirstNames(), "removed");
		}

		static string[] PersonFirstNames(IDataContext db)
		{
			return db
				.GetTable<Person>()
				.AsEnumerable()
				.OrderBy(p => p.ID)
				.Select(p => p.FirstName)
				.ToArray();
		}

		string[] ExpectedFirstNames()
		{
			return Person
				.OrderBy(p => p.ID)
				.Select(p => p.FirstName)
				.ToArray();
		}

		#region IExceptionInterceptor
		[Test]
		public void NonQueryExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.AddInterceptor(new TestExceptionInterceptor());

			Assert.That(() =>
				db.GetTable<InterceptorsTestsTable>()
					.Insert(() => new InterceptorsTestsTable()),
				Throws.TypeOf<TestException>());
		}

		[Test]
		public void ScalarExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.AddInterceptor(new TestExceptionInterceptor());

			Assert.That(() =>
				db.GetTable<InterceptorsTestsTable>().Count(),
				Throws.TypeOf<TestException>());
		}

		[Test]
		public void ReaderExceptionIntercepted([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.AddInterceptor(new TestExceptionInterceptor());

			Assert.That(() =>
				db.GetTable<InterceptorsTestsTable>().ToList(),
				Throws.TypeOf<TestException>());
		}

		// The combined eager-loading engine executes a multi-statement group via ADO.NET DbBatch on batch-capable providers
		// (net8.0+, CanUseDbBatch). A DbBatch is not a DbCommand, so no command interceptor runs on that path - but the
		// exception interceptor MUST still fire (RunBatchReader). The tables here are intentionally never created, so the
		// batch execution throws; on net462 the same eager load runs via the semicolon-concat fallback, which also intercepts.
		// UseCombinedCommands is forced on because it is off by default in 6.x - without it the batch templates are never
		// rendered, the eager load runs sequentially, and both tests pass while testing the plain ExecuteReader path.
		[Test]
		public void EagerLoadExceptionIntercepted([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var commands = new List<string>();

			using var db = GetDataConnection(context, o => o
				.UseCombinedCommands(true)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						commands.Add(info.SqlText);
				}));

			db.AddInterceptor(new TestExceptionInterceptor());

			Action act = () => db.GetTable<EagerExceptionParent>()
				.LoadWith(p => p.Children)
				.ToList();

			act.ShouldThrow<TestException>();

			AssertCombinedEagerExecution(context, db, commands);
		}

		[Test]
		public async Task EagerLoadExceptionInterceptedAsync([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var commands = new List<string>();

			using var db = GetDataConnection(context, o => o
				.UseCombinedCommands(true)
				.UseTracing(TraceLevel.Info, info =>
				{
					if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
						commands.Add(info.SqlText);
				}));

			db.AddInterceptor(new TestExceptionInterceptor());

			Func<Task> act = () => db.GetTable<EagerExceptionParent>()
				.LoadWith(p => p.Children)
				.ToListAsync();

			await act.ShouldThrowAsync<TestException>();

			AssertCombinedEagerExecution(context, db, commands);
		}

		// The sequential path fails on its first query the same way, so the throw alone does not show that the intercepted
		// execution carried both statements.
		static void AssertCombinedEagerExecution(string context, DataConnection db, List<string> commands)
		{
#if SUPPORTS_DBBATCH
			var batch = !context.IsAnyOf(TestProvName.AllSqlServerSequentialAccess)
				&& db.OpenDbConnection().CanCreateBatch;
#else
			var batch = false;
#endif

			var text = commands.ShouldHaveSingleItem();

			if (batch)
				text.ShouldContain("-- Command 2");
			else
				text
					.Split('\n')
					.ShouldContain(line => line.Trim() == ";");
		}
		#endregion

		#region Eager read-consistency transaction
		// The eager loader opens a read-consistency transaction (at SqlProviderFlags.DefaultMultiQueryIsolationLevel) whenever a
		// query has preambles, so the main query and its child collections see one snapshot. The decision: keep it always (never
		// skip for a single round-trip) and never double it when a transaction is already active. These lock that in.
		[Test]
		public void EagerLoadOpensReadConsistencyTransaction([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var beginCount  = 0;
			var interceptor = new ReaderTransactionInterceptor();

			using var db = GetDataContext(context, o => o.UseTracing(ti =>
			{
				if (ti.Operation == TraceOperation.BeginTransaction)
					beginCount++;
			}));

			db.AddInterceptor(interceptor);

			using var parents  = db.CreateLocalTable<Pr5EagerParent>();
			using var children = db.CreateLocalTable<Pr5EagerChild>();

			db.Insert(new Pr5EagerParent { Id = 1 });
			db.Insert(new Pr5EagerChild  { Id = 1, ParentId = 1 });

			beginCount                       = 0;
			interceptor.ReaderHadTransaction = null;

			var result = db.GetTable<Pr5EagerParent>()
				.LoadWith(p => p.Children)
				.ToList();

			result                          .Count.ShouldBe(1);
			result[0].Children              .Count.ShouldBe(1);
			beginCount                      .ShouldBeGreaterThanOrEqualTo(1, "eager load should open a read-consistency transaction");
			interceptor.ReaderHadTransaction.ShouldBe(true, "eager load must execute its read inside the read-consistency transaction");
		}

		[Test]
		public void EagerLoadReusesActiveTransaction([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var beginCount = 0;

			using var db = GetDataContext(context, o => o.UseTracing(ti =>
			{
				if (ti.Operation == TraceOperation.BeginTransaction)
					beginCount++;
			}));

			var dc = (DataConnection)db;

			using var parents  = db.CreateLocalTable<Pr5EagerParent>();
			using var children = db.CreateLocalTable<Pr5EagerChild>();

			db.Insert(new Pr5EagerParent { Id = 1 });
			db.Insert(new Pr5EagerChild  { Id = 1, ParentId = 1 });

			using var tx = dc.BeginTransaction();

			beginCount = 0;

			var result = db.GetTable<Pr5EagerParent>()
				.LoadWith(p => p.Children)
				.ToList();

			result            .Count.ShouldBe(1);
			result[0].Children.Count.ShouldBe(1);
			beginCount        .ShouldBe(0, "eager load should reuse the active transaction, not open a second one");
		}

		[Test]
		public void EagerLoadConsumesNextQueryHints([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var parents  = db.CreateLocalTable<Pr5EagerParent>();
			using var children = db.CreateLocalTable<Pr5EagerChild>();

			db.Insert(new Pr5EagerParent { Id = 1 });
			db.Insert(new Pr5EagerChild  { Id = 1, ParentId = 1 });

			db.NextQueryHints.Add("-- pr5 next-query hint");

			var result = db.GetTable<Pr5EagerParent>()
				.LoadWith(p => p.Children)
				.ToList();

			result[0].Children.Count.ShouldBe(1);
			// The one-shot hint must be consumed by the eager load, not left behind to leak onto the next query.
			db.NextQueryHints.ShouldBeEmpty("eager load must consume NextQueryHints so it cannot leak to the next query");
		}

		[Test]
		public void EagerLoadCachedRenderBindsPerExecutionParameters([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllMySql)] string context, [Values] bool combinedCommands)
		{
			using var db = GetDataContext(context, o => o.UseCombinedCommands(combinedCommands));

			using var parents  = db.CreateLocalTable<Pr5EagerParent>();
			using var children = db.CreateLocalTable<Pr5EagerChild>();

			db.Insert(new Pr5EagerParent { Id = 1 });
			db.Insert(new Pr5EagerParent { Id = 2 });
			db.Insert(new Pr5EagerChild  { Id = 10, ParentId = 1 });
			db.Insert(new Pr5EagerChild  { Id = 20, ParentId = 2 });

			// Same compiled query, different @id each run: the second execution reuses the cached render and must bind its
			// OWN parameter (2), not the first run's cached value (1). A stale bind would return parent 1 / child 10.
			for (var id = 1; id <= 2; id++)
			{
				var result = db.GetTable<Pr5EagerParent>()
					.Where(p => p.Id == id)
					.LoadWith(p => p.Children)
					.ToList();

				result                        .Count.ShouldBe(1);
				result[0].Id                  .ShouldBe(id);
				result[0].Children            .Count.ShouldBe(1);
				result[0].Children[0].ParentId.ShouldBe(id);
			}
		}

		// Captures whether the eager read ran while a transaction was active on the command (see EagerLoadOpensReadConsistencyTransaction).
		sealed class ReaderTransactionInterceptor : CommandInterceptor
		{
			public bool? ReaderHadTransaction;

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				ReaderHadTransaction ??= command.Transaction != null;
				return base.ExecuteReader(eventData, command, commandBehavior, result);
			}

			public override Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
			{
				ReaderHadTransaction ??= command.Transaction != null;
				return base.ExecuteReaderAsync(eventData, command, commandBehavior, result, cancellationToken);
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

		// Never created - an eager LoadWith over these forces the combined batch/concat execution to throw (see
		// EagerLoadExceptionIntercepted).
		[Table("EagerExceptionParent")]
		sealed class EagerExceptionParent
		{
			[Column, PrimaryKey] public int Id;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(EagerExceptionChild.ParentId))]
			public List<EagerExceptionChild> Children = null!;
		}

		[Table("EagerExceptionChild")]
		sealed class EagerExceptionChild
		{
			[Column, PrimaryKey] public int Id;
			[Column]             public int ParentId;
		}

		// Real tables (CreateLocalTable) for the eager read-consistency transaction tests.
		[Table("Pr5EagerParent")]
		sealed class Pr5EagerParent
		{
			[Column, PrimaryKey] public int Id;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Pr5EagerChild.ParentId))]
			public List<Pr5EagerChild> Children = null!;
		}

		[Table("Pr5EagerChild")]
		sealed class Pr5EagerChild
		{
			[Column, PrimaryKey] public int Id;
			[Column]             public int ParentId;
		}

		private sealed class TestExceptionInterceptor : ExceptionInterceptor
		{
			public override void ProcessException(ExceptionEventData eventData, Exception exception)
			{
				throw new TestException();
			}
		}

		public sealed class TestException() : Exception("Test Exception");

		sealed class CancelCommandInterceptor : CommandInterceptor
		{
			private DbCommand? _lastCommand;

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				_lastCommand = command;
				return base.ExecuteReader(eventData, command, commandBehavior, result);
			}

			public override Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
			{
				cancellationToken.Register(CancelCurrentCommand);
				_lastCommand = command;
				return base.ExecuteReaderAsync(eventData, command, commandBehavior, result, cancellationToken);
			}

			public void CancelCurrentCommand()
			{
				_lastCommand?.Cancel();
			}
		}

		[Test]
		public void CancelCommandInterceptorTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var interceptor = new CancelCommandInterceptor();
			using var db = GetDataContext(context);
			db.AddInterceptor(interceptor);

			var lastId = 0L;
			try
			{
				foreach (var id in db.Query<long>("SELECT * FROM generate_series(1,1000000)"))
				{
					lastId = id;
					if (id == 10)
					{
						interceptor.CancelCurrentCommand();
					}
				}

				Assert.Fail("Command wasn't cancelled");
			}
			catch (OperationCanceledException)
			{
				Assert.That(lastId, Is.LessThan(1_000_000));
			}
		}

		[Test]
		public async Task CancelCommandInterceptorAsyncTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var interceptor = new CancelCommandInterceptor();
			using var db = GetDataContext(context);
			db.AddInterceptor(interceptor);

			var lastId = 0L;
			try
			{
				await foreach (var id in db.QueryToAsyncEnumerable<long>("SELECT * FROM generate_series(1,1000000)"))
				{
					lastId = id;
					if (id == 10)
					{
						interceptor.CancelCurrentCommand();
					}
				}

				Assert.Fail("Command wasn't cancelled");
			}
			catch (OperationCanceledException)
			{
				Assert.That(lastId, Is.LessThan(1_000_000));
			}
		}

		[Test]
		public async Task CancelCommandInterceptorAsyncTest_UsingToken([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var interceptor = new CancelCommandInterceptor();
			using var db = GetDataContext(context);
			db.AddInterceptor(interceptor);

			using var cts = new CancellationTokenSource();

			var lastId = 0L;
			try
			{
				await foreach (var id in db.QueryToAsyncEnumerable<long>("SELECT * FROM generate_series(1,1000000)")
					.WithCancellation(cts.Token))
				{
					lastId = id;
					if (id == 10)
					{
						cts.Cancel();
					}
				}

				Assert.Fail("Command wasn't cancelled");
			}
			catch (OperationCanceledException)
			{
				Assert.That(lastId, Is.LessThan(1_000_000));
			}
		}
	}
}
