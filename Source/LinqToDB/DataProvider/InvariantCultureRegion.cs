using System;
using System.Globalization;
using System.Threading;

namespace LinqToDB.DataProvider.Informix
{
	internal class InvariantCultureRegion : IDisposable
	{
#if !NETSTANDARD1_6
		private readonly CultureInfo _original;
#endif

		public InvariantCultureRegion()
		{
#if !NETSTANDARD1_6
			if (!Thread.CurrentThread.CurrentCulture.Equals(CultureInfo.InvariantCulture))
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
