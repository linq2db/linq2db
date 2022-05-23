using System;

namespace LinqToDB.Data.RetryPolicy
{
	using Common.Internal;
	using Data;
	using Infrastructure;

	/// <param name="RetryPolicy">
	/// Retry policy for new <see cref="DataConnection"/> instance.
	/// </param>
	/// <param name="Factory">
	/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
	/// If factory method is not set, retry policy is not used.
	/// </param>
	/// <param name="MaxRetryCount">
	/// The number of retry attempts.
	/// </param>
	/// <param name="MaxDelay">
	/// The maximum time delay between retries, must be nonnegative.
	/// </param>
	/// <param name="RandomFactor">
	/// The maximum random factor, must not be lesser than 1.
	/// </param>
	/// <param name="ExponentialBase">
	/// The base for the exponential function used to compute the delay between retries, must be positive.
	/// </param>
	/// <param name="Coefficient">
	/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
	/// </param>
	public sealed record RetryPolicyOptions
	(
		IRetryPolicy?                       RetryPolicy,
		Func<DataConnection,IRetryPolicy?>? Factory         = default,
		int                                 MaxRetryCount   = default,
		TimeSpan                            MaxDelay        = default,
		double                              RandomFactor    = default,
		double                              ExponentialBase = default,
		TimeSpan                            Coefficient     = default
	) : IOptionSet, IApplicable<DataConnection>
	{
		public RetryPolicyOptions() : this((IRetryPolicy?)null)
		{
		}

		int? _configurationID;
		int IOptionSet.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(RetryPolicy?.GetType())
			.Add(Factory)
			.Add(MaxRetryCount)
			.Add(MaxDelay)
			.Add(RandomFactor)
			.Add(ExponentialBase)
			.Add(Coefficient)
			.CreateID();

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		#region IEquatable implementation

		public bool Equals(RetryPolicyOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
