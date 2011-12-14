using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using JetBrains.Annotations;

namespace LinqToDB.Reflection
{
	using TypeBuilder;

	/// <summary>
	/// A wrapper around the <see cref="Type"/> class.
	/// </summary>
	[DebuggerDisplay("Type = {Type}")]
	public class TypeHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeHelper"/> class.
		/// </summary>
		/// <param name="type">The Type to wrap.</param>
		public TypeHelper(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			Type = type;
		}

		/// <summary>
		/// Gets associated Type.
		/// </summary>
		public Type Type { get; private set; }

		/// <summary>
		/// Converts the supplied <see cref="Type"/> to a <see cref="TypeHelper"/>.
		/// </summary>
		/// <param name="type">The Type.</param>
		/// <returns>A TypeHelper.</returns>
		public static implicit operator TypeHelper(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return new TypeHelper(type);
		}

		/// <summary>
		/// Converts the supplied <see cref="TypeHelper"/> to a <see cref="Type"/>.
		/// </summary>
		/// <param name="typeHelper">The TypeHelper.</param>
		/// <returns>A Type.</returns>
		public static implicit operator Type(TypeHelper typeHelper)
		{
			if (typeHelper == null) throw new ArgumentNullException("typeHelper");

			return typeHelper.Type;
		}

		#region GetAttributes

