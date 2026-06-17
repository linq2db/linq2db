using NUnit.Framework.Internal.Execution;
using NUnit.ParallelByResource;

namespace Tests
{
	// linq2db's resource-lane strategy: the shared resource is the physical database, keyed by
	// provider context. A provider's direct and remote (LinqService) variants map to the same lane
	// (NUnitUtils.GetContext strips the remote suffix) so they never overlap; remote variants also
	// take the secondary mutex so only one LinqService test (sharing one in-process server) runs at
	// a time. CreateDatabase runs ungated so a provider's other tests can wait on its readiness latch
	// without it being serialized behind, or blocking, the provider lane.
	public sealed class DatabaseLaneStrategy : IResourceLaneStrategy
	{
		public LaneAssignment? Classify(WorkItem work)
		{
			var (context, isRemote) = NUnitUtils.GetContext(work.Test);

			if (context == null)
				return LaneAssignment.GatedInline();

			if (NUnitUtils.IsCreateDatabase(work.Test))
				return LaneAssignment.Ungated(context);

			return LaneAssignment.Serial(context, requiresSecondaryMutex: isRemote);
		}
	}
}
