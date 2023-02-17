using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	static class ProjectFlagExtensions
	{
		const ProjectFlags FlagsToPreserve = ProjectFlags.Test | ProjectFlags.ForceOuterAssociation;

		public static ProjectFlags CleanFlagsForCache(this ProjectFlags flags)
		{
			return flags & FlagsToPreserve;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]

		public static ProjectFlags RootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Root;
		}

		public static ProjectFlags ExpressionFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Expression;
		}

		public static ProjectFlags ExpandFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Expand;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags SqlFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.SQL;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags TestFlag(this ProjectFlags flags)
		{
			return flags | ProjectFlags.Test;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ProjectFlags AssociationRootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.AssociationRoot;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTest(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Test) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Root) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAssociationRoot(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.AssociationRoot) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSql(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.SQL) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpression(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Expression) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTable(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Table) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsExpand(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Expand) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTraverse(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Traverse) != 0;
		}

	}
}
