using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using LinqToDB.Remote;
using LinqToDB.Remote.SignalR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Shouldly;

#if NETFRAMEWORK
using Microsoft.AspNetCore;
#else
using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Hosting;
#endif

namespace Tests.Remote
{
	// Behaviour of the Signal/R transport itself, against hubs hosted by each test. Runs on both Signal/R stacks:
	// net462 is the legacy client + legacy server pair, net8+ the current one.
	[TestFixture]
	public sealed class SignalRTransportTests : TestBase
	{
		const string HubPath       = "/hub/linq2db";
		const string Configuration = "SignalRTransportTests";

		static readonly TimeSpan Prompt = TimeSpan.FromSeconds(2);

		#region Plain calls

		[Test]
		public async Task AllOperationsRoundTrip()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			await using (var db = CreateContext(hubConnection))
			{
				(await db.InsertAsync(new Item { Id = 1, Value = "one" })).ShouldBe(1);                     // ExecuteNonQuery
				(await db.GetTable<Item>().CountAsync()).ShouldBe(1);                                         // ExecuteScalar
				(await db.GetTable<Item>().ToListAsync()).Single().Value.ShouldBe("one");                     // ExecuteReader

				db.BeginBatch();
				await db.InsertAsync(new Item { Id = 2, Value = "two" });
				await db.InsertAsync(new Item { Id = 3, Value = "three" });
				await db.CommitBatchAsync();                                                                  // ExecuteBatch

				(await db.GetTable<Item>().CountAsync()).ShouldBe(3);
				(await db.GetTable<Item>().Where(i => i.Id == -1).Select(i => i.Value).FirstOrDefaultAsync()).ShouldBeNull();
			}
		}

