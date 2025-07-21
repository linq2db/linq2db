using System;

namespace LinqToDB.Internal.Common
{
	sealed class DisposableAction(Action action) : IDisposable
	{
		public void Dispose() => action();
	}
}
