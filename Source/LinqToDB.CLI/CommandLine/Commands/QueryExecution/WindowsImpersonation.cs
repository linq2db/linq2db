using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

using Microsoft.Win32.SafeHandles;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	static class WindowsImpersonation
	{
		const int Logon32LogonInteractive      = 2;
		const int Logon32LogonNetwork          = 3;
		const int Logon32LogonNetworkCleartext = 8;
		const int Logon32LogonNewCredentials   = 9;
		const int Logon32ProviderDefault       = 0;
		const int Logon32ProviderWinnt50       = 3;

		public static T Run<T>(string user, string password, WindowsImpersonationMode mode, Func<T> action)
		{
			if (!OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException("Windows impersonation is supported only on Windows.");

			return RunWindows(user, password, mode, action);
		}

		public static Task<T> RunAsync<T>(string user, string password, WindowsImpersonationMode mode, Func<Task<T>> action)
		{
			if (!OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException("Windows impersonation is supported only on Windows.");

			return RunWindowsAsync(user, password, mode, action);
		}

		[SupportedOSPlatform("windows")]
		static T RunWindows<T>(string user, string password, WindowsImpersonationMode mode, Func<T> action)
		{
			SplitUserName(user, out var domain, out var userName);
			GetLogonOptions(mode, out var logonType, out var logonProvider);

			if (!LogonUser(userName, domain, password, logonType, logonProvider, out var token))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows impersonation logon failed.");

			using (token)
				return WindowsIdentity.RunImpersonated(token, action);
		}

		[SupportedOSPlatform("windows")]
		static async Task<T> RunWindowsAsync<T>(string user, string password, WindowsImpersonationMode mode, Func<Task<T>> action)
		{
			SplitUserName(user, out var domain, out var userName);
			GetLogonOptions(mode, out var logonType, out var logonProvider);

			if (!LogonUser(userName, domain, password, logonType, logonProvider, out var token))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows impersonation logon failed.");

			using (token)
				return await WindowsIdentity.RunImpersonated(token, action).ConfigureAwait(false);
		}

		static void GetLogonOptions(WindowsImpersonationMode mode, out int logonType, out int logonProvider)
		{
			switch (mode)
			{
				case WindowsImpersonationMode.Interactive:
					logonType     = Logon32LogonInteractive;
					logonProvider = Logon32ProviderDefault;
					return;
				case WindowsImpersonationMode.Network:
					logonType     = Logon32LogonNetwork;
					logonProvider = Logon32ProviderDefault;
					return;
				case WindowsImpersonationMode.NewCredentials:
					logonType     = Logon32LogonNewCredentials;
					logonProvider = Logon32ProviderWinnt50;
					return;
				default:
					logonType     = Logon32LogonNetworkCleartext;
					logonProvider = Logon32ProviderDefault;
					return;
			}
		}

		static void SplitUserName(string user, out string? domain, out string userName)
		{
			var separator = user.IndexOf('\\', StringComparison.Ordinal);

			if (separator > 0 && separator + 1 < user.Length)
			{
				domain   = user[..separator];
				userName = user[(separator + 1)..];
				return;
			}

			domain   = null;
			userName = user;
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool LogonUser(
			string userName,
			string? domain,
			string password,
			int logonType,
			int logonProvider,
			out SafeAccessTokenHandle token);
	}
}