		// The DI-free way to share one connection between contexts: disposing one context leaves the connection
		// to the others.
		[Test]
		public async Task ContextsShareACallerOwnedConnection()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			{
				await using (var first = CreateContext(hubConnection))
					await first.InsertAsync(new Item { Id = 1, Value = "one" });

				await using var second = CreateContext(hubConnection);

				(await second.GetTable<Item>().CountAsync()).ShouldBe(1);
			}
		}

		#endregion

		#region Cancellation

		[Test]
		public async Task ClientCancellationReachesTheServer()
		{
			var service = new ScriptedLinqService();
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var client = (ILinqService)new SignalRLinqServiceClient(hubConnection);

			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

			var error = await CatchAsync(() => client.ExecuteReaderAsync(Configuration, "query", cts.Token));
			error.ShouldBeAssignableTo<OperationCanceledException>();

			(await service.WaitCancelledAsync(Prompt)).ShouldBeTrue("the server-side token did not fire");

			// The cancelled call released the connection's only call slot: the next call runs at once.
			service.Behavior = static (_, _) => Task.FromResult("next");

			var sw = Stopwatch.StartNew();
			(await client.ExecuteReaderAsync(Configuration, "query")).ShouldBe("next");
			sw.Elapsed.ShouldBeLessThan(Prompt);
		}

		[Test]
		public async Task DisconnectCancelsTheServerCall()
		{
			var service = new ScriptedLinqService();
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var hubConnection = await host.ConnectAsync();
			var client        = (ILinqService)new SignalRLinqServiceClient(hubConnection);

			var call = client.ExecuteNonQueryAsync(Configuration, "query");

			(await service.WaitStartedAsync(Prompt)).ShouldBeTrue("the call did not reach the server");

			await hubConnection.DisposeAsync();

			(await service.WaitCancelledAsync(Prompt)).ShouldBeTrue("the server-side token did not fire on disconnect");

			// No replay: the call fails, and the server ran it once.
			(await CatchAsync(() => call)).ShouldNotBeNull();
			service.Calls.ShouldBe(1);
		}

		// Signal/R disposes the hub's service scope when the stream ends; the stream must not end before the
		// operation has finished with the scoped service, even when the operation ignores its token for a while.
		[Test]
		public async Task CancelledOperationFinishesBeforeItsScopeIsDisposed()
		{
			ScopedLinqService.Reset();

			using var host = TestHost.Start<LinqToDBHub<ScopedContext>>(services => services.AddScoped<ILinqService<ScopedContext>, ScopedLinqService>());

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var client = (ILinqService)new SignalRLinqServiceClient(hubConnection);

			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

			(await CatchAsync(() => client.ExecuteReaderAsync(Configuration, "query", cts.Token))).ShouldBeAssignableTo<OperationCanceledException>();

			(await WaitAsync(ScopedLinqService.Finished.Task, Prompt + Prompt)).ShouldBeTrue("the operation did not finish");
			ScopedLinqService.UsedAfterDispose.ShouldBeFalse("the scope was disposed while the operation was still running");
		}

		// The rows of a result are read synchronously on the server; a cancelled call stops at the next row and does
		// not read the result to the end.
		[Test]
		public async Task CancellationStopsReadingRows()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			await using (var db = CreateContext(hubConnection))
			{
				using var cts = new CancellationTokenSource();

				// Every row the server reads calls row_probe: cancel once rows are flowing, then slow the rest down
				// so reading all of them would take far longer than the test allows.
				database.RowProbe = row =>
				{
					if (row == 10)
						cts.Cancel();
					else if (row > 10)
						Thread.Sleep(1);
				};

				var sw    = Stopwatch.StartNew();
				var error = await CatchAsync(() => db.GetTable<ProbeRow>().ToListAsync(cts.Token));

				error.ShouldBeAssignableTo<OperationCanceledException>();

				// Wait for the server to give up on the reader.
				await database.WaitIdleAsync(Prompt + Prompt);

				database.RowsRead.ShouldBeLessThan(SqliteDatabase.ProbeRowCount / 10, "the server kept reading rows after the call was cancelled");
				sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
			}
		}

		// The core loop itself, without a transport: the token passed to LinqService stops the read at the next row.
		[Test]
		public async Task LinqServiceStopsReadingRowsOnCancellation()
		{
			using var database = new SqliteDatabase();
			using var cts      = new CancellationTokenSource();

			database.RowProbe = row =>
			{
				if (row == 3)
					cts.Cancel();
			};

			await using var db = new InProcessRemoteContext(database);

			var error = await CatchAsync(() => db.GetTable<ProbeRow>().ToListAsync(cts.Token));

			error.ShouldBeAssignableTo<OperationCanceledException>();
			database.RowsRead.ShouldBeLessThanOrEqualTo(4);
		}

		#endregion

		#region Concurrency

		[Test]
		public async Task CallsOnOneConnectionRunOneAtATimeByDefault()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(150) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			await RunConcurrentCallsAsync(host, connections: 1, callsPerConnection: 4);

			service.MaxConcurrency.ShouldBe(1);
		}

		[Test]
		public async Task MaxConcurrentCallsPerConnectionIsHonoured()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<ScriptedHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o => o.MaxConcurrentCallsPerConnection = 3);
			});

			await RunConcurrentCallsAsync(host, connections: 1, callsPerConnection: 8);

			service.MaxConcurrency.ShouldBe(3);
		}

#if !NETFRAMEWORK
		// Without the linq2db option the hub keeps Signal/R's own per-client setting.
		[Test]
		public async Task DefaultFollowsMaximumParallelInvocationsPerClient()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service), hub => hub.MaximumParallelInvocationsPerClient = 2);

			await RunConcurrentCallsAsync(host, connections: 1, callsPerConnection: 6);

			service.MaxConcurrency.ShouldBe(2);
		}
