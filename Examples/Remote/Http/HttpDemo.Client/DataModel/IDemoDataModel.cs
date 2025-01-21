using System;

using LinqToDB;

namespace HttpDemo.Client.DataModel
{
	public interface IDemoDataModel : IDataContext
	{
		DemoDataModel Model { get; }
	}
}
