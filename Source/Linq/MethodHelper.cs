using System;
using System.Reflection;

namespace LinqToDB.Linq
{
	public static class MethodHelper
	{

#if SILVERLIGHT
		public static MethodInfo GetMethodInfo(this Delegate del)
		{
			if ((object) del == null)
				throw new ArgumentNullException("del");
			return del.Method;
		}
#endif

		#region Helper methods to obtain MethodInfo in a safe way

		public static MethodInfo GetMethodInfo<T1,T2>(Func<T1,T2> f, T1 unused1)
		{
			return f.GetMethodInfo();
		}

		public static MethodInfo GetMethodInfo<T1,T2,T3>(Func<T1,T2,T3> f, T1 unused1, T2 unused2)
		{
			return f.GetMethodInfo();
		}

		public static MethodInfo GetMethodInfo<T1,T2,T3,T4>(Func<T1,T2,T3,T4> f, T1 unused1, T2 unused2, T3 unused3)
		{
			return f.GetMethodInfo();
		}

		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5>(Func<T1,T2,T3,T4,T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
		{
			return f.GetMethodInfo();
		}

		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5,T6>(Func<T1,T2,T3,T4,T5,T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
		{
			return f.GetMethodInfo();
		}

		public static MethodInfo GetMethodInfo<T1,T2,T3,T4,T5,T6, T7>(Func<T1,T2,T3,T4,T5,T6,T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
		{
			return f.GetMethodInfo();
		}

		#endregion
	}
}