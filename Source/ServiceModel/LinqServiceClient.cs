using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace LinqToDB.ServiceModel
{
	class LinqServiceClient : ClientBase<ILinqService>, ILinqService, IDisposable
	{
		#region Init

		//public LinqServiceClient() {}
		public LinqServiceClient(string endpointConfigurationName)                                                                  : base(endpointConfigurationName) { }
		public LinqServiceClient(string endpointConfigurationName, string remoteAddress)                                            : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress)                                   : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(Binding binding, EndpointAddress remoteAddress)                                                    : base(binding, remoteAddress) { }
		//public LinqServiceClient(InstanceContext callbackInstance)                                                                  : base(callbackInstance) { }
		//public LinqServiceClient(InstanceContext callbackInstance, string endpointConfigurationName)                                : base(callbackInstance, endpointConfigurationName) { }
		//public LinqServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress)          : base(callbackInstance, endpointConfigurationName, remoteAddress) { }
		//public LinqServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress) : base(callbackInstance, endpointConfigurationName, remoteAddress) { }
		//public LinqServiceClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)                  : base(callbackInstance, binding, remoteAddress) { }

		#endregion

		#region ILinqService Members

		public string GetSqlProviderType()
		{
			return Channel.GetSqlProviderType();
		}

		public int ExecuteNonQuery(string queryData)
		{
			return Channel.ExecuteNonQuery(queryData);
		}

		public object ExecuteScalar(string queryData)
		{
			return Channel.ExecuteScalar(queryData);
		}

		public string ExecuteReader(string queryData)
		{
			return Channel.ExecuteReader(queryData);
		}

		public int ExecuteBatch(string queryData)
		{
			return Channel.ExecuteBatch(queryData);
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
