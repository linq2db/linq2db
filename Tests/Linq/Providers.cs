using System;
using System.Collections;
using System.Linq;

using NUnit.Framework;

namespace Data.Linq
{
	public class ProvidersAttribute : TestCaseSourceAttribute
	{
		public ProvidersAttribute() : base(typeof(ProviderFactory), "All")
		{
		}
	}

	class ProviderFactory
	{
		public static IEnumerable All
		{
			get { return TestBase.Providers.Select(provider => provider.Name); }
		}
	}
}
