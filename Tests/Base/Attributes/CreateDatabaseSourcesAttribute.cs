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
			var list = new List<string>();

			try
			{
				if (!TestConfiguration.UserProviders.Contains(TestConfiguration.DefaultProvider!))
				{
					// initialize default database, even if we don't run tests against it
					// because it is used as source of test data
					list.Add(TestConfiguration.DefaultProvider!);
				}

				foreach (var provider in TestConfiguration.UserProviders.Where(p => !Providers.Contains(p) && TestConfiguration.Providers.Contains(p)))
				{
					list.Add(provider);
				}
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}

			return list;
		}
	}
}
