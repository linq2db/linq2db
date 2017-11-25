using System;

// ReSharper disable CheckNamespace

namespace System.Security
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	[Runtime.InteropServices.ComVisible(true)]
	public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
	{
	}
}
