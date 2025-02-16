using System;

using LinqToDB;

namespace SignalRDemo.Client.DataModel
{
	public interface IDemoDataModel : IDataContext
	{
		DemoDataModel Model { get; }
	}
}
