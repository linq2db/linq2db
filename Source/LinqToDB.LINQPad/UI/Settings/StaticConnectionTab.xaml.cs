using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

using LINQPad.Extensibility.DataContext;

using Microsoft.Win32;

namespace LinqToDB.LINQPad.UI;

#pragma warning disable CA1812 // Remove unused type
internal sealed partial class StaticConnectionTab
#pragma warning restore CA1812 // Remove unused type
{
	private const string IDATACONTEXT_NAME = $"{nameof(LinqToDB)}.{nameof(IDataContext)}";

	private StaticConnectionModel Model => (StaticConnectionModel)DataContext;

	public StaticConnectionTab()
	{
		InitializeComponent();

		DataContextChanged += StaticConnectionTab_DataContextChanged;
	}

	private void StaticConnectionTab_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		// delayed init
		LoadContextTypes();
		LoadConfigurations();
		Model.PropertyChanged += Model_PropertyChanged;
	}

	private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(Model.ContextAssemblyPath):
				LoadContextTypes();
				LoadConfigurations();
				break;
			case nameof(Model.ConfigurationPath):
				LoadConfigurations();
				break;
		}
	}

	private void LoadContextTypes()
	{
		Model.ContextTypes.Clear();

		if (Model.ContextAssemblyPath != null)
		{
			var oldCursor = Cursor;

			try
			{
				Mouse.OverrideCursor = Cursors.Wait;

				var assembly = DataContextDriver.LoadAssemblySafely(Model.ContextAssemblyPath);
				// as referenced linq2db assembly from context could be different version than
				// linq2db assembly from current process
				// we cannot compare types directly and should use by-name comparison
				foreach (var type in assembly.GetExportedTypes())
				{
					foreach (var iface in type.GetInterfaces())
					{
						if (iface.FullName == IDATACONTEXT_NAME)
							Model.ContextTypes.Add(type.FullName!);
					}
				}
			}
			catch (Exception ex)
			{
				Notification.Error(Window.GetWindow(this), ex.Message, "Context assembly load error");
			}
			finally
			{
				Mouse.OverrideCursor = oldCursor;
			}
		}
	}

	void LoadConfigurations()
	{
		Model.Configurations.Clear();

		// try to load appsettings.json
		if (Model.ConfigurationPath != null
			&& Model.ConfigurationPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
		{
			var oldCursor = Cursor;
			try
			{
				Mouse.OverrideCursor = Cursors.Wait;

				var config = AppConfig.LoadJson(Model.ConfigurationPath!);

				if (config.ConnectionStrings.Any())
					foreach (var cs in config.ConnectionStrings)
						Model.Configurations.Add(cs.Name);

				return;
			}
			catch (Exception ex)
			{
				Notification.Error(Window.GetWindow(this), ex.Message, "JSON configuration file read error");
			}
			finally
			{
				Mouse.OverrideCursor = oldCursor;
			}
		}

		// try to load custom app.config
		else if (Model.ConfigurationPath != null)
		{
			var oldCursor = Cursor;

			try
			{
				Mouse.OverrideCursor = Cursors.Wait;

				var configMap               = new ExeConfigurationFileMap();
				configMap.ExeConfigFilename = Model.ConfigurationPath;
				var config                  = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

				foreach (var cs in config.ConnectionStrings.ConnectionStrings.Cast<ConnectionStringSettings>())
					Model.Configurations.Add(cs.Name);

				Mouse.OverrideCursor = oldCursor;
			}
			catch (Exception ex)
			{
				Notification.Error(Window.GetWindow(this), ex.Message, "Custom app.config file read error");
			}
			finally
			{
				Mouse.OverrideCursor = oldCursor;
			}
		}

		// try to load default app.config
		else if (Model.ContextAssemblyPath != null)
		{
			var oldCursor = Cursor;

			try
			{
				Mouse.OverrideCursor = Cursors.Wait;

				var config = ConfigurationManager.OpenExeConfiguration(Model.ContextAssemblyPath);

				foreach (var cs in config.ConnectionStrings.ConnectionStrings.Cast<ConnectionStringSettings>())
					Model.Configurations.Add(cs.Name);

				Model.ConfigurationPath = config.FilePath;
				Mouse.OverrideCursor    = oldCursor;
			}
			catch (Exception ex)
			{
				Notification.Error(Window.GetWindow(this), ex.Message, "Default app.config file read error");
			}
			finally
			{
				Mouse.OverrideCursor = oldCursor;
			}
		}
	}

	void Click_SelectAssembly(object sender, RoutedEventArgs e)
	{
		if (Model == null)
			return;

		var dialog = new OpenFileDialog()
		{
			Title           = "Choose assembly with data model",
			DefaultExt      = ".dll",
			FileName        = Model.ContextAssemblyPath,
			CheckPathExists = true,
			Filter          = "Assembly files (*.dll, *.exe)|*.dll;*.exe|All Files(*.*)|*.*",
			InitialDirectory = Model.ContextAssemblyPath == null ? null : Path.GetDirectoryName(Model.ContextAssemblyPath)
		};

		if (dialog.ShowDialog() == true)
			Model.ContextAssemblyPath = dialog.FileName;
	}

	void Click_SelectConfig(object sender, RoutedEventArgs e)
	{
		if (Model != null)
		{
			var dialog = new OpenFileDialog()
			{
				Title           = "Choose application config file",
				DefaultExt      = ".config",
				FileName        = Model.ConfigurationPath,
				CheckPathExists = true,
				Filter          = "Configuration files (*.json, *.config)|*.json;*.config|All Files(*.*)|*.*",
				InitialDirectory = Model.ConfigurationPath == null ? null : Path.GetDirectoryName(Model.ConfigurationPath)
			};

			if (dialog.ShowDialog() == true)
				Model.ConfigurationPath = dialog.FileName;
		}
	}

	private void Url_Click(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
		{
			UseShellExecute = true
		});

		e.Handled = true;
	}
}
