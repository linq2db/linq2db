using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LinqToDB.LINQPad.UI;

internal sealed class DynamicConnectionModel : ConnectionModelBase, INotifyPropertyChanged
{
	public DynamicConnectionModel(ConnectionSettings settings, bool enabled)
		: base(settings, enabled)
	{
		// the database already configured stays listed even where it cannot work: the combo binds SelectedValue
		// against the items, so dropping it would push null back and clear the connection's database and provider
		var current = Database;

		foreach (var db in DatabaseProviders.Providers.Values.Where(db => db.IsPlatformSupported || db == current).OrderBy(static db => db.Description, System.StringComparer.Ordinal))
			Databases.Add(db);

		UpdateProviders();
		UpdateProviderPathVisibility();
		UpdateSecondaryConnection();
		UpdateProviderDownloadUrl();
	}

	private void UpdateProviders()
	{
		var db = Database;

		if (db != null)
		{
			Providers.Clear();

			foreach (var provider in db.Providers.Where(p => !p.IsHidden))
				Providers.Add(provider);

			if (Providers.Count == 1)
				Settings.Connection.Provider = Providers[0].Name;
		}

		var old = ProviderVisibility;
		ProviderVisibility = Providers.Count > 1 && db?.AutomaticProviderSelection == false ? Visibility.Visible : Visibility.Collapsed;

		if (ProviderVisibility != old)
			OnPropertyChanged(_providerVisibilityChangedEventArgs);
	}

	private void UpdateProviderPathVisibility()
	{
		var db       = Database;
		var provider = Provider;

		if (db == null || provider == null || !db.IsProviderPathSupported(provider.Name))
		{
			ProviderPathVisibility = Visibility.Collapsed;
			ProviderPathLabel      = null;
			ProviderPath           = null;

			OnPropertyChanged(_providerPathVisibilityChangedEventArgs);
			return;
		}

		var assemblyNames      = db.GetProviderAssemblyNames(provider.Name);
		ProviderPathVisibility = Visibility.Visible;
		ProviderPath           = Settings.Connection.ProviderPath ?? db.TryGetDefaultPath(provider.Name);
		ProviderPathLabel      = $"Specify path to {string.JoinStrings('/', assemblyNames)}";

		OnPropertyChanged(_providerPathVisibilityChangedEventArgs);
		OnPropertyChanged(_providerPathLabelChangedEventArgs);
		OnPropertyChanged(_providerPathChangedEventArgs);
	}

	private void UpdateProviderDownloadUrl()
	{
		var db       = Database;
		var provider = Provider;

		if (db == null)
		{
			ProviderDownloadUrlVisibility = Visibility.Collapsed;
		}
		else
		{
			ProviderDownloadUrl           = db.GetProviderDownloadUrl(provider?.Name);
			ProviderDownloadUrlVisibility = ProviderDownloadUrl != null ? Visibility.Visible : Visibility.Collapsed;
		}

		OnPropertyChanged(_providerDownloadUrlChangedEventArgs);
		OnPropertyChanged(_providerDownloadUrlVisibilityChangedEventArgs);
	}

	private void UpdateSecondaryConnection()
	{
		var provider = Provider;

		if (provider?.SecondaryName != null)
		{
			SecondaryConnectionStringVisibility = Visibility.Visible;
			ConnectionStringLabel               = $"{GetProviderDisplayName(provider.Name)} connection string";
			SecondaryConnectionStringLabel      = $"{GetProviderDisplayName(provider.SecondaryName)} connection string (schema only)";
		}
		else
		{
			SecondaryConnectionStringVisibility = Visibility.Collapsed;
			ConnectionStringLabel               = "Connection string";
			SecondaryConnectionStringLabel      = null;
		}

		OnPropertyChanged(_secondaryConnectionStringVisibilityChangedEventArgs);
		OnPropertyChanged(_connectionStringLabelChangedEventArgs);
		OnPropertyChanged(_secondaryConnectionStringLabelChangedEventArgs);
	}

	private string GetProviderDisplayName(string providerName)
	{
		foreach (var provider in Database!.Providers)
			if (provider.SecondaryName == null && string.Equals(provider.Name, providerName, System.StringComparison.Ordinal))
				return provider.DisplayName;

		return providerName;
	}

	public ObservableCollection<IDatabaseProvider> Databases { get; } = new();

