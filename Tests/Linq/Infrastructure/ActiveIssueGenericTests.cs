using NUnit.Framework;

namespace Tests.Infrastructure
{
	internal class ActiveIssueGenericTests
	{
		[Test]
		[ActiveIssue]
		public void NoDetailsOrIssue()
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		[ActiveIssue(Details = "This is a test without issue link")]
		public void DetailsProvided()
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		[ActiveIssue("https://linq2db.github.io", Details = "This is a test with link to github.io")]
		public void IssueLinkWithoutDetailsProvided()
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		[ActiveIssue("https://no.details")]
		public void IssueLinkProvided()
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		[ActiveIssue(713)]
		public void IssueNumberProvided()
		{
			Assert.Fail("This test should be available only for explicit run");
		}
	}
}
