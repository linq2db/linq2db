using System;

namespace LinqToDB
{
	/// <summary>
	/// Specifies options for handling identity values during merge operations.
	/// </summary>
	public enum MergeOptions
	{
		/// <summary>
		/// Not set option. Actual value will be determined by the provider.
		/// </summary>
		NotSet            = 0,
		/// <summary>
		/// Keep the identity values from the source data.
		/// </summary>
		KeepIdentity      = 1,
		/// <summary>
		/// Do not keep the identity values from the source data.
		/// </summary>
		DoNotKeepIdentity = 2,
	}
}
