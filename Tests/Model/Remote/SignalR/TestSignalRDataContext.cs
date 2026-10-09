using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Remote;
using LinqToDB.Remote.SignalR;

using Microsoft.AspNetCore.SignalR.Client;

namespace Tests.Model.Remote.SignalR
{
	public class TestSignalRDataContext : SignalRDataContext, ITestDataContext
	{
		// Started hub connections that no test context is using, per hub URL. A context leases one for
		// its lifetime and gives it back on dispose, so sequential tests reuse a connection instead of
		// a negotiate request plus a WebSocket per test, while contexts alive at the same time still
		// get separate connections: the test hub runs one invocation per connection at a time, so a
		// single shared connection would serialize concurrent remote tests.
		static readonly Dictionary<string, Stack<PooledHubConnection>> _idleConnections = new(StringComparer.Ordinal);
		static readonly Lock                                           _idleConnectionsLock = new();

		// Idle connections kept per URL; more than this only exist while that many contexts are alive.
		const int MaxIdleConnectionsPerUrl = 32;

		readonly Lease _lease;

		public TestSignalRDataContext(string hubUrl, Func<DataOptions, DataOptions>? optionBuilder = null)
			: this(new Lease(hubUrl, RentConnection(hubUrl)), optionBuilder)
		{
		}

		TestSignalRDataContext(Lease lease, Func<DataOptions, DataOptions>? optionBuilder)
			: base(lease.Connection.Client, optionBuilder)
		{
			_lease = lease;
		}

		/// <summary>
		/// The client this context runs its queries through. It stops working when the context is disposed.
		/// </summary>
		public ILinqService LeasedClient => _lease;

		protected override ILinqService GetClient()
		{
			// null only if the base constructor asks for a client before this one has run
			return (ILinqService?)_lease ?? base.GetClient();
		}

		public override void Dispose()
		{
			try
			{
				base.Dispose();
			}
			finally
			{
				_lease.Return();
			}
		}

		public override async ValueTask DisposeAsync()
		{
			try
			{
				await base.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				_lease.Return();
			}
		}

		static PooledHubConnection RentConnection(string hubUrl)
		{
			while (true)
			{
				PooledHubConnection? connection = null;

				lock (_idleConnectionsLock)
				{
					if (_idleConnections.TryGetValue(hubUrl, out var idle) && idle.Count > 0)
						connection = idle.Pop();
				}

				if (connection == null)
					return PooledHubConnection.Start(hubUrl);

				// It may have dropped while idle; a dropped connection would fail the test that gets it.
				if (connection.IsConnected)
					return connection;

				connection.Dispose();
			}
		}

		static void ReturnConnection(string hubUrl, PooledHubConnection connection)
		{
			if (connection.IsConnected)
			{
				lock (_idleConnectionsLock)
				{
					if (!_idleConnections.TryGetValue(hubUrl, out var idle))
						_idleConnections.Add(hubUrl, idle = new Stack<PooledHubConnection>());

					if (idle.Count < MaxIdleConnectionsPerUrl)
					{
						idle.Push(connection);
						return;
					}
				}
			}

			connection.Dispose();
		}

		// One context's use of a pooled connection. Every call is admitted under the lease's lock, and the
		// lease is returned under the same lock, so a call that starts after the return (from a query
		// runner that outlived its context) fails instead of running on the next context's connection.
		//
		// The hub runs one invocation per connection at a time, and the server keeps running an invocation
		// whose client call was cancelled. So the connection goes back to the pool only if no call is
		// running and none was cancelled or failed; otherwise the next test could wait behind it.
		sealed class Lease(string hubUrl, PooledHubConnection connection) : ILinqService
		{
			readonly Lock _lock = new();

			int  _running;
			bool _interrupted;
			bool _returned;

			public PooledHubConnection Connection { get; } = connection;

			public void Return()
			{
				bool reusable;

				lock (_lock)
				{
					if (_returned)
						return;

					_returned = true;
					reusable  = _running == 0 && !_interrupted;
				}

				if (reusable)
					ReturnConnection(hubUrl, Connection);
				else
					Connection.Dispose();
			}

