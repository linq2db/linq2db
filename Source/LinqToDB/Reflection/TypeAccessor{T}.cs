using System;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Reflection
{
	public class TypeAccessor<T> : TypeAccessor
	{
		static TypeAccessor()
		{
			var type = typeof(T);

			var interfaces = !type.IsInterface && !type.IsArray ? type.GetInterfaces() : [];

			if (interfaces.Length == 0)
			{
				_members.AddRange(type.GetPublicInstanceValueMembers());
			}
			else
			{
				// load properties takin into account explicit interface implementations
				var interfacePropertiesList = new List<PropertyInfo?>();
				var interfaceProperties     = new Dictionary<(Type? declaringType, string name, Type type), int>();

				// as interface could be re-implemented multiple times, we should track which inteface property accessors
				// are not mapped yet and reduce this list walking inheritance hierarchy from top to base type
				HashSet<MethodInfo>? unmappedAccessors = null;

				// fill unmappedAccessors with accessors, except those from public properties
				foreach (var iface in type.GetInterfaces())
				{
					var map = type.GetInterfaceMapEx(iface);

					for (var i = 0; i < map.InterfaceMethods.Length; i++)
					{
						if (!map.InterfaceMethods[i].IsStatic)
							(unmappedAccessors ??= new()).Add(map.InterfaceMethods[i]);
					}
				}

				// go down in hierarchy and pick first found explicit implementation for interface properties
				var implementor = type;
				while (unmappedAccessors != null && unmappedAccessors.Count > 0)
				{
					Dictionary<MethodInfo, List<MethodInfo>>? methods = null;

					foreach (var iface in implementor.GetInterfaces())
					{
						var map = implementor.GetInterfaceMapEx(iface);

						for (var i = 0; i < map.InterfaceMethods.Length; i++)
						{
							if (map.InterfaceMethods[i].IsStatic)
								continue;

							methods ??= new();
							if (methods.TryGetValue(map.TargetMethods[i], out var interfaceMethods))
								interfaceMethods.Add(map.InterfaceMethods[i]);
							else
								methods.Add(map.TargetMethods[i], new List<MethodInfo>() { map.InterfaceMethods[i] });
						}
					}

					if (methods != null)
					{
						foreach (var pi in implementor.GetDeclaredPropertiesEx())
						{
							if ((pi.GetMethod == null || (methods.TryGetValue(pi.GetMethod, out var ifaceGetters) && RemoveAll(unmappedAccessors, ifaceGetters))) &&
								(pi.SetMethod == null || (methods.TryGetValue(pi.SetMethod, out var ifaceSetters) && RemoveAll(unmappedAccessors, ifaceSetters))))
							{
								interfaceProperties.Add((pi.DeclaringType, pi.Name, pi.PropertyType), interfacePropertiesList.Count);
								interfacePropertiesList.Add(pi);
							}
						}
					}

					if (implementor.BaseType == null || implementor.BaseType == typeof(object) || implementor.BaseType == typeof(ValueType))
						break;

					implementor = implementor.BaseType;
				}

				var uniqueNames = new HashSet<string>();
				foreach (var mi in type.GetPublicInstanceValueMembers())
				{
					if (mi is PropertyInfo pi && interfaceProperties.TryGetValue((pi.DeclaringType, pi.Name, pi.PropertyType), out var idx))
						interfacePropertiesList[idx] = null;

					if (uniqueNames.Add(mi.Name))
						_members.Add(mi);
				}

				foreach (var pi in interfacePropertiesList)
					if (pi != null && uniqueNames.Add(pi.Name))
						_members.Add(pi);
			}

			// ObjectFactory
			//
			var attr = type.GetAttribute<ObjectFactoryAttribute>();

			if (attr != null)
				_objectFactory = attr.ObjectFactory;

			static bool RemoveAll(HashSet<MethodInfo> unmappedAccessors, List<MethodInfo> ifaceAccessors)
			{
				var removed = true;
				foreach (var accessor in ifaceAccessors)
					removed = unmappedAccessors.Remove(accessor) && removed;

				return removed;
			}
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
