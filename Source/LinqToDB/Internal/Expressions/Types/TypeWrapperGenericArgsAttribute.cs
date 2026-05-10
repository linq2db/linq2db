using System;

namespace LinqToDB.Internal.Expressions.Types
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class TypeWrapperGenericArgsAttribute : Attribute
	{
		public TypeWrapperGenericArgsAttribute(int argCount)
		{
			ArgCount = argCount;
		}

		/// <summary>
		/// Choose overload with specified number of generic arguments. Zero will select non-generic method.
		/// </summary>
		internal int ArgCount { get; }
	}
}
