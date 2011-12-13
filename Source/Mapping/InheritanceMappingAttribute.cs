using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true)]
	public class InheritanceMappingAttribute : Attribute
	{
		object _code;      public object Code      { get { return _code;      } set { _code      = value; } }
		bool   _isDefault; public bool   IsDefault { get { return _isDefault; } set { _isDefault = value; } }
		Type   _type;      public Type   Type      { get { return _type;      } set { _type      = value; } }
	}
}
