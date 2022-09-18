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
		private WcfDataContext()
		{
		}

		public WcfDataContext(string endpointConfigurationName)
		{
			_endpointConfigurationName = endpointConfigurationName ?? ThrowHelper.ThrowArgumentNullException<string>(nameof(endpointConfigurationName));
		}

		public WcfDataContext(string endpointConfigurationName, string remoteAddress)
		{
			_endpointConfigurationName = endpointConfigurationName ?? ThrowHelper.ThrowArgumentNullException<string>(nameof(endpointConfigurationName));
			_remoteAddress             = remoteAddress             ?? ThrowHelper.ThrowArgumentNullException<string>(nameof(remoteAddress));
		}

		public WcfDataContext(string endpointConfigurationName, EndpointAddress endpointAddress)
		{
			_endpointConfigurationName = endpointConfigurationName ?? ThrowHelper.ThrowArgumentNullException<string         >(nameof(endpointConfigurationName));
			_endpointAddress           = endpointAddress           ?? ThrowHelper.ThrowArgumentNullException<EndpointAddress>(nameof(endpointAddress));
		}

		public WcfDataContext(Binding binding, EndpointAddress endpointAddress)
		{
			Binding          = binding         ?? ThrowHelper.ThrowArgumentNullException<Binding        >(nameof(binding));
			_endpointAddress = endpointAddress ?? ThrowHelper.ThrowArgumentNullException<EndpointAddress>(nameof(endpointAddress));
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

		protected override IDataContext Clone()
		{
			return new WcfDataContext()
			{
				MappingSchema              = MappingSchema,
				Configuration              = Configuration,
				Binding                    = Binding,
				_endpointConfigurationName = _endpointConfigurationName,
				_remoteAddress             = _remoteAddress,
				_endpointAddress           = _endpointAddress
			};
		}

		protected override string ContextIDPrefix => "WcfRemoteLinqService";

#endregion
	}
}
