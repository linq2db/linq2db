using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace HttpDemo.Client.DataModel
{
	public class DemoDataModel
	{
		public ITable<WeatherForecast>  WeatherForecasts  { get { return _dataContext.GetTable<WeatherForecast>(); } }

		public DemoDataModel(IDataContext dataContext)
		{
			_dataContext = dataContext;
		}

		readonly IDataContext _dataContext;
	}

	[Table(Name="WeatherForecast")]
	public class WeatherForecast
	{
		[PrimaryKey, Identity   ] public int       WeatherForecastID { get; set; }
		[Column,     NotNull    ] public DateOnly  Date              { get; set; }
		[Column,     NotNull    ] public int       TemperatureC      { get; set; }
		[Column,        Nullable] public string?   Summary           { get; set; }

		[NotColumn              ] public int       TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	}
}
