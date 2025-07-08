using System;

namespace LinqToDB.Common.Internal
{
	class DisposableAction(Action action) : IDisposable
	{
		public void Dispose() => action();
	}
}
