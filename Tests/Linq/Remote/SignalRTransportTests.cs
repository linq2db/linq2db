using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
using Microsoft.AspNetCore.Hosting.Server.Features;
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
using Microsoft.AspNetCore.Hosting.Server;
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

		// How long a test waits for something it expects to happen: the wait ends as soon as it does, so the limit only
		// has to keep a regression from hanging the run, and is generous for loaded CI agents.
		static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(30);

		// How soon something must happen when the test asserts it is prompt: well above any healthy latency, and well
		// below the hang it guards against (the 10 s pg_sleep of CancellationReachesTheDatabase).
		static readonly TimeSpan Prompt = TimeSpan.FromSeconds(5);

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

			(await service.WaitCancelledAsync(WaitLimit)).ShouldBeTrue("the server-side token did not fire");

			// The cancelled call released the connection's only call slot: the next call runs at once.
			service.Behavior = static (_, _) => Task.FromResult("next");

			var sw = Stopwatch.StartNew();
			(await WithinLimit(client.ExecuteReaderAsync(Configuration, "query"))).ShouldBe("next");
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

			(await service.WaitStartedAsync(WaitLimit)).ShouldBeTrue("the call did not reach the server");

			await hubConnection.DisposeAsync();

			(await service.WaitCancelledAsync(WaitLimit)).ShouldBeTrue("the server-side token did not fire on disconnect");

			// No replay: the call fails, and the server ran it once.
			(await WithinLimit(CatchAsync(() => call))).ShouldNotBeNull();
			service.Calls.ShouldBe(1);
		}

		// A finished call leaves nothing on the caller's token: cancelling a long-lived token afterwards must not send
		// a cancellation for every call it was ever passed to.
		[Test]
		public async Task FinishedCallsLeaveNothingOnTheCallersToken()
		{
			var service = new ScriptedLinqService { Behavior = static (query, _) => Task.FromResult(query) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var log           = new LogEventCounter("SendingCancellation");
			var hubConnection = await host.ConnectAsync(logger: log);
			await using var owner = Own(hubConnection);

			var client = (ILinqService)new SignalRLinqServiceClient(hubConnection);

			using var cts = new CancellationTokenSource();

			for (var i = 0; i < 20; i++)
				(await client.ExecuteReaderAsync(Configuration, "query", cts.Token)).ShouldBe("query");

			cts.Cancel();
			await Task.Delay(500);

			log.Count.ShouldBe(0, "finished calls were cancelled again");
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

			(await WaitAsync(ScopedLinqService.Finished.Task, WaitLimit)).ShouldBeTrue("the operation did not finish");
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

				// Wait for the server to give up on the reader, then check it really stopped: one that kept reading
				// would still be adding rows (at most one per millisecond, so a row-count bound alone cannot tell).
				await database.WaitIdleAsync(WaitLimit);

				var rowsRead = database.RowsRead;
				await Task.Delay(500);

				database.RowsRead.ShouldBe(rowsRead, "the server kept reading rows after the call was cancelled");
				rowsRead.ShouldBeLessThan(SqliteDatabase.ProbeRowCount / 10);
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

		// A stream without items (a null scalar) completes too.
		[Test]
		public async Task EmptyStreamCompletes()
		{
			var readerType = typeof(LinqToDBHub).Assembly.GetType("LinqToDB.Remote.SignalR.OperationChannelReader`1", throwOnError: true)!.MakeGenericType(typeof(string));
			Func<CancellationToken, Task<IReadOnlyList<string>>> operation = static _ => Task.FromResult<IReadOnlyList<string>>([]);

			var reader = (ChannelReader<string>)Activator.CreateInstance(readerType, operation, CancellationToken.None)!;

			(await reader.WaitToReadAsync()).ShouldBeFalse();
			(await WaitAsync(reader.Completion, WaitLimit)).ShouldBeTrue("the stream never completed");
		}

		// Signal/R reads a stream through TryRead / WaitToReadAsync / ReadAsync and never looks at its Completion: a
		// failed operation must not leave a faulted task behind that nobody observes.
		[Test]
		public void FailedStreamLeavesNoUnobservedTask()
		{
			var marker     = Guid.NewGuid().ToString();
			var unobserved = 0;

			void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
			{
				if (e.Exception.Flatten().InnerExceptions.Any(ex => ex.Message == marker))
					Interlocked.Increment(ref unobserved);
			}

			TaskScheduler.UnobservedTaskException += OnUnobserved;

			try
			{
				ReadFailingStream(marker);

				for (var i = 0; i < 3; i++)
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}

				Volatile.Read(ref unobserved).ShouldBe(0);
			}
			finally
			{
				TaskScheduler.UnobservedTaskException -= OnUnobserved;
			}

			// A frame of its own, so nothing in the test keeps the reader alive when it collects.
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
			static void ReadFailingStream(string marker)
			{
				var readerType = typeof(LinqToDBHub).Assembly.GetType("LinqToDB.Remote.SignalR.OperationChannelReader`1", throwOnError: true)!.MakeGenericType(typeof(string));
				Func<CancellationToken, Task<IReadOnlyList<string>>> operation = _ => throw new InvalidOperationException(marker);

				var reader = (ChannelReader<string>)Activator.CreateInstance(readerType, operation, CancellationToken.None)!;

				CatchAsync(async () => await reader.WaitToReadAsync()).GetAwaiter().GetResult().ShouldBeOfType<InvalidOperationException>();
			}
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

		// The global limit holds across all connections also for a hub that is given its options directly: Signal/R
		// creates a hub for every call.
		[Test]
		public async Task MaxConcurrentCallsLimitsAllConnectionsWhenTheHubCreatesItsOptions()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<OwnOptionsHub>(services => services.AddSingleton<ILinqService>(service));

			await RunConcurrentCallsAsync(host, connections: 3, callsPerConnection: 2);

			service.MaxConcurrency.ShouldBe(1);
		}

		// Options registered in the services apply to a hub written before 6.6.0, which does not pass them to its
		// base constructor, on every server.
		[Test]
		public async Task RegisteredOptionsApplyToAHubWithoutAnOptionsConstructor()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<PlainConstructorHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o => o.MaxConcurrentCallsPerConnection = 3);
			});

			await RunConcurrentCallsAsync(host, connections: 1, callsPerConnection: 8);

			service.MaxConcurrency.ShouldBe(3);
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
			(await service.WaitStartedAsync(WaitLimit)).ShouldBeTrue();

			using (var queued = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)))
				(await CatchAsync(() => client.ExecuteReaderAsync(Configuration, "second", queued.Token))).ShouldBeAssignableTo<OperationCanceledException>();

			service.Calls.ShouldBe(1);

			// Once the slot is free, a queued call whose cancellation never reached the server would run.
			service.Behavior = static (query, _) => Task.FromResult(query);

			holder.Cancel();
			(await WithinLimit(CatchAsync(() => running))).ShouldBeAssignableTo<OperationCanceledException>();

			(await WithinLimit(client.ExecuteReaderAsync(Configuration, "third"))).ShouldBe("third");
			service.Calls.ShouldBe(2);
		}

		// A call finishing after its connection is gone releases its permit: with a global limit of one call, the
		// hub serves the next connection only if the late call gave its permit back.
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

			using var host = TestHost.Start<ScriptedHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o => o.MaxConcurrentCalls = 1);
			});

			var first = await host.ConnectAsync();
			var call  = ((ILinqService)new SignalRLinqServiceClient(first)).ExecuteReaderAsync(Configuration, "query");

			(await service.WaitStartedAsync(WaitLimit)).ShouldBeTrue();

			await first.DisposeAsync();
			_ = await WithinLimit(CatchAsync(() => call));

			release.SetResult(true);
			(await service.WaitFinishedAsync(WaitLimit)).ShouldBeTrue();

			service.Behavior = static (_, _) => Task.FromResult("next");

			var second = await host.ConnectAsync();
			await using var owner = Own(second);

			var next = ((ILinqService)new SignalRLinqServiceClient(second)).ExecuteReaderAsync(Configuration, "query");

			(await WaitAsync(next, WaitLimit)).ShouldBeTrue("the late call kept its permit");
			(await next).ShouldBe("next");
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
		// Per-hub options (AddHubOptions<THub>) win over the global ones for the size limit and the default
		// parallelism, whatever else they set.
		[Test]
		public async Task PerHubOptionsApply()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<ScriptedHub>(
				services => services.AddSingleton<ILinqService>(service),
				hub    => hub.MaximumReceiveMessageSize = 1024 * 1024,
				perHub: hub =>
				{
					hub.MaximumReceiveMessageSize           = 4096;
					hub.MaximumParallelInvocationsPerClient = 2;
					hub.SupportedProtocols                  = null;
				});

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			(await hubConnection.InvokeAsync<long>("GetMaximumReceiveMessageSize")).ShouldBe(4096);

			await RunConcurrentCallsAsync(host, connections: 1, callsPerConnection: 6);

			service.MaxConcurrency.ShouldBe(2);
		}

