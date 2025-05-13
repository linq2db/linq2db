using System;

using LinqToDB;

namespace SignalRClient.DataModel
{
	public interface IDemoDataModel : IDataContext
	{
		DemoDataModel Model { get; }
	}
}
