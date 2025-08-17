using System;
using System.Collections.Generic;
using System.IO;

using LINQPad.Extensibility.DataContext;

using LinqToDB.Data;
using LinqToDB.LINQPad.UI;
using LinqToDB.Mapping;

using System.Reflection;
using System.Threading.Tasks;

#if NETFRAMEWORK
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
//using System.Reflection.Metadata;

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
		"LinqToDB.Mapping"
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

		// not needed anymore?
		//RegisterResolver("System.Reflection.Metadata", static () => typeof(Blob).Assembly);

		AppDomain.CurrentDomain.DomainUnload += static (_, _) => DatabaseProviders.Unload();

		static void RegisterResolver(string asemblyName, Func<Assembly> resolver)
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				var requestedAssembly = new AssemblyName(args.Name!);

				if (requestedAssembly.Name == asemblyName)
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
							executionManager.SqlTranslationWriter.WriteLine(FormattableString.Invariant($"-- Data read time: {info.ExecutionTime}. Records fetched: {info.RecordsAffected}.\r\n"));
							break;
						case TraceInfoStep.AfterExecute:
							// log query execution stats
							if (info.RecordsAffected != null)
								executionManager.SqlTranslationWriter.WriteLine(FormattableString.Invariant($"-- Execution time: {info.ExecutionTime}. Records affected: {info.RecordsAffected}.\r\n"));
							else
								executionManager.SqlTranslationWriter.WriteLine(FormattableString.Invariant($"-- Execution time: {info.ExecutionTime}\r\n"));
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
		var model    = new SettingsModel(settings, !isDynamic);

		if (SettingsDialog.Show(
			model,
			isDynamic ? TestDynamicConnection : TestStaticConnection,
			isDynamic ? "Connection to database failed." : "Invalid configuration."))
		{
			model.Save();
			settings.Save(cxInfo);
			return true;
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

		static Exception? TestDynamicConnection(SettingsModel model)
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
					&& model.DynamicConnection.Provider.Name == model.DynamicConnection.SecondaryProvider.Name)
					throw new LinqToDBLinqPadException("Secondary connection shouldn't use same provider type as primary connection");

				if (model.DynamicConnection.Database.IsProviderPathSupported(model.DynamicConnection.Provider.Name))
				{
					if (model.DynamicConnection.ProviderPath == null)
						throw new LinqToDBLinqPadException("Provider path is not specified");
					if (!File.Exists(model.DynamicConnection.ProviderPath))
						throw new LinqToDBLinqPadException($"Cannot access provider assembly at {model.DynamicConnection.ProviderPath}");
				}

				var connectionString = PasswordManager.ResolvePasswordManagerFields(model.DynamicConnection.ConnectionString);
				var provider         = DatabaseProviders.GetDataProvider(model.DynamicConnection.Provider.Name, connectionString, model.DynamicConnection.ProviderPath);
				using (var con       = provider.CreateConnection(connectionString))
					con.Open();

				if (model.DynamicConnection.Database.SupportsSecondaryConnection
					&& model.DynamicConnection.SecondaryProvider != null
					&& model.DynamicConnection.SecondaryConnectionString != null)
				{
					var secondaryConnectionString = PasswordManager.ResolvePasswordManagerFields(model.DynamicConnection.SecondaryConnectionString);
					var secondaryProvider         = DatabaseProviders.GetDataProvider(model.DynamicConnection.SecondaryProvider.Name, secondaryConnectionString, null);
					using var con                 = secondaryProvider.CreateConnection(secondaryConnectionString);
					con.Open();
				}

				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}
	}

	// intercepts exceptions from driver to linqpad
	public static void HandleException(Exception ex, string method)
	{
		Notification.Error($"Unhandled error in method '{method}': {ex.Message}\r\n{ex.StackTrace}", "Linq To DB Driver Error");
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
			HandleException(ex, nameof(GetAssembliesToAdd));
			yield break;
		}

		foreach (var assembly in provider.GetAdditionalReferences(settings.Connection.Provider!))
			yield return assembly.FullName!;

		yield return cnType.Assembly.Location;
	}
}
