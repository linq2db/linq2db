using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	static class ProjectFlagExtensions
	{
		const ProjectFlags FlagsToPreserve = ProjectFlags.Test | ProjectFlags.ForceOuterAssociation;

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags RootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Root;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags ExpressionFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Expression;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags ExpandFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Expand;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags SqlFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.SQL;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags TestFlag(this ProjectFlags flags)
		{
			return flags | ProjectFlags.Test;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags AssociationRootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.AssociationRoot;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTest(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Test) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Root) != 0;
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
		public static bool IsTable(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Table) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpand(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Expand) != 0;
		}

		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTraverse(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Traverse) != 0;
		}

	}
}
