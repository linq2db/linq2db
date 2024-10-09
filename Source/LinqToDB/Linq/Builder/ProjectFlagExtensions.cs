using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	static class ProjectFlagExtensions
	{
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Root) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAggregationRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.AggregationRoot) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsKeys(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Keys) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAssociationRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.AssociationRoot) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSql(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.SQL) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpression(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Expression) != 0;
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
			return (flags & ProjectFlags.Table) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTraverse(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Traverse) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSubquery(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Subquery) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExtractProjection(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.ExtractProjection) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsForExtension(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.ForExtension) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsForceOuter(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.ForceOuterAssociation) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpand(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Expand) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMemberRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.MemberRoot) != 0;
		}

	}
}
