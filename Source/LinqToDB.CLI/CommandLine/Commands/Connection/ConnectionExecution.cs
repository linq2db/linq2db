using System;
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
		/// Loads the provider, creates <see cref="DataOptions"/> and, when impersonation is enabled, logs on as the
		/// resolved Windows user.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The impersonated identity may not be able to read anything on this machine, so everything that is loaded
		/// from disk is loaded first, under the original process account: external provider assemblies, the tool's
		/// assemblies and their references, the client library's initialization, native libraries, satellite resources
		/// and configuration files (see <see cref="ImpersonationPreload"/>).
		/// </para>
		/// <para>
		/// Resolving the provider can open a connection to detect the server version. That is database work, so with
		/// impersonation it runs as the impersonated identity, after the preload.
		/// </para>
		/// </remarks>
		public static async Task<ConnectionExecutionResult<ConnectionScope>> OpenAsync(ICliEnvironment environment, ConnectionSettings settings)
		{
			if (!ExternalProviderLoader.LoadExternalProvider(settings.Provider, settings.ProviderLocation, out var error))
				return new ConnectionExecutionResult<ConnectionScope>(StatusCodes.EXPECTED_ERROR, error, null);

			if (!settings.Impersonate)
				return CreateScope(settings, DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString), null);

			ImpersonationPreload.Run(settings);

			var session = environment.StartImpersonation(settings.User!, settings.Password!, settings.ImpersonateMode);

			try
			{
				var dataProvider = await session.RunAsync(() => Task.FromResult(DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString)));

				// The preload has normally initialized this client already; this covers a detection that picked another one.
				//
				if (dataProvider != null)
					ImpersonationPreload.InitializeClient(dataProvider, settings.ConnectionString);

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
