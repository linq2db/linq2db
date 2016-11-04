using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider {
  public class TypeCreator : TypeCreatorBase {
    Func<object> _creator;

    public dynamic CreateInstance() {
      if (_creator == null) {
        var expr = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(Type), typeof(object)));
        _creator = expr.Compile();
      }
      return _creator();
    }
  }

  public class TypeCreator<T> : TypeCreator {
    Func<T, object> _creator;

    public dynamic CreateInstance(T value) {
      return (_creator ?? (_creator = GetCreator<T>()))(value);
    }
  }

  public class TypeCreator<T1, T> : TypeCreator<T1> {
    Func<T, object> _creator;

    public dynamic CreateInstance(T value) {
      return (_creator ?? (_creator = GetCreator<T>()))(value);
    }
  }

  public class TypeCreator<T1, T2, T> : TypeCreator<T1, T2> {
    Func<T, object> _paramCreator;

    public dynamic CreateInstance(T value) {
      return (_paramCreator ?? (_paramCreator = GetCreator<T>()))(value);
    }
  }

}
