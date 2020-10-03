using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Concurrency interface for <see cref="IIdentityRole{TKey}" /> and <see cref="IIdentityUser{TKey}" />/>
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key.</typeparam>
	public interface IConcurrency<TKey>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the primary key.
		/// </summary>
		TKey Id { get; set; }

		/// <summary>
		///     A random value that should change whenever a role is persisted to the store
		/// </summary>
		string ConcurrencyStamp { get; set; }
	}
}