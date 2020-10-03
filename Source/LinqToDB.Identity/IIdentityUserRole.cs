using System;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents the link between a user and a role.
	/// </summary>
	/// <typeparam name="TKey">The type of the primary key used for users and roles.</typeparam>
	public interface IIdentityUserRole<TKey> where TKey : IEquatable<TKey>
	{
		/// <summary>
		///     Gets or sets the primary key of the user that is linked to a role.
		/// </summary>
		TKey UserId { get; set; }

		/// <summary>
		///     Gets or sets the primary key of the role that is linked to the user.
		/// </summary>
		TKey RoleId { get; set; }
	}
}