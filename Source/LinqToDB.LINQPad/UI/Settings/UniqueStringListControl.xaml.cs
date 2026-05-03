using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LinqToDB.LINQPad.UI;

#pragma warning disable CA1812 // Remove unused type
internal sealed partial class UniqueStringListControl
#pragma warning restore CA1812 // Remove unused type
{
	private UniqueStringListModel Model => (UniqueStringListModel)DataContext;

	public UniqueStringListControl()
	{
		InitializeComponent();
	}

	private void GotFocus_TextBox(object sender, RoutedEventArgs e)
	{
		_button.Content = "Add";
	}

	private void Click_Button(object sender, RoutedEventArgs e)
	{
		if (_button.Content is string buttonName)
		{
			if (string.Equals(buttonName, "Add", System.StringComparison.Ordinal))
				AddNewItem();
			else
				DeleteSelectedItem();
		}
	}

	private void DeleteSelectedItem()
	{
		if (_listBox.SelectedItem is string selectedItem)
		{
			Model.Items.Remove(selectedItem);
			_button.Content = "Add";
		}
	}

	private void AddNewItem()
	{
		var item = _textBox.Text;

		if (!string.IsNullOrWhiteSpace(item))
		{
			item = item.Trim();

			if (!Model.Items.Contains(item))
				Model.Items.Add(item);
		}

		_textBox.Text = null;
	}

	private void KeyDown_TextBox(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			AddNewItem();
			e.Handled = true;
		}
	}

	private void KeyDown_ListBox(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Delete)
		{
			DeleteSelectedItem();
			e.Handled = true;
		}
	}

	private void SelectionChanged_ListBox(object sender, SelectionChangedEventArgs e)
	{
		_button.Content = _listBox.SelectedItem != null ? "Remove" : "Add";
	}
}
