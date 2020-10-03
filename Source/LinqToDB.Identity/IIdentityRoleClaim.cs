using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents a claim that is granted to all users within a role.
	/// </summary>
	/// <typeparam name="TKey">The type of the primary key of the role associated with this claim.</typeparam>
	public interface IIdentityRoleClaim<TKey> : IClameConverter
		where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the identifier for this role claim.
		/// </summary>
		int Id { get; set; }

		/// <summary>
		///     Gets or sets the of the primary key of the role associated with this claim.
		/// </summary>
		TKey RoleId { get; set; }

		/// <summary>
		///     Gets or sets the claim type for this claim.
		/// </summary>
		string ClaimType { get; set; }

		/// <summary>
		///     Gets or sets the claim value for this claim.
		/// </summary>
		string ClaimValue { get; set; }
	}
}