using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	sealed partial class WindowsCredentialStore : ICredentialStore
	{
		const string TargetPrefix = "linq2db/";
		const string FormatMarker = "linq2db.Credential.v1";

		const int CredentialTypeGeneric         = 1;
		const int CredentialPersistLocalMachine = 2;
		const int CryptProtectUiForbidden       = 1;
		const int ErrorNotFound                 = 1168;

		const int PayloadMagic = 0x3143324C;

		public static ICredentialStore Instance { get; } = new WindowsCredentialStore();

		WindowsCredentialStore()
		{
		}

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!CheckPlatform(out error))
				return false;

			if (!CredRead(target, CredentialTypeGeneric, 0, out var credentialPointer))
			{
				var nativeError = Marshal.GetLastWin32Error();

				error = nativeError == ErrorNotFound
					? $"Credential target '{target}' was not found for the current Windows account."
					: $"Cannot read credential target '{target}': {new Win32Exception(nativeError).Message}";
				return false;
			}

			try
			{
				return TryDecodeCredential(target, Marshal.PtrToStructure<NativeCredential>(credentialPointer), true, out user, out password, out error);
			}
			finally
			{
				CredFree(credentialPointer);
			}
		}

		public bool TryStore(string profile, string user, string password, out string? error)
		{
			if (!CheckPlatform(out error))
				return false;

			var target = TargetPrefix + profile;
			byte[] payload;

			using (var stream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
				{
					writer.Write(PayloadMagic);
					writer.Write(user);
					writer.Write(password);
				}

				payload = stream.ToArray();
			}

			byte[]? secret = null;

			try
			{
				if (!TryTransformData(target, payload, true, out secret, out error))
					return false;

				var protectedPayload = secret!;
				var targetPointer     = Marshal.StringToCoTaskMemUni(target);
				var markerPointer     = Marshal.StringToCoTaskMemUni(FormatMarker);
				var secretPointer     = Marshal.AllocHGlobal(protectedPayload.Length);
				var credentialPointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeCredential>());

				try
				{
					Marshal.Copy(protectedPayload, 0, secretPointer, protectedPayload.Length);

					var credential = new NativeCredential
					{
						Type               = CredentialTypeGeneric,
						TargetName         = targetPointer,
						CredentialBlobSize = protectedPayload.Length,
						CredentialBlob     = secretPointer,
						Persist            = CredentialPersistLocalMachine,
						UserName           = markerPointer,
					};

					Marshal.StructureToPtr(credential, credentialPointer, false);

					if (CredWrite(credentialPointer, 0))
					{
						error = null;
						return true;
					}

					var nativeError = Marshal.GetLastWin32Error();

					error = $"Cannot store credential profile '{profile}': {new Win32Exception(nativeError).Message}";
					return false;
				}
				finally
				{
					Marshal.FreeHGlobal(credentialPointer);
					ZeroAndFree(secretPointer, protectedPayload.Length);
					Marshal.FreeCoTaskMem(markerPointer);
					Marshal.FreeCoTaskMem(targetPointer);
				}
			}
			finally
			{
				CryptographicOperations.ZeroMemory(payload);

				if (secret != null)
					CryptographicOperations.ZeroMemory(secret);
			}
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles    = [];
			diagnostics = [];

			if (!TryEnumerateTargets(out var targets, out error))
				return false;

			var result = new List<CredentialProfile>(targets.Count);
			var errors = new List<string>();

			foreach (var target in targets)
			{
				if (!CredRead(target, CredentialTypeGeneric, 0, out var credentialPointer))
				{
					errors.Add($"Cannot read credential target '{target}': {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
					continue;
				}

				try
				{
					if (!TryDecodeCredential(target, Marshal.PtrToStructure<NativeCredential>(credentialPointer), false, out var user, out _, out var decodeError))
					{
						errors.Add(decodeError!);
						continue;
					}

					result.Add(new CredentialProfile(target.Substring(TargetPrefix.Length), user!));
				}
				finally
				{
					CredFree(credentialPointer);
				}
			}

			profiles    = result.OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase).ToArray();
			diagnostics = errors;
			error       = null;
			return true;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			if (!TryEnumerateTargets(out var targets, out error))
			{
				count = 0;
				return false;
			}

			count = targets.Count;
			return true;
		}

		public bool TryRemove(string profile, out bool removed, out string? error)
		{
			removed = false;

			if (!CheckPlatform(out error))
				return false;

			if (CredDelete(TargetPrefix + profile, CredentialTypeGeneric, 0))
			{
				removed = true;
				error   = null;
				return true;
			}

			var nativeError = Marshal.GetLastWin32Error();

			if (nativeError == ErrorNotFound)
			{
				error = null;
				return true;
			}

			error = $"Cannot remove credential profile '{profile}': {new Win32Exception(nativeError).Message}";
			return false;
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			removedCount = 0;

			if (!TryEnumerateTargets(out var targets, out error))
				return false;

			foreach (var target in targets)
			{
				if (CredDelete(target, CredentialTypeGeneric, 0))
				{
					removedCount++;
					continue;
				}

				var nativeError = Marshal.GetLastWin32Error();

				if (nativeError == ErrorNotFound)
					continue;

				error = $"Cannot remove credential target '{target}': {new Win32Exception(nativeError).Message}";
				return false;
			}

			error = null;
			return true;
		}

		static bool TryEnumerateTargets(out IReadOnlyList<string> targets, out string? error)
		{
			targets = [];

			if (!CheckPlatform(out error))
				return false;

			if (!CredEnumerate(TargetPrefix + "*", 0, out var count, out var credentialsPointer))
			{
				var nativeError = Marshal.GetLastWin32Error();

				if (nativeError == ErrorNotFound)
				{
					error = null;
					return true;
				}

				error = $"Cannot enumerate linq2db credential profiles: {new Win32Exception(nativeError).Message}";
				return false;
			}

			try
			{
				var result = new List<string>(count);

				for (var i = 0; i < count; i++)
				{
					var credentialPointer = Marshal.ReadIntPtr(credentialsPointer, i * IntPtr.Size);
					var credential        = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
					var target            = Marshal.PtrToStringUni(credential.TargetName);

					if (target != null && target.StartsWith(TargetPrefix, StringComparison.OrdinalIgnoreCase))
						result.Add(target);
				}

				targets = result;
				error   = null;
				return true;
			}
			finally
			{
				CredFree(credentialsPointer);
			}
		}

		static bool TryDecodeCredential(string target, NativeCredential credential, bool readPassword, out string? user, out string? password, out string? error)
		{
			user     = Marshal.PtrToStringUni(credential.UserName);
			password = null;

			if (string.IsNullOrEmpty(user))
			{
				error = $"Credential target '{target}' doesn't contain a user name.";
				return false;
			}

			if (credential.CredentialBlobSize < 0 || credential.CredentialBlobSize > 0 && credential.CredentialBlob == IntPtr.Zero)
			{
				error = $"Credential target '{target}' contains an unsupported credential value.";
				return false;
			}

			if (string.Equals(user, FormatMarker, StringComparison.Ordinal))
			{
				var storedTarget = Marshal.PtrToStringUni(credential.TargetName);

				if (storedTarget == null)
				{
					error = $"Credential target '{target}' doesn't contain a target name.";
					return false;
				}

				var protectedPayload = new byte[credential.CredentialBlobSize];

				try
				{
					if (protectedPayload.Length > 0)
						Marshal.Copy(credential.CredentialBlob, protectedPayload, 0, protectedPayload.Length);

					// Generic Credential target lookup is case-insensitive. Use the stored target spelling,
					// which is the exact value used to derive entropy when the credential was written.
					//
					if (!TryTransformData(storedTarget, protectedPayload, false, out var unprotectedPayload, out error))
						return false;

					var payload = unprotectedPayload!;

					try
					{
						using var stream = new MemoryStream(payload, false);
						using var reader = new BinaryReader(stream, Encoding.UTF8, false);

						if (reader.ReadInt32() != PayloadMagic)
						{
							error = $"Credential target '{target}' contains an unsupported linq2db credential format.";
							return false;
						}

						user = reader.ReadString();

						if (readPassword)
							password = reader.ReadString();
						else
						{
							var passwordByteCount = reader.Read7BitEncodedInt();

							if (passwordByteCount < 0 || passwordByteCount > stream.Length - stream.Position)
							{
								error = $"Credential target '{target}' contains an unsupported linq2db credential format.";
								return false;
							}

							stream.Position += passwordByteCount;
						}

						if (stream.Position != stream.Length || string.IsNullOrEmpty(user))
						{
							error = $"Credential target '{target}' contains an unsupported linq2db credential format.";
							return false;
						}

						error = null;
						return true;
					}
					catch (Exception ex) when (ex is EndOfStreamException or FormatException or IOException)
					{
						error = $"Credential target '{target}' contains an unsupported linq2db credential format.";
						return false;
					}
					finally
					{
						CryptographicOperations.ZeroMemory(payload);
					}
				}
				finally
				{
					CryptographicOperations.ZeroMemory(protectedPayload);
				}
			}

			if (credential.CredentialBlobSize % sizeof(char) != 0)
			{
				error = $"Credential target '{target}' contains an unsupported credential value.";
				return false;
			}

			if (!readPassword)
			{
				error = null;
				return true;
			}

			password = credential.CredentialBlobSize == 0
				? string.Empty
				: Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / sizeof(char))?.TrimEnd('\0');

			if (password == null)
			{
				error = $"Credential target '{target}' contains an unsupported credential value.";
				return false;
			}

			error = null;
			return true;
		}

		static bool TryTransformData(string target, byte[] input, bool protect, out byte[]? output, out string? error)
		{
			output = null;

			var entropy = SHA256.HashData(Encoding.UTF8.GetBytes(FormatMarker + "\0" + target));
			var inputPointer   = Marshal.AllocHGlobal(input.Length);
			var entropyPointer = Marshal.AllocHGlobal(entropy.Length);

			try
			{
				Marshal.Copy(input,   0, inputPointer,   input.Length);
				Marshal.Copy(entropy, 0, entropyPointer, entropy.Length);

				var inputBlob   = new DataBlob(input.Length, inputPointer);
				var entropyBlob = new DataBlob(entropy.Length, entropyPointer);
				var succeeded   = protect
					? CryptProtectData(ref inputBlob, null, ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out var outputBlob)
					: CryptUnprotectData(ref inputBlob, IntPtr.Zero, ref entropyBlob, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out outputBlob);

				if (!succeeded)
				{
					var nativeError = Marshal.GetLastWin32Error();

					error = $"{(protect ? "Cannot protect" : "Cannot decrypt")} linq2db credential data: {new Win32Exception(nativeError).Message}";
					return false;
				}

				try
				{
					output = new byte[outputBlob.Size];
					Marshal.Copy(outputBlob.Data, output, 0, output.Length);
				}
				finally
				{
					// The decrypt output holds the plaintext credential payload. ZeroAndFree cannot be reused
					// here because this block is LocalAlloc-owned rather than HGlobal-owned.
					if (outputBlob.Size > 0)
						Marshal.Copy(new byte[outputBlob.Size], 0, outputBlob.Data, outputBlob.Size);

					LocalFree(outputBlob.Data);
				}

				error = null;
				return true;
			}
			finally
			{
				ZeroAndFree(inputPointer, input.Length);
				ZeroAndFree(entropyPointer, entropy.Length);
				CryptographicOperations.ZeroMemory(entropy);
			}
		}

		static bool CheckPlatform(out string? error)
		{
			if (OperatingSystem.IsWindows())
			{
				error = null;
				return true;
			}

			error = "System credential profiles are currently supported only on Windows.";
			return false;
		}

		static void ZeroAndFree(IntPtr pointer, int length)
		{
			if (pointer == IntPtr.Zero)
				return;

			if (length > 0)
				Marshal.Copy(new byte[length], 0, pointer, length);

			Marshal.FreeHGlobal(pointer);
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

		[StructLayout(LayoutKind.Sequential)]
		readonly struct DataBlob(int size, IntPtr data)
		{
			public readonly int    Size = size;
			public readonly IntPtr Data = data;
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CredRead(string target, int type, int flags, out IntPtr credential);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CredWrite(IntPtr credential, int flags);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("advapi32.dll", EntryPoint = "CredEnumerateW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CredDelete(string target, int type, int flags);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("advapi32.dll")]
		private static partial void CredFree(IntPtr buffer);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("crypt32.dll", EntryPoint = "CryptProtectData", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CryptProtectData(ref DataBlob data, string? description, ref DataBlob entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool CryptUnprotectData(ref DataBlob data, IntPtr description, ref DataBlob entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("kernel32.dll")]
		private static partial IntPtr LocalFree(IntPtr memory);
	}
}
