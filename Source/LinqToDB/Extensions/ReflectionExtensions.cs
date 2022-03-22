using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

using JetBrains.Annotations;

namespace LinqToDB.Extensions
{
	using System.Diagnostics.CodeAnalysis;
	using Expressions;
	using LinqToDB.Common;

	[PublicAPI]
	public static class ReflectionExtensions
	{
		#region Type extensions
		public static MemberInfo[] GetPublicInstanceMembersEx(this Type type)
		{
			return type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
		}

		public static MemberInfo[] GetPublicInstanceValueMembers(this Type type)
		{
			var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
				.Where(m => m.IsFieldEx() || m.IsPropertyEx() && ((PropertyInfo)m).GetIndexParameters().Length == 0);

			var baseType = type.BaseType;
			if (baseType == null || baseType == typeof(object) || baseType == typeof(ValueType))
				return members.ToArray();

			var results = new LinkedList<MemberInfo>();
			var names   = new HashSet<string>();
			for (var t = type; t != typeof(object) && t != typeof(ValueType); t = t.BaseType!)
			{
				foreach (var m in members.Where(_ => _.DeclaringType == t))
				{
					if (!names.Contains(m.Name))
					{
						results.AddFirst(m);
						names.Add(m.Name);
					}
				}
			}

			return results.ToArray();
		}

		public static MemberInfo[] GetStaticMembersEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		}

		/// <summary>
		/// Returns <see cref="MemberInfo"/> of <paramref name="type"/> described by <paramref name="memberInfo"/>
		/// It us useful when member's declared and reflected types are not the same.
		/// </summary>
		/// <remarks>This method searches only properties, fields and methods</remarks>
		/// <param name="type"><see cref="Type"/> to find member info</param>
		/// <param name="memberInfo"><see cref="MemberInfo"/> </param>
		/// <returns><see cref="MemberInfo"/> or null</returns>
		public static MemberInfo? GetMemberEx(this Type type, MemberInfo memberInfo)
		{
			if (memberInfo.ReflectedType == type)
				return memberInfo;

			if (memberInfo.IsPropertyEx())
			{
				var props = type.GetProperties();

				PropertyInfo? foundByName = null;
				foreach (var prop in props)
				{
					if (prop.Name == memberInfo.Name)
					{
						foundByName ??= prop;
						if (prop.GetMemberType() == memberInfo.GetMemberType())
						{
							return prop;
						}
					}
				}

				return foundByName;
			}

			if (memberInfo.IsFieldEx())
				return type.GetField   (memberInfo.Name);

			if (memberInfo.IsMethodEx())
				return type.GetMethodEx(memberInfo.Name, ((MethodInfo) memberInfo).GetParameters().Select(_ => _.ParameterType).ToArray());

			return null;
		}

		public static MethodInfo? GetMethodEx(this Type type, string name)
		{
			return type.GetMethod(name);
		}

