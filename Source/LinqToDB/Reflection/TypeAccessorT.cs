using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Reflection
{
	using Extensions;
	using LinqToDB.Common;

	public class TypeAccessor<T> : TypeAccessor
	{
		static TypeAccessor()
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

					if (type.IsAbstract) mi = () => ThrowAbstractException();
					else                 mi = () => ThrowException();

					var body = Expression.Call(null, ((MethodCallExpression)mi.Body).Method);

					_createInstance = Expression.Lambda<Func<T>>(body).CompileExpression();
				}
				else
				{
					_createInstance = Expression.Lambda<Func<T>>(Expression.New(ctor)).CompileExpression();
				}
			}

			var interfaces = !type.IsInterface && !type.IsArray ? type.GetInterfaces() : Array<Type>.Empty;

			if (interfaces.Length == 0)
			{
				_members.AddRange(type.GetPublicInstanceValueMembers());
			}
			else
			{
				// load properties takin into account explicit interface implementations
				var interfacePropertiesList = new List<PropertyInfo>();
				var interfaceProperties     = new HashSet<(Type? declaringType, string name, Type type)>();

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
								interfacePropertiesList.Add(pi);
								interfaceProperties.Add((pi.DeclaringType, pi.Name, pi.PropertyType));
							}
						}
					}

					if (implementor.BaseType == null || implementor.BaseType == typeof(object) || implementor.BaseType == typeof(ValueType))
						break;

					implementor = implementor.BaseType;
				}

				foreach (var mi in type.GetPublicInstanceValueMembers())
					if (mi is not PropertyInfo pi || !interfaceProperties.Contains((pi.DeclaringType, pi.Name, pi.PropertyType)))
						_members.Add(mi);

				_members.AddRange(interfacePropertiesList);
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

		static T ThrowException()
		{
			throw new LinqToDBException($"The '{typeof(T).FullName}' type must have default or init constructor.");
		}

		static T ThrowAbstractException()
		{
			throw new LinqToDBException($"Cant create an instance of abstract class '{typeof(T).FullName}'.");
		}

		static readonly List<MemberInfo> _members = new();
		static readonly IObjectFactory?  _objectFactory;

		internal TypeAccessor()
		{
			// init members
			foreach (var member in _members)
				if (!member.GetMemberType().IsByRef)
					AddMember(new MemberAccessor(this, member, null));

			ObjectFactory = _objectFactory;
		}

		static readonly Func<T> _createInstance;
		public override object   CreateInstance()
		{
			return _createInstance()!;
		}

		public T Create()
		{
			return _createInstance();
		}

		public override Type Type => typeof(T);
	}
}
