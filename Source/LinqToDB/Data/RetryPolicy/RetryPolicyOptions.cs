using System;

using LinqToDB.Common;
using LinqToDB.Common.Internal;

namespace LinqToDB.Data.RetryPolicy
{
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
		IRetryPolicy?                       RetryPolicy     = default,
		Func<DataConnection,IRetryPolicy?>? Factory         = default,
		int                                 MaxRetryCount   = default,
		TimeSpan                            MaxDelay        = default,
		double                              RandomFactor    = default,
		double                              ExponentialBase = default,
		TimeSpan                            Coefficient     = default
		// If you add another parameter here, don't forget to update
		// RetryPolicyOptions copy constructor and IConfigurationID.ConfigurationID.
	)
		: IOptionSet, IApplicable<DataConnection>, IReapplicable<DataConnection>
	{
		public RetryPolicyOptions() : this((IRetryPolicy?)null)
		{
		}

		RetryPolicyOptions(RetryPolicyOptions original)
		{
			RetryPolicy     = original.RetryPolicy;
			Factory         = original.Factory;
			MaxRetryCount   = original.MaxRetryCount;
			MaxDelay        = original.MaxDelay;
			RandomFactor    = original.RandomFactor;
			ExponentialBase = original.ExponentialBase;
			Coefficient     = original.Coefficient;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(RetryPolicy?.GetType())
						.Add(Factory)
						.Add(MaxRetryCount)
						.Add(MaxDelay)
						.Add(RandomFactor)
						.Add(ExponentialBase)
						.Add(Coefficient)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		IOptionSet IOptionSet.Default => Common.Configuration.RetryPolicy.Options;

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		Action? IReapplicable<DataConnection>.Apply(DataConnection obj, object? previousObject)
		{
			return ((IConfigurationID)this).ConfigurationID == ((IConfigurationID?)previousObject)?.ConfigurationID
				? null
				: DataConnection.ConfigurationApplier.Reapply(obj, this, (RetryPolicyOptions?)previousObject);
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
