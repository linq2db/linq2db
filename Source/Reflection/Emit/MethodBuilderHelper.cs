using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace LinqToDB.Reflection.Emit
{
	/// <summary>
	/// A wrapper around the <see cref="MethodBuilder"/> class.
	/// </summary>
	/// <include file="Examples.CS.xml" path='examples/emit[@name="Emit"]/*' />
	/// <include file="Examples.VB.xml" path='examples/emit[@name="Emit"]/*' />
	/// <seealso cref="System.Reflection.Emit.MethodBuilder">MethodBuilder Class</seealso>
	public class MethodBuilderHelper : MethodBuilderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MethodBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="typeBuilder">Associated <see cref="TypeBuilderHelper"/>.</param>
		/// <param name="methodBuilder">A <see cref="MethodBuilder"/></param>
		public MethodBuilderHelper(TypeBuilderHelper typeBuilder, MethodBuilder methodBuilder)
			: base(typeBuilder)
		{
			if (methodBuilder == null) throw new ArgumentNullException("methodBuilder");

			_methodBuilder = methodBuilder;
		}

		/// <summary>
		/// Sets a custom attribute using a custom attribute type.
		/// </summary>
		/// <param name="attributeType">Attribute type.</param>
		public void SetCustomAttribute(Type attributeType)
		{
			if (attributeType == null) throw new ArgumentNullException("attributeType");

			ConstructorInfo ci = attributeType.GetConstructor(System.Type.EmptyTypes);
			CustomAttributeBuilder caBuilder = new CustomAttributeBuilder(ci, new object[0]);

			_methodBuilder.SetCustomAttribute(caBuilder);
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

			ConstructorInfo ci = attributeType.GetConstructor(System.Type.EmptyTypes);
			CustomAttributeBuilder caBuilder = new CustomAttributeBuilder(
				ci, new object[0], properties, propertyValues);

			_methodBuilder.SetCustomAttribute(caBuilder);
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
				new object[] { propertyValue });
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="typeBuilder">Associated <see cref="TypeBuilderHelper"/>.</param>
		/// <param name="methodBuilder">A <see cref="MethodBuilder"/></param>
		/// <param name="genericArguments">Generic arguments of the method.</param>
		/// <param name="returnType">The return type of the method.</param>
		/// <param name="parameterTypes">The types of the parameters of the method.</param>
		internal MethodBuilderHelper(
			TypeBuilderHelper  typeBuilder,
			MethodBuilder      methodBuilder,
			Type[]             genericArguments,
			Type               returnType,
			Type[]             parameterTypes
			)
			: base(typeBuilder)
		{
			if (methodBuilder    == null) throw new ArgumentNullException("methodBuilder");
			if (genericArguments == null) throw new ArgumentNullException("genericArguments");

			_methodBuilder = methodBuilder;

			var genArgNames = genericArguments.Select(t => t.Name).ToArray();
			var genParams   = methodBuilder.DefineGenericParameters(genArgNames);

			// Copy parameter constraints.
			//
			List<Type> interfaceConstraints = null;

			for (var i = 0; i < genParams.Length; i++)
			{
				genParams[i].SetGenericParameterAttributes(genericArguments[i].GenericParameterAttributes);

				foreach (var constraint in genericArguments[i].GetGenericParameterConstraints())
				{
					if (constraint.IsClass)
						genParams[i].SetBaseTypeConstraint(constraint);
					else
					{
						if (interfaceConstraints == null)
							interfaceConstraints = new List<Type>();
						interfaceConstraints.Add(constraint);
					}
				}

				if (interfaceConstraints != null && interfaceConstraints.Count != 0)
				{
					genParams[i].SetInterfaceConstraints(interfaceConstraints.ToArray());
					interfaceConstraints.Clear();
				}
			}

			// When a method contains a generic parameter we need to replace all
			// generic types from methodInfoDeclaration with local ones.
			//
			for (var i = 0; i < parameterTypes.Length; i++)
				parameterTypes[i] = TypeHelper.TranslateGenericParameters(parameterTypes[i], genParams);

			methodBuilder.SetParameters(parameterTypes);
			methodBuilder.SetReturnType(TypeHelper.TranslateGenericParameters(returnType, genParams));
		}

		private readonly MethodBuilder _methodBuilder;
		/// <summary>
		/// Gets MethodBuilder.
		/// </summary>
		public MethodBuilder MethodBuilder
		{
			get { return _methodBuilder; }
		}

		/// <summary>
		/// Converts the supplied <see cref="MethodBuilderHelper"/> to a <see cref="MethodBuilder"/>.
		/// </summary>
		/// <param name="methodBuilder">The <see cref="MethodBuilderHelper"/>.</param>
		/// <returns>A <see cref="MethodBuilder"/>.</returns>
		public static implicit operator MethodBuilder(MethodBuilderHelper methodBuilder)
		{
			if (methodBuilder == null) throw new ArgumentNullException("methodBuilder");

			return methodBuilder.MethodBuilder;
		}

		private EmitHelper _emitter;
		/// <summary>
		/// Gets <see cref="EmitHelper"/>.
		/// </summary>
		public override EmitHelper Emitter
		{
			get
			{
				if (_emitter == null)
					_emitter = new EmitHelper(this, _methodBuilder.GetILGenerator());

				return _emitter;
			}
		}

		private MethodInfo _overriddenMethod;
		/// <summary>
		/// Gets or sets the base type method overridden by this method, if any.
		/// </summary>
		public  MethodInfo  OverriddenMethod
		{
			get { return _overriddenMethod;  }
			set { _overriddenMethod = value; }
		}

		/// <summary>
		/// Returns the type that declares this method.
		/// </summary>
		public Type DeclaringType
		{
			get { return _methodBuilder.DeclaringType; }
		}
	}
}