#if NET9_0_OR_GREATER
		// With tracing on, the .NET 9+ client adds trace context headers to every invocation. A request the size check
		// lets through must still fit with them, or the server closes the connection after all. Untraced, the same
		// request fits.
		[Test]
		public async Task RequestSizeCheckLeavesRoomForTraceHeaders([Values] bool traced)
		{
			var queryData = new string('a', 20_000);
			var frame     = new System.Buffers.ArrayBufferWriter<byte>();

			// The longest invocation id the client generates: a request of exactly this frame passes the check.
			new JsonHubProtocol().WriteMessage(new StreamInvocationMessage("2147483647", "ExecuteNonQueryStream", [Configuration, queryData]), frame);

			var service = new ScriptedLinqService { Behavior = static (query, _) => Task.FromResult(query) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service), perHub: hub => hub.MaximumReceiveMessageSize = frame.WrittenCount);

			// Traces this test's calls only.
			using var trace    = new Activity(nameof(RequestSizeCheckLeavesRoomForTraceHeaders)).SetIdFormat(ActivityIdFormat.W3C).Start();
			var       traceId  = trace.TraceId;
			using var listener = new ActivityListener
			{
				ShouldListenTo = static source => source.Name == "Microsoft.AspNetCore.SignalR.Client",
				Sample         = (ref options) => traced && options.Parent.TraceId == traceId ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
			};

			ActivitySource.AddActivityListener(listener);

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var error = await CatchAsync(() => ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteNonQueryAsync(Configuration, queryData));

			// Refused before sending, or sent within the limit; never a closed connection.
			(error is null or LinqToDBException).ShouldBeTrue(error?.ToString());
			hubConnection.State.ShouldBe(HubConnectionState.Connected);
		}

		// The trace context the client propagates grows with the tracestate and baggage of the current activity, far
		// beyond a fixed reserve. The limit admits the request with a traceparent only, but not with the real headers:
		// with little slack the exact measurement decides, with more the cheap bound alone would let it through.
		[Test]
		public async Task RequestSizeCheckCountsThePropagatedTraceContext([Values(50, 400)] int slack)
		{
			var queryData = new string('a', 20_000);
			var frame     = new System.Buffers.ArrayBufferWriter<byte>();

			new JsonHubProtocol().WriteMessage(new StreamInvocationMessage("2147483647", "ExecuteNonQueryStream", [Configuration, queryData]), frame);

			// The request, a traceparent with its "headers" wrapper (well under 128 bytes), and the slack; the tracestate
			// and baggage below take about 2 KB more.
			var limit = frame.WrittenCount + 128 + slack;

			var service = new ScriptedLinqService { Behavior = static (query, _) => Task.FromResult(query) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service), perHub: hub => hub.MaximumReceiveMessageSize = limit);

			using var trace = new Activity(nameof(RequestSizeCheckCountsThePropagatedTraceContext)).SetIdFormat(ActivityIdFormat.W3C);

			trace.TraceStateString = "vendor=" + new string('s', 200);

			for (var i = 0; i < 15; i++)
				trace.AddBaggage("item" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), new string('b', 100));

			trace.Start();

			var traceId  = trace.TraceId;
			using var listener = new ActivityListener
			{
				ShouldListenTo = static source => source.Name == "Microsoft.AspNetCore.SignalR.Client",
				Sample         = (ref options) => options.Parent.TraceId == traceId ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
			};

			ActivitySource.AddActivityListener(listener);

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var error = await WithinLimit(CatchAsync(() => ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteNonQueryAsync(Configuration, queryData)));

			// Refused before sending: sent, it would close the connection.
			error.ShouldBeOfType<LinqToDBException>(error?.ToString());
			hubConnection.State.ShouldBe(HubConnectionState.Connected);
			service.Calls.ShouldBe(0);
		}
