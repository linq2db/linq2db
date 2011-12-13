using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

using JetBrains.Annotations;

namespace LinqToDB.ServiceModel
{
	using Data.Linq;

	using NotNullAttribute = NotNullAttribute;

	public class SoapDataContext : RemoteDataContextBase
	{
		#region Init

		SoapDataContext()
		{
		}

		public SoapDataContext([NotNull] string endpointConfigurationName)
			: this()
		{
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");

			_endpointConfigurationName = endpointConfigurationName;
		}

		public SoapDataContext([NotNull] string endpointConfigurationName, [NotNull] string remoteAddress)
			: this()
		{
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");
			if (remoteAddress             == null) throw new ArgumentNullException("remoteAddress");

			_endpointConfigurationName = endpointConfigurationName;
			_remoteAddress             = remoteAddress;
		}

		public SoapDataContext([NotNull] string endpointConfigurationName, [NotNull] EndpointAddress endpointAddress)
			: this()
		{
			if (endpointConfigurationName == null) throw new ArgumentNullException("endpointConfigurationName");
			if (endpointAddress           == null) throw new ArgumentNullException("endpointAddress");

			_endpointConfigurationName = endpointConfigurationName;
			_endpointAddress           = endpointAddress;
		}

		public SoapDataContext([NotNull] Binding binding, [NotNull] EndpointAddress endpointAddress)
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

		protected override ILinqService GetClient()
		{
			if (Binding != null)
				return new LinqSoapServiceClient(Binding, _endpointAddress);

			if (_endpointAddress != null)
				return new LinqSoapServiceClient(_endpointConfigurationName, _endpointAddress);

			if (_remoteAddress != null)
				return new LinqSoapServiceClient(_endpointConfigurationName, _remoteAddress);

			return new LinqSoapServiceClient(_endpointConfigurationName);
		}

		protected override IDataContext Clone()
		{
			return new SoapDataContext
			{
				MappingSchema              = MappingSchema,
				Binding                    = Binding,
				_endpointConfigurationName = _endpointConfigurationName,
				_remoteAddress             = _remoteAddress,
				_endpointAddress           = _endpointAddress,
			};
		}

		protected override string ContextIDPrefix
		{
			get { return "LinqSoapService_"; }
		}

		#endregion
	}
}
