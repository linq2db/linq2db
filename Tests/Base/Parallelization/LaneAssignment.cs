namespace NUnit.ParallelByResource
{
	/// <summary>
	/// The routing decision an <see cref="IResourceLaneStrategy"/> returns for a leaf test work item.
	/// </summary>
	public readonly struct LaneAssignment
	{
		LaneAssignment(LaneDisposition disposition, string? resourceKey, bool requiresSecondaryMutex)
		{
			Disposition            = disposition;
			ResourceKey            = resourceKey;
			RequiresSecondaryMutex = requiresSecondaryMutex;
		}

		/// <summary>How the item should be run.</summary>
		public LaneDisposition Disposition { get; }

		/// <summary>
		/// The shared-resource key. Required for both <see cref="LaneDisposition.SerialLane"/> and
		/// <see cref="LaneDisposition.Ungated"/> - each selects a lane by it, and the two keyspaces are
		/// separate so a resource's preparation never queues behind the items waiting on it.
		/// <see langword="null"/> otherwise.
		/// </summary>
		public string? ResourceKey { get; }

		/// <summary>
		/// <see cref="LaneDisposition.SerialLane"/> only: also acquire the single process-wide
		/// secondary mutex while running, so at most one such item runs across all lanes at a time
		/// (e.g. items that share one in-process server).
		/// </summary>
		public bool RequiresSecondaryMutex { get; }

		/// <summary>Run inline on the calling thread under the read gate (no resource binding).</summary>
		public static LaneAssignment GatedInline() => new(LaneDisposition.GatedInline, null, false);

		/// <summary>Run on the serial lane for <paramref name="resourceKey"/>.</summary>
		public static LaneAssignment Serial(string resourceKey, bool requiresSecondaryMutex = false)
			=> new(LaneDisposition.SerialLane, resourceKey, requiresSecondaryMutex);

		/// <summary>
		/// Run on the ungated lane for <paramref name="resourceKey"/> (resource preparation) - its own
		/// thread, taking neither the gate nor a throttle permit.
		/// </summary>
		public static LaneAssignment Ungated(string resourceKey)
			=> new(LaneDisposition.Ungated, resourceKey, false);
	}
}
