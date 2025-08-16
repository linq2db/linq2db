namespace LinqToDB.LINQPad.UI;

internal sealed class LinqToDBModel(ConnectionSettings settings) : TabModelBase(settings)
{
	public bool OptimizeJoins
	{
		get => Settings.LinqToDB.OptimizeJoins;
		set => Settings.LinqToDB.OptimizeJoins = value;
	}
}
