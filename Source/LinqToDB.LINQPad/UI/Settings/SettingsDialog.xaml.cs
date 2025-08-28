using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LinqToDB.LINQPad.UI;

internal sealed partial class SettingsDialog
{
	private readonly Func<SettingsModel, Exception?>? _connectionTester;
	private readonly string?                          _testErrorMessage;

	public SettingsDialog()
	{
		InitializeComponent();
	}

	private SettingsModel Model => (SettingsModel)DataContext;

	SettingsDialog(SettingsModel model, Func<SettingsModel, Exception?> connectionTester, string testErrorMessage)
		: this()
	{
		DataContext       = model;
		_connectionTester = connectionTester;
		_testErrorMessage = testErrorMessage;

		model.DynamicConnection.PropertyChanged += DynamicConnection_PropertyChanged;
	}

	private void DynamicConnection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(Model.DynamicConnection.Database):
				Model.Scaffold.UpdateClickHouseVisibility();
				break;
		}
	}

	public static bool Show(SettingsModel model, Func<SettingsModel, Exception?> connectionTester, string testErrorMessage)
	{
		return new SettingsDialog(model, connectionTester, testErrorMessage).ShowDialog() == true;
	}

	private void Click_Save(object sender, RoutedEventArgs e)
	{
		if (_connectionTester == null)
		{
			DialogResult = true;
			return;
		}

		// test configured connection and ask for confirmation on error
		Exception? ex;

		try
		{
			Mouse.OverrideCursor = Cursors.Wait;
			ex = _connectionTester(Model);
		}
		finally
		{
			Mouse.OverrideCursor = null;
		}

		if (ex == null
			|| Notification.YesNo(this, $"{_testErrorMessage ?? "Connection to database failed"} Save anyway?\r\n\r\n{ex.Message}", "Error", icon: MessageBoxImage.Stop))
		{
			DialogResult = true;
		}
	}

	void Click_Test(object sender, RoutedEventArgs e)
	{
		if (_connectionTester != null)
		{
			Exception? ex;

			try
			{
				Mouse.OverrideCursor = Cursors.Wait;
				ex = _connectionTester(Model);
			}
			finally
			{
				Mouse.OverrideCursor = null;
			}

			if (ex == null)
				Notification.Info(this, "Successful!", "Connection Test");
			else
				Notification.Error(this, ex.Message, "Connection Test Error");
		}
	}

	private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_testButton.Visibility = _tabControl.SelectedItem is TabItem ti && ti.Content is DynamicConnectionTab or StaticConnectionTab ? Visibility.Visible : Visibility.Hidden;
	}
}
