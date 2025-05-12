using System;

using LinqToDB;

namespace HttpClientClient.DataModel
{
	public interface IDemoDataModel : IDataContext
	{
		DemoDataModel Model { get; }
	}
}
