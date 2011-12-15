using System;
using System.Collections;
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
		#region GetAttributes

		static class CacheHelper<T>
		{
			public static readonly Dictionary<Type,T[]> TypeAttributes = new Dictionary<Type,T[]>();
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

			if (Common.Configuration.FilterOutBaseEqualAttributes)
			{
				foreach (var t in attrs)
					if (!list.Contains(t))
						list.Add(t);
			}
			else
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

			lock (CacheHelper<T>.TypeAttributes)
			{
				T[] attrs;

				if (!CacheHelper<T>.TypeAttributes.TryGetValue(type, out attrs))
				{
					var list = new List<object>();

					GetAttributesInternal(list, type);

					CacheHelper<T>.TypeAttributes.Add(type, attrs = list.OfType<T>().ToArray());
				}

				return attrs;
			}
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

		#endregion

		#region Static Members

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
		public static Type GetUnderlyingType([NotNull] this Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type.IsNullable()) type = type.GetGenericArguments()[0];
			if (type.IsEnum)       type = Enum.GetUnderlyingType(type);

			return type;
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
		public static bool IsSameOrParent([NotNull] Type parent, [NotNull] Type child)
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

		public static Type GetGenericType([NotNull] Type genericType, Type type)
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

		/// <summary>
		/// Searches for the method defined for a <see cref="System.Type"/>,
		/// using the specified name and binding flags.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public static MethodInfo GetMethod([NotNull] Type type, bool generic, string methodName, BindingFlags flags)
		{
			if (type == null) throw new ArgumentNullException("type");

			foreach (var method in type.GetMethods(flags))
				if (method.IsGenericMethodDefinition == generic && method.Name == methodName)
					return method;

			return null;
		}

		/// <summary>
		/// Searches for the methods defined for a <see cref="System.Type"/>,
		/// using the specified name and binding flags.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods defined 
		/// for the current Type that match the specified binding constraints.</returns>
		public static MethodInfo[] GetMethods(Type type, bool generic, BindingFlags flags)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetMethods(flags).Where(method => method.IsGenericMethodDefinition == generic).ToArray();
		}

		/// <summary>
		/// Searches for the method defined for a <see cref="System.Type"/>,
		/// using the specified name and binding flags.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="requiredParametersCount">Number of required (non optional)
		/// parameter types.</param>
		/// <param name="bindingFlags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <param name="parameterTypes">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public static MethodInfo GetMethod(
			Type          type,
			string        methodName,
			BindingFlags  bindingFlags,
			int           requiredParametersCount,
			params Type[] parameterTypes)
		{
			while (parameterTypes.Length >= requiredParametersCount)
			{
				var method = type.GetMethod(methodName, parameterTypes);

				if (null != method)
					return method;

				if (parameterTypes.Length == 0)
					break;

				Array.Resize(ref parameterTypes, parameterTypes.Length - 1);
			}

			return null;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="list">A <see cref="System.Object"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(object list)
		{
			var typeOfObject = typeof(object);

			if (list == null)
				return typeOfObject;

			if (list is Array)
				return list.GetType().GetElementType();

			var type = list.GetType();

			// object[] attrs = type.GetCustomAttributes(typeof(DefaultMemberAttribute), true);
			// string   itemMemberName = (attrs.Length == 0)? "Item": ((DefaultMemberAttribute)attrs[0]).MemberName;

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

			try
			{
				if (list is IList)
				{
					foreach (var o in (IList)list)
						if (o != null && o.GetType() != typeOfObject)
							return o.GetType();
				}
				else if (list is IEnumerable)
				{
					foreach (var o in (IEnumerable)list)
						if (o != null && o.GetType() != typeOfObject)
							return o.GetType();
				}
			}
			catch
			{
			}

			return typeOfObject;
		}

		///<summary>
		/// Gets the Type of a list item.
		///</summary>
		/// <param name="listType">A <see cref="System.Type"/> instance. </param>
		///<returns>The Type instance that represents the exact runtime type of a list item.</returns>
		public static Type GetListItemType(Type listType)
		{
			if (listType.IsGenericType)
			{
				var elementTypes = GetGenericArguments(listType, typeof(IList));

				if (elementTypes != null)
					return elementTypes[0];
			}

			if (IsSameOrParent(typeof(IList),       listType)
#if !SILVERLIGHT
				|| IsSameOrParent(typeof(ITypedList),  listType)
				|| IsSameOrParent(typeof(IListSource), listType)
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

		public static Type GetElementType(Type type)
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
					var eType = GetElementType(iType);

					if (eType != null)
						return eType;
				}
			}

			return GetElementType(type.BaseType);
		}

		/// <summary>
		/// Gets a value indicating whether a type can be used as a db primitive.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <returns> True, if the type parameter is a primitive type; otherwise, False.</returns>
		/// <remarks><see cref="System.String"/>. <see cref="Stream"/>. 
		/// <see cref="XmlReader"/>. <see cref="XmlDocument"/>. are specially handled by the library
		/// and, therefore, can be treated as scalar types.</remarks>
		public static bool IsScalar(Type type)
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
		public static Type[] GetGenericArguments(Type type, Type baseType)
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

		/// <summary>
		/// Substitutes the elements of an array of types for the type parameters
		/// of the current generic type definition and returns a Type object
		/// representing the resulting constructed type.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance.</param>
		/// <param name="typeArguments">An array of types to be substituted for
		/// the type parameters of the current generic type.</param>
		/// <returns>A Type representing the constructed type formed by substituting
		/// the elements of <paramref name="typeArguments"/> for the type parameters
		/// of the current generic type.</returns>
		/// <seealso cref="System.Type.MakeGenericType"/>
		public static Type TranslateGenericParameters(Type type, Type[] typeArguments)
		{
			// 'T paramName' case
			//
			if (type.IsGenericParameter)
				return typeArguments[type.GenericParameterPosition];

			// 'List<T> paramName' or something like that.
			//
			if (type.IsGenericType && type.ContainsGenericParameters)
			{
				var genArgs = type.GetGenericArguments();

				for (var i = 0; i < genArgs.Length; ++i)
					genArgs[i] = TranslateGenericParameters(genArgs[i], typeArguments);

				return type.GetGenericTypeDefinition().MakeGenericType(genArgs);
			}

			// Non-generic type.
			//
			return type;
		}

		public static bool CompareParameterTypes(Type goal, Type probe)
		{
			if (goal == probe)
				return true;

			if (goal.IsGenericParameter)
				return CheckConstraints(goal, probe);
			if (goal.IsGenericType && probe.IsGenericType)
				return CompareGenericTypes(goal, probe);

			return false;
		}

		public static bool CheckConstraints(Type goal, Type probe)
		{
			var constraints = goal.GetGenericParameterConstraints();

			for (var i = 0; i < constraints.Length; i++)
				if (!constraints[i].IsAssignableFrom(probe))
					return false;

			return true;
		}

		public static bool CompareGenericTypes(Type goal, Type probe)
		{
			var  genArgs =  goal.GetGenericArguments();
			var specArgs = probe.GetGenericArguments();
			var match    = (genArgs.Length == specArgs.Length);

			for (var i = 0; match && i < genArgs.Length; i++)
			{
				if (genArgs[i] == specArgs[i])
					continue;

				if (genArgs[i].IsGenericParameter)
					match = CheckConstraints(genArgs[i], specArgs[i]);
				else if (genArgs[i].IsGenericType && specArgs[i].IsGenericType)
					match = CompareGenericTypes(genArgs[i], specArgs[i]);
				else
					match = false;
			}

			return match;
		}

		public static PropertyInfo GetPropertyByMethod(MethodInfo method)
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

		public static Type GetMemberType(MemberInfo memberInfo)
		{
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Property    : return ((PropertyInfo)   memberInfo).PropertyType;
				case MemberTypes.Field       : return ((FieldInfo)      memberInfo).FieldType;
				case MemberTypes.Method      : return ((MethodInfo)     memberInfo).ReturnType;
				case MemberTypes.Constructor : return ((ConstructorInfo)memberInfo).DeclaringType;
			}

			throw new InvalidOperationException();
		}

		public static bool IsFloatType(Type type)
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

		public static bool IsIntegerType(Type type)
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

		public static bool IsNullableValueMember(MemberInfo member)
		{
			return
				member.Name == "Value" &&
				member.DeclaringType.IsGenericType &&
				member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static bool Equals(MemberInfo member1, MemberInfo member2)
		{
			if (ReferenceEquals(member1, member2))
				return true;

			if (member1 == null || member2 == null)
				return false;

			if (member1.Name == member2.Name)
			{
				if (member1.DeclaringType == member2.DeclaringType)
					return true;

				var isSubclass = IsSameOrParent(member1.DeclaringType, member2.DeclaringType);

				if (!isSubclass && IsSameOrParent(member2.DeclaringType, member1.DeclaringType))
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

		public static object GetDefaultValue(Type type)
		{
			var dtype  = typeof(GetDefaultValueHelper<>).MakeGenericType(type);
			var helper = (IGetDefaultValueHelper)Activator.CreateInstance(dtype);

			return helper.GetDefaultValue();
		}

		#endregion

		internal static string GetTypeFullName(Type type)
		{
			var name = type.FullName;

			if (type.IsGenericType)
			{
				name = name.Split('`')[0];

				foreach (var t in type.GetGenericArguments())
					name += "_" + GetTypeFullName(t).Replace('+', '_').Replace('.', '_');
			}

			return name;
		}
	}
}
