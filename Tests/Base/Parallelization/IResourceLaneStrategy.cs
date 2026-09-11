using NUnit.Framework.Interfaces;

namespace NUnit.ParallelByResource
{
	/// <summary>
	/// Maps a leaf test to a <see cref="LaneAssignment"/>, telling <see cref="ResourceLaneDispatcher"/>
	/// which shared resource (if any) the test contends for and how to run it. This is the single seam a
	/// host customizes to express its own "parallelize by resource" condition.
	/// </summary>
	/// <remarks>
	/// <see cref="Classify"/> is invoked only for leaf items that are neither globally exclusive
	/// (NUnit <c>[NonParallelizable]</c>) nor composite suites - the dispatcher handles those itself.
	/// It takes the test rather than the work item because the decision is a property of the test: that
	/// keeps a strategy independent of NUnit's execution types, and directly testable.
	/// </remarks>
	public interface IResourceLaneStrategy
	{
		/// <summary>
		/// Decide how to run <paramref name="test"/>. Returning <see langword="null"/> is treated
		/// identically to <see cref="LaneAssignment.GatedInline"/>.
		/// </summary>
		LaneAssignment? Classify(ITest test);
	}
}
