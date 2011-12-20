using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true)]
	public class InheritanceMappingAttribute : Attribute
	{
		public object Code      { get; set; }
		public bool   IsDefault { get; set; }
		public Type   Type      { get; set; }
	}
}
