using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework.Internal;

using Tests.Model;

namespace Tests.Remote.ServerContainer
{
	public abstract class ServerContainerBase<TService> : IServerContainer
	{
		private readonly Lock _syncRoot = new ();

		private readonly ConcurrentDictionary<int, HostEntry> _openHosts = new();

		//useful for async tests
		public bool KeepSamePortBetweenThreads { get; set; } = true;

		// Slot key (not a network port): a single shared slot, or one slot per thread.
		// Raw thread id works as a key - the old "% 1000" only kept the *derived port* in range,
		// which no longer applies now that the port is probed. Thread ids are never 0, so they
		// never collide with the shared-slot key.
		private int GetSlotKey() => KeepSamePortBetweenThreads ? 0 : Environment.CurrentManagedThreadId;

		// Probe-then-reuse: ask the OS for a free port, release it, hand back the number.
		protected static int GetFreePort()
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

		// Refreshed on every CreateContext call and read indirectly by the cached host through
		// InvokeConnectionFactory. Each call passes a different factory (the per-test factory bakes
		// in UseConfiguration/UseDataProvider), so a host created once must use the *latest* caller's
		// factory, not the one captured when it was first started.
		//
		// That "latest caller wins" only holds because the dispatcher lets one remote test run at a time
		// (DatabaseLaneStrategy's secondary mutex). A test the classifier does not see as remote would run
		// outside that mutex and overwrite this field - and TestLinqService.MappingSchema - underneath a
		// remote test on another lane, so the invariant is asserted rather than assumed.
		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		// A remote context may only be created by a test the lane classifier can see as remote, either from
		// its parameter value or via [UsesRemoteContext] - or by one that runs globally exclusive, which the
		// classifier never sees. Otherwise it runs without the secondary mutex and silently corrupts a
		// concurrent remote test's server-side state.
		private static void AssertClassifiedAsRemote()
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			if (test.Method == null)
				return;

			// A globally-exclusive test excludes every other test, remote or not, so it needs no mutex.
			if (NUnitUtils.IsGloballyExclusive(test))
				return;

			var (_, isRemote) = NUnitUtils.GetContext(test);

			if (!isRemote && !NUnitUtils.UsesRemoteContext(test))
				throw new InvalidOperationException(
					$"{test.FullName} creates a remote context but is not classified as remote, so it runs without the "
					+ "secondary mutex and can corrupt a concurrent remote test. Use a remote parameter value, or mark it [UsesRemoteContext].");
		}

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			AssertClassifiedAsRemote();

			_connectionFactory = connectionFactory;

			var entry = OpenHost();

			return CreateClientContext(entry.Service, entry.Port, optionBuilder);
		}

		private DataConnection InvokeConnectionFactory(string? configuration, MappingSchema? mappingSchema)
		{
			return _connectionFactory(configuration, mappingSchema);
		}

		// Probe-then-reuse has a TOCTTOU window: another process can claim the probed port
		// between GetFreePort() and the actual bind inside StartHost. Retry with a freshly
		// probed port a few times before giving up. 3 attempts is arbitrary: enough to absorb
		// the rare race without masking a genuine, repeatable start failure.
		private const int MaxStartAttempts = 3;

		private HostEntry OpenHost()
		{
			var slot = GetSlotKey();

			if (_openHosts.TryGetValue(slot, out var existing))
				return existing;

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(slot, out existing))
					return existing;

				var entry = StartHostWithRetry();

				_openHosts[slot] = entry;

				return entry;
			}
		}

		private HostEntry StartHostWithRetry()
		{
			for (var attempt = 1; ; attempt++)
			{
				var port = GetFreePort();

				try
				{
					return new HostEntry(StartHost(port, InvokeConnectionFactory), port);
				}
				catch (Exception) when (attempt < MaxStartAttempts)
				{
					// Probed port was taken between probe and bind; retry with a fresh one.
				}
			}
		}

		// Start the transport host bound to the given port and return the server-side service.
		// Invoked under the container lock, so the per-transport static-Startup handshake stays serialized.
		protected abstract TService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory);

		// Build the client-side data context against the given port.
		protected abstract ITestDataContext CreateClientContext(TService service, int port, Func<ITestLinqService,DataOptions, DataOptions> optionBuilder);

		private sealed class HostEntry(TService service, int port)
		{
			public TService Service { get; } = service;
			public int      Port    { get; } = port;
		}
	}
}
