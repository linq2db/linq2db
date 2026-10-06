using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Loads, under the original process account, everything from disk that database work needs, so that nothing is
	/// read from disk while impersonating.
	/// </summary>
	/// <remarks>
	/// The impersonated identity may have no rights on this machine at all: it may not be able to read the tool's
	/// installation folder, the .NET installation (a per-user install lives in the user profile) or the provider's
	/// folder. Code that runs in the session loads assemblies lazily (the JIT loads an assembly on the first method
	/// that references it), loads native libraries on first P/Invoke call, reads satellite resource assemblies for
	/// error messages and reads application configuration files on the client's first use. Each step below moves
	/// one of those reads before the session; none of them executes database work.
	/// </remarks>
	internal static class ImpersonationPreload
	{
		static readonly Lock _lock = new();

		static bool                                      _applicationAssembliesLoaded;
		static bool                                      _configurationRead;
		static readonly HashSet<Assembly>                _referencesLoaded      = new();
		static readonly HashSet<(Assembly, string)>      _nativeLibrariesLoaded = new();
		static readonly HashSet<(Assembly, string)>      _satellitesLoaded      = new();
		static readonly ConcurrentDictionary<Type, bool> _clientsInitialized    = new();

		/// <summary>
		/// Loads everything the provider named in <paramref name="settings"/> needs, without connecting to the
		/// database.
		/// </summary>
		public static void Run(ConnectionSettings settings)
		{
			lock (_lock)
			{
				LoadApplicationAssemblies();

				// Resolving the provider without a connection string never connects: version detection needs one, so
				// it falls back to the default dialect. That provider uses the same ADO.NET client as the provider
				// resolved later in the session (only the dialect version differs), so its client, the provider's
				// adapter and the detection code all initialize here, as the process account.
				//
				try
				{
					var dataProvider = DataConnection.GetDataProvider(settings.Provider, connectionString: null!);

					if (dataProvider != null)
						InitializeClient(dataProvider, settings.ConnectionString);
				}
				catch (Exception)
				{
					// The provider or its client cannot be created as the process account either; the resolution in
					// the session fails the same way and reports it.
				}

				ReadApplicationConfiguration();
				LoadReferencedAssemblies();
				LoadSatelliteAssemblies(CultureInfo.CurrentUICulture);
			}
		}

		/// <summary>
		/// Runs the ADO.NET client's first-use initialization and loads the native libraries it and the assemblies it
		/// references import. The native libraries of a client are loaded once.
		/// </summary>
		public static void InitializeClient(IDataProvider dataProvider, string connectionString)
		{
			// Creating (not opening) a connection and a command runs the client's static constructors and loads the
			// assemblies that run them.
			//
			using var connection = dataProvider.CreateConnection(connectionString);
			using var command    = connection.CreateCommand();

			if (!_clientsInitialized.TryAdd(connection.GetType(), true))
				return;

			lock (_lock)
			{
				// Native libraries are loaded on the first call into them: the SQL Server network interface on
				// Windows, the TLS and GSSAPI shims of the .NET runtime on Linux and macOS. Only libraries imported by
				// the client and the assemblies it references are loaded, not those of every provider shipped with the
				// tool.
				//
				foreach (var assembly in GetReferenceClosure(connection.GetType().Assembly, new HashSet<Assembly>()))
					LoadImportedLibraries(assembly);
			}
		}

		/// <summary>
		/// Loads every assembly shipped with the tool, so that nothing the database work needs (provider adapters,
		/// provider value types, output formatting) is loaded from disk later under the impersonated identity.
		/// </summary>
		static void LoadApplicationAssemblies()
		{
			if (_applicationAssembliesLoaded)
				return;

			var baseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

			if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
			{
				foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
				{
					if (!Path.GetFullPath(path).StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
						continue;

					TryLoad(AssemblyLoadContext.Default, new AssemblyName(Path.GetFileNameWithoutExtension(path)));
				}
			}

			_applicationAssembliesLoaded = true;
		}

		/// <summary>
		/// Loads the assemblies referenced by every loaded assembly, transitively: the .NET assemblies the JIT would
		/// otherwise load on first use (networking, cryptography, expression compilation, JSON output) and the
		/// dependencies of an external provider. References are resolved in the load context of the referencing
		/// assembly, as the runtime does.
		/// </summary>
		static void LoadReferencedAssemblies()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				GetReferenceClosure(assembly, _referencesLoaded);
		}

		/// <summary>
		/// Loads the assemblies <paramref name="root"/> references, transitively, and returns those not in
		/// <paramref name="visited"/> yet.
		/// </summary>
		static List<Assembly> GetReferenceClosure(Assembly root, HashSet<Assembly> visited)
		{
			var closure = new List<Assembly>();
			var pending = new Stack<Assembly>();

			pending.Push(root);

			while (pending.Count > 0)
			{
				var assembly = pending.Pop();

				if (assembly.IsDynamic || !visited.Add(assembly))
					continue;

				closure.Add(assembly);

				var context = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;

				foreach (var reference in assembly.GetReferencedAssemblies())
					if (TryLoad(context, reference) is { } referenced)
						pending.Push(referenced);
			}

			return closure;
		}

		static Assembly? TryLoad(AssemblyLoadContext context, AssemblyName name)
		{
			try
			{
				return context.LoadFromAssemblyName(name);
			}
			catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
			{
				// Not loadable as the process account either (e.g. a reference used only on another platform); code
				// that needs it fails the same way later.
				return null;
			}
		}

		/// <summary>
		/// Loads the native libraries that <paramref name="assembly"/> imports and that ship with the tool, the
		/// provider or the .NET runtime. Libraries of the operating system are left to the system loader: every account
		/// can read them, and trying to load all of them would load unrelated ones (e.g. the Windows UI libraries).
		/// </summary>
		static unsafe void LoadImportedLibraries(Assembly assembly)
		{
			if (assembly.IsDynamic || !assembly.TryGetRawMetadata(out var blob, out var length))
				return;

			var reader      = new MetadataReader(blob, length);
			var directories = GetNativeLibraryDirectories(assembly);

			for (var row = 1; row <= reader.GetTableRowCount(TableIndex.ModuleRef); row++)
			{
				var name = reader.GetString(reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(row)).Name);

				if (!_nativeLibrariesLoaded.Add((assembly, name)))
					continue;

				if (directories.Exists(directory => GetNativeLibraryFileNames(name).Any(file => File.Exists(Path.Combine(directory, file)))))
					NativeLibrary.TryLoad(name, assembly, null, out _);
			}
		}

		static List<string> GetNativeLibraryDirectories(Assembly assembly)
		{
			var directories = new List<string>
			{
				AppContext.BaseDirectory,
				RuntimeEnvironment.GetRuntimeDirectory(),
			};

			if (Path.GetDirectoryName(assembly.Location) is { Length: > 0 } assemblyDirectory)
				directories.Add(assemblyDirectory);

			// The runtime finds native assets of a framework-dependent application through the native search paths
			// in its dependency manifest (e.g. runtimes/win-x64/native).
			//
			if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string searchDirectories)
				directories.AddRange(searchDirectories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));

			return directories;
		}

		static IEnumerable<string> GetNativeLibraryFileNames(string name)
		{
			yield return name;

			foreach (var prefix in new[] { "", "lib" })
				foreach (var suffix in new[] { ".dll", ".so", ".dylib" })
					yield return prefix + name + suffix;
		}

		/// <summary>
		/// Loads the satellite resource assemblies of the current UI culture and its parent cultures, so that messages
		/// from the client (e.g. SQL Server errors) keep the user's language: a resource manager in the session finds
		/// them already loaded. Only satellites that exist are loaded (next to the assembly, or in the application folder
		/// where the dependency manifest places those of RID-specific assemblies), so no assembly is probed in vain.
		/// Lookups for satellites that do not exist still probe the disk in the session; they fail there as they fail
		/// here, and the neutral resources are used either way.
		/// </summary>
		static void LoadSatelliteAssemblies(CultureInfo culture)
		{
			for (; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (assembly.IsDynamic || !_satellitesLoaded.Add((assembly, culture.Name)) || !SatelliteExists(assembly, culture))
						continue;

					try
					{
						assembly.GetSatelliteAssembly(culture);
					}
					catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
					{
						// Not loadable as the process account either; the resource manager falls back to the neutral
						// resources.
					}
				}
			}

			static bool SatelliteExists(Assembly assembly, CultureInfo culture)
			{
				var fileName = assembly.GetName().Name + ".resources.dll";

				return File.Exists(Path.Combine(AppContext.BaseDirectory, culture.Name, fileName))
					|| Path.GetDirectoryName(assembly.Location) is { Length: > 0 } directory && File.Exists(Path.Combine(directory, culture.Name, fileName));
			}
		}

		/// <summary>
		/// ADO.NET clients read their sections from the application configuration file on first use
		/// (Microsoft.Data.SqlClient reads its authentication providers and retry logic sections when the first
		/// connection opens). The configuration system reads and caches the machine, application and user configuration
		/// files on its first call, so any section read here moves those file reads out of the session.
		/// </summary>
		static void ReadApplicationConfiguration()
		{
			if (_configurationRead)
				return;

			_configurationRead = true;

			// Only clients that depend on System.Configuration.ConfigurationManager use it; the tool does not reference
			// it directly, so it is looked up among the assemblies loaded above.
			//
			var getSection = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(static a => string.Equals(a.GetName().Name, "System.Configuration.ConfigurationManager", StringComparison.Ordinal))
				?.GetType("System.Configuration.ConfigurationManager")
				?.GetMethod("GetSection", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);

			try
			{
				getSection?.InvokeExt(null, ["SqlClientAuthenticationProviders"]);
			}
			catch (Exception)
			{
				// An invalid configuration file; the client reports it when it reads the file.
			}
		}
	}
}
