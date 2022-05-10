#if !NATIVE_ASYNC
using JetBrains.Annotations;

namespace LinqToDB.Async;

/// <summary>
/// Provides a mechanism for releasing unmanaged resources asynchronously.
/// </summary>
[PublicAPI]
public interface IAsyncDisposable
{
	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or
	/// resetting unmanaged resources asynchronously.
	/// </summary>
	Task DisposeAsync();
}
#endif
