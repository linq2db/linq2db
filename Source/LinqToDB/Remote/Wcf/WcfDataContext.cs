#if NETFRAMEWORK
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LinqToDB.Remote.Infra;

namespace LinqToDB.Remote.Wcf
{
	public class WcfDataContext : RemoteDataContextBase
	{
#region Init

		WcfDataContext()
		{
		}

		public WcfDataContext(string endpointConfigurationName)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
		}

		public WcfDataContext(string endpointConfigurationName, string remoteAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_remoteAddress             = remoteAddress             ?? throw new ArgumentNullException(nameof(remoteAddress));
		}

		public WcfDataContext(string endpointConfigurationName, EndpointAddress endpointAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_endpointAddress           = endpointAddress           ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		public WcfDataContext(Binding binding, EndpointAddress endpointAddress)
			: this()
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

		protected override ILinqClient GetClient()
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
			return new WcfDataContext
			{
				MappingSchema              = MappingSchema,
				Configuration              = Configuration,
				Binding                    = Binding,
				_endpointConfigurationName = _endpointConfigurationName,
				_remoteAddress             = _remoteAddress,
				_endpointAddress           = _endpointAddress
			};
		}

		protected override string ContextIDPrefix => "LinqService";

#endregion
	}
}
#endif
