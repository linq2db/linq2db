using System;
using System.Threading;
using System.Windows;

namespace T4Model.Wpf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var data = new ViewModel();
			DataContext = data;

			new Thread(() =>
			{
				var r = new Random();

				while (true)
				{
					data.NotifiedProp1 = (r.NextDouble() - 0.5) * 1000;
					Thread.Sleep(data.NotifiedProp2);
				}
			}).Start();
		}
	}
}