		/// <summary>
		/// Gets method by name, input parameters and return type.
		/// Usefull for method overloads by return type, like op_Explicit/op_Implicit conversions.
		/// </summary>
		public static MethodInfo? GetMethodEx(this Type type, Type returnType, string name, params Type[] types)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.Name == name && method.ReturnType == returnType)
				{
					var parameters = method.GetParameters();
					if (parameters.Length != types.Length)
						continue;

					var found = true;
					for (var i = 0; i < types.Length; i++)
					{
						if (types[i] != parameters[i].ParameterType)
						{
							found = false;
							break;
						}
					}

					if (found)
						return method;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets generic method.
		/// </summary>
		public static MethodInfo? GetMethodEx(this Type type, string name, int genericParametersCount, params Type[] types)
		{
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.IsGenericMethod
					&& method.Name == name
					&& method.GetGenericMethodDefinition().GetGenericArguments().Length == genericParametersCount)
				{
					var parameters = method.GetParameters();
					if (parameters.Length == types.Length)
					{
						var found = true;
						for (var i = 0; i < types.Length; i++)
						{
							if (!parameters[i].ParameterType.IsGenericParameter && parameters[i].ParameterType != types[i])
							{
								found = false;
								break;
							}
						}

						if (found)
							return method;
					}
				}
			}

			return null;
		}

		public static MethodInfo? GetMethodEx(this Type type, string name, params Type[] types)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
		}

		public static MethodInfo? GetPublicInstanceMethodEx(this Type type, string name, params Type[] types)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
		}

		public static ConstructorInfo? GetDefaultConstructorEx(this Type type)
		{
			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
				null,
				Type.EmptyTypes,
				null);
		}

		public static TypeCode GetTypeCodeEx(this Type type)
		{
			return Type.GetTypeCode(type);
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

		private static readonly MemberInfo SQLPropertyMethod = MemberHelper.MethodOf(() => Sql.Property<string>(null!, null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Determines whether member info represent a Sql.Property method.
		/// </summary>
		/// <param name="memberInfo">The member information.</param>
		/// <returns>
		///   <c>true</c> if member info is Sql.Property method; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsSqlPropertyMethodEx(this MemberInfo memberInfo)
		{
			return memberInfo is MethodInfo methodCall && methodCall.IsGenericMethod &&
			       methodCall.GetGenericMethodDefinition() == SQLPropertyMethod;
		}

		/// <summary>
		/// Determines whether member info is dynamic column property.
		/// </summary>
		/// <param name="memberInfo">The member information.</param>
		/// <returns>
		///   <c>true</c> if member info is dynamic column property; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsDynamicColumnPropertyEx(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType == MemberTypes.Property && memberInfo is Mapping.DynamicColumnInfo;
		}

		public static PropertyInfo[] GetPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		public static PropertyInfo[] GetNonPublicPropertiesEx(this Type type)
		{
			return type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MemberInfo[] GetInstanceMemberEx(this Type type, string name)
		{
			return type.GetMember(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public static MemberInfo[] GetPublicMemberEx(this Type type, string name)
		{
			return type.GetMember(name);
		}

#if NETSTANDARD2_0
		private static Func<Type, Type, InterfaceMapping>? _getInterfaceMap;
#endif
		public static InterfaceMapping GetInterfaceMapEx(this Type type, Type interfaceType)
		{
			// native UWP builds (corert) had no GetInterfaceMap() implementation
			// (added here https://github.com/dotnet/corert/pull/8144)
#if NETSTANDARD2_0
			if (_getInterfaceMap == null)
			{
				_getInterfaceMap = (t, i) => t.GetInterfaceMap(i);
				try
				{
					return _getInterfaceMap(type, interfaceType);
				}
				catch (PlatformNotSupportedException)
				{
					// porting of https://github.com/dotnet/corert/pull/8144 is not possible as it requires access
					// to non-public runtime data and reflection doesn't work in corert
					_getInterfaceMap = (t, i) => new InterfaceMapping()
					{
						TargetType       = t,
						InterfaceType    = i,
						TargetMethods    = Array.Empty<MethodInfo>(),
						InterfaceMethods = Array.Empty<MethodInfo>()
					};
				}
			}

			return _getInterfaceMap(type, interfaceType);
#else
			return type.GetInterfaceMap(interfaceType);
#endif
		}

		static class CacheHelper<T>
		{
			public static readonly ConcurrentDictionary<Type,T[]> TypeAttributes = new ();
		}

#region Attributes cache

		static readonly ConcurrentDictionary<Type, IReadOnlyCollection<object>> _typeAttributesTopInternal = new ();

		static IReadOnlyCollection<object> GetAttributesInternal(Type type)
		{
			return _typeAttributesTopInternal.GetOrAdd(type, static type => GetAttributesTreeInternal(type));
		}

		static readonly ConcurrentDictionary<Type, IReadOnlyCollection<object>> _typeAttributesInternal = new ();

		static IReadOnlyCollection<object> GetAttributesTreeInternal(Type type)
		{
			IReadOnlyCollection<object> attrs = _typeAttributesInternal.GetOrAdd(type, x => type.GetCustomAttributes(false));

			if (type.IsInterface)
				return attrs;

			// Reflection returns interfaces for the whole inheritance chain.
			// So, we are going to get some hemorrhoid here to restore the inheritance sequence.
			//
			var interfaces      = type.GetInterfaces();
			var nBaseInterfaces = type.BaseType != null? type.BaseType.GetInterfaces().Length: 0;
			List<object>? list  = null;

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

				var ifaceAttrs = GetAttributesTreeInternal(intf);
				if (ifaceAttrs.Count > 0)
				{
					if (list != null)
						list.AddRange(ifaceAttrs);
					else if (attrs.Count == 0)
						attrs = ifaceAttrs;
					else
						(list ??= new(attrs)).AddRange(ifaceAttrs);
				}
			}

			if (type.BaseType != null && type.BaseType != typeof(object))
			{
				var baseAttrs = GetAttributesTreeInternal(type.BaseType);
				if (baseAttrs.Count > 0)
				{
					if (list != null)
						list.AddRange(baseAttrs);
					else if (attrs.Count == 0)
						attrs = baseAttrs;
					else
						(list ??= new(attrs)).AddRange(baseAttrs);
				}
			}

			if (list != null   ) return list;
			if (attrs.Count > 0) return attrs;
			return Array<object>.Empty;
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
		public static T[] GetAttributes<T>(this Type type)
			where T : Attribute
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return CacheHelper<T>.TypeAttributes.GetOrAdd(
				type,
				static type => GetAttributesInternal(type).OfType<T>().ToArray());
		}

		/// <summary>
		/// Retrieves a custom attribute applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</typeparam>
		/// <returns>A reference to the first custom attribute of type attributeType
		/// that is applied to element, or null if there is no such attribute.</returns>
		public static T GetFirstAttribute<T>(this Type type)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type);
			return attrs.Length > 0 ? attrs[0] : null!;
		}

		/// <summary>
		/// Returns true, if type is <see cref="Nullable{T}"/> type.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> instance. </param>
		/// <returns><c>true</c>, if <paramref name="type"/> represents <see cref="Nullable{T}"/> type; otherwise, <c>false</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullable(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		/// <summary>
		/// Returns the underlying type argument of the specified type.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> instance. </param>
		/// <returns><list>
		/// <item>The type argument of the type parameter,
		/// if the type parameter is a closed generic nullable type.</item>
		/// <item>The underlying Type if the type parameter is an enum type.</item>
		/// <item>Otherwise, the type itself.</item>
		/// </list>
		/// </returns>
		public static Type ToUnderlying(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (type.IsNullable()) type = type.GetGenericArguments()[0];
			if (type.IsEnum      ) type = Enum.GetUnderlyingType(type);

			return type;
		}

		public static Type ToNullableUnderlying(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));
			//return type.IsNullable() ? type.GetGenericArguments()[0] : type;
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		/// <summary>
		/// Wraps type into <see cref="Nullable{T}"/> class.
		/// </summary>
		/// <param name="type">Value type to wrap. Must be value type (except <see cref="Nullable{T}"/> itself).</param>
		/// <returns>Type, wrapped by <see cref="Nullable{T}"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Type AsNullable(this Type type)
		{
			if (type == null)      throw new ArgumentNullException(nameof(type));
			if (!type.IsValueType) throw new ArgumentException($"{type} is not a value type");
			if (type.IsNullable()) throw new ArgumentException($"{type} is nullable type already");

			return typeof(Nullable<>).MakeGenericType(type);
		}

		public static IEnumerable<Type> GetDefiningTypes(this Type child, MemberInfo member)
		{
			if (member.IsPropertyEx())
			{
				var prop = (PropertyInfo)member;
				member = prop.GetGetMethod()!;
			}

			foreach (var inf in child.GetInterfaces())
			{
				var pm = child.GetInterfaceMapEx(inf);

				for (var i = 0; i < pm.TargetMethods.Length; i++)
				{
					var method = pm.TargetMethods[i];

					if (method == member || (method.DeclaringType == member.DeclaringType && method.Name == member.Name))
						yield return inf;
				}
			}

			yield return member.DeclaringType!;
		}

		static readonly ConcurrentDictionary<(Type parent, Type child), bool> _isSameOrParentOf = new ();

		/// <summary>
		/// Determines whether the specified types are considered equal.
		/// </summary>
		/// <param name="parent">A <see cref="Type"/> instance. </param>
		/// <param name="child">A type possible derived from the <c>parent</c> type</param>
		/// <returns>True, when an object instance of the type <c>child</c>
		/// can be used as an object of the type <c>parent</c>; otherwise, false.</returns>
		/// <remarks>Note that nullable types does not have a parent-child relation to it's underlying type.
		/// For example, the 'int?' type (nullable int) and the 'int' type
		/// aren't a parent and it's child.</remarks>
		public static bool IsSameOrParentOf(this Type parent, Type child)
		{
			if (parent == null) throw new ArgumentNullException(nameof(parent));
			if (child  == null) throw new ArgumentNullException(nameof(child));

			if (parent == child)
				return true;

			return _isSameOrParentOf.GetOrAdd((parent, child), static key =>
			{
				var (parent, child) = key;

				if (child.IsEnum && Enum.GetUnderlyingType(child) == parent ||
					child.IsSubclassOf(parent))
					return true;

				if (parent.IsGenericTypeDefinition)
					for (var t = child; t != typeof(object) && t != null; t = t.BaseType)
						if (t.IsGenericType && t.GetGenericTypeDefinition() == parent)
							return true;

				if (parent.IsInterface)
				{
					var interfaces = child.GetInterfaces();

					foreach (var t in interfaces)
					{
						if (parent.IsGenericTypeDefinition)
						{
							if (t.IsGenericType && t.GetGenericTypeDefinition() == parent)
								return true;
						}
						else if (t == parent)
							return true;
					}
				}

				return false;
			});
		}

		/// <summary>
		/// Determines whether the <paramref name="type"/> derives from the specified <paramref name="check"/>.
		/// </summary>
		/// <remarks>
		/// This method also returns false if <paramref name="type"/> and the <paramref name="check"/> are equal.
		/// </remarks>
		/// <param name="type">The type to test.</param>
		/// <param name="check">The type to compare with. </param>
		/// <returns>
		/// true if the <paramref name="type"/> derives from <paramref name="check"/>; otherwise, false.
		/// </returns>
		[Pure]
		internal static bool IsSubClassOf(this Type type, Type check)
		{
			if (type  == null) throw new ArgumentNullException(nameof(type));
			if (check == null) throw new ArgumentNullException(nameof(check));

			if (type == check)
				return false;

			while (true)
			{
				if (check.IsInterface)
					// ReSharper disable once LoopCanBeConvertedToQuery
					foreach (var interfaceType in type.GetInterfaces())
						if (interfaceType == check || interfaceType.IsSubClassOf(check))
							return true;

				if (type.IsGenericType && !type.IsGenericTypeDefinition)
				{
					var definition = type.GetGenericTypeDefinition();
					if (definition == check || definition.IsSubClassOf(check))
						return true;
				}

				if (type.BaseType == null)
					return false;

				type = type.BaseType;

				if (type == check)
					return true;
			}
		}

		public static Type? GetGenericType(this Type genericType, Type type)
		{
			if (genericType == null) throw new ArgumentNullException(nameof(genericType));

			while (type != typeof(object))
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
					return type;

				if (genericType.IsInterface)
				{
					foreach (var interfaceType in type.GetInterfaces())
					{
						var gType = GetGenericType(genericType, interfaceType);

						if (gType != null)
							return gType;
					}
				}

				if (type.BaseType == null)
					break;

				type = type.BaseType;
			}

			return null;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="list">A <see cref="System.Object"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this IEnumerable? list)
		{
			var typeOfObject = typeof(object);

			if (list == null)
				return typeOfObject;

			if (list is Array)
				return list.GetType().GetElementType()!;

			var type = list.GetType();

			if (list is IList || list is ITypedList || list is IListSource)
			{
				PropertyInfo? last = null;

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

			if (list is IList list1)
			{
				foreach (var o in list1)
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
		/// <param name="listType">A <see cref="Type"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(this Type listType)
		{
			if (listType.IsGenericType)
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

				PropertyInfo? last = null;

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

		public static bool IsEnumerableTType(this Type type, Type elementType)
		{
			foreach (var interfaceType in type.GetInterfaces())
			{
				if (interfaceType.IsGenericType
						&& interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
						&& interfaceType.GetGenericArguments()[0] == elementType)
					return true;
			}

			return false;
		}

		public static bool IsGenericEnumerableType(this Type type)
		{
			if (type.IsGenericType)
				if (typeof(IEnumerable<>).IsSameOrParentOf(type))
					return true;
			return false;
		}

		static readonly ConcurrentDictionary<Type,Type?> _getItemTypeCache = new ();

		public static Type? GetItemType(this Type? type)
		{
			if (type == null)
				return null;

			return _getItemTypeCache.GetOrAdd(type, static t =>
			{
				if (t == typeof(object))
					return null;

				if (t.IsArray)
					return t.GetElementType();

				if (t.IsGenericType)
					foreach (var aType in t.GetGenericArguments())
						if (typeof(IEnumerable<>).MakeGenericType(new[] {aType})
							.IsAssignableFrom(t))
							return aType;

				var interfaces = t.GetInterfaces();

				if (interfaces != null && interfaces.Length > 0)
				{
					foreach (var iType in interfaces)
					{
						var eType = iType.GetItemType();

						if (eType != null)
							return eType;
					}
				}

				return t.BaseType.GetItemType();
			});
		}

		/// <summary>
		/// Gets a value indicating whether a type can be used as a db primitive.
		/// </summary>
		/// <param name="type">A <see cref="Type"/> instance. </param>
		/// <param name="checkArrayElementType">True if needed to check element type for arrays</param>
		/// <returns> True, if the type parameter is a primitive type; otherwise, False.</returns>
		/// <remarks><see cref="System.String"/>. <see cref="Stream"/>.
		/// <see cref="XmlReader"/>. <see cref="XmlDocument"/>. are specially handled by the library
		/// and, therefore, can be treated as scalar types.</remarks>
		public static bool IsScalar(this Type type, bool checkArrayElementType = true)
		{
			if (type == typeof(byte[]))
				return true;

			while (checkArrayElementType && type.IsArray)
				type = type.GetElementType()!;

			return type.IsValueType
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
		/// <param name="type">A <see cref="Type"/> instance.</param>
		///<param name="baseType">Non generic base type.</param>
		///<returns>An array of Type objects that represent the type arguments
		/// of a generic type. Returns an empty array if the current type is not a generic type.</returns>
		public static Type[]? GetGenericArguments(this Type type, Type baseType)
		{
			var baseTypeName = baseType.Name;

			for (var t = type; t != typeof(object) && t != null; t = t.BaseType)
			{
				if (t.IsGenericType)
				{
					if (baseType.IsGenericTypeDefinition)
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArguments();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArguments();
					}
				}
			}

			foreach (var t in type.GetInterfaces())
			{
				if (t.IsGenericType)
				{
					if (baseType.IsGenericTypeDefinition)
					{
						if (t.GetGenericTypeDefinition() == baseType)
							return t.GetGenericArguments();
					}
					else if (baseTypeName == null || t.Name.Split('`')[0] == baseTypeName)
					{
						return t.GetGenericArguments();
					}
				}
			}

			return null;
		}

		public static bool IsFloatType(this Type type)
		{
			if (type.IsNullable())
				type = type.GetGenericArguments()[0];

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
				type = type.GetGenericArguments()[0];

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
			object? GetDefaultValue();
		}

		class GetDefaultValueHelper<T> : IGetDefaultValueHelper
		{
			public object? GetDefaultValue()
			{
				return default(T)!;
			}
		}

		public static object? GetDefaultValue(this Type type)
		{
			var dtype  = typeof(GetDefaultValueHelper<>).MakeGenericType(type);
			var helper = (IGetDefaultValueHelper)Activator.CreateInstance(dtype)!;

			return helper.GetDefaultValue();
		}

		public static EventInfo? GetEventEx(this Type type, string eventName)
		{
			return type.GetEvent(eventName);
		}

#endregion

#region MethodInfo extensions

		[return: NotNullIfNotNull("method")]
		public static PropertyInfo? GetPropertyInfo(this MethodInfo? method)
		{
			if (method != null)
			{
				var type = method.DeclaringType!;

				foreach (var info in type.GetPropertiesEx())
				{
					if (info.CanRead && method == info.GetGetMethod(true))
						return info;

					if (info.CanWrite && method == info.GetSetMethod(true))
						return info;
				}
			}

			return null;
		}

#endregion

#region MemberInfo extensions

		public static Type GetMemberType(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType switch
			{
				MemberTypes.Property    => ((PropertyInfo)memberInfo).PropertyType,
				MemberTypes.Field       => ((FieldInfo)memberInfo).FieldType,
				MemberTypes.Method      => ((MethodInfo)memberInfo).ReturnType,
				MemberTypes.Constructor => memberInfo.DeclaringType!,
				_                       => throw new InvalidOperationException(),
			};
		}

		public static bool IsNullableValueMember(this MemberInfo member)
		{
			return
				member.Name == "Value" &&
				member.DeclaringType!.IsNullable();
		}

		public static bool IsNullableHasValueMember(this MemberInfo member)
		{
			return
				member.Name == "HasValue" &&
				member.DeclaringType!.IsNullable();
		}

		public static bool IsNullableGetValueOrDefault(this MemberInfo member)
		{
			return
				member.Name == "GetValueOrDefault" &&
				member.DeclaringType!.IsNullable();
		}

		static readonly Dictionary<Type,HashSet<Type>> _castDic = new ()
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

			if (fromType.GetMethods()
				.Any(m => m.IsStatic && m.IsPublic && m.ReturnType == toType && (m.Name == "op_Implicit" || m.Name == "op_Explicit")))
				return true;

			return false;
		}

		public static bool EqualsTo(this MemberInfo? member1, MemberInfo? member2, Type? declaringType = null)
		{
			if (ReferenceEquals(member1, member2))
				return true;

			if (member1 == null || member2 == null)
				return false;

			if (member1.Name == member2.Name)
			{
				if (member1.DeclaringType == member2.DeclaringType)
					return true;

				if (member1 is PropertyInfo info1)
				{
					var isSubclass =
						member1.DeclaringType!.IsSameOrParentOf(member2.DeclaringType!) ||
						member2.DeclaringType!.IsSameOrParentOf(member1.DeclaringType!);

					if (isSubclass)
						return true;

					if (declaringType != null && member2.DeclaringType!.IsInterface)
					{
						var getter1 = info1.GetGetMethod()!;
						var getter2 = ((PropertyInfo)member2).GetGetMethod()!;

						var map = declaringType.GetInterfaceMapEx(member2.DeclaringType);

						for (var i = 0; i < map.InterfaceMethods.Length; i++)
							if (getter2.Name == map.InterfaceMethods[i].Name && getter2.DeclaringType == map.InterfaceMethods[i].DeclaringType &&
								getter1.Name == map.TargetMethods   [i].Name && getter1.DeclaringType == map.TargetMethods   [i].DeclaringType)
								return true;
					}
				}
			}

			if (member2.DeclaringType!.IsInterface && !member1.DeclaringType!.IsInterface && member1.Name.EndsWith(member2.Name))
			{
				if (member1 is PropertyInfo info)
				{
					var isSubclass = member2.DeclaringType.IsAssignableFrom(member1.DeclaringType);

					if (isSubclass)
					{
						var getter1 = info.GetGetMethod();
						var getter2 = ((PropertyInfo)member2).GetGetMethod();

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

		public static bool IsAnonymous(this Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			return
				!type.IsPublic &&
				 type.IsGenericType &&
				// C# anonymous type name prefix
				(type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) ||
				 // VB.NET anonymous type name prefix
				 type.Name.StartsWith("VB$AnonymousType", StringComparison.Ordinal)) &&
				type.GetCustomAttribute(typeof(CompilerGeneratedAttribute), false) != null;
		}

		internal static MemberInfo GetMemberOverride(this Type type, MemberInfo mi)
		{
			if (mi.DeclaringType == type)
				return mi;

			if (mi is MethodInfo method)
			{
				var baseDefinition = method.GetBaseDefinition();

				foreach (var m in type.GetMethods())
					if (m.GetBaseDefinition() == baseDefinition)
						return m;
			}
			else if (mi is PropertyInfo property)
			{
				if (property.GetMethod != null)
				{
					var baseDefinition = property.GetMethod.GetBaseDefinition();

					foreach (var p in type.GetProperties())
						if (p.GetMethod?.GetBaseDefinition() == baseDefinition)
							return p;
				}
				if (property.SetMethod != null)
				{
					var baseDefinition = property.SetMethod.GetBaseDefinition();

					foreach (var p in type.GetProperties())
						if (p.SetMethod?.GetBaseDefinition() == baseDefinition)
							return p;
				}
			}

			return mi;
		}

		static ConcurrentDictionary<MethodInfo, MethodInfo> _methodDefinitionCache = new ();

		internal static MethodInfo GetGenericMethodDefinitionCached(this MethodInfo method)
		{
			if (!method.IsGenericMethod || method.IsGenericMethodDefinition)
				return method;

			return _methodDefinitionCache.GetOrAdd(method, static mi => mi.GetGenericMethodDefinition());
		}
	}
}
