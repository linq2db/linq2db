using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

#if !NOASYNC
using System.Threading.Tasks;
#endif

namespace LinqToDB.ServiceModel
{
	class LinqServiceClient : ClientBase<ILinqClient>, ILinqClient, IDisposable
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

#if !NOASYNC

		public Task<LinqServiceInfo> GetInfoAsync(string configuration)
		{
			return Channel.GetInfoAsync(configuration);
		}

		public Task<int> ExecuteNonQueryAsync(string configuration, string queryData)
		{
			return Channel.ExecuteNonQueryAsync(configuration, queryData);
		}

		public Task<object> ExecuteScalarAsync(string configuration, string queryData)
		{
			return Channel.ExecuteScalarAsync(configuration, queryData);
		}

		public Task<string> ExecuteReaderAsync(string configuration, string queryData)
		{
			return Channel.ExecuteReaderAsync(configuration, queryData);
		}

		public Task<int> ExecuteBatchAsync(string configuration, string queryData)
		{
			return Channel.ExecuteBatchAsync(configuration, queryData);
		}

#endif

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
