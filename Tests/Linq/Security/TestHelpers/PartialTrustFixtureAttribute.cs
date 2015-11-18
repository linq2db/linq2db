using System;

using Xunit;

namespace Tests.Security.TestHelpers
{
	class PartialTrustFixtureAttribute : RunWithAttribute
	{
		public PartialTrustFixtureAttribute() : base(typeof(PartialTrustClassCommand))
		{
		}
	}
}
