using System;

// ReSharper disable CheckNamespace

namespace System
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	[System.Runtime.InteropServices.ComVisible(true)]
	sealed class SerializableAttribute : Attribute 
	{
	}
}
