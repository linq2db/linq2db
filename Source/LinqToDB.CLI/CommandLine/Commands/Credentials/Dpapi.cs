using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Windows DPAPI for the current user (<c>CryptProtectData</c>/<c>CryptUnprotectData</c> without UI), used by the
	/// Windows Credential Manager store for its payloads and by the local store for its key. Native buffers that held
	/// plaintext are zeroed before they are freed.
	/// </summary>
	internal static partial class Dpapi
	{
		const int CryptProtectUiForbidden = 1;

		/// <summary>
		/// Protects (<paramref name="protect"/>) or unprotects <paramref name="input"/> with <paramref name="entropy"/>.
		/// On failure returns <see langword="false"/> with the Win32 error message.
		/// </summary>
		public static bool TryTransform(byte[] input, byte[] entropy, bool protect, out byte[]? output, out string? error)
		{
			output = null;

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
					error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
					return false;
				}

				try
				{
					output = new byte[outputBlob.Size];
					Marshal.Copy(outputBlob.Data, output, 0, output.Length);
				}
				finally
				{
					// The decrypt output holds plaintext. ZeroAndFree cannot be reused here because this block is
					// LocalAlloc-owned rather than HGlobal-owned.
					if (outputBlob.Size > 0)
						Marshal.Copy(new byte[outputBlob.Size], 0, outputBlob.Data, outputBlob.Size);

					LocalFree(outputBlob.Data);
				}

				error = null;
				return true;
			}
			finally
			{
				ZeroAndFree(inputPointer,   input.Length);
				ZeroAndFree(entropyPointer, entropy.Length);
			}
		}

		/// <summary>Zeroes and frees an <see cref="Marshal.AllocHGlobal(int)"/> block.</summary>
		public static void ZeroAndFree(IntPtr pointer, int length)
		{
			if (pointer == IntPtr.Zero)
				return;

			if (length > 0)
				Marshal.Copy(new byte[length], 0, pointer, length);

			Marshal.FreeHGlobal(pointer);
		}

		[StructLayout(LayoutKind.Sequential)]
		readonly struct DataBlob(int size, IntPtr data)
		{
			public readonly int    Size = size;
			public readonly IntPtr Data = data;
		}

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