			async Task<T> RunAsync<T>(Func<ILinqService, Task<T>> invoke)
			{
				lock (_lock)
				{
					ObjectDisposedException.ThrowIf(_returned, typeof(TestSignalRDataContext));

					_running++;
				}

				try
				{
					return await invoke(Connection.Client).ConfigureAwait(false);
				}
				catch
				{
					lock (_lock)
						_interrupted = true;

					throw;
				}
				finally
				{
					lock (_lock)
						_running--;
				}
			}

			Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
			{
				return RunAsync(c => c.GetInfoAsync(configuration, cancellationToken));
			}

			Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				return RunAsync(c => c.ExecuteNonQueryAsync(configuration, queryData, cancellationToken));
			}

			Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				return RunAsync(c => c.ExecuteScalarAsync(configuration, queryData, cancellationToken));
			}

			Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				return RunAsync(c => c.ExecuteReaderAsync(configuration, queryData, cancellationToken));
			}

			Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				return RunAsync(c => c.ExecuteBatchAsync(configuration, queryData, cancellationToken));
			}

			string? ILinqService.RemoteClientTag
			{
				get => ((ILinqService)Connection.Client).RemoteClientTag;
				set => ((ILinqService)Connection.Client).RemoteClientTag = value;
			}
		}

		sealed class PooledHubConnection : IDisposable
		{
			volatile bool _closed;

			PooledHubConnection(HubConnection connection)
			{
				Connection         = connection;
				Client             = new SignalRLinqServiceClient(connection);
				Connection.Closed += _ =>
				{
					_closed = true;
					return Task.CompletedTask;
				};
			}

			public HubConnection            Connection { get; }
			public SignalRLinqServiceClient Client     { get; }

			// Without automatic reconnect a connection that drops stays down. It reports Disconnected
			// before it raises Closed, so check both where the client has State. The net462 client has
			// no State, so a connection that dropped but has not raised Closed yet can still be handed
			// out there; only the test using it fails, and it is not pooled again after that failure.
#if NETFRAMEWORK
			public bool IsConnected => !_closed;
#else
			public bool IsConnected => !_closed && Connection.State == HubConnectionState.Connected;
#endif

			public static PooledHubConnection Start(string hubUrl)
			{
				var connection = new PooledHubConnection(new HubConnectionBuilder().WithUrl(hubUrl).Build());

				try
				{
					connection.Connection.StartAsync().GetAwaiter().GetResult();
				}
				catch
				{
					connection.Dispose();
					throw;
				}

				return connection;
			}

			public void Dispose()
			{
				Task.Run(DisposeConnectionAsync).GetAwaiter().GetResult();
			}

			// DisposeAsync returns Task on net462 and ValueTask on net8.0+, so awaiting it is the one form
			// that compiles on both; MA0215 fires on net462 only, where returning the task would compile.
#pragma warning disable MA0215 // Return the task instead of awaiting it
			async Task DisposeConnectionAsync()
			{
				await Connection.DisposeAsync().ConfigureAwait(false);
			}
#pragma warning restore MA0215
		}

		public ITable<Person>                 Person                 => this.GetTable<Person>();
		public ITable<ComplexPerson>          ComplexPerson          => this.GetTable<ComplexPerson>();
		public ITable<Patient>                Patient                => this.GetTable<Patient>();
		public ITable<Doctor>                 Doctor                 => this.GetTable<Doctor>();
		public ITable<Parent>                 Parent                 => this.GetTable<Parent>();
		public ITable<Parent1>                Parent1                => this.GetTable<Parent1>();
		public ITable<IParent>                Parent2                => this.GetTable<IParent>();
		public ITable<Parent4>                Parent4                => this.GetTable<Parent4>();
		public ITable<Parent5>                Parent5                => this.GetTable<Parent5>();
		public ITable<ParentInheritanceBase>  ParentInheritance      => this.GetTable<ParentInheritanceBase>();
		public ITable<ParentInheritanceBase2> ParentInheritance2     => this.GetTable<ParentInheritanceBase2>();
		public ITable<ParentInheritanceBase3> ParentInheritance3     => this.GetTable<ParentInheritanceBase3>();
		public ITable<ParentInheritanceBase4> ParentInheritance4     => this.GetTable<ParentInheritanceBase4>();
		public ITable<ParentInheritance1>     ParentInheritance1     => this.GetTable<ParentInheritance1>();
		public ITable<ParentInheritanceValue> ParentInheritanceValue => this.GetTable<ParentInheritanceValue>();
		public ITable<Child>                  Child                  => this.GetTable<Child>();
		public ITable<GrandChild>             GrandChild             => this.GetTable<GrandChild>();
		public ITable<GrandChild1>            GrandChild1            => this.GetTable<GrandChild1>();
		public ITable<LinqDataTypes>          Types                  => this.GetTable<LinqDataTypes>();
		public ITable<LinqDataTypes2>         Types2                 => this.GetTable<LinqDataTypes2>();
		public ITable<TestIdentity>           TestIdentity           => this.GetTable<TestIdentity>();
		public ITable<InheritanceParentBase>  InheritanceParent      => this.GetTable<InheritanceParentBase>();
		public ITable<InheritanceChildBase>   InheritanceChild       => this.GetTable<InheritanceChildBase>();

		public ITable<Parent> GetParentByID(int? id)
		{
			throw new NotImplementedException();
		}
	}
}
