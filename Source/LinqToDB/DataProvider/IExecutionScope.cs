using System;

public interface IExecutionScope : IDisposable
#if NATIVE_ASYNC
	, IAsyncDisposable
#else
	, LinqToDB.Async.IAsyncDisposable
#endif
{
}
