using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public class ReferenceComparer<T> : IEqualityComparer<T>
		where T : class
	{
		public static readonly IEqualityComparer<T> Instance = new ReferenceComparer<T>();
		bool IEqualityComparer<T>.Equals(T x, T y)
		{
			return ReferenceEquals(x, y);
		}

		int IEqualityComparer<T>.GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}
}
