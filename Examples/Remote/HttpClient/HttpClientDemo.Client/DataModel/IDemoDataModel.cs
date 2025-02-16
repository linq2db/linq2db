using System;

using LinqToDB;

namespace HttpClientDemo.Client.DataModel
{
	public interface IDemoDataModel : IDataContext
	{
		DemoDataModel Model { get; }
	}
}
