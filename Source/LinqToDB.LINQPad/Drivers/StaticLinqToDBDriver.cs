using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;

using LINQPad.Extensibility.DataContext;

using LinqToDB.Data;

namespace LinqToDB.LINQPad;

// IMPORTANT:
// 1. driver must be public or it will be missing from create connection dialog (existing connections will work)
// 2. don't rename class or namespace as it is used by LINQPad as driver identifier. If renamed, old connections will disappear from UI
/// <summary>
/// Implements LINQPad driver for static (pre-compiled) model.
/// </summary>
#pragma warning disable MA0048 // File name must match type name
public sealed class LinqToDBStaticDriver : StaticDataContextDriver
#pragma warning restore MA0048 // File name must match type name
{
	/// <inheritdoc/>
	public override string Name   => DriverHelper.Name;
	/// <inheritdoc/>
	public override string Author => DriverHelper.Author;

	static LinqToDBStaticDriver() => DriverHelper.Init();

	/// <inheritdoc/>
	public override string GetConnectionDescription(IConnectionInfo cxInfo) => DriverHelper.GetConnectionDescription(cxInfo);

	/// <inheritdoc/>
	public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions) => DriverHelper.ShowConnectionDialog(cxInfo, false);

	/// <inheritdoc/>
	public override List<ExplorerItem> GetSchema(IConnectionInfo cxInfo, Type? customType)
	{
		if (customType == null)
			return new();

		try
		{
			return StaticSchemaGenerator.GetSchema(customType);
		}
		catch (Exception ex)
		{
			Notification.Error($"{ex}\n{ex.StackTrace}", "Schema Load Error");
			throw;
		}
	}

	private static readonly ParameterDescriptor[] _contextParameters =
	[
		new ParameterDescriptor("configuration", typeof(string).FullName),
	];

	/// <inheritdoc/>
	public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
	{
		try
		{
			var settings = ConnectionSettings.Load(cxInfo);

			if (settings.StaticContext.ConfigurationName != null)
				return _contextParameters;

			return base.GetContextConstructorParameters(cxInfo);
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(GetContextConstructorParameters));
			return [];
		}
	}

	/// <inheritdoc/>
	public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
	{
		try
		{
			var settings = ConnectionSettings.Load(cxInfo);

#if NETFRAMEWORK
			var configurationPath = settings.StaticContext.LocalConfigurationPath ?? settings.StaticContext.ConfigurationPath;
#else
			var configurationPath = settings.StaticContext.ConfigurationPath;
#endif
			TryLoadAppSettingsJson(configurationPath);

			if (settings.StaticContext.ConfigurationName != null)
				return [settings.StaticContext.ConfigurationName];

			return base.GetContextConstructorArguments(cxInfo);
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(GetContextConstructorArguments));
			return [];
		}
	}

	/// <inheritdoc/>
	public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo) => DriverHelper.DefaultImports;

	/// <inheritdoc/>
	public override void ClearConnectionPools(IConnectionInfo cxInfo) => DriverHelper.ClearConnectionPools(cxInfo);

	/// <inheritdoc/>
	public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
	{
		_ = DriverHelper.InitializeContext(cxInfo, (IDataContext)context, executionManager);
	}

	/// <inheritdoc/>
	public override void TearDownContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager, object[] constructorArguments)
	{
		try
		{
			if (context is IDisposable ctx)
				ctx.Dispose();
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(TearDownContext));
		}
	}

	/// <inheritdoc/>
	public override void PreprocessObjectToWrite(ref object objectToWrite, ObjectGraphInfo info)
	{
		try
		{
			objectToWrite = ValueFormatter.Format(objectToWrite);
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(PreprocessObjectToWrite));
		}
	}

	private void TryLoadAppSettingsJson(string? appConfigPath)
	{
		if (string.IsNullOrWhiteSpace(appConfigPath) || !File.Exists(appConfigPath))
			return;

		if (appConfigPath!.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			DataConnection.DefaultSettings = AppConfig.LoadJson(appConfigPath);
#if !NETFRAMEWORK
		if (appConfigPath.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
			DataConnection.DefaultSettings = AppConfig.LoadAppConfig(appConfigPath);
#endif
	}

	/// <inheritdoc/>
	public override IDbConnection GetIDbConnection(IConnectionInfo cxInfo)
	{
		try
		{
			return DatabaseProviders.CreateConnection(ConnectionSettings.Load(cxInfo));
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(GetIDbConnection));
			throw;
		}
	}

	/// <inheritdoc/>
	public override DbProviderFactory? GetProviderFactory(IConnectionInfo cxInfo)
	{
		try
		{
			return DatabaseProviders.GetProviderFactory(ConnectionSettings.Load(cxInfo));
		}
		catch (Exception ex)
		{
			DriverHelper.HandleException(ex, nameof(GetProviderFactory));
			throw;
		}
	}
}
