using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class IncludeDataSourcesAttribute : DataSourcesBaseAttribute
	{
		public IncludeDataSourcesAttribute(params string[] includeProviders)
			: base(false, includeProviders)
		{
		}

		public IncludeDataSourcesAttribute(bool includeLinqService, params string[] includeProviders)
			: base(includeLinqService, includeProviders)
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			return Providers.Where(TestBase.UserProviders.Contains);
		}
	}
}
