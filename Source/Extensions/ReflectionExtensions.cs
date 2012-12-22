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
	static class ReflectionExtensions
	{
		#region Type extensions

		static class CacheHelper<T>
		{
			public static readonly ConcurrentDictionary<Type,T[]> TypeAttributes = new ConcurrentDictionary<Type,T[]>();
		}

		#region Attributes cache

		static readonly Dictionary<Type, object[]> _typeAttributesTopInternal = new Dictionary<Type, object[]>(10);

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
				_typeAttributesTopInternal.Add(type, list.ToArray());
			}
		}

		static readonly Dictionary<Type, object[]> _typeAttributesInternal = new Dictionary<Type, object[]>(10);

		static void GetAttributesTreeInternal(List<object> list, Type type)
		{
			object[] attrs;

			if (!_typeAttributesInternal.TryGetValue(type, out attrs))
				_typeAttributesInternal.Add(type, attrs = type.GetCustomAttributes(false));

			list.AddRange(attrs);

			if (type.IsInterface)
				return;

			// Reflection returns interfaces for the whole inheritance chain.
			// So, we are going to get some hemorrhoid here to restore the inheritance sequence.
			//
			var interfaces      = type.GetInterfaces();
			var nBaseInterfaces = type.BaseType != null? type.BaseType.GetInterfaces().Length: 0;

			for (var i = 0; i < interfaces.Length; i++)
			{
				var intf = interfaces[i];

				if (i < nBaseInterfaces)
				{
					var getAttr = false;

					foreach (var mi in type.GetInterfaceMap(intf).TargetMethods)
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

			if (type.BaseType != null && type.BaseType != typeof(object))
				GetAttributesTreeInternal(list, type.BaseType);
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
			return attrs.Length > 0? attrs[0]: null;
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
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
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

			if (type.IsNullable()) type = type.GetGenericArguments()[0];
			if (type.IsEnum)       type = Enum.GetUnderlyingType(type);

			return type;
		}

		public static Type ToNullableUnderlying([NotNull] this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");
			return type.IsNullable() ? type.GetGenericArguments()[0] : type;
		}

		public static IEnumerable<Type> GetDefiningTypes(this Type child, MemberInfo member)
		{
			if (member.MemberType == MemberTypes.Property)
			{
				var prop = (PropertyInfo)member;
				member = prop.GetGetMethod();
			}

			foreach (var inf in child.GetInterfaces())
			{
				var pm = child.GetInterfaceMap(inf);

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
				child.IsEnum && Enum.GetUnderlyingType(child) == parent ||
				child.IsSubclassOf(parent))
			{
				return true;
			}

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
		}

		public static Type GetGenericType([NotNull] this Type genericType, Type type)
		{
			if (genericType == null) throw new ArgumentNullException("genericType");

			while (type != null && type != typeof(object))
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

				type = type.BaseType;
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

			if (list is IList
#if !SILVERLIGHT
				|| list is ITypedList || list is IListSource
#endif
				)
			{
				PropertyInfo last = null;

				foreach (var pi in type.GetProperties())
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
			if (listType.IsGenericType)
			{
				var elementTypes = listType.GetGenericArguments(typeof(IList<>));

				if (elementTypes != null)
					return elementTypes[0];
			}

			if (typeof(IList).IsSameOrParentOf(listType)
#if !SILVERLIGHT
				|| typeof(ITypedList). IsSameOrParentOf(listType)
				|| typeof(IListSource).IsSameOrParentOf(listType)
#endif
				)
			{
				var elementType = listType.GetElementType();

				if (elementType != null)
					return elementType;

				PropertyInfo last = null;

				foreach (var pi in listType.GetProperties())
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

			if (type.IsGenericType)
				foreach (var aType in type.GetGenericArguments())
					if (typeof(IEnumerable<>).MakeGenericType(new[] { aType }).IsAssignableFrom(type))
						return aType;

			var interfaces = type.GetInterfaces();

			if (interfaces != null && interfaces.Length > 0)
			{
				foreach (var iType in interfaces)
				{
					var eType = iType.GetItemType();

					if (eType != null)
						return eType;
				}
			}

			return type.BaseType.GetItemType();
		}

		/// <summary>
		/// Gets a value indicating whether a type can be used as a db primitive.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <returns> True, if the type parameter is a primitive type; otherwise, False.</returns>
		/// <remarks><see cref="System.String"/>. <see cref="Stream"/>. 
		/// <see cref="XmlReader"/>. <see cref="XmlDocument"/>. are specially handled by the library
		/// and, therefore, can be treated as scalar types.</remarks>
		public static bool IsScalar(this Type type)
		{
			while (type.IsArray)
				type = type.GetElementType();

			return type.IsValueType
				|| type == typeof(string)
				|| type == typeof(Binary)
				|| type == typeof(Stream)
				|| type == typeof(XmlReader)
#if !SILVERLIGHT
				|| type == typeof(XmlDocument)
#endif
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

			switch (Type.GetTypeCode(type))
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

			switch (Type.GetTypeCode(type))
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

		#endregion

		#region MethodInfo extensions

		public static PropertyInfo GetPropertyInfo(this MethodInfo method)
		{
			if (method != null)
			{
				var type = method.DeclaringType;
				var attr = BindingFlags.NonPublic | BindingFlags.Public | (method.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

				foreach (var info in type.GetProperties(attr))
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
				member.DeclaringType.IsGenericType &&
				member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static bool EqualsTo(this MemberInfo member1, MemberInfo member2)
		{
			if (ReferenceEquals(member1, member2))
				return true;

			if (member1 == null || member2 == null)
				return false;

			if (member1.Name == member2.Name)
			{
				if (member1.DeclaringType == member2.DeclaringType)
					return true;

				var isSubclass = member1.DeclaringType.IsSameOrParentOf(member2.DeclaringType);

				if (!isSubclass && member2.DeclaringType.IsSameOrParentOf(member1.DeclaringType))
				{
					isSubclass = true;

					var member = member1;
					member1 = member2;
					member2 = member;
				}

				if (isSubclass)
				{
					return member1 is PropertyInfo;
				}
			}

			return false;
		}

		#endregion
	}
}
