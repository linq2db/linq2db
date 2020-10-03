using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents a role in the identity system
	/// </summary>
	/// <typeparam name="TKey">The type used for the primary key for the role.</typeparam>
	public interface IIdentityRole<TKey> : IConcurrency<TKey>
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the name for this role.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		///     Gets or sets the normalized name for this role.
		/// </summary>
		string NormalizedName { get; set; }
	}
}