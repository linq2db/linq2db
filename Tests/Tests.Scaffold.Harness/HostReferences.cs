using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests.Scaffold.Harness
{
	/// <summary>
	/// Compilation references: all managed assemblies of the test host (runtime, linq2db, F# runtime and ADO.NET providers),
	/// so generated code compiles against the same assemblies it executes with.
	/// </summary>
	static class HostReferences
	{
		static readonly Lazy<IReadOnlyList<string>> _paths = new(static () =>
			((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES not available"))
				.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
				.Where(static p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray());

		public static IReadOnlyList<string> Paths => _paths.Value;
	}
}
