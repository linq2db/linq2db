using System;

public interface IExecutionScope : IDisposable
#if NATIVE_ASYNC
	, IAsyncDisposable
#endif
{
}
