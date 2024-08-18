using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class EFDataSourcesAttribute : DataSourcesBaseAttribute
	{
		public EFDataSourcesAttribute(params string[] excludeProviders)
			: base(false, excludeProviders.SplitAll().ToArray())
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			try
			{
				return TestConfiguration.UserProviders.Where(p => !Providers.Contains(p) && TestConfiguration.EFProviders.Contains(p));
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}
	}
}
