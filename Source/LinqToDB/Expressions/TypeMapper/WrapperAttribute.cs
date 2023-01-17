﻿using System;

namespace LinqToDB.Expressions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate)]
	public class WrapperAttribute : Attribute
	{
		public WrapperAttribute() { }

		public WrapperAttribute(string typeName)
		{
			TypeName = typeName;
		}

		public string? TypeName { get; }
	}
}
