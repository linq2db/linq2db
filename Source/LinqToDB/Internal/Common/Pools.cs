using System.Text;

namespace LinqToDB.Internal.Common
{
	internal static class Pools
	{
		public static readonly ObjectPool<StringBuilder> StringBuilder = new(() => new StringBuilder(), sb => { sb.Length = 0; }, 100);
	}
}