#endif

		[Test]
		public async Task MaxConcurrentCallsLimitsAllConnections()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(150) };
			using var host = TestHost.Start<ScriptedHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o =>
				{
					o.MaxConcurrentCallsPerConnection = 4;
					o.MaxConcurrentCalls              = 2;
				});
			});

			await RunConcurrentCallsAsync(host, connections: 3, callsPerConnection: 3);

			service.MaxConcurrency.ShouldBe(2);
		}

		// A call waiting for a slot is cancelled while it waits: it never runs, and the slot holder is unaffected.
		[Test]
		public async Task QueuedCallCanBeCancelled()
		{
			var service = new ScriptedLinqService();
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var client  = (ILinqService)new SignalRLinqServiceClient(hubConnection);
			using var holder = new CancellationTokenSource();

			var running = client.ExecuteReaderAsync(Configuration, "first", holder.Token);
			(await service.WaitStartedAsync(Prompt)).ShouldBeTrue();

			using (var queued = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)))
				(await CatchAsync(() => client.ExecuteReaderAsync(Configuration, "second", queued.Token))).ShouldBeAssignableTo<OperationCanceledException>();

			service.Calls.ShouldBe(1);

			holder.Cancel();
			(await CatchAsync(() => running)).ShouldBeAssignableTo<OperationCanceledException>();
		}

		// A permit released after its connection is gone must not fail (the per-connection semaphore is never
		// disposed), and the hub keeps serving new connections.
		[Test]
		public async Task CallFinishingAfterDisconnectReleasesItsSlot()
		{
			var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var service = new ScriptedLinqService
			{
				Behavior = async (_, _) =>
				{
					// Ignores its token on purpose: it finishes only after the client is gone.
					await release.Task;
					return "late";
				},
			};

			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var first = await host.ConnectAsync();
			var call  = ((ILinqService)new SignalRLinqServiceClient(first)).ExecuteReaderAsync(Configuration, "query");

			(await service.WaitStartedAsync(Prompt)).ShouldBeTrue();

			await first.DisposeAsync();
			_ = await CatchAsync(() => call);

			release.SetResult(true);
			(await service.WaitFinishedAsync(Prompt)).ShouldBeTrue();
			service.Failures.ShouldBeEmpty();

			service.Behavior = static (_, _) => Task.FromResult("next");

			var second = await host.ConnectAsync();
			await using var owner = Own(second);

			(await ((ILinqService)new SignalRLinqServiceClient(second)).ExecuteReaderAsync(Configuration, "query")).ShouldBe("next");
		}

		#endregion

		#region Message size

		[Test]
		public async Task LargeResultIsSentInChunks()
		{
			// A surrogate pair right at the chunk boundary: splitting it would corrupt both halves.
			var result = new string('x', 32 * 1024 - 1) + "\U0001F600" + new string('y', 100_000);

			var service = new ScriptedLinqService { Behavior = (_, _) => Task.FromResult(result) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			(await ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteReaderAsync(Configuration, "query")).ShouldBe(result);

			var chunks = new List<string>();
			var reader = await hubConnection.StreamAsChannelAsync<string>("ExecuteReaderStream", Configuration, "query");

			while (await reader.WaitToReadAsync())
				while (reader.TryRead(out var chunk))
					chunks.Add(chunk);

			chunks.Count.ShouldBe(5);
			chunks.ShouldAllBe(c => c.Length <= 32 * 1024);
			chunks[0].Length.ShouldBe(32 * 1024 - 1);
			string.Concat(chunks).ShouldBe(result);
		}

		[Test]
		public async Task LargeQueryResultRoundTrips()
		{
			using var database = new SqliteDatabase();
#if NETFRAMEWORK
			// The legacy server has no message size limit.
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));
#else
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database), hub => hub.MaximumReceiveMessageSize = null);
#endif

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			await using (var db = CreateContext(hubConnection))
			{
				var value = string.Concat(Enumerable.Range(0, 20_000).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)));

				await db.InsertAsync(new Item { Id = 1, Value = value });

				(await db.GetTable<Item>().SingleAsync()).Value.ShouldBe(value);
			}
		}

#if !NETFRAMEWORK
		// Signal/R closes the whole connection on a message over the hub's limit. The client knows the limit and
		// refuses such a request instead, so only that call fails.
		[Test]
		public async Task OversizedRequestFailsBeforeSending()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database), perHub: hub => hub.MaximumReceiveMessageSize = 4096);

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			await using (var db = CreateContext(hubConnection))
			{
				var error = await CatchAsync(() => db.InsertAsync(new Item { Id = 1, Value = new string('v', 5000) }));

				error.ShouldBeOfType<LinqToDBException>().Message.ShouldContain("4096");

				hubConnection.State.ShouldBe(HubConnectionState.Connected);
				(await db.GetTable<Item>().CountAsync()).ShouldBe(0);
			}
		}

		// The pre-flight estimate must never be below the real frame, or an oversized request slips through and
		// the connection is closed. Escaped characters are the worst case for the JSON protocol.
		[Test]
		public async Task RequestSizeEstimateCoversTheJsonFrame([Values("\"", "\\", "<", "é", "\U0001F600", "\u0001", "a")] string unit)
		{
			var queryData = string.Concat(Enumerable.Repeat(unit, 20_000 / unit.Length));
			var frame     = new System.Buffers.ArrayBufferWriter<byte>();

			new JsonHubProtocol().WriteMessage(new StreamInvocationMessage("1", "ExecuteNonQueryStream", [Configuration, queryData]), frame);

			var service = new ScriptedLinqService();
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service), perHub: hub => hub.MaximumReceiveMessageSize = frame.WrittenCount - 2);

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var error = await CatchAsync(() => ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteNonQueryAsync(Configuration, queryData));

			error.ShouldBeOfType<LinqToDBException>();
			service.Calls.ShouldBe(0);
			hubConnection.State.ShouldBe(HubConnectionState.Connected);
		}
