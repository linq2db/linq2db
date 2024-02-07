using System;
using System.Threading;

namespace LinqToDB.Common.Internal
{
	using Linq;

	internal sealed class ConcurrencyDetector
	{
		private const string ConcurrentExecutionMessage = "A second operation was started on this context instance before a previous operation completed. This is usually caused by different threads concurrently using the same instance of DbContext.";

		private int _inCriticalSection;
		private int _currentContextRefCount;

		public IDisposable EnterCriticalSection()
		{
			if (Interlocked.CompareExchange(ref _inCriticalSection, 1, 0) == 1)
			{
				throw new LinqException(ConcurrentExecutionMessage);
			}

			_currentContextRefCount++;
			return new CurrencyDetectorDisposer(this);
		}

		/// <summary>
		///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
		///     the same compatibility standards as public APIs. It may be changed or removed without notice in
		///     any release. You should only use it directly in your code with extreme caution and knowing that
		///     doing so can result in application failures when updating to a new Entity Framework Core release.
		/// </summary>
		private void ExitCriticalSection()
		{
			if (--_currentContextRefCount == 0)
			{
				_inCriticalSection = 0;
			}
		}

		private class CurrencyDetectorDisposer(ConcurrencyDetector detector) : IDisposable
		{
			public void Dispose() => detector.ExitCriticalSection();
		}
	}
}
