using System;

using LinqToDB;
using LinqToDB.Remote.HttpClient.Client;

namespace HttpClientClient.DataModel
{
	public class DemoClientData : HttpClientDataContext, IDemoDataModel
	{
		public DemoClientData(HttpClientLinqServiceClient client, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(client, optionBuilder)
		{
			Model = new DemoDataModel(this);
		}

		public DemoDataModel Model { get; }
	}
}