#endif

		#endregion

		#region Errors

		[Test]
		public async Task ServerErrorTextIsHiddenByDefault()
		{
			var service = new ScriptedLinqService { Behavior = static (_, _) => throw new InvalidOperationException("secret detail") };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var error = await CallAsync(host);

			error.ShouldBeOfType<HubException>().Message.ShouldNotContain("secret detail");
		}

		[Test]
		public async Task TransferInternalExceptionToClient()
		{
			var service = new ScriptedLinqService { Behavior = static (_, _) => throw new InvalidOperationException("secret detail") };
			using var host = TestHost.Start<ScriptedHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o => o.TransferInternalExceptionToClient = true);
			});

			var error = await CallAsync(host);

#if NETFRAMEWORK
			// The legacy server drops the text of every error unless HubOptions.EnableDetailedErrors is set.
			error.ShouldBeOfType<HubException>().Message.ShouldNotContain("secret detail");
#else
			error.ShouldBeOfType<HubException>().Message.ShouldContain("secret detail");
#endif
		}

		static async Task<Exception?> CallAsync(TestHost host)
		{
			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			return await CatchAsync(() => ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteReaderAsync(Configuration, "query"));
		}

		#endregion

		#region DI helper

		[Test]
		public async Task DIConnectionStartsOnFirstQueryAndAfterAFailedStart()
		{
			var port = TestHost.GetFreePort();

			await using var provider = BuildClientServices(port);

			// Nothing listens yet: the first query fails to connect.
			await using (var scope = provider.CreateAsyncScope())
				(await CatchAsync(() => scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync())).ShouldNotBeNull();

			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database), port: port);

			await using (var scope = provider.CreateAsyncScope())
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);

			// Idempotent.
			await provider.InitSignalRAsync<ClientContext>();
			await provider.InitSignalRAsync<ClientContext>();
		}

		[Test]
		public async Task DIConnectionRestartsAfterTheServerWasRestarted()
		{
			var port = TestHost.GetFreePort();

			using var database = new SqliteDatabase();

			await using var provider = BuildClientServices(port);

			// A query sent before the client notices the loss fails (it is never re-sent), so the test waits for that.
			var lost          = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var hubConnection = provider.GetRequiredService<LinqToDBSignalRConnection>().HubConnection;

			hubConnection.Closed += _ =>
			{
				lost.TrySetResult(true);
				return Task.CompletedTask;
			};
#if !NETFRAMEWORK
			hubConnection.Reconnecting += _ =>
			{
				lost.TrySetResult(true);
				return Task.CompletedTask;
			};
#endif

			using (TestHost.Start<SqliteHub>(services => services.AddSingleton(database), port: port))
			{
				await using var scope = provider.CreateAsyncScope();
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);
			}

			(await WaitAsync(lost.Task, Prompt + Prompt)).ShouldBeTrue("the client did not notice the server going away");

			using var restarted = TestHost.Start<SqliteHub>(services => services.AddSingleton(database), port: port);

			// The next query brings the connection back: on .NET 8+ it waits for the automatic reconnect, or starts
			// the connection again once the reconnect attempts are exhausted; on the legacy client it always starts it again.
			await using (var scope = provider.CreateAsyncScope())
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);
		}

		[Test]
		public async Task DIConcurrentFirstQueriesShareOneStart()
		{
			ConnectionCountingHub.Reset();

			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<ConnectionCountingHub>(services => services.AddSingleton(database));

			await using var provider = BuildClientServices(host.Port);

			await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
			{
				await using var scope = provider.CreateAsyncScope();
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);
			}));

			ConnectionCountingHub.Connections.ShouldBe(1);
		}

		[Test]
		public async Task DIContainerDisposesTheConnection()
		{
			var provider   = BuildClientServices(TestHost.GetFreePort());
			var connection = provider.GetRequiredService<LinqToDBSignalRConnection>();

			await provider.DisposeAsync();

			(await CatchAsync(() => connection.EnsureConnectedAsync())).ShouldBeOfType<ObjectDisposedException>();
		}

