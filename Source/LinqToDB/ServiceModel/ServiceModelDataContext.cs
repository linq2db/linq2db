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
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");

			_endpointConfigurationName = endpointConfigurationName;
		}

		public ServiceModelDataContext([NotNull] string endpointConfigurationName, [NotNull] string remoteAddress)
			: this()
		{
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");
			if (remoteAddress             == null) throw new ArgumentNullException("remoteAddress");

			_endpointConfigurationName = endpointConfigurationName;
			_remoteAddress             = remoteAddress;
		}

		public ServiceModelDataContext([NotNull] string endpointConfigurationName, [NotNull] EndpointAddress endpointAddress)
			: this()
		{
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");
			if (endpointAddress           == null) throw new ArgumentNullException("endpointAddress");

			_endpointConfigurationName = endpointConfigurationName;
			_endpointAddress           = endpointAddress;
		}

		public ServiceModelDataContext([NotNull] Binding binding, [NotNull] EndpointAddress endpointAddress)
			: this()
		{
			if (binding         == null) throw new ArgumentNullException("binding");
			if (endpointAddress == null) throw new ArgumentNullException("endpointAddress");

			Binding          = binding;
			_endpointAddress = endpointAddress;
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

		protected override string ContextIDPrefix
		{
			get { return "LinqService"; }
		}

		#endregion
	}
}
