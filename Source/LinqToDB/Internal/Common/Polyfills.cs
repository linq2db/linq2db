// Useful methods that are available in current .net releases

using System.Collections.Generic;
using System.Linq;

namespace System.Linq
{
#if NET462
	internal static class EnumerablePolyfills
	{		
		public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T element)
			=> source.Concat([element]);
	}
#endif
}
