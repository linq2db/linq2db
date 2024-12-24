using System.Text;

namespace LinqToDB.Internals.Common
{
	internal static class Pools
	{
		public static readonly ObjectPool<StringBuilder> StringBuilder = new(() => new StringBuilder(), sb => { sb.Length = 0; }, 100);
	}
}
