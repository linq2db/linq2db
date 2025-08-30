using System.Diagnostics;

namespace LinqToDB.Internal.Infrastructure
{
	[DebuggerDisplay("Generator({_current})")]
	sealed class UniqueIdGenerator<T> : IUniqueIdGenerator<T>
	{
		int _current;

		public int GetNext()
		{
			return _current++;
		}
	}
}
