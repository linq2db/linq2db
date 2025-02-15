using System;

using NUnit.Framework;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public class SkipCIAttribute : CategoryAttribute
	{
		public SkipCIAttribute()
			: base(TestCategory.SkipCI)
		{
		}

		public SkipCIAttribute(string reason)
			: this()
		{
		}
	}
}