	private static readonly PropertyChangedEventArgs _databaseChangedEventArgs = new (nameof(Database));
	public IDatabaseProvider? Database
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.Connection.Database))
				return null;

			return DatabaseProviders.GetProvider(Settings.Connection.Database);
		}
		set
		{
			Settings.Connection.Database = value?.Database;
			UpdateProviders();
			UpdateSecondaryConnection();
			UpdateProviderDownloadUrl();
			OnPropertyChanged(_databaseChangedEventArgs);
			Provider = GetCurrentProvider();
		}
	}

	private static readonly PropertyChangedEventArgs _providerVisibilityChangedEventArgs = new (nameof(ProviderVisibility));
	public Visibility ProviderVisibility { get; set; }

	public ObservableCollection<ProviderInfo> Providers { get; } = new();

	private static readonly PropertyChangedEventArgs _providerChangedEventArgs = new (nameof(Provider));
	public ProviderInfo? Provider
	{
		get => GetCurrentProvider();
		set
		{
			Settings.Connection.Provider          = value?.Name;
			Settings.Connection.SecondaryProvider = value?.SecondaryName;

			if (value?.SecondaryName == null)
				Settings.Connection.SecondaryConnectionString = null;

			UpdateProviderPathVisibility();
			UpdateProviderDownloadUrl();
			UpdateSecondaryConnection();
			OnPropertyChanged(_providerChangedEventArgs);
			OnPropertyChanged(_secondaryConnectionStringChangedEventArgs);
		}
	}

	private ProviderInfo? GetCurrentProvider()
	{
		if (Database == null)
			return null;

		if (!string.IsNullOrWhiteSpace(Settings.Connection.Provider))
		{
			var secondary = string.IsNullOrWhiteSpace(Settings.Connection.SecondaryProvider) ? null : Settings.Connection.SecondaryProvider;

			foreach (var provider in Database.Providers)
				if (string.Equals(provider.Name, Settings.Connection.Provider, System.StringComparison.Ordinal)
					&& string.Equals(provider.SecondaryName, secondary, System.StringComparison.Ordinal))
					return provider;
		}

		foreach (var provider in Database.Providers)
			if (provider.IsDefault)
				return provider;

		return null;
	}

	private static readonly PropertyChangedEventArgs _providerPathVisibilityChangedEventArgs = new (nameof(ProviderPathVisibility));
	public Visibility ProviderPathVisibility { get; set; }

	private static readonly PropertyChangedEventArgs _providerPathChangedEventArgs = new (nameof(ProviderPath));
	public string? ProviderPath
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.Connection.ProviderPath))
				return null;

			return Settings.Connection.ProviderPath;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.Connection.ProviderPath = value;
		}
	}

	private static readonly PropertyChangedEventArgs _providerPathLabelChangedEventArgs = new (nameof(ProviderPathLabel));
	public string? ProviderPathLabel { get; set; }

	private static readonly PropertyChangedEventArgs _providerDownloadUrlVisibilityChangedEventArgs = new (nameof(ProviderDownloadUrlVisibility));
	public Visibility ProviderDownloadUrlVisibility { get; set; }

	private static readonly PropertyChangedEventArgs _providerDownloadUrlChangedEventArgs = new (nameof(ProviderDownloadUrl));
#pragma warning disable CA1056 // URI-like properties should not be strings
	public string? ProviderDownloadUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

	public string? ConnectionString
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.Connection.ConnectionString))
				return null;

			return Settings.Connection.ConnectionString;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.Connection.ConnectionString = value;

			if (Database != null && value != null && Database.AutomaticProviderSelection)
				Provider = Database.GetProviderByConnectionString(value);
		}
	}

	private static readonly PropertyChangedEventArgs _connectionStringLabelChangedEventArgs = new (nameof(ConnectionStringLabel));
	public string ConnectionStringLabel { get; set; } = "Connection string";

	private static readonly PropertyChangedEventArgs _secondaryConnectionStringVisibilityChangedEventArgs = new (nameof(SecondaryConnectionStringVisibility));
	public Visibility SecondaryConnectionStringVisibility { get; set; }

	private static readonly PropertyChangedEventArgs _secondaryConnectionStringLabelChangedEventArgs = new (nameof(SecondaryConnectionStringLabel));
	public string? SecondaryConnectionStringLabel { get; set; }

	private static readonly PropertyChangedEventArgs _secondaryConnectionStringChangedEventArgs = new (nameof(SecondaryConnectionString));
	public string? SecondaryConnectionString
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.Connection.SecondaryConnectionString))
				return null;

			return Settings.Connection.SecondaryConnectionString;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.Connection.SecondaryConnectionString = value;
		}
	}

	public bool EncryptConnectionString
	{
		get => Settings.Connection.EncryptConnectionString;
		set => Settings.Connection.EncryptConnectionString = value;
	}

	public int? CommandTimeout
	{
		get => Settings.Connection.CommandTimeout;
		set => Settings.Connection.CommandTimeout = value;
	}

	#region INotifyPropertyChanged
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
	#endregion
}
