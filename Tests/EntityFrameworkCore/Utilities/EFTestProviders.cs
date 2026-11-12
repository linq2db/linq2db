using System.Collections.Generic;
using System.Linq;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	static class EFTestProviders
	{
		// TestConfiguration.EFProviders is EF-version-agnostic, so it must be narrowed to what this test
		// project can actually configure: a provider ContextTestBase.ProviderSetup has no arm for fails
		// every case it is offered to instead of not running. Every current test project defines EF_MYSQL;
		// the filter is for an EF major that ships before its MySQL provider.
		public static readonly IReadOnlyCollection<string> Supported = TestConfiguration.EFProviders
#if !EF_MYSQL
			.Where(p => !p.IsAnyOf(TestProvName.AllMySql))
#endif
			.ToList();
	}
}
