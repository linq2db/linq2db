using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

using JetBrains.Annotations;

namespace LinqToDB.ServiceModel
{
	public class ServiceModelDataContext : RemoteDataContextBase
	{
		#region Init

		ServiceModelDataContext()
		{
		}

		public ServiceModelDataContext([NotNull] string endpointConfigurationName)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
		}

		public ServiceModelDataContext([NotNull] string endpointConfigurationName, [NotNull] string remoteAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_remoteAddress             = remoteAddress ?? throw new ArgumentNullException(nameof(remoteAddress));
		}

		public ServiceModelDataContext([NotNull] string endpointConfigurationName, [NotNull] EndpointAddress endpointAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_endpointAddress           = endpointAddress           ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		public ServiceModelDataContext([NotNull] Binding binding, [NotNull] EndpointAddress endpointAddress)
			: this()
		{
			Binding          = binding         ?? throw new ArgumentNullException(nameof(binding));
			_endpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		string          _endpointConfigurationName;
		string          _remoteAddress;
		EndpointAddress _endpointAddress;

		public Binding Binding { get; private set; }

		#endregion

		#region Overrides

		protected override ILinqClient GetClient()
		{
			if (Binding != null)
				return new LinqServiceClient(Binding, _endpointAddress);

			if (_endpointAddress != null)
				return new LinqServiceClient(_endpointConfigurationName, _endpointAddress);

			if (_remoteAddress != null)
				return new LinqServiceClient(_endpointConfigurationName, _remoteAddress);

			return new LinqServiceClient(_endpointConfigurationName);
		}

		protected override IDataContext Clone()
		{
			return new ServiceModelDataContext
			{
				MappingSchema              = MappingSchema,
				Configuration              = Configuration,
				Binding                    = Binding,
				_endpointConfigurationName = _endpointConfigurationName,
				_remoteAddress             = _remoteAddress,
				_endpointAddress           = _endpointAddress,
			};
		}

		protected override string ContextIDPrefix => "LinqService";

		#endregion
	}
}
