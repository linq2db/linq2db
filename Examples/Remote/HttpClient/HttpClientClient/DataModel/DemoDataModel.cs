using LinqToDB;

namespace HttpClientClient.DataModel
{
	public class DemoDataModel(IDataContext dataContext)
	{
		public ITable<WeatherForecast>  WeatherForecasts => dataContext.GetTable<WeatherForecast>();
	}
}
