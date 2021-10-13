﻿using System;

namespace LinqToDB.Expressions
{
	/// <summary>
	/// Used to tell query expression comparer to skip method call argument comparison if it is constant.
	/// Method parameter parametrization should be also implemented in method builder.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	internal class SkipIfConstantAttribute : Attribute
	{
	}
}
