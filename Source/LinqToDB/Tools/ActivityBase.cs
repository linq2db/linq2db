using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Tools
{
	/// <summary>
	/// Provides a basic implementation of the <see cref="IActivity"/> interface.
	/// You do not have to use this class.
	/// However, it can help you to avoid incompatibility issues in the future if the <see cref="IActivity"/> interface is extended.
	/// </summary>
	[PublicAPI]
	public abstract class ActivityBase : IActivity
	{
		public abstract void Dispose();

#if NATIVE_ASYNC
		public virtual ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}
#else
		public virtual Task DisposeAsync()
		{
			Dispose();
			return Task.FromResult(false);
		}
#endif
	}
}
