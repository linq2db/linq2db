using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LinqToDB.ServiceModel
{
	class LinqSoapServiceClient : ClientBase<ILinqSoapService>, ILinqService, IDisposable
	{
		#region Init

		public LinqSoapServiceClient(string endpointConfigurationName)                                : base(endpointConfigurationName) { }
		public LinqSoapServiceClient(string endpointConfigurationName, string remoteAddress)          : base(endpointConfigurationName, remoteAddress) { }
		public LinqSoapServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
		public LinqSoapServiceClient(Binding binding, EndpointAddress remoteAddress)                  : base(binding, remoteAddress) { }

		#endregion

		#region ILinqService Members

		public LinqServiceInfo GetInfo(string configuration)
		{
			return Channel.GetInfo(configuration);
		}

		public int ExecuteNonQuery(string configuration, string queryData)
		{
			return Channel.ExecuteNonQuery(configuration, queryData);
		}

		public object ExecuteScalar(string configuration, string queryData)
		{
			return Channel.ExecuteScalar(configuration, queryData);
		}

		public string ExecuteReader(string configuration, string queryData)
		{
			return Channel.ExecuteReader(configuration, queryData);
		}

		public int ExecuteBatch(string configuration, string queryData)
		{
			return Channel.ExecuteBatch(configuration, queryData);
		}

		#endregion

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			try
			{
				if (State != CommunicationState.Faulted)
					((ICommunicationObject)this).Close();
				else
					Abort();
			}
			catch (CommunicationException)
			{
				Abort();
			}
			catch (TimeoutException)
			{
				Abort();
			}
			catch (Exception)
			{
				Abort();
				throw;
			}
		}

		#endregion
	}
}
