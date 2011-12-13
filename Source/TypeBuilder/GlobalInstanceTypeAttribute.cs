using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.TypeBuilder
{
	[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
	public class GlobalInstanceTypeAttribute : InstanceTypeAttribute
	{
		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType)
			: base(instanceType)
		{
			_propertyType = propertyType;
		}

		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType, object parameter1)
			: base(instanceType, parameter1)
		{
			_propertyType = propertyType;
		}

		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType,
			object parameter1,
			object parameter2)
			: base(instanceType, parameter1, parameter2)
		{
			_propertyType = propertyType;
		}

		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3)
			: base(instanceType, parameter1, parameter2, parameter3)
		{
			_propertyType = propertyType;
		}

		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4)
			: base(instanceType, parameter1, parameter2, parameter3, parameter4)
		{
			_propertyType = propertyType;
		}

		public GlobalInstanceTypeAttribute(Type propertyType, Type instanceType,
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4,
			object parameter5)
			: base(instanceType, parameter1, parameter2, parameter3, parameter4, parameter5)
		{
			_propertyType = propertyType;
		}

		private readonly Type _propertyType;
		public           Type  PropertyType
		{
			get { return _propertyType; }
		}

		private         Builders.IAbstractTypeBuilder _typeBuilder;
		public override Builders.IAbstractTypeBuilder  TypeBuilder
		{
			get 
			{
				if (_typeBuilder == null)
					_typeBuilder = new Builders.InstanceTypeBuilder(_propertyType, InstanceType, IsObjectHolder);

				return _typeBuilder;
			}
		}
	}
}
