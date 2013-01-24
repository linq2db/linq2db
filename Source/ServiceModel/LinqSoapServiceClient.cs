using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LinqToDB.SqlProvider;

namespace LinqToDB.ServiceModel
{
	class LinqSoapServiceClient : ClientBase<ILinqSoapService>, ILinqService, IDisposable
	{
		#region Init

		public LinqSoapServiceClient(string endpointConfigurationName)                                                                  : base(endpointConfigurationName) { }
		public LinqSoapServiceClient(string endpointConfigurationName, string remoteAddress)                                            : base(endpointConfigurationName, remoteAddress) { }
		public LinqSoapServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress)                                   : base(endpointConfigurationName, remoteAddress) { }
		public LinqSoapServiceClient(Binding binding, EndpointAddress remoteAddress)                                                    : base(binding, remoteAddress) { }

		#endregion

		#region ILinqService Members

		public string GetSqlProviderType()
		{
			return Channel.GetSqlProviderType();
		}

		public SqlProviderFlags GetSqlProviderFlags()
		{
			return Channel.GetSqlProviderFlags();
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
