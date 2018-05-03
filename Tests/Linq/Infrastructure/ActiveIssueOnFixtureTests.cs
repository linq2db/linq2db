using NUnit.Framework;

namespace Tests.Infrastructure
{
	[ActiveIssue(Details = "Active Issue Testing: applied to fixture")]
	internal class ActiveIssueOnFixtureTests
	{
		[Test]
		public void Test1()
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		public void Test2()
		{
			Assert.Fail("This test should be available only for explicit run");
		}
	}
}
