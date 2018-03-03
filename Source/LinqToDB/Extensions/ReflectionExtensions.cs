using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using JetBrains.Annotations;

namespace LinqToDB.Extensions
{
	public static class ReflectionExtensions
	{
		#region Type extensions

		public static bool IsGenericTypeEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsGenericType;
#else
			return type.IsGenericType;
#endif
		}

		public static bool IsValueTypeEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsValueType;
#else
			return type.IsValueType;
#endif
		}

		public static bool IsAbstractEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsAbstract;
#else
			return type.IsAbstract;
#endif
		}

		public static bool IsPublicEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsPublic;
#else
			return type.IsPublic;
#endif
		}

		public static bool IsClassEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsClass;
#else
			return type.IsClass;
#endif
		}

		public static bool IsEnumEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsEnum;
#else
			return type.IsEnum;
#endif
		}

		public static bool IsPrimitiveEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsPrimitive;
#else
			return type.IsPrimitive;
#endif
		}

		public static bool IsInterfaceEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsInterface;
#else
			return type.IsInterface;
#endif
		}

		public static Type BaseTypeEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().BaseType;
#else
			return type.BaseType;
#endif
		}

		public static Type[] GetInterfacesEx(this Type type)
		{
			return type.GetInterfaces();
		}

		public static object[] GetCustomAttributesEx(this Type type, Type attributeType, bool inherit)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit).Cast<object>().ToArray();
#else
			return type.GetCustomAttributes(attributeType, inherit);
#endif
		}

		public static MemberInfo[] GetPublicInstanceMembersEx(this Type type)
		{
			return type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
		}

		public static MemberInfo[] GetStaticMembersEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		}

		/// <summary>
		/// Returns <see cref="MemberInfo"/> of <paramref name="type"/> described by <paramref name="memberInfo"/>
		/// It us useful when member's declared and reflected types are not the same
		/// </summary>
		/// <remarks>This method searches only properties, fields and methods</remarks>
		/// <param name="type"><see cref="Type"/> to find member info</param>
		/// <param name="memberInfo"><see cref="MemberInfo"/> </param>
		/// <returns><see cref="MemberInfo"/> or null</returns>
		public static MemberInfo GetMemberEx(this Type type, MemberInfo memberInfo)
		{
			if (memberInfo.IsPropertyEx())
				return type.GetPropertyEx(memberInfo.Name);

			if (memberInfo.IsFieldEx())
				return type.GetFieldEx   (memberInfo.Name);

			if (memberInfo.IsMethodEx())
				return type.GetMethodEx  (memberInfo.Name, ((MethodInfo) memberInfo).GetParameters().Select(_ => _.ParameterType).ToArray());

			return null;
		}

		public static MethodInfo GetMethodEx(this Type type, string name, params Type[] types)
		{
#if NETSTANDARD1_6
			// https://github.com/dotnet/corefx/issues/12921
			return type.GetMethodsEx().FirstOrDefault(mi =>
			{
				var res = mi.IsPublic && mi.Name == name;
				if (!res)
					return res;

				var pars = mi.GetParameters().Select(_ => _.ParameterType).ToArray();

				if (types.Length == 0 && pars.Length == 0)
					return true;

				if (pars.Length != types.Length)
					return false;

				for (var i = 0; i < types.Length; i++)
				{
					if (types[i] != pars[i])
						return false;
				}

				return true;
			});
#else
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
#endif
		}

		public static MethodInfo GetMethodEx(this Type type, string name)
		{
			return type.GetMethod(name);
		}
		public static ConstructorInfo GetDefaultConstructorEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(p => p.GetParameters().Length == 0);
#else
			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				Type.EmptyTypes,
				null);
#endif
		}

		public static TypeCode GetTypeCodeEx(this Type type)
		{
#if NETSTANDARD1_6
			if (type == typeof(DBNull))
				return (TypeCode)2;
#endif

			return Type.GetTypeCode(type);
		}

		public static bool IsAssignableFromEx(this Type type, Type c)
		{
			return type.IsAssignableFrom(c);
		}

		public static FieldInfo[] GetFieldsEx(this Type type)
		{
			return type.GetFields();
		}

		public static Type[] GetGenericArgumentsEx(this Type type)
		{
			return type.GetGenericArguments();
		}

		public static MethodInfo GetGetMethodEx(this PropertyInfo propertyInfo, bool nonPublic)
		{
			return propertyInfo.GetGetMethod(nonPublic);
		}

		public static MethodInfo GetGetMethodEx(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetGetMethod();
		}

		public static MethodInfo GetSetMethodEx(this PropertyInfo propertyInfo, bool nonPublic)
		{
			return propertyInfo.GetSetMethod(nonPublic);
		}

		public static MethodInfo GetSetMethodEx(this PropertyInfo propertyInfo)
		{
			return propertyInfo.GetSetMethod();
		}

		public static object[] GetCustomAttributesEx(this Type type, bool inherit)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().GetCustomAttributes(inherit).Cast<object>().ToArray();
