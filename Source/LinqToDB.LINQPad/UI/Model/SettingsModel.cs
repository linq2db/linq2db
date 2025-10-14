namespace LinqToDB.LINQPad.UI;

internal sealed class SettingsModel(ConnectionSettings settings, bool staticConnection)
{
	// Don't remove. Design-time .ctor
	public SettingsModel()
		: this(new ConnectionSettings(), false)
	{
	}

	public StaticConnectionModel  StaticConnection  { get; } = new StaticConnectionModel(settings, staticConnection);
	public DynamicConnectionModel DynamicConnection { get; } = new DynamicConnectionModel(settings, !staticConnection);
	public ScaffoldModel          Scaffold          { get; } = new ScaffoldModel(settings, !staticConnection);
	public SchemaModel            Schema            { get; } = new SchemaModel(settings, !staticConnection);
	public LinqToDBModel          LinqToDB          { get; } = new LinqToDBModel(settings);
	public AboutModel             About                      => AboutModel.Instance;

	public void Save()
	{
		// save settings that is not saved automatically by tab models
		Schema.Save();
	}
}
