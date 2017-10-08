using System;
using System.Globalization;
using System.Threading;

namespace LinqToDB.DataProvider.Informix
{
	internal class InformixCultureFixRegion : IDisposable
	{
#if !NETSTANDARD1_6
		private readonly CultureInfo _original;
#endif

		public InformixCultureFixRegion()
		{
#if !NETSTANDARD1_6
			if (Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator != ".")
			{
				_original = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			}
#endif
		}

		void IDisposable.Dispose()
		{
#if !NETSTANDARD1_6
			if (_original != null)
				Thread.CurrentThread.CurrentCulture = _original;
#endif
		}
	}
}
