using System;
using System.Globalization;
using System.Threading;

namespace LinqToDB
{
	internal class InvariantCultureRegion : IDisposable
	{
		private readonly CultureInfo _original;

		public InvariantCultureRegion()
		{
			if (!Thread.CurrentThread.CurrentCulture.Equals(CultureInfo.InvariantCulture))
			{
				_original = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			}
		}

		void IDisposable.Dispose()
		{
			if (_original != null)
				Thread.CurrentThread.CurrentCulture = _original;
		}
	}
}
