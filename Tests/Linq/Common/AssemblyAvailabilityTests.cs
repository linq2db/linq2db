using System.IO;

using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	[TestFixture]
	public class AssemblyAvailabilityTests
	{
		[Test]
		public void ReturnsTrueForLoadedSelfAssembly()
		{
			// This test assembly is obviously loaded at the time this test runs.
			LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent("linq2db.Tests").ShouldBeTrue();
		}

		[Test]
		public void ReturnsTrueForLinqToDbCoreAssembly()
		{
			// Referenced by the test project and thus loadable via Assembly.Load
			// on every TFM the tests run under (net462 / net8.0 / net9.0 / net10.0).
			LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent("linq2db").ShouldBeTrue();
		}

		[Test]
		public void ReturnsFalseForNonexistentAssembly()
		{
			// The exception bubbling up from Assembly.Load must be swallowed,
			// not propagated — this is the guarantee provider detectors rely on
			// when probing an optional ADO.NET backend that is not installed.
			LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent("LinqToDB.Does.Not.Exist.ZZZ").ShouldBeFalse();
		}

		[Test]
		public void ReturnsTrueViaFileProbeForDeployedButUnloadableAssembly()
		{
			// Drives the ProviderAssemblyFileExists fallback (the "deployed provider wins"
			// branch): a <name>.dll exists next to linq2db.dll but cannot be loaded, so the
			// TryLoadAssembly arm returns null and only the file probe can return true.
			var location = typeof(LinqToDB.Internal.Common.Tools).Assembly.Location;

			// The file probe is intentionally disabled under PublishSingleFile (empty Location);
			// there is nothing on disk to probe, so the fallback cannot be exercised here.
			if (string.IsNullOrEmpty(location))
				Assert.Ignore("Assembly.Location is empty (single-file) — file probe is disabled by design.");

			var directory = Path.GetDirectoryName(location)!;
			var name      = "linq2db.FileProbe.NotLoadable.ZZZ";
			var dllPath   = Path.Combine(directory, name + ".dll");

			// Not a valid PE image: Assembly.Load(name) cannot resolve/load it, so the load arm fails.
			File.WriteAllText(dllPath, "not a real assembly");
			try
			{
				LinqToDB.Internal.Common.Tools.IsProviderAssemblyPresent(name).ShouldBeTrue();
			}
			finally
			{
				File.Delete(dllPath);
			}
		}
	}
}
