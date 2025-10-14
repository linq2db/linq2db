using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

			if (Settings.StaticContext.ContextAssemblyPath != value)
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
			Settings.StaticContext.ConfigurationPath      = null;
			Settings.StaticContext.LocalConfigurationPath = null;

			if (value != null && value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			{
				if (Settings.StaticContext.LocalConfigurationPath != value)
				{
					Settings.StaticContext.LocalConfigurationPath = value;
					OnPropertyChanged(_configurationPathChangedEventArgs);
				}
			}
			else
#endif
			if (Settings.StaticContext.ConfigurationPath != value)
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

	public ObservableCollection<string> ContextTypes   { get; } = new();

	public ObservableCollection<string> Configurations { get; } = new();

	#region INotifyPropertyChanged
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(PropertyChangedEventArgs args) => PropertyChanged?.Invoke(this, args);
	#endregion
}
