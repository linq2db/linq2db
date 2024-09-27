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
			try
			{
				return TestConfiguration.UserProviders.Where(p => !Providers.Contains(p) && TestConfiguration.Providers.Contains(p));
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}
	}
}
