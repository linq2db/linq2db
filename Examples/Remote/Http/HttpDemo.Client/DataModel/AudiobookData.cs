using System;

using LinqToDB;
using LinqToDB.Remote.Http.Client;

namespace HttpDemo.Client.DataModel
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
