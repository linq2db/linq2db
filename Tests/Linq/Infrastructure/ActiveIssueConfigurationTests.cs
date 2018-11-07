using System;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	[ActiveIssue(
		Details = "Active Issue Testing: Noop provider disabled on fixture level for non-wcf tests",
		Configuration = TestProvName.NoopProvider,
		SkipForLinqService = true)]
	public class ActiveIssueConfigurationTests : TestBase
	{
		[Test]
		[ActiveIssue(Details = "Active Issue Testing: all configurations disabled")]
		public void AllConfigurationsDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			Assert.Fail("This test should be available only for explicit run");
		}

		[Test]
		[ActiveIssue(Details = "Active Issue Testing: sqlite disabled", Configuration = ProviderName.SQLiteClassic)]
		public void OneProviderDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case ProviderName.Access:
				case ProviderName.Access + ".LinqService":
				case TestProvName.NoopProvider + ".LinqService":
					return;
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic:
				case ProviderName.SQLiteClassic + ".LinqService":
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}
	}

	public class ActiveIssueConfigurationTestsXXX : TestBase
	{
		[Test]
		public void NoopProviderDisabledOnFixtureLevelTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case ProviderName.Access:
				case ProviderName.Access + ".LinqService":
				case ProviderName.SQLiteClassic:
				case ProviderName.SQLiteClassic + ".LinqService":
				case TestProvName.NoopProvider + ".LinqService":
					return;
				case TestProvName.NoopProvider:
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: sqlite non-wcf disabled",
			Configuration = ProviderName.SQLiteClassic,
			SkipForLinqService = true)]
		public void NonWcfTestDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case ProviderName.Access:
				case ProviderName.Access + ".LinqService":
				case ProviderName.SQLiteClassic + ".LinqService":
				case TestProvName.NoopProvider + ".LinqService":
					return;
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic:
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexprected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: sqlite wcf disabled",
			Configuration = ProviderName.SQLiteClassic,
			SkipForNonLinqService = true)]
		public void WcfTestDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case ProviderName.Access:
				case ProviderName.Access + ".LinqService":
				case TestProvName.NoopProvider + ".LinqService":
				case ProviderName.SQLiteClassic:
					return;
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic + ".LinqService":
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: Access wcf disabled",
			Configuration = ProviderName.Access,
			SkipForNonLinqService = true)]
		[ActiveIssue(
			Details = "Active Issue Testing: sqlite non-wcf disabled",
			Configuration = ProviderName.SQLiteClassic,
			SkipForLinqService = true)]
		public void MultipleAttributesTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case ProviderName.Access:
				case TestProvName.NoopProvider + ".LinqService":
				case ProviderName.SQLiteClassic + ".LinqService":
					return;
				case ProviderName.SQLiteClassic:
				case ProviderName.Access + ".LinqService":
				case TestProvName.NoopProvider:
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}
	}
}
