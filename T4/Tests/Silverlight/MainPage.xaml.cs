using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace T4Model.Silverlight
{
	public partial class MainPage : UserControl
	{
		static volatile bool _stop;

		public MainPage()
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
