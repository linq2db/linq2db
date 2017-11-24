using System;
using System.Runtime.InteropServices;

// ReSharper disable CheckNamespace

namespace System
{

	[ComVisible(true)]
	interface ICloneable
	{
		object Clone();
	}
}
