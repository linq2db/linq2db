using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Loads a native library from a known file.
	/// </summary>
	public static partial class NativeLibraryLoader
	{
		const uint LoadLibrarySearchDllLoadDir = 0x00000100;
		const uint LoadLibrarySearchSystem32   = 0x00000800;

		/// <summary>
		/// Loads the native library at <paramref name="path"/>, which must be fully qualified. On Windows the libraries
		/// it depends on are looked up only in its own folder and in System32, never in the current directory or the
		/// <c>PATH</c>: a dependency that is missing (e.g. a C runtime) fails the load instead of being picked up from an
		/// untrusted folder.
		/// </summary>
		/// <returns><see langword="false"/>, with the loader's message in <paramref name="error"/>, when the library cannot be loaded.</returns>
		public static bool TryLoad(string path, out string? error)
		{
			if (!Path.IsPathFullyQualified(path))
				throw new ArgumentException($"Native library path '{path}' is not fully qualified.", nameof(path));

			if (OperatingSystem.IsWindows())
			{
				if (LoadLibraryEx(path, IntPtr.Zero, LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32) != IntPtr.Zero)
				{
					error = null;
					return true;
				}

				error = new Win32Exception(Marshal.GetLastPInvokeError()).Message;
				return false;
			}

			// dlopen of a path never searches the current directory, for the library or its dependencies.
			//
			try
			{
				NativeLibrary.Load(path);
				error = null;
				return true;
			}
			catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException)
			{
				error = ex.Message;
				return false;
			}
		}

		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		private static partial IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);
	}
}
