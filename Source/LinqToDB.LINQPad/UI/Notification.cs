using System.Windows;

namespace LinqToDB.LINQPad;

internal static class Notification
{
	public static void Error(string message, string title = "Error")
	{
		MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public static void Error(Window owner, string message, string title = "Error")
	{
		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public static void Warning(Window owner, string message, string title = "Warning")
	{
		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
	}

	public static void Info(Window owner, string message, string title = "Information")
	{
		MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
	}

	public static bool YesNo(Window owner, string message, string title = "Information", MessageBoxImage icon = MessageBoxImage.Question)
	{
		return MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes;
	}
}
