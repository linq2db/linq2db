using System;
using System.ServiceModel;

namespace LinqToDB.ServiceModel
{
	class LinqServiceClient : ClientBase<Async.ILinqService>, ILinqService, IDisposable
	{
		#region Init

		public LinqServiceClient(string endpointConfigurationName)                                            : base(endpointConfigurationName) { }
		public LinqServiceClient(string endpointConfigurationName, string remoteAddress)                      : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress)             : base(endpointConfigurationName, remoteAddress) { }
		public LinqServiceClient(System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }

		#endregion

		#region ILinqService Members

		public LinqServiceInfo GetInfo(string configuration)
		{
			var async = Channel.BeginGetInfo(configuration, null, null);
			return Channel.EndGetInfo(async);
		}

		public int ExecuteNonQuery(string configuration, string queryData)
		{
			var async = Channel.BeginExecuteNonQuery(configuration, queryData, null, null);
			return Channel.EndExecuteNonQuery(async);
		}

		public object ExecuteScalar(string configuration, string queryData)
		{
			var async = Channel.BeginExecuteScalar(configuration, queryData, null, null);
			return Channel.EndExecuteScalar(async);
		}

		public string ExecuteReader(string configuration, string queryData)
		{
			var async = Channel.BeginExecuteReader(configuration, queryData, null, null);
			return Channel.EndExecuteReader(async);
		}

		public int ExecuteBatch(string configuration, string queryData)
		{
			var async = Channel.BeginExecuteBatch(configuration, queryData, null, null);
			return Channel.EndExecuteBatch(async);
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

		#region Overrides

		protected override Async.ILinqService CreateChannel()
		{
			return new LinqServiceClientChannel(this);
		}

		#endregion

		#region Channel

		class LinqServiceClientChannel : ChannelBase<Async.ILinqService>, Async.ILinqService
		{
			public LinqServiceClientChannel(ClientBase<Async.ILinqService> client) :
				base(client)
			{
			}

			public IAsyncResult BeginGetInfo(string configuration, AsyncCallback callback, object asyncState)
			{
				return BeginInvoke("GetInfo", new object[] { configuration }, callback, asyncState);
			}

			public LinqServiceInfo EndGetInfo(IAsyncResult result)
			{
				return (LinqServiceInfo)EndInvoke("GetInfo", new object[0], result);
			}

			public IAsyncResult BeginExecuteNonQuery(string configuration, string queryData, AsyncCallback callback, object asyncState)
			{
				return BeginInvoke("ExecuteNonQuery", new object[] { configuration, queryData }, callback, asyncState);
			}

			public int EndExecuteNonQuery(IAsyncResult result)
			{
				return (int)EndInvoke("ExecuteNonQuery", new object[0], result);
			}

			public IAsyncResult BeginExecuteScalar(string configuration, string queryData, AsyncCallback callback, object asyncState)
			{
				return BeginInvoke("ExecuteScalar", new object[] { configuration, queryData }, callback, asyncState);
			}

			public object EndExecuteScalar(IAsyncResult result)
			{
				return EndInvoke("ExecuteScalar", new object[0], result);
			}

			public IAsyncResult BeginExecuteReader(string configuration, string queryData, AsyncCallback callback, object asyncState)
			{
				return BeginInvoke("ExecuteReader", new object[] { configuration, queryData }, callback, asyncState);
			}

			public string EndExecuteReader(IAsyncResult result)
			{
				return (string)EndInvoke("ExecuteReader", new object[0], result);
			}

			public IAsyncResult BeginExecuteBatch(string configuration, string queryData, AsyncCallback callback, object asyncState)
			{
				return BeginInvoke("ExecuteBatch", new object[] { configuration, queryData }, callback, asyncState);
			}

			public int EndExecuteBatch(IAsyncResult result)
			{
				return (int)EndInvoke("ExecuteBatch", new object[0], result);
			}
		}

		#endregion
	}
}
