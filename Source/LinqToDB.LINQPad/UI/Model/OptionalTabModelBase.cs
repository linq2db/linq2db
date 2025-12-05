using System.Windows;

namespace LinqToDB.LINQPad.UI;

internal abstract class OptionalTabModelBase(ConnectionSettings settings, bool enabled) : TabModelBase(settings)
{
	public Visibility Visibility { get; } = enabled ? Visibility.Visible : Visibility.Collapsed;
}
