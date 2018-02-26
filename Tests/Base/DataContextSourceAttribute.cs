using System;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Method)]
	public class DataContextSourceAttribute : BaseDataContextSourceAttribute
	{
		public DataContextSourceAttribute()
			: this(true, null)
		{
		}

		public DataContextSourceAttribute(params string[] except)
			: this(true, except)
		{
		}

		public DataContextSourceAttribute(bool includeLinqService, params string[] except)
			: base(includeLinqService,
				TestBase.Providers.Where(providerName => /*UserProviders.Contains(providerName) &&*/ except == null || !except.Contains(providerName)).ToArray())
		{
		}
	}
}
