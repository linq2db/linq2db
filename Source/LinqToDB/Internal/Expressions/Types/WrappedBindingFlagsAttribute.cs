using System;
using System.Reflection;

namespace LinqToDB.Internal.Expressions.Types
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
	public sealed class WrappedBindingFlagsAttribute : Attribute
	{
		public WrappedBindingFlagsAttribute(BindingFlags bindingFlags)
		{
			BindingFlags = bindingFlags;
		}

		public BindingFlags BindingFlags { get; }
	}
}
