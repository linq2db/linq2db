using System;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Reflection
{
	public static class ObjectFactory<T>
	{
		static readonly Func<T> _createInstance = BuildCreateInstance();

		static T ThrowException()
		{
			throw new LinqToDBException($"The '{typeof(T).FullName}' type must have default or init constructor.");
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException($"Cant create an instance of abstract class '{typeof(T).FullName}'.");
		}

		static Func<T> BuildCreateInstance()
		{
			// Create Instance.
			//
			var type = typeof(T);

			if (type.IsValueType)
				return () => default!;

			var ctor = type.IsAbstract ? null : type.GetDefaultConstructorEx();

			if (ctor is not null)
				return Expression.Lambda<Func<T>>(Expression.New(ctor)).CompileExpression();

			Expression<Func<T>> mi = type.IsAbstract
				? () => ThrowAbstractException()
				: () => ThrowException();

			var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

			return Expression.Lambda<Func<T>>(body).CompileExpression();
		}

		public static T CreateInstance()
		{
			return _createInstance();
		}
	}
}
