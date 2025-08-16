using System.Windows;
using System.Windows.Controls;

namespace LinqToDB.LINQPad.UI;

#pragma warning disable CA1812 // Remove unused type
internal sealed partial class SchemaTab
#pragma warning restore CA1812 // Remove unused type
{
	public SchemaTab()
	{
		InitializeComponent();
	}

	private void ProcLoad_Click(object sender, RoutedEventArgs e)
	{
		if (((CheckBox)sender).IsChecked == true)
		{
			Notification.Warning(
				Window.GetWindow(this),
				"Including Stored Procedures or Table Functions may be dangerous if they contain non-transactional logic because driver needs to execute them for returned table schema population.");
		}
	}
}
