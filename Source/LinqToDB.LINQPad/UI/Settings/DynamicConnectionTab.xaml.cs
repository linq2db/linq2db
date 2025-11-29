using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

using Microsoft.Win32;

namespace LinqToDB.LINQPad.UI;

#pragma warning disable CA1812 // Remove unused type
internal sealed partial class DynamicConnectionTab
#pragma warning restore CA1812 // Remove unused type
{
	private DynamicConnectionModel Model => (DynamicConnectionModel)DataContext;

	public DynamicConnectionTab()
	{
		InitializeComponent();
	}

	private void Url_Click(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
		{
			UseShellExecute = true
		});

		e.Handled = true;
	}

	void Click_SelectProvider(object sender, RoutedEventArgs e)
	{
		if (Model == null)
			return;

		var provider = Model.Database;

		if (provider == null || Model.Provider == null || !provider.IsProviderPathSupported(Model.Provider.Name))
			return;

		var assemblyNames = provider.GetProviderAssemblyNames(Model.Provider.Name);
		var defaultPath   = provider.TryGetDefaultPath(Model.Provider.Name);
		var startPath     = Model.ProviderPath ?? defaultPath;

		var dialog = new OpenFileDialog()
		{
			Title            = $"Choose {string.Join("/", assemblyNames)} provider assembly",
			DefaultExt       = ".dll",
			FileName         = Model.ProviderPath,
			CheckPathExists  = true,
			Filter           = $"Provider File(s)|{string.Join(";", assemblyNames)}|All Files(*.*)|*.*",
			InitialDirectory = startPath == null ? null : Path.GetDirectoryName(startPath)
		};

		if (dialog.ShowDialog() == true)
			Model.ProviderPath = dialog.FileName;
	}
}