#if !NETFRAMEWORK
		// Access token and authorization: the DI helper's options reach the HTTP connection.
		[Test]
		public async Task DIConnectionSendsTheAccessToken([Values] bool withToken)
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<AuthorizedHub>(services => services.AddSingleton(database), authentication: true);

			await using var provider = BuildClientServices(host.Port, options =>
			{
				if (withToken)
					options.ConfigureHttpConnection = http => http.AccessTokenProvider = static () => Task.FromResult<string?>(TokenAuthenticationHandler.Token);
			});

			await using var scope = provider.CreateAsyncScope();

			var count = scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync();

			if (withToken)
				(await count).ShouldBe(0);
			else
				(await CatchAsync(() => count)).ShouldNotBeNull();
		}
#endif

		static ServiceProvider BuildClientServices(int port, Action<LinqToDBSignalRClientOptions>? configure = null)
		{
			var services = new ServiceCollection();

			services.AddLinqToDBSignalRDataContext(
				new Uri($"http://localhost:{port}{HubPath}"),
				client => new ClientContext(client),
				options =>
				{
					options.ConnectTimeout = TimeSpan.FromSeconds(10);
					configure?.Invoke(options);
				});

			return services.BuildServiceProvider();
		}

		sealed class ClientContext(SignalRLinqServiceClient client) : SignalRDataContext(client, o => o.UseConfiguration(Configuration));

		#endregion

		#region PostgreSQL

		// The audit scenario: a long query cancelled on the client is cancelled in the database too, and the
		// connection is free for the next query at once.
		[Test]
		public async Task CancellationReachesTheDatabase([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var host = TestHost.Start<ConfigurationHub>(services => services.AddSingleton(new ConfigurationHub.Target(context)));

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			await using (var db = new SignalRDataContext(hubConnection, o => o.UseConfiguration(Configuration + ".PostgreSQL")))
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

				var sw    = Stopwatch.StartNew();
				var error = await CatchAsync(() => db.FromSql<SleepRow>("SELECT 1 AS \"Value\" FROM pg_sleep(5)").ToListAsync(cts.Token));

				error.ShouldBeAssignableTo<OperationCanceledException>();
				sw.Elapsed.ShouldBeLessThan(Prompt);

				using var admin = GetDataConnection(context);

				var deadline = Stopwatch.StartNew();
				int sleeping;

				do
				{
					sleeping = admin.Execute<int>("SELECT count(*)::int FROM pg_stat_activity WHERE state = 'active' AND query LIKE '%pg_sleep(5)%' AND pid <> pg_backend_pid()");

					if (sleeping == 0)
						break;

					await Task.Delay(50);
				}
				while (deadline.Elapsed < Prompt);

				sleeping.ShouldBe(0, "the query kept running in the database");

				sw.Restart();
				(await db.FromSql<SleepRow>("SELECT 2 AS \"Value\"").ToListAsync()).Single().Value.ShouldBe(2);
				sw.Elapsed.ShouldBeLessThan(Prompt);
			}
		}

		sealed class SleepRow
		{
			[Column] public int Value { get; set; }
		}

		sealed class ConfigurationHub(ConfigurationHub.Target target, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			public sealed record Target(string Configuration);

			protected override ILinqService CreateLinqService()
			{
				return new ConfigurationLinqService(target.Configuration);
			}

			sealed class ConfigurationLinqService(string target) : LinqService
			{
				public override DataConnection CreateDataContext(string? configuration)
				{
					return new DataConnection(new DataOptions().UseConfiguration(target));
				}
			}
		}

		#endregion

		#region Helpers

		static SignalRDataContext CreateContext(HubConnection hubConnection)
		{
			return new SignalRDataContext(hubConnection, o => o.UseConfiguration(Configuration));
		}

		static IAsyncDisposable Own(HubConnection hubConnection)
		{
			return new HubConnectionOwner(hubConnection);
		}

		// HubConnection is IAsyncDisposable on .NET 8+ only.
		sealed class HubConnectionOwner(HubConnection hubConnection) : IAsyncDisposable
		{
			public async ValueTask DisposeAsync()
			{
				await hubConnection.DisposeAsync();
			}
		}

		static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
		{
			return await Task.WhenAny(task, Task.Delay(timeout)) == task;
		}

		static async Task<Exception?> CatchAsync(Func<Task> action)
		{
			try
			{
				await action();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		static async Task RunConcurrentCallsAsync(TestHost host, int connections, int callsPerConnection)
		{
			var hubConnections = new List<HubConnection>();

			try
			{
				for (var i = 0; i < connections; i++)
					hubConnections.Add(await host.ConnectAsync());

				await Task.WhenAll(hubConnections.SelectMany(c => Enumerable.Range(0, callsPerConnection).Select(n =>
					((ILinqService)new SignalRLinqServiceClient(c)).ExecuteReaderAsync(Configuration, "query"))));
			}
			finally
			{
				foreach (var c in hubConnections)
					await c.DisposeAsync();
			}
		}

		[Table("SignalRProbeRows")]
		sealed class ProbeRow
		{
			[Column] public long Id { get; set; }
		}

		[Table("SignalRItems")]
		sealed class Item
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column]     public string? Value { get; set; }
		}

		// A file database: linq2db opens a connection per query, and an unshared in-memory database would die with
		// the connection that created it.
		sealed class SqliteDatabase : IDisposable
		{
			readonly string _path = Path.Combine(Path.GetTempPath(), $"linq2db-signalr-{Guid.NewGuid():N}.sqlite");
			int             _rowsRead;

			public SqliteDatabase()
			{
				using var db = Open();
				db.CreateTable<Item>();

				// Every row of the view calls row_probe while the server reads it.
				db.Execute($"CREATE VIEW SignalRProbeRows AS WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < {ProbeRowCount}) SELECT row_probe(i) AS Id FROM n");
			}

			public const int ProbeRowCount = 100_000;

			public Action<long>? RowProbe { get; set; }
			public int           RowsRead => Volatile.Read(ref _rowsRead);

			public DataConnection Open()
			{
				var db = new DataConnection(new DataOptions().UseSQLite($"Data Source={_path}", SQLiteProvider.Microsoft));

				((SqliteConnection)db.OpenDbConnection()).CreateFunction("row_probe", (long row) =>
				{
					Interlocked.Increment(ref _rowsRead);
					RowProbe?.Invoke(row);
					return row;
				});

				return db;
			}

			// Waits until the server stops reading rows.
			public async Task WaitIdleAsync(TimeSpan timeout)
			{
				var sw   = Stopwatch.StartNew();
				var last = -1;

				while (sw.Elapsed < timeout)
				{
					var current = RowsRead;

					if (current == last)
						return;

					last = current;
					await Task.Delay(300);
				}
			}

			public void Dispose()
			{
				try
				{
					using (var pooled = new SqliteConnection($"Data Source={_path}"))
						SqliteConnection.ClearPool(pooled);

					File.Delete(_path);
				}
				catch
				{
					// best-effort: a temp file left behind is not worth failing the test over
				}
			}
		}

		sealed class SqliteLinqService(SqliteDatabase database) : LinqService
		{
			public override DataConnection CreateDataContext(string? configuration)
			{
				return database.Open();
			}
		}

		sealed class SqliteHub(SqliteDatabase database, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			protected override ILinqService CreateLinqService()
			{
				return new SqliteLinqService(database) { AllowUpdates = true };
			}
		}

		sealed class ConnectionCountingHub(SqliteDatabase database, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			static int _connections;

			public static int Connections => Volatile.Read(ref _connections);

			public static void Reset()
			{
				Volatile.Write(ref _connections, 0);
			}

			public override Task OnConnectedAsync()
			{
				Interlocked.Increment(ref _connections);
				return base.OnConnectedAsync();
			}

			protected override ILinqService CreateLinqService()
			{
				return new SqliteLinqService(database);
			}
		}

		// The remote protocol served in-process, without a transport, so the token reaches LinqService unchanged.
		sealed class InProcessRemoteContext(SqliteDatabase database) : RemoteDataContextBase(new DataOptions().UseConfiguration(Configuration + ".InProcess"))
		{
			protected override ILinqService GetClient()
			{
				return new SqliteLinqService(database);
			}

			protected override string ContextIDPrefix => "InProcess";
		}

		sealed class ScriptedHub(ILinqService service, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			protected override ILinqService CreateLinqService()
			{
				return service;
			}
		}

		// Records what the hub does with its calls: how many ran, how many at once, and what their tokens did.
		sealed class ScriptedLinqService : ILinqService
		{
			readonly TaskCompletionSource<bool> _started   = new(TaskCreationOptions.RunContinuationsAsynchronously);
			readonly TaskCompletionSource<bool> _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
			readonly TaskCompletionSource<bool> _finished  = new(TaskCreationOptions.RunContinuationsAsynchronously);

			int _calls;
			int _running;
			int _maxConcurrency;

			// By default a call runs until its token fires.
			public Func<string, CancellationToken, Task<string>> Behavior { get; set; } = static async (_, cancellationToken) =>
			{
				await Task.Delay(Timeout.Infinite, cancellationToken);
				return "never";
			};

			public static Func<string, CancellationToken, Task<string>> Delay(int milliseconds)
			{
				return async (query, cancellationToken) =>
				{
					await Task.Delay(milliseconds, cancellationToken);
					return query;
				};
			}

			public int                MaxConcurrency => Volatile.Read(ref _maxConcurrency);
			public int                Calls          => Volatile.Read(ref _calls);
			public List<Exception>    Failures       { get; } = new();
			public string?            RemoteClientTag { get; set; }

			public async Task<bool> WaitStartedAsync(TimeSpan timeout)   { return await Wait(_started.Task, timeout); }
			public async Task<bool> WaitCancelledAsync(TimeSpan timeout) { return await Wait(_cancelled.Task, timeout); }
			public async Task<bool> WaitFinishedAsync(TimeSpan timeout)  { return await Wait(_finished.Task, timeout); }

			static async Task<bool> Wait(Task task, TimeSpan timeout)
			{
				return await Task.WhenAny(task, Task.Delay(timeout)) == task;
			}

			async Task<string> RunAsync(string query, CancellationToken cancellationToken)
			{
				Interlocked.Increment(ref _calls);

				var running = Interlocked.Increment(ref _running);

				for (var max = MaxConcurrency; running > max; max = MaxConcurrency)
					Interlocked.CompareExchange(ref _maxConcurrency, running, max);

				using var registration = cancellationToken.Register(() => _cancelled.TrySetResult(true));

				_started.TrySetResult(true);

				try
				{
					return await Behavior(query, cancellationToken);
				}
				catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
				{
					lock (Failures)
						Failures.Add(ex);
					throw;
				}
				finally
				{
					Interlocked.Decrement(ref _running);
					_finished.TrySetResult(true);
				}
			}

			public Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException();
			}

			public async Task<int> ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				return (await RunAsync(queryData, cancellationToken)).Length;
			}

			public Task<string?> ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				return RunAsync(queryData, cancellationToken)!;
			}

			public Task<string> ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				return RunAsync(queryData, cancellationToken);
			}

			public async Task<int> ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				return (await RunAsync(queryData, cancellationToken)).Length;
			}
		}

		sealed class ScopedContext : DataConnection;

		// A scoped service that notices being used after its scope was disposed.
		sealed class ScopedLinqService : ILinqService<ScopedContext>, IDisposable
		{
			static volatile bool _usedAfterDispose;
			volatile        bool _disposed;

			public static TaskCompletionSource<bool> Finished { get; private set; } = new();
			public static bool                       UsedAfterDispose => _usedAfterDispose;

			public static void Reset()
			{
				_usedAfterDispose = false;
				Finished          = new(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			public string? RemoteClientTag { get; set; }

			public void Dispose()
			{
				_disposed = true;
			}

			public async Task<string> ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				try
				{
					await Task.Delay(Timeout.Infinite, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					// Keeps using the scope for a while after the cancellation, as a slow provider would.
					await Task.Delay(300, CancellationToken.None);

					if (_disposed)
						_usedAfterDispose = true;

					Finished.TrySetResult(true);
					throw;
				}

				return "never";
			}

			public Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException();
			}

			public Task<int> ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException();
			}

			public Task<string?> ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException();
			}

			public Task<int> ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
			{
				throw new NotSupportedException();
			}
		}

