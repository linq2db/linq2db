using System;

namespace Tests.Playground.TypeMapping
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum)]
	public class WrapperAttribute : Attribute
	{
		
	}
}