#endif

#endif

		// The bound counts six bytes for every non-ASCII character, but the JSON protocol writes them as UTF-8: a
		// request over the bound and under the limit must still be sent.
		[Test]
		public async Task NonAsciiRequestUnderTheLimitIsSent()
		{
			var service = new ScriptedLinqService { Behavior = static (query, _) => Task.FromResult(query) };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var hubConnection = await host.ConnectAsync();
			await using var owner = Own(hubConnection);

			var queryData = new string('\u00e9', 6000);

			(await ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteNonQueryAsync(Configuration, queryData)).ShouldBe(6000);
			service.Calls.ShouldBe(1);
		}

		#endregion

		#region Errors

		[Test]
		public async Task ServerErrorTextIsHiddenByDefault()
		{
			var service = new ScriptedLinqService { Behavior = static (_, _) => throw new InvalidOperationException("secret detail") };
			using var host = TestHost.Start<ScriptedHub>(services => services.AddSingleton<ILinqService>(service));

			var error = await CallAsync(host);

			error.ShouldBeOfType<HubException>().Message.ShouldNotContain("secret detail");
			service.Calls.ShouldBe(1);
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

			return await WithinLimit(CatchAsync(() => ((ILinqService)new SignalRLinqServiceClient(hubConnection)).ExecuteReaderAsync(Configuration, "query")));
		}

		#endregion

		#region Clients older than 6.6.0

		// A client older than 6.6.0 calls the hub's original methods by name, as plain invocations; a 6.6.0 server
		// still serves it.
		[Test]
		public async Task OldClientRoundTrips()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var hubConnection = await host.ConnectAsync();

			await using (Own(hubConnection))
			{
				(await WithinLimit(hubConnection.InvokeAsync<LinqServiceInfo>("GetInfoAsync", Configuration))).ShouldNotBeNull();

				await using var db = new OldClientContext(hubConnection);

				(await db.InsertAsync(new Item { Id = 1, Value = "one" })).ShouldBe(1);                     // ExecuteNonQuery
				(await db.GetTable<Item>().CountAsync()).ShouldBe(1);                                         // ExecuteScalar
				(await db.GetTable<Item>().ToListAsync()).Single().Value.ShouldBe("one");                     // ExecuteReader

				db.BeginBatch();
				await db.InsertAsync(new Item { Id = 2, Value = "two" });
				await db.InsertAsync(new Item { Id = 3, Value = "three" });
				await db.CommitBatchAsync();                                                                  // ExecuteBatch

				(await db.GetTable<Item>().CountAsync()).ShouldBe(3);
			}
		}

		// The global limit holds for the calls of old clients too.
		[Test]
		public async Task OldClientCallsHonourMaxConcurrentCalls()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<OldClientHub>(services =>
			{
				services.AddSingleton<ILinqService>(service);
				services.Configure<LinqToDBHubOptions>(o =>
				{
					o.MaxConcurrentCallsPerConnection = 4;
					o.MaxConcurrentCalls              = 1;
				});
			});

			await RunConcurrentOldClientCallsAsync(host, connections: 3, callsPerConnection: 2);

			service.MaxConcurrency.ShouldBe(1);
			service.Calls.ShouldBe(6);
		}

