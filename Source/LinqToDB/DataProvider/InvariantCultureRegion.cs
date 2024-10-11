using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider
{
	internal sealed class InvariantCultureRegion : IExecutionScope
	{
		private readonly IExecutionScope? _parentRegion;
		private readonly CultureInfo?     _original;

		public InvariantCultureRegion(IExecutionScope? parentRegion)
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

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_original != null)
				Thread.CurrentThread.CurrentCulture = _original;

			return _parentRegion?.DisposeAsync() ?? default;
		}
	}
}
