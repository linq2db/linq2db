using System;

using LinqToDB.CommandLine.Commands.QueryExecution;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class WindowsImpersonationTests
	{
		[TestCase("user",                null,     "user")]
		[TestCase("DOMAIN\\user",        "DOMAIN", "user")]
		[TestCase("DOMAIN\\user\\extra", "DOMAIN", "user\\extra")]
		[TestCase("\\user",              null,     "\\user")]
		[TestCase("DOMAIN\\",            null,     "DOMAIN\\")]
		public void SplitUserNameReturnsDomainAndUserName(string user, string? expectedDomain, string expectedUserName)
		{
			var (domain, userName) = WindowsImpersonation.SplitUserName(user);

			{
				domain.ShouldBe(expectedDomain);
				userName.ShouldBe(expectedUserName);
			}
		}

		[TestCase(WindowsImpersonationMode.Interactive,      2, 0)]
		[TestCase(WindowsImpersonationMode.Network,          3, 0)]
		[TestCase(WindowsImpersonationMode.NetworkCleartext, 8, 0)]
		[TestCase(WindowsImpersonationMode.NewCredentials,   9, 3)]
		[TestCase((WindowsImpersonationMode)int.MaxValue,     8, 0)]
		public void GetLogonOptionsReturnsNativeValues(WindowsImpersonationMode mode, int expectedLogonType, int expectedLogonProvider)
		{
			var (logonType, logonProvider) = WindowsImpersonation.GetLogonOptions(mode);

			{
				logonType.ShouldBe(expectedLogonType);
				logonProvider.ShouldBe(expectedLogonProvider);
			}
		}
	}
}
