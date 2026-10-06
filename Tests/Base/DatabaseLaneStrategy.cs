using NUnit.Framework.Interfaces;
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
		public LaneAssignment? Classify(ITest test)
		{
			var (context, isRemote) = NUnitUtils.GetContext(test);

			if (context == null)
				return LaneAssignment.GatedInline();

			if (NUnitUtils.IsCreateDatabase(test))
				return LaneAssignment.Ungated(context);

			// A test can also reach the shared LinqService host from a non-remote parameter value by
			// appending the remote suffix in its body - the classifier cannot see that from the arguments,
			// so [UsesRemoteContext] declares it.
			return LaneAssignment.Serial(context, requiresSecondaryMutex: isRemote || NUnitUtils.UsesRemoteContext(test));
		}
	}
}
