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
			LinqToDB.Internal.Common.Tools.IsAssemblyAvailable("linq2db.Tests").ShouldBeTrue();
		}

		[Test]
		public void ReturnsTrueForLinqToDbCoreAssembly()
		{
			// Referenced by the test project and thus loadable via Assembly.Load
			// on every TFM the tests run under (net462 / net8.0 / net9.0 / net10.0).
			LinqToDB.Internal.Common.Tools.IsAssemblyAvailable("linq2db").ShouldBeTrue();
		}

		[Test]
		public void ReturnsFalseForNonexistentAssembly()
		{
			// The exception bubbling up from Assembly.Load must be swallowed,
			// not propagated — this is the guarantee provider detectors rely on
			// when probing an optional ADO.NET backend that is not installed.
			LinqToDB.Internal.Common.Tools.IsAssemblyAvailable("LinqToDB.Does.Not.Exist.ZZZ").ShouldBeFalse();
		}
	}
}
