using System;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider {

  public abstract class TypeCreatorBase {
    public Type Type;

    protected Func<T, object> GetCreator<T>() {
      var ctor = Type.GetConstructor(new[] { typeof(T) });
      var parm = Expression.Parameter(typeof(T));
      var expr = Expression.Lambda<Func<T, object>>(
        Expression.Convert(Expression.New(ctor, parm), typeof(object)),
        parm);

      return expr.Compile();
    }

    protected Func<T, object> GetCreator<T>(Type paramType) {
      var ctor = Type.GetConstructor(new[] { paramType });
      var parm = Expression.Parameter(typeof(T));
      var expr = Expression.Lambda<Func<T, object>>(
        Expression.Convert(Expression.New(ctor, Expression.Convert(parm, paramType)), typeof(object)),
        parm);

      return expr.Compile();
    }

    public static implicit operator Type(TypeCreatorBase typeCreator) {
      return typeCreator.Type;
    }

    public bool IsSupported { get { return Type != null; } }
  }

}