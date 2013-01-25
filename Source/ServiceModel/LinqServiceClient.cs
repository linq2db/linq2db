using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LinqToDB.ServiceModel
{
	class LinqServiceClient : ClientBase<ILinqService>, ILinqService, IDisposable
	{
		#region Init

		public LinqServiceClient(string endpointConfigurationName)                                : base(endpointConfigurationName) { }
		public LinqServiceClient(string endpointConfigurationName, string remoteAddress)          : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(Binding binding, EndpointAddress remoteAddress)                  : base(binding, remoteAddress) { }

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
