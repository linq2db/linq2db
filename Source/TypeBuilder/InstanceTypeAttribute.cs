using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Reflection;

namespace LinqToDB.TypeBuilder
{
	///<summary>
	/// Specifies a value holder type.
	///</summary>
	[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
	[AttributeUsage(AttributeTargets.Property)]
	public class InstanceTypeAttribute : Builders.AbstractTypeBuilderAttribute
	{
		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		public InstanceTypeAttribute(Type instanceType)
		{
			_instanceType = instanceType;
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType, object parameter1)
		{
			_instanceType = instanceType;
			SetParameters(parameter1);
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<param name="parameter2">An additional parameter.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType,
			object parameter1,
			object parameter2)
		{
			_instanceType = instanceType;
			SetParameters(parameter1, parameter2);
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<param name="parameter2">An additional parameter.</param>
		///<param name="parameter3">An additional parameter.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3)
		{
			_instanceType = instanceType;
			SetParameters(parameter1, parameter2, parameter3);
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<param name="parameter2">An additional parameter.</param>
		///<param name="parameter3">An additional parameter.</param>
		///<param name="parameter4">An additional parameter.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4)
		{
			_instanceType = instanceType;
			SetParameters(parameter1, parameter2, parameter3, parameter4);
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<param name="parameter2">An additional parameter.</param>
		///<param name="parameter3">An additional parameter.</param>
		///<param name="parameter4">An additional parameter.</param>
		///<param name="parameter5">An additional parameter.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4,
			object parameter5)
		{
			_instanceType = instanceType;
			SetParameters(parameter1, parameter2, parameter3, parameter4, parameter5);
		}

		///<summary>
		/// Initializes a new instance of the InstanceTypeAttribute class.
		///</summary>
		///<param name="instanceType">The <see cref="System.Type"/> of an instance.</param>
		///<param name="parameter1">An additional parameter.</param>
		///<param name="parameters">More additional parameters.</param>
		///<seealso cref="Parameters"/>
		public InstanceTypeAttribute(Type instanceType, object parameter1, params object[] parameters)
		{
			_instanceType = instanceType;

			// Note: we can not use something like
			// public InstanceTypeAttribute(Type instanceType, params object[] parameters)
			// because [InstanceType(typeof(Foo), new object[] {1,2,3})] will be treated as
			// [InstanceType(typeof(Foo), 1, 2, 3)] so it will be not possible to specify
			// an instance type with array as the type of the one and only parameter.
			// An extra parameter of type object made it successul.

			int len = parameters.Length;
			Array.Resize(ref parameters, len + 1);
			Array.ConstrainedCopy(parameters, 0, parameters, 1, len);
			parameters[0] = parameter1;

			SetParameters(parameters);
		}

		protected void SetParameters(params object[] parameters)
		{
			_parameters = parameters;
		}

		private object[] _parameters;
		///<summary>
		/// Any additional parameters passed to a value holder constructor
		/// with <see cref="InitContext"/> parameter.
		///</summary>
		public  object[]  Parameters
		{
			get { return _parameters;  }
		}

		private readonly Type _instanceType;
		protected        Type  InstanceType
		{
			get { return _instanceType; }
		}

		private bool _isObjectHolder;
		///<summary>
		/// False (default value) for holders for scalar types,
		/// true for holders for complex objects.
		///</summary>
		public  bool  IsObjectHolder
		{
			get { return _isObjectHolder;  }
			set { _isObjectHolder = value; }
		}

		private         Builders.IAbstractTypeBuilder _typeBuilder;
		///<summary>
		/// An <see cref="Builders.IAbstractTypeBuilder"/> required for this attribute
		/// to build an abstract type inheritor.
		///</summary>
		public override Builders.IAbstractTypeBuilder  TypeBuilder
		{
			get
			{
				return _typeBuilder ?? (_typeBuilder = 
					new Builders.InstanceTypeBuilder(_instanceType, _isObjectHolder));
			}
		}
	}
}
