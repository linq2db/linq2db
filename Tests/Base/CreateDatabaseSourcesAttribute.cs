using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class CreateDatabaseSourcesAttribute : DataSourcesBaseAttribute
	{
		public CreateDatabaseSourcesAttribute()
			: base(false, Array.Empty<string>())
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			if (!TestBase.UserProviders.Contains(TestBase.DefaultProvider!))
			{
				// initialize default database, even if we don't run tests against it
				// because it is used as source of test data
				yield return TestBase.DefaultProvider!;
			}

			foreach (var provider in TestBase.UserProviders.Where(p => !Providers.Contains(p) && TestBase.Providers.Contains(p)))
			{
				yield return provider;
			}
		}
	}
}