#else
			return type.GetCustomAttributes(inherit);
#endif
		}

		public static InterfaceMapping GetInterfaceMapEx(this Type type, Type interfaceType)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().GetRuntimeInterfaceMap(interfaceType);
#else
			return type.GetInterfaceMap(interfaceType);
#endif
		}

		public static bool IsPropertyEx(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType == MemberTypes.Property;
		}

		public static bool IsFieldEx(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType == MemberTypes.Field;
		}

		public static bool IsMethodEx(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType == MemberTypes.Method;
		}

		public static object[] GetCustomAttributesEx(this MemberInfo memberInfo, Type attributeType, bool inherit)
		{
#if NETSTANDARD1_6
			return memberInfo.GetCustomAttributes(attributeType, inherit).Cast<object>().ToArray();
#else
			return memberInfo.GetCustomAttributes(attributeType, inherit);
#endif
		}

		public static bool IsSubclassOfEx(this Type type, Type c)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsSubclassOf(c);
#else
			return type.IsSubclassOf(c);
#endif
		}

		public static bool IsGenericTypeDefinitionEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().IsGenericTypeDefinition;
#else
			return type.IsGenericTypeDefinition;
#endif
		}

		public static PropertyInfo[] GetPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		public static PropertyInfo[] GetPropertiesEx(this Type type, BindingFlags flags)
		{
			return type.GetProperties(flags);
		}

		public static PropertyInfo[] GetNonPublicPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MethodInfo[] GetMethodsEx(this Type type)
		{
			return type.GetMethods();
		}

		public static Assembly AssemblyEx(this Type type)
		{
#if NETSTANDARD1_6
			return type.GetTypeInfo().Assembly;
#else
			return type.Assembly;
#endif
		}

		public static ConstructorInfo[] GetConstructorsEx(this Type type)
		{
			return type.GetConstructors();
		}

		public static ConstructorInfo GetConstructorEx(this Type type, Type[] parameterTypes)
		{
			return type.GetConstructor(parameterTypes);
		}

		public static PropertyInfo GetPropertyEx(this Type type, string propertyName)
		{
			return type.GetProperty(propertyName);
		}

		public static FieldInfo GetFieldEx(this Type type, string propertyName)
		{
			return type.GetField(propertyName);
		}

		public static Type ReflectedTypeEx(this MemberInfo memberInfo)
		{
#if NETSTANDARD1_6
			return memberInfo.DeclaringType;
#else
			return memberInfo.ReflectedType;
#endif
		}

		public static MemberInfo[] GetInstanceMemberEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MemberInfo[] GetPublicMemberEx(this Type type, string name)
		{
			return type.GetMember(name);
		}

		public static object[] GetCustomAttributesEx(this MemberInfo memberInfo, bool inherit)
		{
#if NETSTANDARD1_6
			return memberInfo.GetCustomAttributes(inherit).Cast<object>().ToArray();
#else
			return memberInfo.GetCustomAttributes(inherit);
#endif
		}

		public static object[] GetCustomAttributesEx(this ParameterInfo parameterInfo, bool inherit)
		{
#if NETSTANDARD1_6
			return parameterInfo.GetCustomAttributes(inherit).Cast<object>().ToArray();
#else
			return parameterInfo.GetCustomAttributes(inherit);
#endif
		}

		static class CacheHelper<T>
		{
			public static readonly ConcurrentDictionary<Type,T[]> TypeAttributes = new ConcurrentDictionary<Type,T[]>();
		}

		#region Attributes cache

		static readonly ConcurrentDictionary<Type, object[]> _typeAttributesTopInternal = new ConcurrentDictionary<Type, object[]>();

		static void GetAttributesInternal(List<object> list, Type type)
		{
			object[] attrs;

			if (_typeAttributesTopInternal.TryGetValue(type, out attrs))
			{
				list.AddRange(attrs);
			}
			else
			{
				GetAttributesTreeInternal(list, type);
				_typeAttributesTopInternal[type] = list.ToArray();
			}
		}

		static readonly ConcurrentDictionary<Type, object[]> _typeAttributesInternal = new ConcurrentDictionary<Type, object[]>();

		static void GetAttributesTreeInternal(List<object> list, Type type)
		{
			var attrs = _typeAttributesInternal.GetOrAdd(type, x => type.GetCustomAttributesEx(false));

			list.AddRange(attrs);

			if (type.IsInterfaceEx())
				return;

			// Reflection returns interfaces for the whole inheritance chain.
			// So, we are going to get some hemorrhoid here to restore the inheritance sequence.
			//
			var interfaces      = type.GetInterfacesEx();
			var nBaseInterfaces = type.BaseTypeEx() != null? type.BaseTypeEx().GetInterfacesEx().Length: 0;

			for (var i = 0; i < interfaces.Length; i++)
			{
				var intf = interfaces[i];

				if (i < nBaseInterfaces)
				{
					var getAttr = false;

					foreach (var mi in type.GetInterfaceMapEx(intf).TargetMethods)
					{
						// Check if the interface is reimplemented.
						//
						if (mi.DeclaringType == type)
						{
							getAttr = true;
							break;
						}
					}

					if (getAttr == false)
						continue;
				}

				GetAttributesTreeInternal(list, intf);
			}

			if (type.BaseTypeEx() != null && type.BaseTypeEx() != typeof(object))
				GetAttributesTreeInternal(list, type.BaseTypeEx());
		}

		#endregion

		/// <summary>
		/// Returns an array of custom attributes applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>An array of custom attributes applied to this type,
		/// or an array with zero (0) elements if no attributes have been applied.</returns>
		public static T[] GetAttributes<T>([NotNull] this Type type)
			where T : Attribute
		{
			if (type == null) throw new ArgumentNullException("type");

			T[] attrs;

			if (!CacheHelper<T>.TypeAttributes.TryGetValue(type, out attrs))
			{
				var list = new List<object>();

				GetAttributesInternal(list, type);

				CacheHelper<T>.TypeAttributes[type] = attrs = list.OfType<T>().ToArray();
			}

			return attrs;
		}

		/// <summary>
		/// Retrieves a custom attribute applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A reference to the first custom attribute of type attributeType
		/// that is applied to element, or null if there is no such attribute.</returns>
		public static T GetFirstAttribute<T>([NotNull] this Type type)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type);
			return attrs.Length > 0 ? attrs[0] : null;
		}

		/// <summary>
		/// Gets a value indicating whether a type (or type's element type)
		/// instance can be null in the underlying data store.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <returns> True, if the type parameter is a closed generic nullable type; otherwise, False.</returns>
		/// <remarks>Arrays of Nullable types are treated as Nullable types.</remarks>
		public static bool IsNullable([NotNull] this Type type)
		{
			return type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		/// <summary>
		/// Returns the underlying type argument of the specified type.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <returns><list>
		/// <item>The type argument of the type parameter,
		/// if the type parameter is a closed generic nullable type.</item>
		/// <item>The underlying Type if the type parameter is an enum type.</item>
		/// <item>Otherwise, the type itself.</item>
		/// </list>
		/// </returns>
		public static Type ToUnderlying([NotNull] this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type.IsNullable()) type = type.GetGenericArgumentsEx()[0];
			if (type.IsEnumEx  ()) type = Enum.GetUnderlyingType(type);

			return type;
		}

		public static Type ToNullableUnderlying([NotNull] this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");
			//return type.IsNullable() ? type.GetGenericArgumentsEx()[0] : type;
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		public static IEnumerable<Type> GetDefiningTypes(this Type child, MemberInfo member)
		{
			if (member.IsPropertyEx())
			{
				var prop = (PropertyInfo)member;
				member = prop.GetGetMethodEx();
			}

			foreach (var inf in child.GetInterfacesEx())
			{
				var pm = child.GetInterfaceMapEx(inf);

				for (var i = 0; i < pm.TargetMethods.Length; i++)
				{
					var method = pm.TargetMethods[i];

					if (method == member || (method.DeclaringType == member.DeclaringType && method.Name == member.Name))
						yield return inf;
				}
			}

			yield return member.DeclaringType;
		}

		/// <summary>
		/// Determines whether the specified types are considered equal.
		/// </summary>
		/// <param name="parent">A <see cref="System.Type"/> instance. </param>
		/// <param name="child">A type possible derived from the <c>parent</c> type</param>
		/// <returns>True, when an object instance of the type <c>child</c>
		/// can be used as an object of the type <c>parent</c>; otherwise, false.</returns>
		/// <remarks>Note that nullable types does not have a parent-child relation to it's underlying type.
		/// For example, the 'int?' type (nullable int) and the 'int' type
		/// aren't a parent and it's child.</remarks>
		public static bool IsSameOrParentOf([NotNull] this Type parent, [NotNull] Type child)
		{
			if (parent == null) throw new ArgumentNullException("parent");
			if (child  == null) throw new ArgumentNullException("child");

			if (parent == child ||
				child.IsEnumEx() && Enum.GetUnderlyingType(child) == parent ||
				child.IsSubclassOfEx(parent))
			{
				return true;
			}

			if (parent.IsGenericTypeDefinitionEx())
				for (var t = child; t != typeof(object) && t != null; t = t.BaseTypeEx())
					if (t.IsGenericTypeEx() && t.GetGenericTypeDefinition() == parent)
						return true;

			if (parent.IsInterfaceEx())
			{
				var interfaces = child.GetInterfacesEx();

				foreach (var t in interfaces)
				{
					if (parent.IsGenericTypeDefinitionEx())
					{
						if (t.IsGenericTypeEx() && t.GetGenericTypeDefinition() == parent)
							return true;
					}
					else if (t == parent)
						return true;
				}
			}

			return false;
		}

		public static Type GetGenericType([NotNull] this Type genericType, Type type)
		{
			if (genericType == null) throw new ArgumentNullException("genericType");

			while (type != null && type != typeof(object))
			{
				if (type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == genericType)
					return type;

				if (genericType.IsInterfaceEx())
				{
					foreach (var interfaceType in type.GetInterfacesEx())
					{
						var gType = GetGenericType(genericType, interfaceType);

						if (gType != null)
							return gType;
					}
				}

				type = type.BaseTypeEx();
			}

			return null;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="list">A <see cref="System.Object"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this IEnumerable list)
		{
			var typeOfObject = typeof(object);

			if (list == null)
				return typeOfObject;

			if (list is Array)
				return list.GetType().GetElementType();

			var type = list.GetType();

			if (list is IList || list is ITypedList || list is IListSource)
			{
				PropertyInfo last = null;

				foreach (var pi in type.GetPropertiesEx())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeOfObject)
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			if (list is IList)
			{
				foreach (var o in (IList)list)
					if (o != null && o.GetType() != typeOfObject)
						return o.GetType();
			}
			else
			{
				foreach (var o in list)
					if (o != null && o.GetType() != typeOfObject)
						return o.GetType();
			}

			return typeOfObject;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="listType">A <see cref="System.Type"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this Type listType)
		{
			if (listType.IsGenericTypeEx())
			{
				var elementTypes = listType.GetGenericArguments(typeof(IList<>));

				if (elementTypes != null)
					return elementTypes[0];
			}

			if (typeof(IList).      IsSameOrParentOf(listType) ||
				typeof(ITypedList). IsSameOrParentOf(listType) ||
				typeof(IListSource).IsSameOrParentOf(listType))
			{
				var elementType = listType.GetElementType();

				if (elementType != null)
					return elementType;

				PropertyInfo last = null;

				foreach (var pi in listType.GetPropertiesEx())
				{
					if (pi.GetIndexParameters().Length > 0 && pi.PropertyType != typeof(object))
					{
						if (pi.Name == "Item")
							return pi.PropertyType;

						last = pi;
					}
				}

				if (last != null)
					return last.PropertyType;
			}

			return typeof(object);
		}

		public static Type GetItemType(this Type type)
		{
			if (type == null)
				return null;

			if (type == typeof(object))
				return type.HasElementType ? type.GetElementType(): null;

			if (type.IsArray)
				return type.GetElementType();

			if (type.IsGenericTypeEx())
				foreach (var aType in type.GetGenericArgumentsEx())
					if (typeof(IEnumerable<>).MakeGenericType(new[] { aType }).IsAssignableFromEx(type))
						return aType;

			var interfaces = type.GetInterfacesEx();

			if (interfaces != null && interfaces.Length > 0)
			{
				foreach (var iType in interfaces)
				{
					var eType = iType.GetItemType();

					if (eType != null)
						return eType;
				}
			}

			return type.BaseTypeEx().GetItemType();
		}

		/// <summary>
		/// Gets a value indicating whether a type can be used as a db primitive.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <param name="checkArrayElementType">True if needed to check element type for arrays</param>
		/// <returns> True, if the type parameter is a primitive type; otherwise, False.</returns>
		/// <remarks><see cref="System.String"/>. <see cref="Stream"/>. 
		/// <see cref="XmlReader"/>. <see cref="XmlDocument"/>. are specially handled by the library
		/// and, therefore, can be treated as scalar types.</remarks>
		public static bool IsScalar(this Type type, bool checkArrayElementType = true)
		{
			while (checkArrayElementType && type.IsArray)
				type = type.GetElementType();

			return type.IsValueTypeEx()
				|| type == typeof(string)
				|| type == typeof(Binary)
				|| type == typeof(Stream)
				|| type == typeof(XmlReader)
				|| type == typeof(XmlDocument)
				;
		}

		///<summary>
		/// Returns an array of Type objects that represent the type arguments
		/// of a generic type or the type parameters of a generic type definition.
		///</summary>
		/// <param name="type">A <see cref="System.Type"/> instance.</param>
		///<param name="baseType">Non generic base type.</param>
		///<returns>An array of Type objects that represent the type arguments
		/// of a generic type. Returns an empty array if the current type is not a generic type.</returns>
		public static Type[] GetGenericArguments(this Type type, Type baseType)
		{
			var baseTypeName = baseType.Name;

			for (var t = type; t != typeof(object) && t != null; t = t.BaseTypeEx())
			{
				if (t.IsGenericTypeEx())
				{
					if (baseType.IsGenericTypeDefinitionEx())
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArgumentsEx();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArgumentsEx();
					}
				}
			}

			foreach (var t in type.GetInterfacesEx())
			{
				if (t.IsGenericTypeEx())
				{
					if (baseType.IsGenericTypeDefinitionEx())
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArgumentsEx();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArgumentsEx();
					}
				}
			}

			return null;
		}

		public static bool IsFloatType(this Type type)
		{
			if (type.IsNullable())
				type = type.GetGenericArgumentsEx()[0];

			switch (type.GetTypeCodeEx())
			{
				case TypeCode.Single  :
				case TypeCode.Double  :
				case TypeCode.Decimal : return true;
			}

			return false;
		}

		public static bool IsIntegerType(this Type type)
		{
			if (type.IsNullable())
				type = type.GetGenericArgumentsEx()[0];

			switch (type.GetTypeCodeEx())
			{
				case TypeCode.SByte  :
				case TypeCode.Byte   :
				case TypeCode.Int16  :
				case TypeCode.UInt16 :
				case TypeCode.Int32  :
				case TypeCode.UInt32 :
				case TypeCode.Int64  :
				case TypeCode.UInt64 : return true;
			}

			return false;
		}

		interface IGetDefaultValueHelper
		{
			object GetDefaultValue();
		}

		class GetDefaultValueHelper<T> : IGetDefaultValueHelper
		{
			public object GetDefaultValue()
			{
				return default(T);
			}
		}

		public static object GetDefaultValue(this Type type)
		{
			var dtype  = typeof(GetDefaultValueHelper<>).MakeGenericType(type);
			var helper = (IGetDefaultValueHelper)Activator.CreateInstance(dtype);

			return helper.GetDefaultValue();
		}

		public static EventInfo GetEventEx(this Type type, string eventName)
		{
			return type.GetEvent(eventName);
		}
		
		#endregion

		#region MethodInfo extensions

		public static PropertyInfo GetPropertyInfo(this MethodInfo method)
		{
			if (method != null)
			{
				var type = method.DeclaringType;

				foreach (var info in type.GetPropertiesEx())
				{
					if (info.CanRead && method == info.GetGetMethodEx(true))
						return info;

					if (info.CanWrite && method == info.GetSetMethodEx(true))
						return info;
				}
			}

			return null;
		}

		#endregion

		#region MemberInfo extensions

		public static Type GetMemberType(this MemberInfo memberInfo)
		{
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Property    : return ((PropertyInfo)memberInfo).PropertyType;
				case MemberTypes.Field       : return ((FieldInfo)   memberInfo).FieldType;
				case MemberTypes.Method      : return ((MethodInfo)  memberInfo).ReturnType;
				case MemberTypes.Constructor : return                memberInfo. DeclaringType;
			}

			throw new InvalidOperationException();
		}

		public static bool IsNullableValueMember(this MemberInfo member)
		{
			return
				member.Name == "Value" &&
				member.DeclaringType.IsGenericTypeEx() &&
				member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static bool IsNullableHasValueMember(this MemberInfo member)
		{
			return
				member.Name == "HasValue" &&
				member.DeclaringType.IsGenericTypeEx() &&
				member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		static readonly Dictionary<Type,HashSet<Type>> _castDic = new Dictionary<Type,HashSet<Type>>
		{
			{ typeof(decimal), new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char)                } },
			{ typeof(double),  new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
			{ typeof(float),   new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
			{ typeof(ulong),   new HashSet<Type> { typeof(byte),  typeof(ushort), typeof(uint),  typeof(char)                                                                                        } },
			{ typeof(long),    new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char)                                             } },
			{ typeof(uint),    new HashSet<Type> { typeof(byte),  typeof(ushort), typeof(char)                                                                                                       } },
			{ typeof(int),     new HashSet<Type> { typeof(sbyte), typeof(byte),   typeof(short), typeof(ushort), typeof(char)                                                                        } },
			{ typeof(ushort),  new HashSet<Type> { typeof(byte),  typeof(char)                                                                                                                       } },
			{ typeof(short),   new HashSet<Type> { typeof(byte)                                                                                                                                      } }
		};

		public static bool CanConvertTo(this Type fromType, Type toType)
		{
			if (fromType == toType)
				return true;

			if (_castDic.ContainsKey(toType) && _castDic[toType].Contains(fromType))
				return true;

			var tc = TypeDescriptor.GetConverter(fromType);

			if (toType.IsAssignableFrom(fromType))
				return true; 

			if (tc.CanConvertTo(toType))
				return true;

			tc = TypeDescriptor.GetConverter(toType);

			if (tc.CanConvertFrom(fromType))
				return true;

			if (fromType.GetMethodsEx()
				.Any(m => m.IsStatic && m.IsPublic && m.ReturnType == toType && (m.Name == "op_Implicit" || m.Name == "op_Explicit")))
				return true;

			return false;
		}

		public static bool EqualsTo(this MemberInfo member1, MemberInfo member2, Type declaringType = null)
		{
			if (ReferenceEquals(member1, member2))
				return true;

			if (member1 == null || member2 == null)
				return false;

			if (member1.Name == member2.Name)
			{
				if (member1.DeclaringType == member2.DeclaringType)
					return true;

				if (member1 is PropertyInfo)
				{
					var isSubclass =
						member1.DeclaringType.IsSameOrParentOf(member2.DeclaringType) ||
						member2.DeclaringType.IsSameOrParentOf(member1.DeclaringType);

					if (isSubclass)
						return true;

					if (declaringType != null && member2.DeclaringType.IsInterfaceEx())
					{
						var getter1 = ((PropertyInfo)member1).GetGetMethodEx();
						var getter2 = ((PropertyInfo)member2).GetGetMethodEx();

						var map = declaringType.GetInterfaceMapEx(member2.DeclaringType);

						for (var i = 0; i < map.InterfaceMethods.Length; i++)
							if (getter2.Name == map.InterfaceMethods[i].Name && getter2.DeclaringType == map.InterfaceMethods[i].DeclaringType &&
								getter1.Name == map.TargetMethods   [i].Name && getter1.DeclaringType == map.TargetMethods   [i].DeclaringType)
								return true;
					}
				}
			}

			if (member2.DeclaringType.IsInterfaceEx() && !member1.DeclaringType.IsInterfaceEx() && member1.Name.EndsWith(member2.Name))
			{
				if (member1 is PropertyInfo)
				{
					var isSubclass = member2.DeclaringType.IsAssignableFromEx(member1.DeclaringType);

					if (isSubclass)
					{
						var getter1 = ((PropertyInfo)member1).GetGetMethodEx();
						var getter2 = ((PropertyInfo)member2).GetGetMethodEx();

						var map = member1.DeclaringType.GetInterfaceMapEx(member2.DeclaringType);

						for (var i = 0; i < map.InterfaceMethods.Length; i++)
						{
							var imi = map.InterfaceMethods[i];
							var tmi = map.TargetMethods[i];

							if ((getter2 == null || (getter2.Name == imi.Name && getter2.DeclaringType == imi.DeclaringType)) &&
							    (getter1 == null || (getter1.Name == tmi.Name && getter1.DeclaringType == tmi.DeclaringType)))
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		#endregion

	}
}