#if !NETFRAMEWORK
		// The per-connection limit holds for the calls of old clients too. (The legacy server runs the plain
		// invocations of a connection one at a time anyway.)
		[Test]
		public async Task OldClientCallsHonourMaxConcurrentCallsPerConnection()
		{
			var service = new ScriptedLinqService { Behavior = ScriptedLinqService.Delay(200) };
			using var host = TestHost.Start<OldClientHub>(
				services =>
				{
					services.AddSingleton<ILinqService>(service);
					services.Configure<LinqToDBHubOptions>(o => o.MaxConcurrentCallsPerConnection = 2);
				},
				hub => hub.MaximumParallelInvocationsPerClient = 8);

			await RunConcurrentOldClientCallsAsync(host, connections: 1, callsPerConnection: 6);

			service.MaxConcurrency.ShouldBe(2);
			service.Calls.ShouldBe(6);
		}
#endif

		static async Task RunConcurrentOldClientCallsAsync(TestHost host, int connections, int callsPerConnection)
		{
			var hubConnections = new List<HubConnection>();

			try
			{
				for (var i = 0; i < connections; i++)
					hubConnections.Add(await host.ConnectAsync());

				await WithinLimit(Task.WhenAll(hubConnections.SelectMany(c => Enumerable.Range(0, callsPerConnection).Select(n =>
					c.InvokeAsync<string>("ExecuteReaderAsync", Configuration, "query")))));
			}
			finally
			{
				foreach (var c in hubConnections)
					await c.DisposeAsync();
			}
		}

		// A hub type of its own: the global limit is shared by all hubs of one type in the process.
		sealed class OldClientHub(ILinqService service, IOptions<LinqToDBHubOptions> options) : LinqToDBHub(options)
		{
			protected override ILinqService CreateLinqService()
			{
				return service;
			}
		}

		// The remote context of a client older than 6.6.0: every call is a plain invocation of the method of the same name.
		sealed class OldClientContext(HubConnection hubConnection) : RemoteDataContextBase(new DataOptions().UseConfiguration(Configuration))
		{
			protected override ILinqService GetClient()
			{
				return new OldClient(hubConnection);
			}

			protected override string ContextIDPrefix => "OldSignalR";

			sealed class OldClient(HubConnection hubConnection) : ILinqService
			{
				public string? RemoteClientTag { get; set; } = "Signal/R";

				public Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken = default)
				{
					return hubConnection.InvokeAsync<LinqServiceInfo>("GetInfoAsync", configuration, cancellationToken);
				}

				public Task<int> ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
				{
					return hubConnection.InvokeAsync<int>("ExecuteNonQueryAsync", configuration, queryData, cancellationToken);
				}

				public Task<string?> ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
				{
					return hubConnection.InvokeAsync<string?>("ExecuteScalarAsync", configuration, queryData, cancellationToken);
				}

				public Task<string> ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
				{
					return hubConnection.InvokeAsync<string>("ExecuteReaderAsync", configuration, queryData, cancellationToken);
				}

				public Task<int> ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken = default)
				{
					return hubConnection.InvokeAsync<int>("ExecuteBatchAsync", configuration, queryData, cancellationToken);
				}
			}
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

			(await WaitAsync(lost.Task, WaitLimit)).ShouldBeTrue("the client did not notice the server going away");

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

