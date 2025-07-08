using System;

namespace LinqToDB.Common.Internal
{
	sealed class DisposableAction(Action action) : IDisposable
	{
		public void Dispose() => action();
	}
}