#if !NETFRAMEWORK
		[Authorize]
		sealed class AuthorizedHub(SqliteDatabase database, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			protected override ILinqService CreateLinqService()
			{
				return new SqliteLinqService(database);
			}
		}

		sealed class TokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
			: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
		{
			public const string SchemeName = "Test";
			public const string Token  = "test-token";

			protected override Task<AuthenticateResult> HandleAuthenticateAsync()
			{
				var token = Request.Query["access_token"].ToString();

				if (token.Length == 0 && Request.Headers.Authorization.ToString() is { } header && header.StartsWith("Bearer ", StringComparison.Ordinal))
					token = header.Substring("Bearer ".Length);

				if (token != Token)
					return Task.FromResult(AuthenticateResult.NoResult());

				var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], SchemeName);

				return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
			}
		}
#endif

		sealed class TestHost : IDisposable
		{
#if NETFRAMEWORK
			readonly IWebHost _host;
#else
			readonly IHost _host;
#endif

			TestHost(int port,
#if NETFRAMEWORK
				IWebHost host
#else
				IHost host
#endif
				)
			{
				Port  = port;
				_host = host;
			}

			public int    Port   { get; }
			public string HubUrl => $"http://localhost:{Port}{HubPath}";

			public static int GetFreePort()
			{
				var listener = new TcpListener(IPAddress.Loopback, 0);
				listener.Start();

				try
				{
					return ((IPEndPoint)listener.LocalEndpoint).Port;
				}
				finally
				{
					listener.Stop();
				}
			}

			public static TestHost Start<THub>(
				Action<IServiceCollection>? services       = null,
				Action<HubOptions>?         hub            = null,
				int?                        port           = null,
				Action<HubOptions>?         perHub         = null,
				bool                        authentication = false)
				where THub : Hub
			{
				var actualPort = port ?? GetFreePort();
				var url        = $"http://localhost:{actualPort}";

#if NETFRAMEWORK
				var host = WebHost.CreateDefaultBuilder()
					.UseUrls(url)
					.ConfigureLogging(logging => logging.ClearProviders())
					.ConfigureServices(s =>
					{
						s.AddSignalR(o =>
						{
							hub?.Invoke(o);
							perHub?.Invoke(o);
						});

						services?.Invoke(s);
					})
					.Configure(app => app.UseSignalR(routes => routes.MapHub<THub>(HubPath)))
					.Build();
#else
				var host = Host.CreateDefaultBuilder()
					.ConfigureLogging(logging => logging.ClearProviders())
					.ConfigureWebHostDefaults(web => web
						.UseUrls(url)
						.ConfigureServices(s =>
						{
							var signalR = s.AddSignalR(o => hub?.Invoke(o));

							if (perHub != null)
								signalR.AddHubOptions<THub>(perHub);

							if (authentication)
							{
								s.AddAuthentication(TokenAuthenticationHandler.SchemeName)
									.AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(TokenAuthenticationHandler.SchemeName, null);
								s.AddAuthorization();
							}

							services?.Invoke(s);
						})
						.Configure(app =>
						{
							app.UseRouting();

							if (authentication)
							{
								app.UseAuthentication();
								app.UseAuthorization();
							}

							app.UseEndpoints(endpoints => endpoints.MapHub<THub>(HubPath));
						}))
					.Build();
#endif

				host.Start();

				return new TestHost(actualPort, host);
			}

			public async Task<HubConnection> ConnectAsync(string? accessToken = null)
			{
				var hubConnection = new HubConnectionBuilder()
					.WithUrl(HubUrl, http =>
					{
						if (accessToken != null)
							http.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
					})
					.Build();

				await hubConnection.StartAsync();

				return hubConnection;
			}

			public void Dispose()
			{
				_host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
				_host.Dispose();
			}
		}

		#endregion
	}
}
