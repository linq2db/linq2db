using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace T4Model.Silverlight
{
	public partial class MainPage : UserControl
	{
		public MainPage()
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
