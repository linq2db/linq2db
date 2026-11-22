#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Remote;
using LinqToDB.Remote.Wcf;

namespace Tests.Model.Remote.Wcf
{
	public class TestWcfDataContext : WcfDataContext, ITestDataContext
	{
		static readonly NetTcpBinding _binding = CreateBinding();

		// One channel factory per endpoint, shared by every test context targeting it. The library's
		// client builds a new ChannelFactory, and with it a new TCP connection, for every query; a
		// shared factory keeps its connections pooled, and a query only opens and closes a channel.
		static readonly Dictionary<string, ChannelFactory<IWcfLinqService>> _channelFactories = new(StringComparer.Ordinal);
		static readonly Lock                                                _channelFactoriesLock = new();

		readonly EndpointAddress _endpointAddress;

		public TestWcfDataContext(int port, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(new EndpointAddress(string.Create(CultureInfo.InvariantCulture, $"net.tcp://{RemoteHost.Loopback}:{port}/LinqOverWcf")), optionBuilder)
		{
		}

		TestWcfDataContext(EndpointAddress endpointAddress, Func<DataOptions,DataOptions>? optionBuilder)
			: base(_binding, endpointAddress, optionBuilder)
		{
			_endpointAddress = endpointAddress;
		}

		static NetTcpBinding CreateBinding()
		{
			var binding = new NetTcpBinding(SecurityMode.None)
			{
				MaxReceivedMessageSize = 10000000,
				MaxBufferPoolSize      = 10000000,
				MaxBufferSize          = 10000000,
				CloseTimeout           = new TimeSpan(00, 01, 00),
				OpenTimeout            = new TimeSpan(00, 01, 00),
				ReceiveTimeout         = new TimeSpan(00, 10, 00),
				SendTimeout            = new TimeSpan(00, 10, 00),
			};

			binding.ReaderQuotas.MaxStringContentLength = 1000000;

			return binding;
		}

		protected override ILinqService GetClient()
		{
			return new SharedFactoryLinqServiceClient(GetChannelFactory(_endpointAddress));
		}

		static ChannelFactory<IWcfLinqService> GetChannelFactory(EndpointAddress endpointAddress)
		{
			var key = endpointAddress.Uri.AbsoluteUri;

			lock (_channelFactoriesLock)
			{
				if (_channelFactories.TryGetValue(key, out var factory))
				{
					// A faulted factory cannot create channels any more, so replace it.
					if (factory.State is not (CommunicationState.Faulted or CommunicationState.Closing or CommunicationState.Closed))
						return factory;

					factory.Abort();
					_channelFactories.Remove(key);
				}

				factory = new ChannelFactory<IWcfLinqService>(_binding, endpointAddress);

				_channelFactories.Add(key, factory);

				return factory;
			}
		}

		// The library's WcfLinqServiceClient always creates its own ChannelFactory, so this client
		// takes a channel from the shared factory instead. Disposing it, which the remote context
		// does after each query, closes only the channel; the factory keeps the connection pooled.
		sealed class SharedFactoryLinqServiceClient : ILinqService, IDisposable
		{
			readonly IWcfLinqService _channel;

			public SharedFactoryLinqServiceClient(ChannelFactory<IWcfLinqService> channelFactory)
			{
				_channel = channelFactory.CreateChannel();
			}

			Task<LinqServiceInfo> ILinqService.GetInfoAsync(string? configuration, CancellationToken cancellationToken)
			{
				return _channel.GetInfoAsync(configuration);
			}

			Task<int> ILinqService.ExecuteNonQueryAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return _channel.ExecuteNonQueryAsync(configuration, queryData);
			}

			Task<string?> ILinqService.ExecuteScalarAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return _channel.ExecuteScalarAsync(configuration, queryData);
			}

			Task<string> ILinqService.ExecuteReaderAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return _channel.ExecuteReaderAsync(configuration, queryData);
			}

			Task<int> ILinqService.ExecuteBatchAsync(string? configuration, string queryData, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return _channel.ExecuteBatchAsync(configuration, queryData);
			}

			string? ILinqService.RemoteClientTag { get; set; } = "Wcf";

			void IDisposable.Dispose()
			{
				var channel = (IClientChannel)_channel;

				try
				{
					if (channel.State == CommunicationState.Faulted)
						channel.Abort();
					else
						channel.Close();
				}
				catch (CommunicationException)
				{
					channel.Abort();
				}
				catch (TimeoutException)
				{
					channel.Abort();
				}
			}
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
#endif
