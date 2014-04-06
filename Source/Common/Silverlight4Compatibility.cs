using System;

// ReSharper disable CheckNamespace

namespace System.Security
{
//	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
//	class SerializableAttribute : Attribute
//	{
//	}

	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	[Runtime.InteropServices.ComVisible(true)]
	public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
	{
	}
}
