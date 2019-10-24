using System;
using NUnit.Framework;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public class SkipCIAttribute : CategoryAttribute
	{
		public SkipCIAttribute(string reason)
			: base("SkipCI")
		{
		}
	}
}
