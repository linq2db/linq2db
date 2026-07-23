namespace NUnit.ParallelByResource
{
	/// <summary>
	/// How <see cref="ResourceLaneDispatcher"/> should run a leaf test work item, as decided by an
	/// <see cref="IResourceLaneStrategy"/>.
	/// </summary>
	public enum LaneDisposition
	{
		/// <summary>
		/// Run inline on the calling thread under the shared read gate, so a globally-exclusive
		/// item (NUnit <c>[NonParallelizable]</c>) still excludes it. Used for items not bound to
		/// any shared resource.
		/// </summary>
		GatedInline,

		/// <summary>
		/// Run on a dedicated serial lane keyed by <see cref="LaneAssignment.ResourceKey"/>: items
		/// sharing a key never overlap, while different keys run concurrently.
		/// </summary>
		SerialLane,

		/// <summary>
		/// Run immediately, outside the gate and off any lane. Used for items that prepare a shared
		/// resource (e.g. create/seed it) and so must not be serialized behind, or block, the lane
		/// whose other items wait on that resource's readiness.
		/// </summary>
		Ungated,
	}
}
