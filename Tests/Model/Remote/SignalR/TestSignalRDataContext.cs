using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Remote.SignalR;

using Microsoft.AspNetCore.SignalR.Client;

namespace Tests.Model.Remote.SignalR
{
	public class TestSignalRDataContext : SignalRDataContext, ITestDataContext
	{
		// Started hub connections that no test context is using, per hub URL. A context takes one for
		// its lifetime and gives it back on dispose, so sequential tests reuse a connection instead of
		// a negotiate request plus a WebSocket per test, while contexts alive at the same time still
		// get separate connections: the test hub runs one invocation per connection at a time, so a
		// single shared connection would serialize concurrent remote tests.
		static readonly Dictionary<string, Stack<PooledHubConnection>> _idleConnections = new(StringComparer.Ordinal);
		static readonly Lock                                           _idleConnectionsLock = new();

		// Idle connections kept per URL; more than this only exist while that many contexts are alive.
		const int MaxIdleConnectionsPerUrl = 32;

		readonly string      _hubUrl;
		PooledHubConnection? _connection;

		public TestSignalRDataContext(string hubUrl, Func<DataOptions, DataOptions>? optionBuilder = null)
			: this(hubUrl, RentConnection(hubUrl), optionBuilder)
		{
		}

		TestSignalRDataContext(string hubUrl, PooledHubConnection connection, Func<DataOptions, DataOptions>? optionBuilder)
			: base(new SignalRLinqServiceClient(connection.Connection), optionBuilder)
		{
			_hubUrl     = hubUrl;
			_connection = connection;
		}

		public override void Dispose()
		{
			try
			{
				base.Dispose();
			}
			finally
			{
				ReturnConnection();
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
				ReturnConnection();
			}
		}

		void ReturnConnection()
		{
			var connection = Interlocked.Exchange(ref _connection, null);

			if (connection == null)
				return;

			if (connection.IsUsable)
			{
				lock (_idleConnectionsLock)
				{
					if (!_idleConnections.TryGetValue(_hubUrl, out var idle))
						_idleConnections.Add(_hubUrl, idle = new Stack<PooledHubConnection>());

					if (idle.Count < MaxIdleConnectionsPerUrl)
					{
						idle.Push(connection);
						return;
					}
				}
			}

			connection.Dispose();
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
				if (connection.IsUsable)
					return connection;

				connection.Dispose();
			}
		}

		sealed class PooledHubConnection : IDisposable
		{
			volatile bool _closed;

			PooledHubConnection(HubConnection connection)
			{
				Connection         = connection;
				Connection.Closed += _ =>
				{
					_closed = true;
					return Task.CompletedTask;
				};
			}

			public HubConnection Connection { get; }

			// Without automatic reconnect a connection that drops stays down. It reports Disconnected
			// before it raises Closed, so check both where the client has State. The net462 client has
			// no State, so a connection that dropped but has not raised Closed yet can still be handed
			// out there; only the test using it fails, and it is discarded when that context returns it.
#if NETFRAMEWORK
			public bool IsUsable => !_closed;
#else
			public bool IsUsable => !_closed && Connection.State == HubConnectionState.Connected;
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
