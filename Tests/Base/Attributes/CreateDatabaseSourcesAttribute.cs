using System;
using System.Collections.Generic;

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
			try
			{
				return TestConfiguration.GetCreateDatabaseProviders(Providers);
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}
	}
}
