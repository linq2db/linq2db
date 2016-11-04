using System;
using System.Data;

namespace LinqToDB.DataProvider.DB2iSeries {
  using Data;

  public class DB2iSeriesTypeCreator<T> : TypeCreatorNoDefault<T> {
    private Func<IDbConnection, object> _creator;
    public object CreateInstance(DataConnection value) {
      if (_creator == null) {
        _creator = GetCreator<IDbConnection>(DB2iSeriesTypes.ConnectionType);
      }
      return _creator(value.Connection);
    }

  }
}