using System;

using LinqToDB;
using LinqToDB.Remote.SignalR;

namespace SignalRClient.DataModel
{
	public class DemoClientData : SignalRDataContext, IDemoDataModel
	{
		public DemoClientData(SignalRLinqServiceClient client, Func<DataOptions,DataOptions>? optionBuilder = null)
			: base(client, optionBuilder)
		{
			Model = new DemoDataModel(this);
		}

		public DemoDataModel Model { get; }
	}
}
