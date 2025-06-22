using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace HttpClientClient.DataModel
{
	public class DemoDataModel(IDataContext dataContext)
	{
		public ITable<WeatherForecast>  WeatherForecasts => dataContext.GetTable<WeatherForecast>();
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
