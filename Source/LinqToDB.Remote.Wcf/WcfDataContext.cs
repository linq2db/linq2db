using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LinqToDB.Remote.Wcf
{
	/// <summary>
	/// WCF-based remote data context implementation.
	/// </summary>
	public class WcfDataContext : RemoteDataContextBase
	{
		#region Init

		// clone constructor
		WcfDataContext() : base(new DataOptions())
		{
		}

		WcfDataContext(DataOptions options) : base(options)
		{
		}

		public WcfDataContext(string endpointConfigurationName) : this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
		}

		public WcfDataContext(string endpointConfigurationName, string remoteAddress) : this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_remoteAddress             = remoteAddress             ?? throw new ArgumentNullException(nameof(remoteAddress));
		}

		public WcfDataContext(string endpointConfigurationName, EndpointAddress endpointAddress) : this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_endpointAddress           = endpointAddress           ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		public WcfDataContext(Binding binding, EndpointAddress endpointAddress, Func<DataOptions,DataOptions>? optionBuilder = null)
			: this(optionBuilder == null ? new() : optionBuilder(new()))
		{
			Binding          = binding         ?? throw new ArgumentNullException(nameof(binding));
			_endpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		string?          _endpointConfigurationName;
		string?          _remoteAddress;
		EndpointAddress? _endpointAddress;

		public Binding? Binding { get; private set; }

		#endregion

		#region Overrides

		protected override ILinqService GetClient()
		{
			if (Binding != null)
				return new WcfLinqServiceClient(Binding, _endpointAddress!);

			if (_endpointAddress != null)
				return new WcfLinqServiceClient(_endpointConfigurationName!, _endpointAddress);

			if (_remoteAddress != null)
				return new WcfLinqServiceClient(_endpointConfigurationName!, _remoteAddress);

			return new WcfLinqServiceClient(_endpointConfigurationName!);
		}

		protected override string ContextIDPrefix => "WcfRemoteLinqService";

		#endregion
	}
}
