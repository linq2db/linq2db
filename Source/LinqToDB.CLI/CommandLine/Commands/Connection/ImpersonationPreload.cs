using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider.SqlServer;

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
	/// error messages and reads application configuration files on the client's first use. The preload opens a
	/// connection with the command's own provider and connection string, so that the client loads what connecting
	/// needs, and loads the managed code that running the command needs beyond that.
	/// </remarks>
	internal static partial class ImpersonationPreload
	{
		/// <summary>
		/// How long the command waits for the warm-up connection. Some clients open synchronously or ignore
		/// cancellation, so the bound does not rely on them.
		/// </summary>
		internal static TimeSpan WarmUpTimeout { get; set; } = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Pooling keywords of the connection string, by connection type, for clients that pool connections.
		/// </summary>
		static readonly Dictionary<string, string> _poolingKeywords = new(StringComparer.Ordinal)
		{
			["Microsoft.Data.SqlClient.SqlConnection"]           = "Pooling",
			["System.Data.SqlClient.SqlConnection"]              = "Pooling",
			["Npgsql.NpgsqlConnection"]                          = "Pooling",
			["MySqlConnector.MySqlConnection"]                   = "Pooling",
			["MySql.Data.MySqlClient.MySqlConnection"]           = "Pooling",
			["Oracle.ManagedDataAccess.Client.OracleConnection"] = "Pooling",
			["Oracle.DataAccess.Client.OracleConnection"]        = "Pooling",
			["FirebirdSql.Data.FirebirdClient.FbConnection"]     = "Pooling",
			["Microsoft.Data.Sqlite.SqliteConnection"]           = "Pooling",
			["System.Data.SQLite.SQLiteConnection"]              = "Pooling",
			["AdoNetCore.AseClient.AseConnection"]               = "Pooling",
			["Sybase.Data.AseClient.AseConnection"]              = "Pooling",
			["IBM.Data.Db2.DB2Connection"]                       = "Pooling",
			["IBM.Data.DB2.Core.DB2Connection"]                  = "Pooling",
			["IBM.Data.DB2.DB2Connection"]                       = "Pooling",
			["IBM.Data.Informix.IfxConnection"]                  = "Pooling",
			["Sap.Data.Hana.HanaConnection"]                     = "Pooling",
		};

		/// <summary>
		/// Static methods by which a connection type that is not in <see cref="_poolingKeywords"/> shows that its client
		/// pools connections (System.Data.Odbc, System.Data.OleDb, Ydb).
		/// </summary>
		static readonly string[] _poolMethods = ["ClearPool", "ClearPools", "ClearAllPools", "ReleaseObjectPool"];

		static readonly Lock _lock = new();

		static bool                                 _applicationAssembliesLoaded;
		static bool                                 _spatialLibrariesLoaded;
		static readonly HashSet<Assembly>           _referencesLoaded     = new();
		static readonly HashSet<Assembly>           _runtimeImportsLoaded = new();
		static readonly HashSet<(Assembly, string)> _satellitesLoaded     = new();

		/// <summary>
		/// Loads everything the command described by <paramref name="settings"/> needs.
		/// </summary>
		public static async Task RunAsync(ConnectionSettings settings, CancellationToken cancellationToken)
		{
			IDataProvider? dataProvider;

			lock (_lock)
			{
				LoadApplicationAssemblies();
				dataProvider = TryGetDataProvider(settings.Provider);
			}

			if (dataProvider != null)
			{
				// The warm-up runs on its own task, as the process account. If it is still running when the bound
				// expires, the command goes on without it, and it finishes, disposing its connection, in the background.
				//
				await Task.WhenAny(
					Task.Run(() => WarmUpConnectionAsync(dataProvider, settings.ConnectionString, cancellationToken), CancellationToken.None),
					Task.Delay(WarmUpTimeout, cancellationToken));

				cancellationToken.ThrowIfCancellationRequested();
			}

			lock (_lock)
			{
				LoadReferencedAssemblies();
				LoadRuntimeNativeLibraries();

				if (dataProvider is SqlServerDataProvider)
					LoadSpatialNativeLibraries();

				LoadSatelliteAssemblies(CultureInfo.CurrentUICulture);
			}
		}

		/// <summary>
		/// Resolves the provider without a connection string, which never connects: version detection needs one, so
		/// it falls back to the default dialect. That provider uses the same ADO.NET client as the provider resolved
		/// later in the session (only the dialect version differs), so the server version is still detected as the
		/// impersonated user.
		/// </summary>
		static IDataProvider? TryGetDataProvider(string provider)
		{
			try
			{
				return DataConnection.GetDataProvider(provider, connectionString: null!);
			}
			catch (Exception)
			{
				// The provider or its client cannot be created as the process account either; the resolution in the
				// session fails the same way and reports it.
				return null;
			}
		}

		/// <summary>
		/// Opens and closes a connection with the command's client and connection string as the process account. Opening
		/// runs the client's connection code up to where it fails or succeeds, which loads what that code needs: the
		/// client's assemblies, native libraries (the SQL Server network interface, native SQLite or DuckDB, the TLS
		/// and GSSAPI shims of the .NET runtime), configuration files, and, on failure, message resources and whatever
		/// the client's error path loads (e.g. the runtime's native symbol reader for a stack trace of a client without
		/// a portable PDB).
		/// </summary>
		/// <remarks>
		/// Failing is expected: the process account may have no access to the database. The error is not reported, so
		/// nothing from the connection string reaches the output. The connection is never pooled, so no request can get
		/// a connection opened as the process account: a client that pools gets its pooling keyword turned off, and a
		/// client that pools by other means is not warmed up.
		/// </remarks>
		static async Task WarmUpConnectionAsync(IDataProvider dataProvider, string connectionString, CancellationToken cancellationToken)
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			timeout.CancelAfter(WarmUpTimeout);

			try
			{
				if (GetUnpooledConnectionString(dataProvider, connectionString) is not { } warmUpConnectionString)
					return;

				await using var db = new DataConnection(new DataOptions().UseConnectionString(dataProvider, warmUpConnectionString));

				await db.OpenDbConnectionAsync(timeout.Token);
			}
			catch (Exception)
			{
				// Expected when the process account cannot connect; see remarks.
			}
		}

		/// <summary>
		/// Returns <paramref name="connectionString"/> with the client's pooling turned off, the string itself for a
		/// client that does not pool, or <see langword="null"/> for a client that pools by means this does not know.
		/// </summary>
		static string? GetUnpooledConnectionString(IDataProvider dataProvider, string connectionString)
		{
			using var connection = dataProvider.CreateConnection(connectionString);

			var type = connection.GetType();

			if (_poolingKeywords.TryGetValue(type.FullName!, out var keyword))
			{
				var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

				builder[keyword] = "false";

				return builder.ConnectionString;
			}

			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

			if (Array.Exists(methods, method => _poolMethods.Contains(method.Name, StringComparer.Ordinal)))
				return null;

			return connectionString;
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
		/// <remarks>
		/// The warm-up connection runs only the client's connection code; running the command (executing it, reading
		/// rows, mapping values, formatting output) and the client's paths past a login the process account was refused
		/// use code that it never reaches.
		/// </remarks>
		static void LoadReferencedAssemblies()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				LoadReferenceClosure(assembly, _referencesLoaded);
		}

		/// <summary>
		/// Loads the assemblies <paramref name="root"/> references, transitively, skipping those in
		/// <paramref name="visited"/>.
		/// </summary>
		static void LoadReferenceClosure(Assembly root, HashSet<Assembly> visited)
		{
			var pending = new Stack<Assembly>();

			pending.Push(root);

			while (pending.Count > 0)
			{
				var assembly = pending.Pop();

				if (assembly.IsDynamic || !visited.Add(assembly))
					continue;

				var context = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;

				foreach (var reference in assembly.GetReferencedAssemblies())
					if (TryLoad(context, reference) is { } referenced)
						pending.Push(referenced);
			}
		}

		/// <summary>
		/// Loads the native shims of the .NET runtime (compression, cryptography, networking) that loaded .NET assemblies
		/// import. Like the managed references, they can be needed only past the warm-up connection: a client may
		/// decompress a response only when the command reads it (ClickHouse.Driver loads the compression shim then). Only
		/// libraries in the runtime folder are loaded, by name and for the importing assembly, as the runtime would load
		/// them on the first call.
		/// </summary>
		static unsafe void LoadRuntimeNativeLibraries()
		{
			var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.IsDynamic
					|| !_runtimeImportsLoaded.Add(assembly)
					|| !string.Equals(Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + Path.DirectorySeparatorChar), Path.GetFullPath(runtimeDirectory), StringComparison.OrdinalIgnoreCase)
					|| !assembly.TryGetRawMetadata(out var blob, out var length))
				{
					continue;
				}

				var reader = new MetadataReader(blob, length);

				for (var row = 1; row <= reader.GetTableRowCount(TableIndex.ModuleRef); row++)
				{
					var name = reader.GetString(reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(row)).Name);

					if (IsInRuntimeDirectory(name))
						NativeLibrary.TryLoad(name, assembly, null, out _);
				}
			}

			bool IsInRuntimeDirectory(string name)
			{
				foreach (var file in new[] { name, name + ".dll", name + ".so", "lib" + name + ".so", name + ".dylib", "lib" + name + ".dylib" })
					if (File.Exists(Path.Combine(runtimeDirectory, file)))
						return true;

				return false;
			}
		}

		/// <summary>
		/// Loads the native libraries Microsoft.SqlServer.Types imports (SqlServerSpatial*.dll, shipped for Windows
		/// only). SqlClient resolves the user-defined type of a <c>geography</c> or <c>geometry</c> column to that
		/// assembly by name, and it loads its native library when the first value is read, so neither the warm-up
		/// connection nor the reference closure reaches it. Each library is loaded from the file the runtime would
		/// resolve, by its full path; a library that fails to load is left alone, and a spatial read then fails as it
		/// would without impersonation.
		/// </summary>
		static unsafe void LoadSpatialNativeLibraries()
		{
			if (_spatialLibrariesLoaded || !OperatingSystem.IsWindows())
				return;

			_spatialLibrariesLoaded = true;

			var assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), static a => string.Equals(a.GetName().Name, "Microsoft.SqlServer.Types", StringComparison.Ordinal));

			if (assembly == null || assembly.IsDynamic || !assembly.TryGetRawMetadata(out var blob, out var length))
				return;

			// The runtime looks for a DllImport library in the native search directories of the dependency manifest
			// (e.g. runtimes/win-x64/native), then in the importing assembly's folder.
			//
			var directories = new List<string>();

			if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string searchDirectories)
				directories.AddRange(searchDirectories.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));

			if (Path.GetDirectoryName(assembly.Location) is { Length: > 0 } assemblyDirectory)
				directories.Add(assemblyDirectory);

			var reader = new MetadataReader(blob, length);

			for (var row = 1; row <= reader.GetTableRowCount(TableIndex.ModuleRef); row++)
			{
				var name = reader.GetString(reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(row)).Name);

				if (!name.StartsWith("SqlServerSpatial", StringComparison.OrdinalIgnoreCase))
					continue;

				// The runtime's name variants on Windows: the name, then the name with .dll unless it has an extension
				// the loader keeps. Each variant is looked up in every directory before the next one.
				//
				string[] variants = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith('.')
					? [name]
					: [name, name + ".dll"];

				var path = variants
					.SelectMany(variant => directories.Select(directory => Path.Combine(directory, variant)))
					.FirstOrDefault(File.Exists);

				if (path != null)
					NativeLibrary.TryLoad(Path.GetFullPath(path), out _);
			}
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
	}
}
