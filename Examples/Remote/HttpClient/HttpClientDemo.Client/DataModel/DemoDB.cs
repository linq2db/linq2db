using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;

namespace HttpClientDemo.Client.DataModel
{
	public class DemoDB : DataConnection, IDemoDataModel
	{
		static readonly DataOptions _dataOptions = new DataOptions()
			.UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared", SQLiteProvider.Microsoft)
			;

		public DemoDB() : this(_dataOptions)
		{
		}

		public DemoDB(DataOptions dataOptions) : base(dataOptions)
		{
			Model = new DemoDataModel(this);
		}

		public DemoDataModel Model { get; }
	}
}
