using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class DataSourcesAttribute : DataSourcesBaseAttribute
	{
		public DataSourcesAttribute(params string[] excludeProviders)
			: base(true, excludeProviders)
		{
		}

		public DataSourcesAttribute(bool includeLinqService, params string[] excludeProviders)
			: base(includeLinqService, excludeProviders)
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			return TestBase.UserProviders.Where(p => !Providers.Contains(p) && TestBase.Providers.Contains(p));
		}
	}
}
