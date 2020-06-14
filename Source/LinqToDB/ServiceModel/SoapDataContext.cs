﻿using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LinqToDB.ServiceModel
{
	public class SoapDataContext : RemoteDataContextBase
	{
		#region Init

		SoapDataContext()
		{
		}

		public SoapDataContext(string endpointConfigurationName)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
		}

		public SoapDataContext(string endpointConfigurationName, string remoteAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_remoteAddress             = remoteAddress             ?? throw new ArgumentNullException(nameof(remoteAddress));
		}

		public SoapDataContext(string endpointConfigurationName, EndpointAddress endpointAddress)
			: this()
		{
			_endpointConfigurationName = endpointConfigurationName ?? throw new ArgumentNullException(nameof(endpointConfigurationName));
			_endpointAddress           = endpointAddress           ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		public SoapDataContext(Binding binding, EndpointAddress endpointAddress)
			: this()
		{
			Binding          = binding         ?? throw new ArgumentNullException(nameof(binding));
			_endpointAddress = endpointAddress ?? throw new ArgumentNullException(nameof(endpointAddress));
		}

		string?          _endpointConfigurationName;
		string?          _remoteAddress;
		EndpointAddress? _endpointAddress;

		public Binding?  Binding { get; private set; }

		#endregion

		#region Overrides

		protected override ILinqClient GetClient()
		{
			if (Binding != null)
				return new LinqSoapServiceClient(Binding, _endpointAddress!);

			if (_endpointAddress != null)
				return new LinqSoapServiceClient(_endpointConfigurationName!, _endpointAddress);

			if (_remoteAddress != null)
				return new LinqSoapServiceClient(_endpointConfigurationName!, _remoteAddress);

			return new LinqSoapServiceClient(_endpointConfigurationName!);
		}

		protected override IDataContext Clone()
		{
			return new SoapDataContext
			{
				MappingSchema              = MappingSchema,
				Configuration              = Configuration,
				Binding                    = Binding,
				_endpointConfigurationName = _endpointConfigurationName,
				_remoteAddress             = _remoteAddress,
				_endpointAddress           = _endpointAddress,
			};
		}

		protected override string ContextIDPrefix => "LinqSoapService";

		#endregion
	}
}
