using System;
using System.Collections.Generic;
using System.IO;

using LINQPad.Extensibility.DataContext;

using LinqToDB.Data;
using LinqToDB.LINQPad.UI;
using LinqToDB.Mapping;

using System.Reflection;
using System.Threading.Tasks;
using System.Globalization;

#if NETFRAMEWORK
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using System.Reflection.Metadata;

using Microsoft.Extensions.Logging;
#endif

namespace LinqToDB.LINQPad;

/// <summary>
/// Contains shared driver code for dynamic (scaffolded) and static (precompiled) drivers.
/// </summary>
internal static class DriverHelper
{
	public const string Name   = "Linq To DB";
	public const string Author = "Linq To DB Team";

	/// <summary>
	/// Returned by <see cref="DataContextDriver.GetNamespacesToAdd(IConnectionInfo)"/> method implementation.
	/// </summary>
	public static readonly IReadOnlyCollection<string> DefaultImports =
	[
		"LinqToDB",
		"LinqToDB.Data",
		"LinqToDB.Mapping",
	];

	/// <summary>
	/// Initialization method, called from driver's static constructor.
	/// </summary>
	public static void Init()
	{
#if NETFRAMEWORK
		// Dynamically resolve assembly bindings to currently used assembly version for transitive dependencies. Used by.NET Framework build (LINQPad 5).

		// manage transitive dependencies dll hell
		// separate resolvers registered to avoid resolve errors from resolvers itself

		// linq2db version resolver could be needed for:
		// - iSeries provider
		// - static contexts
		RegisterResolver("linq2db", static () => typeof(DataContext).Assembly);

		RegisterResolver("System.Threading.Tasks.Extensions", static () => typeof(ValueTask).Assembly);
		RegisterResolver("System.Runtime.CompilerServices.Unsafe", static () => typeof(Unsafe).Assembly);
		RegisterResolver("System.Numerics.Vectors", static () => typeof(Vector).Assembly);
		RegisterResolver("System.Memory", static () => typeof(Span<>).Assembly);
		RegisterResolver("System.Buffers", static () => typeof(ArrayPool<>).Assembly);
		RegisterResolver("System.Text.Json", static () => typeof(JsonDocument).Assembly);
		RegisterResolver("System.Diagnostics.DiagnosticSource", static () => typeof(DiagnosticSource).Assembly);
		RegisterResolver("Microsoft.Bcl.AsyncInterfaces", static () => typeof(IAsyncDisposable).Assembly);
		RegisterResolver("Microsoft.Extensions.Logging.Abstractions", static () => typeof(ILogger).Assembly);
		RegisterResolver("System.Collections.Immutable", static () => typeof(ImmutableArray).Assembly);
		// Npgsql 8.0.9 references System.Threading.Channels 8.0.0.0; the driver ships 10.0.0.3.
		RegisterResolver("System.Threading.Channels", static () => typeof(Channel).Assembly);

		// Needed again as of the Roslyn 5.6 bump (6.4.0): Microsoft.CodeAnalysis 5.6 references
		// System.Reflection.Metadata 10.0.0.0, but the driver ships 10.0.0.1, and .NET Framework binds
		// by exact version — without this resolver LINQPad 5 fails at model build with
		// "FileLoadException: Could not load file or assembly 'System.Reflection.Metadata,
		// Version=10.0.0.0' ... manifest definition does not match the assembly reference".
		RegisterResolver("System.Reflection.Metadata", static () => typeof(Blob).Assembly);

		AppDomain.CurrentDomain.DomainUnload += static (_, _) => DatabaseProviders.Unload();

		static void RegisterResolver(string asemblyName, Func<Assembly> resolver)
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				var requestedAssembly = new AssemblyName(args.Name!);

				if (string.Equals(requestedAssembly.Name, asemblyName, StringComparison.Ordinal))
					return resolver();

				return null;
			};
		}
