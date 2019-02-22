using System;
using System.Windows.Media;

namespace Tests.T4.Wpf
{
	partial class ViewModel
	{
		static readonly Brush _normalBrushes   = new SolidColorBrush(Colors.Black);
		static readonly Brush _negativeBrushes = new SolidColorBrush(Colors.Red);

		Brush GetBrush()
		{
			return NotifiedProp1 < 0 ? _negativeBrushes : _normalBrushes;
		}
	}
}
