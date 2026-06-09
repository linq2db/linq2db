using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

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
		private Func<string?, MappingSchema?, DataConnection> _connectionFactory = null!;

		ITestDataContext IServerContainer.CreateContext(Func<ITestLinqService,DataOptions, DataOptions> optionBuilder, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			_connectionFactory = connectionFactory;

			var entry = OpenHost();

			return CreateClientContext(entry.Service, entry.Port, optionBuilder);
		}

		private DataConnection InvokeConnectionFactory(string? configuration, MappingSchema? mappingSchema)
		{
			return _connectionFactory(configuration, mappingSchema);
		}

		private HostEntry OpenHost()
		{
			var slot = GetSlotKey();

			if (_openHosts.TryGetValue(slot, out var existing))
				return existing;

			lock (_syncRoot)
			{
				if (_openHosts.TryGetValue(slot, out existing))
					return existing;

				var port  = GetFreePort();
				var entry = new HostEntry(StartHost(port, InvokeConnectionFactory), port);

				_openHosts[slot] = entry;

				return entry;
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
