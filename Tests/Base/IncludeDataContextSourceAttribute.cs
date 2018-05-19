using System;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public class IncludeDataContextSourceAttribute : BaseDataContextSourceAttribute
	{
		public IncludeDataContextSourceAttribute(params string[] include)
			: this(false, include)
		{
		}

		public IncludeDataContextSourceAttribute(bool includeLinqService, params string[] include)
			: base(includeLinqService, include)
		{
		}
	}
}
