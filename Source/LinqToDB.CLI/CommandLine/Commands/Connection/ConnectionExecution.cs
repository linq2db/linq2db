using System;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Shared provider loading, DataOptions creation, and optional impersonation boundary.
	/// </summary>
	internal static class ConnectionExecution
	{
		public static async Task<ConnectionExecutionResult<T>> RunAsync<T>(
			ConnectionSettings settings,
			Func<DataOptions, IDataProvider, CancellationToken, Task<T>> action,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!ExternalProviderLoader.LoadExternalProvider(settings.Provider, settings.ProviderLocation, out var error))
				return new ConnectionExecutionResult<T>(StatusCodes.EXPECTED_ERROR, error, default);

			var dataProvider = DataConnection.GetDataProvider(settings.Provider, settings.ConnectionString);

			if (dataProvider == null)
				return new ConnectionExecutionResult<T>(StatusCodes.EXPECTED_ERROR, CreateProviderCreationError(settings.Provider), default);

			var dataOptions = new DataOptions().UseConnectionString(dataProvider, settings.ConnectionString);

			if (settings.CommandTimeout > 0)
				dataOptions = dataOptions.UseCommandTimeout(settings.CommandTimeout);

			var result = settings.Impersonate
				? await WindowsImpersonation.RunAsync(
					settings.User!,
					settings.Password!,
					settings.ImpersonateMode,
					() => action(dataOptions, dataProvider, cancellationToken)).ConfigureAwait(false)
				: await action(dataOptions, dataProvider, cancellationToken).ConfigureAwait(false);

			return new ConnectionExecutionResult<T>(StatusCodes.SUCCESS, null, result);
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
