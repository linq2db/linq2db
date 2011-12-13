using System;
using System.Reflection;

namespace LinqToDB.TypeBuilder.Builders
{
	class FakeSetter : FakeMethodInfo
	{
		public FakeSetter(PropertyInfo propertyInfo) 
			: base(propertyInfo, propertyInfo.GetGetMethod(true))
		{
		}

		public override ParameterInfo[] GetParameters()
		{
			var index = _property.GetIndexParameters();
			var pi    = new ParameterInfo[index.Length + 1];

			index.CopyTo(pi, 0);
			pi[index.Length] = new FakeParameterInfo("value", _property.PropertyType, _property, null);

			return pi;
		}

		public override string Name
		{
			get { return "set_" + _property.Name; }
		}

		public override Type ReturnType
		{
			get { return typeof(void); }
		}
	}
}
