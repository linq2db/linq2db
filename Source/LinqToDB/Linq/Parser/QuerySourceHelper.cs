using System.Threading;

namespace LinqToDB.Linq.Parser
{
	public static class QuerySourceHelper
	{
		private static int _currentSourceId;

		public static int GetNexSourceId()
		{
			return Interlocked.Increment(ref _currentSourceId);
		}
	}
}
