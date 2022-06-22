using System.Runtime.CompilerServices;
#if NET45
using LinqToDB.Common.Internal;
#endif

namespace System.Threading.Tasks
{
	static class TaskEx
	{
		public static Task CompletedTask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
#if NET45
				return TaskCache.False;
#else
				return Task.CompletedTask;
#endif
			}
		}
	}
}
