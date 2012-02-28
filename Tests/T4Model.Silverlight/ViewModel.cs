using System;
using System.Windows.Media;

namespace T4Model.Silverlight
{
	partial class ViewModel
	{
		Brush _normalBrushes   = new SolidColorBrush(Colors.Black);
		Brush _negativeBrushes = new SolidColorBrush(Colors.Red);

		Brush GetBrush()
		{
			return NotifiedProp1 < 0 ? _negativeBrushes : _normalBrushes;
		}
	}
}
