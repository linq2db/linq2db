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
	/// <summary>
	/// Executes operations under a Windows user identity.
	/// </summary>
	public static class WindowsImpersonation
	{
		const int Logon32LogonInteractive      = 2;
		const int Logon32LogonNetwork          = 3;
		const int Logon32LogonNetworkCleartext = 8;
		const int Logon32LogonNewCredentials   = 9;
		const int Logon32ProviderDefault       = 0;
		const int Logon32ProviderWinnt50       = 3;

		/// <summary>
		/// Executes an operation under the specified Windows user identity.
		/// </summary>
		public static T Run<T>(string user, string password, WindowsImpersonationMode mode, Func<T> action)
		{
			if (!OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException("Windows impersonation is supported only on Windows.");

			return RunWindows(user, password, mode, action);
		}

		/// <summary>
		/// Executes an asynchronous operation under the specified Windows user identity.
		/// </summary>
		public static Task<T> RunAsync<T>(string user, string password, WindowsImpersonationMode mode, Func<Task<T>> action)
		{
			if (!OperatingSystem.IsWindows())
				throw new PlatformNotSupportedException("Windows impersonation is supported only on Windows.");

			return RunWindowsAsync(user, password, mode, action);
		}

		[SupportedOSPlatform("windows")]
		static T RunWindows<T>(string user, string password, WindowsImpersonationMode mode, Func<T> action)
		{
			var (domain, userName)           = SplitUserName  (user);
			var (logonType, logonProvider)   = GetLogonOptions(mode);

			if (!LogonUser(userName, domain, password, logonType, logonProvider, out var token))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows impersonation logon failed.");

			using (token)
				return WindowsIdentity.RunImpersonated(token, action);
		}

		[SupportedOSPlatform("windows")]
		static async Task<T> RunWindowsAsync<T>(string user, string password, WindowsImpersonationMode mode, Func<Task<T>> action)
		{
			var (domain, userName)         = SplitUserName  (user);
			var (logonType, logonProvider) = GetLogonOptions(mode);

			if (!LogonUser(userName, domain, password, logonType, logonProvider, out var token))
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows impersonation logon failed.");

			using (token)
				return await WindowsIdentity.RunImpersonatedAsync(token, action).ConfigureAwait(false);
		}

		/// <summary>
		/// Resolves the native Windows logon type and provider for an impersonation mode.
		/// </summary>
		public static (int LogonType, int LogonProvider) GetLogonOptions(WindowsImpersonationMode mode)
		{
			return mode switch
			{
				WindowsImpersonationMode.Interactive    => (Logon32LogonInteractive,      Logon32ProviderDefault),
				WindowsImpersonationMode.Network        => (Logon32LogonNetwork,          Logon32ProviderDefault),
				WindowsImpersonationMode.NewCredentials => (Logon32LogonNewCredentials,   Logon32ProviderWinnt50),
				_                                       => (Logon32LogonNetworkCleartext, Logon32ProviderDefault),
			};
		}

		/// <summary>
		/// Splits a Windows user name into optional domain and user-name components.
		/// </summary>
		public static (string? Domain, string UserName) SplitUserName(string user)
		{
			var separator = user.IndexOf('\\', StringComparison.Ordinal);

			if (separator > 0 && separator + 1 < user.Length)
				return (user[..separator], user[(separator + 1)..]);

			return (null, user);
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
