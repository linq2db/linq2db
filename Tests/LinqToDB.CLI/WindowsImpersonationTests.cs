using System;
using System.ComponentModel;

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

			using (Assert.EnterMultipleScope())
			{
				domain.  ShouldBe(expectedDomain);
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

			using (Assert.EnterMultipleScope())
			{
				logonType.    ShouldBe(expectedLogonType);
				logonProvider.ShouldBe(expectedLogonProvider);
			}
		}

		[Test]
		public void LogonWithUnknownUserFails()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Logs on through Windows LogonUser.");

			// Not NewCredentials, which does not check the credentials; domain ".", so only the local account database is asked.
			var exception = Should.Throw<Win32Exception>(() => WindowsImpersonation.Logon(".\\linq2db-no-such-user", "x", WindowsImpersonationMode.NetworkCleartext));

			using (Assert.EnterMultipleScope())
			{
				exception.NativeErrorCode.ShouldBe(1326);
				exception.Message.        ShouldBe("Windows impersonation logon failed.");
			}
		}

		[Test]
		public void LogonIsNotSupportedOutsideWindows()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Checks the platform guard of other systems.");

			Should.Throw<PlatformNotSupportedException>(() => WindowsImpersonation.Logon("user", "x", WindowsImpersonationMode.NetworkCleartext));
		}
	}
}
