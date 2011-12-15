using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;

namespace LinqToDB.Reflection
{
	class ExprTypeAccessor<T> : TypeAccessor
	{
		static ExprTypeAccessor()
		{
			// Create Instance.
			//
			var type = typeof(T);

			if (type.IsValueType)
			{
				var body = Expression.Constant(default(T));
				_createInstance = Expression.Lambda<Func<T>>(body).Compile();
			}
			else
			{
				var ctor = type.IsAbstract ?
					null :
					type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);

				if (ctor == null)
				{
					Expression<Func<T>> mi;

					if (type.IsAbstract) mi = () => ThrowAbstractException();
					else                 mi = () => ThrowException();

					var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

					_createInstance = Expression.Lambda<Func<T>>(body).Compile();
				}
				else
				{
					_createInstance = Expression.Lambda<Func<T>>(Expression.New(ctor)).Compile();
				}
			}

			// Add fields.
			//
			foreach (var fi in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
				_members.Add(fi);

			foreach (var pi in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
				if (pi.GetIndexParameters().Length == 0)
					_members.Add(pi);

			// ObjectFactory
			//
			var attr = ReflectionExtensions.GetFirstAttribute(type, typeof(ObjectFactoryAttribute));

			if (attr != null)
				_objectFactory = ((ObjectFactoryAttribute)attr).ObjectFactory;
		}

		static T ThrowException()
		{
			throw new LinqToDBException(string.Format("The '{0}' type must have default or init constructor.", typeof(T).FullName));
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException(string.Format("Cant create an instance of abstract class '{0}'.", typeof(T).FullName));
		}

		static readonly List<MemberInfo> _members = new List<MemberInfo>();
		static readonly IObjectFactory   _objectFactory;

		public ExprTypeAccessor()
		{
			foreach (var member in _members)
				AddMember(ExprMemberAccessor.GetMemberAccessor(this, member.Name));

			ObjectFactory = _objectFactory;
		}

		static readonly Func<T> _createInstance;
		public override object   CreateInstance()
		{
			return _createInstance();
		}

		public override Type Type { get { return typeof(T); } }
	}
}
