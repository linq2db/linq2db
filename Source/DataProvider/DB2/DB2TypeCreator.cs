using System;
using System.Data;

namespace LinqToDB.DataProvider.DB2 {
  using Data;

  public class DB2TypeCreator<T> : TypeCreatorNoDefault<T> {
    Func<IDbConnection, object> _creator;

    public dynamic CreateInstance(DataConnection value) {
      return (_creator ?? (_creator = GetCreator<IDbConnection>(DB2Types.ConnectionType)))(value.Connection);
    }
  }

}