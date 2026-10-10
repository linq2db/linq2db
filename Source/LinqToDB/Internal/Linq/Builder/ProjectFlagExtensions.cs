using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.Linq.Builder
{
	static class ProjectFlagExtensions
	{
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRoot(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Root);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAggregationRoot(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.AggregationRoot);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsKeys(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Keys);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAssociationRoot(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.AssociationRoot);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSql(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.SQL);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpression(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Expression);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSqlOrExpression(this ProjectFlags flags)
		{
			return (flags & (ProjectFlags.SQL | ProjectFlags.Expression)) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTable(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Table);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTraverse(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Traverse);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSubquery(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Subquery);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExtractProjection(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.ExtractProjection);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpand(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.Expand);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMemberRoot(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.MemberRoot);
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsForSetProjection(this ProjectFlags flags)
		{
			return flags.HasFlag(ProjectFlags.ForSetProjection);
		}

	}
}
