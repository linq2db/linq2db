using System;
using System.Threading;
using System.Windows;

namespace Tests.T4.Wpf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		static volatile bool _stop;

		public MainWindow()
		{
			InitializeComponent();

			var data = new ViewModel();
			DataContext = data;

			Application.Current.Exit += (s,e) => { _stop = true; };

			new Thread(() =>
			{
				var r = new Random();

				while (!_stop)
				{
					data.NotifiedProp1 = (r.NextDouble() - 0.5) * 1000;
					Thread.Sleep(data.NotifiedProp2);
				}
			}).Start();
		}
	}
}