#endif

		DatabaseProviders.Init();
	}

	/// <summary>
	/// Implements <see cref="DataContextDriver.InitializeContext(IConnectionInfo, object, QueryExecutionManager)"/> method.
	/// </summary>
	public static MappingSchema InitializeContext(IConnectionInfo cxInfo, IDataContext context, QueryExecutionManager executionManager)
	{
		try
		{
			var settings = ConnectionSettings.Load(cxInfo);

			// apply context-specific Linq To DB options
			Common.Configuration.Linq.OptimizeJoins = settings.LinqToDB.OptimizeJoins;

			context.UseQueryTraceOptions(o => o.WithOnTrace(GetSqlLogAction(executionManager)));

			DataConnection.TurnTraceSwitchOn();

			return context.MappingSchema;

			// Implements Linq To DB connection logging handler to feed SQL logs to LINQPad.
			static Action<TraceInfo> GetSqlLogAction(QueryExecutionManager executionManager)
			{
				return info =>
				{
					switch (info.TraceInfoStep)
					{
						case TraceInfoStep.BeforeExecute:
							// log SQL query
							executionManager.SqlTranslationWriter.WriteLine(info.SqlText);
							break;
						case TraceInfoStep.Error:
							// log error
							if (info.Exception != null)
							{
								for (var ex = info.Exception; ex != null; ex = ex.InnerException)
								{
									executionManager.SqlTranslationWriter.WriteLine();
									executionManager.SqlTranslationWriter.WriteLine("/*");
									executionManager.SqlTranslationWriter.WriteLine($"Exception: {ex.GetType()}");
									executionManager.SqlTranslationWriter.WriteLine($"Message  : {ex.Message}");
									executionManager.SqlTranslationWriter.WriteLine(ex.StackTrace);
									executionManager.SqlTranslationWriter.WriteLine("*/");
								}
							}

							break;
						case TraceInfoStep.Completed:
							// log data reader execution stats
							executionManager.SqlTranslationWriter.WriteLine(string.Create(CultureInfo.InvariantCulture, $"-- Data read time: {info.ExecutionTime}. Records fetched: {info.RecordsAffected}.\r\n"));
							break;
						case TraceInfoStep.AfterExecute:
							// log query execution stats
							if (info.RecordsAffected != null)
								executionManager.SqlTranslationWriter.WriteLine(string.Create(CultureInfo.InvariantCulture, $"-- Execution time: {info.ExecutionTime}. Records affected: {info.RecordsAffected}.\r\n"));
							else
								executionManager.SqlTranslationWriter.WriteLine(string.Create(CultureInfo.InvariantCulture, $"-- Execution time: {info.ExecutionTime}\r\n"));
							break;
					}
				};
			}
		}
		catch (Exception ex)
		{
			HandleException(ex, nameof(InitializeContext));
			return MappingSchema.Default;
		}
	}

	/// <summary>
	/// Implements <see cref="DataContextDriver.GetConnectionDescription(IConnectionInfo)"/> method.
	/// </summary>
	public static string GetConnectionDescription(IConnectionInfo cxInfo)
	{
		try
		{
			var settings = ConnectionSettings.Load(cxInfo);

			// this is default connection name string in connecion explorer when user doesn't specify own name
			return $"[Linq To DB: {settings.Connection.Provider}] {settings.Connection.Server}\\{settings.Connection.DatabaseName} (v.{settings.Connection.DbVersion})";
		}
		catch (Exception ex)
		{
			HandleException(ex, nameof(GetConnectionDescription));
			return "Error";
		}
	}

	/// <summary>
	/// Implements <see cref="DataContextDriver.ClearConnectionPools(IConnectionInfo)"/> method.
	/// </summary>
	public static void ClearConnectionPools(IConnectionInfo cxInfo)
	{
		try
		{
			var settings = ConnectionSettings.Load(cxInfo);
			DatabaseProviders.GetProvider(settings.Connection.Database).ClearAllPools(settings.Connection.Provider!);
		}
		catch (Exception ex)
		{
			HandleException(ex, nameof(ClearConnectionPools));
		}
	}

	public static bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isDynamic)
	{
		var settings = ConnectionSettings.Load(cxInfo);

		// WPF is available to a driver only from this method, see Notification
		Notification.BeginConnectionDialog();

		try
		{
			var model = new SettingsModel(settings, !isDynamic);

			if (SettingsDialog.Show(
				model,
				isDynamic ? TestDynamicConnection : TestStaticConnection,
				isDynamic ? "Connection to database failed." : "Invalid configuration."))
			{
				model.Save();
				settings.Save(cxInfo);
				return true;
			}
		}
		finally
		{
			Notification.EndConnectionDialog();
		}

		return false;

		static Exception? TestStaticConnection(SettingsModel model)
		{
			try
			{
				// basic checks
				if (model.StaticConnection.ContextAssemblyPath == null)
					throw new LinqToDBLinqPadException("Data context assembly not specified");

				if (model.StaticConnection.ContextTypeName == null)
					throw new LinqToDBLinqPadException("Data context class not specified");

				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		Exception? TestDynamicConnection(SettingsModel model)
		{
			try
			{
				// TODO: add secondary connection test
				if (model.DynamicConnection.Database == null)
					throw new LinqToDBLinqPadException("Database is not selected");

				if (model.DynamicConnection.Provider == null)
					throw new LinqToDBLinqPadException("Database provider is not selected");

				if (model.DynamicConnection.ConnectionString == null)
					throw new LinqToDBLinqPadException("Connection string is not specified");

				if (model.DynamicConnection.SecondaryProvider != null
					&& string.Equals(model.DynamicConnection.Provider.Name, model.DynamicConnection.SecondaryProvider.Name, StringComparison.Ordinal))
					throw new LinqToDBLinqPadException("Secondary connection shouldn't use same provider type as primary connection");

				if (model.DynamicConnection.Database.IsProviderPathSupported(model.DynamicConnection.Provider.Name))
				{
					if (model.DynamicConnection.ProviderPath == null)
						throw new LinqToDBLinqPadException("Provider path is not specified");
					if (!File.Exists(model.DynamicConnection.ProviderPath))
						throw new LinqToDBLinqPadException($"Cannot access provider assembly at {model.DynamicConnection.ProviderPath}");
				}

#if NETFRAMEWORK
				// LINQPad 5 ships every database client with the driver, so the connection can be opened here
				OpenConnections(settings);

				return null;
#else
				// This dialog runs in LINQPad's own process, which has only the driver's static dependencies:
				// database clients are provisioned per connection (see OverrideDriverDependencies) and resolve
				// in the driver process, so the connection must be opened there. LINQPad rolls back changes to
				// cxInfo when the dialog is cancelled, so saving before the test is safe.
				settings.Save(cxInfo);

				var error = DataContextDriver.TestConnection(cxInfo, out _);

				return error == null ? null : new LinqToDBLinqPadException(error);
#endif
			}
			catch (Exception ex)
			{
				return ex;
			}
		}
	}

#if !NETFRAMEWORK
	/// <summary>
	/// Implements <see cref="DataContextDriver.TestConnectionCore(IConnectionInfo)"/>. Runs in the driver
	/// process, where the database clients provisioned for the connection can be loaded.
	/// </summary>
	public static string? TestConnection(IConnectionInfo cxInfo)
	{
		try
		{
			OpenConnections(ConnectionSettings.Load(cxInfo));

			return null;
		}
		catch (Exception ex)
		{
			Notification.Error(ex, "Connection test failed.", "Connection Test");

			return Notification.FormatMessages(ex);
		}
	}
#endif

	private static void OpenConnections(ConnectionSettings settings)
	{
		var database         = DatabaseProviders.GetProvider(settings.Connection.Database);
		var connectionString = PasswordManager.ResolvePasswordManagerFields(settings.Connection.ConnectionString!);
		var provider         = DatabaseProviders.GetDataProvider(settings.Connection.Provider, connectionString, settings.Connection.ProviderPath);

		using (var cn = provider.CreateConnection(connectionString))
			cn.Open();

		if (database.SupportsSecondaryConnection
			&& settings.Connection.SecondaryProvider != null
			&& settings.Connection.SecondaryConnectionString != null)
		{
			var secondaryConnectionString = PasswordManager.ResolvePasswordManagerFields(settings.Connection.SecondaryConnectionString);
			var secondaryProvider         = DatabaseProviders.GetDataProvider(settings.Connection.SecondaryProvider, secondaryConnectionString, null);

			using var cn = secondaryProvider.CreateConnection(secondaryConnectionString);
			cn.Open();
		}
	}

#if !NETFRAMEWORK
	/// <summary>
	/// Implements <see cref="DataContextDriver.OverrideDriverDependencies(DriverDependencyInfo)"/> method.
	/// Database clients are not declared as dependencies of the driver package, so a user downloads only
	/// the client libraries for databases he actually connects to.
	/// </summary>
	public static void OverrideDriverDependencies(DriverDependencyInfo dependencyInfo, bool isDynamic)
	{
		try
		{
			var settings = ConnectionSettings.Load(dependencyInfo.CxInfo);
			var packages = new HashSet<(string Id, string Version)>();

			if (isDynamic && settings.Connection.Database != null && settings.Connection.Provider != null)
			{
				var provider = DatabaseProviders.GetProvider(settings.Connection.Database);

				packages.UnionWith(provider.GetNuGetPackages(settings.Connection.Provider));

				if (settings.Connection.SecondaryProvider != null)
					packages.UnionWith(provider.GetNuGetPackages(settings.Connection.SecondaryProvider));
			}
			// a static context selects its provider itself, so it cannot be detected - but the connection may
			// name the database, and then the clients of that one are enough
			else if (!isDynamic
				&& settings.StaticContext.Database != null
				&& DatabaseProviders.Providers.TryGetValue(settings.StaticContext.Database, out var staticProvider))
			{
				packages.UnionWith(DatabaseProviders.GetNuGetPackages(staticProvider));
			}
			// a connection being created has no provider selected yet, and a static context that names no
			// database could be any of them: the client is unknown, so all are provisioned, as before
			else
			{
				packages.UnionWith(DatabaseProviders.GetAllNuGetPackages());
			}

			if (packages.Count > 0)
				dependencyInfo.AddNuGetPackages(packages);
		}
		catch (Exception ex)
		{
			HandleException(ex, nameof(OverrideDriverDependencies));
		}
	}
#endif

	// intercepts exceptions from driver to linqpad
	public static void HandleException(Exception ex, string method)
	{
		Notification.Error(ex, string.Create(CultureInfo.InvariantCulture, $"Unhandled error in method '{method}':"), "Linq To DB Driver Error");
	}

	/// <summary>
	/// Same as <see cref="HandleException"/> for failures the driver recovers from: they go to the log
	/// without interrupting the user with a dialog.
	/// </summary>
	private static void LogException(Exception ex, string method)
	{
		Notification.Log(ex, string.Create(CultureInfo.InvariantCulture, $"Recovered error in method '{method}':"));
	}

	public static IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
	{
#if !NETFRAMEWORK
		yield return "*";
#endif
		yield return typeof(DataConnection).Assembly.Location;
		yield return typeof(LINQPadDataConnection).Assembly.Location;

		var settings = ConnectionSettings.Load(cxInfo);

		Type              cnType;
		IDatabaseProvider provider;
		try
		{
			provider     = DatabaseProviders.GetProvider(settings.Connection.Database);
			using var cn = DatabaseProviders.CreateConnection(ConnectionSettings.Load(cxInfo));
			cnType       = cn.GetType();
		}
		catch (Exception ex)
		{
			// LINQPad also asks for these while the connection dialog is open, from its own process, where the
			// database client provisioned for the connection cannot be loaded. The list is advisory - the call
			// made from the query process returns the full set - so this must not interrupt the user.
			LogException(ex, nameof(GetAssembliesToAdd));
			yield break;
		}

		foreach (var assembly in provider.GetAdditionalReferences(settings.Connection.Provider!))
			yield return assembly.FullName!;

		yield return cnType.Assembly.Location;
	}
}