#if !NETFRAMEWORK
		// Signal/R raises Reconnecting/Reconnected/Closed asynchronously, so they may arrive in any order. A late
		// Reconnecting after Reconnected must not leave the wrapper waiting for a reconnect that already happened.
		[Test]
		public async Task DIConnectionIgnoresReorderedEvents()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			await using var provider = BuildClientServices(host.Port);

			var connection = provider.GetRequiredService<LinqToDBSignalRConnection>();

			await connection.EnsureConnectedAsync();

			const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

			await (Task)typeof(LinqToDBSignalRConnection).GetMethod("OnReconnected",  Flags)!.Invoke(connection, ["id"])!;
			await (Task)typeof(LinqToDBSignalRConnection).GetMethod("OnReconnecting", Flags)!.Invoke(connection, [null])!;

			var sw = Stopwatch.StartNew();

			await using (var scope = provider.CreateAsyncScope())
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);

			sw.Elapsed.ShouldBeLessThan(Prompt);
		}
#endif

		[Test]
		public async Task DIContainerDisposesTheConnection()
		{
			var provider   = BuildClientServices(TestHost.GetFreePort());
			var connection = provider.GetRequiredService<LinqToDBSignalRConnection>();

			await provider.DisposeAsync();

			(await CatchAsync(() => connection.EnsureConnectedAsync())).ShouldBeOfType<ObjectDisposedException>();
		}

		// The synchronous Dispose stops the connection before it returns, and the connection is not started again.
		[Test]
		public async Task SyncDisposeStopsTheConnection()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var connection = new LinqToDBSignalRConnection(new Uri(host.HubUrl));
			var closed     = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			connection.HubConnection.Closed += _ =>
			{
				closed.TrySetResult(true);
				return Task.CompletedTask;
			};

			await connection.EnsureConnectedAsync();

			connection.Dispose();

