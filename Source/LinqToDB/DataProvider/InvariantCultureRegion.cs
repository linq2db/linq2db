using System;
using System.Globalization;
using System.Threading;

namespace LinqToDB
{
	internal class InvariantCultureRegion : IDisposable
	{
		private readonly IDisposable? _parentRegion;
		private readonly CultureInfo? _original;

		public InvariantCultureRegion(IDisposable? parentRegion)
		{
			_parentRegion = parentRegion;

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

			_parentRegion?.Dispose();
		}
	}
}
