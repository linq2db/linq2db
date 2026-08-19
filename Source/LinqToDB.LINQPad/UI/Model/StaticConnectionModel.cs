using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LinqToDB.LINQPad.UI;

internal sealed class StaticConnectionModel(ConnectionSettings settings, bool enabled) : ConnectionModelBase(settings, enabled), INotifyPropertyChanged
{
	private static readonly PropertyChangedEventArgs _contextAssemblyPathChangedEventArgs = new (nameof(ContextAssemblyPath));

	public string? ContextAssemblyPath
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.StaticContext.ContextAssemblyPath))
				return null;

			return Settings.StaticContext.ContextAssemblyPath;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;
			else
				value = value!.Trim();

			if (!string.Equals(Settings.StaticContext.ContextAssemblyPath, value, StringComparison.Ordinal))
			{
				Settings.StaticContext.ContextAssemblyPath = value;
				OnPropertyChanged(_contextAssemblyPathChangedEventArgs);
			}
		}
	}

	public string? ContextTypeName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.StaticContext.ContextTypeName))
				return null;

			return Settings.StaticContext.ContextTypeName;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.StaticContext.ContextTypeName = value;
		}
	}

	private static readonly PropertyChangedEventArgs _configurationPathChangedEventArgs = new (nameof(ConfigurationPath));
	public string? ConfigurationPath
	{
		get
		{
#if NETFRAMEWORK
			if (!string.IsNullOrWhiteSpace(Settings.StaticContext.LocalConfigurationPath))
				return Settings.StaticContext.LocalConfigurationPath;
#endif
			if (string.IsNullOrWhiteSpace(Settings.StaticContext.ConfigurationPath))
				return null;

			return Settings.StaticContext.ConfigurationPath;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;
			else
				value = value!.Trim();

#if NETFRAMEWORK
			Settings.StaticContext.ConfigurationPath = null;
			Settings.StaticContext.LocalConfigurationPath = null;

			if (value != null && value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.Equals(Settings.StaticContext.LocalConfigurationPath, value, StringComparison.Ordinal))
				{
					Settings.StaticContext.LocalConfigurationPath = value;
					OnPropertyChanged(_configurationPathChangedEventArgs);
				}
			}
			else
#endif
			if (!string.Equals(Settings.StaticContext.ConfigurationPath, value, StringComparison.Ordinal))
			{
				Settings.StaticContext.ConfigurationPath = value;
				OnPropertyChanged(_configurationPathChangedEventArgs);
			}
		}
	}

	public string? ConfigurationName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.StaticContext.ConfigurationName))
				return null;

			return Settings.StaticContext.ConfigurationName;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.StaticContext.ConfigurationName = value;
		}
	}

	/// <summary>
	/// Database the context connects to. Only limits which clients LINQPad downloads for this connection,
	/// so <see langword="null"/> is valid and means all of them.
	/// </summary>
	public IDatabaseProvider? Database
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.StaticContext.Database)
				|| !DatabaseProviders.Providers.TryGetValue(Settings.StaticContext.Database!, out var provider))
			{
				return null;
			}

			return provider;
		}
		set
		{
			if (!string.Equals(Settings.StaticContext.Database, value?.Database, StringComparison.Ordinal))
			{
				Settings.StaticContext.Database = value?.Database;
				OnPropertyChanged(_databaseChangedEventArgs);
			}
		}
	}

	private static readonly PropertyChangedEventArgs _databaseChangedEventArgs = new (nameof(Database));

	public ObservableCollection<string> ContextTypes { get; } = new();

	public ObservableCollection<string> Configurations { get; } = new();

	/// <summary>
	/// Databases offered for <see cref="Database"/>. Empty on the LINQPad 5 build, whose plugin bundles every
	/// client, so there is nothing to limit there.
	/// </summary>
	public ObservableCollection<IDatabaseProvider> Databases { get; } = CreateDatabases(settings);

	private static ObservableCollection<IDatabaseProvider> CreateDatabases(ConnectionSettings settings)
	{
		var databases = new ObservableCollection<IDatabaseProvider>();

#if !NETFRAMEWORK
		// as in DynamicConnectionModel, one already configured is kept listed even where it cannot work, so
		// that the combo doesn't push a null back over it
		var current = settings.StaticContext.Database;

		foreach (var db in DatabaseProviders.Providers.Values
			.Where(db => db.IsPlatformSupported || string.Equals(db.Database, current, StringComparison.Ordinal))
			.OrderBy(static db => db.Description, StringComparer.Ordinal))
		{
			databases.Add(db);
		}
#endif

		return databases;
	}

	/// <summary>
	/// Only the nuget driver downloads clients per connection, see <see cref="Databases"/>.
	/// </summary>
	public Visibility ClientDownloadVisibility =>
#if NETFRAMEWORK
		Visibility.Collapsed;
#else
		Visibility.Visible;
#endif

	#region INotifyPropertyChanged
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
	#endregion
}
