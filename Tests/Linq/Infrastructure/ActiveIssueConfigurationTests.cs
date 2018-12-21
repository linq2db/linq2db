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
			Configurations = new[] { ProviderName.SQLiteClassic, TestProvName.NoopProvider },
			Details = "Active Issue Testing: sqlite disabled")]
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

		[Test]
		[ActiveIssue(Configurations = new[] { TestProvName.NoopProvider })]
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
			Configurations = new[] { ProviderName.SQLiteClassic, TestProvName.NoopProvider },
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
				case ProviderName.Access:
				case ProviderName.Access + ".LinqService":
				case TestProvName.NoopProvider:
				case ProviderName.SQLiteClassic:
					return;
				case TestProvName.NoopProvider + ".LinqService":
				case ProviderName.SQLiteClassic + ".LinqService":
					Assert.Fail("This test should be available only for explicit run");
					break;
			}

			Assert.Fail($"Unexpected configuration: {configuration}");
		}

		[Test]
		[ActiveIssue(
			Details = "Active Issue Testing: Access wcf disabled and sqlite non-wcf disabled",
			Configurations = new[] { ProviderName.Access + ".LinqService", ProviderName.SQLiteClassic, TestProvName.NoopProvider },
			SkipForNonLinqService = true)]
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
