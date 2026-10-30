using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Windows path canonicalization: the final path of the deepest existing part of a path, as the file system resolves it
	/// (8.3 short names expanded, junctions and symbolic links followed), so two spellings of one directory compare equal
	/// and a link cannot make an outside directory look like it is inside another.
	/// </summary>
	[SupportedOSPlatform("windows")]
	internal static partial class WindowsPaths
	{
		const uint FileShareAll            = 0x1 | 0x2 | 0x4;
		const uint OpenExisting            = 3;
		const uint FileFlagBackupSemantics = 0x02000000;

		/// <summary>
		/// Returns the final path of <paramref name="path"/>: its deepest existing ancestor resolved by the file system, with
		/// the part that does not exist yet appended unchanged. Falls back to the full path when nothing can be resolved.
		/// </summary>
		public static string GetFinalPath(string path)
		{
			var full      = Path.GetFullPath(path);
			var existing  = full;
			var remainder = string.Empty;

			while (!Directory.Exists(existing) && !File.Exists(existing))
			{
				var parent = Path.GetDirectoryName(existing);

				if (parent == null)
					return full;

				remainder = Path.Combine(Path.GetFileName(existing), remainder);
				existing  = parent;
			}

			var resolved = TryGetFinalPathOfExisting(existing);

			if (resolved == null)
				return full;

			return remainder.Length == 0 ? resolved : Path.Combine(resolved, remainder.TrimEnd('\\'));
		}

		static string? TryGetFinalPathOfExisting(string path)
		{
			using var handle = CreateFile(path, 0, FileShareAll, IntPtr.Zero, OpenExisting, FileFlagBackupSemantics, IntPtr.Zero);

			if (handle.IsInvalid)
				return null;

			var capacity = 1024u;
			var buffer   = Marshal.AllocHGlobal((int)capacity * sizeof(char));

			try
			{
				var length = GetFinalPathNameByHandle(handle, buffer, capacity, 0);

				if (length > capacity)
				{
					capacity = length;
					Marshal.FreeHGlobal(buffer);
					buffer = IntPtr.Zero;
					buffer = Marshal.AllocHGlobal((int)capacity * sizeof(char));
					length = GetFinalPathNameByHandle(handle, buffer, capacity, 0);
				}

				if (length == 0 || length > capacity)
					return null;

				var result = Marshal.PtrToStringUni(buffer, (int)length);

				if (result.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
					return string.Concat(@"\\", result.AsSpan(8));

				return result.StartsWith(@"\\?\", StringComparison.Ordinal) ? result.Substring(4) : result;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		private static partial SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
		private static partial uint GetFinalPathNameByHandle(SafeFileHandle file, IntPtr filePath, uint filePathLength, uint flags);
	}
}
