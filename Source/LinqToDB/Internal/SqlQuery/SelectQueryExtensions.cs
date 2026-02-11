using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.SqlQuery
{
	public static class SelectQueryExtensions
	{
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasWhere(this SelectQuery selectQuery)
		{
			return !selectQuery.Where.SearchCondition.IsTrue();
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasGroupBy(this SelectQuery selectQuery)
		{
			return !selectQuery.GroupBy.IsEmpty;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasHaving(this SelectQuery selectQuery)
		{
			return !selectQuery.Having.SearchCondition.IsTrue();
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasOrderBy(this SelectQuery selectQuery)
		{
			return !selectQuery.OrderBy.IsEmpty;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsDistinct(this SelectQuery selectQuery)
		{
			return selectQuery.Select.IsDistinct;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLimited(this SelectQuery selectQuery)
		{
			return selectQuery.Select.TakeValue != null || selectQuery.Select.SkipValue != null;
		}
	}
}
