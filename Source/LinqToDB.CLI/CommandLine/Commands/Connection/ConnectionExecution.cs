using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Shared provider loading, DataOptions creation, and the optional impersonation boundary.
	/// </summary>
	internal static class ConnectionExecution
	{
		/// <summary>
		/// Loads the provider and creates <see cref="DataOptions"/> for the connection.
		/// Runs under the original process account: provider assemblies are loaded from local files.
		/// </summary>
		public static ConnectionExecutionResult<PreparedConnection> Prepare(ConnectionSettings settings)
		{
			if (!ExternalProviderLoader.LoadExternalProvider(settings.Provider, settings.ProviderLocation, out var error))
				return new ConnectionExecutionResult<PreparedConnection>(StatusCodes.EXPECTED_ERROR, error, null);

			var dataProvider = DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString);

			if (dataProvider == null)
				return new ConnectionExecutionResult<PreparedConnection>(StatusCodes.EXPECTED_ERROR, CreateProviderCreationError(settings.Provider), null);

			var dataOptions = new DataOptions().UseConnectionString(dataProvider, settings.ConnectionString);

			if (settings.CommandTimeout > 0)
				dataOptions = dataOptions.UseCommandTimeout(settings.CommandTimeout);

			return new ConnectionExecutionResult<PreparedConnection>(StatusCodes.SUCCESS, null, new PreparedConnection(dataOptions, dataProvider));
		}

		/// <summary>
		/// Runs database work, under the resolved Windows identity when impersonation is enabled.
		/// </summary>
		/// <remarks>
		/// The impersonated identity usually cannot read the tool's own files, so <paramref name="action"/> must
		/// contain only connection opening and SQL execution. Configuration, credentials, provider loading and
		/// SQL validation run before this call under the original process account.
		/// </remarks>
		public static Task<T> RunDatabaseWorkAsync<T>(ICliEnvironment environment, ConnectionSettings settings, Func<Task<T>> action)
		{
			if (!settings.Impersonate)
				return action();

			// Code the database work needs (provider value types, output formatting) is loaded on first use.
			// Load it now, while file access still uses the original process account.
			//
			LoadReferencedAssemblies();

			return environment.RunImpersonatedAsync(settings.User!, settings.Password!, settings.ImpersonateMode, action);
		}

		static bool _referencedAssembliesLoaded;

		static void LoadReferencedAssemblies()
		{
			if (_referencedAssembliesLoaded)
				return;

			foreach (var assemblyName in typeof(ConnectionExecution).Assembly.GetReferencedAssemblies())
			{
				try
				{
					Assembly.Load(assemblyName);
				}
				catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
				{
					// Not loadable as the process account either; code that needs it fails the same way later.
				}
			}

			_referencedAssembliesLoaded = true;
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
