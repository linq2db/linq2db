using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace LinqToDB.CommandLine.Commands.Connection
{
	static class WindowsCredentialManager
	{
		const int CredentialTypeGeneric = 1;
		const int ErrorNotFound          = 1168;

		public static bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!OperatingSystem.IsWindows())
			{
				error = "Option '--windows-credentials' is supported only on Windows.";
				return false;
			}

			if (!CredRead(target, CredentialTypeGeneric, 0, out var credentialPointer))
			{
				var nativeError = Marshal.GetLastWin32Error();
				error = nativeError == ErrorNotFound
					? $"Windows Credential Manager target '{target}' was not found for the current Windows account."
					: $"Cannot read Windows Credential Manager target '{target}': {new Win32Exception(nativeError).Message}";
				return false;
			}

			try
			{
				var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);

				user = Marshal.PtrToStringUni(credential.UserName);

				if (string.IsNullOrEmpty(user))
				{
					error = $"Windows Credential Manager target '{target}' doesn't contain a user name.";
					return false;
				}

				if (credential.CredentialBlobSize % sizeof(char) != 0 || credential.CredentialBlobSize > 0 && credential.CredentialBlob == IntPtr.Zero)
				{
					error = $"Windows Credential Manager target '{target}' contains an unsupported credential value.";
					return false;
				}

				password = credential.CredentialBlobSize == 0
					? string.Empty
					: Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / sizeof(char))?.TrimEnd('\0');

				if (password == null)
				{
					error = $"Windows Credential Manager target '{target}' contains an unsupported credential value.";
					return false;
				}

				error = null;
				return true;
			}
			finally
			{
				CredFree(credentialPointer);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct NativeCredential
		{
			public int      Flags;
			public int      Type;
			public IntPtr   TargetName;
			public IntPtr   Comment;
			public FILETIME LastWritten;
			public int      CredentialBlobSize;
			public IntPtr   CredentialBlob;
			public int      Persist;
			public int      AttributeCount;
			public IntPtr   Attributes;
			public IntPtr   TargetAlias;
			public IntPtr   UserName;
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("advapi32.dll")]
		static extern void CredFree(IntPtr buffer);
	}
}
