using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Shared provider loading, DataOptions creation, and the optional impersonation boundary.
	/// </summary>
	internal static class ConnectionExecution
	{
		/// <summary>
		/// Loads the provider, creates <see cref="DataOptions"/> and, when impersonation is enabled, logs on as the
		/// resolved Windows user.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The impersonated identity usually cannot read the tool's own files, so everything that loads code runs
		/// first, under the original process account: external provider assemblies, the tool's own assemblies and the
		/// client library's static initialization and native libraries.
		/// </para>
		/// <para>
		/// Resolving the provider can open a connection to detect the server version. That is database work, so with
		/// impersonation it runs as the impersonated identity.
		/// That detection connection is then the client library's first use, so its initialization also happens in the
		/// session; with a versioned provider name nothing connects before <see cref="WarmUpClient"/>.
		/// </para>
		/// </remarks>
		public static async Task<ConnectionExecutionResult<ConnectionScope>> OpenAsync(ICliEnvironment environment, ConnectionSettings settings)
		{
			if (!ExternalProviderLoader.LoadExternalProvider(settings.Provider, settings.ProviderLocation, out var error))
				return new ConnectionExecutionResult<ConnectionScope>(StatusCodes.EXPECTED_ERROR, error, null);

			if (!settings.Impersonate)
				return CreateScope(settings, DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString), null);

			LoadApplicationAssemblies();

			var session = environment.StartImpersonation(settings.User!, settings.Password!, settings.ImpersonateMode);

			try
			{
				var dataProvider = await session.RunAsync(() => Task.FromResult(DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString)));

				if (dataProvider != null)
					WarmUpClient(dataProvider, settings.ConnectionString);

				var result = CreateScope(settings, dataProvider, session);

				if (result.Value == null)
					session.Dispose();

				return result;
			}
			catch
			{
				session.Dispose();
				throw;
			}
		}

		static ConnectionExecutionResult<ConnectionScope> CreateScope(ConnectionSettings settings, IDataProvider? dataProvider, IImpersonationSession? session)
		{
			if (dataProvider == null)
				return new ConnectionExecutionResult<ConnectionScope>(StatusCodes.EXPECTED_ERROR, CreateProviderCreationError(settings.Provider), null);

			var dataOptions = new DataOptions().UseConnectionString(dataProvider, settings.ConnectionString);

			if (settings.CommandTimeout > 0)
				dataOptions = dataOptions.UseCommandTimeout(settings.CommandTimeout);

			return new ConnectionExecutionResult<ConnectionScope>(StatusCodes.SUCCESS, null, new ConnectionScope(dataOptions, dataProvider, session));
		}

		static bool _applicationAssembliesLoaded;

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

					try
					{
						Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
					}
					catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
					{
						// Not loadable as the process account either; code that needs it fails the same way later.
					}
				}
			}

			_applicationAssembliesLoaded = true;
		}

		static readonly ConcurrentDictionary<Assembly, bool> _nativeLibrariesLoaded = new();

		/// <summary>
		/// Runs the client library's first-use initialization: creating (not opening) a connection runs its static
		/// constructors, and loading the native libraries it imports covers those loaded on first open
		/// (e.g. the SQL Server network interface on Windows).
		/// </summary>
		static void WarmUpClient(IDataProvider dataProvider, string connectionString)
		{
			using var connection = dataProvider.CreateConnection(connectionString);

			var clientAssembly = connection.GetType().Assembly;

			if (!_nativeLibrariesLoaded.TryAdd(clientAssembly, true))
				return;

			foreach (var library in GetImportedLibraries(clientAssembly))
				NativeLibrary.TryLoad(library, clientAssembly, null, out _);

			static HashSet<string> GetImportedLibraries(Assembly assembly)
			{
				Type?[] types;

				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types;
				}

				var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (var type in types)
				{
					if (type == null)
						continue;

					foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					{
						if (!method.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
							continue;

						foreach (var import in method.GetAttributes<DllImportAttribute>(inherit: false))
							libraries.Add(import.Value);
					}
				}

				return libraries;
			}
		}

		static string CreateProviderCreationError(string provider)
		{
			var suggestion = TryGetProviderNameSuggestion(provider);

			if (suggestion != null)
				return $"Cannot create database provider '{provider}'. Provider name '{provider}' looks like a test data source alias. linq2db CLI expects a provider name registered by linq2db itself; use '{suggestion}' or another canonical provider name in CLI configuration.";

			return $"Cannot create database provider '{provider}'. Verify that the configured provider name is a linq2db provider name, not a test data source alias, and that any required provider assembly was loaded with '--provider-location'.";
		}

		static string? TryGetProviderNameSuggestion(string provider)
		{
			if (provider.StartsWith("Oracle.", StringComparison.OrdinalIgnoreCase)
				&& provider.EndsWith(".Managed", StringComparison.OrdinalIgnoreCase))
			{
				return "Oracle.Managed";
			}

			return null;
		}
	}
}
