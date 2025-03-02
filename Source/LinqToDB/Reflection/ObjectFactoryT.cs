using System;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Reflection
{
	public static class ObjectFactory<T>
	{
		static readonly Func<T>         _createInstance;

		static T ThrowException()
		{
			throw new LinqToDBException($"The '{typeof(T).FullName}' type must have default or init constructor.");
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException($"Cant create an instance of abstract class '{typeof(T).FullName}'.");
		}

		static ObjectFactory()
		{
			// Create Instance.
			//
			var type = typeof(T);

			if (type.IsValueType)
			{
				_createInstance = () => default!;
			}
			else
			{
				var ctor = type.IsAbstract ? null : type.GetDefaultConstructorEx();

				if (ctor == null)
				{
					Expression<Func<T>> mi;

					if (type.IsAbstract) mi     = () => ThrowAbstractException();
					else                     mi = () => ThrowException();

					var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

					_createInstance = Expression.Lambda<Func<T>>(body).CompileExpression();
				}
				else
				{
					_createInstance = Expression.Lambda<Func<T>>(Expression.New(ctor)).CompileExpression();
				}
			}
		}

		public static T CreateInstance()
		{
			return _createInstance();
		}
	}
}
