using System;

namespace LinqToDB.Infrastructure
{
	using Data;
	using Data.RetryPolicy;

	public class RetryPolicyOptionsExtension : IDbContextOptionsExtension
	{
		private IRetryPolicy?                       _retryPolicy;
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
			_retryPolicy     = copyFrom._retryPolicy;
			_factory         = copyFrom._factory;
			_maxRetryCount   = copyFrom._maxRetryCount;
			_maxDelay        = copyFrom._maxDelay;
			_randomFactor    = copyFrom._randomFactor;
			_exponentialBase = copyFrom._exponentialBase;
			_coefficient     = copyFrom._coefficient;
		}

		/// <summary>
		/// Retry policy for new <see cref="DataConnection"/> instance.
		/// </summary>
		public IRetryPolicy? RetryPolicy => _retryPolicy;

		/// <summary>
		/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
		/// If factory method is not set, retry policy is not used.
		/// </summary>
		public Func<DataConnection, IRetryPolicy?>? Factory => _factory;

		/// <summary>
		/// The number of retry attempts.
		/// </summary>
		public int MaxRetryCount => _maxRetryCount;

		/// <summary>
		/// The maximum time delay between retries, must be nonnegative.
		/// </summary>
		public TimeSpan MaxDelay => _maxDelay;

		/// <summary>
		/// The maximum random factor, must not be lesser than 1.
		/// </summary>
		public double RandomFactor => _randomFactor;

		/// <summary>
		/// The base for the exponential function used to compute the delay between retries, must be positive.
		/// </summary>
		public double ExponentialBase => _exponentialBase;

		/// <summary>
		/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
		/// </summary>
		public TimeSpan Coefficient => _coefficient;

		#region With Methods

		public RetryPolicyOptionsExtension WithRetryPolicy(IRetryPolicy retryPolicy) =>
			SetValue(o => o._retryPolicy = retryPolicy);
		
		public RetryPolicyOptionsExtension WithFactory(Func<DataConnection, IRetryPolicy?>? factory) =>
			SetValue(o => o._factory = factory);

		public RetryPolicyOptionsExtension WithMaxRetryCount(int maxRetryCount) =>
			SetValue(o => o._maxRetryCount = maxRetryCount);

		public RetryPolicyOptionsExtension WithMaxDelay(TimeSpan maxDelay) =>
			SetValue(o => o._maxDelay = maxDelay);

		public RetryPolicyOptionsExtension WithRandomFactor(double randomFactor) =>
			SetValue(o => o._randomFactor = randomFactor);

		public RetryPolicyOptionsExtension WithExponentialBase(double exponentialBase) =>
			SetValue(o => o._exponentialBase = exponentialBase);

		public RetryPolicyOptionsExtension WithCoefficient(TimeSpan coefficient) =>
			SetValue(o => o._coefficient = coefficient);

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
