using System;
using System.Collections.Generic;
using System.Linq;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Restricts a parameterized test to the provider families the NHibernate integration supports
	/// (<see cref="TestConfiguration.NHProviders"/>), intersected with the enabled providers
	/// (<see cref="TestConfiguration.UserProviders"/>, which honors the <c>--provider</c> command-line flag).
	/// With no arguments the test runs on every enabled NHibernate provider; passing provider names narrows it further.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	public class NHIncludeDataSourcesAttribute : DataSourcesBaseAttribute
	{
		public NHIncludeDataSourcesAttribute(params string[] includeProviders)
			: base(false, includeProviders.SplitAll().ToArray())
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			try
			{
				return TestConfiguration.UserProviders.Where(p =>
					(Providers.Length == 0 || Providers.Contains(p)) && TestConfiguration.NHProviders.Contains(p));
			}
			catch (Exception e)
			{
				TestUtils.Log(e);
				throw;
			}
		}
	}
}
