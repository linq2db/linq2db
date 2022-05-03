using System;

namespace LinqToDB.Infrastructure
{
	using Data;
	using Data.RetryPolicy;

	public class RetryPolicyOptionsExtension : IDbContextOptionsExtension
	{
		private Func<DataConnection,IRetryPolicy?>? _factory;
		private int                                 _maxRetryCount;
		private TimeSpan                            _maxDelay;
		private double                              _randomFactor;
		private double                              _exponentialBase;
		private TimeSpan                            _coefficient;

		public RetryPolicyOptionsExtension()
		{

		}

		public RetryPolicyOptionsExtension(RetryPolicyOptionsExtension copyFrom)
		{
			_factory         = copyFrom._factory;
			_maxRetryCount   = copyFrom._maxRetryCount;
			_maxDelay        = copyFrom._maxDelay;
			_randomFactor    = copyFrom._randomFactor;
			_exponentialBase = copyFrom._exponentialBase;
			_coefficient     = copyFrom._coefficient;
		}

		/// <summary>
		/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
		/// If factory method is not set, retry policy is not used.
		/// Not set by default.
		/// </summary>
		public Func<DataConnection, IRetryPolicy?>? Factory => _factory;

		/// <summary>
		/// The number of retry attempts.
		/// Default value: <c>5</c>.
		/// </summary>
		public int MaxRetryCount => _maxRetryCount;

		/// <summary>
		/// The maximum time delay between retries, must be nonnegative.
		/// Default value: 30 seconds.
		/// </summary>
		public TimeSpan MaxDelay => _maxDelay;

		/// <summary>
		/// The maximum random factor, must not be lesser than 1.
		/// Default value: <c>1.1</c>.
		/// </summary>
		public double RandomFactor => _randomFactor;

		/// <summary>
		/// The base for the exponential function used to compute the delay between retries, must be positive.
		/// Default value: <c>2</c>.
		/// </summary>
		public double ExponentialBase => _exponentialBase;

		/// <summary>
		/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
		/// Default value: 1 second.
		/// </summary>
		public TimeSpan Coefficient => _coefficient;

		#region With Methods

		public RetryPolicyOptionsExtension WithFactory(Func<DataConnection, IRetryPolicy?>? factory) =>
			SetValue(o => o._factory = factory);

		public RetryPolicyOptionsExtension WithMaxRetryCount(int defaultMaxRetryCount) =>
			SetValue(o => o._maxRetryCount = defaultMaxRetryCount);

		public RetryPolicyOptionsExtension WithMaxDelay(TimeSpan defaultMaxDelay) =>
			SetValue(o => o._maxDelay = defaultMaxDelay);

		public RetryPolicyOptionsExtension WithRandomFactor(double defaultRandomFactor) =>
			SetValue(o => o._randomFactor = defaultRandomFactor);

		public RetryPolicyOptionsExtension WithExponentialBase(double defaultExponentialBase) =>
			SetValue(o => o._exponentialBase = defaultExponentialBase);

		public RetryPolicyOptionsExtension WithCoefficient(TimeSpan defaultCoefficient) =>
			SetValue(o => o._coefficient = defaultCoefficient);

		#endregion

		#region Helper Methods

		protected virtual RetryPolicyOptionsExtension Clone()
		{
			return new RetryPolicyOptionsExtension(this);
		}

		private RetryPolicyOptionsExtension SetValue(Action<RetryPolicyOptionsExtension> setter)
		{
			var clone = Clone();
			setter(clone);

			return clone;
		}

		#endregion

		public DbContextOptionsExtensionInfo Info => throw new NotImplementedException();

		public void ApplyServices()
		{
			throw new NotImplementedException();
		}

		public void Validate(IDataContextOptions options)
		{
			throw new NotImplementedException();
		}
	}
	
}