		/// <summary>
		/// Returns an array of custom attributes identified by <b>Type</b>.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain
		/// to find the attributes.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return Type.GetCustomAttributes(attributeType, inherit);
		}

		/// <summary>
		/// Returns an array of custom attributes identified by <b>Type</b>
		/// including attribute's inheritance chain.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(Type attributeType)
		{
			return Type.GetCustomAttributes(attributeType, true);
		}


		/// <summary>
		/// Returns an array of all of the custom attributes.
		/// </summary>
		/// <param name="inherit">Specifies whether to search this member's inheritance chain
		/// to find the attributes.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes(bool inherit)
		{
			return Type.GetCustomAttributes(inherit);
		}

		/// <summary>
		/// Returns an array of all of the custom attributes including attributes' inheritance chain.
		/// </summary>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetCustomAttributes()
		{
			return Type.GetCustomAttributes(true);
		}

		/// <summary>
		/// Returns an array of all custom attributes identified by <b>Type</b> including type's
		/// inheritance chain.
		/// </summary>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetAttributes(Type attributeType)
		{
			return GetAttributes(Type, attributeType);
		}

		/// <summary>
		/// Returns an array of all custom attributes including type's inheritance chain.
		/// </summary>
		/// <returns>An array of custom attributes defined on this reflected member,
		/// or an array with zero (0) elements if no attributes are defined.</returns>
		public object[] GetAttributes()
		{
			return GetAttributesInternal();
		}

		#region Attributes cache

		object[] GetAttributesInternal()
		{
			lock (_typeAttributes)
			{
				var key = Type.FullName;

				object[] attrs;

				if (!_typeAttributes.TryGetValue(key, out attrs))
				{
					var list = new List<object>();

					GetAttributesInternal(list, Type);

					_typeAttributes.Add(key, attrs = list.ToArray());
				}

				return attrs;
			}
		}

		static readonly Dictionary<Type, object[]> _typeAttributesTopInternal = new Dictionary<Type, object[]>(10);

		static void GetAttributesInternal(List<object> list, Type type)
		{
			object[] attrs;

			if (_typeAttributesTopInternal.TryGetValue(type, out attrs))
				list.AddRange(attrs);
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

		static readonly Dictionary<string, object[]> _typeAttributes = new Dictionary<string, object[]>(10);

		#endregion

		/// <summary>
		/// Returns an array of custom attributes applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>An array of custom attributes applied to this type,
		/// or an array with zero (0) elements if no attributes have been applied.</returns>
		public static object[] GetAttributes(Type type, Type attributeType)
		{
			if (type          == null) throw new ArgumentNullException("type");
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			lock (_typeAttributes)
			{
				var key   = type.FullName + "#" + attributeType.FullName;

				object[] attrs;

				if (!_typeAttributes.TryGetValue(key, out attrs))
				{
					var list = new List<object>();

					GetAttributesInternal(list, type);

					for (var i = 0; i < list.Count; i++)
						if (attributeType.IsInstanceOfType(list[i]) == false)
							list.RemoveAt(i--);

					_typeAttributes.Add(key, attrs = list.ToArray());
				}

				return attrs;
			}
		}

		/// <summary>
		/// Retrieves a custom attribute applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <param name="attributeType">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>A reference to the first custom attribute of type <paramref name="attributeType"/>
		/// that is applied to element, or null if there is no such attribute.</returns>
		public static Attribute GetFirstAttribute(Type type, Type attributeType)
		{
			var attrs = new TypeHelper(type).GetAttributes(attributeType);

			return attrs.Length > 0? (Attribute)attrs[0]: null;
		}

		/// <summary>
		/// Retrieves a custom attribute applied to a type.
		/// </summary>
		/// <param name="type">A type instance.</param>
		/// <typeparam name="T">The type of attribute to search for.
		/// Only attributes that are assignable to this type are returned.</param>
		/// <returns>A reference to the first custom attribute of type attributeType
		/// that is applied to element, or null if there is no such attribute.</returns>
		public static T GetFirstAttribute<T>(Type type) where T : Attribute
		{
			var attrs = new TypeHelper(type).GetAttributes(typeof(T));

			return attrs.Length > 0? (T)attrs[0]: null;
		}

		#endregion

		#region Property Wrappers

		/// <summary>
		/// Gets the fully qualified name of the Type, including the namespace of the Type.
		/// </summary>
		public string FullName
		{
			get { return Type.FullName; }
		}

		/// <summary>
		/// Gets the name of the Type.
		/// </summary>
		public string Name
		{
			get { return Type.Name; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is abstract and must be overridden.
		/// </summary>
		public bool IsAbstract
		{
			get { return Type.IsAbstract; }
		}

		/// <summary>
		/// Gets a value indicating whether the System.Type is an array.
		/// </summary>
		public bool IsArray
		{
			get { return Type.IsArray; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is a value type.
		/// </summary>
		public bool IsValueType
		{
			get { return Type.IsValueType; }
		}

		/// <summary>
		/// Gets a value indicating whether the Type is a class; that is, not a value type or interface.
		/// </summary>
		public bool IsClass
		{
			get { return Type.IsClass; }
		}

		/// <summary>
		/// Gets a value indicating whether the System.Type is an interface; that is, not a class or a value type.
		/// </summary>
		public bool IsInterface
		{
			get { return Type.IsInterface; }
		}

		/// <summary>
		/// Indicates whether the Type is serializable.
		/// </summary>
		public bool IsSerializable
		{
			get
			{
#if SILVERLIGHT
				return false;
#else
				return Type.IsSerializable;
#endif
			}
		}

		#endregion

		#region GetMethods

		/// <summary>
		/// Returns all the methods of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetMethods()
		{
			return Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Returns all the public methods of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all the public methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetPublicMethods()
		{
			return Type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the methods defined for the current Type,
		/// using the specified binding constraints.
		/// </summary>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods defined 
		/// for the current Type that match the specified binding constraints.</returns>
		public MethodInfo[] GetMethods(BindingFlags flags)
		{
			return Type.GetMethods(flags);
		}

		/// <summary>
		/// Returns all the generic or non-generic methods of the current Type.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetMethods(bool generic)
		{
			return GetMethods(Type, generic, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Returns all the public and non-generic methods of the current Type.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all the public methods 
		/// defined for the current Type.</returns>
		public MethodInfo[] GetPublicMethods(bool generic)
		{
			return GetMethods(Type, generic, BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the generic methods defined for the current Type,
		/// using the specified binding constraints.
		/// </summary>
		/// <param name="generic">True to return all generic methods, false to return all non-generic.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing all methods defined 
		/// for the current Type that match the specified binding constraints.</returns>
		public MethodInfo[] GetMethods(bool generic, BindingFlags flags)
		{
			return GetMethods(Type, generic, flags);
		}

		#endregion

		#region GetMethod

		/// <summary>
		/// Searches for the specified instance method (public or non-public), using the specified name.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified name, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(string methodName)
		{
			return Type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Searches for the specified public instance method, using the specified name.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified name, if found; otherwise, null.</returns>
		public MethodInfo GetPublicMethod(string methodName)
		{
			return Type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the specified method, using the specified name and binding flags.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(string methodName, BindingFlags flags)
		{
			return Type.GetMethod(methodName, flags);
		}

		/// <summary>
		/// Searches for the specified public instance method, using the specified name.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetPublicMethod(string methodName, params Type[] types)
		{
			return Type.GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public,
				null,
				types,
				null);
		}

		/// <summary>
		/// Searches for the specified instance method (public or non-public),
		/// using the specified name and argument types.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(string methodName, params Type[] types)
		{
			return Type.GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				types,
				null);
		}

		/// <summary>
		/// Searches for the specified method, using the specified name,
		/// binding flags and argument types.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(string methodName, BindingFlags flags, params Type[] types)
		{
			return Type.GetMethod(methodName, flags, null, types, null);
		}

		/// <summary>
		/// Searches for the specified instance method (public or non-public), using the specified name.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(bool generic, string methodName)
		{
			return GetMethod(Type, generic, methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		/// <summary>
		/// Searches for the specified public instance method, using the specified name.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetPublicMethod(bool generic, string methodName)
		{
			return GetMethod(Type, generic, methodName,
				BindingFlags.Instance | BindingFlags.Public);
		}

		/// <summary>
		/// Searches for the specified method, using the specified name and binding flags.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(bool generic, string methodName, BindingFlags flags)
		{
			return GetMethod(Type, generic, methodName, flags);
		}

		/// <summary>
		/// Searches for the specified public instance method, using the specified name and argument types.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetPublicMethod(bool generic, string methodName, params Type[] types)
		{
			return Type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

		/// <summary>
		/// Searches for the specified instance method (public or non-public),
		/// using the specified name and argument types.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(bool generic, string methodName, params Type[] types)
		{
			return Type.GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

		/// <summary>
		/// Searches for the specified method using the specified name, binding flags and argument types.
		/// </summary>
		/// <param name="methodName">The String containing the name of the method to get.</param>
		/// <param name="generic">True to search only for a generic method, or
		/// False to search only for non-generic method.</param>
		/// <param name="types">An array of <see cref="System.Type"/> objects representing
		/// the number, order, and type of the parameters for the method to get.-or-
		/// An empty array of the type <see cref="System.Type"/> (for example, <see cref="System.Type.EmptyTypes"/>)
		/// to get a method that takes no parameters.</param>
		/// <param name="flags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public MethodInfo GetMethod(bool generic, string methodName, BindingFlags flags, params Type[] types)
		{
			return Type.GetMethod(methodName,
				flags,
				generic ? GenericBinder.Generic : GenericBinder.NonGeneric,
				types, null);
		}

		#endregion

		#region GetFields

		/// <summary>
		/// Returns all the public fields of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="FieldInfo"/> objects representing
		/// all the public fields defined for the current Type.</returns>
		public FieldInfo[] GetFields()
		{
			return Type.GetFields();
		}

		/// <summary>
		/// Searches for the fields of the current Type, using the specified binding constraints.
		/// </summary>
		/// <param name="bindingFlags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="FieldInfo"/> objects representing
		/// all fields of the current Type
		/// that match the specified binding constraints.</returns>
		public FieldInfo[] GetFields(BindingFlags bindingFlags)
		{
			return Type.GetFields(bindingFlags);
		}

		/// <summary>
		/// Searches for the public field with the specified name.
		/// </summary>
		/// <param name="name">The String containing the name of the public field to get.</param>
		/// <returns>A <see cref="PropertyInfo"/> object representing the public field with the specified name,
		/// if found; otherwise, a null reference.</returns>
		public FieldInfo GetField(string name)
		{
			return Type.GetField(
				name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		#endregion

		#region GetProperties

		/// <summary>
		/// Returns all the public properties of the current Type.
		/// </summary>
		/// <returns>An array of <see cref="PropertyInfo"/> objects representing
		/// all public properties of the current Type.</returns>
		public PropertyInfo[] GetProperties()
		{
			return Type.GetProperties();
		}

		/// <summary>
		/// Searches for the properties of the current Type, using the specified binding constraints.
		/// </summary>
		/// <param name="bindingFlags">A bitmask comprised of one or more <see cref="BindingFlags"/> 
		/// that specify how the search is conducted.</param>
		/// <returns>An array of <see cref="PropertyInfo"/> objects representing
		/// all properties of the current Type
		/// that match the specified binding constraints.</returns>
		public PropertyInfo[] GetProperties(BindingFlags bindingFlags)
		{
			return Type.GetProperties(bindingFlags);
		}

		/// <summary>
		/// Searches for the public property with the specified name.
		/// </summary>
		/// <param name="name">The String containing the name of the public property to get.</param>
		/// <returns>A <see cref="PropertyInfo"/> object representing the public property with the specified name,
		/// if found; otherwise, a null reference.</returns>
		public PropertyInfo GetProperty(string name)
		{
			return Type.GetProperty(
				name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		#endregion

		#region GetInterfaces

		/*
		private Type[] _interfaces;

		/// <summary>
		/// Gets all the interfaces implemented or inherited by the current <see cref="Type"/>.
		/// </summary>
		/// <returns>An array of Type objects representing all the interfaces implemented or
		/// inherited by the current Type,
		/// if found; otherwise, an empty array.</returns>
		public Type[] GetInterfaces()
		{
			if (_interfaces == null)
				_interfaces = _type.GetInterfaces();

			return _interfaces;
		}

		/// <summary>
		/// Gets a specific interface implemented or inherited by the current <see cref="Type"/>.
		/// </summary>
		/// <param name="interfaceType">The type of the interface to get.</param>
		/// <returns>A Type object representing the interface of the specified type, if found;
		///  otherwise, a null reference (Nothing in Visual Basic).</returns>
		public Type GetInterface(Type interfaceType)
		{
			foreach (Type intf in GetInterfaces())
				if (intf == interfaceType)
					return null;

			_type.IsSubclassOf(interfaceType);

			return null;
		}
		*/

		/// <summary>
		/// Returns an interface mapping for the current <see cref="Type"/>.
		/// </summary>
		/// <param name="interfaceType">The <see cref="System.Type"/>
		/// of the interface of which to retrieve a mapping.</param>
		/// <returns>An <see cref="InterfaceMapping"/> object representing the interface
		/// mapping for <paramref name="interfaceType"/>.</returns>
		public InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			return Type.GetInterfaceMap(interfaceType);
		}

		#endregion

		#region GetConstructor

		/// <summary>
		/// Searches for a public instance constructor whose parameters match
		/// the types in the specified array.
		/// </summary>
		/// <param name="types">An array of Type objects representing the number,
		/// order, and type of the parameters for the constructor to get.</param>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the
		/// public instance constructor whose parameters match the types in
		/// the parameter type array, if found; otherwise, a null reference.</returns>
		public ConstructorInfo GetPublicConstructor(params Type[] types)
		{
			return Type.GetConstructor(types);
		}

		/// <summary>
		/// Searches for an instance constructor (public or non-public) whose
		/// parameters match the types in the specified array.
		/// </summary>
		/// <param name="parameterType">Type object representing type of the
		/// parameter for the constructor to get.</param>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor
		///  whose parameters match the types in the parameter type array, if found;
		/// otherwise, a null reference.</returns>
		public ConstructorInfo GetConstructor(Type parameterType)
		{
			return GetConstructor(Type, parameterType);
		}

		/// <summary>
		/// Searches for an instance constructor (public or non-public) whose
		/// parameters match the types in the specified array.
		/// </summary>
		/// <param name="type">An instance of <see cref="System.Type"/> to search constructor for.</param>
		/// <param name="types">An array of Type objects representing the number,
		/// order, and type of the parameters for the constructor to get.</param>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor
		///  whose parameters match the types in the parameter type array, if found;
		/// otherwise, a null reference.</returns>
		public static ConstructorInfo GetConstructor(Type type, params Type[] types)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				types,
				null);
		}

		/// <summary>
		/// Searches for a public default constructor.
		/// </summary>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor.</returns>
		public ConstructorInfo GetPublicDefaultConstructor()
		{
			return Type.GetConstructor(Type.EmptyTypes);
		}

		/// <summary>
		/// Searches for a default constructor.
		/// </summary>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor.</returns>
		public ConstructorInfo GetDefaultConstructor()
		{
			return GetDefaultConstructor(Type);
		}

		/// <summary>
		/// Searches for a default constructor.
		/// </summary>
		/// <param name="type">An instance of <see cref="System.Type"/> to search constructor for.</param>
		/// <returns>A <see cref="ConstructorInfo"/> object representing the constructor.</returns>
		public static ConstructorInfo GetDefaultConstructor(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);
		}

		/// <summary>
		/// Searches for a public constructors.
		/// </summary>
		/// <returns>An array of <see cref="ConstructorInfo"/> objects
		/// representing all the type public constructors, if found; otherwise, an empty array.</returns>
		public ConstructorInfo[] GetPublicConstructors()
		{
			return Type.GetConstructors();
		}

		/// <summary>
		/// Searches for all constructors (except type constructors).
		/// </summary>
		/// <returns>An array of <see cref="ConstructorInfo"/> objects
		/// representing all the type constructors, if found; otherwise, an empty array.</returns>
		public ConstructorInfo[] GetConstructors()
		{
			return Type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
		public static bool IsNullable(Type type)
		{
			while (type.IsArray)
				type = type.GetElementType();

			return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
		}

		public static bool IsNullableType(Type type)
		{
			return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
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
		public static Type GetUnderlyingType(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (IsNullableType(type))
				type = type.GetGenericArguments()[0];

			if (type.IsEnum)
				type = Enum.GetUnderlyingType(type);

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

		public static object[] GetPropertyParameters(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");

			var attrs = propertyInfo.GetCustomAttributes(typeof(ParameterAttribute), true);

			if (attrs != null && attrs.Length > 0)
				return ((ParameterAttribute)attrs[0]).Parameters;

			return null;
		}

		/// <summary>
		/// Searches for the property defined for a <see cref="System.Type"/>,
		/// using the specified name and parameter types.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> instance. </param>
		/// <param name="propertyName">The String containing the name of the method to get.</param>
		/// <param name="types">An array of Type objects representing the number,
		/// order, and type of the parameters for the constructor to get.</param>
		/// <param name="returnType">The property return <see cref="System.Type"/>. </param>
		/// <returns>A <see cref="MethodInfo"/> object representing the method
		/// that matches the specified requirements, if found; otherwise, null.</returns>
		public static PropertyInfo GetPropertyInfo(
			Type type, string propertyName, Type returnType, Type[] types)
		{
			if (type == null) throw new ArgumentNullException("type");

			return type.GetProperty(
				propertyName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				returnType,
				types,
				null);
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
			if (IsNullableType(type))
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
			if (IsNullableType(type))
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