#if !NETFRAMEWORK
			connection.HubConnection.State.ShouldBe(HubConnectionState.Disconnected);
#endif
			(await WaitAsync(closed.Task, WaitLimit)).ShouldBeTrue("the connection was not stopped");
			(await CatchAsync(() => connection.EnsureConnectedAsync())).ShouldBeOfType<ObjectDisposedException>();

			// A second disposal does nothing.
			connection.Dispose();
		}

		// ConfigureConnection reaches the hub connection builder: a logger set there sees the connection start.
		[Test]
		public async Task ConfigureConnectionTakesEffect()
		{
			using var database = new SqliteDatabase();
			using var host     = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			var log = new LogEventCounter("Started");

			await using var provider = BuildClientServices(host.Port, options =>
				options.ConfigureConnection = builder => builder.ConfigureLogging(logging => logging.AddProvider(log).SetMinimumLevel(LogLevel.Trace)));

			await using (var scope = provider.CreateAsyncScope())
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);

			log.Count.ShouldBeGreaterThan(0, "the logger set by ConfigureConnection saw nothing");
		}

#if !NETFRAMEWORK
		// A reconnect policy set by ConfigureConnection replaces the automatic reconnect the connection is built with.
		[Test]
		public async Task ConfigureConnectionReplacesTheReconnectPolicy()
		{
			using var database = new SqliteDatabase();

			var policy = new RecordingRetryPolicy();
			var host   = TestHost.Start<SqliteHub>(services => services.AddSingleton(database));

			await using var provider = BuildClientServices(host.Port, options =>
				options.ConfigureConnection = builder => builder.WithAutomaticReconnect(policy));

			using (host)
			{
				await using var scope = provider.CreateAsyncScope();
				(await scope.ServiceProvider.GetRequiredService<ClientContext>().GetTable<Item>().CountAsync()).ShouldBe(0);
			}

			(await WaitAsync(policy.Called.Task, WaitLimit)).ShouldBeTrue("the connection did not use the reconnect policy");
		}

		sealed class RecordingRetryPolicy : IRetryPolicy
		{
			public TaskCompletionSource<bool> Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

			public TimeSpan? NextRetryDelay(RetryContext retryContext)
			{
				Called.TrySetResult(true);
				return null;
			}
		}
