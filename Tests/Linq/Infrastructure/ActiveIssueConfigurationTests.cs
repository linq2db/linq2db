using System;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Infrastructure
{
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
		[ActiveIssue(
			Configurations = new[] { TestProvName.AllFirebird },
			Details = "Active Issue Testing: comma-separated providers")]
		public void CommaSeparatedConfigurationsTest([IncludeDataSources(TestProvName.NoopProvider, TestProvName.AllFirebird)]
			string configuration)
		{
			switch (configuration)
			{
				case TestProvName.NoopProvider:
					return;
				case string when configuration.IsAnyOf(TestProvName.AllFirebird):
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Configuration = TestProvName.AllFirebird,
			Details = "Active Issue Testing: configuration and comma-separated providers")]
		public void CommaSeparatedConfigurationTest([IncludeDataSources(TestProvName.NoopProvider, TestProvName.AllFirebird)]
			string configuration)
		{
			switch (configuration)
			{
				case TestProvName.NoopProvider:
					return;
				case string when configuration.IsAnyOf(TestProvName.AllFirebird):
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Configurations = new[] { ProviderName.SQLiteClassic, TestProvName.NoopProvider },
			Details        = "Active Issue Testing: sqlite disabled")]
		public void OneProviderDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case string when configuration.IsAnyOf(ProviderName.Access):
					return;
				case string when configuration.IsAnyOf(TestProvName.NoopProvider):
				case string when configuration.IsAnyOf(ProviderName.SQLiteClassic):
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(Configurations = new[] { TestProvName.NoopProvider })]
		public void NoopProviderDisabledOnFixtureLevelTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case string when configuration.IsAnyOf(ProviderName.Access):
				case string when configuration.IsAnyOf(ProviderName.SQLiteClassic):
					return;
				case string when configuration.IsAnyOf(TestProvName.NoopProvider):
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: sqlite non-wcf disabled",
			Configurations = new[] { ProviderName.SQLiteClassic, TestProvName.NoopProvider },
			SkipForLinqService = true)]
		public void NonWcfTestDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case string when configuration.IsAnyOf(ProviderName.Access):
				case ProviderName.SQLiteClassic + LinqServiceSuffix:
				case TestProvName.NoopProvider + LinqServiceSuffix:
					return;
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic:
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: sqlite wcf disabled",
			Configurations = new[] { ProviderName.SQLiteClassic, TestProvName.NoopProvider },
			SkipForNonLinqService = true)]
		public void WcfTestDisabledTest([IncludeDataSources(true,
			TestProvName.NoopProvider, ProviderName.SQLiteClassic, ProviderName.Access)]
			string configuration)
		{
			switch (configuration)
			{
				case string when configuration.IsAnyOf(ProviderName.Access):
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic:
					return;
				case TestProvName.NoopProvider + LinqServiceSuffix:
				case ProviderName.SQLiteClassic + LinqServiceSuffix:
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}
	}
}
