using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Common;
	using Extensions;

	public class TypeAccessor<T> : TypeAccessor
	{
		static TypeAccessor()
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

			foreach (var memberInfo in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
			{
				if (memberInfo.MemberType == MemberTypes.Field ||
					memberInfo.MemberType == MemberTypes.Property && ((PropertyInfo)memberInfo).GetIndexParameters().Length == 0)
					_members.Add(memberInfo);
			}

			// ObjectFactory
			//
			var attr = type.GetFirstAttribute<ObjectFactoryAttribute>();

			if (attr != null)
				_objectFactory = attr.ObjectFactory;
		}

		static T ThrowException()
		{
			throw new LinqToDBException("The '{0}' type must have default or init constructor.".Args(typeof(T).FullName));
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException("Cant create an instance of abstract class '{0}'.".Args(typeof(T).FullName));
		}

		static readonly List<MemberInfo> _members = new List<MemberInfo>();
		static readonly IObjectFactory   _objectFactory;

		public TypeAccessor()
		{
			foreach (var member in _members)
				AddMember(new MemberAccessor(this, member));

			ObjectFactory = _objectFactory;
		}

		static readonly Func<T> _createInstance;
		public override object   CreateInstance()
		{
			return _createInstance();
		}

		public T Create()
		{
			return _createInstance();
		}

		public override Type Type { get { return typeof(T); } }
	}
}
