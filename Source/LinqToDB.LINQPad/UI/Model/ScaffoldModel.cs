using System.ComponentModel;
using System.Windows;

namespace LinqToDB.LINQPad.UI;

internal sealed class ScaffoldModel : OptionalTabModelBase, INotifyPropertyChanged
{
	public ScaffoldModel(ConnectionSettings settings, bool enabled)
		: base(settings, enabled)
	{
		UpdateClickHouseVisibility();
	}

	public bool Capitalize
	{
		get => Settings.Scaffold.Capitalize;
		set => Settings.Scaffold.Capitalize = value;
	}

	public bool Pluralize
	{
		get => Settings.Scaffold.Pluralize;
		set => Settings.Scaffold.Pluralize = value;
	}

	public bool AsIsNames
	{
		get => Settings.Scaffold.AsIsNames;
		set => Settings.Scaffold.AsIsNames = value;
	}

	public bool UseProviderTypes
	{
		get => Settings.Scaffold.UseProviderTypes;
		set => Settings.Scaffold.UseProviderTypes = value;
	}

	public bool ClickHouseUseStrings
	{
		get => Settings.Scaffold.ClickHouseFixedStringAsString;
		set => Settings.Scaffold.ClickHouseFixedStringAsString = value;
	}

	private static readonly PropertyChangedEventArgs _clickHouseVisibilityChangedEventArgs = new (nameof(ClickHouseVisibility));
	public Visibility ClickHouseVisibility { get; set; }

	internal void UpdateClickHouseVisibility()
	{
		ClickHouseVisibility = string.Equals(Settings.Connection.Database, ProviderName.ClickHouse, System.StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
		OnPropertyChanged(_clickHouseVisibilityChangedEventArgs);
	}

	#region INotifyPropertyChanged
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
	#endregion
}
