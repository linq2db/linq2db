using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

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
	/// error messages and reads application configuration files on the client's first use. The preload opens a
	/// connection with the command's own provider and connection string, so that the client loads what connecting
	/// needs, and loads the managed code that running the command needs beyond that.
	/// </remarks>
	internal static class ImpersonationPreload
	{
		// Bounds a warm-up whose client does not apply its own connect timeout.
		//
		static readonly TimeSpan _warmUpTimeout = TimeSpan.FromSeconds(30);

		static readonly Lock _lock = new();

		static bool                                 _applicationAssembliesLoaded;
		static readonly HashSet<Assembly>           _referencesLoaded = new();
		static readonly HashSet<(Assembly, string)> _satellitesLoaded = new();

		/// <summary>
		/// Loads everything the command described by <paramref name="settings"/> needs.
		/// </summary>
		public static async Task RunAsync(ConnectionSettings settings)
		{
			IDataProvider? dataProvider;

			lock (_lock)
			{
				LoadApplicationAssemblies();
				dataProvider = TryGetDataProvider(settings.Provider);
			}

			if (dataProvider != null)
				await WarmUpConnectionAsync(dataProvider, settings.ConnectionString);

			lock (_lock)
			{
				LoadReferencedAssemblies();
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
		/// nothing from the connection string reaches the output. The pool is cleared afterwards, so that the session
		/// never gets a connection opened as the process account, and the client cannot keep a pooled error for it.
		/// </remarks>
		static async Task WarmUpConnectionAsync(IDataProvider dataProvider, string connectionString)
		{
			using var timeout = new CancellationTokenSource(_warmUpTimeout);

			try
			{
				await using var db = new DataConnection(new DataOptions().UseConnectionString(dataProvider, connectionString));

				await db.OpenDbConnectionAsync(timeout.Token);
			}
			catch (Exception)
			{
				// Expected when the process account cannot connect; see remarks.
			}
			finally
			{
				ClearPool(dataProvider, connectionString);
			}
		}

		/// <summary>
		/// Clears the client's pool for <paramref name="connectionString"/> through the static <c>ClearPool</c> method of
		/// its connection type, which the pooling ADO.NET clients have (SqlClient, Npgsql, MySqlConnector, ODP.NET,
		/// FirebirdClient, Microsoft.Data.Sqlite). A client without it is left alone.
		/// </summary>
		static void ClearPool(IDataProvider dataProvider, string connectionString)
		{
			try
			{
				using var connection = dataProvider.CreateConnection(connectionString);

				var clearPool = connection.GetType().GetMethod("ClearPool", BindingFlags.Public | BindingFlags.Static, [connection.GetType()]);

				clearPool?.InvokeExt(null, [connection]);
			}
			catch (Exception)
			{
				// Nothing was pooled if the connection cannot even be created.
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
