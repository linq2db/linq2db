using System;

namespace System
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	class SerializableAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false )]
	[Runtime.InteropServices.ComVisible(true)] 
	sealed public class AllowPartiallyTrustedCallersAttribute : Attribute
	{
	}
}