#endif

		// A start that runs out of time fails every caller waiting for it with TimeoutException, including one that
		// joined the start later and has time left of its own.
		[Test]
		public async Task ConnectTimeoutFailsEveryWaitingCallerWithTimeoutException()
		{
			// Takes connections into its backlog and never answers them, so the start hangs until the timeout.
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();

			try
			{
				var port = ((IPEndPoint)listener.LocalEndpoint).Port;
				var sw   = Stopwatch.StartNew();

				var connection = new LinqToDBSignalRConnection(
					new Uri($"http://127.0.0.1:{port}{HubPath}"),
					new LinqToDBSignalRClientOptions { ConnectTimeout = TimeSpan.FromSeconds(2) });

				var first = CatchAsync(() => connection.EnsureConnectedAsync());

				// The second caller's own deadline is a second after the start's.
				await Task.Delay(1000);

				var second = CatchAsync(() => connection.EnsureConnectedAsync());

				(await first).ShouldBeOfType<TimeoutException>();
				(await second).ShouldBeOfType<TimeoutException>();
				sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(6), "the callers waited past the connect timeout");

				// The timed-out start does not hold up the disposal either.
				sw.Restart();
				await connection.DisposeAsync();
				sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5), "disposal waited for the timed-out start");
			}
			finally
			{
				listener.Stop();
			}
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
				using var noBaseline = new DisableBaseline("polls pg_stat_activity: the number of polls depends on timing");
				using var admin      = GetDataConnection(context);
				using var cts        = new CancellationTokenSource();

				const string SleepingQuery = "SELECT count(*)::int FROM pg_stat_activity WHERE state = 'active' AND query LIKE '%pg_sleep(10)%' AND pid <> pg_backend_pid()";

				var query = CatchAsync(() => db.FromSql<SleepRow>("SELECT 1 AS \"Value\" FROM pg_sleep(10)").ToListAsync(cts.Token));

				// Cancel only once the command is really running in the database.
				(await WaitForAsync(() => admin.Execute<int>(SleepingQuery) == 1, TimeSpan.FromSeconds(8))).ShouldBeTrue("the query never started in the database");

				var sw = Stopwatch.StartNew();

				cts.Cancel();

				(await query).ShouldBeAssignableTo<OperationCanceledException>();
				sw.Elapsed.ShouldBeLessThan(Prompt);

				// Bounded by Prompt, not WaitLimit: the sleep would end by itself after 10 s.
				(await WaitForAsync(() => admin.Execute<int>(SleepingQuery) == 0, Prompt)).ShouldBeTrue("the query kept running in the database");

				sw.Restart();
				(await db.FromSql<SleepRow>("SELECT 2 AS \"Value\"").ToListAsync()).Single().Value.ShouldBe(2);
				sw.Elapsed.ShouldBeLessThan(Prompt);
			}
		}

		// A read cancelled while the rows come in cancels the command and fails with OperationCanceledException, not
		// with the error the provider reports for the cancelled command.
		[Test]
		public async Task CancelledRowReadFailsWithOperationCanceledException([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var noBaseline = new DisableBaseline("the number of rows read before the cancellation depends on timing");
			using var cts        = new CancellationTokenSource(TimeSpan.FromSeconds(2));

			await using var db = new InProcessConfigurationContext(context);

			// Rows trickle in at about one per millisecond, so the token fires while they are read.
			var query = CatchAsync(() => db.FromSql<SleepRow>("SELECT g AS \"Value\", pg_sleep(0.001)::text AS \"Sleep\" FROM generate_series(1, 1000000) g").ToListAsync(cts.Token));

			(await WaitAsync(query, TimeSpan.FromSeconds(20))).ShouldBeTrue("the server kept reading rows after the call was cancelled");

			var error = await query;
			error.ShouldBeAssignableTo<OperationCanceledException>(error?.ToString());
		}

		sealed class InProcessConfigurationContext(string target) : RemoteDataContextBase(new DataOptions().UseConfiguration(Configuration + ".InProcess"))
		{
			protected override ILinqService GetClient()
			{
				return new TargetLinqService(target);
			}

			protected override string ContextIDPrefix => "InProcessConfiguration";

			sealed class TargetLinqService(string target) : LinqService
			{
				public override DataConnection CreateDataContext(string? configuration)
				{
					return new DataConnection(new DataOptions().UseConfiguration(target));
				}
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

		// Counts the log events of one name, e.g. those the Signal/R client writes for the messages it sends.
		sealed class LogEventCounter(string eventName) : ILoggerProvider, ILogger, IDisposable
		{
			int _count;

			public int Count => Volatile.Read(ref _count);

			public ILogger CreateLogger(string categoryName)
			{
				return this;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			{
				if (eventId.Name == eventName)
					Interlocked.Increment(ref _count);
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public IDisposable BeginScope<TState>(TState state)
				where TState : notnull
			{
				return this;
			}

			public void Dispose()
			{
			}
		}

		static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
		{
			return await Task.WhenAny(task, Task.Delay(timeout)) == task;
		}

		// Fails the test, rather than hanging the run, when a call never finishes.
		static async Task<T> WithinLimit<T>(Task<T> task)
		{
			(await WaitAsync(task, WaitLimit)).ShouldBeTrue("the call did not finish");
			return await task;
		}

		static async Task WithinLimit(Task task)
		{
			(await WaitAsync(task, WaitLimit)).ShouldBeTrue("the call did not finish");
			await task;
		}

		static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
		{
			var sw = Stopwatch.StartNew();

			while (!condition())
			{
				if (sw.Elapsed > timeout)
					return false;

				await Task.Delay(20);
			}

			return true;
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

				await WithinLimit(Task.WhenAll(hubConnections.SelectMany(c => Enumerable.Range(0, callsPerConnection).Select(n =>
					((ILinqService)new SignalRLinqServiceClient(c)).ExecuteReaderAsync(Configuration, "query")))));
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

		sealed class OwnOptionsHub(ILinqService service) : LinqToDBHub(Options.Create(new LinqToDBHubOptions { MaxConcurrentCalls = 1 }))
		{
			protected override ILinqService CreateLinqService()
			{
				return service;
			}
		}

		sealed class PlainConstructorHub(ILinqService service) : LinqToDBHub
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

				// Reports a token that fires while the behaviour ignores it. One that ends the behaviour is seen by the check
				// in finally instead: .NET Framework runs the continuations of a cancelled Task.Delay inside Cancel(),
				// before this callback (callbacks run in reverse order of registration), and the registration is
				// disposed before its turn comes.
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
					if (cancellationToken.IsCancellationRequested)
						_cancelled.TrySetResult(true);

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

			TestHost(string hostName, int port,
#if NETFRAMEWORK
				IWebHost host
#else
				IHost host
#endif
				)
			{
				_hostName = hostName;
				Port      = port;
				_host     = host;
			}

			readonly string _hostName;

			public int    Port   { get; }
			public string HubUrl => $"http://{_hostName}:{Port}{HubPath}";

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
				// Without a port the server binds a free one itself: a port found free beforehand may be taken by
				// another test in between. Kestrel binds port 0 on an IP address only, not on localhost.
				var hostName = port == null ? "127.0.0.1" : "localhost";
				var url      = $"http://{hostName}:{port ?? 0}";

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

#if NETFRAMEWORK
				var addresses = host.ServerFeatures.Get<IServerAddressesFeature>()!.Addresses;
#else
				var addresses = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
#endif

				return new TestHost(hostName, port ?? new Uri(addresses.First()).Port, host);
			}

			public async Task<HubConnection> ConnectAsync(string? accessToken = null, ILoggerProvider? logger = null)
			{
				var builder = new HubConnectionBuilder()
					.WithUrl(HubUrl, http =>
					{
						if (accessToken != null)
							http.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
					});

				if (logger != null)
					builder.ConfigureLogging(logging => logging.AddProvider(logger).SetMinimumLevel(LogLevel.Trace));

				var hubConnection = builder.Build();

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
