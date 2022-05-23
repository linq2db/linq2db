using System;

using LinqToDB.Common.Internal;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class DataOptionsTests: TestBase
	{
		[Test]
		public void LinqOptionsTest()
		{
			var lo1 = LinqToDB.Common.Configuration.Linq.Options with { GuardGrouping = false };
			var lo2 = lo1 with { GuardGrouping = true };

			Assert.That(((IConfigurationID)lo1).ConfigurationID, Is.Not.EqualTo(((IConfigurationID)lo2).ConfigurationID));
		}
	}
}
