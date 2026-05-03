using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.Internal.Common
{
	// contains reusable task instances to avoid allocations
	internal static class TaskCache
	{
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
		public static readonly Task<bool> True  = Task.FromResult(true);
		public static readonly Task<bool> False = Task.FromResult(false);

		public static readonly Task<int> Zero     = Task.FromResult(0);
		public static readonly Task<int> MinusOne = Task.FromResult(-1);

		public static readonly Task<DataConnectionTransaction?> CompletedTransaction = Task.FromResult<DataConnectionTransaction?>(null);
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
	}
}
