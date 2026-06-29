using NUnit.Framework.Internal.Execution;

namespace NUnit.ParallelByResource
{
	/// <summary>
	/// Maps a leaf test work item to a <see cref="LaneAssignment"/>, telling
	/// <see cref="ResourceLaneDispatcher"/> which shared resource (if any) the item contends for and
	/// how to run it. This is the single seam a host customizes to express its own
	/// "parallelize by resource" condition.
	/// </summary>
	/// <remarks>
	/// <see cref="Classify"/> is invoked only for leaf items that are neither globally exclusive
	/// (NUnit <c>[NonParallelizable]</c>) nor composite suites - the dispatcher handles those itself.
	/// </remarks>
	public interface IResourceLaneStrategy
	{
		/// <summary>
		/// Decide how to run <paramref name="work"/>. Returning <see langword="null"/> is treated
		/// identically to <see cref="LaneAssignment.GatedInline"/>.
		/// </summary>
		LaneAssignment? Classify(WorkItem work);
	}
}
