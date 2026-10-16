using System.Collections.Generic;
using System.Linq;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	static class EFTestProviders
	{
		// TestConfiguration.EFProviders is EF-version-agnostic, so it must be narrowed to what this test
		// project can actually configure: a provider ContextTestBase.ProviderSetup has no arm for fails
		// every case it is offered to instead of not running.
		public static readonly IReadOnlyCollection<string> Supported = TestConfiguration.EFProviders
#if !EF_MYSQL
			.Where(p => !p.IsAnyOf(TestProvName.AllMySql))
#endif
			.ToList();
	}
}
