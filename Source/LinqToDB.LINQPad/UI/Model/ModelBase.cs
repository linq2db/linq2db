using System.ComponentModel;

namespace LinqToDB.LINQPad;

internal abstract partial class ModelBase : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);

	#region Sample Simple Property

	private string? _name;
	public string?   Name
	{
		get => _name;
		set
		{
			if (_name != value)
			{
				_name = value;
				OnPropertyChanged(_nameChangedEventArgs);
				AfterNameChanged();
			}
		}
	}

	private static readonly PropertyChangedEventArgs _nameChangedEventArgs = new (nameof(Name));

	partial void AfterNameChanged();
	#endregion
}
