using System;

using LinqToDB.Mapping;

namespace SignalRClient.DataModel
{
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
