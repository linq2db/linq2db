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

		public static ProjectFlags RootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.Root;
		}

		public static ProjectFlags AssociationRootFlag(this ProjectFlags flags)
		{
			return (flags & FlagsToPreserve) | ProjectFlags.AssociationRoot;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTest(this ProjectFlags flags)
		{
			return (flags & ProjectFlags.Test) != 0;
		}
	}
}
