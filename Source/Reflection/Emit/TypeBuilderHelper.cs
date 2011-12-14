using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LinqToDB.Reflection.Emit
{
	using TypeBuilder.Builders;

	/// <summary>
	/// A wrapper around the <see cref="TypeBuilder"/> class.
	/// </summary>
	/// <include file="Examples.CS.xml" path='examples/emit[@name="Emit"]/*' />
	/// <include file="Examples.VB.xml" path='examples/emit[@name="Emit"]/*' />
	/// <seealso cref="System.Reflection.Emit.TypeBuilder">TypeBuilder Class</seealso>
	public class TypeBuilderHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="assemblyBuilder">Associated <see cref="AssemblyBuilderHelper"/>.</param>
		/// <param name="typeBuilder">A <see cref="TypeBuilder"/></param>
		public TypeBuilderHelper(AssemblyBuilderHelper assemblyBuilder, System.Reflection.Emit.TypeBuilder typeBuilder)
		{
			if (assemblyBuilder == null) throw new ArgumentNullException("assemblyBuilder");
			if (typeBuilder     == null) throw new ArgumentNullException("typeBuilder");

			_assembly    = assemblyBuilder;
			_typeBuilder = typeBuilder;
		}

		private readonly AssemblyBuilderHelper _assembly;
		/// <summary>
		/// Gets associated <see cref="AssemblyBuilderHelper"/>.
		/// </summary>
		public  AssemblyBuilderHelper  Assembly
		{
			get { return _assembly; }
		}

		private readonly System.Reflection.Emit.TypeBuilder _typeBuilder;
		/// <summary>
		/// Gets <see cref="System.Reflection.Emit.TypeBuilder"/>.
		/// </summary>
		public  System.Reflection.Emit.TypeBuilder  TypeBuilder
		{
			get { return _typeBuilder; }
		}

		/// <summary>
		/// Converts the supplied <see cref="TypeBuilderHelper"/> to a <see cref="TypeBuilder"/>.
		/// </summary>
		/// <param name="typeBuilder">The <see cref="TypeBuilderHelper"/>.</param>
		/// <returns>A <see cref="TypeBuilder"/>.</returns>
		public static implicit operator System.Reflection.Emit.TypeBuilder(TypeBuilderHelper typeBuilder)
		{
			if (typeBuilder == null) throw new ArgumentNullException("typeBuilder");

			return typeBuilder.TypeBuilder;
		}

		#region DefineMethod Overrides

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <param name="returnType">The return type of the method.</param>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(
			string name, MethodAttributes attributes, Type returnType, params Type[] parameterTypes)
		{
			return new MethodBuilderHelper(this, _typeBuilder.DefineMethod(name, attributes, returnType, parameterTypes));
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <param name="callingConvention">The <see cref="CallingConventions">calling convention</see> of the method.</param>
		/// <param name="returnType">The return type of the method.</param>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(
			string             name,
			MethodAttributes   attributes,
			CallingConventions callingConvention,
			Type               returnType,
			Type[]             parameterTypes)
		{
			return new MethodBuilderHelper(this, _typeBuilder.DefineMethod(
				name, attributes, callingConvention, returnType, parameterTypes));
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <param name="returnType">The return type of the method.</param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(string name, MethodAttributes attributes, Type returnType)
		{
			return new MethodBuilderHelper(
				this,
				_typeBuilder.DefineMethod(name, attributes, returnType, Type.EmptyTypes));
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(string name, MethodAttributes attributes)
		{
			return new MethodBuilderHelper(
				this,
				_typeBuilder.DefineMethod(name, attributes, typeof(void), Type.EmptyTypes));
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <returns>The defined method.</returns>
		/// <param name="callingConvention">The calling convention of the method.</param>
		public MethodBuilderHelper DefineMethod(
			string             name,
			MethodAttributes   attributes,
			CallingConventions callingConvention)
		{
			return new MethodBuilderHelper(
				this,
				_typeBuilder.DefineMethod(name, attributes, callingConvention));
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <param name="callingConvention">The <see cref="CallingConventions">calling convention</see> of the method.</param>
		/// <param name="genericArguments">Generic arguments of the method.</param>
		/// <param name="returnType">The return type of the method.</param>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		/// <returns>The defined generic method.</returns>
		public MethodBuilderHelper DefineGenericMethod(
			string             name,
			MethodAttributes   attributes,
			CallingConventions callingConvention,
			Type[]             genericArguments,
			Type               returnType,
			Type[]             parameterTypes)
		{
			return new MethodBuilderHelper(
				this,
				_typeBuilder.DefineMethod(name, attributes, callingConvention), genericArguments, returnType, parameterTypes);
		}


		private Dictionary<MethodInfo, MethodBuilder> _overriddenMethods;

		/// <summary>
		/// Retrieves the map of base type methods overridden by this type.
		/// </summary>
		public  Dictionary<MethodInfo, MethodBuilder>  OverriddenMethods
		{
			get { return _overriddenMethods ?? (_overriddenMethods = new Dictionary<MethodInfo,MethodBuilder>()); }
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="methodInfoDeclaration">The method whose declaration is to be used.</param>
		/// <param name="attributes">The attributes of the method. </param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(
			string           name,
			MethodInfo       methodInfoDeclaration,
			MethodAttributes attributes)
		{
			if (methodInfoDeclaration == null) throw new ArgumentNullException("methodInfoDeclaration");

			MethodBuilderHelper method;
			ParameterInfo[]     pi         = methodInfoDeclaration.GetParameters();
			Type[]              parameters = new Type[pi.Length];

			for (int i = 0; i < pi.Length; i++)
				parameters[i] = pi[i].ParameterType;

			if (methodInfoDeclaration.ContainsGenericParameters)
			{
				method = DefineGenericMethod(
					name,
					attributes,
					methodInfoDeclaration.CallingConvention,
					methodInfoDeclaration.GetGenericArguments(),
					methodInfoDeclaration.ReturnType,
					parameters);
			}
			else
			{
				method = DefineMethod(
					name,
					attributes,
					methodInfoDeclaration.CallingConvention,
					methodInfoDeclaration.ReturnType,
					parameters);
			}

			// Compiler overrides methods only for interfaces. We do the same.
			// If we wanted to override virtual methods, then methods should've had
			// MethodAttributes.VtableLayoutMask attribute
			// and the following condition should've been used below:
			// if ((methodInfoDeclaration is FakeMethodInfo) == false)
			//
			if (methodInfoDeclaration.DeclaringType.IsInterface
#if !SILVERLIGHT
				&& !(methodInfoDeclaration is FakeMethodInfo)
#endif
				)
			{
				OverriddenMethods.Add(methodInfoDeclaration, method.MethodBuilder);
				_typeBuilder.DefineMethodOverride(method.MethodBuilder, methodInfoDeclaration);
			}

			method.OverriddenMethod = methodInfoDeclaration;

			for (int i = 0; i < pi.Length; i++)
				method.MethodBuilder.DefineParameter(i + 1, pi[i].Attributes, pi[i].Name);

			return method;
		}

		/// <summary>
		/// Adds a new method to the class, with the given name and method signature.
		/// </summary>
		/// <param name="name">The name of the method. name cannot contain embedded nulls. </param>
		/// <param name="methodInfoDeclaration">The method whose declaration is to be used.</param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(string name, MethodInfo methodInfoDeclaration)
		{
			return DefineMethod(name, methodInfoDeclaration, MethodAttributes.Virtual);
		}

		/// <summary>
		/// Adds a new private method to the class.
		/// </summary>
		/// <param name="methodInfoDeclaration">The method whose declaration is to be used.</param>
		/// <returns>The defined method.</returns>
		public MethodBuilderHelper DefineMethod(MethodInfo methodInfoDeclaration)
		{
			if (methodInfoDeclaration == null) throw new ArgumentNullException("methodInfoDeclaration");

			var isInterface = methodInfoDeclaration.DeclaringType.IsInterface;
#if SILVERLIGHT
			var isFake      = false;
#else
			var isFake      = methodInfoDeclaration is FakeMethodInfo;
#endif

			var name = isInterface && !isFake?
				methodInfoDeclaration.DeclaringType.FullName + "." + methodInfoDeclaration.Name:
				methodInfoDeclaration.Name;

			var attributes = 
				MethodAttributes.Virtual |
				MethodAttributes.HideBySig |
				MethodAttributes.PrivateScope |
				methodInfoDeclaration.Attributes & MethodAttributes.SpecialName;

			if (isInterface && !isFake)
				attributes |= MethodAttributes.Private;
			else if ((attributes & MethodAttributes.SpecialName) != 0)
				attributes |= MethodAttributes.Public;
			else
				attributes |= methodInfoDeclaration.Attributes & 
					(MethodAttributes.Public | MethodAttributes.Private);

			return DefineMethod(name, methodInfoDeclaration, attributes);
		}

		#endregion

		/// <summary>
		/// Creates a Type object for the class.
		/// </summary>
		/// <returns>Returns the new Type object for this class.</returns>
		public Type Create()
		{
			return TypeBuilder.CreateType();
		}

		/// <summary>
		/// Sets a custom attribute using a custom attribute type.
		/// </summary>
		/// <param name="attributeType">Attribute type.</param>
		public void SetCustomAttribute(Type attributeType)
		{
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			ConstructorInfo        ci        = attributeType.GetConstructor(Type.EmptyTypes);
			CustomAttributeBuilder caBuilder = new CustomAttributeBuilder(ci, new object[0]);

			_typeBuilder.SetCustomAttribute(caBuilder);
		}

		/// <summary>
		/// Sets a custom attribute using a custom attribute type
		/// and named properties.
		/// </summary>
		/// <param name="attributeType">Attribute type.</param>
		/// <param name="properties">Named properties of the custom attribute.</param>
		/// <param name="propertyValues">Values for the named properties of the custom attribute.</param>
		public void SetCustomAttribute(
			Type           attributeType,
			PropertyInfo[] properties,
			object[]       propertyValues)
		{
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			ConstructorInfo        ci        = attributeType.GetConstructor(Type.EmptyTypes);
			CustomAttributeBuilder caBuilder = new CustomAttributeBuilder(
				ci, new object[0], properties, propertyValues);

			_typeBuilder.SetCustomAttribute(caBuilder);
		}

		/// <summary>
		/// Sets a custom attribute using a custom attribute type
		/// and named property.
		/// </summary>
		/// <param name="attributeType">Attribute type.</param>
		/// <param name="propertyName">A named property of the custom attribute.</param>
		/// <param name="propertyValue">Value for the named property of the custom attribute.</param>
		public void SetCustomAttribute(
			Type   attributeType,
			string propertyName,
			object propertyValue)
		{
			SetCustomAttribute(
				attributeType,
				new PropertyInfo[] { attributeType.GetProperty(propertyName) },
				new object[]       { propertyValue } );
		}

		private ConstructorBuilderHelper _typeInitializer;
		/// <summary>
		/// Gets the initializer for this type.
		/// </summary>
		public ConstructorBuilderHelper TypeInitializer
		{
			get 
			{
				if (_typeInitializer == null)
					_typeInitializer = new ConstructorBuilderHelper(this, _typeBuilder.DefineTypeInitializer());

				return _typeInitializer;
			}
		}

		/// <summary>
		/// Returns true if the initializer for this type has a body.
		/// </summary>
		public bool IsTypeInitializerDefined
		{
			get { return _typeInitializer != null; }
		}

		private ConstructorBuilderHelper _defaultConstructor;
		/// <summary>
		/// Gets the default constructor for this type.
		/// </summary>
		public  ConstructorBuilderHelper  DefaultConstructor
		{
			get 
			{
				if (_defaultConstructor == null)
				{
					ConstructorBuilder builder = _typeBuilder.DefineConstructor(
						MethodAttributes.Public,
						CallingConventions.Standard,
						Type.EmptyTypes);

					_defaultConstructor = new ConstructorBuilderHelper(this, builder);
				}

				return _defaultConstructor;
			}
		}

		/// <summary>
		/// Returns true if the default constructor for this type has a body.
		/// </summary>
		public bool IsDefaultConstructorDefined
		{
			get { return _defaultConstructor != null; }
		}

		private ConstructorBuilderHelper _initConstructor;
		/// <summary>
		/// Gets the init context constructor for this type.
		/// </summary>
		public  ConstructorBuilderHelper  InitConstructor
		{
			get 
			{
				if (_initConstructor == null)
				{
					ConstructorBuilder builder = _typeBuilder.DefineConstructor(
						MethodAttributes.Public, 
						CallingConventions.Standard,
						new Type[] { typeof(InitContext) });

					_initConstructor = new ConstructorBuilderHelper(this, builder);
				}

				return _initConstructor;
			}
		}

		/// <summary>
		/// Returns true if a constructor with parameter of <see cref="InitContext"/> for this type has a body.
		/// </summary>
		public bool IsInitConstructorDefined
		{
			get { return _initConstructor != null; }
		}

		/// <summary>
		/// Adds a new field to the class, with the given name, attributes and field type.
		/// </summary>
		/// <param name="fieldName">The name of the field. <paramref name="fieldName"/> cannot contain embedded nulls.</param>
		/// <param name="type">The type of the field.</param>
		/// <param name="attributes">The attributes of the field.</param>
		/// <returns>The defined field.</returns>
		public FieldBuilder DefineField(
			string          fieldName,
			Type            type,
			FieldAttributes attributes)
		{
			return _typeBuilder.DefineField(fieldName, type, attributes);
		}

		#region DefineConstructor Overrides

		/// <summary>
		/// Adds a new public constructor to the class, with the given parameters.
		/// </summary>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		/// <returns>The defined constructor.</returns>
		public ConstructorBuilderHelper DefinePublicConstructor(params Type[] parameterTypes)
		{
			return new ConstructorBuilderHelper(
				this,
				_typeBuilder.DefineConstructor(
					MethodAttributes.Public, CallingConventions.Standard, parameterTypes));
		}

		/// <summary>
		/// Adds a new constructor to the class, with the given attributes and parameters.
		/// </summary>
		/// <param name="attributes">The attributes of the field.</param>
		/// <param name="callingConvention">The <see cref="CallingConventions">calling convention</see> of the method.</param>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		/// <returns>The defined constructor.</returns>
		public ConstructorBuilderHelper DefineConstructor(
			MethodAttributes   attributes,
			CallingConventions callingConvention,
			params Type[]      parameterTypes)
		{
			return new ConstructorBuilderHelper(
				this,
				_typeBuilder.DefineConstructor(attributes, callingConvention, parameterTypes));
		}

		#endregion

		#region DefineNestedType Overrides

		/// <summary>
		/// Defines a nested type given its name..
		/// </summary>
		/// <param name="name">The short name of the type.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.TypeBuilder.DefineNestedType(string)">
		/// TypeBuilder.DefineNestedType Method</seealso>
		public TypeBuilderHelper DefineNestedType(string name)
		{
			return new TypeBuilderHelper(_assembly, _typeBuilder.DefineNestedType(name));
		}

		/// <summary>
		/// Defines a public nested type given its name and the type that it extends.
		/// </summary>
		/// <param name="name">The short name of the type.</param>
		/// <param name="parent">The type that the nested type extends.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.TypeBuilder.DefineNestedType(string,TypeAttributes,Type)">
		/// TypeBuilder.DefineNestedType Method</seealso>
		public TypeBuilderHelper DefineNestedType(string name, Type parent)
		{
			return new TypeBuilderHelper(
				_assembly,
				_typeBuilder.DefineNestedType(name, TypeAttributes.NestedPublic, parent));
		}

		/// <summary>
		/// Defines a nested type given its name, attributes, and the type that it extends.
		/// </summary>
		/// <param name="name">The short name of the type.</param>
		/// <param name="attributes">The attributes of the type.</param>
		/// <param name="parent">The type that the nested type extends.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.TypeBuilder.DefineNestedType(string,TypeAttributes,Type)">
		/// TypeBuilder.DefineNestedType Method</seealso>
		public TypeBuilderHelper DefineNestedType(
			string         name,
			TypeAttributes attributes,
			Type           parent)
		{
			return new TypeBuilderHelper(
				_assembly,
				_typeBuilder.DefineNestedType(name, attributes, parent));
		}

		/// <summary>
		/// Defines a public nested type given its name, the type that it extends, and the interfaces that it implements.
		/// </summary>
		/// <param name="name">The short name of the type.</param>
		/// <param name="parent">The type that the nested type extends.</param>
		/// <param name="interfaces">The interfaces that the nested type implements.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.TypeBuilder.DefineNestedType(string,TypeAttributes,Type,Type[])">
		/// TypeBuilder.DefineNestedType Method</seealso>
		public TypeBuilderHelper DefineNestedType(
			string        name,
			Type          parent,
			params Type[] interfaces)
		{
			return new TypeBuilderHelper(
				_assembly,
				_typeBuilder.DefineNestedType(name, TypeAttributes.NestedPublic, parent, interfaces));
		}

		/// <summary>
		/// Defines a nested type given its name, attributes, the type that it extends, and the interfaces that it implements.
		/// </summary>
		/// <param name="name">The short name of the type.</param>
		/// <param name="attributes">The attributes of the type.</param>
		/// <param name="parent">The type that the nested type extends.</param>
		/// <param name="interfaces">The interfaces that the nested type implements.</param>
		/// <returns>Returns the created <see cref="TypeBuilderHelper"/>.</returns>
		/// <seealso cref="System.Reflection.Emit.ModuleBuilder.DefineType(string,TypeAttributes,Type,Type[])">ModuleBuilder.DefineType Method</seealso>
		public TypeBuilderHelper DefineNestedType(
			string         name,
			TypeAttributes attributes,
			Type           parent,
			params         Type[] interfaces)
		{
			return new TypeBuilderHelper(
				_assembly,
				_typeBuilder.DefineNestedType(name, attributes, parent, interfaces));
		}

		#endregion

	}
}
