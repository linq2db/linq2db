using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Extensions;

	public class TypeAccessor<T> : TypeAccessor
	{
		static TypeAccessor()
		{
			var type = typeof(T);

			_members.AddRange(type.GetPublicInstanceValueMembers());

			// Add explicit interface implementation properties support
			// Or maybe we should support all private fields/properties?
			//
			if (!type.IsInterface && !type.IsArray)
			{
				var interfaceMethods = type.GetInterfaces().SelectMany(ti => type.GetInterfaceMapEx(ti).TargetMethods)
					.ToList();

				if (interfaceMethods.Count > 0)
				{
					foreach (var pi in type.GetNonPublicPropertiesEx())
					{
						if (pi.GetIndexParameters().Length == 0)
						{
							var getMethod = pi.GetGetMethod(true);
							var setMethod = pi.GetSetMethod(true);

							if ((getMethod == null || interfaceMethods.Contains(getMethod)) &&
								(setMethod == null || interfaceMethods.Contains(setMethod)))
							{
								_members.Add(pi);
							}
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

		static readonly List<MemberInfo> _members = new();
		static readonly IObjectFactory?  _objectFactory;

		internal TypeAccessor()
		{
			// init members
			foreach (var member in _members)
				if (!member.GetMemberType().IsByRef)
					AddMember(new MemberAccessor(this, member, null));
		}

		public override object CreateInstance()
		{
			if (_objectFactory != null)
				return _objectFactory.CreateInstance(this);
			return ObjectFactory<T>.CreateInstance()!;
		}

		public T Create()
		{
			if (_objectFactory != null)
				return (T)_objectFactory.CreateInstance(this);
			return ObjectFactory<T>.CreateInstance()!;
		}

		public override Type Type => typeof(T);
	}
}
