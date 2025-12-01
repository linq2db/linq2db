using System.ComponentModel;
using System.Windows;

namespace LinqToDB.LINQPad.UI;

internal sealed class TroubleshootModel : TabModelBase, INotifyPropertyChanged
{
	private readonly DynamicConnectionModel _connection;

	private static readonly PropertyChangedEventArgs _visibilityChangedEventArgs = new (nameof(Visibility));
	private static readonly PropertyChangedEventArgs _helpTextEventArgs = new (nameof(HelpText));

	public TroubleshootModel(DynamicConnectionModel connection, ConnectionSettings settings) : base(settings)
	{
		_connection = connection;
		UpdateVisibility();
	}

	public Visibility Visibility { get; set; }

	public string?    HelpText   { get; set; }

	internal void UpdateVisibility()
	{
		Visibility = _connection.Provider?.Troubleshoot != null ? Visibility.Visible : Visibility.Collapsed;
		HelpText   = _connection.Provider?.Troubleshoot;

		OnPropertyChanged(_visibilityChangedEventArgs);
		OnPropertyChanged(_helpTextEventArgs);
	}

	#region INotifyPropertyChanged
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
	#endregion
}
