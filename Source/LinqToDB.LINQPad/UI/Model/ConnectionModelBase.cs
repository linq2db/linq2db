namespace LinqToDB.LINQPad.UI;

internal abstract class ConnectionModelBase(ConnectionSettings settings, bool enabled) : OptionalTabModelBase(settings, enabled)
{
	public string? Name
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Settings.Connection.DisplayName))
				return null;

			return Settings.Connection.DisplayName;
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
				value = null;

			Settings.Connection.DisplayName = value;
		}
	}

	public bool IsSelected { get; set; } = enabled;

	public bool Persistent
	{
		get => Settings.Connection.Persistent;
		set => Settings.Connection.Persistent = value;
	}

	public bool Production
	{
		get => Settings.Connection.IsProduction;
		set => Settings.Connection.IsProduction = value;
	}
}
