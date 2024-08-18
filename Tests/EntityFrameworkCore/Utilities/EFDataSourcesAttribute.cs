using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class EFIncludeDataSourcesAttribute : DataSourcesBaseAttribute
	{
		public EFIncludeDataSourcesAttribute(params string[] includeProviders)
			: base(false, includeProviders.SplitAll().ToArray())
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			try
			{
				return Providers.Intersect(TestConfiguration.UserProviders);
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}
	}
}
