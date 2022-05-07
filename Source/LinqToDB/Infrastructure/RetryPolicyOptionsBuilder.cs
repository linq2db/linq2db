using System;

namespace LinqToDB.Infrastructure
{
	using Data;
	using Data.RetryPolicy;

	/// <summary>
	/// <para>
	/// Allows specific Retry Policy configuration to be performed on <see cref="DataContextOptions" />.
	/// </para>
	/// </summary>
	public class RetryPolicyOptionsBuilder
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerDataContextOptionsBuilder" /> class.
		/// </summary>
		/// <param name="optionsBuilder"> The options builder. </param>
		public RetryPolicyOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
		{
			OptionsBuilder = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
		}

		/// <summary>
		///     Gets the core options builder.
		/// </summary>
		protected virtual DataContextOptionsBuilder OptionsBuilder { get; }

		/// <summary>
		/// Uses retry policy
		/// </summary>
		public RetryPolicyOptionsBuilder WithRetryPolicy(IRetryPolicy retryPolicy)
		{
			return WithOption(o => o.WithRetryPolicy(retryPolicy));
		}

		/// <summary>
		/// Uses default retry policy factory
		/// </summary>
		public RetryPolicyOptionsBuilder UseDefaultRetryPolicyFactory()
		{
			return WithOption(o => o.WithFactory(DefaultRetryPolicyFactory.GetRetryPolicy));
		}

		/// <summary>
		/// Retry policy factory method, used to create retry policy for new <see cref="DataConnection"/> instance.
		/// If factory method is not set, retry policy is not used.
		/// Not set by default.
		/// </summary>
		public RetryPolicyOptionsBuilder WithFactory(Func<DataConnection, IRetryPolicy?>? factory)
		{
			return WithOption(o => o.WithFactory(factory));
		}

		/// <summary>
		/// The number of retry attempts.
		/// Default value: <c>5</c>.
		/// </summary>
		public RetryPolicyOptionsBuilder WithMaxRetryCount(int maxRetryCount)
		{
			return WithOption(o => o.WithMaxRetryCount(maxRetryCount));
		}

		/// <summary>
		/// The maximum time delay between retries, must be nonnegative.
		/// Default value: 30 seconds.
		/// </summary>
		public RetryPolicyOptionsBuilder WithMaxDelay(TimeSpan defaultMaxDelay)
		{
			return WithOption(o => o.WithMaxDelay(defaultMaxDelay));
		}

		/// <summary>
		/// The maximum random factor, must not be lesser than 1.
		/// Default value: <c>1.1</c>.
		/// </summary>
		public RetryPolicyOptionsBuilder WithRandomFactor(double randomFactor)
		{
			return WithOption(o => o.WithRandomFactor(randomFactor));
		}

		/// <summary>
		/// The base for the exponential function used to compute the delay between retries, must be positive.
		/// Default value: <c>2</c>.
		/// </summary>
		public RetryPolicyOptionsBuilder WithExponentialBase(double exponentialBase)
		{
			return WithOption(o => o.WithExponentialBase(exponentialBase));
		}

		/// <summary>
		/// The coefficient for the exponential function used to compute the delay between retries, must be nonnegative.
		/// Default value: 1 second.
		/// </summary>
		public RetryPolicyOptionsBuilder WithCoefficient(TimeSpan coefficient)
		{
			return WithOption(o => o.WithCoefficient(coefficient));
		}

		/// <summary>
		/// Sets an option by cloning the extension used to store the settings. This ensures the builder does not modify options that are already in use elsewhere.
		/// </summary>
		/// <param name="setAction">An action to set the option.</param>
		/// <returns>The same builder instance so that multiple calls can be chained.</returns>
		protected virtual RetryPolicyOptionsBuilder WithOption(Func<RetryPolicyOptionsExtension, RetryPolicyOptionsExtension> setAction)
		{
			((IDataContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
				setAction(OptionsBuilder.Options.FindExtension<RetryPolicyOptionsExtension>() ?? Common.Configuration.RetryPolicy.Options));

			return this;
		}
	}
}
