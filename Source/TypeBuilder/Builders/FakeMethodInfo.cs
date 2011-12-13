using System;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.TypeBuilder.Builders
{
	abstract class FakeMethodInfo : MethodInfo
	{
		protected FakeMethodInfo(PropertyInfo propertyInfo, MethodInfo pair)
		{
			_property = propertyInfo;
			_pair     = pair;
		}

		protected MethodInfo   _pair;
		protected PropertyInfo _property;

		public override MethodAttributes Attributes
		{
			get { return _pair.Attributes; }
		}

		public override CallingConventions CallingConvention
		{
			get { return _pair.CallingConvention; }
		}

		public override Type DeclaringType
		{
			get { return _property.DeclaringType; }
		}

		public override MethodInfo GetBaseDefinition()
		{
			return _pair.GetBaseDefinition();
		}

		public override object[] GetCustomAttributes(bool inherit)
		{
			return _property.GetCustomAttributes(inherit);
		}

		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return _property.GetCustomAttributes(attributeType, inherit);
		}

		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return _pair.GetMethodImplementationFlags();
		}

		public override object Invoke(
			object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return null;
		}

		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return false;
		}

		public override MemberTypes MemberType
		{
			get { return _property.MemberType; }
		}

		public override RuntimeMethodHandle MethodHandle
		{
			get { return new RuntimeMethodHandle(); }
		}

		public override Type ReflectedType
		{
			get { return _property.ReflectedType; }
		}

		class CustomAttributeProvider : ICustomAttributeProvider
		{
			static readonly object[] _object = new object[0];

			public object[] GetCustomAttributes(bool inherit)
			{
				return _object;
			}

			public object[] GetCustomAttributes(Type attributeType, bool inherit)
			{
				return _object;
			}

			public bool IsDefined(Type attributeType, bool inherit)
			{
				return false;
			}
		}

		static readonly CustomAttributeProvider _customAttributeProvider = new CustomAttributeProvider();

		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get { return _customAttributeProvider; }
		}

		public override ParameterInfo ReturnParameter
		{
			get { return new FakeParameterInfo("ret", ReturnType, this, null); }
		}
	}
}
