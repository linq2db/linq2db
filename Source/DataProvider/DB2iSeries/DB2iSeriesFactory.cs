using System.Collections.Specialized;
namespace LinqToDB.DataProvider.DB2iSeries {

  public class DB2iSeriesFactory : IDataProviderFactory {
    public IDataProvider GetDataProvider(NameValueCollection attributes) {
      return new DB2iSeriesDataProvider();
    }
  }
}