using LinqToDB.Data;

namespace LinqToDB.Common.Internal
{
	// contains reusable task instances to avoid allocations
	internal static class TaskCache
	{
		public static readonly Task<bool> True  = Task.FromResult(true);
		public static readonly Task<bool> False = Task.FromResult(false);

		public static readonly Task<int> Zero     = Task.FromResult(0);
		public static readonly Task<int> MinusOne = Task.FromResult(-1);

#if !NATIVE_ASYNC
		public static readonly Task CompletedTask = True;
#else
		public static readonly Task CompletedTask = Task.CompletedTask;
#endif
		public static readonly Task<DataConnectionTransaction?> CompletedTransaction = Task.FromResult<DataConnectionTransaction?>(null);
	}
}
