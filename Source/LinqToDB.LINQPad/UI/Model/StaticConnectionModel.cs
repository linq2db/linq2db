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
	/// One entry of <see cref="Databases"/>. A <see langword="null"/> <paramref name="Database"/> is the
	/// "all of them" choice, which is why the list carries this instead of the provider itself.
	/// </summary>
	/// <param name="Description">Text shown in the combo.</param>
	/// <param name="Database">Database to limit client downloads to, or <see langword="null"/> for all.</param>
	public sealed record DatabaseChoice(string Description, IDatabaseProvider? Database);

	/// <summary>
	/// Database the context connects to, limiting which clients LINQPad downloads for this connection.
	/// </summary>
	public DatabaseChoice? SelectedDatabase
	{
		get
		{
			if (Databases.Count == 0)
				return null;

			var database = Settings.StaticContext.Database;

			foreach (var choice in Databases)
			{
				if (string.Equals(choice.Database?.Database, database, StringComparison.Ordinal))
					return choice;
			}

			// a database that is no longer registered: fall back to "all", which is what will be provisioned
			return Databases[0];
		}
		set
		{
			if (!string.Equals(Settings.StaticContext.Database, value?.Database?.Database, StringComparison.Ordinal))
			{
				Settings.StaticContext.Database = value?.Database?.Database;
				OnPropertyChanged(_selectedDatabaseChangedEventArgs);
			}
		}
	}

	private static readonly PropertyChangedEventArgs _selectedDatabaseChangedEventArgs = new (nameof(SelectedDatabase));

	public ObservableCollection<string> ContextTypes { get; } = new();

	public ObservableCollection<string> Configurations { get; } = new();

	/// <summary>
	/// Databases offered for <see cref="SelectedDatabase"/>, the first being "all of them". Empty on the
	/// LINQPad 5 build, whose plugin bundles every client, so there is nothing to limit there.
	/// </summary>
	public ObservableCollection<DatabaseChoice> Databases { get; } = CreateDatabases(settings);

	private static ObservableCollection<DatabaseChoice> CreateDatabases(ConnectionSettings settings)
	{
		var databases = new ObservableCollection<DatabaseChoice>();

#if !NETFRAMEWORK
		databases.Add(new DatabaseChoice("(all databases)", null));

		// as in DynamicConnectionModel, one already configured is kept listed even where it cannot work, so
		// that the combo doesn't silently drop it
		var current = settings.StaticContext.Database;

		foreach (var db in DatabaseProviders.Providers.Values
			.Where(db => db.IsPlatformSupported || string.Equals(db.Database, current, StringComparison.Ordinal))
			.OrderBy(static db => db.Description, StringComparer.Ordinal))
		{
			databases.Add(new DatabaseChoice(db.Description, db));
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
