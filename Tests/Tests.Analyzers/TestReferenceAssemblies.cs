using Microsoft.CodeAnalysis.Testing;

namespace Tests.Analyzers
{
	internal static class TestReferenceAssemblies
	{
		// Must track this project's TargetFramework ($(LowestSupportedTargetFramework)): the harness adds the real
		// linq2db assembly built for that TFM, and a lower reference pack trips CS1705.
		public static readonly ReferenceAssemblies Default = ReferenceAssemblies.Net.Net100;
	}
}
