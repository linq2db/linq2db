using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Extensions;

	public class TypeAccessor<T> : TypeAccessor
	{
		static TypeAccessor()
		{
			// Create Instance.
			//
			var type = typeof(T);

			if (type.IsValueTypeEx())
			{
				_createInstance = () => default;
			}
			else
			{
				var ctor = type.IsAbstractEx() ? null : type.GetDefaultConstructorEx();

				if (ctor == null)
				{
					Expression<Func<T>> mi;

					if (type.IsAbstractEx()) mi = () => ThrowAbstractException();
					else                     mi = () => ThrowException();

					var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

					_createInstance = Expression.Lambda<Func<T>>(body).Compile();
				}
				else
				{
					_createInstance = Expression.Lambda<Func<T>>(Expression.New(ctor)).Compile();
				}
			}

			foreach (var memberInfo in type.GetPublicInstanceMembersEx())
			{
				if (memberInfo.IsFieldEx() || memberInfo.IsPropertyEx() && ((PropertyInfo)memberInfo).GetIndexParameters().Length == 0)
					_members.Add(memberInfo);
			}

			// Add explicit interface implementation properties support
			// Or maybe we should support all private fields/properties?
			//
			var interfaceMethods = type.GetInterfacesEx().SelectMany(ti => type.GetInterfaceMapEx(ti).TargetMethods).ToList();

			if (interfaceMethods.Count > 0)
			{
				foreach (var pi in type.GetNonPublicPropertiesEx())
				{
					if (pi.GetIndexParameters().Length == 0)
					{
						var getMethod = pi.GetGetMethodEx(true);
						var setMethod = pi.GetSetMethodEx(true);

						if ((getMethod == null || interfaceMethods.Contains(getMethod)) &&
							(setMethod == null || interfaceMethods.Contains(setMethod)))
						{
							_members.Add(pi);
						}
					}
				}
			}

			// ObjectFactory
			//
			var attr = type.GetFirstAttribute<ObjectFactoryAttribute>();

			if (attr != null)
				_objectFactory = attr.ObjectFactory;
		}

		static T ThrowException()
		{
			throw new LinqToDBException($"The '{typeof(T).FullName}' type must have default or init constructor.");
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException($"Cant create an instance of abstract class '{typeof(T).FullName}'.");
		}

		static readonly List<MemberInfo> _members = new List<MemberInfo>();
		static readonly IObjectFactory   _objectFactory;

		internal TypeAccessor()
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
