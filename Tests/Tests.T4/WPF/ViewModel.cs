#if NETFRAMEWORK
using System.Windows.Media;
#endif

namespace Tests.T4.Wpf
{
	/// <summary />
	partial class ViewModel
	{
#if NETFRAMEWORK
		static readonly Brush _normalBrushes   = new SolidColorBrush(Colors.Black);
		static readonly Brush _negativeBrushes = new SolidColorBrush(Colors.Red);

		Brush GetBrush()
		{
			return NotifiedProp1 < 0 ? _negativeBrushes : _normalBrushes;
		}
#endif
	}
}
