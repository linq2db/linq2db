using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

				// GetExportedTypes() would give up on the whole assembly when a single type cannot be loaded, and
				// a context assembly's own dependencies often cannot be loaded here: this dialog runs in LINQPad's
				// process, which has only the driver's static dependencies, while database clients are provisioned
				// per connection. Take the types that did load and report the rest.
				Type?[] types;
				Exception? loadFailure = null;

				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types       = ex.Types;
					loadFailure = ex.LoaderExceptions.FirstOrDefault(static e => e != null) ?? ex;
				}

				foreach (var type in types)
				{
					// IsVisible is the predicate GetExportedTypes applied: public all the way up the nesting
					// chain, where IsNestedPublic is true for a public type inside an internal container
					if (type == null || !type.IsVisible)
						continue;

					try
					{
						// as referenced linq2db assembly from context could be different version than
						// linq2db assembly from current process
						// we cannot compare types directly and should use by-name comparison
						foreach (var iface in type.GetInterfaces())
						{
							if (string.Equals(iface.FullName, IDATACONTEXT_NAME, StringComparison.Ordinal))
								Model.ContextTypes.Add(type.FullName!);
						}
					}
					catch (Exception ex)
					{
						// a type whose base type or interfaces live in an assembly we cannot load
						loadFailure ??= ex;
					}
				}

				if (loadFailure != null)
					ReportContextLoadFailure(loadFailure);
			}
			catch (Exception ex)
			{
				ReportContextLoadFailure(ex);
			}
			finally
			{
				Mouse.OverrideCursor = oldCursor;
			}
		}
	}

	private void ReportContextLoadFailure(Exception ex)
	{
		// a dialog only where the user is actually stuck: with a context class in the list they can carry on,
		// so that case goes to the log, as everything else the driver recovers from does
		if (Model.ContextTypes.Count > 0)
		{
			Notification.Log(ex, "Some types of the data context assembly could not be loaded.");

			return;
		}

		// the driver no longer ships the database clients, so a context assembly that references one has to
		// bring it along - which a build output folder does, but a hand-assembled one may not
		var message = new StringBuilder(Notification.FormatMessages(ex));

		message
			.AppendLine()
			.AppendLine()
			.AppendLine("The data context assembly and everything it references must be loadable from its folder.")
			.AppendLine("Database client libraries are not shipped with the driver - LINQPad downloads them per")
			.Append("connection and they cannot be loaded here, so copy the missing assembly next to the context ")
			.Append("one, or type the context class name instead of picking it from the list.");

		Notification.Error(Window.GetWindow(this), message.ToString(), "Context assembly load error");
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
			InitialDirectory = Model.ContextAssemblyPath == null ? null : Path.GetDirectoryName(Model.ContextAssemblyPath),
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
				InitialDirectory = Model.ConfigurationPath == null ? null : Path.GetDirectoryName(Model.ConfigurationPath),
			};

			if (dialog.ShowDialog() == true)
				Model.ConfigurationPath = dialog.FileName;
		}
	}

	private void Url_Click(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
		{
			UseShellExecute = true,
		});

		e.Handled = true;
	}
}
